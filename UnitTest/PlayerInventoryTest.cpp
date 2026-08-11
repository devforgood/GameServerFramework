#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include <fstream>
#include <sstream>
#include "PlayerItem.h"
#include "PlayerSkill.h"
#include "PlayerWallet.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "DbRecord.h"
#include "GameObject.h"
#include "PlayerEventBroker.h"
#include "EventMessage.h"
#include "GameData/ResourceLoader.h"
#include "SqlScript.h"

// 인벤토리/보유 스킬/지갑의 게임 로직과 DB 왕복.
// 실제 DB 없이 PlayerLoadData -> Load -> 로직 -> Save -> PlayerSaveData 흐름만 검증한다.

namespace
{

constexpr int kPotionSmall = 1;      // item.json consumable
constexpr int kPotionMedium = 2;
constexpr int kSword = 4;            // equipment
constexpr int kGoblinEar = 100;      // quest 아이템, quest_id 1001
constexpr int kWolfPelt = 101;       // quest 아이템, quest_id 1003
constexpr int kGoblinHunt = 1001;
constexpr int kNormalAttackSkill = 1;
constexpr int kJumpSkill = 2;

void EnsureResources()
{
	const std::string& path = GameDataPath::Resolve();
	ASSERT_TRUE(std::filesystem::exists(path + "item.json"))
		<< "통합 GameData 폴더를 찾지 못했습니다: " << path;
	ASSERT_TRUE(ResourceLoader::Instance().LoadResources(path)) << "LoadResources 실패";
}

PlayerLoadData MakeLoadData(int character_id = 1001)
{
	PlayerLoadData data{};
	data.player.id = character_id;
	data.player.name = "Tester";
	data.player.level = 10;
	return data;
}

class PlayerInventoryTest : public ::testing::Test
{
protected:
	void SetUp() override { EnsureResources(); }
};

} // namespace

// ============================================================
// PlayerItem
// ============================================================

TEST_F(PlayerInventoryTest, Item_AddStacksAndReports)
{
	PlayerItem inventory;
	inventory.Load(MakeLoadData());

	EXPECT_EQ(inventory.AddItem(kPotionSmall, 3), 3);
	EXPECT_EQ(inventory.AddItem(kPotionSmall, 2), 2);
	EXPECT_EQ(inventory.GetCount(kPotionSmall), 5);
	EXPECT_EQ(inventory.DistinctCount(), 1u);
	EXPECT_TRUE(inventory.Has(kPotionSmall, 5));
	EXPECT_FALSE(inventory.Has(kPotionSmall, 6));
}

TEST_F(PlayerInventoryTest, Item_UnknownItemIsRejected)
{
	PlayerItem inventory;
	inventory.Load(MakeLoadData());

	// 데이터에 없는 id 는 넣지 않는다(이름조차 못 붙이는 유령 아이템 방지).
	EXPECT_EQ(inventory.AddItem(999999, 1), 0);
	EXPECT_EQ(inventory.DistinctCount(), 0u);
}

TEST_F(PlayerInventoryTest, Item_RemoveIsAllOrNothing)
{
	PlayerItem inventory;
	inventory.Load(MakeLoadData());
	inventory.AddItem(kPotionSmall, 3);

	// 모자라면 부분 차감하지 않는다.
	EXPECT_EQ(inventory.RemoveItem(kPotionSmall, 5), 0);
	EXPECT_EQ(inventory.GetCount(kPotionSmall), 3);

	EXPECT_EQ(inventory.RemoveItem(kPotionSmall, 3), 3);
	EXPECT_EQ(inventory.GetCount(kPotionSmall), 0);
	EXPECT_EQ(inventory.DistinctCount(), 0u); // 0 이 된 줄은 남기지 않는다
}

TEST_F(PlayerInventoryTest, Item_AddPublishesAcquiredEvent)
{
	struct Listener
	{
		int item_id = 0;
		int count = 0;
		void OnAcquired(const EventItemAcquired& msg) { item_id = msg.item_id; count += msg.count; }
	} listener;

	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerItem* inventory = go.AddComponent<PlayerItem>();
	go.GetComponent<PlayerEventBroker>()
		->subscribe<Listener, EventItemAcquired, &Listener::OnAcquired>(&listener);

	inventory->Load(MakeLoadData());
	inventory->AddItem(kPotionSmall, 4);

	EXPECT_EQ(listener.item_id, kPotionSmall);
	EXPECT_EQ(listener.count, 4);
}

TEST_F(PlayerInventoryTest, Item_UsePublishesUsedEventOnlyOnSuccess)
{
	struct Listener
	{
		int used = 0;
		void OnUsed(const EventItemUsed& msg) { used += msg.count; }
	} listener;

	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerItem* inventory = go.AddComponent<PlayerItem>();
	go.GetComponent<PlayerEventBroker>()
		->subscribe<Listener, EventItemUsed, &Listener::OnUsed>(&listener);

	inventory->Load(MakeLoadData());
	inventory->AddItem(kPotionSmall, 1);

	EXPECT_TRUE(inventory->UseItem(kPotionSmall));
	EXPECT_EQ(listener.used, 1);

	// 이제 없다 — 실패하면 이벤트도 나가면 안 된다.
	EXPECT_FALSE(inventory->UseItem(kPotionSmall));
	EXPECT_EQ(listener.used, 1);
}

TEST_F(PlayerInventoryTest, Item_RemoveQuestItemsOnlyTouchesThatQuest)
{
	PlayerItem inventory;
	inventory.Load(MakeLoadData());
	inventory.AddItem(kGoblinEar, 5);   // quest 1001
	inventory.AddItem(kWolfPelt, 3);    // quest 1003
	inventory.AddItem(kPotionSmall, 2); // 일반 아이템

	EXPECT_EQ(inventory.RemoveQuestItems(kGoblinHunt), 1);

	EXPECT_EQ(inventory.GetCount(kGoblinEar), 0);
	EXPECT_EQ(inventory.GetCount(kWolfPelt), 3);
	EXPECT_EQ(inventory.GetCount(kPotionSmall), 2);
}

TEST_F(PlayerInventoryTest, Item_SaveEmitsInsertUpdateDelete)
{
	// 이미 DB 에 있는 줄 하나로 시작한다.
	PlayerLoadData load = MakeLoadData();
	PlayerItemVO existing{};
	existing.character_id = 1001;
	existing.item_id = kPotionSmall;
	existing.count = 2;
	load.items.push_back(existing);

	PlayerItem inventory;
	inventory.Load(load);
	EXPECT_FALSE(inventory.IsDirty());

	inventory.AddItem(kPotionSmall, 1);   // 기존 줄 -> UPDATE
	inventory.AddItem(kSword, 1);         // 새 줄  -> INSERT
	load.items.clear();

	PlayerSaveData saved{};
	inventory.Save(&saved);

	ASSERT_TRUE(saved.items.has_value());
	ASSERT_EQ(saved.items->size(), 2u);

	const DbRecord<PlayerItemVO>* update = nullptr;
	const DbRecord<PlayerItemVO>* insert = nullptr;
	for (const auto& record : *saved.items)
	{
		if (record.action == DbAction::Update) update = &record;
		if (record.action == DbAction::Insert) insert = &record;
	}
	ASSERT_NE(update, nullptr);
	EXPECT_EQ(update->vo.item_id, kPotionSmall);
	EXPECT_EQ(update->vo.count, 3);
	ASSERT_NE(insert, nullptr);
	EXPECT_EQ(insert->vo.item_id, kSword);
}

TEST_F(PlayerInventoryTest, Item_ExhaustedRowBecomesDelete)
{
	PlayerLoadData load = MakeLoadData();
	PlayerItemVO existing{};
	existing.character_id = 1001;
	existing.item_id = kPotionMedium;
	existing.count = 1;
	load.items.push_back(existing);

	PlayerItem inventory;
	inventory.Load(load);
	ASSERT_TRUE(inventory.UseItem(kPotionMedium));

	PlayerSaveData saved{};
	inventory.Save(&saved);

	ASSERT_TRUE(saved.items.has_value());
	ASSERT_EQ(saved.items->size(), 1u);
	EXPECT_EQ((*saved.items)[0].action, DbAction::Remove);
	EXPECT_EQ((*saved.items)[0].vo.item_id, kPotionMedium);
}

TEST_F(PlayerInventoryTest, Item_NoChangeWritesNothing)
{
	PlayerLoadData load = MakeLoadData();
	PlayerItemVO existing{};
	existing.character_id = 1001;
	existing.item_id = kPotionSmall;
	existing.count = 2;
	load.items.push_back(existing);

	PlayerItem inventory;
	inventory.Load(load);

	PlayerSaveData saved{};
	inventory.Save(&saved);

	if (saved.items.has_value())
		EXPECT_TRUE(saved.items->empty());
}

// ============================================================
// PlayerSkill
// ============================================================

TEST_F(PlayerInventoryTest, Skill_LearnAndQuery)
{
	PlayerSkill skills;
	skills.Load(MakeLoadData());

	EXPECT_FALSE(skills.Has(kNormalAttackSkill));
	EXPECT_TRUE(skills.LearnSkill(kNormalAttackSkill));
	EXPECT_TRUE(skills.Has(kNormalAttackSkill));
	EXPECT_EQ(skills.GetLevel(kNormalAttackSkill), 1);

	// 이미 배운 스킬은 새로 배운 것이 아니다.
	EXPECT_FALSE(skills.LearnSkill(kNormalAttackSkill));
	EXPECT_EQ(skills.Count(), 1u);
}

TEST_F(PlayerInventoryTest, Skill_LevelOnlyGoesUp)
{
	PlayerSkill skills;
	skills.Load(MakeLoadData());
	skills.LearnSkill(kJumpSkill, 5);

	skills.LearnSkill(kJumpSkill, 2);
	EXPECT_EQ(skills.GetLevel(kJumpSkill), 5);

	skills.LearnSkill(kJumpSkill, 7);
	EXPECT_EQ(skills.GetLevel(kJumpSkill), 7);
}

TEST_F(PlayerInventoryTest, Skill_UnknownSkillIsRejected)
{
	PlayerSkill skills;
	skills.Load(MakeLoadData());

	EXPECT_FALSE(skills.LearnSkill(999999));
	EXPECT_EQ(skills.Count(), 0u);
}

TEST_F(PlayerInventoryTest, Skill_SaveEmitsInsert)
{
	PlayerSkill skills;
	skills.Load(MakeLoadData());
	skills.LearnSkill(kNormalAttackSkill);

	PlayerSaveData saved{};
	skills.Save(&saved);

	ASSERT_TRUE(saved.skills.has_value());
	ASSERT_EQ(saved.skills->size(), 1u);
	EXPECT_EQ((*saved.skills)[0].action, DbAction::Insert);
	EXPECT_EQ((*saved.skills)[0].vo.skill_id, kNormalAttackSkill);
}

// ============================================================
// PlayerWallet
// ============================================================

TEST_F(PlayerInventoryTest, Wallet_AddAndSpend)
{
	PlayerWallet wallet;
	wallet.Load(MakeLoadData());

	EXPECT_EQ(wallet.GetGold(), 0);
	EXPECT_EQ(wallet.AddGold(500), 500);
	EXPECT_EQ(wallet.GetGold(), 500);

	EXPECT_FALSE(wallet.SpendGold(600)); // 잔액 부족 -> 아무 일도 없다
	EXPECT_EQ(wallet.GetGold(), 500);

	EXPECT_TRUE(wallet.SpendGold(200));
	EXPECT_EQ(wallet.GetGold(), 300);
}

TEST_F(PlayerInventoryTest, Wallet_NewRowInsertsThenUpdates)
{
	PlayerWallet wallet;
	wallet.Load(MakeLoadData());

	PlayerSaveData first{};
	wallet.Save(&first);
	ASSERT_TRUE(first.wallet.has_value());
	EXPECT_EQ(first.wallet->action, DbAction::Insert);

	wallet.AddGold(100);
	PlayerSaveData second{};
	wallet.Save(&second);
	ASSERT_TRUE(second.wallet.has_value());
	EXPECT_EQ(second.wallet->action, DbAction::Update);
	EXPECT_EQ(second.wallet->vo.gold, 100);
}

TEST_F(PlayerInventoryTest, Wallet_LoadsExistingBalance)
{
	PlayerLoadData load = MakeLoadData();
	load.wallet.character_id = 1001;
	load.wallet.gold = 12345;

	PlayerWallet wallet;
	wallet.Load(load);
	EXPECT_EQ(wallet.GetGold(), 12345);

	// 변경이 없으면 아무것도 쓰지 않는다.
	PlayerSaveData saved{};
	wallet.Save(&saved);
	EXPECT_FALSE(saved.wallet.has_value());
}

// ============================================================
// create_tables.sql 문장 분리
// (커넥션에 allowMultiQueries 가 없어 한 문장씩 실행해야 한다)
// ============================================================

TEST(SqlScriptTest, SplitsStatementsAndDropsComments)
{
	const std::string script =
		"-- 주석만 있는 줄\n"
		"CREATE TABLE IF NOT EXISTS `a`\n"
		"(\n"
		"    `id` INT NOT NULL\n"
		");\n"
		"\n"
		"-- Reconcile columns\n"
		"ALTER TABLE `a` ADD COLUMN IF NOT EXISTS `b` INT NOT NULL DEFAULT 0;\n";

	const auto statements = sql_script::Split(script);

	ASSERT_EQ(statements.size(), 2u);
	EXPECT_NE(statements[0].find("CREATE TABLE"), std::string::npos);
	EXPECT_EQ(statements[0].find("--"), std::string::npos);
	EXPECT_NE(statements[1].find("ALTER TABLE"), std::string::npos);
	EXPECT_EQ(statements[1].find(';'), std::string::npos);
}

TEST(SqlScriptTest, EmptyAndCommentOnlyScriptsYieldNothing)
{
	EXPECT_TRUE(sql_script::Split("").empty());
	EXPECT_TRUE(sql_script::Split("-- nothing here\n\n   \n").empty());
	EXPECT_TRUE(sql_script::Split(";;;\n").empty());
}

TEST(SqlScriptTest, GeneratedScriptSplitsIntoRunnableStatements)
{
	// 실제 생성물로도 확인한다. 문장이 하나로 뭉쳐 나오면 드라이버가 거부한다.
	std::ifstream file(std::string(GameDataPath::Resolve()) + "../../../../Engine/SQL/generated/create_tables.sql");
	ASSERT_TRUE(file.is_open()) << "생성된 create_tables.sql 을 찾지 못했습니다";

	std::stringstream buffer;
	buffer << file.rdbuf();
	const auto statements = sql_script::Split(buffer.str());

	ASSERT_FALSE(statements.empty());
	for (const auto& statement : statements)
	{
		EXPECT_EQ(statement.find(';'), std::string::npos)
			<< "문장 안에 세미콜론이 남아 있습니다: " << statement;
		EXPECT_TRUE(statement.rfind("CREATE", 0) == 0 || statement.rfind("ALTER", 0) == 0)
			<< "예상치 못한 문장: " << statement;
	}
}
