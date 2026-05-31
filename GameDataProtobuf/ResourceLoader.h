// This file is auto-generated. Do not modify directly.

#pragma once

#include <boost/noncopyable.hpp>
#include <unordered_map>
#include "gamedata.pb.h"

class ResourceLoader : private boost::noncopyable
{
private:

    std::unordered_map<long, gamedata::Skill*> skills;

    std::unordered_map<long, gamedata::Item*> items;

    std::unordered_map<long, gamedata::Quest*> quests;

    std::unordered_map<long, gamedata::GameMode*> game_modes;

    std::unordered_map<long, gamedata::Map*> maps;


public:
    static ResourceLoader& Instance() {
        static ResourceLoader instance;
        return instance;
    }

    bool LoadResources();


    const std::unordered_map<long, gamedata::Skill*>& GetSkills() const { return skills; }
    const gamedata::Skill* GetSkills(long id) const { auto itr = skills.find(id); return itr != skills.end() ? itr->second : nullptr; }

    const std::unordered_map<long, gamedata::Item*>& GetItems() const { return items; }
    const gamedata::Item* GetItems(long id) const { auto itr = items.find(id); return itr != items.end() ? itr->second : nullptr; }

    const std::unordered_map<long, gamedata::Quest*>& GetQuests() const { return quests; }
    const gamedata::Quest* GetQuests(long id) const { auto itr = quests.find(id); return itr != quests.end() ? itr->second : nullptr; }

    const std::unordered_map<long, gamedata::GameMode*>& GetGameModes() const { return game_modes; }
    const gamedata::GameMode* GetGameModes(long id) const { auto itr = game_modes.find(id); return itr != game_modes.end() ? itr->second : nullptr; }

    const std::unordered_map<long, gamedata::Map*>& GetMaps() const { return maps; }
    const gamedata::Map* GetMaps(long id) const { auto itr = maps.find(id); return itr != maps.end() ? itr->second : nullptr; }


private:
    ResourceLoader() = default;
    ~ResourceLoader();

    void ClearResources();
};