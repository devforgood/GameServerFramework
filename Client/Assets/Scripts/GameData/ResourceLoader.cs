using System;
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
        /// GameData JSON 파일을 직접 로드하여 Dictionary에 저장한다.
        /// JSON은 최상위 배열이므로 JsonUtility가 파싱할 수 있도록 { "items": [...] } 로 감싼다.
        /// </summary>
        /// <typeparam name="TList">items 리스트를 가진 래퍼 타입 (예: SkillList)</typeparam>
        /// <typeparam name="TElement">리스트 요소 타입 (예: Skill)</typeparam>
        private bool LoadTable<TList, TElement>(
            string resourcePath,
            Func<TList, List<TElement>> getList,
            Func<TElement, int> getId,
            Dictionary<int, TElement> outDict)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
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

        public void Load()
        {
            Debug.Log("ResourceLoader initialized.");

            LoadTable<Gamedata.SkillList, Gamedata.Skill>("GameData/skill", l => l.items, s => s.id, Skills);
            LoadTable<Gamedata.ItemList, Gamedata.Item>("GameData/item", l => l.items, i => i.id, Items);
            LoadTable<Gamedata.QuestList, Gamedata.Quest>("GameData/quest", l => l.items, q => q.id, GameManager.Instance.resource.Quests);

            // 게임 데이터 로드가 완료되었음을 로그로 남김
            Debug.Log("ResourceLoader: Game data loaded successfully.");
        }

        public Dictionary<int, Gamedata.Skill> Skills = new Dictionary<int, Gamedata.Skill>();
        public Dictionary<int, Gamedata.Item> Items = new Dictionary<int, Gamedata.Item>();
        public Dictionary<int, Gamedata.Quest> Quests = new Dictionary<int, Gamedata.Quest>();
    }
}
