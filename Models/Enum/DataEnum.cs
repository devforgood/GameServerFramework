using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public enum HistoryLogAction
    {
        None,
        GainPlayPoint,
        UsePlayPoint,
        GainAccountBattleScore,
        UseAccountBattleScore,
        GainCharacterBattleScore,
        UseCharacterBattleScore,
        Login,
        GainItem,
        UseItem,
        StartPlay,
        TryStartPlay, //플레이 횟수와는 관계 없이 매칭을 시도한 횟수를 카운트
    }

    public enum HistoryLogReason
    {
        None,
        GameResultReward,
        MissionReward,
        Shop,
        Debug,
        Gacha,
        MissionRewardBase,
        SelectCharacter,
        GameEvent,
        UpgradePowerLevel,
        AdvertisementReward,
    }

    /// <summary>
    /// 시즌 주기
    /// </summary>
    public enum SeasonPeriod
    {
        None,
        Infinity, // 무기한
        Hourly, // 매시간
        Daily, // 매일
        Weekly, // 매주
        Monthly, // 매월
    }

    /// <summary>
    /// 기록 갱신 유형
    /// </summary>
    public enum ScoreUpdateType
    {
        None,
        ScoreAccumulate, // 점수 누적
        ScoreBast, // 가장 좋은 점수일때 갱신
        ScoreLatest, // 최근 점수로 매번 갱신
    }

    public enum MailState
    {
        None,
        Send,
        Read,
        Delete,
        Expiry,
    }


}
