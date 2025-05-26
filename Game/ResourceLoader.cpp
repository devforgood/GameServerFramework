#include "ResourceLoader.h"
#include <nlohmann/json.hpp>
#include <fstream>
#include <sstream>
#include "RItem.hpp"
#include "RSkill.hpp"

namespace {
    template<typename T>
    bool LoadJsonToMap(const std::string& filename, std::unordered_map<long, T*>& map, T* (*fromJsonFunc)(const nlohmann::json&)) {
        std::ifstream file(filename);
        if (!file.is_open()) {
            return false;
        }
        std::stringstream buffer;
        buffer << file.rdbuf();
        std::string jsonStr = buffer.str();
        file.close();

        nlohmann::json doc;
        try {
            doc = nlohmann::json::parse(jsonStr);
        }
        catch (const nlohmann::json::parse_error&) {
            return false;
        }

        if (!doc.is_array()) {
            return false;
        }

        for (const auto& v : doc) {
            T* obj = fromJsonFunc(v);
            map[obj->id] = obj;
        }
        return true;
    }
}


bool ResourceLoader::LoadResources()
{
    bool itemResult = LoadJsonToMap<RItem>("item.json", items, RItem::FromJSON);
    bool skillResult = LoadJsonToMap<RSkill>("skill.json", skills, RSkill::FromJSON);
    return itemResult && skillResult;
}