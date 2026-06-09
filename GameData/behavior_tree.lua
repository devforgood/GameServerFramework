-- behavior_tree.lua

BehaviorTree = {}

-- 기본 노드 구조
BehaviorTree.Node = {}
BehaviorTree.Node.__index = BehaviorTree.Node

function BehaviorTree.Node:new(type, name)
    return setmetatable({type = type, name = name, children = {}}, self)
end

function BehaviorTree.Node:addChildren(children)
    for _, child in ipairs(children) do
        table.insert(self.children, child)
    end
    return self  -- 체이닝(Chaining) 지원
end

function BehaviorTree.Node:toString(indent)
    indent = indent or 0
    local prefix = string.rep("  ", indent) -- 들여쓰기
    local result = prefix .. "- [" .. self.type .. "] " .. self.name .. "\n"

    for _, child in ipairs(self.children) do
        result = result .. child:toString(indent + 1)
    end

    return result
end

-- Selector 노드
BehaviorTree.Selector = setmetatable({}, {__index = BehaviorTree.Node})
function BehaviorTree.Selector:new(name)
    local obj = BehaviorTree.Node:new("Selector", name)
    setmetatable(obj, {__index = self})
    return obj
end

function BehaviorTree.Selector:run(monster)
    for _, child in ipairs(self.children) do
        local status = child:run(monster)
        if status == "SUCCESS" then
            return "SUCCESS"
        end
    end
    return "FAILURE"
end

-- Sequence 노드
BehaviorTree.Sequence = setmetatable({}, {__index = BehaviorTree.Node})
function BehaviorTree.Sequence:new(name)
    local obj = BehaviorTree.Node:new("Sequence", name)
    setmetatable(obj, {__index = self})
    return obj
end

function BehaviorTree.Sequence:run(monster)
    for _, child in ipairs(self.children) do
        local status = child:run(monster)
        if status == "FAILURE" then
            return "FAILURE"
        end
    end
    return "SUCCESS"
end

-- Action 노드
BehaviorTree.Action = setmetatable({}, {__index = BehaviorTree.Node})
function BehaviorTree.Action:new(name, func)
    local obj = BehaviorTree.Node:new("Action", name)
    obj.func = func or function() return "SUCCESS" end
    setmetatable(obj, {__index = self})
    return obj
end

function BehaviorTree.Action:run(monster)
    return self.func(monster)
end

-- 헬퍼 함수: Action 노드를 생성하는 함수
function BehaviorTree.createAction(name)
    return BehaviorTree.Action:new(name, function(monster) return BehaviorTree.executeAction(name, monster) end)
end

-- 헬퍼 함수: Sequence 노드를 생성하는 함수
function BehaviorTree.createSequence(name, actions)
    local sequence = BehaviorTree.Sequence:new(name)
    sequence:addChildren(actions)
    return sequence
end

-- 헬퍼 함수: Selector 노드를 생성하는 함수
function BehaviorTree.createSelector(name, actions)
    local selector = BehaviorTree.Selector:new(name)
    selector:addChildren(actions)
    return selector
end

-- 행동 실행 (C++에서 함수 호출)
function BehaviorTree.executeAction(actionName, monster)
    local cppFunction = _G[actionName] -- C++에서 제공하는 함수 실행
    if cppFunction then
        return cppFunction(monster)
    else
        return "FAILURE"
    end
end

-- 트리의 루트 노드 생성
BehaviorTree.root = BehaviorTree.Selector:new("Root")

-- 트리 출력 함수 추가
function BehaviorTree.printTree()
    print(BehaviorTree.root:toString())
end

return BehaviorTree
