#include "GridManager.h"
#include <array>
#include <immintrin.h> // SIMD(Single Instruction Multiple Data) 연산을 위한 헤더

// ===== 기본 상수 정의 =====
constexpr float PI = 3.1415926535897932f;        // 원주율
constexpr float TWO_PI = 2.0f * PI;              // 2π
constexpr float PI_180 = PI / 180.0f;            // 도(degree)를 라디안으로 변환하는 상수
constexpr float EPSILON = 1e-6f;                 // 부동소수점 비교를 위한 오차 허용값

// ===== 삼각함수 Look-up 테이블 클래스 =====
// 자주 사용되는 삼각함수 값을 미리 계산하여 저장하고 빠르게 조회하는 클래스
class TrigLookupTable {
private:
    // 테이블 크기 설정
    static constexpr size_t SAMPLES_PER_QUADRANT = 64;    // 90도당 64개의 샘플 (정밀도와 메모리 사용량의 균형)
    static constexpr size_t TABLE_SIZE = SAMPLES_PER_QUADRANT * 4;  // 전체 360도에 대한 샘플 수
    static constexpr float ANGLE_STEP = TWO_PI / TABLE_SIZE;        // 각 샘플 간의 각도 간격

    // SIMD 연산을 위해 32바이트 경계에 정렬된 테이블
    alignas(32) std::array<float, TABLE_SIZE> sin_table;  // sine 값 테이블
    alignas(32) std::array<float, TABLE_SIZE> cos_table;  // cosine 값 테이블

    // 선형 보간을 위한 보조 함수
    // a와 b 사이의 t 비율만큼의 값을 반환 (0 <= t <= 1)
    float lerp(float a, float b, float t) const {
        return a + t * (b - a);
    }

public:
    // 생성자: 테이블 초기화
    TrigLookupTable() {
        for (size_t i = 0; i < TABLE_SIZE; ++i) {
            float angle = static_cast<float>(i) * ANGLE_STEP;
            sin_table[i] = std::sin(angle);  // sine 값 계산 및 저장
            cos_table[i] = std::cos(angle);  // cosine 값 계산 및 저장
        }
    }

    // sine 값을 계산하는 함수 (선형 보간 사용)
    float sin(float angle) const {
        // 각도를 0 ~ 2π 범위로 정규화
        float normalized = std::fmod(angle + TWO_PI, TWO_PI);
        // 테이블 인덱스 계산
        float indexF = normalized / ANGLE_STEP;
        size_t index1 = static_cast<size_t>(indexF) % TABLE_SIZE;
        size_t index2 = (index1 + 1) % TABLE_SIZE;
        // 보간 비율 계산
        float t = indexF - std::floor(indexF);
        // 선형 보간된 값 반환
        return lerp(sin_table[index1], sin_table[index2], t);
    }

    // cosine 값을 계산하는 함수 (선형 보간 사용)
    float cos(float angle) const {
        float normalized = std::fmod(angle + TWO_PI, TWO_PI);
        float indexF = normalized / ANGLE_STEP;
        size_t index1 = static_cast<size_t>(indexF) % TABLE_SIZE;
        size_t index2 = (index1 + 1) % TABLE_SIZE;
        float t = indexF - std::floor(indexF);
        return lerp(cos_table[index1], cos_table[index2], t);
    }

    // arctangent를 계산하는 함수
    // Look-up 테이블을 사용하지 않는 이유: 정확도가 중요하고 자주 호출되지 않음
    float atan2(float y, float x) const {
        float angle = std::atan2(y, x);
        if (angle < 0) angle += TWO_PI;  // 각도를 0 ~ 2π 범위로 조정
        return angle;
    }
};

// 전역 Look-up 테이블 인스턴스 (프로그램 시작 시 한 번만 초기화됨)
static const TrigLookupTable trigTable;

// ===== SIMD를 사용한 거리 계산 함수 =====
// AVX2 명령어를 사용하여 8개의 거리를 동시에 계산
inline void calculateDistancesSIMD(const float* x_coords, const float* y_coords, 
                                 float center_x, float center_y, float* distances_sq, 
                                 int count) {
    // 중심점 좌표를 8개의 동일한 값으로 복제
    __m256 centerX = _mm256_set1_ps(center_x);
    __m256 centerY = _mm256_set1_ps(center_y);

    // 8개씩 묶어서 처리
    for (int i = 0; i < count; i += 8) {
        // 8개의 x, y 좌표를 동시에 로드
        __m256 x = _mm256_loadu_ps(&x_coords[i]);
        __m256 y = _mm256_loadu_ps(&y_coords[i]);

        // 중심점과의 차이 계산 (8개 동시 처리)
        __m256 dx = _mm256_sub_ps(x, centerX);
        __m256 dy = _mm256_sub_ps(y, centerY);

        // 거리의 제곱 계산: dx*dx + dy*dy (8개 동시 처리)
        __m256 distSq = _mm256_add_ps(
            _mm256_mul_ps(dx, dx),
            _mm256_mul_ps(dy, dy)
        );

        // 결과 저장
        _mm256_storeu_ps(&distances_sq[i], distSq);
    }
}

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

void GridManager::enterCell(IGridActor* actor, int x, int y) {
    if (x < 0 || x >= grid_->getWidth() || y < 0 || y >= grid_->getHeight()) return;

    auto& cell = grid_->get(x, y);
    if (actor->isCharacter())
        cell.characters.insert(actor);
    else
        cell.monsters.insert(actor);

    //LOG.info("Actor {} entered cell ({}, {})", actor->agent_id(), x, y);
}

void GridManager::leaveCell(IGridActor* actor, int x, int y) {
    if (x < 0 || x >= grid_->getWidth() || y < 0 || y >= grid_->getHeight()) return;

    auto& cell =  grid_->get(x, y);;
    if (actor->isCharacter())
        cell.characters.erase(actor);
    else
        cell.monsters.erase(actor);

    //LOG.info("Actor {} left cell ({}, {})", actor->agent_id(), x, y);
}

void GridManager::add(IGridActor* actor) {
    auto [cx, cy] = getCellCoord(actor->getVector2X(), actor->getVector2Y());
    actor->setGridX(cx);
    actor->setGridY(cy);
    enterCell(actor, cx, cy);
}

void GridManager::move(IGridActor* actor, float newX, float newY) {
    auto [newCX, newCY] = getCellCoord(newX, newY);
    if (newCX != actor->getGridX() || newCY != actor->getGridY()) {
        leaveCell(actor, actor->getGridX(), actor->getGridY());
        enterCell(actor, newCX, newCY);
        actor->setGridX(newCX);
        actor->setGridY(newCY);
    }
}

void GridManager::remove(IGridActor* actor) {
    leaveCell(actor, actor->getGridX(), actor->getGridY());
}

std::vector<IGridActor*> GridManager::getEntitiesInViewRange(IGridActor* viewer, float range) {
    std::vector<IGridActor*> result;
    auto [cx, cy] = getCellCoord(viewer->getVector2X(), viewer->getVector2Y());
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
                if (e != viewer) result.push_back(e);  // 몬스터도 자신을 제외
        }
    }
    return result;
}

void GridManager::broadcastToNearby(float x, float y, float range, const std::string& msg) {
    auto entities = getEntitiesInAoEMask(x, y, range, 0);
    for (auto* e : entities) {
        std::cout << "Broadcast to Entity " << e->getAgentID() << ": " << msg << "\n";
    }
}

// ===== AoE(Area of Effect) 범위 내의 엔티티들을 찾는 함수 =====
std::vector<IGridActor*> GridManager::getEntitiesInAoEMask(float x, float y, float range, float angle) {
    std::vector<IGridActor*> result;
    auto [cx, cy] = getCellCoord(x, y);  // 중심점의 그리드 좌표 계산
    int cells = static_cast<int>(std::ceil(range / grid_->getCellSize()));  // 검사할 셀의 범위 계산

    float rangeSq = range * range;  // 거리 비교를 위해 제곱값 미리 계산
    float dirRad = angle * PI_180;  // 각도를 라디안으로 변환
    bool isFullCircle = angle >= 360.0f || angle <= 0.0f;  // 완전한 원형 여부 확인
    float halfAngleRad = (angle * 0.5f) * PI_180;  // 부채꼴의 반각 계산

    // SIMD 처리를 위한 임시 버퍼 선언
    static constexpr size_t MAX_BATCH_SIZE = 256;  // 한 번에 처리할 최대 엔티티 수
    alignas(32) float x_coords[MAX_BATCH_SIZE];    // x 좌표 배열 (32바이트 정렬)
    alignas(32) float y_coords[MAX_BATCH_SIZE];    // y 좌표 배열 (32바이트 정렬)
    alignas(32) float distances_sq[MAX_BATCH_SIZE]; // 거리 제곱 배열 (32바이트 정렬)
    std::vector<IGridActor*> batch_actors;              // 배치 처리할 엔티티들의 임시 저장소
    batch_actors.reserve(MAX_BATCH_SIZE);

    // 주변 셀들을 순회
    for (int dx = -cells; dx <= cells; ++dx) {
        for (int dy = -cells; dy <= cells; ++dy) {
            int nx = cx + dx;
            int ny = cy + dy;
            // 그리드 범위를 벗어나면 건너뜀
            if (nx < 0 || ny < 0 || nx >= grid_->getWidth() || ny >= grid_->getHeight()) continue;

            auto& cell = grid_->get(nx, ny);

            // 현재 셀의 모든 엔티티를 배치 처리 목록에 추가
            batch_actors.clear();
            batch_actors.insert(batch_actors.end(), cell.characters.begin(), cell.characters.end());
            batch_actors.insert(batch_actors.end(), cell.monsters.begin(), cell.monsters.end());

            // 배치 단위로 SIMD 처리 수행
            for (size_t batch_start = 0; batch_start < batch_actors.size(); batch_start += MAX_BATCH_SIZE) {
                size_t batch_size = std::min(MAX_BATCH_SIZE, batch_actors.size() - batch_start);

                // 현재 배치의 좌표들을 배열에 수집
                for (size_t i = 0; i < batch_size; ++i) {
                    IGridActor* e = batch_actors[batch_start + i];
                    x_coords[i] = e->getVector2X();
                    y_coords[i] = e->getVector2Y();
                }

                // SIMD를 사용하여 거리 계산
                calculateDistancesSIMD(x_coords, y_coords, x, y, distances_sq, batch_size);

                // 각 엔티티에 대한 결과 처리
                for (size_t i = 0; i < batch_size; ++i) {
                    if (distances_sq[i] <= rangeSq) {  // 거리가 범위 내에 있는 경우
                        IGridActor* e = batch_actors[batch_start + i];
                        if (isFullCircle) {  // 완전한 원형인 경우
                            result.push_back(e);
                        } else {  // 부채꼴인 경우
                            // 엔티티의 각도 계산
                            float dx = x_coords[i] - x;
                            float dy = y_coords[i] - y;
                            float entityAngle = trigTable.atan2(dy, dx);
                            
                            // 각도 차이 계산 및 보정
                            float angleDiff = std::abs(entityAngle - dirRad);
                            if (angleDiff > PI) {
                                angleDiff = TWO_PI - angleDiff;
                            }
                            
                            // 부채꼴 범위 내에 있는지 확인
                            if (angleDiff <= halfAngleRad + EPSILON) {
                                result.push_back(e);
                            }
                        }
                    }
                }
            }
        }
    }
    return result;
}