// This file is auto-generated. Do not modify directly.
using System;
using System.Collections.Generic;

namespace Gamedata
{

    [Serializable]
    public class Skill
    {
        public string code_name;
        public string effect;
        public int hieght;
        public int duration;
        public int max_damage;
        public int range;
        public int min_damage;
        public int id;
        public string desc_id;
        public string name_id;
        public string type;
        public int angle;
    }


    [Serializable]
    public class ItemItemOption
    {
        public double value;
        public int id;
    }


    [Serializable]
    public class Item
    {
        public int heal;
        public int id;
        public string desc_id;
        public string name_id;
        public string type;
        public System.Collections.Generic.List<ItemItemOption> item_options;
    }


    [Serializable]
    public class Quest
    {
        public string code_name;
        public bool is_repeatable;
        public string objective_type;
        public int reward_exp;
        public int level_requirement;
        public int id;
        public string desc_id;
        public int reward_gold;
        public int objective_count;
        public System.Collections.Generic.List<int> reward_item_ids;
        public int objective_target_id;
        public string name_id;
        public string type;
    }


    [Serializable]
    public class GameModeBossInfo
    {
        public int boss_id;
        public string boss_name_id;
        public int boss_level;
        public int boss_hp;
    }


    [Serializable]
    public class GameModeRewards
    {
        public double exp_multiplier;
        public double drop_rate_multiplier;
        public double gold_multiplier;
    }


    [Serializable]
    public class GameModeRules
    {
        public bool has_time_limit;
        public bool allow_pvp;
        public bool respawn_enabled;
        public string end_condition;
        public int respawn_time;
        public int time_limit;
        public bool allow_trading;
        public int min_level;
        public int max_players;
    }


    [Serializable]
    public class GameMode
    {
        public string category;
        public GameModeBossInfo boss_info;
        public int id;
        public string desc_id;
        public string name_id;
        public System.Collections.Generic.List<int> maps;
        public GameModeRewards rewards;
        public string name;
        public GameModeRules rules;
        public string type;
    }


    [Serializable]
    public class MapGatePosition
    {
        public double y;
        public double x;
        public double z;
    }


    [Serializable]
    public class MapGate
    {
        public int required_level;
        public MapGatePosition position;
        public int target_gate_id;
        public string name;
        public int target_map_id;
        public int id;
    }


    [Serializable]
    public class MapSpawnPointsMonsterSpawnPosition
    {
        public double y;
        public double x;
        public double z;
    }


    [Serializable]
    public class MapSpawnPointsMonsterSpawn
    {
        public int spawn_interval;
        public MapSpawnPointsMonsterSpawnPosition position;
        public int boss_id;
        public int monster_id;
        public int spawn_delay;
    }


    [Serializable]
    public class MapSpawnPointsPlayerSpawnPosition
    {
        public double y;
        public double x;
        public double z;
    }


    [Serializable]
    public class MapSpawnPointsPlayerSpawn
    {
        public int spawn_interval;
        public MapSpawnPointsPlayerSpawnPosition position;
        public int boss_id;
        public int monster_id;
        public int spawn_delay;
    }


    [Serializable]
    public class MapSpawnPointsBossSpawnPosition
    {
        public double x;
        public double y;
        public double z;
    }


    [Serializable]
    public class MapSpawnPointsBossSpawn
    {
        public MapSpawnPointsBossSpawnPosition position;
        public int monster_id;
        public int spawn_interval;
        public int boss_id;
        public int spawn_delay;
    }


    [Serializable]
    public class MapSpawnPoints
    {
        public System.Collections.Generic.List<MapSpawnPointsMonsterSpawn> monster_spawn;
        public System.Collections.Generic.List<MapSpawnPointsPlayerSpawn> player_spawn;
        public System.Collections.Generic.List<MapSpawnPointsBossSpawn> boss_spawn;
    }


    [Serializable]
    public class MapSize
    {
        public double width;
        public double height;
    }


    [Serializable]
    public class MapObjectsStaticObjectPosition
    {
        public int y;
        public int x;
        public int z;
    }


    [Serializable]
    public class MapObjectsStaticObjectSize
    {
        public int y;
        public int x;
        public int z;
    }


    [Serializable]
    public class MapObjectsStaticObject
    {
        public MapObjectsStaticObjectPosition position;
        public bool collision;
        public string name;
        public MapObjectsStaticObjectSize size;
        public int loot_table_id;
        public int id;
        public int damage;
        public string type;
    }


    [Serializable]
    public class MapObjectsMovableObjectPosition
    {
        public int x;
        public int y;
        public int z;
    }


    [Serializable]
    public class MapObjectsMovableObjectPatrolPath
    {
        public int y;
        public int x;
        public int z;
    }


    [Serializable]
    public class MapObjectsMovableObject
    {
        public MapObjectsMovableObjectPosition position;
        public System.Collections.Generic.List<MapObjectsMovableObjectPatrolPath> patrol_path;
        public int movement_range;
        public string name;
        public int id;
        public string type;
        public double movement_speed;
    }


    [Serializable]
    public class MapObjects
    {
        public System.Collections.Generic.List<MapObjectsStaticObject> static_objects;
        public System.Collections.Generic.List<MapObjectsMovableObject> movable_objects;
    }


    [Serializable]
    public class Map
    {
        public int id;
        public string desc_id;
        public System.Collections.Generic.List<MapGate> gates;
        public string navmesh_path;
        public MapSpawnPoints spawn_points;
        public int game_mode_id;
        public string name;
        public MapSize size;
        public MapObjects objects;
        public string name_id;
    }



    // Wrapper so UnityEngine.JsonUtility can parse the top-level JSON array.
    [Serializable]
    public class SkillList
    {
        public List<Skill> items;
    }


    // Wrapper so UnityEngine.JsonUtility can parse the top-level JSON array.
    [Serializable]
    public class ItemList
    {
        public List<Item> items;
    }


    // Wrapper so UnityEngine.JsonUtility can parse the top-level JSON array.
    [Serializable]
    public class QuestList
    {
        public List<Quest> items;
    }


    // Wrapper so UnityEngine.JsonUtility can parse the top-level JSON array.
    [Serializable]
    public class GameModeList
    {
        public List<GameMode> items;
    }


    // Wrapper so UnityEngine.JsonUtility can parse the top-level JSON array.
    [Serializable]
    public class MapList
    {
        public List<Map> items;
    }


}