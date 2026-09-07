#pragma once
#include "Actor.h"
#include "LuaObject.h"
#include "MonsterAIProfile.h"
#include "MonsterBTRunner.h"
#include "SkillSet.h"

#include <string>

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
		CodeBase, // ../BehaviorTree (인하우스, MonsterCodeBaseBT). 마리마다 노드 객체를 만든다.
		Ecs,      // 트리를 컴파일한 결정표 + 개체별 컴포넌트 (MonsterAISystem) — 기본값.
	};
	static BTBackend btBackend_;

	// 기본 성향(Aggressive)이 쓰는 근접 공격 스킬과 판정 거리.
	// 성향마다 달라지므로 실제 값은 공유 표(MonsterAIProfile.h)가 갖고, 여기 있는 것은
	// 그 표의 기본 항목을 가리키는 별칭이다 — 성향을 모르는 코드(AttackRange 등)가 쓴다.
	static constexpr int kMeleeSkillId = monsterai::kMeleeSkillId;
	static constexpr int kAttackRange = monsterai::kMeleeAttackRange;

private:
	MonsterBTRunner brain_;     // 선택된 백엔드의 BT 트리(생성/틱/해제를 위임한다)
	float spawnPos_[3];
	bool deadNotified_ = false; // 사망 이벤트 중복 발행 방지
	SkillSet skillSet_;         // 플레이어와 동일한 스킬 파이프라인(TryCast)을 사용한다

	// monster.json 의 종류 id. 스폰 마커(monster_spawn.monster_id)가 정한다.
	// 사망 이벤트에 실려 나가서 퀘스트의 kill 목표가 종류별로 셀 수 있게 한다.
	int dataId_ = 0;

	// monster.json 의 "ai" 필드가 정하는 성향과, 지금 서 있는 공격 페이즈.
	// 세 백엔드가 공유하는 상태다 — 성향을 읽는 노드/패스가 전부 여기를 본다.
	monsterai::AIProfile aiProfile_ = monsterai::AIProfile::Aggressive;
	uint8_t combatPhase_ = 0;

public:
	int targetActorId_;
	std::string name_;

public:

	Monster(Map* map);
	virtual ~Monster();
	virtual void Update(float dt);
	virtual bool Init(Vector3& pos) override;


	// 현재 공격 패턴의 사거리 안에 대상이 있는지. 있으면 대상 actor id, 없으면 -1.
	int AttackRange();

	// 현재 공격 패턴의 스킬로 추격 대상을 친다.
	int Attack();

	// 스킬을 지정해 친다. ECS 백엔드는 패턴을 컴포넌트에 캐시해 두고 이쪽으로 부른다
	// (패스 안에서 Monster 를 한 번 더 만지지 않으려고).
	int Attack(int skillId);

	int Resume();

	// 체력이 바뀌면 AI 를 깨운다. ECS 백엔드는 배회 중 몇 틱에 한 번만 사고하므로,
	// 그냥 두면 맞아 죽은 몬스터가 다음 사고 차례까지 살아 있는 것처럼 보인다.
	void SetHealth(int health) override;
	void DecrementHealth(int amount) override;

	SkillSet& GetSkillSet() { return skillSet_; }

	// 배회(patrol) 의 중심점. 스폰 시 네비메시에 스냅된 위치다.
	const float* GetSpawnPos() const { return spawnPos_; }

	monsterai::AIProfile GetAIProfile() const { return aiProfile_; }
	uint8_t GetCombatPhase() const { return combatPhase_; }

	// 지금 체력에 해당하는 공격 패턴(스킬 + 사거리).
	const monsterai::AttackPattern& GetAttackPattern() const;

	// 체력 구간에 맞는 페이즈로 맞춘다. 바뀌었으면 true.
	// 세 백엔드가 각자의 방식으로 매 사고마다 부른다 — BT 는 ActionUpdateCombatPhase
	// 노드가, ECS 는 EvaluatePhase 패스가 부른다.
	bool UpdateCombatPhase();

	// 피격. 반격 대상을 잡고(먼저 공격하지 않는 성향이 교전에 들어가는 유일한 경로다)
	// AI 를 깨운다. 데미지 적용 경로(combat::ApplyDamage)가 부르는 것과 같은 순서로,
	// 마지막 공격자가 새겨진 뒤에 불려야 한다.
	void OnDamaged(int attackerActorId);

	// 종류 id 를 새기면서 그 종류의 전투 스탯(monster.json 의 hp/attack/defense)을 적용한다.
	// 예전에는 종류만 새기고 스탯은 쓰지 않아, 슬라임과 고대 드래곤이 똑같이 체력 100 이었다.
	void SetDataId(int dataId);
	int GetDataId() const { return dataId_; }

	// 처치 보상 경험치(monster.json 의 exp). 데이터가 없으면 0.
	int GetRewardExp() const;

	// 사망 시 킬한 플레이어에게 EventActorDead 이벤트를 발행한다(최초 1회).
	void NotifyKilledBy();

	static void registerLuaFunctionAll();

private:
	// ECS 백엔드에서만 의미가 있다(다른 백엔드는 매 틱 사고하므로 깨울 것이 없다).
	void WakeAI();

	// 성향을 적용한다: 그 성향이 페이즈마다 쓰는 스킬을 등록하고 AI 슬롯에 반영한다.
	void ApplyAIProfile(monsterai::AIProfile profile);
};

