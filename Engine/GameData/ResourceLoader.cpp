// This file is auto-generated. Do not modify directly.
#include "ResourceLoader.h"
#include <cstdio>
#include <deque>
#include <fstream>
#include <future>
#include <unordered_map>
#include <utility>
#include <vector>
#include <nlohmann/json.hpp>
#include "gamedata.h"

namespace {
// Parse a JSON array file (e.g. skill.json) into an owning deque + id->ptr map.
// The deque keeps element addresses stable, so the map may store raw pointers.
template<typename TMessage>
bool LoadJsonFile(
    const std::string& filename,
    std::deque<TMessage>& storage,
    std::unordered_map<long, const TMessage*>& map)
{
    std::ifstream input(filename);
    if (!input) {
        printf("[ERROR] %s file open failed.\n", filename.c_str());
        return false;
    }

    nlohmann::json doc;
    try {
        input >> doc;
    } catch (const std::exception& e) {
        printf("[ERROR] %s parse failed: %s\n", filename.c_str(), e.what());
        return false;
    }

    if (!doc.is_array()) {
        printf("[ERROR] %s is not a JSON array.\n", filename.c_str());
        return false;
    }

    for (const auto& entry : doc) {
        TMessage obj{};
        try {
            gamedata::from_json(entry, obj);
        } catch (const std::exception& e) {
            printf("[ERROR] %s entry parse failed: %s\n", filename.c_str(), e.what());
            return false;
        }
        storage.push_back(std::move(obj));
        long id = static_cast<long>(storage.back().id);
        if (map.count(id)) {
            printf("[WARN] %s duplicate id=%ld, overwriting.\n", filename.c_str(), id);
        }
        map[id] = &storage.back();
    }
    return true;
}
}

ResourceLoader::~ResourceLoader()
{
    ClearResources();
}

void ResourceLoader::ClearResources()
{

    skills.clear();
    storage_skills.clear();

    items.clear();
    storage_items.clear();

    quests.clear();
    storage_quests.clear();

    npcs.clear();
    storage_npcs.clear();

    dialogs.clear();
    storage_dialogs.clear();

    game_modes.clear();
    storage_game_modes.clear();

    maps.clear();
    storage_maps.clear();

    levels.clear();
    storage_levels.clear();

    monsters.clear();
    storage_monsters.clear();


    item_item_options.clear();

    map_gates.clear();

    map_objects_movable_objects.clear();

    map_objects_static_objects.clear();

    map_spawn_points_boss_spawns.clear();

    map_spawn_points_monster_spawns.clear();

    map_spawn_points_player_spawns.clear();

}

// 중첩 오브젝트의 parent 를 연결하고 id 인덱스를 채운다.
// 저장소는 deque 라 원소 주소가 고정되므로 포인터를 그대로 보관해도 된다.
void ResourceLoader::BuildIndexes()
{

    item_item_options.clear();
    for (auto& root : storage_items)
    for (auto& lv0 : root.item_options)
    {
        lv0.parent = &root;
        long id = static_cast<long>(lv0.id);
        if (item_item_options.count(id)) {
            printf("[WARN] ItemItemOption duplicate id=%ld — id 는 파일 전체에서 유일해야 합니다.\n", id);
        }
        item_item_options[id] = &lv0;
    }

    map_gates.clear();
    for (auto& root : storage_maps)
    for (auto& lv0 : root.gates)
    {
        lv0.parent = &root;
        long id = static_cast<long>(lv0.id);
        if (map_gates.count(id)) {
            printf("[WARN] MapGate duplicate id=%ld — id 는 파일 전체에서 유일해야 합니다.\n", id);
        }
        map_gates[id] = &lv0;
    }

    map_objects_movable_objects.clear();
    for (auto& root : storage_maps)
    for (auto& lv0 : root.objects.movable_objects)
    {
        lv0.parent = &root;
        long id = static_cast<long>(lv0.id);
        if (map_objects_movable_objects.count(id)) {
            printf("[WARN] MapObjectsMovableObject duplicate id=%ld — id 는 파일 전체에서 유일해야 합니다.\n", id);
        }
        map_objects_movable_objects[id] = &lv0;
    }

    map_objects_static_objects.clear();
    for (auto& root : storage_maps)
    for (auto& lv0 : root.objects.static_objects)
    {
        lv0.parent = &root;
        long id = static_cast<long>(lv0.id);
        if (map_objects_static_objects.count(id)) {
            printf("[WARN] MapObjectsStaticObject duplicate id=%ld — id 는 파일 전체에서 유일해야 합니다.\n", id);
        }
        map_objects_static_objects[id] = &lv0;
    }

    map_spawn_points_boss_spawns.clear();
    for (auto& root : storage_maps)
    for (auto& lv0 : root.spawn_points.boss_spawn)
    {
        lv0.parent = &root;
        long id = static_cast<long>(lv0.id);
        if (map_spawn_points_boss_spawns.count(id)) {
            printf("[WARN] MapSpawnPointsBossSpawn duplicate id=%ld — id 는 파일 전체에서 유일해야 합니다.\n", id);
        }
        map_spawn_points_boss_spawns[id] = &lv0;
    }

    map_spawn_points_monster_spawns.clear();
    for (auto& root : storage_maps)
    for (auto& lv0 : root.spawn_points.monster_spawn)
    {
        lv0.parent = &root;
        long id = static_cast<long>(lv0.id);
        if (map_spawn_points_monster_spawns.count(id)) {
            printf("[WARN] MapSpawnPointsMonsterSpawn duplicate id=%ld — id 는 파일 전체에서 유일해야 합니다.\n", id);
        }
        map_spawn_points_monster_spawns[id] = &lv0;
    }

    map_spawn_points_player_spawns.clear();
    for (auto& root : storage_maps)
    for (auto& lv0 : root.spawn_points.player_spawn)
    {
        lv0.parent = &root;
        long id = static_cast<long>(lv0.id);
        if (map_spawn_points_player_spawns.count(id)) {
            printf("[WARN] MapSpawnPointsPlayerSpawn duplicate id=%ld — id 는 파일 전체에서 유일해야 합니다.\n", id);
        }
        map_spawn_points_player_spawns[id] = &lv0;
    }

}

bool ResourceLoader::LoadResources(const std::string& basePath)
{

    std::deque<gamedata::Skill> tmp_storage_skills;
    std::unordered_map<long, const gamedata::Skill*> tmp_skills;

    std::deque<gamedata::Item> tmp_storage_items;
    std::unordered_map<long, const gamedata::Item*> tmp_items;

    std::deque<gamedata::Quest> tmp_storage_quests;
    std::unordered_map<long, const gamedata::Quest*> tmp_quests;

    std::deque<gamedata::Npc> tmp_storage_npcs;
    std::unordered_map<long, const gamedata::Npc*> tmp_npcs;

    std::deque<gamedata::Dialog> tmp_storage_dialogs;
    std::unordered_map<long, const gamedata::Dialog*> tmp_dialogs;

    std::deque<gamedata::GameMode> tmp_storage_game_modes;
    std::unordered_map<long, const gamedata::GameMode*> tmp_game_modes;

    std::deque<gamedata::Map> tmp_storage_maps;
    std::unordered_map<long, const gamedata::Map*> tmp_maps;

    std::deque<gamedata::Level> tmp_storage_levels;
    std::unordered_map<long, const gamedata::Level*> tmp_levels;

    std::deque<gamedata::MonsterData> tmp_storage_monsters;
    std::unordered_map<long, const gamedata::MonsterData*> tmp_monsters;


    // Each table reads its own file into its own storage, so the loads share no
    // state and run concurrently — one task (thread) per table.
    std::vector<std::future<bool>> tasks;

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Skill>,
        basePath + "skill.json", std::ref(tmp_storage_skills), std::ref(tmp_skills)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Item>,
        basePath + "item.json", std::ref(tmp_storage_items), std::ref(tmp_items)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Quest>,
        basePath + "quest.json", std::ref(tmp_storage_quests), std::ref(tmp_quests)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Npc>,
        basePath + "npc.json", std::ref(tmp_storage_npcs), std::ref(tmp_npcs)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Dialog>,
        basePath + "dialog.json", std::ref(tmp_storage_dialogs), std::ref(tmp_dialogs)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::GameMode>,
        basePath + "GameMode.json", std::ref(tmp_storage_game_modes), std::ref(tmp_game_modes)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Map>,
        basePath + "Map.json", std::ref(tmp_storage_maps), std::ref(tmp_maps)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Level>,
        basePath + "level.json", std::ref(tmp_storage_levels), std::ref(tmp_levels)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::MonsterData>,
        basePath + "monster.json", std::ref(tmp_storage_monsters), std::ref(tmp_monsters)));


    // get() every future (so all threads join) before deciding success.
    bool ok = true;
    for (auto& task : tasks)
        ok = task.get() && ok;
    if (!ok)
        return false;

    // All files parsed successfully — swap in atomically.
    // (std::deque move preserves element addresses, so the maps stay valid.)

    storage_skills = std::move(tmp_storage_skills);
    skills = std::move(tmp_skills);

    storage_items = std::move(tmp_storage_items);
    items = std::move(tmp_items);

    storage_quests = std::move(tmp_storage_quests);
    quests = std::move(tmp_quests);

    storage_npcs = std::move(tmp_storage_npcs);
    npcs = std::move(tmp_npcs);

    storage_dialogs = std::move(tmp_storage_dialogs);
    dialogs = std::move(tmp_dialogs);

    storage_game_modes = std::move(tmp_storage_game_modes);
    game_modes = std::move(tmp_game_modes);

    storage_maps = std::move(tmp_storage_maps);
    maps = std::move(tmp_maps);

    storage_levels = std::move(tmp_storage_levels);
    levels = std::move(tmp_levels);

    storage_monsters = std::move(tmp_storage_monsters);
    monsters = std::move(tmp_monsters);


    // 중첩 오브젝트의 parent/인덱스는 저장소가 제자리에 놓인 뒤에 만든다.
    BuildIndexes();
    return true;
}