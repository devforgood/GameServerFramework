#pragma once

#include <string>
#include <unordered_map>
#include <vector>

#include "WorldView.h"

namespace bot
{
	//-----------------------------------------------------------------------------------
	// 봇이 시나리오를 진행하는 데 필요한 게임 데이터.
	//
	// 서버와 같은 파일(Client/Assets/Resources/GameData/)을 읽는다 — 봇 전용 사본을 두면
	// 데이터가 바뀔 때마다 둘이 어긋나고, 그 어긋남은 "봇이 아무 데도 안 간다"로만 나타나
	// 원인을 찾기 어렵다.
	//
	// 실행 중에는 바뀌지 않으므로 러너가 하나만 만들어 모든 봇이 const 참조로 공유한다
	// (봇마다 파싱하면 수백 개의 같은 사본이 생긴다).
	//-----------------------------------------------------------------------------------

	// quest.json 의 stages[].objectives[].type. Engine/Quest/QuestObjective.h 와 같은 이름이며,
	// 봇은 이 가운데 자기가 스스로 진행할 수 있는 것만 실제로 다룬다.
	enum class ObjectiveKind
	{
		None = 0,
		Kill,
		Collect,
		UseItem,
		UseSkill,
		Reach,
		Talk,
		Interact,
		Level,
		Escort,
		Protect,
	};

	ObjectiveKind ParseObjectiveKind(const std::string& type);

	struct ScenarioObjective
	{
		ObjectiveKind kind = ObjectiveKind::None;
		int target_id = 0;
		int count = 1;
	};

	struct ScenarioStage
	{
		// logic 이 "or" 면 목표 하나만 채워도 스테이지가 끝난다.
		bool any_of = false;
		std::vector<ScenarioObjective> objectives;
	};

	struct ScenarioQuest
	{
		int id = 0;
		int chain_id = 0;
		int chain_step = 0;
		int min_level = 0;
		int map_id = 0;
		int start_npc_id = 0;
		int end_npc_id = 0;
		bool main_quest = false;
		bool auto_complete = false;
		bool disabled = false;

		// 선택 보상 개수. 0 이 아니면 대화만으로는 완료되지 않는다 — 서버가 무엇을 고를지
		// 알 수 없어 거절하므로, 클라이언트처럼 QuestComplete 에 번호를 실어 보내야 한다.
		int reward_choice_count = 0;

		std::vector<int> requires_completed;
		std::vector<int> blocked_by;   // 분기: 이쪽을 하면 저쪽을 못 한다
		std::vector<ScenarioStage> stages;
	};

	struct ScenarioNpc
	{
		int id = 0;
		int map_id = 0;
		int dialog_id = 0;
		Vec3 pos;
		float interact_range = 3.0f;
	};

	struct ScenarioChoice
	{
		std::string text_id;
		std::string action;   // close | goto | accept_quest | complete_quest
		int param = 0;
		int next_id = 0;
	};

	struct ScenarioDialogNode
	{
		int id = 0;
		int npc_id = 0;
		std::vector<ScenarioChoice> choices;
	};

	struct ScenarioSpawn
	{
		int map_id = 0;
		int monster_id = 0;
		Vec3 pos;
		float radius = 0.0f;
	};

	struct ScenarioGate
	{
		int id = 0;
		int map_id = 0;
		int target_id = 0;        // 도착 지점 마커 id(다른 맵의 게이트 또는 player_spawn)
		int required_level = 1;
		Vec3 pos;
	};

	class BotScenario
	{
	public:
		// gamedata_dir 이 비어 있으면 GameDataPath::Resolve() 가 찾은 통합 폴더를 쓴다.
		// 파일이 없거나 형식이 깨졌으면 false 와 함께 error 를 채운다.
		bool Load(const std::string& gamedata_dir, std::string& error);
		bool Loaded() const { return loaded_; }

		const ScenarioQuest* FindQuest(int quest_id) const;
		const ScenarioNpc* FindNpc(int npc_id) const;
		const ScenarioDialogNode* FindDialog(int node_id) const;
		const ScenarioGate* FindGate(int gate_id) const;

		// 메인 퀘스트 체인 목록(오름차순). 봇은 이 가운데 하나를 골라 처음부터 진행한다.
		const std::vector<int>& MainChains() const { return main_chains_; }

		// branch_index 로 가지를 고른 진행 순서.
		//   - 어느 체인을 탈지: branch_index % 체인 수
		//   - 같은 자리에 놓인 분기 퀘스트 중 무엇을 할지: (branch_index / 체인 수) 로 고른다
		// 두 자리를 나눠 써야 봇을 늘렸을 때 체인과 가지가 함께 골고루 퍼진다.
		std::vector<int> BuildMainQuestPlan(int branch_index) const;

		// 사냥터. prefer_map_id 에 있는 자리를 우선 고른다(맵 이동을 줄인다).
		const ScenarioSpawn* FindHuntSpot(int monster_id, int prefer_map_id) const;

		// 이 아이템을 떨구는 몬스터의 사냥터. 수집 목표는 처치로만 채울 수 있다.
		const ScenarioSpawn* FindItemSource(int item_id, int prefer_map_id) const;

		// 아무 사냥터나(레벨 올리기용). 없으면 nullptr.
		const ScenarioSpawn* FindAnySpot(int map_id) const;

		// from_map 에서 to_map 으로 가려면 지금 맵에서 어떤 게이트를 밟아야 하는가.
		// 같은 맵이거나 길이 없으면 nullptr.
		const ScenarioGate* NextGate(int from_map_id, int to_map_id) const;

		// 이 맵에서 밖으로 나가는 게이트 아무거나. 이미 도착해 있는 맵에 "도달" 목표가
		// 걸렸을 때(서버는 맵에 들어오는 순간에만 세어 준다) 한 번 나갔다 오는 데 쓴다.
		const ScenarioGate* FindAnyGate(int map_id) const;

		// 지금 노드에서 (action, quest_id) 를 실행하려면 몇 번 선택지를 눌러야 하는가.
		// 그 자리에 있으면 그 선택지를, 없으면 그쪽으로 가는 goto 선택지의 번호를 돌려준다.
		// 돌려주는 값은 '데이터의 번호'다 — 서버는 조건에 걸러진 목록만 보내므로,
		// 부르는 쪽이 text_id 로 짝을 지어 보이는 번호로 바꿔야 한다.
		int NextDialogChoice(int node_id, const std::string& action, int quest_id) const;

	private:
		bool LoadQuests(const std::string& path, std::string& error);
		bool LoadNpcs(const std::string& path, std::string& error);
		bool LoadDialogs(const std::string& path, std::string& error);
		bool LoadMaps(const std::string& path, std::string& error);
		bool LoadMonsters(const std::string& path, std::string& error);
		void BuildRoutes();

		const ScenarioSpawn* PickSpawn(const std::vector<int>& indices, int prefer_map_id) const;

		bool loaded_ = false;

		std::unordered_map<int, ScenarioQuest> quests_;
		std::unordered_map<int, ScenarioNpc> npcs_;
		std::unordered_map<int, ScenarioDialogNode> dialogs_;
		std::unordered_map<int, ScenarioGate> gates_;

		std::vector<ScenarioSpawn> spawns_;
		std::unordered_map<int, std::vector<int>> spawns_by_monster_;  // monster_id -> spawns_ 번호
		std::unordered_map<int, std::vector<int>> spawns_by_map_;      // map_id -> spawns_ 번호
		std::unordered_map<int, std::vector<int>> monsters_by_drop_;   // item_id -> monster_id

		// 마커 id(게이트/스폰 지점) -> 그 마커가 있는 맵. 게이트의 target_id 가 어느 맵을
		// 가리키는지는 이것으로만 알 수 있다(게이트는 도착 '맵'이 아니라 '마커'를 가리킨다).
		std::unordered_map<int, int> marker_map_;

		// (from_map, to_map) -> 밟아야 할 게이트 id. 맵이 몇 개 안 되므로 미리 다 만들어 둔다.
		std::unordered_map<long long, int> routes_;

		std::vector<int> main_chains_;
		// chain_id -> (chain_step -> 그 자리의 퀘스트 id 들. 둘 이상이면 분기다)
		std::unordered_map<int, std::vector<std::pair<int, std::vector<int>>>> chain_steps_;
	};
}
