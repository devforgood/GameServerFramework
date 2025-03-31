local bt = require("behavior_tree")

-- 트리 정의 (각 행동은 C++에서 처리)
bt.root:addChildren {
    bt.Sequence:new("Combat") :addChildren {
        bt.Action:new("Attack", function() return executeAction("Attack") end),
        bt.Action:new("Defend", function() return executeAction("Defend") end)
    },
    bt.Selector:new("Idle") :addChildren {
        bt.Action:new("Patrol", function() return executeAction("Patrol") end),
        bt.Action:new("LookAround", function() return executeAction("LookAround") end)
    }
}

-- 행동 실행 (C++에서 함수 호출)
function executeAction(actionName)
    local cppFunction = _G[actionName] -- C++에서 제공하는 함수 실행
    if cppFunction then
        return cppFunction()
    else
        return "FAILURE"
    end
end

-- 트리 실행 (C++에서 호출)
function runTree()
    return bt.root:run()
end

-- 트리 출력
bt.printTree()