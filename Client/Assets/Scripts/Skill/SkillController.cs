using syncnet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 스킬 시전의 클라 측 흐름을 담당한다.
//
//   시전: 로컬 쿨다운 예측으로 거른 뒤 전송 → 낙관적으로 연출을 즉시 재생
//         (서버 브로드캐스트는 캐스터를 제외하므로 자신의 연출은 여기서 재생해야 한다)
//   거부: 서버가 result != Success 를 돌려주면 재생 중인 연출을 취소하고 예측을 보정
//   원격: 다른 캐릭터의 시전 브로드캐스트를 받아 같은 연출을 재생
//
// 판정은 전적으로 서버가 한다. 여기 있는 쿨다운은 왕복 없이 즉시 반응하기 위한 예측일 뿐이다.
public class SkillController
{
    // todo : 스킬 테이블 생성시 스킬별 지속 시간 설정
    private const float JumpDuration = 1f; // 점프 지속 시간
    private const float JumpHeight = 3f;   // 점프 높이

    private readonly MonoBehaviour host;          // 코루틴 실행 + 연출 디스패처 host
    private readonly ServerConnection connection;
    private readonly ActorSync actors;
    private readonly Func<int> playerAgentId;     // 내 캐릭터 agent id(세션이 소유)

    private readonly SkillCooldownTracker cooldowns = new SkillCooldownTracker();

    // 낙관적으로 재생한 연출 핸들. 서버가 거부하면 이걸 멈춰 연출을 취소한다.
    private readonly Dictionary<int, Coroutine> localFx = new Dictionary<int, Coroutine>();

    /// <summary>HUD 등에서 남은 쿨다운을 표시하기 위한 접근자.</summary>
    public SkillCooldownTracker Cooldowns => cooldowns;

    private long UnixTimestampMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public SkillController(MonoBehaviour host, ServerConnection connection, ActorSync actors, Func<int> playerAgentId)
    {
        this.host = host;
        this.connection = connection;
        this.actors = actors;
        this.playerAgentId = playerAgentId;
    }

    /// <summary>캐릭터가 새로 만들어지면(게이트 이동/재접속) 서버 스킬 상태도 초기화되므로 예측을 버린다.</summary>
    public void Reset()
    {
        cooldowns.Clear();
        localFx.Clear();
    }

    public void Cast(int skillId, Vector3 pos, int type)
    {
        var timestamp = UnixTimestampMs;
        int agentId = playerAgentId();
        Debug.Log($"UseSkill agent_id: {agentId}, pos({pos.x}, {pos.y}, {pos.z}) timestamp({timestamp})");

        GameObject gameObject;
        if (!actors.TryGet(agentId, out gameObject))
        {
            Debug.LogError("Player agent not found in game_objects dictionary.");
            return;
        }

        var actor = gameObject.GetComponent<Actor>();
        if (actor == null)
        {
            Debug.LogError("Actor component not found on player agent GameObject.");
            return;
        }
        if (actor.input_locked)
        {
            Debug.Log("Player input is locked, cannot use skill.");
            return;
        }

        Gamedata.Skill resSkill = null;
        if (!GameManager.Instance.resource.Skills.TryGetValue(skillId, out resSkill))
        {
            Debug.LogError($"Skill with ID {skillId} not found in resource skills.");
            return;
        }

        // 로컬 쿨다운 예측으로 미리 거른다. 이게 없으면 쿨다운 중에도 패킷을 보내고 연출까지 재생해
        // 서버는 거부(OnCooldown)했는데 클라만 이펙트가 나오는 상태가 된다.
        float remaining;
        if (!cooldowns.IsReady(resSkill, out remaining))
        {
            Debug.Log($"UseSkill 스킵: skillId {skillId} 쿨다운 {remaining:F1}초 남음");
            return;
        }

        connection.Send(PacketFactory.CreateUseSkillMessage(skillId, agentId, pos, type, timestamp));
        cooldowns.OnCast(resSkill);

        // 서버 브로드캐스트는 캐스터(자신)를 제외하므로, 자신의 연출은 여기서 즉시 재생한다.
        if (resSkill.code_name == "JumpSkill")
        {
            host.StartCoroutine(JumpToPosition(gameObject, gameObject.transform.position, pos, JumpDuration, JumpHeight, timestamp));
        }
        else
        {
            // 서버가 거부하면 취소할 수 있도록 연출 코루틴을 스킬별로 보관한다.
            Coroutine running;
            if (localFx.TryGetValue(skillId, out running) && running != null)
                host.StopCoroutine(running);
            localFx[skillId] = SkillFxDispatcher.Play(resSkill, gameObject, pos, host);
        }
    }

    /// <summary>서버가 보낸 UseSkill 처리 — 성공 브로드캐스트(원격 연출)이거나 내 시전의 거부 응답이다.</summary>
    public void OnServerMessage(GameMessage recv_msg, syncnet.UseSkill useSkill)
    {
        if (recv_msg.Result != StatusCode.Success)
        {
            OnRejected(useSkill, recv_msg.Result);
            return;
        }

        // Pos가 null인 경우 처리
        if (!useSkill.Pos.HasValue)
        {
            Debug.LogWarning("UseSkill: Pos is null, skipping skill effect");
            return;
        }

        var pos = new Vector3(useSkill.Pos.Value.X, useSkill.Pos.Value.Y, useSkill.Pos.Value.Z);

        GameObject gameObject;
        if (!actors.TryGet(useSkill.Id, out gameObject))
            return;

        Gamedata.Skill resSkill = null;
        GameManager.Instance.resource.Skills.TryGetValue(useSkill.SkillId, out resSkill);

        // 점프는 액터 위치를 시간에 걸쳐 이동시키는 특수 연출이라 기존 경로 유지(지연 보정 포함).
        if (resSkill != null && resSkill.code_name == "JumpSkill")
        {
            var remoteDuration = JumpDuration - (UnixTimestampMs - useSkill.Timestamp) / 1000f;
            host.StartCoroutine(JumpToPosition(gameObject, gameObject.transform.position, pos, remoteDuration, JumpHeight, useSkill.Timestamp));
            return;
        }

        // 그 외는 fx 기반 디스패처로 연출한다(원격 관전자에게도 자신과 동일한 연출이 보인다).
        SkillFxDispatcher.Play(resSkill, gameObject, pos, host);
    }

    /// <summary>
    /// 서버가 시전을 거부했을 때. 낙관적으로 재생한 연출을 취소하고 쿨다운 예측을 되돌린다.
    /// 로컬 예측이 정확하면 거의 오지 않는 경로다(시차/패킷 유실 등으로 어긋났을 때의 안전망).
    /// </summary>
    private void OnRejected(syncnet.UseSkill useSkill, StatusCode result)
    {
        int skillId = useSkill.SkillId;

        Coroutine running;
        if (localFx.TryGetValue(skillId, out running))
        {
            if (running != null)
                host.StopCoroutine(running);
            localFx.Remove(skillId);
        }

        Gamedata.Skill resSkill = null;
        if (GameManager.Instance != null && GameManager.Instance.resource != null)
            GameManager.Instance.resource.Skills.TryGetValue(skillId, out resSkill);
        cooldowns.OnRejected(resSkill);

        Debug.LogWarning($"UseSkill 거부됨: skillId {skillId}, result {result} — 연출을 취소하고 쿨다운 예측을 보정합니다.");
    }

    // 점프는 클라가 포물선을 직접 그리는 유일한 스킬이다(서버에는 중간 위치가 없고 착지 순간만 있다).
    // 그리는 동안에는 서버 위치 반영이 끼어들지 않도록 액터를 '로컬 애니메이션 중'으로 등록한다.
    // 중간에 StopCoroutine 으로 끊겨도 finally 가 반드시 해제한다(안 그러면 그 액터가 영영 안 움직인다).
    private IEnumerator JumpToPosition(GameObject gameObject, Vector3 start, Vector3 end, float duration, float height, long timestamp)
    {
        float time = 0;
        float dropPoint = 0.7f; // 상승 구간 비율 (0~1)

        var actor = gameObject.GetComponent<Actor>();
        actor.input_locked = true; // 서버 통보 전까지의 선반영(시전 차단용). 위치 동기화와는 무관하다.
        actors.BeginLocalAnimation(actor.agnet_id);

        try
        {
            while (time < duration)
            {
                float t = time / duration;
                float yOffset;

                if (t < dropPoint)
                {
                    // 천천히 상승 (곡선 조정 가능)
                    yOffset = Mathf.Lerp(0, height, t / dropPoint);
                }
                else
                {
                    // 완만하게 하강 (선형 하강)
                    float fallT = (t - dropPoint) / (1f - dropPoint); // 0~1
                    yOffset = Mathf.Lerp(height, 0, fallT);
                }

                gameObject.transform.position = Vector3.Lerp(start, end, t) + Vector3.up * yOffset;
                time += Time.deltaTime;
                yield return null;
            }

            // 마지막 위치는 착지점
            gameObject.transform.position = end;
            Debug.Log($"JumpToPosition End: {gameObject.name}, pos({end.x}, {end.y}, {end.z}), timestamp({timestamp})");
        }
        finally
        {
            actors.EndLocalAnimation(actor.agnet_id);
        }
    }
}
