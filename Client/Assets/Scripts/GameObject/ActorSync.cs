using syncnet;
using System.Collections.Generic;
using UnityEngine;

// 서버가 보내는 액터 스냅샷(UpdateActorNotify)을 씬에 반영한다:
// 처음 보는 agent 는 생성하고, 위치·상태·체력을 갱신하며, 매 프레임 위치를 보간한다.
//
// 위치의 진실은 항상 서버다. 다만 클라가 transform 을 직접 그리는 구간(점프 포물선)에는
// 그 액터만 반영을 멈춘다 — BeginLocalAnimation/EndLocalAnimation.
public class ActorSync
{
    // 이보다 큰 위치 변화(텔레포트/게이트/스폰)는 보간 대신 즉시 스냅한다.
    private const float PositionSnapDistance = 4f;

    /// <summary>actorId → 씬 오브젝트. 외부(Actor 등)가 참조를 지우기도 하므로 그대로 노출한다.</summary>
    public Dictionary<int, GameObject> Objects { get; } = new Dictionary<int, GameObject>();

    // 클라가 transform 을 직접 애니메이션 중인 액터. 이 액터만 서버 위치 반영을 건너뛴다.
    private readonly HashSet<int> locallyAnimated = new HashSet<int>();

    public bool TryGet(int actorId, out GameObject gameObject)
    {
        return Objects.TryGetValue(actorId, out gameObject);
    }

    /// <summary>씬 전환으로 파괴된 이전 맵의 액터 참조를 버린다.</summary>
    public void Clear()
    {
        Objects.Clear();
        locallyAnimated.Clear();
    }

    /// <summary>클라 연출이 이 액터의 transform 을 소유하는 구간의 시작/끝.</summary>
    public void BeginLocalAnimation(int actorId) { locallyAnimated.Add(actorId); }
    public void EndLocalAnimation(int actorId) { locallyAnimated.Remove(actorId); }

    /// <summary>서버 스냅샷 적용(생성 + 상태 갱신 + 디버그 표식).</summary>
    public void Apply(UpdateActorNotify notify)
    {
        for (int i = 0; i < notify.ActorsLength; ++i)
        {
            var updatedActor = notify.Actors(i).Value;
            var actorId = updatedActor.ActorId;

            Vector3 pos = new Vector3();
            GameObject gameObject = null;
            Actor actor = null;

            if (!Objects.TryGetValue(actorId, out gameObject))
            {
                if (updatedActor.Pos.HasValue)
                    pos = new Vector3(updatedActor.Pos.Value.X, updatedActor.Pos.Value.Y, updatedActor.Pos.Value.Z);
                else
                    Debug.LogWarning($"Actor {actorId} has no position, skipping creation");

                gameObject = Create(updatedActor.GameObjectType, pos, actorId);
                if (gameObject != null)
                {
                    Objects[actorId] = gameObject;
                    actor = gameObject.GetComponent<Actor>();
                    Debug.LogWarning($"Created new {updatedActor.GameObjectType} object for agent {actorId}");
                }
            }
            else
            {
                actor = gameObject.GetComponent<Actor>();
                if (updatedActor.Pos.HasValue)
                    pos = new Vector3(updatedActor.Pos.Value.X, updatedActor.Pos.Value.Y, updatedActor.Pos.Value.Z);
                else
                    pos = actor.pos; // Pos가 null인 경우 기존 위치 유지
            }

            if (actor != null)
                UpdateActorState(actor, updatedActor, pos);
            else
                Debug.LogError($"Failed to get Actor component for agent {actorId}");
        }

        // 관심영역(AoI) 이탈 처리.
        // 서버가 "이 액터는 더 이상 동기화하지 않는다"고 알려준 것이지 죽은 게 아니다 —
        // 사망 연출(ShowDeathEffect) 없이 조용히 치운다. 다시 가까워지면 서버가 전체 스냅샷으로
        // 다시 보내주므로 그때 새로 만들어진다.
        for (int i = 0; i < notify.RemovedLength; ++i)
        {
            int actorId = notify.Removed(i);
            GameObject leaving;
            if (!Objects.TryGetValue(actorId, out leaving))
                continue;

            Objects.Remove(actorId);
            locallyAnimated.Remove(actorId);
            if (leaving != null)
                Object.Destroy(leaving);
        }

        // Debug lines 처리
        for (int i = 0; i < notify.DebugsLength; ++i)
        {
            var debugInfo = notify.Debugs(i).Value;
            if (!debugInfo.EndPos.HasValue)
            {
                Debug.LogWarning($"Debug {i}: EndPos is null, skipping debug line");
                continue;
            }

            Vector3 pos = new Vector3(debugInfo.EndPos.Value.X, debugInfo.EndPos.Value.Y, debugInfo.EndPos.Value.Z);
            Object.Instantiate(Resources.Load("DebugTarget"), pos, Quaternion.identity);
        }
    }

    /// <summary>매 프레임 위치 반영. 서버 갱신(10Hz) 사이는 Actor 가 등속으로 메운다.</summary>
    public void Tick()
    {
        foreach (var gameObject in Objects.Values)
        {
            try
            {
                var actor = gameObject.GetComponent<Actor>();

                // 클라가 직접 transform 을 애니메이션하는 동안(점프 포물선)만 서버 위치 반영을 멈춘다.
                // 예전에는 input_locked 전체를 건너뛰었는데, 그러면 돌진(dash)처럼 서버가 잠금 상태로
                // 이동시키는 스킬은 잠금이 풀릴 때까지 제자리에 있다가 도착 지점으로 튀어(=텔레포트처럼)
                // 보였다. 입력 잠금은 '시전 중 조작 금지'이지 '위치 동기화 금지'가 아니다.
                //
                // 돌진은 서버가 매 틱 실제로 이동시키는 '이동 공격'이라 위치는 서버가 진실이다
                // (오라 데미지·몬스터 감지 판정이 그 경로를 쓰고, 벽에 막히면 네비메시가 경로를 꺾는다).
                if (locallyAnimated.Contains(actor.actor_id))
                    continue;

                gameObject.transform.position = actor.InterpolatedPosition();
            }
            catch
            {

            }
        }
    }

    private GameObject Create(GameObjectType type, Vector3 pos, int actorId)
    {
        GameObject gameObject = null;
        Actor actor = null;

        switch (type)
        {
            case GameObjectType.Monster:
                gameObject = (GameObject)Object.Instantiate(Resources.Load("Monster"), pos, Quaternion.identity);
                actor = gameObject.GetComponent<Monster>();
                break;
            case GameObjectType.Character:
                gameObject = (GameObject)Object.Instantiate(Resources.Load("Character2"), pos, Quaternion.identity);
                actor = gameObject.GetComponent<Character>();
                break;
            default:
                Debug.LogError("error game object type");
                return null;
        }

        if (actor != null)
            actor.actor_id = actorId;

        return gameObject;
    }

    private void UpdateActorState(Actor actor, ActorInfo updatedActor, Vector3 pos)
    {
        actor.SetServerPosition(pos, PositionSnapDistance);
        actor.input_locked = updatedActor.InputLocked;

        // 상태 업데이트
        if (updatedActor.State.HasValue)
            actor.UpdateState(actor.gameObject, updatedActor.State.Value.State);

        // HP 업데이트
        if (updatedActor.Health.HasValue)
        {
            int oldHealth = actor.health;
            int newHealth = updatedActor.Health.Value.Health;
            if (oldHealth > newHealth)
                Debug.LogWarning($"=== DAMAGE EVENT === Actor {actor.actor_id} ({actor.gameObject.name}) health: {oldHealth} -> {newHealth}");

            actor.UpdateHealth(newHealth);
        }

        // 몬스터 상태에 따른 시각적 업데이트
        if (updatedActor.GameObjectType == GameObjectType.Monster && updatedActor.State.HasValue)
            UpdateMonsterVisuals(actor.gameObject, updatedActor.State.Value.State);
    }

    private void UpdateMonsterVisuals(GameObject monster, AIState state)
    {
        var renderer = monster.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        switch (state)
        {
            case AIState.Detect: renderer.material.color = Color.red; break;
            case AIState.Patrol: renderer.material.color = Color.white; break;
            case AIState.Attack: renderer.material.color = Color.blue; break;
        }
    }
}
