local bt = require("behavior_tree")

-- 트리 정의 (각 행동은 C++에서 처리)
bt.root:addChildren {
    bt.Sequence:new("Combat") :addChildren {
        bt.Action:new("Attack", function(monster) return executeAction("Attack", monster) end),
        bt.Action:new("Defend", function(monster) return executeAction("Defend", monster) end)
    },
    bt.Selector:new("Idle") :addChildren {
        bt.Action:new("Patrol", function(monster) return executeAction("Patrol", monster) end),
        bt.Action:new("LookAround", function(monster) return executeAction("LookAround", monster) end)
    }
}

-- 행동 실행 (C++에서 함수 호출)
function executeAction(actionName, monster)
    local cppFunction = _G[actionName] -- C++에서 제공하는 함수 실행
    if cppFunction then
        return cppFunction(monster)
    else
        return "FAILURE"
    end
end

-- 트리 실행 (C++에서 호출)
function runTree(monster)
    return bt.root:run(monster)
end

-- 트리 출력
bt.printTree()