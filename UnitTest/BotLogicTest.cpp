#include <gtest/gtest.h>

#include <string>
#include <vector>

#include "BotBTNodes.h"
#include "BotBehaviorTree.h"
#include "BotMetrics.h"
#include "BotPacket.h"
#include "WorldView.h"

#include "../BehaviorTree/BehaviorTree.h"

//---------------------------------------------------------------------------------------
// 봇 부하 테스트 클라이언트(Bot 프로젝트)의 순수 로직 검증.
//
// 소켓이 필요한 부분(BotSession/BotWorker)은 여기서 다루지 않는다. 대신 봇이 서버 응답을
// 어떻게 접어 넣고(WorldView), 그 상태에서 어떤 명령을 내리는지(BT)를 가짜 행동 구현으로
// 고정한다 — 시나리오가 조용히 망가지면 부하 수치는 나오는데 아무도 사냥하지 않는
// 상태가 되어 알아채기 어렵기 때문이다.
//---------------------------------------------------------------------------------------

namespace
{
	using namespace bot;

	struct ActorUpdate
	{
		int actor_id = 0;
		syncnet::GameObjectType type = syncnet::GameObjectType::GameObjectType_Monster;
		float x = 0.0f;
		float y = 0.0f;
		float z = 0.0f;
		bool has_state = false;
		syncnet::AIState state = syncnet::AIState::AIState_Patrol;
		bool has_health = false;
		int health = 0;
	};

	// 서버가 보내는 UpdateActorNotify 를 그대로 흉내 낸다.
	// 서버는 변경된 필드만 채우므로(state/health 가 빠질 수 있다) 그 성질을 재현한다.
	class NotifyBuilder
	{
	public:
		NotifyBuilder& Add(const ActorUpdate& update)
		{
			updates_.push_back(update);
			return *this;
		}

		NotifyBuilder& Remove(int actor_id)
		{
			removed_.push_back(actor_id);
			return *this;
		}

		// 반환된 포인터는 이 객체가 살아 있는 동안만 유효하다.
		const syncnet::UpdateActorNotify* Build()
		{
			std::vector<flatbuffers::Offset<syncnet::ActorInfo>> actors;
			for (const ActorUpdate& update : updates_)
			{
				const syncnet::Vec3 pos(update.x, update.y, update.z);
				const syncnet::ActorState state(update.state);
				const syncnet::ActorHealth health(update.health);

				actors.push_back(syncnet::CreateActorInfo(builder_, update.actor_id, &pos,
					update.type,
					update.has_state ? &state : nullptr,
					update.has_health ? &health : nullptr,
					false));
			}

			auto notify = syncnet::CreateUpdateActorNotifyDirect(builder_, &actors, nullptr,
				removed_.empty() ? nullptr : &removed_);
			builder_.Finish(notify);

			return flatbuffers::GetRoot<syncnet::UpdateActorNotify>(builder_.GetBufferPointer());
		}

	private:
		flatbuffers::FlatBufferBuilder builder_{ 1024 };
		std::vector<ActorUpdate> updates_;
		std::vector<int32_t> removed_;
	};

	// BT 가 무엇을 시켰는지만 기록하는 가짜 행동 구현.
	struct RecordingActions : public BotActions
	{
		struct MoveCommand { Vec3 pos; };
		struct AttackCommand { int target_id; Vec3 pos; };

		std::vector<MoveCommand> moves;
		std::vector<AttackCommand> attacks;

		void MoveTo(const Vec3& pos) override { moves.push_back({ pos }); }
		void Attack(int target_actor_id, const Vec3& target_pos) override
		{
			attacks.push_back({ target_actor_id, target_pos });
		}
	};

	ActorUpdate MakeMonster(int actor_id, float x, float z, int health = 100)
	{
		ActorUpdate update;
		update.actor_id = actor_id;
		update.type = syncnet::GameObjectType::GameObjectType_Monster;
		update.x = x;
		update.z = z;
		update.has_state = true;
		update.state = syncnet::AIState::AIState_Patrol;
		update.has_health = true;
		update.health = health;
		return update;
	}

	ActorUpdate MakeCharacter(int actor_id, float x, float z, int health = 100)
	{
		ActorUpdate update = MakeMonster(actor_id, x, z, health);
		update.type = syncnet::GameObjectType::GameObjectType_Character;
		return update;
	}
}

//--- WorldView: 부분 갱신 병합 -------------------------------------------------------

TEST(BotWorldViewTest, KeepsPreviousFieldsWhenServerOmitsThem)
{
	WorldView view;

	NotifyBuilder first;
	first.Add(MakeMonster(10, 5.0f, 5.0f, 80));
	view.Apply(first.Build(), 1.0, WorldView::kNoSelf);

	// 두 번째 갱신은 위치만 바뀌었다 — 서버는 state/health 를 싣지 않는다.
	ActorUpdate moved;
	moved.actor_id = 10;
	moved.x = 6.0f;
	moved.z = 5.0f;

	NotifyBuilder second;
	second.Add(moved);
	view.Apply(second.Build(), 2.0, WorldView::kNoSelf);

	const ActorSnapshot* snapshot = view.Find(10);
	ASSERT_NE(snapshot, nullptr);
	EXPECT_FLOAT_EQ(snapshot->pos.x, 6.0f);
	EXPECT_TRUE(snapshot->has_health);
	EXPECT_EQ(snapshot->health, 80);
	EXPECT_FALSE(snapshot->IsDead());
}

TEST(BotWorldViewTest, RemovesActorsThatLeaveTheAreaOfInterest)
{
	WorldView view;

	NotifyBuilder first;
	first.Add(MakeMonster(10, 5.0f, 5.0f));
	first.Add(MakeMonster(11, 6.0f, 5.0f));
	view.Apply(first.Build(), 1.0, WorldView::kNoSelf);
	ASSERT_EQ(view.Size(), 2u);

	NotifyBuilder second;
	second.Remove(11);
	const WorldView::ApplyResult result = view.Apply(second.Build(), 2.0, WorldView::kNoSelf);

	EXPECT_EQ(result.removed, 1);
	EXPECT_EQ(result.monster_deaths, 0);   // 시야 이탈은 처치가 아니다
	EXPECT_EQ(view.Size(), 1u);
	EXPECT_EQ(view.Find(11), nullptr);
}

TEST(BotWorldViewTest, CountsMonsterDeathOnceAndDetectsSelfDeath)
{
	WorldView view;

	NotifyBuilder first;
	first.Add(MakeMonster(10, 5.0f, 5.0f, 50));
	first.Add(MakeCharacter(99, 0.0f, 0.0f, 100));
	view.Apply(first.Build(), 1.0, 99);

	ActorUpdate dead_monster = MakeMonster(10, 5.0f, 5.0f, 0);
	dead_monster.state = syncnet::AIState::AIState_Dead;

	ActorUpdate dead_self = MakeCharacter(99, 0.0f, 0.0f, 0);
	dead_self.state = syncnet::AIState::AIState_Dead;

	NotifyBuilder second;
	second.Add(dead_monster).Add(dead_self);
	const WorldView::ApplyResult result = view.Apply(second.Build(), 2.0, 99);

	EXPECT_EQ(result.monster_deaths, 1);
	EXPECT_TRUE(result.self_died);

	// 같은 사망 상태가 다시 와도 두 번 세지 않는다.
	NotifyBuilder third;
	third.Add(dead_monster);
	const WorldView::ApplyResult again = view.Apply(third.Build(), 3.0, 99);
	EXPECT_EQ(again.monster_deaths, 0);
}

// actor id 0 은 유효한 값이다. 내비메시 crowd 의 첫 에이전트가 0 을 받으므로 맵마다
// 한 명은 0 을 배정받는다. 0 을 "없음" 으로 쓰면 그 봇은 자기 사망을 인식하지 못한다.
TEST(BotWorldViewTest, TreatsActorIdZeroAsAValidSelf)
{
	NotifyBuilder alive;
	alive.Add(MakeCharacter(0, 0.0f, 0.0f, 100));

	ActorUpdate dead = MakeCharacter(0, 0.0f, 0.0f, 0);
	dead.state = syncnet::AIState::AIState_Dead;

	WorldView mine;
	mine.Apply(alive.Build(), 1.0, 0);
	NotifyBuilder death;
	death.Add(dead);
	EXPECT_TRUE(mine.Apply(death.Build(), 2.0, 0).self_died);

	// 아직 캐릭터가 없는 봇은 kNoSelf 를 넘긴다 — 남의 캐릭터 사망을 내 것으로 보지 않는다.
	WorldView other;
	NotifyBuilder alive2;
	alive2.Add(MakeCharacter(0, 0.0f, 0.0f, 100));
	other.Apply(alive2.Build(), 1.0, WorldView::kNoSelf);
	NotifyBuilder death2;
	death2.Add(dead);
	EXPECT_FALSE(other.Apply(death2.Build(), 2.0, WorldView::kNoSelf).self_died);
}

TEST(BotWorldViewTest, FindsNearestLivingMonsterWithinRadius)
{
	WorldView view;

	ActorUpdate dead = MakeMonster(1, 1.0f, 0.0f, 0);
	dead.state = syncnet::AIState::AIState_Dead;

	NotifyBuilder notify;
	notify.Add(dead);                          // 가장 가깝지만 죽었다
	notify.Add(MakeMonster(2, 4.0f, 0.0f));    // 가장 가까운 생존 몬스터
	notify.Add(MakeMonster(3, 2.0f, 0.0f));    // 더 가깝지만 아래에서 반경 밖 검사에 쓴다
	notify.Add(MakeCharacter(4, 0.5f, 0.0f));  // 다른 플레이어는 대상이 아니다
	view.Apply(notify.Build(), 1.0, WorldView::kNoSelf);

	EXPECT_EQ(view.FindNearestMonster(Vec3(0.0f, 0.0f, 0.0f), 30.0f), 3);
	EXPECT_EQ(view.FindNearestMonster(Vec3(0.0f, 0.0f, 0.0f), 3.0f), 3);
	EXPECT_EQ(view.FindNearestMonster(Vec3(0.0f, 0.0f, 0.0f), 1.5f), 0);
}

//--- 패킷 프레이밍 -------------------------------------------------------------------

TEST(BotPacketTest, EncodesLengthPrefixedFrameThatParsesBack)
{
	const packet::Frame frame = packet::Login(7, "bot_000001", "", "");

	ASSERT_GT(frame.size(), packet::kHeaderLength);

	uint16_t body_length = 0;
	std::memcpy(&body_length, frame.data(), packet::kHeaderLength);
	EXPECT_EQ(frame.size(), packet::kHeaderLength + body_length);

	// 서버는 본문 512바이트를 넘는 요청을 프로토콜 위반으로 끊는다.
	EXPECT_LE(body_length, packet::kMaxRequestBodyLength);

	const syncnet::GameMessage* message =
		packet::ParseMessage(frame.data() + packet::kHeaderLength, body_length);
	ASSERT_NE(message, nullptr);
	EXPECT_EQ(message->id(), 7);
	ASSERT_EQ(message->msg_type(), syncnet::GameMessages::GameMessages_Login);

	const syncnet::Login* login = message->msg_as_Login();
	ASSERT_NE(login, nullptr);
	ASSERT_NE(login->userId(), nullptr);
	EXPECT_EQ(login->userId()->str(), "bot_000001");
	EXPECT_EQ(login->authToken(), nullptr);   // 빈 토큰은 필드를 넣지 않는다
}

TEST(BotPacketTest, ExtractsWholeFramesAndLeavesPartialBytes)
{
	packet::Frame first = packet::Ping(1, 1);
	packet::Frame second = packet::Ping(2, 2);

	std::vector<char> stream(first);
	// 두 번째 프레임은 절반만 도착한 상태로 둔다.
	stream.insert(stream.end(), second.begin(), second.begin() + second.size() / 2);

	std::vector<int> ids;
	size_t consumed = 0;
	const bool ok = packet::ExtractFrames(stream.data(), stream.size(),
		[&ids](const char* body, size_t size)
		{
			const syncnet::GameMessage* message = packet::ParseMessage(body, size);
			ASSERT_NE(message, nullptr);
			ids.push_back(message->id());
		}, consumed);

	EXPECT_TRUE(ok);
	EXPECT_EQ(consumed, first.size());
	ASSERT_EQ(ids.size(), 1u);
	EXPECT_EQ(ids[0], 1);

	// 나머지가 도착하면 두 번째도 꺼내진다.
	stream.insert(stream.end(), second.begin() + second.size() / 2, second.end());
	std::vector<char> rest(stream.begin() + consumed, stream.end());

	size_t consumed_rest = 0;
	packet::ExtractFrames(rest.data(), rest.size(),
		[&ids](const char* body, size_t size)
		{
			const syncnet::GameMessage* message = packet::ParseMessage(body, size);
			ASSERT_NE(message, nullptr);
			ids.push_back(message->id());
		}, consumed_rest);

	EXPECT_EQ(consumed_rest, second.size());
	ASSERT_EQ(ids.size(), 2u);
	EXPECT_EQ(ids[1], 2);
}

TEST(BotPacketTest, RejectsZeroLengthFrame)
{
	// 길이 0 은 정상 서버가 만들 수 없는 값이다. 그냥 두면 영원히 소비되지 않는
	// 바이트가 버퍼 앞을 막아 세션이 조용히 멈춘다.
	const char stream[] = { 0, 0, 1, 2 };

	int calls = 0;
	size_t consumed = 0;
	const bool ok = packet::ExtractFrames(stream, sizeof(stream),
		[&calls](const char*, size_t) { ++calls; }, consumed);

	EXPECT_FALSE(ok);
	EXPECT_EQ(calls, 0);
	EXPECT_EQ(consumed, 0u);
}

//--- 지연 분포 -----------------------------------------------------------------------

TEST(BotMetricsTest, ComputesPercentilesFromHistogram)
{
	LatencyHistogram histogram;
	for (int i = 1; i <= 100; ++i)
		histogram.Add(static_cast<uint32_t>(i));

	EXPECT_EQ(histogram.count, 100u);
	EXPECT_EQ(histogram.max_ms, 100u);
	EXPECT_DOUBLE_EQ(histogram.Average(), 50.5);
	EXPECT_EQ(histogram.Percentile(0.50), 50u);
	EXPECT_EQ(histogram.Percentile(0.95), 95u);
	EXPECT_EQ(histogram.Percentile(0.99), 99u);
}

TEST(BotMetricsTest, MergesHistogramsAndCountsOverflow)
{
	LatencyHistogram a;
	a.Add(10);
	a.Add(20);

	LatencyHistogram b;
	b.Add(5000);   // 버킷 범위를 넘는 표본

	a.Merge(b);

	EXPECT_EQ(a.count, 3u);
	EXPECT_EQ(a.overflow, 1u);
	EXPECT_EQ(a.max_ms, 5000u);
	EXPECT_EQ(a.Percentile(1.0), static_cast<uint32_t>(LatencyHistogram::kBucketCount));
}

//--- 시나리오(BT) --------------------------------------------------------------------

namespace
{
	// 봇 하나 분량의 BT 실행 환경. 트리는 블랙보드마다 새로 만든다.
	struct TreeFixture
	{
		RecordingActions actions;
		BotBlackboard blackboard;
		BT::BehaviorTree* tree = nullptr;

		TreeFixture()
		{
			blackboard.actions = &actions;
			blackboard.has_character = true;
			blackboard.self_actor_id = 99;
			blackboard.rng.seed(1234);
			tree = CreateBotTree(&blackboard);
		}

		~TreeFixture() { DestroyBotTree(tree); }

		void Tick(double now)
		{
			blackboard.now = now;
			tree->Tick();
		}
	};
}

TEST(BotBehaviorTreeTest, ChasesNearestMonsterThenAttacksInRange)
{
	TreeFixture fixture;

	NotifyBuilder far_away;
	far_away.Add(MakeMonster(10, 10.0f, 0.0f));
	fixture.blackboard.view.Apply(far_away.Build(), 1.0, 99);

	// 1틱: 대상 획득. 2틱: 사거리 밖이므로 이동 명령.
	fixture.Tick(1.0);
	EXPECT_EQ(fixture.blackboard.target_actor_id, 10);

	fixture.Tick(1.1);
	ASSERT_FALSE(fixture.actions.moves.empty());
	EXPECT_FLOAT_EQ(fixture.actions.moves.back().pos.x, 10.0f);
	EXPECT_TRUE(fixture.actions.attacks.empty());

	// 서버가 이동을 처리해 사거리 안으로 들어왔다.
	fixture.blackboard.self_pos = Vec3(9.0f, 0.0f, 0.0f);
	fixture.Tick(1.2);

	ASSERT_EQ(fixture.actions.attacks.size(), 1u);
	EXPECT_EQ(fixture.actions.attacks[0].target_id, 10);
	EXPECT_FLOAT_EQ(fixture.actions.attacks[0].pos.x, 10.0f);

	// 공격 간격 안에는 다시 쏘지 않는다.
	fixture.Tick(1.3);
	EXPECT_EQ(fixture.actions.attacks.size(), 1u);

	// 간격이 지나면 다시 쏜다.
	fixture.Tick(1.2 + fixture.blackboard.ai.attack_interval_ms / 1000.0);
	EXPECT_EQ(fixture.actions.attacks.size(), 2u);
}

TEST(BotBehaviorTreeTest, DropsTargetWhenItDiesAndPicksAnother)
{
	TreeFixture fixture;

	NotifyBuilder notify;
	notify.Add(MakeMonster(10, 1.0f, 0.0f));
	notify.Add(MakeMonster(11, 5.0f, 0.0f));
	fixture.blackboard.view.Apply(notify.Build(), 1.0, 99);

	fixture.Tick(1.0);
	ASSERT_EQ(fixture.blackboard.target_actor_id, 10);

	ActorUpdate dead = MakeMonster(10, 1.0f, 0.0f, 0);
	dead.state = syncnet::AIState::AIState_Dead;

	NotifyBuilder death;
	death.Add(dead);
	fixture.blackboard.view.Apply(death.Build(), 2.0, 99);

	fixture.Tick(2.0);
	EXPECT_EQ(fixture.blackboard.target_actor_id, 11);
}

// 회귀 테스트: 이 BT 는 Running 인 Sequence 를 다음 틱에 재초기화하지 않는다
// (앞의 조건 노드를 건너뛰고 진행 중이던 자식부터 다시 틱한다). 교전 분기가 Running 으로
// 잠긴 상태에서 대상이 죽으면, 액션 노드가 스스로 Failure 로 끝내지 않는 한 봇은
// 시체를 계속 때린다 — 그러면 부하는 걸리는데 사냥은 진행되지 않는다.
TEST(BotBehaviorTreeTest, StopsAttackingCorpseWhileEngagementBranchIsRunning)
{
	TreeFixture fixture;

	NotifyBuilder notify;
	notify.Add(MakeMonster(10, 1.0f, 0.0f));
	notify.Add(MakeMonster(11, 5.0f, 0.0f));
	fixture.blackboard.view.Apply(notify.Build(), 1.0, 99);

	fixture.Tick(1.0);   // 대상 획득
	fixture.Tick(1.1);   // 사거리 안 → 공격(교전 분기가 Running 으로 잠긴다)
	ASSERT_EQ(fixture.blackboard.target_actor_id, 10);
	ASSERT_EQ(fixture.actions.attacks.size(), 1u);

	// 대상이 죽었지만 아직 시야에는 남아 있다(사망은 상태 변경일 뿐 시야 이탈이 아니다).
	ActorUpdate corpse = MakeMonster(10, 1.0f, 0.0f, 0);
	corpse.state = syncnet::AIState::AIState_Dead;

	NotifyBuilder death;
	death.Add(corpse);
	fixture.blackboard.view.Apply(death.Build(), 2.0, 99);

	const size_t attacks_before_death = fixture.actions.attacks.size();

	fixture.Tick(2.0);
	fixture.Tick(2.1);

	EXPECT_EQ(fixture.blackboard.target_actor_id, 11);
	for (size_t i = attacks_before_death; i < fixture.actions.attacks.size(); ++i)
		EXPECT_NE(fixture.actions.attacks[i].target_id, 10) << "죽은 대상을 다시 공격했다";
}

TEST(BotBehaviorTreeTest, WandersAroundSpawnWhenNothingToHunt)
{
	TreeFixture fixture;
	fixture.blackboard.spawn_pos = Vec3(100.0f, 0.0f, 100.0f);
	fixture.blackboard.self_pos = Vec3(100.0f, 0.0f, 100.0f);
	fixture.blackboard.wander_target = fixture.blackboard.self_pos;

	fixture.Tick(1.0);

	ASSERT_EQ(fixture.actions.moves.size(), 1u);
	const Vec3& destination = fixture.actions.moves[0].pos;
	EXPECT_LE(DistanceXZ(destination, fixture.blackboard.spawn_pos),
		fixture.blackboard.ai.wander_radius * 1.5f);
	EXPECT_TRUE(fixture.actions.attacks.empty());
}

TEST(BotBehaviorTreeTest, SendsNothingWhileDead)
{
	TreeFixture fixture;
	fixture.blackboard.self_dead = true;

	NotifyBuilder notify;
	notify.Add(MakeMonster(10, 1.0f, 0.0f));
	fixture.blackboard.view.Apply(notify.Build(), 1.0, 99);

	fixture.Tick(1.0);
	fixture.Tick(2.0);

	EXPECT_TRUE(fixture.actions.moves.empty());
	EXPECT_TRUE(fixture.actions.attacks.empty());
	EXPECT_EQ(fixture.blackboard.target_actor_id, 0);

	// 부활하면 다시 사냥한다.
	fixture.blackboard.self_dead = false;
	fixture.Tick(3.0);
	EXPECT_EQ(fixture.blackboard.target_actor_id, 10);
}
