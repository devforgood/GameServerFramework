#ifndef IGRID_ACTOR_H
#define IGRID_ACTOR_H

class IGridActor {
public:
    virtual ~IGridActor() = default; // 가상 소멸자
	virtual bool isCharacter() const = 0; // 순수 가상 함수
	virtual void setGridX(int gridX) = 0; // 순수 가상 함수
	virtual void setGridY(int gridY) = 0; // 순수 가상 함수
	virtual int getGridX() const = 0; // 순수 가상 함수
	virtual int getGridY() const = 0; // 순수 가상 함수
	virtual float getVector2X() const = 0; // 순수 가상 함수
	virtual float getVector2Y() const = 0; // 순수 가상 함수
	virtual int getAgentID() const = 0; // 순수 가상 함수
	virtual void decrementHealth(int amount) = 0; // 순수 가상 함수 - 임시 주석 처리
	virtual void setLastAttackerPlayerId(long player_id) = 0; // 마지막 공격자(킬러) 플레이어 ID 기록
};

#endif // IGRID_ACTOR_H