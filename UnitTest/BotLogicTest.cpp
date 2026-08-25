#include <gtest/gtest.h>

#include <algorithm>
#include <string>
#include <vector>

#include "BotBTNodes.h"
#include "BotBehaviorTree.h"
#include "BotMetrics.h"
#include "BotPacket.h"
#include "BotQuestBrain.h"
#include "BotScenario.h"
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
		struct DialogCommand { int node_id; int choice_index; };
		struct CompleteCommand { int quest_id; int reward_choice; };

		std::vector<MoveCommand> moves;
		std::vector<AttackCommand> attacks;
		std::vector<int> interacts;
		std::vector<DialogCommand> dialogs;
		std::vector<CompleteCommand> completes;
		std::vector<int> gates;

		void MoveTo(const Vec3& pos) override { moves.push_back({ pos }); }
		void Attack(int target_actor_id, const Vec3& target_pos) override
		{
			attacks.push_back({ target_actor_id, target_pos });
		}
		void Interact(int target_id) override { interacts.push_back(target_id); }
		void SelectDialog(int node_id, int choice_index) override
		{
			dialogs.push_back({ node_id, choice_index });
		}
		void CompleteQuest(int quest_id, int reward_choice) override
		{
			completes.push_back({ quest_id, reward_choice });
		}
		void EnterGate(int gate_id) override { gates.push_back(gate_id); }
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

//--- 시나리오 데이터 -----------------------------------------------------------------
//
// 서버가 읽는 것과 같은 게임 데이터(Client/Assets/Resources/GameData/)를 그대로 읽는다.
// 여기서 깨지면 봇은 접속은 하되 아무 데도 가지 않는데, 패킷 수치만 봐서는 정상으로 보인다.

namespace
{
	// 파싱은 한 번만 한다(테스트마다 다시 읽을 이유가 없다).
	const BotScenario* SharedScenario()
	{
		static BotScenario scenario;
		static const bool loaded = []() {
			std::string error;
			const bool ok = scenario.Load("", error);
			if (!ok)
				ADD_FAILURE() << "시나리오 데이터를 읽지 못했다: " << error;
			return ok;
		}();
		return loaded ? &scenario : nullptr;
	}

	// 대화 노드에서 서버가 보낼 법한 목록을 만든다. 서버는 조건(show_if)에 걸러진 것만
	// 보내므로, 데이터의 번호와 보이는 번호가 다른 상황을 여기서 재현한다.
	std::vector<std::string> VisibleTextIds(const BotScenario& scenario, int node_id,
		const std::vector<std::string>& drop)
	{
		std::vector<std::string> visible;
		const ScenarioDialogNode* node = scenario.FindDialog(node_id);
		if (node == nullptr)
			return visible;

		for (const ScenarioChoice& choice : node->choices)
		{
			if (std::find(drop.begin(), drop.end(), choice.text_id) != drop.end())
				continue;
			visible.push_back(choice.text_id);
		}
		return visible;
	}
}

TEST(BotScenarioTest, LoadsMainChainsAndSplitsBranchesByBotIndex)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	const std::vector<int>& chains = scenario->MainChains();
	ASSERT_GE(chains.size(), 2u) << "메인 체인이 둘 이상이어야 봇마다 다른 체인을 탄다";

	// 체인은 봇 번호로 갈린다 — 이웃한 두 봇은 서로 다른 체인을 탄다.
	const std::vector<int> first = scenario->BuildMainQuestPlan(0);
	const std::vector<int> second = scenario->BuildMainQuestPlan(1);
	ASSERT_FALSE(first.empty());
	ASSERT_FALSE(second.empty());
	EXPECT_NE(first.front(), second.front());

	// 계획은 언제나 체인의 첫 칸(chain_step 1)부터다 — "처음부터" 진행한다.
	const ScenarioQuest* head = scenario->FindQuest(first.front());
	ASSERT_NE(head, nullptr);
	EXPECT_EQ(head->chain_step, 1);
}

TEST(BotScenarioTest, PicksOneSideOfAMutuallyExclusiveBranch)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	// 분기 한 쌍(같은 자리에 놓이고 서로를 막는 퀘스트)을 데이터에서 찾는다.
	const ScenarioQuest* branch = scenario->FindQuest(11002);
	ASSERT_NE(branch, nullptr);
	ASSERT_FALSE(branch->blocked_by.empty()) << "11002 에 분기 상대가 없다";

	const int other_id = branch->blocked_by.front();
	const ScenarioQuest* other = scenario->FindQuest(other_id);
	ASSERT_NE(other, nullptr);
	EXPECT_EQ(other->chain_id, branch->chain_id);
	EXPECT_EQ(other->chain_step, branch->chain_step);

	// 서로를 막고 있어야 진짜 분기다(한쪽만 막으면 순서에 따라 둘 다 할 수 있다).
	EXPECT_NE(std::find(other->blocked_by.begin(), other->blocked_by.end(), branch->id),
		other->blocked_by.end());

	// 한 봇의 계획에는 한쪽만 들어가고, 가지 번호를 옮기면 반대쪽이 들어간다.
	const int chain_count = static_cast<int>(scenario->MainChains().size());
	bool saw_branch = false;
	bool saw_other = false;

	for (int branch_index = 0; branch_index < chain_count * 4; ++branch_index)
	{
		const std::vector<int> plan = scenario->BuildMainQuestPlan(branch_index);
		const bool has_branch = std::find(plan.begin(), plan.end(), branch->id) != plan.end();
		const bool has_other = std::find(plan.begin(), plan.end(), other_id) != plan.end();

		EXPECT_FALSE(has_branch && has_other) << "한 봇이 배타적인 두 가지를 모두 계획했다";
		saw_branch = saw_branch || has_branch;
		saw_other = saw_other || has_other;
	}

	EXPECT_TRUE(saw_branch);
	EXPECT_TRUE(saw_other) << "봇을 늘려도 한쪽 가지는 아무도 진행하지 않는다";
}

TEST(BotScenarioTest, RoutesBetweenMapsThroughGates)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	// 시작 마을(1)에서 어둠의 숲(2)까지는 게이트를 여러 번 갈아탄다.
	// 봇은 매번 "지금 맵에서 밟을 게이트" 하나만 알면 된다.
	int map_id = 1;
	int hops = 0;
	while (map_id != 2 && hops < 8)
	{
		const ScenarioGate* gate = scenario->NextGate(map_id, 2);
		ASSERT_NE(gate, nullptr) << "맵 " << map_id << " 에서 2 로 가는 길이 없다";
		EXPECT_EQ(gate->map_id, map_id);

		// 도착 맵은 게이트가 정한다. 다음 걸음을 위해 그 맵으로 옮겨 간다.
		const ScenarioGate* landing = scenario->FindGate(gate->target_id);
		map_id = landing != nullptr ? landing->map_id : 0;
		++hops;
	}

	EXPECT_EQ(map_id, 2);
	EXPECT_EQ(scenario->NextGate(1, 1), nullptr);   // 같은 맵이면 이동할 필요가 없다
}

TEST(BotScenarioTest, FindsDialogRouteToAQuestAction)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	// 리네의 첫 노드(3201)에는 수락 선택지가 없다 — 가지별 창구로 한 번 건너가야 한다.
	const int hop = scenario->NextDialogChoice(3201, "accept_quest", 11009);
	ASSERT_GE(hop, 0);

	const ScenarioDialogNode* greet = scenario->FindDialog(3201);
	ASSERT_NE(greet, nullptr);
	ASSERT_LT(hop, static_cast<int>(greet->choices.size()));
	EXPECT_EQ(greet->choices[hop].action, "goto");
	EXPECT_EQ(greet->choices[hop].next_id, 3204);

	// 그 창구에서는 수락 선택지가 바로 나온다.
	const int direct = scenario->NextDialogChoice(3204, "accept_quest", 11009);
	ASSERT_GE(direct, 0);
	const ScenarioDialogNode* node = scenario->FindDialog(3204);
	ASSERT_NE(node, nullptr);
	EXPECT_EQ(node->choices[direct].action, "accept_quest");
	EXPECT_EQ(node->choices[direct].param, 11009);

	// 없는 퀘스트로는 길이 없다.
	EXPECT_LT(scenario->NextDialogChoice(3201, "accept_quest", 999999), 0);
}

//--- 시나리오 판단(BotQuestBrain) ----------------------------------------------------

namespace
{
	// 계획의 첫 퀘스트가 quest_id 인 가지 번호를 찾는다. 봇 번호로 가지가 갈리므로
	// 테스트는 "그 가지를 탄 봇"을 골라 써야 한다.
	int BranchIndexStartingWith(const BotScenario& scenario, int quest_id)
	{
		for (int branch_index = 0; branch_index < 16; ++branch_index)
		{
			const std::vector<int> plan = scenario.BuildMainQuestPlan(branch_index);
			if (!plan.empty() && plan.front() == quest_id)
				return branch_index;
		}
		return -1;
	}
}

TEST(BotQuestBrainTest, StartsAtTheFirstQuestGiverOfItsChain)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	const int branch_index = BranchIndexStartingWith(*scenario, 11001);
	ASSERT_GE(branch_index, 0);

	BotQuestBrain brain;
	brain.Configure(scenario, branch_index);
	ASSERT_TRUE(brain.Enabled());

	brain.SetMapId(1);          // 시작 마을에서 접속했다
	brain.Update(1.0);

	const QuestGoal& goal = brain.Goal();
	EXPECT_EQ(goal.kind, QuestGoalKind::Interact);
	EXPECT_EQ(goal.quest_id, 11001);
	EXPECT_EQ(goal.npc_id, 2001);       // 11001 의 시작 NPC
	EXPECT_EQ(goal.dialog_quest_id, 11001);
	EXPECT_FALSE(goal.dialog_complete);
}

TEST(BotQuestBrainTest, TravelsWhenTheQuestGiverIsInAnotherMap)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	const int branch_index = BranchIndexStartingWith(*scenario, 11001);
	ASSERT_GE(branch_index, 0);

	BotQuestBrain brain;
	brain.Configure(scenario, branch_index);
	brain.SetMapId(2);          // 어둠의 숲에 있는데 첫 NPC 는 시작 마을에 있다
	brain.Update(1.0);

	const QuestGoal& goal = brain.Goal();
	EXPECT_EQ(goal.kind, QuestGoalKind::Travel);
	EXPECT_NE(goal.gate_id, 0);

	const ScenarioGate* gate = scenario->FindGate(goal.gate_id);
	ASSERT_NE(gate, nullptr);
	EXPECT_EQ(gate->map_id, 2) << "지금 맵에 없는 게이트를 밟으러 간다";
}

TEST(BotQuestBrainTest, TurnsKillObjectivesIntoAHuntGoalAndReportsWhenDone)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	const int branch_index = BranchIndexStartingWith(*scenario, 11001);
	ASSERT_GE(branch_index, 0);

	BotQuestBrain brain;
	brain.Configure(scenario, branch_index);
	brain.SetMapId(1);

	// 11001 2단계: 슬라임 5마리. 시작 마을에 슬라임 스폰이 있으므로 그 자리로 간다.
	const int progress[3] = { 0, 0, 0 };
	brain.ApplyQuestInfo(11001, BotQuestBrain::kStateInProgress, 2, progress, 3);
	brain.Update(1.0);

	EXPECT_EQ(brain.Goal().kind, QuestGoalKind::Hunt);
	EXPECT_EQ(brain.Goal().quest_id, 11001);
	EXPECT_EQ(brain.Goal().map_id, 1);

	// 사냥터가 정해지면 배회도 그 주변에서 한다.
	Vec3 anchor;
	float radius = 0.0f;
	EXPECT_TRUE(brain.HuntAnchor(anchor, radius));
	EXPECT_GT(radius, 0.0f);

	// 목표를 다 채우면 서버가 완료 대기로 바꾼다 — 그때는 완료 NPC 를 찾아간다.
	brain.ApplyQuestInfo(11001, BotQuestBrain::kStateReadyToComplete, 3, progress, 3);
	brain.Update(2.0);

	EXPECT_EQ(brain.Goal().kind, QuestGoalKind::Travel);   // 완료 NPC(리네)는 다른 맵에 있다
	EXPECT_EQ(brain.Goal().quest_id, 11001);
}

TEST(BotQuestBrainTest, MapsFilteredDialogChoicesBackToTheDataChoice)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	const int branch_index = BranchIndexStartingWith(*scenario, 11001);
	ASSERT_GE(branch_index, 0);

	BotQuestBrain brain;
	brain.Configure(scenario, branch_index);
	brain.SetMapId(1);
	brain.Update(1.0);
	ASSERT_EQ(brain.Goal().npc_id, 2001);

	// 서버는 조건에 걸러진 목록만 보낸다. 앞쪽 선택지가 빠지면 데이터의 번호와
	// 보이는 번호가 어긋나는데, 봇은 text_id 로 되짚어 제 선택지를 찾아야 한다.
	const ScenarioDialogNode* node = scenario->FindDialog(3001);
	ASSERT_NE(node, nullptr);

	const std::vector<std::string> visible =
		VisibleTextIds(*scenario, 3001, { node->choices.front().text_id });
	ASSERT_FALSE(visible.empty());

	brain.OnDialogOpened(2001, 3001, visible);
	const int choice = brain.ChooseDialogChoice();
	ASSERT_GE(choice, 0);
	ASSERT_LT(choice, static_cast<int>(visible.size()));

	// 고른 번호가 가리키는 것이 실제로 11001 로 가는 길이어야 한다.
	const int data_index = scenario->NextDialogChoice(3001, "accept_quest", 11001);
	ASSERT_GE(data_index, 0);
	EXPECT_EQ(visible[choice], node->choices[data_index].text_id);
}

TEST(BotQuestBrainTest, ClosesDialogThatHasNothingToDoForTheCurrentGoal)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	const int branch_index = BranchIndexStartingWith(*scenario, 11001);
	ASSERT_GE(branch_index, 0);

	BotQuestBrain brain;
	brain.Configure(scenario, branch_index);
	brain.SetMapId(1);
	brain.Update(1.0);

	// 계획에 없는 NPC 의 대화가 열렸다(대장장이). 여기서 할 일은 없으니 닫는다.
	brain.OnDialogOpened(2002, 3101, VisibleTextIds(*scenario, 3101, {}));
	EXPECT_LT(brain.ChooseDialogChoice(), 0);
}

TEST(BotQuestBrainTest, BacksOffToLevelUpWhenTheAcceptChoiceIsNotOffered)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	const int branch_index = BranchIndexStartingWith(*scenario, 11001);
	ASSERT_GE(branch_index, 0);

	BotQuestBrain brain;
	brain.Configure(scenario, branch_index);
	brain.SetMapId(1);
	brain.Update(1.0);
	ASSERT_EQ(brain.Goal().kind, QuestGoalKind::Interact);

	// 레벨이 모자라면 서버가 수락 선택지를 아예 내보내지 않는다. 그 자리에서 NPC 를
	// 계속 두드리는 대신 잡으러 가야 한다 — 그러지 않으면 봇이 영원히 멈춰 선다.
	const ScenarioDialogNode* node = scenario->FindDialog(3001);
	ASSERT_NE(node, nullptr);
	const int data_index = scenario->NextDialogChoice(3001, "accept_quest", 11001);
	ASSERT_GE(data_index, 0);

	const std::vector<std::string> without_route =
		VisibleTextIds(*scenario, 3001, { node->choices[data_index].text_id });

	for (int attempt = 0; attempt < 3; ++attempt)
	{
		brain.OnDialogOpened(2001, 3001, without_route);
		EXPECT_LT(brain.ChooseDialogChoice(), 0);
		brain.OnDialogClosed();
		brain.Update(2.0 + attempt);
	}

	EXPECT_EQ(brain.Goal().kind, QuestGoalKind::Hunt);
}

//--- 시나리오(BT) 실행 ---------------------------------------------------------------

TEST(BotBehaviorTreeTest, WalksToTheQuestGiverAndTalksInsteadOfHunting)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	const int branch_index = BranchIndexStartingWith(*scenario, 11001);
	ASSERT_GE(branch_index, 0);

	TreeFixture fixture;
	fixture.blackboard.quest.Configure(scenario, branch_index);
	fixture.blackboard.quest.SetMapId(1);
	fixture.blackboard.self_pos = Vec3(0.0f, 0.0f, 0.0f);

	// 가는 길에 몬스터가 보여도 쫓아가지 않는다. 쫓아가기 시작하면 NPC 에 닿지 못한다.
	NotifyBuilder notify;
	notify.Add(MakeMonster(10, 1.0f, 0.0f));
	fixture.blackboard.view.Apply(notify.Build(), 1.0, 99);

	fixture.blackboard.quest.Update(1.0);
	const QuestGoal& goal = fixture.blackboard.quest.Goal();
	ASSERT_EQ(goal.kind, QuestGoalKind::Interact);

	fixture.Tick(1.0);
	ASSERT_FALSE(fixture.actions.moves.empty());
	EXPECT_TRUE(fixture.actions.attacks.empty());
	EXPECT_FLOAT_EQ(fixture.actions.moves.back().pos.x, goal.pos.x);

	// 서버가 이동을 처리해 NPC 앞에 도착했다.
	fixture.blackboard.self_pos = goal.pos;
	fixture.Tick(1.5);

	ASSERT_EQ(fixture.actions.interacts.size(), 1u);
	EXPECT_EQ(fixture.actions.interacts[0], goal.npc_id);
}

TEST(BotBehaviorTreeTest, StepsOnTheGateWhenTheGoalIsInAnotherMap)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	const int branch_index = BranchIndexStartingWith(*scenario, 11001);
	ASSERT_GE(branch_index, 0);

	TreeFixture fixture;
	fixture.blackboard.quest.Configure(scenario, branch_index);
	fixture.blackboard.quest.SetMapId(2);        // 첫 NPC 는 다른 맵에 있다
	fixture.blackboard.quest.Update(1.0);

	const QuestGoal& goal = fixture.blackboard.quest.Goal();
	ASSERT_EQ(goal.kind, QuestGoalKind::Travel);

	fixture.blackboard.self_pos = Vec3(0.0f, 0.0f, 0.0f);
	fixture.Tick(1.0);
	ASSERT_FALSE(fixture.actions.moves.empty());
	EXPECT_TRUE(fixture.actions.gates.empty()) << "게이트에 닿기도 전에 진입을 보냈다";

	fixture.blackboard.self_pos = goal.pos;
	fixture.Tick(1.5);

	ASSERT_EQ(fixture.actions.gates.size(), 1u);
	EXPECT_EQ(fixture.actions.gates[0], goal.gate_id);

	// 게이트 요청은 간격을 두고 보낸다(서버 쿨타임이 1초다).
	fixture.Tick(1.6);
	EXPECT_EQ(fixture.actions.gates.size(), 1u);
}

TEST(BotBehaviorTreeTest, KeepsHuntingWhenTheGoalIsAHuntingGround)
{
	const BotScenario* scenario = SharedScenario();
	ASSERT_NE(scenario, nullptr);

	const int branch_index = BranchIndexStartingWith(*scenario, 11001);
	ASSERT_GE(branch_index, 0);

	TreeFixture fixture;
	fixture.blackboard.quest.Configure(scenario, branch_index);
	fixture.blackboard.quest.SetMapId(1);

	const int progress[3] = { 0, 0, 0 };
	fixture.blackboard.quest.ApplyQuestInfo(11001, BotQuestBrain::kStateInProgress, 2, progress, 3);
	fixture.blackboard.quest.Update(1.0);
	ASSERT_EQ(fixture.blackboard.quest.Goal().kind, QuestGoalKind::Hunt);

	// 사냥터 안에 있고 몬스터가 보이면 평소처럼 싸운다.
	fixture.blackboard.self_pos = fixture.blackboard.quest.Goal().pos;

	NotifyBuilder notify;
	notify.Add(MakeMonster(10, fixture.blackboard.self_pos.x + 1.0f,
		fixture.blackboard.self_pos.z));
	fixture.blackboard.view.Apply(notify.Build(), 1.0, 99);

	fixture.Tick(1.0);
	EXPECT_EQ(fixture.blackboard.target_actor_id, 10);

	fixture.Tick(1.1);
	EXPECT_FALSE(fixture.actions.attacks.empty());
}
