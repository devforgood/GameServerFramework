#pragma once
#include "Actor.h"
#include "SkillSet.h"

class Vector3;

class Character : public Actor
{
private:
	long playerId_;
	SkillSet skillSet_;

public:
	Character(Map* map);
	virtual ~Character();



	void SetPlayerId(long player_id)
	{
		playerId_ = player_id;
	}
	long GetPlayerId() const { return playerId_; }

	// 네트워크 메시지를 CastContext 로 변환해 스킬 파이프라인에 넘긴다.
	// Success 외에는 상태 변화가 없으므로 호출 측은 Success 일 때만 브로드캐스트한다.
	CastResult use_skill(const syncnet::UseSkill* msg);

	// AI/테스트 등 네트워크를 거치지 않는 시전 경로.
	SkillSet& GetSkillSet() { return skillSet_; }

	virtual void Update(float deltaTime) override;
	virtual bool PreCreate(std::shared_ptr<Player> player) override;
	virtual bool Init(Vector3& pos) override;
	virtual bool PostCreate(std::shared_ptr<Player> player, std::shared_ptr<GameObject> game_object) override;
};
