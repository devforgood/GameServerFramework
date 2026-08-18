// This file is auto-generated. Do not modify directly.
//
// table_meta.json 의 테이블 목록에서 만들어진다. 클라이언트 팩토리
// ({Table}Factory.cs) 는 여기 있는 {Table}s 사전을 참조하므로, 테이블을 추가하면
// 사전과 로딩 코드가 함께 생겨야 한다. 손으로 유지하던 시절에는 테이블을 늘릴 때마다
// 팩토리만 생기고 사전이 빠져 클라이언트 컴파일이 깨졌다.
//
// 손으로 쓰는 나머지 부분(ParseTable, 맵 인덱스, 조회 헬퍼)은 ResourceLoader.cs 에 있다.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.GameData
{
    public partial class ResourceLoader
    {
        public Dictionary<int, Gamedata.Skill> Skills = new Dictionary<int, Gamedata.Skill>();
        public Dictionary<int, Gamedata.Item> Items = new Dictionary<int, Gamedata.Item>();
        public Dictionary<int, Gamedata.Quest> Quests = new Dictionary<int, Gamedata.Quest>();
        public Dictionary<int, Gamedata.Npc> Npcs = new Dictionary<int, Gamedata.Npc>();
        public Dictionary<int, Gamedata.Dialog> Dialogs = new Dictionary<int, Gamedata.Dialog>();
        public Dictionary<int, Gamedata.GameMode> GameModes = new Dictionary<int, Gamedata.GameMode>();
        public Dictionary<int, Gamedata.Map> Maps = new Dictionary<int, Gamedata.Map>();
        public Dictionary<int, Gamedata.Level> Levels = new Dictionary<int, Gamedata.Level>();
        public Dictionary<int, Gamedata.MonsterData> MonsterDatas = new Dictionary<int, Gamedata.MonsterData>();

        /// <summary>
        /// 모든 테이블의 로드 요청을 먼저 띄워 디스크 대기를 겹친 뒤, 완료된 TextAsset 을
        /// 메인 스레드에서 파싱한다(JsonUtility 는 스레드 안전하지 않다).
        /// </summary>
        private IEnumerator LoadAllTables()
        {
            var skillReq = Resources.LoadAsync<TextAsset>("GameData/skill");
            var itemReq = Resources.LoadAsync<TextAsset>("GameData/item");
            var questReq = Resources.LoadAsync<TextAsset>("GameData/quest");
            var npcReq = Resources.LoadAsync<TextAsset>("GameData/npc");
            var dialogReq = Resources.LoadAsync<TextAsset>("GameData/dialog");
            var gameModeReq = Resources.LoadAsync<TextAsset>("GameData/GameMode");
            var mapReq = Resources.LoadAsync<TextAsset>("GameData/Map");
            var levelReq = Resources.LoadAsync<TextAsset>("GameData/level");
            var monsterDataReq = Resources.LoadAsync<TextAsset>("GameData/monster");
            yield return skillReq;
            yield return itemReq;
            yield return questReq;
            yield return npcReq;
            yield return dialogReq;
            yield return gameModeReq;
            yield return mapReq;
            yield return levelReq;
            yield return monsterDataReq;


            ParseTable<Gamedata.SkillList, Gamedata.Skill>(
                (TextAsset)skillReq.asset, "GameData/skill", l => l.items, e => e.id, Skills);
            ParseTable<Gamedata.ItemList, Gamedata.Item>(
                (TextAsset)itemReq.asset, "GameData/item", l => l.items, e => e.id, Items);
            ParseTable<Gamedata.QuestList, Gamedata.Quest>(
                (TextAsset)questReq.asset, "GameData/quest", l => l.items, e => e.id, Quests);
            ParseTable<Gamedata.NpcList, Gamedata.Npc>(
                (TextAsset)npcReq.asset, "GameData/npc", l => l.items, e => e.id, Npcs);
            ParseTable<Gamedata.DialogList, Gamedata.Dialog>(
                (TextAsset)dialogReq.asset, "GameData/dialog", l => l.items, e => e.id, Dialogs);
            ParseTable<Gamedata.GameModeList, Gamedata.GameMode>(
                (TextAsset)gameModeReq.asset, "GameData/GameMode", l => l.items, e => e.id, GameModes);
            ParseTable<Gamedata.MapList, Gamedata.Map>(
                (TextAsset)mapReq.asset, "GameData/Map", l => l.items, e => e.id, Maps);
            ParseTable<Gamedata.LevelList, Gamedata.Level>(
                (TextAsset)levelReq.asset, "GameData/level", l => l.items, e => e.id, Levels);
            ParseTable<Gamedata.MonsterDataList, Gamedata.MonsterData>(
                (TextAsset)monsterDataReq.asset, "GameData/monster", l => l.items, e => e.id, MonsterDatas);
        }
    }
}