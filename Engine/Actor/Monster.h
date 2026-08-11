#pragma once
#include "Actor.h"
#include "LuaObject.h"
#include "SkillSet.h"

#include <string>

namespace BT
{
	class BehaviorTree;
	class Tree;
}

class Action_Patrol;
class ActionPatrol;
class Vector3;

class Monster : public Actor, public LuaObject<Monster>
{
public:
	// 틱에 사용할 BT 프레임워크. 두 트리는 동일한 로직을 수행하므로 이 값만 바꿔
	// 프레임워크 오버헤드를 비교할 수 있다(Benchmark/PERFORMANCE.md).
	// 선택된 백엔드의 트리 하나만 Init 에서 생성되므로, 바꾸려면 몬스터 스폰 전에 설정해야 한다.
	enum class BTBackend
	{
		BTCpp,    // behaviortree_cpp (GameData/Monster.xml). BT 디버그 뷰어를 지원한다.
		CodeBase, // ../BehaviorTree (인하우스, MonsterCodeBaseBT) — 기본값. 틱 비용이 낮다.
	};
	static BTBackend btBackend_;

	// 몬스터 근접 공격 스킬 id(skill.json, monster_only 데이터 전용 스킬).
	// 사거리/각도/데미지/쿨다운 튜닝은 전부 데이터에서 한다.
	static constexpr int kMeleeSkillId = 3;

private:
	BT::BehaviorTree * bt_;
	float spawnPos_[3];
	BT::Tree* tree_;
	bool deadNotified_ = false; // 사망 이벤트 중복 발행 방지
	SkillSet skillSet_;         // 플레이어와 동일한 스킬 파이프라인(TryCast)을 사용한다

	// monster.json 의 종류 id. 스폰 마커(monster_spawn.monster_id)가 정한다.
	// 사망 이벤트에 실려 나가서 퀘스트의 kill 목표가 종류별로 셀 수 있게 한다.
	int dataId_ = 0;

public:
	int targetAgentId_;
	std::string name_;

public:

	Monster(Map* map);
	virtual ~Monster();
	virtual void Update(float dt);
	virtual bool Init(Vector3& pos) override;


	int AttackRange();
	int Attack();
	int Resume();

	SkillSet& GetSkillSet() { return skillSet_; }

	void SetDataId(int dataId) { dataId_ = dataId; }
	int GetDataId() const { return dataId_; }

	// 사망 시 킬한 플레이어에게 EventActorDead 이벤트를 발행한다(최초 1회).
	void NotifyKilledBy();

	static void registerLuaFunctionAll();

	friend Action_Patrol;
	friend ActionPatrol;
};

