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
