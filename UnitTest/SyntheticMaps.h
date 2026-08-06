#pragma once
#include <cstdlib>
#include <deque>
#include <string>
#include <utility>
#include <vector>

#include "gamedata.h"
#include "syncnet_generated.h"

// 존 그래프 검증/측정용 합성 맵 생성기.
//
// Map.json 의 실제 데이터로는 존 그래프를 제대로 확인할 수 없다. 맵이 6개뿐이라
// 최단 경로가 하나밖에 없고(=고를 것이 없다), 정점 수도 측정에 의미가 없을 만큼 적다.
// 그래서 **정답을 미리 아는** 위상을 만들어 쓴다.
//
// 만드는 것은 W×H 격자 세계다. 맵 하나가 격자 한 칸이고, 상하좌우로 붙은 맵끼리
// two_way 게이트로 이어진다. 이 위상에서는 두 맵 사이의 최소 맵 전환 횟수가
// 맨해튼 거리와 같으므로, 탐색 결과를 손으로 검산할 수 있다.
//
// (x, y) 칸의 맵 좌표계는 맵마다 독립이다 — 실제로도 맵마다 자기 씬 좌표를 쓴다.
// 각 맵은 kMapExtent 크기의 정사각형이고, 게이트는 이웃 쪽 변에 놓인다.
//
// 이 헤더는 UnitTest 와 Benchmark 가 함께 쓴다(Benchmark 는 상대 경로로 포함한다).
// 둘 다 '같은 그래프'를 봐야 측정치와 검증이 같은 대상을 가리킨다.
namespace synthetic
{
	// 맵 한 변의 길이(유닛). 게이트는 중심에서 이 값의 절반쯤 떨어진 변에 놓인다.
	constexpr double kMapExtent = 100.0;

	// id 대역. 실제 데이터(게이트 1000번대, 스폰 10000번대)와 겹치지 않게 띄워 둔다.
	constexpr int kMapIdBase = 900000;
	constexpr int kGateIdBase = 9000000;

	// 격자 세계. 맵 실체를 deque 에 담아 주소를 고정한다
	// (vector 는 커질 때 재할당하며 원소가 이사하므로 포인터를 들고 있을 수 없다).
	struct GridWorld
	{
		std::deque<gamedata::Map> storage;
		std::vector<const gamedata::Map*> maps;

		int width = 0;
		int height = 0;
		int gatesPerNeighbor = 1;

		// (x, y) 칸의 맵 id.
		int MapIdAt(int x, int y) const { return kMapIdBase + y * width + x; }

		// 맵 중심(각 맵의 로컬 좌표계 원점).
		static syncnet::Vec3 Center() { return syncnet::Vec3(0.0f, 0.0f, 0.0f); }

		// (x0,y0) -> (x1,y1) 의 최소 맵 전환 횟수. 격자라 맨해튼 거리와 같다.
		static int ManhattanDistance(int x0, int y0, int x1, int y1)
		{
			return std::abs(x1 - x0) + std::abs(y1 - y0);
		}

		size_t GateCount() const
		{
			size_t count = 0;
			for (const gamedata::Map* map : maps)
				count += map->gates.size();
			return count;
		}
	};

	namespace detail
	{
		// 방향 인덱스: 0=동, 1=서, 2=남, 3=북. 반대 방향은 ^1 로 얻는다.
		inline void DirDelta(int dir, int& dx, int& dy)
		{
			switch (dir)
			{
			case 0: dx = 1;  dy = 0;  break;
			case 1: dx = -1; dy = 0;  break;
			case 2: dx = 0;  dy = 1;  break;
			default: dx = 0; dy = -1; break;
			}
		}

		// (칸 인덱스, 방향, 병렬 게이트 번호) -> 전역 유일 게이트 id.
		inline int GateId(int cellIndex, int dir, int lane, int gatesPerNeighbor)
		{
			return kGateIdBase + (cellIndex * 4 + dir) * gatesPerNeighbor + lane;
		}
	}

	// W×H 격자 세계를 만든다.
	//
	// gatesPerNeighbor 는 이웃 한 쌍을 잇는 '문의 개수' 다. 1이면 맵마다 최대 4개의
	// 게이트가 생기고, 늘리면 같은 이웃으로 가는 문이 여러 개 생긴다 — 맵 안의 도보
	// 간선이 게이트 수의 제곱으로 늘어나므로, 빌드 비용의 지배항을 흔들어 볼 때 쓴다.
	//
	// gameModeId 는 field 모드의 id 여야 경로가 그 맵을 지나갈 수 있다(인스턴스 맵은
	// 목적지일 때만 지나간다). 기본값 1 은 GameMode.json 의 Field 다.
	inline GridWorld MakeGridWorld(int width, int height, int gatesPerNeighbor = 1, int gameModeId = 1)
	{
		GridWorld world;
		world.width = width;
		world.height = height;
		world.gatesPerNeighbor = gatesPerNeighbor;

		const double edge = kMapExtent * 0.45;   // 변에서 게이트까지의 거리.
		// 병렬 게이트를 변을 따라 흩어 놓는다(같은 자리에 겹치면 도보 비용이 0 이 되어
		// '문을 고른다' 는 상황 자체가 사라진다).
		const double lanePitch = kMapExtent * 0.6 / (gatesPerNeighbor + 1);

		for (int y = 0; y < height; ++y)
		{
			for (int x = 0; x < width; ++x)
			{
				gamedata::Map map;
				map.id = world.MapIdAt(x, y);
				map.name = "grid_" + std::to_string(x) + "_" + std::to_string(y);
				map.scene = map.name;
				map.game_mode_id = gameModeId;
				map.size.width = kMapExtent;
				map.size.height = kMapExtent;

				const int cellIndex = y * width + x;
				for (int dir = 0; dir < 4; ++dir)
				{
					int dx = 0, dy = 0;
					detail::DirDelta(dir, dx, dy);
					const int nx = x + dx;
					const int ny = y + dy;
					if (nx < 0 || nx >= width || ny < 0 || ny >= height)
						continue;   // 바깥으로 나가는 문은 없다.

					const int neighborIndex = ny * width + nx;
					for (int lane = 0; lane < gatesPerNeighbor; ++lane)
					{
						const double offset = lanePitch * (lane + 1) - kMapExtent * 0.3;

						gamedata::MapGate gate;
						gate.id = detail::GateId(cellIndex, dir, lane, gatesPerNeighbor);
						gate.name = "gate_" + std::to_string(gate.id);
						gate.type = "two_way";
						gate.required_level = 1;
						// 짝: 이웃 맵의 반대 방향, 같은 번호의 문.
						gate.target_id = detail::GateId(neighborIndex, dir ^ 1, lane, gatesPerNeighbor);

						// 이웃 쪽 변 위에, 문 번호만큼 변을 따라 밀어 놓는다.
						gate.position.x = dx != 0 ? edge * dx : offset;
						gate.position.y = 0.0;
						gate.position.z = dy != 0 ? edge * dy : offset;

						map.gates.push_back(gate);
					}
				}

				world.storage.push_back(std::move(map));
			}
		}

		world.maps.reserve(world.storage.size());
		for (const gamedata::Map& map : world.storage)
			world.maps.push_back(&map);
		return world;
	}
}
