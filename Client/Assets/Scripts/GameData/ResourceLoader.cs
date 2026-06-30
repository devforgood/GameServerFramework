using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gamedata; // Gamedata.Skill, Gamedata.Item, Gamedata.Quest, Gamedata.SkillList, ...

namespace Assets.Scripts.GameData
{
    public class ResourceLoader
    {
        // 예시: 리소스 로드 메서드
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            return Resources.Load<T>(path);
        }

        /// <summary>
        /// 이미 로드된 TextAsset(GameData JSON 배열)을 파싱하여 Dictionary에 저장한다.
        /// JSON은 최상위 배열이므로 JsonUtility가 파싱할 수 있도록 { "items": [...] } 로 감싼다.
        /// JsonUtility는 스레드 안전하지 않으므로 파싱은 반드시 메인 스레드에서 수행한다.
        /// </summary>
        /// <typeparam name="TList">items 리스트를 가진 래퍼 타입 (예: SkillList)</typeparam>
        /// <typeparam name="TElement">리스트 요소 타입 (예: Skill)</typeparam>
        private bool ParseTable<TList, TElement>(
            TextAsset asset,
            string resourcePath,
            Func<TList, List<TElement>> getList,
            Func<TElement, int> getId,
            Dictionary<int, TElement> outDict)
        {
            if (asset == null)
            {
                Debug.LogError($"{typeof(TElement).Name} 데이터 파일을 찾을 수 없습니다: {resourcePath}");
                return false;
            }

            string wrapped = "{\"items\":" + asset.text + "}";
            TList listObj = JsonUtility.FromJson<TList>(wrapped);
            List<TElement> list = getList(listObj);
            if (list == null)
            {
                Debug.LogError($"{typeof(TElement).Name} 데이터 파싱에 실패했습니다: {resourcePath}");
                return false;
            }

            foreach (var elem in list)
            {
                outDict[getId(elem)] = elem;
            }
            return true;
        }

        /// <summary>
        /// 모든 GameData 파일의 로드를 동시에 시작하여(I/O 병렬) 디스크 대기 시간을 겹친 뒤,
        /// 완료된 TextAsset들을 메인 스레드에서 파싱한다.
        /// 호출 측에서 StartCoroutine(LoadAsync()) 형태로 실행한다.
        /// </summary>
        public IEnumerator LoadAsync()
        {
            Debug.Log("ResourceLoader initialized.");

            // 모든 로드 요청을 먼저 띄워 동시에 진행되도록 한다.
            var skillReq = Resources.LoadAsync<TextAsset>("GameData/skill");
            var itemReq = Resources.LoadAsync<TextAsset>("GameData/item");
            var questReq = Resources.LoadAsync<TextAsset>("GameData/quest");
            var levelReq = Resources.LoadAsync<TextAsset>("GameData/level");
            var monsterReq = Resources.LoadAsync<TextAsset>("GameData/monster");
            var mapReq = Resources.LoadAsync<TextAsset>("GameData/Map");

            // 모든 요청이 끝날 때까지 대기. (요청은 이미 동시에 진행 중)
            yield return skillReq;
            yield return itemReq;
            yield return questReq;
            yield return levelReq;
            yield return monsterReq;
            yield return mapReq;

            ParseTable<Gamedata.SkillList, Gamedata.Skill>((TextAsset)skillReq.asset, "GameData/skill", l => l.items, s => s.id, Skills);
            ParseTable<Gamedata.ItemList, Gamedata.Item>((TextAsset)itemReq.asset, "GameData/item", l => l.items, i => i.id, Items);
            ParseTable<Gamedata.QuestList, Gamedata.Quest>((TextAsset)questReq.asset, "GameData/quest", l => l.items, q => q.id, Quests);
            ParseTable<Gamedata.LevelList, Gamedata.Level>((TextAsset)levelReq.asset, "GameData/level", l => l.items, lv => lv.id, Levels);
            ParseTable<Gamedata.MonsterDataList, Gamedata.MonsterData>((TextAsset)monsterReq.asset, "GameData/monster", l => l.items, m => m.id, MonsterDatas);
            ParseTable<Gamedata.MapList, Gamedata.Map>((TextAsset)mapReq.asset, "GameData/Map", l => l.items, m => m.id, Maps);

            // 게임 데이터 로드가 완료되었음을 로그로 남김
            Debug.Log("ResourceLoader: Game data loaded successfully.");
        }

        public Dictionary<int, Gamedata.Skill> Skills = new Dictionary<int, Gamedata.Skill>();
        public Dictionary<int, Gamedata.Item> Items = new Dictionary<int, Gamedata.Item>();
        public Dictionary<int, Gamedata.Quest> Quests = new Dictionary<int, Gamedata.Quest>();
        public Dictionary<int, Gamedata.Level> Levels = new Dictionary<int, Gamedata.Level>();
        public Dictionary<int, Gamedata.MonsterData> MonsterDatas = new Dictionary<int, Gamedata.MonsterData>();
        public Dictionary<int, Gamedata.Map> Maps = new Dictionary<int, Gamedata.Map>();

        /// <summary>맵 이름으로 맵 데이터를 찾는다(게이트의 destinationMapName 해석용). 없으면 null.</summary>
        public Gamedata.Map GetMapByName(string name)
        {
            foreach (var map in Maps.Values)
            {
                if (string.Equals(map.name, name, StringComparison.OrdinalIgnoreCase))
                    return map;
            }
            return null;
        }
    }
}
