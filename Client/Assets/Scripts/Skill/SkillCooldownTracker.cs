using System.Collections.Generic;
using UnityEngine;

// 클라 로컬 쿨다운 예측기 — 서버 SkillSet 의 페이즈 머신(Active→Cooldown→Ready)을 데이터 그대로 흉내 낸다.
//
// 시전 검증은 서버 권위지만, 클라는 전송 직후 연출을 낙관적으로 재생하기 때문에
// 서버가 쿨다운으로 거부하면 "이펙트는 나오는데 캐릭터는 그대로"인 헛연출이 남았다(차지에서 특히 눈에 띈다).
// 그래서 두 겹으로 막는다:
//   1) 여기(로컬 예측)에서 미리 걸러 서버 왕복 없이 즉시 반응한다 — 헛시전 대부분이 여기서 끝난다.
//   2) 그래도 어긋나면(시차/패킷 유실) 서버의 거부 응답을 받아 Session 이 연출을 취소하고 OnRejected 로 보정한다.
//
// 쿨다운/지속시간은 skill.json 을 그대로 쓰므로 클라 전용 튜닝 값이 없다(서버와 같은 데이터 = 같은 판정).
public class SkillCooldownTracker
{
    // skillId → 다시 시전 가능해지는 시각(Time.time 기준). 지난 항목은 조회 시 정리한다.
    private readonly Dictionary<int, float> readyAt = new Dictionary<int, float>();

    /// <summary>시전 가능하면 true. 아니면 remaining 에 남은 시간(초)을 채운다.</summary>
    public bool IsReady(Gamedata.Skill data, out float remaining)
    {
        remaining = 0f;
        if (data == null)
            return false;

        if (!readyAt.TryGetValue(data.id, out float at))
            return true;

        remaining = at - Time.time;
        if (remaining <= 0f)
        {
            readyAt.Remove(data.id);
            remaining = 0f;
            return true;
        }
        return false;
    }

    /// <summary>남은 쿨다운(초). 준비됐으면 0 — HUD 표시용.</summary>
    public float Remaining(int skillId)
    {
        if (!readyAt.TryGetValue(skillId, out float at))
            return 0f;
        return Mathf.Max(0f, at - Time.time);
    }

    /// <summary>시전 전송 직후. 서버와 같은 순서로 Active(duration) → Cooldown(cooldown) 만큼 잠근다.</summary>
    public void OnCast(Gamedata.Skill data)
    {
        if (data == null)
            return;

        float lockTime = data.duration + (float)data.cooldown;
        if (lockTime <= 0f)
            return;

        readyAt[data.id] = Time.time + lockTime;
    }

    /// <summary>
    /// 서버가 시전을 거부했을 때의 보정. 예측이 서버보다 앞서 있었다는 뜻이므로
    /// 최소한 쿨다운 한 번은 더 기다린다(연타로 계속 거부당하지 않게).
    /// </summary>
    public void OnRejected(Gamedata.Skill data)
    {
        if (data == null)
            return;

        float at = Time.time + (float)data.cooldown;
        if (!readyAt.TryGetValue(data.id, out float prev) || prev < at)
            readyAt[data.id] = at;
    }

    /// <summary>캐릭터가 새로 만들어지면(게이트 이동/재접속) 서버 쪽 스킬 상태도 초기화되므로 예측을 버린다.</summary>
    public void Clear()
    {
        readyAt.Clear();
    }
}
