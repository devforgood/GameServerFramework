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
    GridManager(int width, int height, int cellSize);

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

    // 셀 내용은 vector 로 담는다. unordered_set 은 원소마다 노드를 할당/해제해서
    // 셀 이동(leave+enter)이 셀 내 이동보다 13배 비쌌고, 순회도 포인터 추적이라 캐시에 불리했다.
    // 제거는 마지막 원소와 swap 후 pop 이라 O(1) 이며, 각 액터는 자신의 인덱스를 GridSlot 으로 들고 있다.
    struct Cell {
        std::vector<IGridActor*> characters;
        std::vector<IGridActor*> monsters;
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

    const int NEGATIVE_VALUE_OFFSET;
};