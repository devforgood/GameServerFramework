using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

public enum eCharacterStateType
{

    eIDEL           = 0,            //대기상태
    eTRACETARGET,                   //AI 전환시 상대 타겟 추적
    eRUN,                           //이동상태
    eATTACK,                        //어택상태
    eSKILL,                         //스킬사용상태
    eBEATTACKED,                    //피격상태
    eDIE,                           //죽었을때
    eNONE    
}
public enum eInGameManagerState
{
    eINGAMEPREPARE          = 0,    //인게임 진입 준비상태
    eINGAMEPLAY,                    //인게임 플레이 상태  
    eINGAMEEND,                     //인게임 종료상태
    eNONE
}

public enum eJoyStickType
{
    eMOVESTICK              = 0,    //이동 스틱
    eBOMBSTICK,                     //폭탄 스틱
    eSKILLSTICK,                    //스킬 스틱
    eNONE                   
}

public enum eSkillState
{
    eNONE,
    eGUIDE,
    eSHOOT,
}

public enum eExplosionType
{
    eNONE,
    eCIRCLE,
    eRECTANGLE,
}

public enum eSkillKind
{
    eBasicAttack,           // 기본 공격
    eSpecialAttack,         // 필살기
    ePassive,               // 패시브
    eNONE,
}

public enum eDamageType
{
    eCharacter = 1,     // 케릭터 대미지.
    eFixed,             // 고정 대미지.
}

public enum eCharacterKind
{
    DEVILMAN  = 1,
    JEUS,
    DRACULA,
    CATHOOD,
    MARIA,
    MONKEYKING,
    MAX,
}

public enum eSceneType
{
    NONE,
    LOGOSCENE,
    LOADINGSCENE,
    LOBBYSCENE,
    INGAMESCENE,
    PANEL,
}

public enum eAlertType
{
    CASTLEHP_WARNING,
    CASTLE_ITEMALARM,
    NONE,
}

public enum Depth
{
    /// <summary>
    /// 일반 패널들
    /// </summary>
    Panel = 0,

    /// <summary>
    /// 상시 메뉴
    /// </summary>
    CommonMenu = 1000,

    /// <summary>
    /// 팝업
    /// </summary>
    Popup = 2000,

    /// <summary>
    /// 알림
    /// </summary>
    Notification = 7000,

    /// <summary>
    /// 토스트
    /// </summary>
    Toast = 8000,

    /// <summary>
    /// 페이드 인/아웃
    /// </summary>
    Fade = 9000,

    /// <summary>
    /// 키 입력 방지
    /// </summary>
    BlockInput = 9001,
}

/// <summary>
/// 스크롤 업데이트 방향
/// </summary>
public enum AddDirectionType
{
    Start = -1,
    None = 0,
    End = 1,
    Set = 2,
}

public enum UIState
{
    None,
    Init,
    LoadFirstPanel,
}

/// <summary>
/// UI 상단 순서.
/// </summary>
public enum eMoneyType
{
    None,
    Coin,
    Gem,
    BattleCoin,
    Medal,
    UpgradeStone,
    Max,
}

public enum ECharacterSortingType
{
    /// <summary>
    /// Id 오름차순
    /// </summary>
    Id,
    /// <summary>
    /// 희귀도 오름차순
    /// </summary>
    RareAscending,
    /// <summary>
    /// 희귀도 내림차순
    /// </summary>
    RareDescending,
    /// <summary>
    /// 공격력 내림차순
    /// </summary>
    AttackDescending,
    /// <summary>
    /// HP 내림차순
    /// </summary>
    HpDescending,



    /// <summary>
    /// 파워레벨 오름차순
    /// </summary>
    PowerLavelAscending,

    /// <summary>
    /// 파워레벨 내림차순
    /// </summary>
    PowerLavelDescending,

    /// <summary>
    /// 히어로 연합
    /// </summary>
    HeorUnion,


    /// <summary>
    /// 빌런연합 
    /// </summary>
    VillainUnion,
}
public enum ePOWERPRICETYPE
{
    /// <summary>
    /// 코인
    /// </summary>
    Coin = 1,
    /// <summary>
    /// 각성석
    /// </summary>
    UpgradeStone    = 5,
}