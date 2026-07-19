-- gamemode_raid.lua
-- Raid 모드 진행 로직.
-- 인스턴스 던전: 제한 시간(1800s) 내 보스 처치가 목표.
-- 시간 초과 또는 전멸 시 실패, 보스 처치 시 성공으로 종료.
--
-- 라이프사이클 계약은 gamemode_field.lua 상단 주석 참고.

local Raid = {}

function Raid.on_start(mode)
    -- 제한 시간 타이머 시작, 보스 스폰.
    GM_StartTimer(mode, GM_GetTimeLimit(mode))
    GM_SpawnBoss(mode, GM_GetBossId(mode))
    print("[Raid] start, time_limit=" .. tostring(GM_GetTimeLimit(mode)))
end

function Raid.on_player_join(mode, player)
    print("[Raid] player joined")
end

function Raid.on_player_dead(mode, player)
    -- respawn_enabled = false: 부활 없음. 전멸 여부를 확인한다.
    if GM_GetAlivePlayerCount(mode) == 0 then
        GM_SetResult(mode, "fail_wipe")
        GM_RequestEnd(mode)
    end
end

function Raid.on_update(mode, dt)
    -- 제한 시간 초과 검사.
    if GM_GetRemainingTime(mode) <= 0 then
        GM_SetResult(mode, "fail_timeout")
        GM_RequestEnd(mode)
    end
end

function Raid.check_end(mode)
    -- 보스 처치 성공 종료.
    if GM_IsBossDead(mode) then
        GM_SetResult(mode, "clear")
        return true
    end
    -- on_update / on_player_dead 에서 RequestEnd 가 호출됐는지 확인.
    return GM_IsEndRequested(mode)
end

function Raid.on_end(mode)
    print("[Raid] end, result=" .. tostring(GM_GetResult(mode)))
    -- 성공 시 보상 지급.
    if GM_GetResult(mode) == "clear" then
        GM_GrantRewards(mode)
    end
end

return Raid
