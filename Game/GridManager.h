#pragma once
// ----------------------------------------
// GridManager 확장 (시야 체크, 타입별 저장, broadcast)
// ----------------------------------------
#include <unordered_set>
#include <vector>
#include <cmath>
#include <iostream>
#include <functional>

namespace std {
    template <>
    struct hash<std::pair<int, int>> {
        size_t operator()(const std::pair<int, int>& p) const {
            return std::hash<int>()(p.first) ^ (std::hash<int>()(p.second) << 1);
        }
    };
}



class Actor;
class GridManager {
public:
    GridManager(int width, int height, int cellSize);

    void add(Actor* actor);
    void move(Actor* actor, float newX, float newY);
    void remove(Actor* actor);

    std::vector<Actor*> getEntitiesInViewRange(Actor* viewer, float range);
    void broadcastToNearby(float x, float y, float range, const std::string& msg);
    std::vector<Actor*> getEntitiesInAoEMask(float x, float y, float range, float dirDeg);

    struct Cell {
        std::unordered_set<Actor*> characters;
        std::unordered_set<Actor*> monsters;
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
    void enterCell(Actor* actor, int x, int y);
    void leaveCell(Actor* actor, int x, int y);

    const int NEGATIVE_VALUE_OFFSET;
};