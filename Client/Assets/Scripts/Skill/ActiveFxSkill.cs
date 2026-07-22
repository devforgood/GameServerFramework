using UnityEngine;

// fx 기반 액티브 스킬의 로컬 입력/프리뷰 클래스.
// 클라 전용이며 데이터 code_name 이 없다 → SkillFactory 스위치/서버 파생 클래스와 무관하다.
// GameInputManager 가 gamedata 를 채워 직접 생성하고, 우클릭 시 마우스 지면 지점을 목표로
// SkillFxDispatcher 연출을 재생한다(순수 프리뷰: 네트워크 전송 없음).
// 실제 시전/데미지는 Session.UseSkill(서버 검증→브로드캐스트) 경로가 담당한다.
public class ActiveFxSkill : Skill
{
    public override void OnSkillButtonDown(GameInputManager context)
    {
        Vector3? pos = context.GetMouseGroundPosition();
        if (pos.HasValue && context.player != null)
            SkillFxDispatcher.Play(gamedata, context.player, pos.Value, context);
    }
}
