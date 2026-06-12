// This file is auto-generated. Do not modify directly.
#include "ResourceLoader.h"
#include <cstdio>
#include <deque>
#include <fstream>
#include <unordered_map>
#include <utility>
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



    if (!LoadJsonFile<gamedata::Skill>(basePath + "skill.json", tmp_storage_skills, tmp_skills))
        return false;

    if (!LoadJsonFile<gamedata::Item>(basePath + "item.json", tmp_storage_items, tmp_items))
        return false;

    if (!LoadJsonFile<gamedata::Quest>(basePath + "quest.json", tmp_storage_quests, tmp_quests))
        return false;

    if (!LoadJsonFile<gamedata::GameMode>(basePath + "GameMode.json", tmp_storage_game_modes, tmp_game_modes))
        return false;

    if (!LoadJsonFile<gamedata::Map>(basePath + "Map.json", tmp_storage_maps, tmp_maps))
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