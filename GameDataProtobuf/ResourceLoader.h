// This file is auto-generated. Do not modify directly.
#pragma once
#include <boost/noncopyable.hpp>
#include <memory>
#include <unordered_map>
#include <vector>
#include "gamedata.pb.h"
class ResourceLoader : private boost::noncopyable
{
private:

    std::unique_ptr<gamedata::SkillList> skills_storage;
    std::unordered_map<long, const gamedata::Skill*> skills;

    std::unique_ptr<gamedata::ItemList> items_storage;
    std::unordered_map<long, const gamedata::Item*> items;

    std::unique_ptr<gamedata::QuestList> quests_storage;
    std::unordered_map<long, const gamedata::Quest*> quests;

    std::unique_ptr<gamedata::GameModeList> game_modes_storage;
    std::unordered_map<long, const gamedata::GameMode*> game_modes;

    std::unique_ptr<gamedata::MapList> maps_storage;
    std::unordered_map<long, const gamedata::Map*> maps;

public:
    static ResourceLoader& Instance() {
        static ResourceLoader instance;
        return instance;
    }
    bool LoadResources(const std::string& basePath = "GameData/");

    const std::unordered_map<long, const gamedata::Skill*>& GetSkills() const { return skills; }
    const gamedata::Skill* GetSkill(long id) const { auto itr = skills.find(id); return itr != skills.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::Item*>& GetItems() const { return items; }
    const gamedata::Item* GetItem(long id) const { auto itr = items.find(id); return itr != items.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::Quest*>& GetQuests() const { return quests; }
    const gamedata::Quest* GetQuest(long id) const { auto itr = quests.find(id); return itr != quests.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::GameMode*>& GetGameModes() const { return game_modes; }
    const gamedata::GameMode* GetGameMode(long id) const { auto itr = game_modes.find(id); return itr != game_modes.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::Map*>& GetMaps() const { return maps; }
    const gamedata::Map* GetMap(long id) const { auto itr = maps.find(id); return itr != maps.end() ? itr->second : nullptr; }

private:
    ResourceLoader() = default;
    ~ResourceLoader();
    void ClearResources();
};