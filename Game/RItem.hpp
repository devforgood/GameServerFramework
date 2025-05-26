#pragma once
#include <string>
#include <nlohmann/json.hpp>

struct RItem {
    
    int id;
    
    std::string type;
    
    double heal;
    
    std::string name_id;
    
    std::string desc_id;
    

    // nlohmann::json을 이용한 역직렬화
    static RItem* FromJSON(const nlohmann::json& obj) {
        RItem* c = new RItem();
        
        
        if (obj.contains("id") && obj["id"].is_number_integer())
            c->id = obj["id"].get<int>();
        
        
        
        if (obj.contains("type") && obj["type"].is_string())
            c->type = obj["type"].get<std::string>();
        
        
        
        if (obj.contains("heal") && obj["heal"].is_number())
            c->heal = obj["heal"].get<double>();
        
        
        
        if (obj.contains("name_id") && obj["name_id"].is_string())
            c->name_id = obj["name_id"].get<std::string>();
        
        
        
        if (obj.contains("desc_id") && obj["desc_id"].is_string())
            c->desc_id = obj["desc_id"].get<std::string>();
        
        
        return c;
    }
};