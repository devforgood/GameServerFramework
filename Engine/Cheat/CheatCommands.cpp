#include "CheatCommands.h"

#include <algorithm>
#include <cctype>
#include <charconv>
#include <cmath>
#include <cstdio>
#include <iterator>
#include <string>
#include <utility>
#include <vector>

#include "Character.h"
#include "GameObject.h"
#include "Map.h"
#include "Monster.h"
#include "Player.h"
#include "PlayerItem.h"
#include "PlayerLevel.h"
#include "PlayerQuest.h"
#include "PlayerSkill.h"
#include "PlayerWallet.h"
#include "Vector3.h"
#include "World.h"
#include "GameData/ResourceLoader.h"
#include "gamedata.h"
#include "LogHelper.h"

namespace
{
	//-----------------------------------------------------------------------------------
	// 입력 한 줄 다루기
	//-----------------------------------------------------------------------------------

	bool IsSpace(char c) { return c == ' ' || c == '\t' || c == '\r' || c == '\n'; }

	// 앞뒤 공백을 벗기고, 명령 앞에 붙은 '/' 도 벗긴다.
	// 채팅 창에 "/allskill" 로 쳐도 "allskill" 로 쳐도 같은 명령이 되게 한다.
	std::string_view Trim(std::string_view text)
	{
		while (!text.empty() && IsSpace(text.front()))
			text.remove_prefix(1);
		while (!text.empty() && IsSpace(text.back()))
			text.remove_suffix(1);
		return text;
	}

	// 공백으로 끊어 낱말 목록을 만든다. 첫 낱말이 명령 이름, 나머지가 인자다.
	std::vector<std::string_view> Split(std::string_view text)
	{
		std::vector<std::string_view> words;
		size_t pos = 0;
		while (pos < text.size())
		{
			while (pos < text.size() && IsSpace(text[pos]))
				++pos;
			const size_t begin = pos;
			while (pos < text.size() && !IsSpace(text[pos]))
				++pos;
			if (pos > begin)
				words.push_back(text.substr(begin, pos - begin));
		}
		return words;
	}

	// 숫자 인자. 못 읽으면 값을 건드리지 않고 false 를 돌려준다 —
	// "0 으로 읽혔다" 와 "안 썼다" 를 구분해야 인자 안내를 낼 수 있다.
	bool ParseInt(std::string_view text, long long& out)
	{
		if (text.empty())
			return false;

		long long value = 0;
		const char* begin = text.data();
		const char* end = text.data() + text.size();
		const auto parsed = std::from_chars(begin, end, value);
		if (parsed.ec != std::errc() || parsed.ptr != end)
			return false;

		out = value;
		return true;
	}

	// 좌표/반경처럼 소수점이 올 수 있는 인자. stof 가 아니라 from_chars 를 쓰는 이유는
	// 잘못 친 값에 예외를 던지지 않기 때문이다(엔진은 예외를 쓰지 않는다).
	bool ParseFloat(std::string_view text, float& out)
	{
		if (text.empty())
			return false;

		float value = 0.0f;
		const char* begin = text.data();
		const char* end = text.data() + text.size();
		const auto parsed = std::from_chars(begin, end, value);
		if (parsed.ec != std::errc() || parsed.ptr != end)
			return false;

		out = value;
		return true;
	}

	//-----------------------------------------------------------------------------------
	// 명령이 받는 것들
	//
	// 치트는 대부분 "내 캐릭터"와 "내 컴포넌트"만 있으면 된다. 매 명령이 같은 널 검사를
	// 반복하지 않도록 한 번만 풀어서 넘긴다. character/map 은 없을 수 있다(로그인 직후,
	// 캐릭터 없이 도는 단위 테스트).
	//-----------------------------------------------------------------------------------
	struct Context
	{
		GameObject* owner = nullptr;
		Player* player = nullptr;
		Character* character = nullptr;
		Map* map = nullptr;

		// 명령 이름 뒤의 낱말들.
		std::vector<std::string_view> args;

		std::string_view Arg(size_t index) const
		{
			return index < args.size() ? args[index] : std::string_view();
		}

		bool ArgInt(size_t index, long long& out) const { return ParseInt(Arg(index), out); }

		// 생략 가능한 숫자 인자. 안 썼거나 못 읽으면 기본값.
		long long ArgIntOr(size_t index, long long fallback) const
		{
			long long value = 0;
			return ArgInt(index, value) ? value : fallback;
		}
	};

	cheat::Result Reply(std::string text)
	{
		cheat::Result result;
		result.handled = true;
		result.reply = std::move(text);
		return result;
	}

	// 캐릭터가 필요한 명령이 공통으로 쓰는 거절문. 로그인 직후(스폰 전)에 칠 수 있다.
	cheat::Result NeedCharacter(std::string_view name)
	{
		return Reply("치트 " + std::string(name) + ": 캐릭터가 아직 월드에 없습니다.");
	}

	//-----------------------------------------------------------------------------------
	// 좌표
	//
	// 서버와 클라는 x 축 부호가 반대다(Vector3::convert_x). 채팅 창에 찍어 주는 좌표와
	// 치트가 받는 좌표는 전부 클라 좌표계로 통일한다 — 플레이어가 보는 값(씬 에디터,
	// Map.json)과 같아야 "저기로 가 봐" 가 성립한다.
	//-----------------------------------------------------------------------------------
	syncnet::Vec3 ToClient(const Vector3& serverPos)
	{
		return syncnet::Vec3(Vector3::convert_x(serverPos.x), serverPos.y, serverPos.z);
	}

	std::string FormatPos(const syncnet::Vec3& pos)
	{
		char buffer[64] = { 0 };
		std::snprintf(buffer, sizeof(buffer), "(%.1f, %.1f, %.1f)", pos.x(), pos.y(), pos.z());
		return buffer;
	}

	//-----------------------------------------------------------------------------------
	// spawn : 내 주변에 몬스터를 세운다.
	//
	// 마커가 관리하는 스폰이 아니라서 정원/리스폰 대상이 아니다. 잡으면 그대로 사라지므로
	// 전투를 몇 번이고 다시 만들어 볼 수 있다(마커 스폰은 spawn_interval 을 기다려야 한다).
	//-----------------------------------------------------------------------------------
	constexpr int kMaxSpawnCount = 50;   // 손이 미끄러져 10000 을 쳐도 서버가 멈추지 않게
	constexpr float kDefaultSpawnRadius = 3.0f;

	cheat::Result Spawn(Context& ctx)
	{
		long long monsterId = 0;
		if (!ctx.ArgInt(0, monsterId))
			return Reply("치트 spawn: 몬스터 id 가 필요합니다. 예) /spawn 1001 5");

		if (ctx.character == nullptr || ctx.map == nullptr)
			return NeedCharacter("spawn");

		const gamedata::MonsterData* data =
			ResourceLoader::Instance().GetMonsterData(static_cast<long>(monsterId));
		if (data == nullptr)
			return Reply("치트 spawn: monster.json 에 종류 " + std::to_string(monsterId) + " 가 없습니다.");

		int count = static_cast<int>(ctx.ArgIntOr(1, 1));
		count = std::clamp(count, 1, kMaxSpawnCount);

		float radius = kDefaultSpawnRadius;
		if (!ctx.Arg(2).empty() && !ParseFloat(ctx.Arg(2), radius))
			radius = kDefaultSpawnRadius;
		radius = std::clamp(radius, 0.0f, 100.0f);

		// 한 점에 겹쳐 세우면 이동 에이전트끼리 밀어내느라 흩어지는 데 시간이 걸린다.
		// 처음부터 원 둘레에 나눠 세운다(마리 수가 늘면 그만큼 촘촘해진다).
		const syncnet::Vec3 center = ToClient(ctx.character->GetPosition());

		int spawned = 0;
		for (int i = 0; i < count; ++i)
		{
			const float angle = (count > 1) ? (6.2831853f * i / count) : 0.0f;
			const double x = center.x() + std::cos(angle) * radius;
			const double z = center.z() + std::sin(angle) * radius;

			if (ctx.map->SpawnMonsterOfType(static_cast<int>(monsterId), x, center.y(), z) >= 0)
				++spawned;
		}

		std::string reply = "치트 spawn: " + data->name + "(" + std::to_string(monsterId) + ") "
			+ std::to_string(spawned) + "마리 스폰.";
		if (spawned < count)
		{
			// navmesh 밖으로 떨어지면 스폰이 거부된다. 반경을 줄이라는 뜻이다.
			reply += " (" + std::to_string(count - spawned) + "마리는 위치를 잡지 못했습니다 — 반경을 줄여 보세요)";
		}
		return Reply(std::move(reply));
	}

	//-----------------------------------------------------------------------------------
	// killall : 맵의 몬스터를 전멸시킨다.
	//
	// 체력을 0 으로 만드는 것으로 끝내지 않고 마지막 공격자를 내 캐릭터로 새긴다.
	// 그래야 정상 처치와 같은 경로(NotifyKilledBy -> 경험치/퀘스트/드랍)를 탄다 —
	// 그냥 지워 버리면 "몬스터 10마리 처치" 퀘스트를 치트로 볼 수 없다.
	//-----------------------------------------------------------------------------------
	cheat::Result KillAll(Context& ctx)
	{
		if (ctx.character == nullptr || ctx.map == nullptr)
			return NeedCharacter("killall");

		float radius = 0.0f; // 0 이면 맵 전체
		const bool hasRadius = ParseFloat(ctx.Arg(0), radius);

		const Vector3& from = ctx.character->GetPosition();
		const int killerActorId = ctx.character->GetActorId();

		int killed = 0;
		for (auto& actor : ctx.map->CollectActors(syncnet::GameObjectType::GameObjectType_Monster))
		{
			if (actor == nullptr || actor->IsDead())
				continue;

			if (hasRadius && radius > 0.0f)
			{
				const Vector3& pos = actor->GetPosition();
				const float dx = pos.x - from.x;
				const float dz = pos.z - from.z;
				if (dx * dx + dz * dz > radius * radius)
					continue;
			}

			actor->SetLastAttacker(killerActorId);
			actor->SetHealth(0);
			++killed;
		}

		const std::string where = (hasRadius && radius > 0.0f)
			? "반경 " + std::string(ctx.Arg(0)) + " 안"
			: std::string("맵 전체");
		return Reply("치트 killall: " + where + "의 몬스터 " + std::to_string(killed) + "마리 처치.");
	}

	//-----------------------------------------------------------------------------------
	// 체력 / 생존
	//-----------------------------------------------------------------------------------
	cheat::Result Heal(Context& ctx)
	{
		if (ctx.character == nullptr)
			return NeedCharacter("heal");

		ctx.character->SetHealth(ctx.character->GetMaxHealth());
		return Reply("치트 heal: 체력 " + std::to_string(ctx.character->GetHealth())
			+ "/" + std::to_string(ctx.character->GetMaxHealth()) + ".");
	}

	cheat::Result Hp(Context& ctx)
	{
		if (ctx.character == nullptr)
			return NeedCharacter("hp");

		long long value = 0;
		if (!ctx.ArgInt(0, value))
			return Reply("치트 hp: 체력 값이 필요합니다. 예) /hp 1");

		const int maxHealth = ctx.character->GetMaxHealth();
		const int health = static_cast<int>(std::clamp<long long>(value, 0, maxHealth));
		ctx.character->SetHealth(health);

		return Reply("치트 hp: 체력 " + std::to_string(health) + "/" + std::to_string(maxHealth)
			+ (health == 0 ? " (사망 처리됩니다)" : ""));
	}

	cheat::Result Die(Context& ctx)
	{
		if (ctx.character == nullptr)
			return NeedCharacter("die");

		ctx.character->SetHealth(0);
		return Reply("치트 die: 캐릭터를 사망 상태로 만들었습니다(부활 대기를 확인하세요).");
	}

	cheat::Result God(Context& ctx)
	{
		if (ctx.character == nullptr)
			return NeedCharacter("god");

		const std::string_view arg = ctx.Arg(0);
		const bool on = arg.empty() ? !ctx.character->IsInvincible()
			: (arg == "on" || arg == "1" || arg == "true");

		ctx.character->SetInvincible(on);
		return Reply(std::string("치트 god: 무적 ") + (on ? "켬" : "끔")
			+ " (맵을 옮기면 캐릭터가 새로 만들어져 풀립니다)");
	}

	//-----------------------------------------------------------------------------------
	// 성장 / 재화 / 인벤토리
	//-----------------------------------------------------------------------------------
	cheat::Result SetLevel(Context& ctx)
	{
		long long level = 0;
		if (!ctx.ArgInt(0, level))
			return Reply("치트 level: 레벨이 필요합니다. 예) /level 20");

		auto* levelComponent = ctx.owner->GetComponent<PlayerLevel>();
		if (levelComponent == nullptr)
			return Reply("치트 level: 이 캐릭터에는 레벨 컴포넌트가 없습니다.");

		if (!levelComponent->GmSetLevel(static_cast<int>(level)))
			return Reply("치트 level: level.json 에 레벨 " + std::to_string(level) + " 행이 없습니다.");

		return Reply("치트 level: 레벨 " + std::to_string(levelComponent->GetLevel())
			+ " (누적 경험치 " + std::to_string(levelComponent->GetExp()) + ").");
	}

	cheat::Result GainExp(Context& ctx)
	{
		long long amount = 0;
		if (!ctx.ArgInt(0, amount) || amount <= 0)
			return Reply("치트 exp: 지급할 경험치가 필요합니다. 예) /exp 500");

		auto* levelComponent = ctx.owner->GetComponent<PlayerLevel>();
		if (levelComponent == nullptr)
			return Reply("치트 exp: 이 캐릭터에는 레벨 컴포넌트가 없습니다.");

		const int before = levelComponent->GetLevel();
		levelComponent->GainExp(static_cast<int>(amount));

		std::string reply = "치트 exp: 경험치 " + std::to_string(amount) + " 지급(누적 "
			+ std::to_string(levelComponent->GetExp()) + ").";
		if (levelComponent->GetLevel() != before)
			reply += " 레벨 " + std::to_string(before) + " -> " + std::to_string(levelComponent->GetLevel()) + ".";
		return Reply(std::move(reply));
	}

	cheat::Result Gold(Context& ctx)
	{
		long long amount = 0;
		if (!ctx.ArgInt(0, amount) || amount == 0)
			return Reply("치트 gold: 지급할 골드가 필요합니다(음수면 차감). 예) /gold 10000");

		auto* wallet = ctx.owner->GetComponent<PlayerWallet>();
		if (wallet == nullptr)
			return Reply("치트 gold: 이 캐릭터에는 지갑 컴포넌트가 없습니다.");

		if (amount > 0)
			wallet->AddGold(amount);
		else if (!wallet->SpendGold(-amount))
			return Reply("치트 gold: 잔액이 모자랍니다(현재 " + std::to_string(wallet->GetGold()) + ").");

		return Reply("치트 gold: 보유 골드 " + std::to_string(wallet->GetGold()) + ".");
	}

	cheat::Result GiveItem(Context& ctx)
	{
		long long itemId = 0;
		if (!ctx.ArgInt(0, itemId))
			return Reply("치트 item: 아이템 id 가 필요합니다. 예) /item 2001 10");

		const gamedata::Item* data = ResourceLoader::Instance().GetItem(static_cast<long>(itemId));
		if (data == nullptr)
			return Reply("치트 item: item.json 에 아이템 " + std::to_string(itemId) + " 가 없습니다.");

		auto* inventory = ctx.owner->GetComponent<PlayerItem>();
		if (inventory == nullptr)
			return Reply("치트 item: 이 캐릭터에는 인벤토리 컴포넌트가 없습니다.");

		const int count = static_cast<int>(std::clamp<long long>(ctx.ArgIntOr(1, 1), 1, 9999));
		inventory->AddItem(static_cast<int>(itemId), count);

		return Reply("치트 item: " + data->name_id + "(" + std::to_string(itemId) + ") "
			+ std::to_string(count) + "개 지급(보유 "
			+ std::to_string(inventory->GetCount(static_cast<int>(itemId))) + ").");
	}

	// 배운 스킬을 캐릭터에 즉시 싣는다. 이게 없으면 다음 접속까지 못 쓴다
	// (캐릭터의 SkillSet 은 빙의 시점에 한 번만 채워진다 — PlayerQuest 의 스킬 보상과 같다).
	void ReloadCharacterSkills(GameObject* owner)
	{
		if (auto* asPlayer = dynamic_cast<Player*>(owner))
			asPlayer->ApplyOwnedSkillsToCharacter();
	}

	cheat::Result LearnSkill(Context& ctx)
	{
		long long skillId = 0;
		if (!ctx.ArgInt(0, skillId))
			return Reply("치트 skill: 스킬 id 가 필요합니다. 예) /skill 5");

		const gamedata::Skill* data = ResourceLoader::Instance().GetSkill(static_cast<long>(skillId));
		if (data == nullptr)
			return Reply("치트 skill: skill.json 에 스킬 " + std::to_string(skillId) + " 가 없습니다.");

		// 몬스터 전용은 배워 봐야 SkillSet::InitFromOwned 가 거른다. 배운 척하지 않는다.
		if (data->monster_only)
			return Reply("치트 skill: " + data->name_id + " 는 몬스터 전용이라 배울 수 없습니다.");

		auto* skills = ctx.owner->GetComponent<PlayerSkill>();
		if (skills == nullptr)
			return Reply("치트 skill: 이 캐릭터에는 스킬 컴포넌트가 없습니다.");

		const bool learned = skills->LearnSkill(static_cast<int>(skillId));
		ReloadCharacterSkills(ctx.owner);

		return Reply("치트 skill: " + data->name_id + "(" + std::to_string(skillId) + ") "
			+ (learned ? "습득." : "이미 배운 스킬입니다."));
	}

	//-----------------------------------------------------------------------------------
	// allskill : skill.json 의 플레이어 스킬을 전부 습득한다.
	//
	// 몬스터 전용(monster_only)은 뺀다. 넣어 봐야 SkillSet::InitFromOwned 가 거르므로
	// player_skill 테이블에 쓸 수 없는 줄만 쌓인다. 패시브(오라)는 넣는다 — 보유만으로
	// 적용되는 진짜 플레이어 스킬이다.
	//-----------------------------------------------------------------------------------
	cheat::Result AllSkill(Context& ctx)
	{
		auto* skills = ctx.owner->GetComponent<PlayerSkill>();
		if (skills == nullptr)
			return Reply("치트 allskill: 이 캐릭터에는 스킬 컴포넌트가 없습니다.");

		int learned = 0;
		int total = 0;
		for (const auto& pair : ResourceLoader::Instance().GetSkills())
		{
			const gamedata::Skill* data = pair.second;
			if (data == nullptr || data->monster_only)
				continue;

			++total;
			if (skills->LearnSkill(static_cast<int>(pair.first)))
				++learned;
		}

		ReloadCharacterSkills(ctx.owner);

		return Reply("치트 allskill: 스킬 " + std::to_string(total) + "개 보유"
			+ " (이번에 새로 배운 것 " + std::to_string(learned) + "개).");
	}

	//-----------------------------------------------------------------------------------
	// quest : 정상 경로의 조건 검사를 건너뛰고 퀘스트 상태를 세운다(PlayerQuest 의 GM 조작).
	//-----------------------------------------------------------------------------------
	cheat::Result QuestCheat(Context& ctx)
	{
		const std::string_view action = ctx.Arg(0);
		long long questId = 0;
		if (action.empty() || !ctx.ArgInt(1, questId))
			return Reply("치트 quest: 사용법 /quest <accept|complete|reset> <questId>");

		if (ResourceLoader::Instance().GetQuest(static_cast<long>(questId)) == nullptr)
			return Reply("치트 quest: quest.json 에 퀘스트 " + std::to_string(questId) + " 가 없습니다.");

		auto* quests = ctx.owner->GetComponent<PlayerQuest>();
		if (quests == nullptr)
			return Reply("치트 quest: 이 캐릭터에는 퀘스트 컴포넌트가 없습니다.");

		const int id = static_cast<int>(questId);
		bool ok = false;
		if (action == "accept")
			ok = quests->GmForceAccept(id);
		else if (action == "complete")
			ok = quests->GmForceComplete(id);
		else if (action == "reset")
			ok = quests->GmResetQuest(id);
		else
			return Reply("치트 quest: 모르는 동작 '" + std::string(action) + "'. accept/complete/reset 중 하나여야 합니다.");

		return Reply("치트 quest: 퀘스트 " + std::to_string(id) + " " + std::string(action)
			+ (ok ? " 성공." : " 실패(이미 그 상태이거나 데이터가 맞지 않습니다)."));
	}

	//-----------------------------------------------------------------------------------
	// 이동
	//-----------------------------------------------------------------------------------
	cheat::Result Teleport(Context& ctx)
	{
		if (ctx.character == nullptr || ctx.map == nullptr)
			return NeedCharacter("tp");

		// 좌표는 클라 좌표계로 받는다(where 가 찍어 주는 값, Map.json 의 값과 같다).
		// y 를 생략하면 지금 높이를 쓴다 — 평지에서는 x/z 만 알면 충분하다.
		float x = 0.0f, y = 0.0f, z = 0.0f;
		const syncnet::Vec3 current = ToClient(ctx.character->GetPosition());

		bool parsed = false;
		if (ctx.args.size() >= 3)
			parsed = ParseFloat(ctx.Arg(0), x) && ParseFloat(ctx.Arg(1), y) && ParseFloat(ctx.Arg(2), z);
		else if (ctx.args.size() == 2)
		{
			y = current.y();
			parsed = ParseFloat(ctx.Arg(0), x) && ParseFloat(ctx.Arg(1), z);
		}

		if (!parsed)
			return Reply("치트 tp: 사용법 /tp <x> <z> 또는 /tp <x> <y> <z> (클라 좌표계)");

		const syncnet::Vec3 destination(x, y, z);
		Vector3 serverPos(&destination);
		ctx.map->TeleportActor(ctx.character, serverPos);

		return Reply("치트 tp: " + FormatPos(destination) + " 로 이동.");
	}

	// mapId 로 갈 수 있는 도착 지점(마커 id)을 찾는다. 맵 이동은 게이트와 같은 규칙으로
	// 마커 id 하나를 따라가므로(World::ChangeMap), 맵의 player_spawn 을 목적지로 쓴다.
	int FindMapArrivalMarker(int mapId)
	{
		const gamedata::Map* data = ResourceLoader::Instance().GetMap(mapId);
		if (data == nullptr || data->spawn_points.player_spawn.empty())
			return 0;

		return data->spawn_points.player_spawn.front().id;
	}

	cheat::Result Goto(Context& ctx)
	{
		long long mapId = 0;
		if (!ctx.ArgInt(0, mapId))
			return Reply("치트 goto: 맵 id 가 필요합니다. 예) /goto 2  ('/list map' 으로 확인)");

		if (ctx.character == nullptr || ctx.map == nullptr || ctx.player == nullptr)
			return NeedCharacter("goto");

		const gamedata::Map* destination = ResourceLoader::Instance().GetMap(static_cast<long>(mapId));
		if (destination == nullptr)
			return Reply("치트 goto: Map.json 에 맵 " + std::to_string(mapId) + " 이 없습니다.");

		if (ctx.map->GetMapId() == static_cast<int>(mapId))
			return Reply("치트 goto: 이미 " + destination->name + " 에 있습니다.");

		const int targetId = FindMapArrivalMarker(static_cast<int>(mapId));
		if (targetId == 0)
			return Reply("치트 goto: " + destination->name + " 에 player_spawn 마커가 없어 도착 지점을 정할 수 없습니다.");

		World* world = ctx.map->world();
		if (world == nullptr)
			return Reply("치트 goto: 월드가 없어 이동할 수 없습니다.");

		// 이동은 캐릭터 재생성 + 클라 통보까지 한 묶음이다(World::ForceMove).
		// 여기서 shared_ptr 을 다시 찾는 이유는 맵이 그 소유권을 들고 있기 때문이다.
		auto player = ctx.map->FindPlayer(ctx.player->GetPlayerId());
		if (player == nullptr)
			return Reply("치트 goto: 맵에서 플레이어를 찾지 못했습니다.");

		if (!world->ForceMove(player, targetId))
			return Reply("치트 goto: " + destination->name + " 으로 이동하지 못했습니다(로그를 확인하세요).");

		return Reply("치트 goto: " + destination->name + "(" + std::to_string(mapId) + ") 으로 이동했습니다.");
	}

	cheat::Result Where(Context& ctx)
	{
		if (ctx.character == nullptr || ctx.map == nullptr)
			return NeedCharacter("where");

		const gamedata::Map* data = ctx.map->GetMapData();
		const std::string mapName = data != nullptr ? data->name : std::string("(데이터 없음)");

		std::string reply = "치트 where:";
		reply += "\n  맵    " + mapName + " (id " + std::to_string(ctx.map->GetMapId()) + ")";
		if (ctx.map->IsInstance())
			reply += " 인스턴스 " + std::to_string(ctx.map->GetInstanceId());
		reply += "\n  위치  " + FormatPos(ToClient(ctx.character->GetPosition()))
			+ "  actor " + std::to_string(ctx.character->GetActorId());
		reply += "\n  체력  " + std::to_string(ctx.character->GetHealth())
			+ "/" + std::to_string(ctx.character->GetMaxHealth())
			+ (ctx.character->IsInvincible() ? " (무적)" : "");

		if (auto* level = ctx.owner->GetComponent<PlayerLevel>())
			reply += "\n  레벨  " + std::to_string(level->GetLevel())
				+ " (경험치 " + std::to_string(level->GetExp()) + ")";

		return Reply(std::move(reply));
	}

	//-----------------------------------------------------------------------------------
	// list : 데이터 id 를 찾아본다.
	//
	// 치트는 대부분 id 를 인자로 받는데, 그 id 를 어디서 보느냐가 매번 문제였다.
	// 클라의 자동완성도 같은 데이터를 읽지만, 서버가 실제로 로드한 것과 다를 수 있어
	// (배포 누락, 다른 브랜치) 서버에 직접 물어보는 통로를 남긴다.
	//-----------------------------------------------------------------------------------
	constexpr size_t kMaxListRows = 25;

	// 이름에 검색어가 들어 있는가(대소문자 무시). 검색어가 비어 있으면 전부 통과.
	bool Matches(std::string_view filter, const std::string& name, int id)
	{
		if (filter.empty())
			return true;

		if (std::to_string(id).find(filter) != std::string::npos)
			return true;

		std::string lowerName = name;
		std::string lowerFilter(filter);
		std::transform(lowerName.begin(), lowerName.end(), lowerName.begin(), ::tolower);
		std::transform(lowerFilter.begin(), lowerFilter.end(), lowerFilter.begin(), ::tolower);
		return lowerName.find(lowerFilter) != std::string::npos;
	}

	std::string FormatRows(std::vector<std::pair<int, std::string>>& rows, std::string_view kind)
	{
		std::sort(rows.begin(), rows.end());

		std::string reply = "치트 list " + std::string(kind) + ": " + std::to_string(rows.size()) + "건";
		const size_t shown = std::min(rows.size(), kMaxListRows);
		for (size_t i = 0; i < shown; ++i)
			reply += "\n  " + std::to_string(rows[i].first) + "  " + rows[i].second;

		if (rows.size() > shown)
			reply += "\n  ... 외 " + std::to_string(rows.size() - shown) + "건(검색어로 좁혀 보세요)";

		return reply;
	}

	cheat::Result List(Context& ctx)
	{
		const std::string_view kind = ctx.Arg(0);
		const std::string_view filter = ctx.Arg(1);

		auto& resource = ResourceLoader::Instance();
		std::vector<std::pair<int, std::string>> rows;

		if (kind == "monster")
		{
			for (const auto& entry : resource.GetMonsterDatas())
				if (const gamedata::MonsterData* data = entry.second;
					data != nullptr && Matches(filter, data->name, data->id))
					rows.emplace_back(data->id, data->name + "  lv" + std::to_string(data->level));
		}
		else if (kind == "item")
		{
			for (const auto& entry : resource.GetItems())
				if (const gamedata::Item* data = entry.second;
					data != nullptr && Matches(filter, data->name_id, data->id))
					rows.emplace_back(data->id, data->name_id + "  " + data->type);
		}
		else if (kind == "skill")
		{
			for (const auto& entry : resource.GetSkills())
				if (const gamedata::Skill* data = entry.second;
					data != nullptr && !data->monster_only && Matches(filter, data->name_id, data->id))
					rows.emplace_back(data->id, data->name_id);
		}
		else if (kind == "map")
		{
			for (const auto& entry : resource.GetMaps())
				if (const gamedata::Map* data = entry.second;
					data != nullptr && Matches(filter, data->name, data->id))
					rows.emplace_back(data->id, data->name);
		}
		else if (kind == "quest")
		{
			for (const auto& entry : resource.GetQuests())
				if (const gamedata::Quest* data = entry.second;
					data != nullptr && Matches(filter, data->name_id, data->id))
					rows.emplace_back(data->id, data->name_id + "  " + data->category);
		}
		else
		{
			return Reply("치트 list: 사용법 /list <monster|item|skill|map|quest> [검색어]");
		}

		return Reply(FormatRows(rows, kind));
	}

	//-----------------------------------------------------------------------------------
	// 명령표.
	//
	// 치트를 추가하려면 여기 한 줄 넣고 함수를 하나 쓰면 된다. help 응답도, 클라 자동완성
	// 목록도 이 표에서 나온다.
	//-----------------------------------------------------------------------------------
	struct Command
	{
		cheat::CommandInfo info;
		cheat::Result (*run)(Context& ctx);
	};

	const Command kCommands[] = {
		{ { "spawn",   "<monsterId> [count] [radius]", "내 주변에 몬스터를 세운다(리스폰되지 않는다)", "monster" }, &Spawn },
		{ { "killall", "[radius]",                     "맵(또는 반경) 안의 몬스터를 전부 처치한다",    ""        }, &KillAll },
		{ { "heal",    "",                             "체력을 최대치로 회복한다",                     ""        }, &Heal },
		{ { "hp",      "<value>",                      "체력을 지정한 값으로 만든다",                  ""        }, &Hp },
		{ { "die",     "",                             "내 캐릭터를 죽인다(부활 흐름 확인용)",         ""        }, &Die },
		{ { "god",     "[on|off]",                     "무적을 켜고 끈다(인자가 없으면 토글)",         ""        }, &God },
		{ { "level",   "<level>",                      "레벨을 지정한 값으로 세운다",                  ""        }, &SetLevel },
		{ { "exp",     "<amount>",                     "경험치를 지급한다",                            ""        }, &GainExp },
		{ { "gold",    "<amount>",                     "골드를 지급한다(음수면 차감)",                 ""        }, &Gold },
		{ { "item",    "<itemId> [count]",             "아이템을 지급한다",                            "item"    }, &GiveItem },
		{ { "skill",   "<skillId>",                    "스킬 하나를 습득한다",                         "skill"   }, &LearnSkill },
		{ { "allskill","",                             "플레이어가 쓸 수 있는 모든 스킬을 습득한다",   ""        }, &AllSkill },
		{ { "quest",   "<accept|complete|reset> <questId>", "퀘스트 상태를 직접 세운다",               "quest"   }, &QuestCheat },
		{ { "tp",      "<x> [y] <z>",                  "같은 맵 안에서 좌표로 순간이동한다",           ""        }, &Teleport },
		{ { "goto",    "<mapId>",                      "다른 맵으로 이동한다",                         "map"     }, &Goto },
		{ { "where",   "",                             "지금 맵/좌표/체력/레벨을 보여 준다",           ""        }, &Where },
		{ { "list",    "<monster|item|skill|map|quest> [검색어]", "데이터 id 를 찾아본다",              ""        }, &List },
	};

	// help 는 표를 읽어 주는 것이라 표 밖에 둔다(자기 자신을 목록에 넣지 않는다).
	std::string HelpText(std::string_view topic)
	{
		if (!topic.empty())
		{
			for (const Command& command : kCommands)
			{
				if (topic != command.info.name)
					continue;

				std::string line = "/" + std::string(command.info.name);
				if (command.info.args[0] != '\0')
					line += " " + std::string(command.info.args);
				return line + "\n  " + command.info.help;
			}
			return "치트 help: 모르는 명령 '" + std::string(topic) + "'.";
		}

		std::string text = "치트 목록(앞의 '/' 는 붙여도 되고 안 붙여도 됩니다):";
		for (const Command& command : kCommands)
		{
			text += "\n  " + std::string(command.info.name);
			if (command.info.args[0] != '\0')
				text += " " + std::string(command.info.args);
			text += "  - " + std::string(command.info.help);
		}
		return text;
	}
}

namespace cheat
{

const std::vector<CommandInfo>& Commands()
{
	// 표는 상수라 한 번만 옮겨 담는다. 돌려주는 쪽(치트 목록 패킷)은 매번 이걸 읽는다.
	static const std::vector<CommandInfo> commands = []
		{
			std::vector<CommandInfo> list;
			list.reserve(std::size(kCommands));
			for (const Command& command : kCommands)
				list.push_back(command.info);
			return list;
		}();

	return commands;
}

Result Execute(GameObject* player, std::string_view line)
{
	Result result;

	if (player == nullptr)
	{
		result.reply = "치트: 캐릭터가 없어 실행할 수 없습니다.";
		return result;
	}

	std::string_view text = Trim(line);
	if (!text.empty() && text.front() == '/')
		text = Trim(text.substr(1));

	if (text.empty())
		return result; // 빈 줄은 조용히 무시한다(엔터만 친 경우)

	std::vector<std::string_view> words = Split(text);
	if (words.empty())
		return result;

	const std::string_view name = words.front();

	if (name == "help" || name == "?")
	{
		result.handled = true;
		result.reply = HelpText(words.size() > 1 ? words[1] : std::string_view());
		return result;
	}

	for (const Command& command : kCommands)
	{
		if (name != command.info.name)
			continue;

		Context ctx;
		ctx.owner = player;
		ctx.player = dynamic_cast<Player*>(player);
		ctx.args.assign(words.begin() + 1, words.end());

		if (ctx.player != nullptr)
		{
			if (auto& character = ctx.player->GetCharacter())
			{
				ctx.character = character.get();
				ctx.map = character->GetMap();
			}
		}

		// 치트는 운영에서 열려 있으면 안 되는 기능이라, 실행 사실 자체를 warn 으로 남긴다.
		LOG.warn("치트 실행: '{}' (player {})", std::string(text),
			ctx.player != nullptr ? ctx.player->GetPlayerId() : 0);

		return command.run(ctx);
	}

	result.reply = "알 수 없는 치트: '" + std::string(name) + "'. 'help' 로 목록을 봅니다.";
	return result;
}

} // namespace cheat
