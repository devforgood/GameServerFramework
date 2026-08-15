#ifndef IGRID_ACTOR_H
#define IGRID_ACTOR_H

class IGridActor {
public:
    virtual ~IGridActor() = default; // 가상 소멸자
	virtual bool IsCharacter() const = 0; // 순수 가상 함수

	// 몬스터가 사냥할 대상인가. 그리드는 이 값으로 셀 버킷을 나누고, 몬스터의 적 탐지는
	// 그 버킷만 훑는다. IsCharacter 와 나눠 둔 이유는 둘의 뜻이 다르기 때문이다 —
	// IsCharacter 는 "Character* 로 캐스팅해도 되는가"이고(플레이어 id 조회 등에서 쓴다),
	// 이쪽은 "몬스터의 표적이 되는가"다. 호위 NPC 는 후자만 참이다.
	virtual bool IsMonsterTarget() const = 0;
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