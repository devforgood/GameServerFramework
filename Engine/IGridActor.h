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
	virtual int GetAgentID() const = 0; // 순수 가상 함수
	virtual void DecrementHealth(int amount) = 0; // 순수 가상 함수 - 임시 주석 처리
	virtual void SetLastAttackerPlayerId(long player_id) = 0; // 마지막 공격자(킬러) 플레이어 ID 기록
};

#endif // IGRID_ACTOR_H