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
    public int spawnInterval = 30;
    public int bossId = 0;
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
        Gizmos.color = Color.white;
    }
}
