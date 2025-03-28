local bt = require("behavior_tree")

-- 트리 정의 (각 행동은 C++에서 처리)
bt.root:addChildren {
    bt.Sequence:new("Combat") :addChildren {
        bt.Action:new("Attack"),
        bt.Action:new("Defend")
    },
    bt.Selector:new("Idle") :addChildren {
        bt.Action:new("Patrol"),
        bt.Action:new("LookAround")
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