local bt = require("behavior_tree")

-- 트리 정의 (각 행동은 C++에서 처리)
bt.root:addChildren {
    bt.createSequence("Combat", {
        bt.createAction("Attack"),
        bt.createAction("Defend")
    }),
    bt.createSelector("Idle", {
        bt.createAction("Patrol"),
        bt.createAction("LookAround")
    })
}

-- 트리 실행 (C++에서 호출)
function runTree(monster)
    return bt.root:run(monster)
end

-- 트리 출력
bt.printTree()
