using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gate : MonoBehaviour
{
    // Map.json의 gates[] 항목과 1:1로 대응한다.
    // (이전에는 이름 기반(destinationMapName/GateName)이라 JSON(id 기반)과 스키마가 어긋났다.)
    [Header("Gate (Map.json 스키마)")]
    [Tooltip("게이트 고유 id. Map.json의 gates[].id. 0이면 Scan 시 자동 발급되어 컴포넌트에 기록된다.")]
    public int id;
    [Tooltip("게이트 이름. Map.json의 gates[].name. 비우면 GameObject 이름을 사용한다.")]
    public string gateName;
    [Tooltip("목적지 맵 id. Map.json의 target_map_id.")]
    public int targetMapId;
    [Tooltip("목적지 게이트 id. Map.json의 target_gate_id.")]
    public int targetGateId;
    [Tooltip("게이트 이용에 필요한 레벨. Map.json의 required_level.")]
    public int requiredLevel = 1;

    // 게이트는 isTrigger 콜라이더여야 하며, 로컬 플레이어가 진입하면 맵 이동을 요청한다.
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Gate] OnTriggerEnter gate:'{gateName}' by collider:'{other.name}'");

        if (Session.Instance == null)
        {
            Debug.LogWarning($"[Gate] '{gateName}': Session.Instance is null. abort.");
            return;
        }

        // 진입한 콜라이더가 로컬 플레이어(내 캐릭터)인지 확인한다.
        var actor = other.GetComponentInParent<Actor>();
        if (actor == null)
        {
            Debug.Log($"[Gate] '{gateName}': collider '{other.name}' has no Actor in parent. ignore.");
            return;
        }
        if (actor.agnet_id != Session.Instance.player_agnet_id)
        {
            Debug.Log($"[Gate] '{gateName}': actor agent_id({actor.agnet_id}) != local player_agnet_id({Session.Instance.player_agnet_id}). not local player, ignore.");
            return;
        }

        // 게이트 이동으로 막 도착하면 목적지 게이트 위치에 스폰되어 도착 즉시 여기로 들어온다.
        // "밖에서 안으로 들어온 경우"만 이동시키기 위해, 도착 직후 진입은 무시한다.
        // 플레이어가 게이트 밖으로 나가면(OnTriggerExit) 억제가 해제되어 다시 이동할 수 있다.
        if (Session.Instance.SuppressGateWarpUntilExit)
        {
            Debug.Log($"[Gate] '{gateName}': spawned on gate after warp. ignore until player exits.");
            return;
        }

        if (targetMapId <= 0)
        {
            Debug.LogWarning($"Gate '{gateName}' has no targetMapId.");
            return;
        }

        // Map.json에 기록된 target_map_id 로 목적지 맵을 직접 조회한다(이름 해석 불필요).
        var resource = GameManager.Instance.resource;
        var destMap = resource.GetMapById(targetMapId);
        if (destMap == null)
        {
            Debug.LogError($"Gate '{gateName}': destination map id {targetMapId} not found in loaded map data.");
            return;
        }

        // 맵 데이터에 연동된 씬 이름을 사용한다(없으면 맵 이름으로 폴백).
        string destScene = !string.IsNullOrEmpty(destMap.scene) ? destMap.scene : destMap.name;
        Debug.Log($"[Gate] '{gateName}': dest mapId:{targetMapId}, gateId:{targetGateId}, scene:'{destScene}'. requesting EnterGate.");
        Session.Instance.EnterGate(targetMapId, targetGateId, destScene);
    }

    // 로컬 플레이어가 게이트 밖으로 나가면 재이동 억제를 해제한다.
    // (도착 시 게이트 위 스폰 → 밖으로 걸어 나감 → 이후 다시 진입하면 정상 이동)
    private void OnTriggerExit(Collider other)
    {
        if (Session.Instance == null)
            return;

        var actor = other.GetComponentInParent<Actor>();
        if (actor == null || actor.agnet_id != Session.Instance.player_agnet_id)
            return;

        if (Session.Instance.SuppressGateWarpUntilExit)
        {
            Debug.Log($"[Gate] '{gateName}': local player exited gate. gate warp re-armed.");
            Session.Instance.SuppressGateWarpUntilExit = false;
        }
    }

    // 씬 뷰에서 게이트 영역을 파란색 와이어 큐브로 표시한다.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        var box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }
        Gizmos.color = Color.white;
    }
}
