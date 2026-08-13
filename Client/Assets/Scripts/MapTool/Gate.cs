using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Map.json의 gates[].type 과 대응한다.
// TwoWay: 입구/출구 겸용. 목적지 게이트와 짝을 이뤄야 한다(상대도 TwoWay + 상호 참조).
// OneWay: 레이드 등 인스턴스 던전 입구용. 짝이 필요 없고,
//         Target Gate Id 를 0 으로 두면 목적지 맵의 player_spawn 지점에 도착한다.
public enum GateType
{
    TwoWay,
    OneWay,
}

public class Gate : MonoBehaviour
{
    // Map.json의 gates[] 항목과 1:1로 대응한다.
    // (이전에는 이름 기반(destinationMapName/GateName)이라 JSON(id 기반)과 스키마가 어긋났다.)
    [Header("Gate (Map.json 스키마)")]
    [Tooltip("게이트 고유 id. Map.json의 gates[].id. 데이터 전체에서 유일하다. 0이면 Scan 시 자동 발급되어 컴포넌트에 기록된다.")]
    public int id;
    [Tooltip("게이트 이름. Map.json의 gates[].name. 비우면 GameObject 이름을 사용한다.")]
    public string gateName;
    [Tooltip("게이트 타입. Map.json의 gates[].type. TwoWay는 목적지 게이트와 짝(상호 참조) 필수, OneWay는 인스턴스 던전 입구용(스폰 지점을 가리켜도 된다).")]
    public GateType gateType = GateType.TwoWay;
    [Tooltip("도착 지점의 전역 유일 id. Map.json의 target_id. 게이트 id 이거나 player_spawn id 다 — 목적지 맵은 그 마커가 속한 맵으로 정해진다.")]
    public int targetId;
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
        if (actor.actor_id != Session.Instance.player_actor_id)
        {
            Debug.Log($"[Gate] '{gateName}': actor actor_id({actor.actor_id}) != local player_actor_id({Session.Instance.player_actor_id}). not local player, ignore.");
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

        if (id <= 0)
        {
            Debug.LogWarning($"Gate '{gateName}' has no id. (맵툴에서 Scan 을 실행해 id 를 발급하세요)");
            return;
        }

        // 서버에는 "내가 밟은 게이트"만 알린다. 목적지는 그 게이트의 target_id 가 정하므로
        // 클라가 목적지를 고를 수 없고, 응답의 mapId 로 씬을 정한다.
        Debug.Log($"[Gate] '{gateName}'(id {id}): requesting EnterGate. target_id:{targetId}");
        Session.Instance.EnterGate(id);
    }

    // 로컬 플레이어가 게이트 밖으로 나가면 재이동 억제를 해제한다.
    // (도착 시 게이트 위 스폰 → 밖으로 걸어 나감 → 이후 다시 진입하면 정상 이동)
    private void OnTriggerExit(Collider other)
    {
        if (Session.Instance == null)
            return;

        var actor = other.GetComponentInParent<Actor>();
        if (actor == null || actor.actor_id != Session.Instance.player_actor_id)
            return;

        if (Session.Instance.SuppressGateWarpUntilExit)
        {
            Debug.Log($"[Gate] '{gateName}': local player exited gate. gate warp re-armed.");
            Session.Instance.SuppressGateWarpUntilExit = false;
        }
    }

    // 씬 뷰에서 게이트 영역을 와이어 큐브로 표시한다(양방향: 파란색, 단방향: 주황색).
    private void OnDrawGizmos()
    {
        Gizmos.color = gateType == GateType.OneWay ? new Color(1f, 0.6f, 0f) : Color.blue;
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
