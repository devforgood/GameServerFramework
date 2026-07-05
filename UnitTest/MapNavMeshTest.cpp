#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include <string>
#include <vector>
#include "spdlog/spdlog.h"
#include "spdlog/sinks/stdout_sinks.h"

#include "GameData/ResourceLoader.h"
#include "Map.h"
#include "NavMesh.h"

namespace
{
	// 테스트 실행 위치에 따라 GameData 경로가 달라지므로 후보들을 순회한다.
	std::string ResolveGameDataPath()
	{
		const std::vector<std::string> candidates = {
			"GameData/",
			"../UnitTest/GameData/",
			"../../UnitTest/GameData/",
		};
		for (const auto& p : candidates)
		{
			if (std::filesystem::exists(p + "Map.json"))
				return p;
		}
		return "";
	}

	// Map::ValidateMapDataOnNavMesh 는 LOG("net") 로거를 사용한다.
	// 테스트 하네스에는 등록돼 있지 않으므로 stdout 로거를 등록해 정합 로그를 보이게 한다.
	void EnsureNetLogger()
	{
		if (!spdlog::get("net"))
		{
			auto logger = std::make_shared<spdlog::logger>(
				"net", std::make_shared<spdlog::sinks::stdout_sink_mt>());
			logger->set_level(spdlog::level::debug);
			spdlog::register_logger(logger);
		}
	}
}

//---------------------------------------------------------------------------------------
// 씬-navmesh 정합: navmesh 바이너리가 배치된 맵(field 월드 맵)의 게이트/스폰 마커가
// 모두 navmesh 위에 있어야 한다. 씬을 수정하고 navmesh 재생성을 잊으면 여기서 잡힌다.
//---------------------------------------------------------------------------------------

TEST(MapNavMeshTest, MarkersAreOnNavMesh)
{
	EnsureNetLogger();

	std::string dataPath = ResolveGameDataPath();
	ASSERT_FALSE(dataPath.empty()) << "GameData/Map.json 을 찾지 못했습니다.";
	ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath)) << "LoadResources 실패";

	int validated = 0;
	for (const auto& [id, mapData] : ResourceLoader::Instance().GetMaps())
	{
		if (mapData->navmesh_path.empty())
			continue;

		std::string binPath = dataPath + mapData->navmesh_path;
		if (!std::filesystem::exists(binPath))
			continue; // navmesh 미배치 맵(씬 없는 샘플 맵)은 검증 대상이 아니다

		NavMesh nav;
		ASSERT_TRUE(nav.Load(binPath.c_str())) << "navmesh 로드 실패: " << binPath;

		EXPECT_EQ(Map::ValidateMapDataOnNavMesh(mapData, &nav), 0)
			<< "map " << id << " (" << mapData->name << ") 의 게이트/스폰 마커가 navmesh 밖에 있습니다.";
		++validated;
	}

	EXPECT_GT(validated, 0) << "navmesh 가 배치된 맵이 하나도 없어 정합 검증이 수행되지 않았습니다.";
}
