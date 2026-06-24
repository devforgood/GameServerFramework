// This file is auto-generated. Do not modify directly.
using System;
using System.Collections.Generic;

namespace Gamedata
{

    [Serializable]
    public class Skill
    {
        public int angle;
        public string code_name;
        public string desc_id;
        public int duration;
        public string effect;
        public int hieght;
        public int id;
        public int max_damage;
        public int min_damage;
        public string name_id;
        public int range;
        public string type;
    }


    [Serializable]
    public class ItemItemOption
    {
        public int id;
        public double value;
    }


    [Serializable]
    public class Item
    {
        public string desc_id;
        public int heal;
        public int id;
        public System.Collections.Generic.List<ItemItemOption> item_options;
        public string name_id;
        public string type;
    }


    [Serializable]
    public class Quest
    {
        public string code_name;
        public string desc_id;
        public int id;
        public bool is_repeatable;
        public int level_requirement;
        public string name_id;
        public int objective_count;
        public int objective_target_id;
        public string objective_type;
        public int reward_exp;
        public int reward_gold;
        public System.Collections.Generic.List<int> reward_item_ids;
        public string type;
    }


    [Serializable]
    public class GameModeBossInfo
    {
        public int boss_hp;
        public int boss_id;
        public int boss_level;
        public string boss_name_id;
    }


    [Serializable]
    public class GameModeRewards
    {
        public double drop_rate_multiplier;
        public double exp_multiplier;
        public double gold_multiplier;
    }


    [Serializable]
    public class GameModeRules
    {
        public bool allow_pvp;
        public bool allow_trading;
        public string end_condition;
        public bool has_time_limit;
        public int max_players;
        public int min_level;
        public bool respawn_enabled;
        public int respawn_time;
        public int time_limit;
    }


    [Serializable]
    public class GameMode
    {
        public GameModeBossInfo boss_info;
        public string category;
        public string desc_id;
        public int id;
        public System.Collections.Generic.List<int> maps;
        public string movement;
        public string name;
        public string name_id;
        public GameModeRewards rewards;
        public GameModeRules rules;
        public string script;
        public string type;
    }


    [Serializable]
    public class MapGatePosition
    {
        public double x;
        public double y;
        public double z;
    }


    [Serializable]
    public class MapGate
    {
        public int id;
        public string name;
        public MapGatePosition position;
        public int required_level;
        public int target_gate_id;
        public int target_map_id;
    }


    [Serializable]
    public class MapObjectsMovableObjectPatrolPath
    {
        public int x;
        public int y;
        public int z;
    }


    [Serializable]
    public class MapObjectsMovableObjectPosition
    {
        public int x;
        public int y;
        public int z;
    }


    [Serializable]
    public class MapObjectsMovableObject
    {
        public int id;
        public int movement_range;
        public double movement_speed;
        public string name;
        public System.Collections.Generic.List<MapObjectsMovableObjectPatrolPath> patrol_path;
        public MapObjectsMovableObjectPosition position;
        public string type;
    }


    [Serializable]
    public class MapObjectsStaticObjectPosition
    {
        public int x;
        public int y;
        public int z;
    }


    [Serializable]
    public class MapObjectsStaticObjectSize
    {
        public int x;
        public int y;
        public int z;
    }


    [Serializable]
    public class MapObjectsStaticObject
    {
        public bool collision;
        public int damage;
        public int id;
        public int loot_table_id;
        public string name;
        public MapObjectsStaticObjectPosition position;
        public MapObjectsStaticObjectSize size;
        public string type;
    }


    [Serializable]
    public class MapObjects
    {
        public System.Collections.Generic.List<MapObjectsMovableObject> movable_objects;
        public System.Collections.Generic.List<MapObjectsStaticObject> static_objects;
    }


    [Serializable]
    public class MapSize
    {
        public double height;
        public double width;
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
        public int boss_id;
        public int monster_id;
        public MapSpawnPointsBossSpawnPosition position;
        public int spawn_delay;
        public int spawn_interval;
    }


    [Serializable]
    public class MapSpawnPointsMonsterSpawnPosition
    {
        public double x;
        public double y;
        public double z;
    }


    [Serializable]
    public class MapSpawnPointsMonsterSpawn
    {
        public int boss_id;
        public int monster_id;
        public MapSpawnPointsMonsterSpawnPosition position;
        public int spawn_delay;
        public int spawn_interval;
    }


    [Serializable]
    public class MapSpawnPointsPlayerSpawnPosition
    {
        public double x;
        public double y;
        public double z;
    }


    [Serializable]
    public class MapSpawnPointsPlayerSpawn
    {
        public int boss_id;
        public int monster_id;
        public MapSpawnPointsPlayerSpawnPosition position;
        public int spawn_delay;
        public int spawn_interval;
    }


    [Serializable]
    public class MapSpawnPoints
    {
        public System.Collections.Generic.List<MapSpawnPointsBossSpawn> boss_spawn;
        public System.Collections.Generic.List<MapSpawnPointsMonsterSpawn> monster_spawn;
        public System.Collections.Generic.List<MapSpawnPointsPlayerSpawn> player_spawn;
    }


    [Serializable]
    public class Map
    {
        public string desc_id;
        public int game_mode_id;
        public System.Collections.Generic.List<MapGate> gates;
        public int id;
        public string name;
        public string name_id;
        public string navmesh_path;
        public MapObjects objects;
        public MapSize size;
        public MapSpawnPoints spawn_points;
    }


    [Serializable]
    public class Level
    {
        public int id;
        public int level;
        public int required_exp;
    }


    [Serializable]
    public class MonsterData
    {
        public int attack;
        public int defense;
        public int exp;
        public int hp;
        public int id;
        public int level;
        public string name;
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


    // Wrapper so UnityEngine.JsonUtility can parse the top-level JSON array.
    [Serializable]
    public class LevelList
    {
        public List<Level> items;
    }


    // Wrapper so UnityEngine.JsonUtility can parse the top-level JSON array.
    [Serializable]
    public class MonsterDataList
    {
        public List<MonsterData> items;
    }


}