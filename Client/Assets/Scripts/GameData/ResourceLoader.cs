using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gamedata; // Gamedata.Skill, Gamedata.Item, Gamedata.Quest, Gamedata.SkillList, ...

namespace Assets.Scripts.GameData
{
    public partial class ResourceLoader
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

            // 한 테이블의 파싱 실패가 나머지 테이블 로드까지 죽이지 않도록 여기서 잡는다.
            // (JsonUtility 는 null 값 등에서 ArgumentException 을 던진다 — 어떤 테이블이
            //  왜 실패했는지 로그로 남겨야 원인 추적이 가능하다)
            TList listObj;
            try
            {
                string wrapped = "{\"items\":" + asset.text + "}";
                listObj = JsonUtility.FromJson<TList>(wrapped);
            }
            catch (Exception e)
            {
                Debug.LogError($"{typeof(TElement).Name} 데이터 파싱 실패 ({resourcePath}): {e.Message}\n" +
                               "JSON 에 null 값이 있으면 JsonUtility 가 실패합니다. GameDataFlow 검증을 실행하세요.");
                return false;
            }
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
        /// 모든 GameData 파일을 읽어 테이블 사전을 채운다.
        /// 호출 측에서 StartCoroutine(LoadAsync()) 형태로 실행한다.
        ///
        /// 테이블별 사전과 실제 로딩은 ResourceLoader.Tables.cs 가 table_meta.json 에서
        /// 생성한다 — 테이블이 늘어도 여기는 건드릴 필요가 없다.
        /// </summary>
        public IEnumerator LoadAsync()
        {
            Debug.Log("ResourceLoader initialized.");

            yield return LoadAllTables();
            yield return LoadLocalization();

            BuildMapIndexes();

            // 게임 데이터 로드가 완료되었음을 로그로 남김
            Debug.Log("ResourceLoader: Game data loaded successfully.");
        }

        /// <summary>
        /// 맵 안의 id 보유 오브젝트(게이트/스폰 지점)에 parent 를 연결하고 id 인덱스를 만든다.
        /// 이 id 들은 파일 전체에서 유일하므로 소속 맵을 몰라도 바로 찾을 수 있다.
        /// (서버 ResourceLoader::BuildIndexes 와 같은 역할)
        /// </summary>
        private void BuildMapIndexes()
        {
            MapGates.Clear();
            PlayerSpawns.Clear();

            foreach (var map in Maps.Values)
            {
                if (map.gates != null)
                {
                    foreach (var gate in map.gates)
                    {
                        gate.parent = map;
                        MapGates[gate.id] = gate;
                    }
                }

                if (map.spawn_points?.player_spawn != null)
                {
                    foreach (var spawn in map.spawn_points.player_spawn)
                    {
                        spawn.parent = map;
                        PlayerSpawns[spawn.id] = spawn;
                    }
                }
            }
        }

        // 테이블 사전(Skills, Items, ...)은 ResourceLoader.Tables.cs 에 생성된다.

        // 맵 안 오브젝트의 전역 id 인덱스. BuildMapIndexes 가 채운다.
        public Dictionary<int, Gamedata.MapGate> MapGates = new Dictionary<int, Gamedata.MapGate>();
        public Dictionary<int, Gamedata.MapSpawnPointsPlayerSpawn> PlayerSpawns
            = new Dictionary<int, Gamedata.MapSpawnPointsPlayerSpawn>();

        /// <summary>맵 id로 맵 데이터를 찾는다. 없으면 null.</summary>
        public Gamedata.Map GetMapById(int id)
        {
            return Maps.TryGetValue(id, out var map) ? map : null;
        }

        /// <summary>게이트 id(전역 유일)로 게이트를 찾는다. 없으면 null.</summary>
        public Gamedata.MapGate GetGateById(int id)
        {
            return MapGates.TryGetValue(id, out var gate) ? gate : null;
        }

        /// <summary>
        /// 게이트의 target_id 가 가리키는 도착 지점의 맵을 찾는다.
        /// 마커는 게이트일 수도 player_spawn 일 수도 있고, 둘 다 parent 로 소속 맵을 안다.
        /// 없으면 null.
        /// </summary>
        public Gamedata.Map GetTargetMap(int targetId)
        {
            if (MapGates.TryGetValue(targetId, out var gate))
                return gate.parent;
            if (PlayerSpawns.TryGetValue(targetId, out var spawn))
                return spawn.parent;
            return null;
        }

        /// <summary>맵 이름으로 맵 데이터를 찾는다. 없으면 null.</summary>
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
