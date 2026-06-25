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

    struct Cell {
        std::unordered_set<IGridActor*> characters;
        std::unordered_set<IGridActor*> monsters;
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

    std::pair<int, int> getCellCoord(float x, float y);
    void enterCell(IGridActor* actor, int x, int y);
    void leaveCell(IGridActor* actor, int x, int y);

    const int NEGATIVE_VALUE_OFFSET;
};