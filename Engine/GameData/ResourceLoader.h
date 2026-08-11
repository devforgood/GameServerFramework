// This file is auto-generated. Do not modify directly.
#pragma once
#include <boost/noncopyable.hpp>
#include <deque>
#include <string>
#include <unordered_map>
#include "GameDataPath.h"
#include "gamedata.h"

class ResourceLoader : private boost::noncopyable
{
private:

    std::deque<gamedata::Skill> storage_skills;
    std::unordered_map<long, const gamedata::Skill*> skills;

    std::deque<gamedata::Item> storage_items;
    std::unordered_map<long, const gamedata::Item*> items;

    std::deque<gamedata::Quest> storage_quests;
    std::unordered_map<long, const gamedata::Quest*> quests;

    std::deque<gamedata::Npc> storage_npcs;
    std::unordered_map<long, const gamedata::Npc*> npcs;

    std::deque<gamedata::GameMode> storage_game_modes;
    std::unordered_map<long, const gamedata::GameMode*> game_modes;

    std::deque<gamedata::Map> storage_maps;
    std::unordered_map<long, const gamedata::Map*> maps;

    std::deque<gamedata::Level> storage_levels;
    std::unordered_map<long, const gamedata::Level*> levels;

    std::deque<gamedata::MonsterData> storage_monsters;
    std::unordered_map<long, const gamedata::MonsterData*> monsters;

    // id 를 가진 중첩 오브젝트(게이트, 스폰 지점 등)의 인덱스.
    // 이 id 들은 파일 전체에서 유일하므로 소속 맵을 몰라도 바로 찾을 수 있고,
    // 찾은 오브젝트의 parent 가 소속 상위 데이터를 돌려준다.

    std::unordered_map<long, const gamedata::ItemItemOption*> item_item_options;

    std::unordered_map<long, const gamedata::MapGate*> map_gates;

    std::unordered_map<long, const gamedata::MapObjectsMovableObject*> map_objects_movable_objects;

    std::unordered_map<long, const gamedata::MapObjectsStaticObject*> map_objects_static_objects;

    std::unordered_map<long, const gamedata::MapSpawnPointsBossSpawn*> map_spawn_points_boss_spawns;

    std::unordered_map<long, const gamedata::MapSpawnPointsMonsterSpawn*> map_spawn_points_monster_spawns;

    std::unordered_map<long, const gamedata::MapSpawnPointsPlayerSpawn*> map_spawn_points_player_spawns;

public:
    static ResourceLoader& Instance() {
        static ResourceLoader instance;
        return instance;
    }
    bool LoadResources(const std::string& basePath = GameDataPath::Resolve());

    const std::unordered_map<long, const gamedata::Skill*>& GetSkills() const { return skills; }
    const gamedata::Skill* GetSkill(long id) const { auto itr = skills.find(id); return itr != skills.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::Item*>& GetItems() const { return items; }
    const gamedata::Item* GetItem(long id) const { auto itr = items.find(id); return itr != items.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::Quest*>& GetQuests() const { return quests; }
    const gamedata::Quest* GetQuest(long id) const { auto itr = quests.find(id); return itr != quests.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::Npc*>& GetNpcs() const { return npcs; }
    const gamedata::Npc* GetNpc(long id) const { auto itr = npcs.find(id); return itr != npcs.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::GameMode*>& GetGameModes() const { return game_modes; }
    const gamedata::GameMode* GetGameMode(long id) const { auto itr = game_modes.find(id); return itr != game_modes.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::Map*>& GetMaps() const { return maps; }
    const gamedata::Map* GetMap(long id) const { auto itr = maps.find(id); return itr != maps.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::Level*>& GetLevels() const { return levels; }
    const gamedata::Level* GetLevel(long id) const { auto itr = levels.find(id); return itr != levels.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::MonsterData*>& GetMonsterDatas() const { return monsters; }
    const gamedata::MonsterData* GetMonsterData(long id) const { auto itr = monsters.find(id); return itr != monsters.end() ? itr->second : nullptr; }


    const std::unordered_map<long, const gamedata::ItemItemOption*>& GetItemItemOptions() const { return item_item_options; }
    const gamedata::ItemItemOption* GetItemItemOption(long id) const { auto itr = item_item_options.find(id); return itr != item_item_options.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::MapGate*>& GetMapGates() const { return map_gates; }
    const gamedata::MapGate* GetMapGate(long id) const { auto itr = map_gates.find(id); return itr != map_gates.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::MapObjectsMovableObject*>& GetMapObjectsMovableObjects() const { return map_objects_movable_objects; }
    const gamedata::MapObjectsMovableObject* GetMapObjectsMovableObject(long id) const { auto itr = map_objects_movable_objects.find(id); return itr != map_objects_movable_objects.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::MapObjectsStaticObject*>& GetMapObjectsStaticObjects() const { return map_objects_static_objects; }
    const gamedata::MapObjectsStaticObject* GetMapObjectsStaticObject(long id) const { auto itr = map_objects_static_objects.find(id); return itr != map_objects_static_objects.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::MapSpawnPointsBossSpawn*>& GetMapSpawnPointsBossSpawns() const { return map_spawn_points_boss_spawns; }
    const gamedata::MapSpawnPointsBossSpawn* GetMapSpawnPointsBossSpawn(long id) const { auto itr = map_spawn_points_boss_spawns.find(id); return itr != map_spawn_points_boss_spawns.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::MapSpawnPointsMonsterSpawn*>& GetMapSpawnPointsMonsterSpawns() const { return map_spawn_points_monster_spawns; }
    const gamedata::MapSpawnPointsMonsterSpawn* GetMapSpawnPointsMonsterSpawn(long id) const { auto itr = map_spawn_points_monster_spawns.find(id); return itr != map_spawn_points_monster_spawns.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::MapSpawnPointsPlayerSpawn*>& GetMapSpawnPointsPlayerSpawns() const { return map_spawn_points_player_spawns; }
    const gamedata::MapSpawnPointsPlayerSpawn* GetMapSpawnPointsPlayerSpawn(long id) const { auto itr = map_spawn_points_player_spawns.find(id); return itr != map_spawn_points_player_spawns.end() ? itr->second : nullptr; }

private:
    ResourceLoader() = default;
    ~ResourceLoader();
    void ClearResources();

    // 로드 직후 중첩 오브젝트의 parent 를 연결하고 id 인덱스를 채운다.
    void BuildIndexes();
};