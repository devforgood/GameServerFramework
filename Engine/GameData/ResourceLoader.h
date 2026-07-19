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

    std::deque<gamedata::GameMode> storage_game_modes;
    std::unordered_map<long, const gamedata::GameMode*> game_modes;

    std::deque<gamedata::Map> storage_maps;
    std::unordered_map<long, const gamedata::Map*> maps;

    std::deque<gamedata::Level> storage_levels;
    std::unordered_map<long, const gamedata::Level*> levels;

    std::deque<gamedata::MonsterData> storage_monsters;
    std::unordered_map<long, const gamedata::MonsterData*> monsters;

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

    const std::unordered_map<long, const gamedata::GameMode*>& GetGameModes() const { return game_modes; }
    const gamedata::GameMode* GetGameMode(long id) const { auto itr = game_modes.find(id); return itr != game_modes.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::Map*>& GetMaps() const { return maps; }
    const gamedata::Map* GetMap(long id) const { auto itr = maps.find(id); return itr != maps.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::Level*>& GetLevels() const { return levels; }
    const gamedata::Level* GetLevel(long id) const { auto itr = levels.find(id); return itr != levels.end() ? itr->second : nullptr; }

    const std::unordered_map<long, const gamedata::MonsterData*>& GetMonsterDatas() const { return monsters; }
    const gamedata::MonsterData* GetMonsterData(long id) const { auto itr = monsters.find(id); return itr != monsters.end() ? itr->second : nullptr; }

private:
    ResourceLoader() = default;
    ~ResourceLoader();
    void ClearResources();
};