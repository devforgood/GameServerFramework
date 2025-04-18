#include "GridManager.h"
#include "Actor.h"
#include "LogHelper.h"


GridManager::GridManager(int width, int height, int cellSize)
    : width(width), height(height), cellSize(cellSize), NEGATIVE_VALUE_OFFSET(width*cellSize/2){
    grid.resize(width, std::vector<Cell>(height));
}

std::pair<int, int> GridManager::getCellCoord(float x, float y) {
    return { static_cast<int>(x+NEGATIVE_VALUE_OFFSET) / cellSize, static_cast<int>(y+NEGATIVE_VALUE_OFFSET) / cellSize };
}

void GridManager::enterCell(Actor* actor, int x, int y) {
    if (x < 0 || x >= width || y < 0 || y >= height) return;
    if (actor->GetType() == syncnet::GameObjectType::GameObjectType_Character)
        grid[x][y].characters.insert(actor);
    else
        grid[x][y].monsters.insert(actor);

	LOG.info("Actor {} entered cell ({}, {})", actor->agent_id(), x, y);
}

void GridManager::leaveCell(Actor* actor, int x, int y) {
    if (x < 0 || x >= width || y < 0 || y >= height) return;
    if (actor->GetType() == syncnet::GameObjectType::GameObjectType_Character)
        grid[x][y].characters.erase(actor);
    else
        grid[x][y].monsters.erase(actor);

	LOG.info("Actor {} left cell ({}, {})", actor->agent_id(), x, y);
}

void GridManager::add(Actor* actor) {
    auto [cx, cy] = getCellCoord(actor->x, actor->y);
    actor->gridX = cx;
    actor->gridY = cy;
    enterCell(actor, cx, cy);
}

void GridManager::move(Actor* actor, float newX, float newY) {
    auto [newCX, newCY] = getCellCoord(newX, newY);
    if (newCX != actor->gridX || newCY != actor->gridY) {
        leaveCell(actor, actor->gridX, actor->gridY);
        enterCell(actor, newCX, newCY);
        actor->gridX = newCX;
        actor->gridY = newCY;
    }
    actor->x = newX;
    actor->y = newY;
}

void GridManager::remove(Actor* actor) {
    leaveCell(actor, actor->gridX, actor->gridY);
}

std::vector<Actor*> GridManager::getEntitiesInViewRange(Actor* viewer, float range) {
    std::vector<Actor*> result;
    auto [cx, cy] = getCellCoord(viewer->x, viewer->y);
    int cells = static_cast<int>(std::ceil(range / cellSize));

    for (int dx = -cells; dx <= cells; ++dx) {
        for (int dy = -cells; dy <= cells; ++dy) {
            int x = cx + dx;
            int y = cy + dy;
            if (x < 0 || y < 0 || x >= width || y >= height) continue;
            for (auto* e : grid[x][y].characters)
                if (e != viewer) result.push_back(e);
            for (auto* e : grid[x][y].monsters)
                result.push_back(e);
        }
    }
    return result;
}

void GridManager::broadcastToNearby(float x, float y, float range, const std::string& msg) {
    auto entities = getEntitiesInAoEMask(x, y, range, 0);
    for (auto* e : entities) {
        std::cout << "Broadcast to Entity " << e->agent_id() << ": " << msg << "\n";
    }
}

std::vector<Actor*> GridManager::getEntitiesInAoEMask(float x, float y, float range, float dirDeg) {
    std::vector<Actor*> result;
    auto [cx, cy] = getCellCoord(x, y);
    int cells = static_cast<int>(std::ceil(range / cellSize));

    float rangeSq = range * range;
    float dirRad = dirDeg * 3.1415926f / 180.0f;

    for (int dx = -cells; dx <= cells; ++dx) {
        for (int dy = -cells; dy <= cells; ++dy) {
            int nx = cx + dx;
            int ny = cy + dy;
            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

            for (auto* e : grid[nx][ny].characters) {
                float dx = e->x - x;
                float dy = e->y - y;
                float distSq = dx * dx + dy * dy;
                if (distSq <= rangeSq) {
                    result.push_back(e);
                }
            }

            for (auto* e : grid[nx][ny].monsters) {
                float dx = e->x - x;
                float dy = e->y - y;
                float distSq = dx * dx + dy * dy;
                if (distSq <= rangeSq) {
                    result.push_back(e);
                }
            }
        }
    }
    return result;
}