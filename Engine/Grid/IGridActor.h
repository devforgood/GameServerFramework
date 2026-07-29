#ifndef IGRID_ACTOR_H
#define IGRID_ACTOR_H

class IGridActor {
public:
    virtual ~IGridActor() = default; // 가상 소멸자
	virtual bool IsCharacter() const = 0; // 순수 가상 함수
	virtual void SetGridX(int gridX) = 0; // 순수 가상 함수
	virtual void SetGridY(int gridY) = 0; // 순수 가상 함수
	virtual int GetGridX() const = 0; // 순수 가상 함수
	virtual int GetGridY() const = 0; // 순수 가상 함수
	virtual float GetVector2X() const = 0; // 순수 가상 함수
	virtual float GetVector2Y() const = 0; // 순수 가상 함수
	// 셀 안에서 자신이 놓인 위치(vector 인덱스). 셀 이동 시 O(1) 로 빼내기 위한 값으로,
	// GridManager 만 쓰고 다른 곳에서 의미를 갖지 않는다.
	virtual void SetGridSlot(int slot) = 0;
	virtual int GetGridSlot() const = 0;
	virtual int GetActorId() const = 0; // 순수 가상 함수
	virtual void DecrementHealth(int amount) = 0; // 순수 가상 함수 - 임시 주석 처리
	virtual void SetLastAttacker(int attacker_actor_id) = 0; // 마지막 공격자(킬러) 액터 ID 기록
};

#endif // IGRID_ACTOR_H