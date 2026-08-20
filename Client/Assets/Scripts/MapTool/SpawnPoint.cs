using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Spawn 타입 enum (전역으로 이동)
public enum SpawnType
{
    Player,
    Monster,
    Boss
}

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("스폰 고유 id (맵 내 유일). Map.json의 spawn id. 0이면 Scan 시 자동 발급되어 컴포넌트에 기록된다.")]
    public int id;
    public SpawnType spawnType = SpawnType.Player;
    public int monsterId = 0;

    [Tooltip("이 마커가 유지할 몬스터 수. 죽으면 spawnInterval 뒤에 이 수만큼 다시 채워진다.")]
    [Min(1)] public int count = 1;

    [Tooltip("마커 중심에서 몬스터를 흩뿌릴 반경(0이면 마커 지점에 정확히 스폰). navmesh 위에 들어오는 크기로 잡을 것.")]
    [Min(0f)] public float radius = 0f;

    [Tooltip("죽은 자리를 다시 채우기까지의 시간(초). 0이면 리스폰하지 않는다.")]
    public int spawnInterval = 30;
    public int bossId = 0;

    [Tooltip("맵이 열린 뒤 최초 스폰까지 기다리는 시간(초).")]
    public int spawnDelay = 0;

    private void OnDrawGizmos()
    {
        switch (spawnType)
        {
            case SpawnType.Player:
                Gizmos.color = Color.green;
                break;
            case SpawnType.Monster:
                Gizmos.color = Color.red;
                break;
            case SpawnType.Boss:
                Gizmos.color = Color.yellow;
                break;
        }

        Gizmos.DrawWireSphere(transform.position, 1f);

        // 흩뿌림 반경. 실제로 몬스터가 어디에 설 수 있는지 씬에서 바로 보이게 한다
        // (반경이 navmesh 밖으로 나가면 그만큼 스폰이 실패한다).
        if (spawnType == SpawnType.Monster && radius > 0f)
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        Gizmos.color = Color.white;
    }
}
