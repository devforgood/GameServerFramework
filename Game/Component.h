#pragma once


class GameObject;

// 1. 모든 컴포넌트의 최상위 추상 클래스
class Component {
public:
    GameObject* gameObject = nullptr; // 자신이 속한 오브젝트 역참조

    virtual ~Component() = default;
    virtual void Start() {}
    virtual void Update() {}
};