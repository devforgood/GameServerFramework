using System.Collections.Generic;
using UnityEngine;

// 맵 오브젝트 분류. Map.json의 objects.static_objects / objects.movable_objects 에 대응한다.
public enum MapObjectKind
{
    Static,
    Movable
}

// 씬에 배치하는 맵 오브젝트 마커.
// Map JSON Updater가 씬 스캔 시 이 컴포넌트를 찾아 Map.json의 objects 항목으로 기록하고,
// 반대로 JSON에서 씬을 빌드할 때 이 컴포넌트를 붙여 오브젝트를 생성한다.
// - 오브젝트 이름: GameObject 이름 사용
// - 위치: transform.position 사용
// - 크기(Static): transform.localScale 사용
public class MapObjectMarker : MonoBehaviour
{
    [Header("Common")]
    public MapObjectKind kind = MapObjectKind.Static;
    [Tooltip("0이면 저장 시 자동 할당. 기존 오브젝트는 이름이 같으면 기존 id를 유지한다.")]
    public int objectId = 0;
    [Tooltip("오브젝트 분류 문자열 (building, decoration, tree, trap, npc, patrol_guard 등)")]
    public string objectType = "decoration";

    [Header("Static Object")]
    public bool collision = true;
    [Tooltip("0이면 JSON에 기록하지 않음 (trap 등 데미지 오브젝트용)")]
    public int damage = 0;
    [Tooltip("0이면 JSON에 기록하지 않음 (treasure_chest 등 루팅 오브젝트용)")]
    public int lootTableId = 0;

    [Header("Movable Object")]
    public float movementRange = 20f;
    public float movementSpeed = 1f;
    [Tooltip("순찰 경로 (월드 좌표). 비어 있으면 JSON에 기록하지 않음.")]
    public List<Vector3> patrolPath = new List<Vector3>();

    private void OnDrawGizmos()
    {
        if (kind == MapObjectKind.Static)
        {
            Gizmos.color = collision ? new Color(1f, 0.5f, 0f) : Color.cyan;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
        else
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 1f);

            // 이동 범위 표시
            if (movementRange > 0f)
            {
                Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, movementRange);
            }

            // 순찰 경로 표시
            if (patrolPath != null && patrolPath.Count > 1)
            {
                Gizmos.color = Color.magenta;
                for (int i = 0; i < patrolPath.Count; i++)
                {
                    Vector3 from = patrolPath[i];
                    Vector3 to = patrolPath[(i + 1) % patrolPath.Count];
                    Gizmos.DrawLine(from, to);
                    Gizmos.DrawWireSphere(from, 0.3f);
                }
            }
        }

        Gizmos.color = Color.white;
    }
}
