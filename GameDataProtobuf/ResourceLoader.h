#pragma once
#include <boost/noncopyable.hpp>
#include <unordered_map>
#include "gamedata.pb.h"


class ResourceLoader : private boost::noncopyable
{
private:
    // Protobuf 객체를 직접 저장
    std::unordered_map<long, gamedata::Item*> items;
    std::unordered_map<long, gamedata::Skill*> skills;

public:
    static ResourceLoader& Instance() {
        static ResourceLoader instance;
        return instance;
    }

    bool LoadResources();

    // 필요시 getter 추가
    const std::unordered_map<long, gamedata::Item*>& GetItems() const { return items; }
    const std::unordered_map<long, gamedata::Skill*>& GetSkills() const { return skills; }
    const gamedata::Skill* GetSkills(long id) const { auto itr = skills.find(id); return itr != skills.end() ? itr->second : nullptr; }
private:
    ResourceLoader() = default;
};