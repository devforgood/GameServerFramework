using System.Collections;
using System.Collections.Generic;


public enum SkillType
{
    None,
    ThrowSkill,
    PushSkill,
    InstallSkill,
    StraightSkill,
    TeleportSkill,
    TargetPointSkill,   // 6 : 목표 좌표 지면 생성 스킬
    InstallAreaOfEffect = 8,
}

public enum SkillTarget
{
    None,
    Enemy,
    Self,
    Ally,
    SelfAndAlly,
    TargetPoint 
}

public enum LaunchType
{
    None,
    ConsecutiveLaunch,  // 연속 발사
    OneShot,            // 한발 발사
    NWayBullet,         // N way 발사.
}

public enum LaunchForm
{
    None,
    Straight,
    Radial
}
public enum eSkillCoolTimeType
{
    None,
    CHARGINTYPE,
    COOLTIMETYPE,
}

public enum DamageTarget
{
    None,
    Player,
    DestroyObject,
    Castle
}

public enum ExplosionMethod
{
    None,
    NoCover,    // 엄폐불가능 범위 전체 
    Cover,      // 엄폐가능
}

public enum eSkillGuideType
{
    None,
    Path,                       // 경로 표시용 가이드
    Explosion_Range,            // 폭발 영역 가이드
    Hit_Range,                  // 피격 범위 가이드
    TargetPoint,                // 타겟 위치 가이드 (범위 가이드 대신 쓰임)
    Origin_Explosion_Range,     // 발사 캐릭터 위치에서 표시되는 폭발 영역 가이드 (조이스틱은 영향 받지 않음)
}

public enum SpellSubType
{
    None,
    NotDivision, // 구분 없음
    FinishTime, // 스킬 종료 부분
    MiddleTime, // 스킬 중간 부분
    StartTime, // 스킬 시작 부분
}