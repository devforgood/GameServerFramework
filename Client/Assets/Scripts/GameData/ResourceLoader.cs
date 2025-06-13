using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Gamedata; // Gamedata.Skill, Gamedata.Item, Gamedata.SkillList, Gamedata.ItemList

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
        /// protobuf 리스트 데이터를 data-driven 방식으로 로드하여 Dictionary에 저장
        /// </summary>
        /// <typeparam name="TList">protobuf 리스트 메시지 타입 (예: SkillList, ItemList)</typeparam>
        /// <typeparam name="TElement">리스트 요소 타입 (예: Skill, Item)</typeparam>
        /// <param name="resourcePath">Resources 폴더 내 경로</param>
        /// <param name="parser">파서 (bytes → TList)</param>
        /// <param name="getList">TList에서 IEnumerable<TElement> 추출 람다</param>
        /// <param name="getId">TElement에서 id 추출 람다</param>
        /// <param name="outDict">결과를 담을 Dictionary</param>
        /// <returns>성공 여부</returns>
        public bool of<TList, TElement>(
            string resourcePath,
            Func<byte[], TList> parser,
            Func<TList, IEnumerable<TElement>> getList,
            Func<TElement, int> getId,
            Dictionary<int, TElement> outDict)
        {
            TextAsset bin = Resources.Load<TextAsset>(resourcePath);
            if (bin == null)
            {
                Debug.LogError($"{typeof(TElement).Name} 데이터 파일을 찾을 수 없습니다: {resourcePath}");
                return false;
            }

            var listObj = parser(bin.bytes);
            foreach (var elem in getList(listObj))
            {
                outDict[getId(elem)] = elem;
            }
            return true;
        }

        public void Load()
        {
            Debug.Log("ResourceLoader initialized.");

            of("GameData/skill", bytes => SkillList.Parser.ParseFrom(bytes), skillList => skillList.Skills, skill=>skill.Id, Skills);
            of("GameData/item", bytes => ItemList.Parser.ParseFrom(bytes), itemList => itemList.Items, item=>item.Id, Items);

            // 게임 데이터 로드가 완료되었음을 로그로 남김
            Debug.Log("ResourceLoader: Game data loaded successfully.");

        }

        public Dictionary<int, Gamedata.Skill> Skills = new Dictionary<int, Gamedata.Skill>();
        public Dictionary<int, Gamedata.Item> Items = new Dictionary<int, Gamedata.Item>();
    }
}
