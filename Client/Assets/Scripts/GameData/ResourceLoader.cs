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

        // Resources 폴더에서 protobuf 바이너리 파일을 읽어 SkillList 반환
        public List<Skill> LoadSkills(string resourcePath)
        {
            TextAsset bin = Resources.Load<TextAsset>(resourcePath);
            if (bin == null)
            {
                Debug.LogError($"Skill 데이터 파일을 찾을 수 없습니다: {resourcePath}");
                return new List<Skill>();
            }

            var skillList = SkillList.Parser.ParseFrom(bin.bytes);
            return new List<Skill>(skillList.Skills);
        }

        // Resources 폴더에서 protobuf 바이너리 파일을 읽어 ItemList 반환
        public List<Item> LoadItems(string resourcePath)
        {
            TextAsset bin = Resources.Load<TextAsset>(resourcePath);
            if (bin == null)
            {
                Debug.LogError($"Item 데이터 파일을 찾을 수 없습니다: {resourcePath}");
                return new List<Item>();
            }

            var itemList = ItemList.Parser.ParseFrom(bin.bytes);
            return new List<Item>(itemList.Items);
        }

        public void Load()
        {
            // 초기화 작업이 필요하다면 여기에 작성
            Debug.Log("ResourceLoader initialized.");
            var skills = LoadSkills("GameData/skill");
            var items = LoadItems("GameData/item");
        }
    }
}
