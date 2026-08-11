// This file is auto-generated. Do not modify directly.
#pragma once
#include <string>
#include <vector>
#include <nlohmann/json.hpp>

namespace gamedata
{
    // 전방 선언 — id 를 가진 중첩 오브젝트의 parent 포인터가 아직 정의되지 않은
    // 상위 구조체를 가리킨다(예: MapGate::parent -> const Map*).
    struct SkillEffect;
    struct Skill;
    struct ItemItemOption;
    struct Item;
    struct QuestPrerequisites;
    struct QuestRewardsChoiceItem;
    struct QuestRewardsItem;
    struct QuestRewards;
    struct QuestStageObjective;
    struct QuestStage;
    struct QuestTime;
    struct Quest;
    struct NpcPosition;
    struct Npc;
    struct GameModeBossInfo;
    struct GameModeRewards;
    struct GameModeRules;
    struct GameMode;
    struct MapGateLink;
    struct MapGatePosition;
    struct MapGate;
    struct MapObjectsMovableObjectPatrolPath;
    struct MapObjectsMovableObjectPosition;
    struct MapObjectsMovableObject;
    struct MapObjectsStaticObjectPosition;
    struct MapObjectsStaticObjectSize;
    struct MapObjectsStaticObject;
    struct MapObjects;
    struct MapSize;
    struct MapSpawnPointsBossSpawnPosition;
    struct MapSpawnPointsBossSpawn;
    struct MapSpawnPointsMonsterSpawnPosition;
    struct MapSpawnPointsMonsterSpawn;
    struct MapSpawnPointsPlayerSpawnPosition;
    struct MapSpawnPointsPlayerSpawn;
    struct MapSpawnPoints;
    struct Map;
    struct Level;
    struct MonsterData;


    struct SkillEffect
    {
        std::string phase;
        std::string type;
    };


    struct Skill
    {
        int angle = 0;
        std::string code_name;
        double cooldown = 0.0;
        std::string desc_id;
        int duration = 0;
        std::vector<SkillEffect> effects;
        std::string element;
        std::string fx;
        int heal = 0;
        int height = 0;
        int id = 0;
        int knockback = 0;
        int max_damage = 0;
        int min_damage = 0;
        bool monster_only = false;
        std::string name_id;
        double pulse_interval = 0.0;
        int radius = 0;
        int range = 0;
        std::string type;
    };


    struct ItemItemOption
    {
        int id = 0;
        double value = 0.0;

        // 이 오브젝트를 소유한 상위 데이터. JSON 필드가 아니라 ResourceLoader 가
        // 로드 직후 연결한다. 덕분에 id 하나로 소속 맵/아이템까지 따라갈 수 있다.
        const Item* parent = nullptr;
    };


    struct Item
    {
        std::string desc_id;
        int heal = 0;
        int id = 0;
        std::vector<ItemItemOption> item_options;
        std::string name_id;
        bool no_sell = false;
        bool no_trade = false;
        int quest_id = 0;
        std::string type;
    };


    struct QuestPrerequisites
    {
        std::vector<int> blocked_quest_ids;
        std::vector<int> completed_quest_ids;
        std::vector<int> item_ids;
        std::vector<int> skill_ids;
    };


    struct QuestRewardsChoiceItem
    {
        int count = 0;
        int item_id = 0;
    };


    struct QuestRewardsItem
    {
        int count = 0;
        int item_id = 0;
    };


    struct QuestRewards
    {
        std::vector<QuestRewardsChoiceItem> choice_items;
        int exp = 0;
        int gold = 0;
        std::vector<QuestRewardsItem> items;
        std::vector<int> skill_ids;
    };


    struct QuestStageObjective
    {
        int count = 0;
        std::string desc_id;
        int target_id = 0;
        std::string type;
    };


    struct QuestStage
    {
        std::string desc_id;
        std::string logic;
        std::vector<QuestStageObjective> objectives;
        int step = 0;
    };


    struct QuestTime
    {
        int cooldown_seconds = 0;
        int limit_seconds = 0;
        bool repeatable = false;
        std::string reset_type;
    };


    struct Quest
    {
        bool auto_complete = false;
        std::string category;
        int chain_id = 0;
        int chain_step = 0;
        std::string code_name;
        std::string desc_id;
        bool disabled = false;
        int end_npc_id = 0;
        int id = 0;
        int level = 0;
        int map_id = 0;
        int max_level = 0;
        int min_level = 0;
        std::string name_id;
        QuestPrerequisites prerequisites;
        int priority = 0;
        int recommended_party_size = 0;
        QuestRewards rewards;
        bool shareable = false;
        std::vector<QuestStage> stages;
        int start_npc_id = 0;
        QuestTime time;
    };


    struct NpcPosition
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
    };


    struct Npc
    {
        int id = 0;
        double interact_range = 0.0;
        int map_id = 0;
        std::string name;
        std::string name_id;
        NpcPosition position;
        std::string type;
    };


    struct GameModeBossInfo
    {
        int boss_hp = 0;
        int boss_id = 0;
        int boss_level = 0;
        std::string boss_name_id;
    };


    struct GameModeRewards
    {
        double drop_rate_multiplier = 0.0;
        double exp_multiplier = 0.0;
        double gold_multiplier = 0.0;
    };


    struct GameModeRules
    {
        bool allow_pvp = false;
        bool allow_trading = false;
        std::string end_condition;
        bool has_time_limit = false;
        int max_players = 0;
        int min_level = 0;
        bool respawn_enabled = false;
        int respawn_time = 0;
        int time_limit = 0;
    };


    struct GameMode
    {
        GameModeBossInfo boss_info;
        std::string category;
        std::string desc_id;
        int id = 0;
        std::vector<int> maps;
        std::string movement;
        std::string name;
        std::string name_id;
        GameModeRewards rewards;
        GameModeRules rules;
        std::string script;
        std::string type;
    };


    struct MapGateLink
    {
        double cost = 0.0;
        int from_id = 0;
        int to_id = 0;
    };


    struct MapGatePosition
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
    };


    struct MapGate
    {
        int id = 0;
        std::string name;
        MapGatePosition position;
        int required_level = 0;
        int target_id = 0;
        std::string type;

        // 이 오브젝트를 소유한 상위 데이터. JSON 필드가 아니라 ResourceLoader 가
        // 로드 직후 연결한다. 덕분에 id 하나로 소속 맵/아이템까지 따라갈 수 있다.
        const Map* parent = nullptr;
    };


    struct MapObjectsMovableObjectPatrolPath
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
    };


    struct MapObjectsMovableObjectPosition
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
    };


    struct MapObjectsMovableObject
    {
        int id = 0;
        double movement_range = 0.0;
        double movement_speed = 0.0;
        std::string name;
        std::vector<MapObjectsMovableObjectPatrolPath> patrol_path;
        MapObjectsMovableObjectPosition position;
        std::string type;

        // 이 오브젝트를 소유한 상위 데이터. JSON 필드가 아니라 ResourceLoader 가
        // 로드 직후 연결한다. 덕분에 id 하나로 소속 맵/아이템까지 따라갈 수 있다.
        const Map* parent = nullptr;
    };


    struct MapObjectsStaticObjectPosition
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
    };


    struct MapObjectsStaticObjectSize
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
    };


    struct MapObjectsStaticObject
    {
        bool collision = false;
        int damage = 0;
        int id = 0;
        int loot_table_id = 0;
        std::string name;
        MapObjectsStaticObjectPosition position;
        MapObjectsStaticObjectSize size;
        std::string type;

        // 이 오브젝트를 소유한 상위 데이터. JSON 필드가 아니라 ResourceLoader 가
        // 로드 직후 연결한다. 덕분에 id 하나로 소속 맵/아이템까지 따라갈 수 있다.
        const Map* parent = nullptr;
    };


    struct MapObjects
    {
        std::vector<MapObjectsMovableObject> movable_objects;
        std::vector<MapObjectsStaticObject> static_objects;
    };


    struct MapSize
    {
        double height = 0.0;
        double width = 0.0;
    };


    struct MapSpawnPointsBossSpawnPosition
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
    };


    struct MapSpawnPointsBossSpawn
    {
        int boss_id = 0;
        int id = 0;
        int monster_id = 0;
        MapSpawnPointsBossSpawnPosition position;
        int spawn_delay = 0;
        int spawn_interval = 0;

        // 이 오브젝트를 소유한 상위 데이터. JSON 필드가 아니라 ResourceLoader 가
        // 로드 직후 연결한다. 덕분에 id 하나로 소속 맵/아이템까지 따라갈 수 있다.
        const Map* parent = nullptr;
    };


    struct MapSpawnPointsMonsterSpawnPosition
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
    };


    struct MapSpawnPointsMonsterSpawn
    {
        int boss_id = 0;
        int id = 0;
        int monster_id = 0;
        MapSpawnPointsMonsterSpawnPosition position;
        int spawn_delay = 0;
        int spawn_interval = 0;

        // 이 오브젝트를 소유한 상위 데이터. JSON 필드가 아니라 ResourceLoader 가
        // 로드 직후 연결한다. 덕분에 id 하나로 소속 맵/아이템까지 따라갈 수 있다.
        const Map* parent = nullptr;
    };


    struct MapSpawnPointsPlayerSpawnPosition
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
    };


    struct MapSpawnPointsPlayerSpawn
    {
        int boss_id = 0;
        int id = 0;
        int monster_id = 0;
        MapSpawnPointsPlayerSpawnPosition position;
        int spawn_delay = 0;
        int spawn_interval = 0;

        // 이 오브젝트를 소유한 상위 데이터. JSON 필드가 아니라 ResourceLoader 가
        // 로드 직후 연결한다. 덕분에 id 하나로 소속 맵/아이템까지 따라갈 수 있다.
        const Map* parent = nullptr;
    };


    struct MapSpawnPoints
    {
        std::vector<MapSpawnPointsBossSpawn> boss_spawn;
        std::vector<MapSpawnPointsMonsterSpawn> monster_spawn;
        std::vector<MapSpawnPointsPlayerSpawn> player_spawn;
    };


    struct Map
    {
        int aoi_radius = 0;
        std::string desc_id;
        int game_mode_id = 0;
        std::vector<MapGateLink> gate_links;
        std::vector<MapGate> gates;
        int id = 0;
        std::string name;
        std::string name_id;
        std::string navmesh_path;
        MapObjects objects;
        std::string scene;
        MapSize size;
        MapSpawnPoints spawn_points;
    };


    struct Level
    {
        int id = 0;
        int level = 0;
        int required_exp = 0;
    };


    struct MonsterData
    {
        int attack = 0;
        int defense = 0;
        int exp = 0;
        int hp = 0;
        int id = 0;
        int level = 0;
        std::string name;
        std::string name_id;
    };



    inline void from_json(const nlohmann::json& j, SkillEffect& o)
    {
        if (j.contains("phase") && !j.at("phase").is_null()) j.at("phase").get_to(o.phase);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, Skill& o)
    {
        if (j.contains("angle") && !j.at("angle").is_null()) j.at("angle").get_to(o.angle);
        if (j.contains("code_name") && !j.at("code_name").is_null()) j.at("code_name").get_to(o.code_name);
        if (j.contains("cooldown") && !j.at("cooldown").is_null()) j.at("cooldown").get_to(o.cooldown);
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("duration") && !j.at("duration").is_null()) j.at("duration").get_to(o.duration);
        if (j.contains("effects") && !j.at("effects").is_null()) j.at("effects").get_to(o.effects);
        if (j.contains("element") && !j.at("element").is_null()) j.at("element").get_to(o.element);
        if (j.contains("fx") && !j.at("fx").is_null()) j.at("fx").get_to(o.fx);
        if (j.contains("heal") && !j.at("heal").is_null()) j.at("heal").get_to(o.heal);
        if (j.contains("height") && !j.at("height").is_null()) j.at("height").get_to(o.height);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("knockback") && !j.at("knockback").is_null()) j.at("knockback").get_to(o.knockback);
        if (j.contains("max_damage") && !j.at("max_damage").is_null()) j.at("max_damage").get_to(o.max_damage);
        if (j.contains("min_damage") && !j.at("min_damage").is_null()) j.at("min_damage").get_to(o.min_damage);
        if (j.contains("monster_only") && !j.at("monster_only").is_null()) j.at("monster_only").get_to(o.monster_only);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
        if (j.contains("pulse_interval") && !j.at("pulse_interval").is_null()) j.at("pulse_interval").get_to(o.pulse_interval);
        if (j.contains("radius") && !j.at("radius").is_null()) j.at("radius").get_to(o.radius);
        if (j.contains("range") && !j.at("range").is_null()) j.at("range").get_to(o.range);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, ItemItemOption& o)
    {
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("value") && !j.at("value").is_null()) j.at("value").get_to(o.value);
    }


    inline void from_json(const nlohmann::json& j, Item& o)
    {
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("heal") && !j.at("heal").is_null()) j.at("heal").get_to(o.heal);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("item_options") && !j.at("item_options").is_null()) j.at("item_options").get_to(o.item_options);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
        if (j.contains("no_sell") && !j.at("no_sell").is_null()) j.at("no_sell").get_to(o.no_sell);
        if (j.contains("no_trade") && !j.at("no_trade").is_null()) j.at("no_trade").get_to(o.no_trade);
        if (j.contains("quest_id") && !j.at("quest_id").is_null()) j.at("quest_id").get_to(o.quest_id);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, QuestPrerequisites& o)
    {
        if (j.contains("blocked_quest_ids") && !j.at("blocked_quest_ids").is_null()) j.at("blocked_quest_ids").get_to(o.blocked_quest_ids);
        if (j.contains("completed_quest_ids") && !j.at("completed_quest_ids").is_null()) j.at("completed_quest_ids").get_to(o.completed_quest_ids);
        if (j.contains("item_ids") && !j.at("item_ids").is_null()) j.at("item_ids").get_to(o.item_ids);
        if (j.contains("skill_ids") && !j.at("skill_ids").is_null()) j.at("skill_ids").get_to(o.skill_ids);
    }


    inline void from_json(const nlohmann::json& j, QuestRewardsChoiceItem& o)
    {
        if (j.contains("count") && !j.at("count").is_null()) j.at("count").get_to(o.count);
        if (j.contains("item_id") && !j.at("item_id").is_null()) j.at("item_id").get_to(o.item_id);
    }


    inline void from_json(const nlohmann::json& j, QuestRewardsItem& o)
    {
        if (j.contains("count") && !j.at("count").is_null()) j.at("count").get_to(o.count);
        if (j.contains("item_id") && !j.at("item_id").is_null()) j.at("item_id").get_to(o.item_id);
    }


    inline void from_json(const nlohmann::json& j, QuestRewards& o)
    {
        if (j.contains("choice_items") && !j.at("choice_items").is_null()) j.at("choice_items").get_to(o.choice_items);
        if (j.contains("exp") && !j.at("exp").is_null()) j.at("exp").get_to(o.exp);
        if (j.contains("gold") && !j.at("gold").is_null()) j.at("gold").get_to(o.gold);
        if (j.contains("items") && !j.at("items").is_null()) j.at("items").get_to(o.items);
        if (j.contains("skill_ids") && !j.at("skill_ids").is_null()) j.at("skill_ids").get_to(o.skill_ids);
    }


    inline void from_json(const nlohmann::json& j, QuestStageObjective& o)
    {
        if (j.contains("count") && !j.at("count").is_null()) j.at("count").get_to(o.count);
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("target_id") && !j.at("target_id").is_null()) j.at("target_id").get_to(o.target_id);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, QuestStage& o)
    {
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("logic") && !j.at("logic").is_null()) j.at("logic").get_to(o.logic);
        if (j.contains("objectives") && !j.at("objectives").is_null()) j.at("objectives").get_to(o.objectives);
        if (j.contains("step") && !j.at("step").is_null()) j.at("step").get_to(o.step);
    }


    inline void from_json(const nlohmann::json& j, QuestTime& o)
    {
        if (j.contains("cooldown_seconds") && !j.at("cooldown_seconds").is_null()) j.at("cooldown_seconds").get_to(o.cooldown_seconds);
        if (j.contains("limit_seconds") && !j.at("limit_seconds").is_null()) j.at("limit_seconds").get_to(o.limit_seconds);
        if (j.contains("repeatable") && !j.at("repeatable").is_null()) j.at("repeatable").get_to(o.repeatable);
        if (j.contains("reset_type") && !j.at("reset_type").is_null()) j.at("reset_type").get_to(o.reset_type);
    }


    inline void from_json(const nlohmann::json& j, Quest& o)
    {
        if (j.contains("auto_complete") && !j.at("auto_complete").is_null()) j.at("auto_complete").get_to(o.auto_complete);
        if (j.contains("category") && !j.at("category").is_null()) j.at("category").get_to(o.category);
        if (j.contains("chain_id") && !j.at("chain_id").is_null()) j.at("chain_id").get_to(o.chain_id);
        if (j.contains("chain_step") && !j.at("chain_step").is_null()) j.at("chain_step").get_to(o.chain_step);
        if (j.contains("code_name") && !j.at("code_name").is_null()) j.at("code_name").get_to(o.code_name);
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("disabled") && !j.at("disabled").is_null()) j.at("disabled").get_to(o.disabled);
        if (j.contains("end_npc_id") && !j.at("end_npc_id").is_null()) j.at("end_npc_id").get_to(o.end_npc_id);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("level") && !j.at("level").is_null()) j.at("level").get_to(o.level);
        if (j.contains("map_id") && !j.at("map_id").is_null()) j.at("map_id").get_to(o.map_id);
        if (j.contains("max_level") && !j.at("max_level").is_null()) j.at("max_level").get_to(o.max_level);
        if (j.contains("min_level") && !j.at("min_level").is_null()) j.at("min_level").get_to(o.min_level);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
        if (j.contains("prerequisites") && !j.at("prerequisites").is_null()) j.at("prerequisites").get_to(o.prerequisites);
        if (j.contains("priority") && !j.at("priority").is_null()) j.at("priority").get_to(o.priority);
        if (j.contains("recommended_party_size") && !j.at("recommended_party_size").is_null()) j.at("recommended_party_size").get_to(o.recommended_party_size);
        if (j.contains("rewards") && !j.at("rewards").is_null()) j.at("rewards").get_to(o.rewards);
        if (j.contains("shareable") && !j.at("shareable").is_null()) j.at("shareable").get_to(o.shareable);
        if (j.contains("stages") && !j.at("stages").is_null()) j.at("stages").get_to(o.stages);
        if (j.contains("start_npc_id") && !j.at("start_npc_id").is_null()) j.at("start_npc_id").get_to(o.start_npc_id);
        if (j.contains("time") && !j.at("time").is_null()) j.at("time").get_to(o.time);
    }


    inline void from_json(const nlohmann::json& j, NpcPosition& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, Npc& o)
    {
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("interact_range") && !j.at("interact_range").is_null()) j.at("interact_range").get_to(o.interact_range);
        if (j.contains("map_id") && !j.at("map_id").is_null()) j.at("map_id").get_to(o.map_id);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, GameModeBossInfo& o)
    {
        if (j.contains("boss_hp") && !j.at("boss_hp").is_null()) j.at("boss_hp").get_to(o.boss_hp);
        if (j.contains("boss_id") && !j.at("boss_id").is_null()) j.at("boss_id").get_to(o.boss_id);
        if (j.contains("boss_level") && !j.at("boss_level").is_null()) j.at("boss_level").get_to(o.boss_level);
        if (j.contains("boss_name_id") && !j.at("boss_name_id").is_null()) j.at("boss_name_id").get_to(o.boss_name_id);
    }


    inline void from_json(const nlohmann::json& j, GameModeRewards& o)
    {
        if (j.contains("drop_rate_multiplier") && !j.at("drop_rate_multiplier").is_null()) j.at("drop_rate_multiplier").get_to(o.drop_rate_multiplier);
        if (j.contains("exp_multiplier") && !j.at("exp_multiplier").is_null()) j.at("exp_multiplier").get_to(o.exp_multiplier);
        if (j.contains("gold_multiplier") && !j.at("gold_multiplier").is_null()) j.at("gold_multiplier").get_to(o.gold_multiplier);
    }


    inline void from_json(const nlohmann::json& j, GameModeRules& o)
    {
        if (j.contains("allow_pvp") && !j.at("allow_pvp").is_null()) j.at("allow_pvp").get_to(o.allow_pvp);
        if (j.contains("allow_trading") && !j.at("allow_trading").is_null()) j.at("allow_trading").get_to(o.allow_trading);
        if (j.contains("end_condition") && !j.at("end_condition").is_null()) j.at("end_condition").get_to(o.end_condition);
        if (j.contains("has_time_limit") && !j.at("has_time_limit").is_null()) j.at("has_time_limit").get_to(o.has_time_limit);
        if (j.contains("max_players") && !j.at("max_players").is_null()) j.at("max_players").get_to(o.max_players);
        if (j.contains("min_level") && !j.at("min_level").is_null()) j.at("min_level").get_to(o.min_level);
        if (j.contains("respawn_enabled") && !j.at("respawn_enabled").is_null()) j.at("respawn_enabled").get_to(o.respawn_enabled);
        if (j.contains("respawn_time") && !j.at("respawn_time").is_null()) j.at("respawn_time").get_to(o.respawn_time);
        if (j.contains("time_limit") && !j.at("time_limit").is_null()) j.at("time_limit").get_to(o.time_limit);
    }


    inline void from_json(const nlohmann::json& j, GameMode& o)
    {
        if (j.contains("boss_info") && !j.at("boss_info").is_null()) j.at("boss_info").get_to(o.boss_info);
        if (j.contains("category") && !j.at("category").is_null()) j.at("category").get_to(o.category);
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("maps") && !j.at("maps").is_null()) j.at("maps").get_to(o.maps);
        if (j.contains("movement") && !j.at("movement").is_null()) j.at("movement").get_to(o.movement);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
        if (j.contains("rewards") && !j.at("rewards").is_null()) j.at("rewards").get_to(o.rewards);
        if (j.contains("rules") && !j.at("rules").is_null()) j.at("rules").get_to(o.rules);
        if (j.contains("script") && !j.at("script").is_null()) j.at("script").get_to(o.script);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, MapGateLink& o)
    {
        if (j.contains("cost") && !j.at("cost").is_null()) j.at("cost").get_to(o.cost);
        if (j.contains("from_id") && !j.at("from_id").is_null()) j.at("from_id").get_to(o.from_id);
        if (j.contains("to_id") && !j.at("to_id").is_null()) j.at("to_id").get_to(o.to_id);
    }


    inline void from_json(const nlohmann::json& j, MapGatePosition& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapGate& o)
    {
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("required_level") && !j.at("required_level").is_null()) j.at("required_level").get_to(o.required_level);
        if (j.contains("target_id") && !j.at("target_id").is_null()) j.at("target_id").get_to(o.target_id);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsMovableObjectPatrolPath& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsMovableObjectPosition& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsMovableObject& o)
    {
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("movement_range") && !j.at("movement_range").is_null()) j.at("movement_range").get_to(o.movement_range);
        if (j.contains("movement_speed") && !j.at("movement_speed").is_null()) j.at("movement_speed").get_to(o.movement_speed);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("patrol_path") && !j.at("patrol_path").is_null()) j.at("patrol_path").get_to(o.patrol_path);
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsStaticObjectPosition& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsStaticObjectSize& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapObjectsStaticObject& o)
    {
        if (j.contains("collision") && !j.at("collision").is_null()) j.at("collision").get_to(o.collision);
        if (j.contains("damage") && !j.at("damage").is_null()) j.at("damage").get_to(o.damage);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("loot_table_id") && !j.at("loot_table_id").is_null()) j.at("loot_table_id").get_to(o.loot_table_id);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("size") && !j.at("size").is_null()) j.at("size").get_to(o.size);
        if (j.contains("type") && !j.at("type").is_null()) j.at("type").get_to(o.type);
    }


    inline void from_json(const nlohmann::json& j, MapObjects& o)
    {
        if (j.contains("movable_objects") && !j.at("movable_objects").is_null()) j.at("movable_objects").get_to(o.movable_objects);
        if (j.contains("static_objects") && !j.at("static_objects").is_null()) j.at("static_objects").get_to(o.static_objects);
    }


    inline void from_json(const nlohmann::json& j, MapSize& o)
    {
        if (j.contains("height") && !j.at("height").is_null()) j.at("height").get_to(o.height);
        if (j.contains("width") && !j.at("width").is_null()) j.at("width").get_to(o.width);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsBossSpawnPosition& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsBossSpawn& o)
    {
        if (j.contains("boss_id") && !j.at("boss_id").is_null()) j.at("boss_id").get_to(o.boss_id);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("monster_id") && !j.at("monster_id").is_null()) j.at("monster_id").get_to(o.monster_id);
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("spawn_delay") && !j.at("spawn_delay").is_null()) j.at("spawn_delay").get_to(o.spawn_delay);
        if (j.contains("spawn_interval") && !j.at("spawn_interval").is_null()) j.at("spawn_interval").get_to(o.spawn_interval);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsMonsterSpawnPosition& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsMonsterSpawn& o)
    {
        if (j.contains("boss_id") && !j.at("boss_id").is_null()) j.at("boss_id").get_to(o.boss_id);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("monster_id") && !j.at("monster_id").is_null()) j.at("monster_id").get_to(o.monster_id);
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("spawn_delay") && !j.at("spawn_delay").is_null()) j.at("spawn_delay").get_to(o.spawn_delay);
        if (j.contains("spawn_interval") && !j.at("spawn_interval").is_null()) j.at("spawn_interval").get_to(o.spawn_interval);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsPlayerSpawnPosition& o)
    {
        if (j.contains("x") && !j.at("x").is_null()) j.at("x").get_to(o.x);
        if (j.contains("y") && !j.at("y").is_null()) j.at("y").get_to(o.y);
        if (j.contains("z") && !j.at("z").is_null()) j.at("z").get_to(o.z);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPointsPlayerSpawn& o)
    {
        if (j.contains("boss_id") && !j.at("boss_id").is_null()) j.at("boss_id").get_to(o.boss_id);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("monster_id") && !j.at("monster_id").is_null()) j.at("monster_id").get_to(o.monster_id);
        if (j.contains("position") && !j.at("position").is_null()) j.at("position").get_to(o.position);
        if (j.contains("spawn_delay") && !j.at("spawn_delay").is_null()) j.at("spawn_delay").get_to(o.spawn_delay);
        if (j.contains("spawn_interval") && !j.at("spawn_interval").is_null()) j.at("spawn_interval").get_to(o.spawn_interval);
    }


    inline void from_json(const nlohmann::json& j, MapSpawnPoints& o)
    {
        if (j.contains("boss_spawn") && !j.at("boss_spawn").is_null()) j.at("boss_spawn").get_to(o.boss_spawn);
        if (j.contains("monster_spawn") && !j.at("monster_spawn").is_null()) j.at("monster_spawn").get_to(o.monster_spawn);
        if (j.contains("player_spawn") && !j.at("player_spawn").is_null()) j.at("player_spawn").get_to(o.player_spawn);
    }


    inline void from_json(const nlohmann::json& j, Map& o)
    {
        if (j.contains("aoi_radius") && !j.at("aoi_radius").is_null()) j.at("aoi_radius").get_to(o.aoi_radius);
        if (j.contains("desc_id") && !j.at("desc_id").is_null()) j.at("desc_id").get_to(o.desc_id);
        if (j.contains("game_mode_id") && !j.at("game_mode_id").is_null()) j.at("game_mode_id").get_to(o.game_mode_id);
        if (j.contains("gate_links") && !j.at("gate_links").is_null()) j.at("gate_links").get_to(o.gate_links);
        if (j.contains("gates") && !j.at("gates").is_null()) j.at("gates").get_to(o.gates);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
        if (j.contains("navmesh_path") && !j.at("navmesh_path").is_null()) j.at("navmesh_path").get_to(o.navmesh_path);
        if (j.contains("objects") && !j.at("objects").is_null()) j.at("objects").get_to(o.objects);
        if (j.contains("scene") && !j.at("scene").is_null()) j.at("scene").get_to(o.scene);
        if (j.contains("size") && !j.at("size").is_null()) j.at("size").get_to(o.size);
        if (j.contains("spawn_points") && !j.at("spawn_points").is_null()) j.at("spawn_points").get_to(o.spawn_points);
    }


    inline void from_json(const nlohmann::json& j, Level& o)
    {
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("level") && !j.at("level").is_null()) j.at("level").get_to(o.level);
        if (j.contains("required_exp") && !j.at("required_exp").is_null()) j.at("required_exp").get_to(o.required_exp);
    }


    inline void from_json(const nlohmann::json& j, MonsterData& o)
    {
        if (j.contains("attack") && !j.at("attack").is_null()) j.at("attack").get_to(o.attack);
        if (j.contains("defense") && !j.at("defense").is_null()) j.at("defense").get_to(o.defense);
        if (j.contains("exp") && !j.at("exp").is_null()) j.at("exp").get_to(o.exp);
        if (j.contains("hp") && !j.at("hp").is_null()) j.at("hp").get_to(o.hp);
        if (j.contains("id") && !j.at("id").is_null()) j.at("id").get_to(o.id);
        if (j.contains("level") && !j.at("level").is_null()) j.at("level").get_to(o.level);
        if (j.contains("name") && !j.at("name").is_null()) j.at("name").get_to(o.name);
        if (j.contains("name_id") && !j.at("name_id").is_null()) j.at("name_id").get_to(o.name_id);
    }


}