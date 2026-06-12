// This file is auto-generated. Do not modify directly.
#pragma once
#include <string>
#include <vector>
#include <nlohmann/json.hpp>

namespace gamedata
{

    struct Skill
    {
        std::string code_name;
        std::string effect;
        int hieght = 0;
        int duration = 0;
        int max_damage = 0;
        int range = 0;
        int min_damage = 0;
        int id = 0;
        std::string desc_id;
        std::string name_id;
        std::string type;
        int angle = 0;
    };


    struct ItemItemOption
    {
        double value = 0.0;
        int id = 0;
    };


    struct Item
    {
        int heal = 0;
        int id = 0;
        std::string desc_id;
        std::string name_id;
        std::string type;
        std::vector<ItemItemOption> item_options;
    };


    struct Quest
    {
        std::string code_name;
        bool is_repeatable = false;
        std::string objective_type;
        int reward_exp = 0;
        int level_requirement = 0;
        int id = 0;
        std::string desc_id;
        int reward_gold = 0;
        int objective_count = 0;
        std::vector<int> reward_item_ids;
        int objective_target_id = 0;
        std::string name_id;
        std::string type;
    };


    struct GameModeBossInfo
    {
        int boss_id = 0;
        std::string boss_name_id;
        int boss_level = 0;
        int boss_hp = 0;
    };


    struct GameModeRewards
    {
        double exp_multiplier = 0.0;
        double drop_rate_multiplier = 0.0;
        double gold_multiplier = 0.0;
    };


    struct GameModeRules
    {
        bool has_time_limit = false;
        bool allow_pvp = false;
        bool respawn_enabled = false;
        std::string end_condition;
        int respawn_time = 0;
        int time_limit = 0;
        bool allow_trading = false;
        int min_level = 0;
        int max_players = 0;
    };


    struct GameMode
    {
        std::string category;
        GameModeBossInfo boss_info;
        int id = 0;
        std::string desc_id;
        std::string name_id;
        std::vector<int> maps;
        GameModeRewards rewards;
        std::string name;
        GameModeRules rules;
        std::string type;
    };


    struct MapGatePosition
    {
        double y = 0.0;
        double x = 0.0;
        double z = 0.0;
    };


    struct MapGate
    {
        int required_level = 0;
        MapGatePosition position;
        int target_gate_id = 0;
        std::string name;
        int target_map_id = 0;
        int id = 0;
    };


    struct MapSpawnPointsMonsterSpawnPosition
    {
        double y = 0.0;
        double x = 0.0;
        double z = 0.0;
    };


    struct MapSpawnPointsMonsterSpawn
    {
        int spawn_interval = 0;
        MapSpawnPointsMonsterSpawnPosition position;
        int boss_id = 0;
        int monster_id = 0;
        int spawn_delay = 0;
    };


    struct MapSpawnPointsPlayerSpawnPosition
    {
        double y = 0.0;
        double x = 0.0;
        double z = 0.0;
    };


    struct MapSpawnPointsPlayerSpawn
    {
        int spawn_interval = 0;
        MapSpawnPointsPlayerSpawnPosition position;
        int boss_id = 0;
        int monster_id = 0;
        int spawn_delay = 0;
    };


    struct MapSpawnPointsBossSpawnPosition
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
    };


    struct MapSpawnPointsBossSpawn
    {
        MapSpawnPointsBossSpawnPosition position;
        int monster_id = 0;
        int spawn_interval = 0;
        int boss_id = 0;
        int spawn_delay = 0;
    };


    struct MapSpawnPoints
    {
        std::vector<MapSpawnPointsMonsterSpawn> monster_spawn;
        std::vector<MapSpawnPointsPlayerSpawn> player_spawn;
        std::vector<MapSpawnPointsBossSpawn> boss_spawn;
    };


    struct MapSize
    {
        double width = 0.0;
        double height = 0.0;
    };


    struct MapObjectsStaticObjectPosition
    {
        int y = 0;
        int x = 0;
        int z = 0;
    };


    struct MapObjectsStaticObjectSize
    {
        int y = 0;
        int x = 0;
        int z = 0;
    };


    struct MapObjectsStaticObject
    {
        MapObjectsStaticObjectPosition position;
        bool collision = false;
        std::string name;
        MapObjectsStaticObjectSize size;
        int loot_table_id = 0;
        int id = 0;
        int damage = 0;
        std::string type;
    };


    struct MapObjectsMovableObjectPosition
    {
        int x = 0;
        int y = 0;
        int z = 0;
    };


    struct MapObjectsMovableObjectPatrolPath
    {
        int y = 0;
        int x = 0;
        int z = 0;
    };


    struct MapObjectsMovableObject
    {
        MapObjectsMovableObjectPosition position;
        std::vector<MapObjectsMovableObjectPatrolPath> patrol_path;
        int movement_range = 0;
        std::string name;
        int id = 0;
        std::string type;
        double movement_speed = 0.0;
    };


    struct MapObjects
    {
        std::vector<MapObjectsStaticObject> static_objects;
        std::vector<MapObjectsMovableObject> movable_objects;
    };


    struct Map
    {
        int id = 0;
        std::string desc_id;
        std::vector<MapGate> gates;
        std::string navmesh_path;
        MapSpawnPoints spawn_points;
        int game_mode_id = 0;
        std::string name;
        MapSize size;
        MapObjects objects;
        std::string name_id;
    };



    inline void from_json(const nlohmann::json& j, Skill& o)
    {
        if (j.contains("code_name") && !j.at("code_name").is_null()) j.at("code_name").get_to(o.code_name);
        if (j.contains("effect") && !j.at("effect").is_null()) j.at("effect").get_to(o.effect);
        if (j.contains("hieght") && !j.at("hieght").is_null()) j.at("hieght").get_to(o.hieght);
        if (j.contains("duration") && !j.at("duration").is_null()) j.at("duration").get_to(o.duration);
        if (j.contains("max_damage") && !j.at("max_damage").is_null()) j.at("max_damage").get_to(o.max_damage);
        if (j.contains("range") && !j.at("range").is_null()) j.at("range").get_to(o.range);
        if (j.contains("min_damage") && !j.at("min_damage").is_null()) j.at("min_damage").get_to(o.min_damage);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
        if (j.contains("angle") && !j.at("angle").is_null()) j.at("angle").get_to(o.angle);
    }


    inline void from_json(const nlohmann::json& j, ItemItemOption& o)
    {
        if (j.contains("value") && !j.at("value").is_null()) j.at("value").get_to(o.value);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
    }


    inline void from_json(const nlohmann::json& j, Item& o)
    {
        if (j.contains("heal") && !j.at("heal").is_null()) j.at("heal").get_to(o.heal);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
        if (j.contains("item_options") && !j.at("item_options").is_null()) j.at("item_options").get_to(o.item_options);
    }


    inline void from_json(const nlohmann::json& j, Quest& o)
    {
        if (j.contains("code_name") && !j.at("code_name").is_null()) j.at("code_name").get_to(o.code_name);
        if (j.contains("is_repeatable") && !j.at("is_repeatable").is_null()) j.at("is_repeatable").get_to(o.is_repeatable);
        if (j.contains("objective_type") && !j.at("objective_type").is_null()) j.at("objective_type").get_to(o.objective_type);
        if (j.contains("reward_exp") && !j.at("reward_exp").is_null()) j.at("reward_exp").get_to(o.reward_exp);
        if (j.contains("level_requirement") && !j.at("level_requirement").is_null()) j.at("level_requirement").get_to(o.level_requirement);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("reward_gold") && !j.at("reward_gold").is_null()) j.at("reward_gold").get_to(o.reward_gold);
        if (j.contains("objective_count") && !j.at("objective_count").is_null()) j.at("objective_count").get_to(o.objective_count);
        if (j.contains("reward_item_ids") && !j.at("reward_item_ids").is_null()) j.at("reward_item_ids").get_to(o.reward_item_ids);
        if (j.contains("objective_target_id") && !j.at("objective_target_id").is_null()) j.at("objective_target_id").get_to(o.objective_target_id);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, GameModeBossInfo& o)
    {
        if (j.contains("boss_id") && !j.at("boss_id").is_null()) j.at("boss_id").get_to(o.boss_id);
        if (j.contains("boss_name_id") && !j.at("boss_name_id").is_null()) j.at("boss_name_id").get_to(o.boss_name_id);
        if (j.contains("boss_level") && !j.at("boss_level").is_null()) j.at("boss_level").get_to(o.boss_level);
        if (j.contains("boss_hp") && !j.at("boss_hp").is_null()) j.at("boss_hp").get_to(o.boss_hp);
    }


    inline void from_json(const nlohmann::json& j, GameModeRewards& o)
    {
        if (j.contains("exp_multiplier") && !j.at("exp_multiplier").is_null()) j.at("exp_multiplier").get_to(o.exp_multiplier);
        if (j.contains("drop_rate_multiplier") && !j.at("drop_rate_multiplier").is_null()) j.at("drop_rate_multiplier").get_to(o.drop_rate_multiplier);
        if (j.contains("gold_multiplier") && !j.at("gold_multiplier").is_null()) j.at("gold_multiplier").get_to(o.gold_multiplier);
    }


    inline void from_json(const nlohmann::json& j, GameModeRules& o)
    {
        if (j.contains("has_time_limit") && !j.at("has_time_limit").is_null()) j.at("has_time_limit").get_to(o.has_time_limit);
        if (j.contains("allow_pvp") && !j.at("allow_pvp").is_null()) j.at("allow_pvp").get_to(o.allow_pvp);
        if (j.contains("respawn_enabled") && !j.at("respawn_enabled").is_null()) j.at("respawn_enabled").get_to(o.respawn_enabled);
        if (j.contains("end_condition") && !j.at("end_condition").is_null()) j.at("end_condition").get_to(o.end_condition);
        if (j.contains("respawn_time") && !j.at("respawn_time").is_null()) j.at("respawn_time").get_to(o.respawn_time);
        if (j.contains("time_limit") && !j.at("time_limit").is_null()) j.at("time_limit").get_to(o.time_limit);
        if (j.contains("allow_trading") && !j.at("allow_trading").is_null()) j.at("allow_trading").get_to(o.allow_trading);
        if (j.contains("min_level") && !j.at("min_level").is_null()) j.at("min_level").get_to(o.min_level);
        if (j.contains("max_players") && !j.at("max_players").is_null()) j.at("max_players").get_to(o.max_players);
    }


    inline void from_json(const nlohmann::json& j, GameMode& o)
    {
        if (j.contains("category") && !j.at("category").is_null()) j.at("category").get_to(o.category);
        if (j.contains("boss_info") && !j.at("boss_info").is_null()) j.at("boss_info").get_to(o.boss_info);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
        if (j.contains("maps") && !j.at("maps").is_null()) j.at("maps").get_to(o.maps);
        if (j.contains("rewards") && !j.at("rewards").is_null()) j.at("rewards").get_to(o.rewards);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("rules") && !j.at("rules").is_null()) j.at("rules").get_to(o.rules);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, MapGatePosition& o)
    {
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapGate& o)
    {
        if (j.contains("required_level") && !j.at("required_level").is_null()) j.at("required_level").get_to(o.required_level);
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("target_gate_id") && !j.at("target_gate_id").is_null()) j.at("target_gate_id").get_to(o.target_gate_id);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("target_map_id") && !j.at("target_map_id").is_null()) j.at("target_map_id").get_to(o.target_map_id);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsMonsterSpawnPosition& o)
    {
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsMonsterSpawn& o)
    {
        if (j.contains("spawn_interval") && !j.at("spawn_interval").is_null()) j.at("spawn_interval").get_to(o.spawn_interval);
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("boss_id") && !j.at("boss_id").is_null()) j.at("boss_id").get_to(o.boss_id);
        if (j.contains("monster_id") && !j.at("monster_id").is_null()) j.at("monster_id").get_to(o.monster_id);
        if (j.contains("spawn_delay") && !j.at("spawn_delay").is_null()) j.at("spawn_delay").get_to(o.spawn_delay);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsPlayerSpawnPosition& o)
    {
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsPlayerSpawn& o)
    {
        if (j.contains("spawn_interval") && !j.at("spawn_interval").is_null()) j.at("spawn_interval").get_to(o.spawn_interval);
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("boss_id") && !j.at("boss_id").is_null()) j.at("boss_id").get_to(o.boss_id);
        if (j.contains("monster_id") && !j.at("monster_id").is_null()) j.at("monster_id").get_to(o.monster_id);
        if (j.contains("spawn_delay") && !j.at("spawn_delay").is_null()) j.at("spawn_delay").get_to(o.spawn_delay);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsBossSpawnPosition& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsBossSpawn& o)
    {
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("monster_id") && !j.at("monster_id").is_null()) j.at("monster_id").get_to(o.monster_id);
        if (j.contains("spawn_interval") && !j.at("spawn_interval").is_null()) j.at("spawn_interval").get_to(o.spawn_interval);
        if (j.contains("boss_id") && !j.at("boss_id").is_null()) j.at("boss_id").get_to(o.boss_id);
        if (j.contains("spawn_delay") && !j.at("spawn_delay").is_null()) j.at("spawn_delay").get_to(o.spawn_delay);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPoints& o)
    {
        if (j.contains("monster_spawn") && !j.at("monster_spawn").is_null()) j.at("monster_spawn").get_to(o.monster_spawn);
        if (j.contains("player_spawn") && !j.at("player_spawn").is_null()) j.at("player_spawn").get_to(o.player_spawn);
        if (j.contains("boss_spawn") && !j.at("boss_spawn").is_null()) j.at("boss_spawn").get_to(o.boss_spawn);
    }


    inline void from_json(const nlohmann::json& j, MapSize& o)
    {
        if (j.contains("width") && !j.at("width").is_null()) j.at("width").get_to(o.width);
        if (j.contains("height") && !j.at("height").is_null()) j.at("height").get_to(o.height);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsStaticObjectPosition& o)
    {
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsStaticObjectSize& o)
    {
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsStaticObject& o)
    {
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("collision") && !j.at("collision").is_null()) j.at("collision").get_to(o.collision);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("size") && !j.at("size").is_null()) j.at("size").get_to(o.size);
        if (j.contains("loot_table_id") && !j.at("loot_table_id").is_null()) j.at("loot_table_id").get_to(o.loot_table_id);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("damage") && !j.at("damage").is_null()) j.at("damage").get_to(o.damage);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsMovableObjectPosition& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsMovableObjectPatrolPath& o)
    {
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsMovableObject& o)
    {
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("patrol_path") && !j.at("patrol_path").is_null()) j.at("patrol_path").get_to(o.patrol_path);
        if (j.contains("movement_range") && !j.at("movement_range").is_null()) j.at("movement_range").get_to(o.movement_range);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
        if (j.contains("movement_speed") && !j.at("movement_speed").is_null()) j.at("movement_speed").get_to(o.movement_speed);
    }


    inline void from_json(const nlohmann::json& j, MapObjects& o)
    {
        if (j.contains("static_objects") && !j.at("static_objects").is_null()) j.at("static_objects").get_to(o.static_objects);
        if (j.contains("movable_objects") && !j.at("movable_objects").is_null()) j.at("movable_objects").get_to(o.movable_objects);
    }


    inline void from_json(const nlohmann::json& j, Map& o)
    {
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("gates") && !j.at("gates").is_null()) j.at("gates").get_to(o.gates);
        if (j.contains("navmesh_path") && !j.at("navmesh_path").is_null()) j.at("navmesh_path").get_to(o.navmesh_path);
        if (j.contains("spawn_points") && !j.at("spawn_points").is_null()) j.at("spawn_points").get_to(o.spawn_points);
        if (j.contains("game_mode_id") && !j.at("game_mode_id").is_null()) j.at("game_mode_id").get_to(o.game_mode_id);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("size") && !j.at("size").is_null()) j.at("size").get_to(o.size);
        if (j.contains("objects") && !j.at("objects").is_null()) j.at("objects").get_to(o.objects);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
    }


}