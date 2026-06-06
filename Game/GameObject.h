#pragma once
#include <iostream>
#include <vector>
#include <unordered_map>
#include <memory>
#include <typeinfo>  
#include <typeindex>
#include <string>
#include "syncnet_generated.h"
#include "Component.h"


class GameObject {

private:
	std::string name;
	// 고유한 타입을 키값으로 사용하기 위해 std::type_index 사용
	// 메모리 자동 관리를 위해 std::unique_ptr로 컴포넌트 소유
	std::unordered_map<std::type_index, std::unique_ptr<Component>> components;

public:
	GameObject() 
	{
	}
	virtual ~GameObject() = default; // 반드시 virtual 소멸자를 추가

	virtual void update(float dt) 
    {
        for (auto& const pair : components) {
            pair.second->Update();
        }
    };


    GameObject(std::string objName) : name(objName) {}

    std::string GetName() const { return name; }

    // 유니티의 AddComponent<T>() 모방
    template <typename T, typename... Args>
    T* AddComponent(Args&&... args) {
        std::type_index typeIdx = typeid(T);

        if (components.find(typeIdx) != components.end()) {
            std::cout << "[경고] " << name << "에 이미 해당 컴포넌트가 존재합니다.\n";
            return static_cast<T*>(components[typeIdx].get());
        }

        // 컴포넌트 생성 및 소유권 이전
        auto component = std::make_unique<T>(std::forward<Args>(args)...);
        component->gameObject = this;

        T* rawPtr = component.get();
        components[typeIdx] = std::move(component);

        rawPtr->Start(); // 생성 후 초기화 호출
        return rawPtr;
    }

    // 유니티의 GetComponent<T>() 모방
    template <typename T>
    T* GetComponent() {
        std::type_index typeIdx = typeid(T);
        auto it = components.find(typeIdx);

        if (it != components.end()) {
            return static_cast<T*>(it->second.get());
        }
        return nullptr;
    }
};
