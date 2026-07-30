#pragma once
// ----------------------------------------
// GridManager 확장 (시야 체크, 타입별 저장, broadcast)
// ----------------------------------------
#include <unordered_set>
#include <vector>
#include <cmath>
#include <iostream>
#include <functional>
#include "IGridActor.h"

namespace std {
    template <>
    struct hash<std::pair<int, int>> {
        size_t operator()(const std::pair<int, int>& p) const {
            return std::hash<int>()(p.first) ^ (std::hash<int>()(p.second) << 1);
        }
    };
}



class GridManager {
public:
    // 맵 원점(셀 (0,0) 의 월드 좌표)을 명시하는 형태. 네비메시 경계가 원점 대칭이 아닌
    // 맵(예: x [-26.3, 25])도 그대로 담을 수 있다.
    GridManager(int width, int height, int cellSize, float originX, float originZ);

    // 원점 대칭 맵을 위한 축약형 — 원점을 (-width*cellSize/2, -height*cellSize/2) 로 잡는다.
    GridManager(int width, int height, int cellSize);

    // 그리드가 담당하는 월드 범위. 이 밖의 좌표는 어떤 셀에도 속하지 않는다.
    float OriginX() const { return originX_; }
    float OriginZ() const { return originZ_; }

    void add(IGridActor* actor);
    void move(IGridActor* actor, float newX, float newY);
    void remove(IGridActor* actor);

    std::vector<IGridActor*> getEntitiesInViewRange(IGridActor* viewer, float range);

    // 시야 범위(원형) 안의 '캐릭터'만 out 에 채운다.
    //  - 몬스터 셀은 순회하지 않는다(적 탐지는 캐릭터만 대상이므로 몬스터 밀집 시 비용이 사라진다).
    //  - 셀 단위가 아닌 실제 거리(range)로 컬링한다.
    //  - out 의 기존 용량을 재사용하므로(호출 측이 버퍼를 보관) 호출당 힙 할당이 없다.
    void getCharactersInViewRange(IGridActor* viewer, float range, std::vector<IGridActor*>& out);

    void broadcastToNearby(float x, float y, float range, const std::string& msg);
    std::vector<IGridActor*> getEntitiesInAoEMask(float x, float y, float range, float dirDeg);
    std::vector<IGridActor*> getEntitiesInAoEMask(float x, float y, float range, float dirDeg, float angle);

    // ── 관심영역(AoI) 구독 ──
    // 뷰어는 자기 주변 셀들을 구독하고, 액터 변경은 그 액터가 속한 셀의 구독자에게만 전달된다.
    // 셀 좌표 계산/유효성 판단이 여기 모여 있으므로 구독 관리도 그리드가 맡는다.
    std::pair<int, int> CellOf(float x, float y);        // 월드 좌표 → 셀 좌표
    int CellRadius(float range) const;                   // 반경(월드 단위) → 셀 개수
    bool IsValidCell(int cellX, int cellY) const;

    void Subscribe(int cellX, int cellY, int viewerSlot);
    void Unsubscribe(int cellX, int cellY, int viewerSlot);
    // 구독자 슬롯 목록. 셀이 범위 밖이면 비어 있는 목록을 돌려준다(호출 측 분기 제거).
    const std::vector<int>& SubscribersOf(int cellX, int cellY);

    // 셀 안의 모든 액터(캐릭터+몬스터)를 out 에 덧붙인다(비우지 않는다).
    void CollectActorsInCell(int cellX, int cellY, std::vector<IGridActor*>& out);

    // 셀 내용은 vector 로 담는다. unordered_set 은 원소마다 노드를 할당/해제해서
    // 셀 이동(leave+enter)이 셀 내 이동보다 13배 비쌌고, 순회도 포인터 추적이라 캐시에 불리했다.
    // 제거는 마지막 원소와 swap 후 pop 이라 O(1) 이며, 각 액터는 자신의 인덱스를 GridSlot 으로 들고 있다.
    struct Cell {
        std::vector<IGridActor*> characters;
        std::vector<IGridActor*> monsters;
        // 이 셀을 관심영역에 포함한 뷰어들의 '슬롯 인덱스'.
        // 액터가 바뀌면 그 액터가 있는 셀의 구독자에게만 전달하면 되므로,
        // 전달 비용이 '전체 플레이어 수'가 아니라 '그 자리를 보고 있는 사람 수'가 된다.
        // 값의 의미는 소유자(Map)가 정한다 — 그래야 전달 시 해시 조회 없이 배열 색인으로 끝난다.
        std::vector<int> subscribers;
    };

    class IGrid
    {
    public:
        virtual ~IGrid() = default;
        virtual Cell& get(int x, int y) = 0;
		virtual int getWidth() const = 0;
		virtual int getHeight() const = 0;
		virtual int getCellSize() const = 0;
    };

private:


    IGrid* grid_;

    // 맵 전체의 캐릭터 목록. 적 탐지는 캐릭터만 찾는데, 셀 스캔은 캐릭터가 하나도 없어도
    // 시야 반경의 모든 셀(예: 반경 10/셀 2 → 121칸)을 방문한다. 캐릭터 수가 그보다 적으면
    // 이 목록을 직접 훑는 편이 싸다(getCharactersInViewRange 가 둘 중 싼 쪽을 고른다).
    std::vector<IGridActor*> characters_;

    std::pair<int, int> getCellCoord(float x, float y);
    void enterCell(IGridActor* actor, int x, int y);
    void leaveCell(IGridActor* actor, int x, int y);

    const float originX_;
    const float originZ_;
    const float invCellSize_;   // 셀 좌표 계산에서 나눗셈을 없애기 위한 역수.
};