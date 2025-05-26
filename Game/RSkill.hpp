#pragma once
#include <string>
#include <nlohmann/json.hpp>

struct RSkill {
    
    int id;
    
    std::string type;
    
    std::string effect;
    
    double value;
    
    double cooldown;
    
    double damage;
    
    double mana_cost;
    
    std::string name_id;
    
    std::string desc_id;
    

    // nlohmann::json을 이용한 역직렬화
    static RSkill* FromJSON(const nlohmann::json& obj) {
        RSkill* c = new RSkill();
        
        
        if (obj.contains("id") && obj["id"].is_number_integer())
            c->id = obj["id"].get<int>();
        
        
        
        if (obj.contains("type") && obj["type"].is_string())
            c->type = obj["type"].get<std::string>();
        
        
        
        if (obj.contains("effect") && obj["effect"].is_string())
            c->effect = obj["effect"].get<std::string>();
        
        
        
        if (obj.contains("value") && obj["value"].is_number())
            c->value = obj["value"].get<double>();
        
        
        
        if (obj.contains("cooldown") && obj["cooldown"].is_number())
            c->cooldown = obj["cooldown"].get<double>();
        
        
        
        if (obj.contains("damage") && obj["damage"].is_number())
            c->damage = obj["damage"].get<double>();
        
        
        
        if (obj.contains("mana_cost") && obj["mana_cost"].is_number())
            c->mana_cost = obj["mana_cost"].get<double>();
        
        
        
        if (obj.contains("name_id") && obj["name_id"].is_string())
            c->name_id = obj["name_id"].get<std::string>();
        
        
        
        if (obj.contains("desc_id") && obj["desc_id"].is_string())
            c->desc_id = obj["desc_id"].get<std::string>();
        
        
        return c;
    }
};