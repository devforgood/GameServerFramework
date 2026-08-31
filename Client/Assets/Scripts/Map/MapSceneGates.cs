using System.Collections.Generic;
using UnityEngine;

// 씬의 게이트를 Map.json 에 맞춘다.
//
// 게이트는 클라에서 트리거 콜라이더로만 존재한다(`Gate.OnTriggerEnter` → `EnterGate`).
// 그래서 **씬 파일에 GameObject 가 없으면 그 게이트는 지나갈 수 없다.** 그런데 원본은
// Map.json 이고 씬은 사람이 유니티에서 손으로 맞춰야 했다 — 데이터에 게이트를 추가한 뒤
// 씬 작업을 잊으면, 서버는 통과시킬 준비가 끝났는데 플레이어만 못 지나가는 상태가 된다.
// 실제로 그렇게 어긋나 있었다(게이트 1007/1008 이 씬에 없었고, 1001/1002 는 낡은 값이었다).
//
// 그래서 맵 씬이 준비될 때마다 데이터로 다시 맞춘다. 씬에 이미 있는 게이트는 그대로 쓰고
// 값만 갱신하므로, 유니티에서 배치한 게이트(Gate.prefab 인스턴스)의 외형은 유지된다.
//
// 위치까지 데이터를 따르는 이유: 서버는 도착한 플레이어를 **게이트의 데이터 좌표**에 세운다.
// 씬 쪽 위치가 다르면 플레이어는 트리거가 없는 자리에 도착해, 게이트가 고장 난 것처럼 보인다.
public static class MapSceneGates
{
    // 데이터로 새로 만든 게이트를 모아 두는 부모. 씬에 저장되지 않는 런타임 오브젝트다.
    public const string GroupName = "MapGates (from Map.json)";

    /// <summary>
    /// 활성 씬의 게이트를 Map.json 에 맞춘다(없으면 만들고, 있으면 값을 갱신하고,
    /// 데이터에 없는 것은 지운다). 맵 데이터가 아직 없거나 씬이 맵이 아니면 아무것도 하지 않는다.
    /// </summary>
    public static void Reconcile(string sceneName)
    {
        var resource = GameManager.Instance != null ? GameManager.Instance.resource : null;
        if (resource == null)
            return;

        var map = resource.GetMapByScene(sceneName);
        if (map == null)
            return;  // 맵이 아닌 씬(타이틀 등)은 건드리지 않는다.

        // 씬에 이미 있는 게이트를 id 로 모은다. id 가 없는 것과 id 가 겹치는 것은 쓸 수 없다 —
        // 전자는 밟아도 서버에 보낼 것이 없고(`Gate.OnTriggerEnter` 가 무시한다),
        // 후자는 어느 쪽이 진짜인지 알 수 없는 데다 둘 다 두면 트리거가 두 번 발동한다.
        var existing = new Dictionary<int, Gate>();
        var strays = new List<KeyValuePair<Gate, string>>();
        foreach (var gate in Object.FindObjectsByType<Gate>(FindObjectsInactive.Include))
        {
            if (gate.id <= 0)
                strays.Add(new KeyValuePair<Gate, string>(gate, "id 가 없어"));
            else if (existing.ContainsKey(gate.id))
                strays.Add(new KeyValuePair<Gate, string>(gate, $"id {gate.id} 가 겹쳐"));
            else
                existing[gate.id] = gate;
        }

        int created = 0, updated = 0;
        Transform group = null;

        var dataGates = map.gates ?? new List<Gamedata.MapGate>();
        foreach (var data in dataGates)
        {
            if (data == null || data.id <= 0)
                continue;

            Gate gate;
            if (existing.TryGetValue(data.id, out gate))
            {
                existing.Remove(data.id);
                if (Apply(gate, data))
                    updated++;   // 씬에 있던 게이트가 데이터와 달랐다
            }
            else
            {
                if (group == null)
                    group = new GameObject(GroupName).transform;
                gate = Create(data, group);
                Apply(gate, data);
                created++;
            }
        }

        // 데이터에 없는 게이트는 밟아도 서버가 거절한다(모르는 id). 남겨 두면 "밟았는데
        // 아무 일도 없는" 트리거가 되므로 치운다.
        foreach (var leftover in existing.Values)
            strays.Add(new KeyValuePair<Gate, string>(leftover, "Map.json 에 없어"));

        foreach (var stray in strays)
        {
            if (stray.Key == null)
                continue;
            Debug.LogWarning($"[MapSceneGates] '{sceneName}': 게이트 '{stray.Key.name}' 는 {stray.Value} " +
                             "제거한다. (남겨 두려면 맵 툴에서 Scan 을 실행해 Map.json 에 기록하세요)");
            Object.Destroy(stray.Key.gameObject);
        }

        if (created > 0 || updated > 0 || strays.Count > 0)
        {
            Debug.Log($"[MapSceneGates] '{sceneName}' 게이트 {dataGates.Count}개 기준 정리: " +
                      $"생성 {created}, 갱신 {updated}, 제거 {strays.Count}");
        }
    }

    private static Gate Create(Gamedata.MapGate data, Transform group)
    {
        // `Assets/Prefabs/Gate.prefab` 과 같은 모습으로 만든다 — 기본 캡슐 메시(r 0.5, h 2)에
        // 트리거 콜라이더. 데이터로 세운 게이트와 씬에 놓아 둔 게이트가 달라 보이면
        // 플레이어에게는 한쪽만 게이트처럼 보인다. (프리팹은 Resources 밖이라 못 부른다)
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = string.IsNullOrEmpty(data.name) ? $"Gate_{data.id}" : data.name;
        go.tag = "Gate";
        go.transform.SetParent(group, true);

        var trigger = go.GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;

        // 트리거는 두 콜라이더 중 **한쪽에 Rigidbody 가 있어야** 발생한다. 플레이어
        // (Resources/Character2)는 CapsuleCollider 만 있으므로 게이트가 들고 있어야 한다.
        // Gate.prefab 도 같은 이유로 kinematic Rigidbody 를 갖고 있다.
        var body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        return go.AddComponent<Gate>();
    }

    /// <summary>게이트 컴포넌트를 데이터에 맞춘다. 실제로 바뀐 것이 있으면 true.</summary>
    private static bool Apply(Gate gate, Gamedata.MapGate data)
    {
        bool changed = false;

        if (gate.id != data.id)
        {
            gate.id = data.id;
            changed = true;
        }

        string name = string.IsNullOrEmpty(data.name) ? gate.gameObject.name : data.name;
        if (gate.gateName != name)
        {
            gate.gateName = name;
            changed = true;
        }

        GateType type = data.type == "one_way" ? GateType.OneWay : GateType.TwoWay;
        if (gate.gateType != type)
        {
            gate.gateType = type;
            changed = true;
        }

        if (gate.targetId != data.target_id)
        {
            gate.targetId = data.target_id;
            changed = true;
        }

        int level = data.required_level > 0 ? data.required_level : 1;
        if (gate.requiredLevel != level)
        {
            gate.requiredLevel = level;
            changed = true;
        }

        if (data.position != null)
        {
            var position = new Vector3((float)data.position.x, (float)data.position.y, (float)data.position.z);
            // 0.05m 는 좌표를 double 로 저장했다 float 로 읽으면서 생기는 오차보다 훨씬 크다.
            // 그보다 크게 벌어졌으면 씬이 낡은 것이므로 데이터 쪽으로 옮긴다.
            if ((gate.transform.position - position).sqrMagnitude > 0.05f * 0.05f)
            {
                gate.transform.position = position;
                changed = true;
            }
        }

        return changed;
    }
}
