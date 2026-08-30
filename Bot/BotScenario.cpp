#include "BotScenario.h"

#include <nlohmann/json.hpp>

#include <algorithm>
#include <deque>
#include <fstream>

#include "../Engine/GameData/GameDataPath.h"

namespace bot
{
	namespace
	{
		using json = nlohmann::json;

		// 게임 데이터에는 null 이 없어야 하지만(클라 JsonUtility 가 통째로 실패한다),
		// 들어와도 봇이 죽지는 않게 기본값으로 넘긴다.
		int ReadInt(const json& obj, const char* key, int fallback = 0)
		{
			if (!obj.is_object())
				return fallback;
			auto it = obj.find(key);
			if (it == obj.end() || !it->is_number())
				return fallback;
			return it->get<int>();
		}

		float ReadFloat(const json& obj, const char* key, float fallback = 0.0f)
		{
			if (!obj.is_object())
				return fallback;
			auto it = obj.find(key);
			if (it == obj.end() || !it->is_number())
				return fallback;
			return it->get<float>();
		}

		bool ReadBool(const json& obj, const char* key, bool fallback = false)
		{
			if (!obj.is_object())
				return fallback;
			auto it = obj.find(key);
			if (it == obj.end() || !it->is_boolean())
				return fallback;
			return it->get<bool>();
		}

		std::string ReadString(const json& obj, const char* key)
		{
			if (!obj.is_object())
				return {};
			auto it = obj.find(key);
			if (it == obj.end() || !it->is_string())
				return {};
			return it->get<std::string>();
		}

		Vec3 ReadPos(const json& obj, const char* key)
		{
			if (!obj.is_object())
				return {};
			auto it = obj.find(key);
			if (it == obj.end() || !it->is_object())
				return {};
			return Vec3(ReadFloat(*it, "x"), ReadFloat(*it, "y"), ReadFloat(*it, "z"));
		}

		std::vector<int> ReadIntArray(const json& obj, const char* key)
		{
			std::vector<int> out;
			if (!obj.is_object())
				return out;
			auto it = obj.find(key);
			if (it == obj.end() || !it->is_array())
				return out;
			for (const json& value : *it)
			{
				if (value.is_number_integer())
					out.push_back(value.get<int>());
			}
			return out;
		}

		bool ReadJsonFile(const std::string& path, json& out, std::string& error)
		{
			std::ifstream file(path);
			if (!file.is_open())
			{
				error = "파일을 열 수 없다: " + path;
				return false;
			}

			try
			{
				file >> out;
			}
			catch (const std::exception& e)
			{
				error = path + " 파싱 실패: " + e.what();
				return false;
			}

			if (!out.is_array())
			{
				error = path + " 의 최상위가 배열이 아니다";
				return false;
			}
			return true;
		}

		long long RouteKey(int from_map_id, int to_map_id)
		{
			return (static_cast<long long>(from_map_id) << 32) | static_cast<unsigned int>(to_map_id);
		}
	}

	ObjectiveKind ParseObjectiveKind(const std::string& type)
	{
		if (type == "kill")      return ObjectiveKind::Kill;
		if (type == "collect")   return ObjectiveKind::Collect;
		if (type == "use_item")  return ObjectiveKind::UseItem;
		if (type == "use_skill") return ObjectiveKind::UseSkill;
		if (type == "reach")     return ObjectiveKind::Reach;
		if (type == "talk")      return ObjectiveKind::Talk;
		if (type == "interact")  return ObjectiveKind::Interact;
		if (type == "level")     return ObjectiveKind::Level;
		if (type == "escort")    return ObjectiveKind::Escort;
		if (type == "protect")   return ObjectiveKind::Protect;
		return ObjectiveKind::None;
	}

	bool BotScenario::Load(const std::string& gamedata_dir, std::string& error)
	{
		std::string dir = gamedata_dir.empty() ? GameDataPath::Resolve() : gamedata_dir;
		if (!dir.empty() && dir.back() != '/' && dir.back() != '\\')
			dir += '/';

		if (!LoadQuests(dir + "quest.json", error)) return false;
		if (!LoadNpcs(dir + "npc.json", error)) return false;
		if (!LoadDialogs(dir + "dialog.json", error)) return false;
		if (!LoadMaps(dir + "Map.json", error)) return false;
		if (!LoadMonsters(dir + "monster.json", error)) return false;

		BuildRoutes();

		if (main_chains_.empty())
		{
			error = "quest.json 에 메인 퀘스트 체인이 없다";
			return false;
		}

		loaded_ = true;
		return true;
	}

	bool BotScenario::LoadQuests(const std::string& path, std::string& error)
	{
		json root;
		if (!ReadJsonFile(path, root, error))
			return false;

		// chain_id -> chain_step -> 퀘스트들. 같은 자리에 둘 이상이면 분기다.
		std::unordered_map<int, std::unordered_map<int, std::vector<int>>> chains;

		for (const json& entry : root)
		{
			if (!entry.is_object())
				continue;

			ScenarioQuest quest;
			quest.id = ReadInt(entry, "id");
			if (quest.id == 0)
				continue;

			quest.chain_id = ReadInt(entry, "chain_id");
			quest.chain_step = ReadInt(entry, "chain_step");
			quest.min_level = ReadInt(entry, "min_level");
			quest.map_id = ReadInt(entry, "map_id");
			quest.start_npc_id = ReadInt(entry, "start_npc_id");
			quest.end_npc_id = ReadInt(entry, "end_npc_id");
			quest.main_quest = ReadString(entry, "code_name") == "MainQuest";
			quest.auto_complete = ReadBool(entry, "auto_complete");
			quest.disabled = ReadBool(entry, "disabled");

			if (auto prerequisites = entry.find("prerequisites");
				prerequisites != entry.end() && prerequisites->is_object())
			{
				quest.requires_completed = ReadIntArray(*prerequisites, "completed_quest_ids");
				quest.blocked_by = ReadIntArray(*prerequisites, "blocked_quest_ids");
			}

			if (auto rewards = entry.find("rewards");
				rewards != entry.end() && rewards->is_object())
			{
				if (auto choices = rewards->find("choice_items");
					choices != rewards->end() && choices->is_array())
				{
					quest.reward_choice_count = static_cast<int>(choices->size());
				}
			}

			if (auto stages = entry.find("stages"); stages != entry.end() && stages->is_array())
			{
				for (const json& stage_json : *stages)
				{
					ScenarioStage stage;
					stage.any_of = ReadString(stage_json, "logic") == "or";

					if (auto objectives = stage_json.find("objectives");
						objectives != stage_json.end() && objectives->is_array())
					{
						for (const json& objective_json : *objectives)
						{
							ScenarioObjective objective;
							objective.kind = ParseObjectiveKind(ReadString(objective_json, "type"));
							objective.target_id = ReadInt(objective_json, "target_id");
							objective.count = ReadInt(objective_json, "count", 1);
							stage.objectives.push_back(objective);
						}
					}

					// 진행도 칸이 3개뿐이라 서버도 스테이지당 목표를 3개까지만 본다.
					if (stage.objectives.size() > 3)
						stage.objectives.resize(3);

					quest.stages.push_back(std::move(stage));
				}
			}

			if (quest.chain_id != 0 && quest.main_quest)
				chains[quest.chain_id][quest.chain_step].push_back(quest.id);

			quests_[quest.id] = std::move(quest);
		}

		for (const auto& [chain_id, steps] : chains)
		{
			main_chains_.push_back(chain_id);

			std::vector<std::pair<int, std::vector<int>>> ordered;
			ordered.reserve(steps.size());
			for (const auto& [step, quest_ids] : steps)
			{
				std::vector<int> sorted_ids = quest_ids;
				std::sort(sorted_ids.begin(), sorted_ids.end());
				ordered.emplace_back(step, std::move(sorted_ids));
			}

			std::sort(ordered.begin(), ordered.end(),
				[](const auto& a, const auto& b) { return a.first < b.first; });
			chain_steps_[chain_id] = std::move(ordered);
		}

		// 봇 번호로 가지를 고르므로 순서가 실행마다 같아야 한다(unordered_map 은 아니다).
		std::sort(main_chains_.begin(), main_chains_.end());
		return true;
	}

	bool BotScenario::LoadNpcs(const std::string& path, std::string& error)
	{
		json root;
		if (!ReadJsonFile(path, root, error))
			return false;

		for (const json& entry : root)
		{
			ScenarioNpc npc;
			npc.id = ReadInt(entry, "id");
			if (npc.id == 0)
				continue;

			npc.map_id = ReadInt(entry, "map_id");
			npc.dialog_id = ReadInt(entry, "dialog_id");
			npc.pos = ReadPos(entry, "position");
			npc.interact_range = ReadFloat(entry, "interact_range", 3.0f);
			npcs_[npc.id] = npc;
		}
		return true;
	}

	bool BotScenario::LoadDialogs(const std::string& path, std::string& error)
	{
		json root;
		if (!ReadJsonFile(path, root, error))
			return false;

		for (const json& entry : root)
		{
			ScenarioDialogNode node;
			node.id = ReadInt(entry, "id");
			if (node.id == 0)
				continue;

			node.npc_id = ReadInt(entry, "npc_id");

			if (auto choices = entry.find("choices"); choices != entry.end() && choices->is_array())
			{
				for (const json& choice_json : *choices)
				{
					ScenarioChoice choice;
					choice.text_id = ReadString(choice_json, "text_id");
					choice.action = ReadString(choice_json, "action");
					choice.param = ReadInt(choice_json, "param");
					choice.next_id = ReadInt(choice_json, "next_id");
					choice.reward_choice = ReadInt(choice_json, "reward_choice", -1);
					node.choices.push_back(std::move(choice));
				}
			}

			dialogs_[node.id] = std::move(node);
		}
		return true;
	}

	bool BotScenario::LoadMaps(const std::string& path, std::string& error)
	{
		json root;
		if (!ReadJsonFile(path, root, error))
			return false;

		for (const json& entry : root)
		{
			const int map_id = ReadInt(entry, "id");
			if (map_id == 0)
				continue;

			if (auto gates = entry.find("gates"); gates != entry.end() && gates->is_array())
			{
				for (const json& gate_json : *gates)
				{
					ScenarioGate gate;
					gate.id = ReadInt(gate_json, "id");
					if (gate.id == 0)
						continue;

					gate.map_id = map_id;
					gate.target_id = ReadInt(gate_json, "target_id");
					gate.required_level = ReadInt(gate_json, "required_level", 1);
					gate.pos = ReadPos(gate_json, "position");

					marker_map_[gate.id] = map_id;
					gates_[gate.id] = gate;
				}
			}

			auto spawn_points = entry.find("spawn_points");
			if (spawn_points == entry.end() || !spawn_points->is_object())
				continue;

			for (const char* group : { "player_spawn", "monster_spawn", "boss_spawn" })
			{
				auto list = spawn_points->find(group);
				if (list == spawn_points->end() || !list->is_array())
					continue;

				for (const json& spawn_json : *list)
				{
					const int marker_id = ReadInt(spawn_json, "id");
					if (marker_id != 0)
						marker_map_[marker_id] = map_id;

					// boss_spawn 은 monster_id 대신 boss_id 에 종류를 적는다.
					int monster_id = ReadInt(spawn_json, "monster_id");
					if (monster_id == 0)
						monster_id = ReadInt(spawn_json, "boss_id");
					if (monster_id == 0)
						continue;

					ScenarioSpawn spawn;
					spawn.map_id = map_id;
					spawn.monster_id = monster_id;
					spawn.pos = ReadPos(spawn_json, "position");
					spawn.radius = ReadFloat(spawn_json, "radius");

					const int index = static_cast<int>(spawns_.size());
					spawns_.push_back(spawn);
					spawns_by_monster_[monster_id].push_back(index);
					spawns_by_map_[map_id].push_back(index);
				}
			}
		}
		return true;
	}

	bool BotScenario::LoadMonsters(const std::string& path, std::string& error)
	{
		json root;
		if (!ReadJsonFile(path, root, error))
			return false;

		for (const json& entry : root)
		{
			const int monster_id = ReadInt(entry, "id");
			if (monster_id == 0)
				continue;

			auto drops = entry.find("drops");
			if (drops == entry.end() || !drops->is_array())
				continue;

			for (const json& drop : *drops)
			{
				const int item_id = ReadInt(drop, "item_id");
				if (item_id != 0)
					monsters_by_drop_[item_id].push_back(monster_id);
			}
		}
		return true;
	}

	void BotScenario::BuildRoutes()
	{
		// 게이트 하나가 간선 하나다(맵 -> 도착 마커가 있는 맵). 도착지마다 거꾸로 너비 우선
		// 탐색을 돌려 "이 맵에서 저 맵으로 가려면 어느 게이트" 를 전부 채워 둔다.
		// 맵이 몇 개 안 되므로 미리 만드는 편이 매번 탐색하는 것보다 단순하다.
		std::unordered_map<int, std::vector<const ScenarioGate*>> incoming;  // 도착 맵 -> 게이트
		std::vector<int> map_ids;

		for (const auto& [gate_id, gate] : gates_)
		{
			auto destination = marker_map_.find(gate.target_id);
			if (destination == marker_map_.end())
				continue;   // 끊어진 게이트(데이터 검증이 막는다)

			incoming[destination->second].push_back(&gate);
		}

		for (const auto& [map_id, spawn_indices] : spawns_by_map_)
			map_ids.push_back(map_id);
		for (const auto& [gate_id, gate] : gates_)
			map_ids.push_back(gate.map_id);

		std::sort(map_ids.begin(), map_ids.end());
		map_ids.erase(std::unique(map_ids.begin(), map_ids.end()), map_ids.end());

		for (int target_map : map_ids)
		{
			std::deque<int> queue{ target_map };
			std::unordered_map<int, bool> visited{ { target_map, true } };

			while (!queue.empty())
			{
				const int here = queue.front();
				queue.pop_front();

				auto gates = incoming.find(here);
				if (gates == incoming.end())
					continue;

				for (const ScenarioGate* gate : gates->second)
				{
					if (visited.find(gate->map_id) != visited.end())
						continue;

					visited[gate->map_id] = true;
					routes_[RouteKey(gate->map_id, target_map)] = gate->id;
					queue.push_back(gate->map_id);
				}
			}
		}
	}

	const ScenarioQuest* BotScenario::FindQuest(int quest_id) const
	{
		auto it = quests_.find(quest_id);
		return it != quests_.end() ? &it->second : nullptr;
	}

	const ScenarioNpc* BotScenario::FindNpc(int npc_id) const
	{
		auto it = npcs_.find(npc_id);
		return it != npcs_.end() ? &it->second : nullptr;
	}

	const ScenarioDialogNode* BotScenario::FindDialog(int node_id) const
	{
		auto it = dialogs_.find(node_id);
		return it != dialogs_.end() ? &it->second : nullptr;
	}

	const ScenarioGate* BotScenario::FindGate(int gate_id) const
	{
		auto it = gates_.find(gate_id);
		return it != gates_.end() ? &it->second : nullptr;
	}

	std::vector<int> BotScenario::BuildMainQuestPlan(int branch_index) const
	{
		std::vector<int> plan;
		if (main_chains_.empty())
			return plan;

		if (branch_index < 0)
			branch_index = -branch_index;

		const int chain_count = static_cast<int>(main_chains_.size());
		const int chain_id = main_chains_[branch_index % chain_count];
		const int branch_pick = branch_index / chain_count;

		auto steps = chain_steps_.find(chain_id);
		if (steps == chain_steps_.end())
			return plan;

		for (const auto& [step, quest_ids] : steps->second)
		{
			if (quest_ids.empty())
				continue;

			// 분기(같은 자리에 놓인 서로 배타적인 퀘스트)는 봇마다 다른 가지를 고른다.
			// 한 봇이 두 가지를 모두 할 수는 없다 — 서버가 blocked_quest_ids 로 막는다.
			plan.push_back(quest_ids[branch_pick % static_cast<int>(quest_ids.size())]);
		}
		return plan;
	}

	const ScenarioSpawn* BotScenario::PickSpawn(const std::vector<int>& indices, int prefer_map_id) const
	{
		const ScenarioSpawn* fallback = nullptr;
		for (int index : indices)
		{
			const ScenarioSpawn& spawn = spawns_[index];
			if (prefer_map_id != 0 && spawn.map_id == prefer_map_id)
				return &spawn;
			if (fallback == nullptr)
				fallback = &spawn;
		}
		return fallback;
	}

	const ScenarioSpawn* BotScenario::FindHuntSpot(int monster_id, int prefer_map_id) const
	{
		auto it = spawns_by_monster_.find(monster_id);
		if (it == spawns_by_monster_.end())
			return nullptr;
		return PickSpawn(it->second, prefer_map_id);
	}

	const ScenarioSpawn* BotScenario::FindItemSource(int item_id, int prefer_map_id) const
	{
		auto it = monsters_by_drop_.find(item_id);
		if (it == monsters_by_drop_.end())
			return nullptr;

		// 같은 맵에서 잡을 수 있는 몬스터를 먼저 본다. 없으면 아무 곳이나.
		for (int monster_id : it->second)
		{
			if (const ScenarioSpawn* spawn = FindHuntSpot(monster_id, prefer_map_id))
			{
				if (prefer_map_id == 0 || spawn->map_id == prefer_map_id)
					return spawn;
			}
		}

		for (int monster_id : it->second)
		{
			if (const ScenarioSpawn* spawn = FindHuntSpot(monster_id, 0))
				return spawn;
		}
		return nullptr;
	}

	void BotScenario::CollectHuntSpots(int monster_id, int prefer_map_id,
		std::vector<const ScenarioSpawn*>& out) const
	{
		out.clear();

		auto it = spawns_by_monster_.find(monster_id);
		if (it == spawns_by_monster_.end())
			return;

		for (int index : it->second)
		{
			const ScenarioSpawn& spawn = spawns_[index];
			if (prefer_map_id != 0 && spawn.map_id == prefer_map_id)
				out.insert(out.begin(), &spawn);
			else
				out.push_back(&spawn);
		}
	}

	void BotScenario::CollectItemSources(int item_id, int prefer_map_id,
		std::vector<const ScenarioSpawn*>& out) const
	{
		out.clear();

		auto it = monsters_by_drop_.find(item_id);
		if (it == monsters_by_drop_.end())
			return;

		std::vector<const ScenarioSpawn*> spots;
		for (int monster_id : it->second)
		{
			CollectHuntSpots(monster_id, prefer_map_id, spots);
			out.insert(out.end(), spots.begin(), spots.end());
		}
	}

	const ScenarioSpawn* BotScenario::FindAnySpot(int map_id) const
	{
		auto it = spawns_by_map_.find(map_id);
		if (it == spawns_by_map_.end() || it->second.empty())
			return nullptr;
		return &spawns_[it->second.front()];
	}

	const ScenarioGate* BotScenario::NextGate(int from_map_id, int to_map_id) const
	{
		if (from_map_id == 0 || to_map_id == 0 || from_map_id == to_map_id)
			return nullptr;

		auto it = routes_.find(RouteKey(from_map_id, to_map_id));
		if (it == routes_.end())
			return nullptr;
		return FindGate(it->second);
	}

	int BotScenario::RouteRequiredLevel(int from_map_id, int to_map_id) const
	{
		if (from_map_id == 0 || to_map_id == 0 || from_map_id == to_map_id)
			return 0;

		int required = 0;
		int current = from_map_id;

		// 맵 수보다 많이 돌면 데이터가 이상한 것이다(경로표는 최단 경로라 돌지 않는다).
		for (int step = 0; step < 16 && current != to_map_id; ++step)
		{
			const ScenarioGate* gate = NextGate(current, to_map_id);
			if (gate == nullptr)
				break;

			required = std::max(required, gate->required_level);

			auto destination = marker_map_.find(gate->target_id);
			if (destination == marker_map_.end())
				break;

			current = destination->second;
		}

		return required;
	}

	const ScenarioGate* BotScenario::FindAnyGate(int map_id) const
	{
		// 어느 게이트를 고르든 상관없지만 실행마다 같아야 한다(unordered_map 은 아니다).
		const ScenarioGate* found = nullptr;
		for (const auto& [gate_id, gate] : gates_)
		{
			if (gate.map_id != map_id)
				continue;
			if (marker_map_.find(gate.target_id) == marker_map_.end())
				continue;
			if (found == nullptr || gate.id < found->id)
				found = &gate;
		}
		return found;
	}

	void BotScenario::DialogChoicesToward(int node_id, const std::string& action, int quest_id,
		std::vector<int>& out) const
	{
		out.clear();

		const ScenarioDialogNode* start = FindDialog(node_id);
		if (start == nullptr)
			return;

		// 지금 노드에 있으면 그것을 누른다.
		for (int i = 0; i < static_cast<int>(start->choices.size()); ++i)
		{
			const ScenarioChoice& choice = start->choices[i];
			if (choice.action == action && choice.param == quest_id)
				out.push_back(i);
		}

		if (!out.empty())
			return;

		// 없으면 goto 를 따라가며 찾는다. 목적지에 닿는 '첫 걸음'을 전부 모은다.
		// (대화는 노드 몇 개짜리 작은 그래프라 매번 탐색해도 값이 싸다.)
		std::deque<std::pair<int, int>> queue;   // (노드 id, 첫 걸음의 선택지 번호)
		std::unordered_map<int, bool> visited{ { node_id, true } };

		for (int i = 0; i < static_cast<int>(start->choices.size()); ++i)
		{
			const ScenarioChoice& choice = start->choices[i];
			if (choice.action == "goto" && choice.next_id != 0)
				queue.emplace_back(choice.next_id, i);
		}

		while (!queue.empty())
		{
			const auto [current_id, first_step] = queue.front();
			queue.pop_front();

			const ScenarioDialogNode* node = FindDialog(current_id);
			if (node == nullptr)
				continue;

			bool reaches = false;
			for (const ScenarioChoice& choice : node->choices)
			{
				if (choice.action == action && choice.param == quest_id)
				{
					reaches = true;
					break;
				}
			}

			if (reaches)
			{
				// 같은 첫 걸음을 두 번 담지 않는다(길이 여러 갈래로 합쳐질 수 있다).
				if (std::find(out.begin(), out.end(), first_step) == out.end())
					out.push_back(first_step);
				continue;
			}

			// 목적지가 아니면 더 들어간다. 노드는 첫 걸음이 다르면 다시 봐야 하므로
			// (같은 노드로 가는 길이 여럿이다) 방문 표시는 첫 걸음까지 함께 본다.
			if (visited.find(current_id) != visited.end())
				continue;
			visited[current_id] = true;

			for (const ScenarioChoice& choice : node->choices)
			{
				if (choice.action == "goto" && choice.next_id != 0)
					queue.emplace_back(choice.next_id, first_step);
			}
		}
	}

	int BotScenario::NextDialogChoice(int node_id, const std::string& action, int quest_id) const
	{
		std::vector<int> candidates;
		DialogChoicesToward(node_id, action, quest_id, candidates);
		return candidates.empty() ? -1 : candidates.front();
	}
}
