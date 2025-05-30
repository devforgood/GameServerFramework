#include "GridManager.h"
#include "Actor.h"
#include "LogHelper.h"

class GridVector : public GridManager::IGrid {  
public:  
   GridVector(int width, int height, int cellSize)  
       : width(width), height(height), cellSize(cellSize) {  
       grid.resize(width * height);  
   }  

   GridManager::Cell& get(int x, int y) override {  
       return grid[y * width + x];  
   } 

   int getWidth() const override { return width; }
   int getHeight() const override { return height; }
   int getCellSize() const override { return cellSize; }

private:  
   int width, height, cellSize;  
   std::vector<GridManager::Cell> grid;  
};

class GridHashMap : public GridManager::IGrid {
public:
	GridHashMap(int width, int height, int cellSize)
		: width(width), height(height), cellSize(cellSize) {
	}
	GridManager::Cell& get(int x, int y) override {
		auto itr = grid.find({ x, y });
		if (itr == grid.end()) {
			grid[{x, y}] = GridManager::Cell();
			return grid[{x, y}];
		}
		return itr->second;
	}
	int getWidth() const override { return width; }
	int getHeight() const override { return height; }
	int getCellSize() const override { return cellSize; }

private:
	int width, height, cellSize;
	std::unordered_map<std::pair<int, int>, GridManager::Cell, std::hash<std::pair<int, int>>> grid;
};


GridManager::GridManager(int width, int height, int cellSize)
    : NEGATIVE_VALUE_OFFSET(width*cellSize/2){
	if (width * height > 100000) {
		grid_ = new GridHashMap(width, height, cellSize); // Use GridVector or GridHashMap based on your needs
	}
    else {
        grid_ = new GridVector(width, height, cellSize); // Use GridVector or GridHashMap based on your needs
    }
}

std::pair<int, int> GridManager::getCellCoord(float x, float y) {
    return { static_cast<int>(x+NEGATIVE_VALUE_OFFSET) / grid_->getCellSize(), static_cast<int>(y + NEGATIVE_VALUE_OFFSET) / grid_->getCellSize() };
}

void GridManager::enterCell(Actor* actor, int x, int y) {
    if (x < 0 || x >= grid_->getWidth() || y < 0 || y >= grid_->getHeight()) return;

    auto& cell = grid_->get(x, y);
    if (actor->type() == syncnet::GameObjectType::GameObjectType_Character)
        cell.characters.insert(actor);
    else
        cell.monsters.insert(actor);

    LOG.info("Actor {} entered cell ({}, {})", actor->agent_id(), x, y);
}

void GridManager::leaveCell(Actor* actor, int x, int y) {
    if (x < 0 || x >= grid_->getWidth() || y < 0 || y >= grid_->getHeight()) return;

    auto& cell =  grid_->get(x, y);;
    if (actor->type() == syncnet::GameObjectType::GameObjectType_Character)
        cell.characters.erase(actor);
    else
        cell.monsters.erase(actor);

    LOG.info("Actor {} left cell ({}, {})", actor->agent_id(), x, y);
}

void GridManager::add(Actor* actor) {
    auto [cx, cy] = getCellCoord(actor->get_vecter2_x(), actor->get_vecter2_y());
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
}

void GridManager::remove(Actor* actor) {
    leaveCell(actor, actor->gridX, actor->gridY);
}

std::vector<Actor*> GridManager::getEntitiesInViewRange(Actor* viewer, float range) {
    std::vector<Actor*> result;
    auto [cx, cy] = getCellCoord(viewer->get_vecter2_x(), viewer->get_vecter2_y());
    int cells = static_cast<int>(std::ceil(range / grid_->getCellSize()));

    for (int dx = -cells; dx <= cells; ++dx) {
        for (int dy = -cells; dy <= cells; ++dy) {
            int x = cx + dx;
            int y = cy + dy;
            if (x < 0 || y < 0 || x >= grid_->getWidth() || y >= grid_->getHeight()) continue;

            auto& cell = grid_->get(x, y);
            for (auto* e : cell.characters)
                if (e != viewer) result.push_back(e);
            for (auto* e : cell.monsters)
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
    int cells = static_cast<int>(std::ceil(range / grid_->getCellSize()));

    float rangeSq = range * range;
    float dirRad = dirDeg * 3.1415926f / 180.0f;

    for (int dx = -cells; dx <= cells; ++dx) {
        for (int dy = -cells; dy <= cells; ++dy) {
            int nx = cx + dx;
            int ny = cy + dy;
            if (nx < 0 || ny < 0 || nx >= grid_->getWidth() || ny >= grid_->getHeight()) continue;

            auto& cell = grid_->get(nx, ny); // <-- 수정: nx, ny 사용

            for (auto* e : cell.characters) {
                float ex = e->get_vecter2_x();
                float ey = e->get_vecter2_y();
                float dx = ex - x;
                float dy = ey - y;
                float distSq = dx * dx + dy * dy;
                if (distSq <= rangeSq) {
                    result.push_back(e);
                }
            }

            for (auto* e : cell.monsters) {
                float ex = e->get_vecter2_x();
                float ey = e->get_vecter2_y();
                float dx = ex - x;
                float dy = ey - y;
                float distSq = dx * dx + dy * dy;
                if (distSq <= rangeSq) {
                    result.push_back(e);
                }
            }
        }
    }
    return result;
}