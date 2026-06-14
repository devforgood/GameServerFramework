-- gamemode_field.lua
-- Field 모드 진행 로직.
-- 상시 오픈 월드: 시간 제한 없음, PvP 허용, 사망 시 리스폰.
--
-- C++ GameMode가 스크립트를 로드하면 이 파일이 반환하는 테이블을 보관하고,
-- 라이프사이클 시점마다 아래 함수들을 호출한다. (없는 함수는 호출하지 않는다)
--   on_start(mode)        : 모드 시작 시 1회
--   on_update(mode, dt)   : 매 틱
--   on_player_join(mode, player)
--   on_player_dead(mode, player)
--   check_end(mode)       : true 반환 시 모드 종료
--   on_end(mode)          : 모드 종료 시 1회
--
-- `mode`는 C++ GameMode를 가리키는 핸들이며, 호스트가 등록한 전역 함수
-- (GM_* )로 상태를 조회/조작한다.

local Field = {}

function Field.on_start(mode)
    print("[Field] start")
end

function Field.on_player_join(mode, player)
    -- 필드는 입장 제한이 없다. 환영 처리만.
    print("[Field] player joined")
end

function Field.on_player_dead(mode, player)
    -- respawn_enabled = true: 일정 시간 후 부활시킨다.
    GM_SchedulePlayerRespawn(mode, player, GM_GetRespawnTime(mode))
end

function Field.on_update(mode, dt)
    -- 상시 모드라 별도 진행 처리 없음.
end

function Field.check_end(mode)
    -- 필드는 종료 조건이 없다(end_condition = "none").
    return false
end

function Field.on_end(mode)
    print("[Field] end")
end

return Field
