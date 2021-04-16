using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public enum ReservedPlayerId
    {
        Fall = -1, // 추락
        Trap = -2, // 함정
        DotDamage = -3,  // 도트 대미지 디버프(spell)
        Train = -4, // 기차
    }

    public enum ApplyObject
    {
        Oneself = 1, // 자신
        Castle, // 기지(거점)
        OurTeam, // 우리팀
        OpposingTeam, // 상대팀
    }

    public enum GetSpellProc
    {
        Accumulate = 1, // 누적
        Reset, // 리셋
    }

    public enum BuffType
    {
        /// <summary>
        /// deprecated BombInstallCount
        /// </summary>
        BombInstallCount, // 폭탄 설치 갯수 증가

        BasicAttack_ChargerTime, // 탄창 충전 속도
        SpeedUp, // 이동 속도 고정 이동속도, 아이템에 쓰일 버프.
        SkillCooldown, // 스킬 쿨타임
        Shield, // 방어막
        RecoveryHp,     // 회복
        
        /// <summary>
        /// deprecated Revival
        /// </summary>
        Revival, // 부활
        Stun,   //스턴
        SpeedSlowByPer, // 쿨타임용 이동속도.
        BurnDotDamage,  // 화상
        PoisonDotDamage,// 독.
        Invincible,     // 무적
        PowerUpGem, // 파워업 잼
        Bloodsucking,//흡혈
        // 4bit로 0~15까지 사용
        MaxBuffType = 1 << MaxBuffTypeBits,
        MaxBuffTypeBits = 4,
    }

    public enum StatusType
    {
        None = 0,
        Damage,
        Health,
        CharacterSpeed,
        CharacterSkillCooldown,
        CharacterDamage,
    }

    public enum AddStatusType
    {
        None = 0,
        Absolute,
        Relative,
    }


}
