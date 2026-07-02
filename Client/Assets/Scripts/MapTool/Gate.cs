using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gate : MonoBehaviour
{
    [Header("Gate Settings")]
    public string gateName;
    public string destinationMapName;
    public string destinationGateName;

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

        if (string.IsNullOrEmpty(destinationMapName))
        {
            Debug.LogWarning($"Gate '{gateName}' has no destinationMapName.");
            return;
        }

        // 로드된 맵 데이터에서 목적지 맵과 도착 게이트를 찾아 맵ID/게이트ID 를 해석한다.
        var resource = GameManager.Instance.resource;
        var destMap = resource.GetMapByName(destinationMapName);
        if (destMap == null)
        {
            Debug.LogError($"Gate '{gateName}': destination map '{destinationMapName}' not found in loaded map data.");
            return;
        }

        int gateId = 1;
        if (destMap.gates != null)
        {
            var destGate = destMap.gates.Find(g => string.Equals(g.name, destinationGateName, System.StringComparison.OrdinalIgnoreCase));
            if (destGate != null)
                gateId = destGate.id;
            else
                Debug.LogWarning($"Gate '{gateName}': destination gate '{destinationGateName}' not found in map '{destinationMapName}'. Using gateId={gateId}.");
        }

        // 맵 데이터에 연동된 씬 이름을 사용한다(없으면 맵 이름으로 폴백).
        string destScene = !string.IsNullOrEmpty(destMap.scene) ? destMap.scene : destinationMapName;
        Debug.Log($"[Gate] '{gateName}': resolved dest mapId:{destMap.id}, gateId:{gateId}, scene:'{destScene}'. requesting EnterGate.");
        Session.Instance.EnterGate(destMap.id, gateId, destScene);
    }
}
