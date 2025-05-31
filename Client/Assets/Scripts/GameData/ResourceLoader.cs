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
        /// protobuf 리스트 데이터를 data-driven 방식으로 로드
        /// </summary>
        /// <typeparam name="TList">protobuf 리스트 메시지 타입 (예: SkillList, ItemList)</typeparam>
        /// <typeparam name="TElement">리스트 요소 타입 (예: Skill, Item)</typeparam>
        /// <param name="resourcePath">Resources 폴더 내 경로</param>
        /// <param name="parser">파서 (bytes → TList)</param>
        /// <param name="getList">TList에서 IEnumerable<TElement> 추출 람다</param>
        /// <returns>요소 리스트</returns>
        public bool of<TList, TElement>(
            string resourcePath,
            Func<byte[], TList> parser,
            Func<TList, IEnumerable<TElement>> getList,
            List<TElement> outList)
        {
            TextAsset bin = Resources.Load<TextAsset>(resourcePath);
            if (bin == null)
            {
                Debug.LogError($"{typeof(TElement).Name} 데이터 파일을 찾을 수 없습니다: {resourcePath}");
                return false;
            }

            var listObj = parser(bin.bytes);
            outList.AddRange(getList(listObj));
            return true;
        }

        public void Load()
        {
            Debug.Log("ResourceLoader initialized.");
            var skills = new List<Gamedata.Skill>();
            var items = new List<Gamedata.Item>();

            of("GameData/skill", bytes => SkillList.Parser.ParseFrom(bytes), skillList => skillList.Skills, skills);
            of("GameData/item", bytes => ItemList.Parser.ParseFrom(bytes), itemList => itemList.Items, items);

            // 게임 데이터 로드가 완료되었음을 로그로 남김
            Debug.Log("ResourceLoader: Game data loaded successfully.");

        }
    }
}
