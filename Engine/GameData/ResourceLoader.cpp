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

    game_modes.clear();
    storage_game_modes.clear();

    maps.clear();
    storage_maps.clear();

}

bool ResourceLoader::LoadResources(const std::string& basePath)
{

    std::deque<gamedata::Skill> tmp_storage_skills;
    std::unordered_map<long, const gamedata::Skill*> tmp_skills;

    std::deque<gamedata::Item> tmp_storage_items;
    std::unordered_map<long, const gamedata::Item*> tmp_items;

    std::deque<gamedata::Quest> tmp_storage_quests;
    std::unordered_map<long, const gamedata::Quest*> tmp_quests;

    std::deque<gamedata::GameMode> tmp_storage_game_modes;
    std::unordered_map<long, const gamedata::GameMode*> tmp_game_modes;

    std::deque<gamedata::Map> tmp_storage_maps;
    std::unordered_map<long, const gamedata::Map*> tmp_maps;


    // Each table reads its own file into its own storage, so the loads share no
    // state and run concurrently — one task (thread) per table.
    std::vector<std::future<bool>> tasks;

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Skill>,
        basePath + "skill.json", std::ref(tmp_storage_skills), std::ref(tmp_skills)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Item>,
        basePath + "item.json", std::ref(tmp_storage_items), std::ref(tmp_items)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Quest>,
        basePath + "quest.json", std::ref(tmp_storage_quests), std::ref(tmp_quests)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::GameMode>,
        basePath + "GameMode.json", std::ref(tmp_storage_game_modes), std::ref(tmp_game_modes)));

    tasks.push_back(std::async(std::launch::async, &LoadJsonFile<gamedata::Map>,
        basePath + "Map.json", std::ref(tmp_storage_maps), std::ref(tmp_maps)));


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

    storage_game_modes = std::move(tmp_storage_game_modes);
    game_modes = std::move(tmp_game_modes);

    storage_maps = std::move(tmp_storage_maps);
    maps = std::move(tmp_maps);

    return true;
}