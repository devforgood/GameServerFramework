#include "MonsterBT.h"

#include "behaviortree_cpp/bt_factory.h"

#include <fstream>      // std::ifstream
#include <memory>       // std::unique_ptr
#include <sstream>      // std::stringstream
#include <string>
#include <type_traits>  // std::conditional_t

#include "BTDebugManager.h"
#include "GameDataPath.h"
#include "LogHelper.h"
#include "MonsterBTNodes.h"

//---------------------------------------------------------------------------------------
// behaviortree_cpp 백엔드 어댑터.
//
// 게임 로직은 MonsterBTNodes.h 의 로직 구조체 하나뿐이고, 여기서는 그것을
// behaviortree_cpp 의 노드 클래스로 감싸는 템플릿과 팩토리 등록만 한다.
// 노드를 추가/삭제하려면 MonsterBTNodes.h 의 AllNodes 와 Monster.xml 만 고치면 된다.
//---------------------------------------------------------------------------------------

namespace
{
	BT::NodeStatus ToNodeStatus(monsterbt::Status status)
	{
		switch (status)
		{
		case monsterbt::Status::Success: return BT::NodeStatus::SUCCESS;
		case monsterbt::Status::Failure: return BT::NodeStatus::FAILURE;
		default:                         return BT::NodeStatus::RUNNING;
		}
	}

	// 조건 노드는 ConditionNode, 액션 노드는 SyncActionNode 로 붙는다.
	// (behaviortree_cpp 팩토리가 기반 클래스로 노드 종류를 판별한다.)
	template <class Logic>
	using NodeBase = std::conditional_t<
		Logic::kKind == monsterbt::NodeKind::Condition, BT::ConditionNode, BT::SyncActionNode>;

	// 로직 구조체 하나를 behaviortree_cpp 노드로 만드는 어댑터.
	// tick() 안에서 Logic::Tick 이 그대로 인라인되므로 직접 구현했을 때와 비용이 같다.
	//
	// 포트(providedPorts)는 일부러 노출하지 않는다. PortsList 의 문자열이 라이브러리
	// 매니페스트로 전달되면 힙 손상이 발생해서, 튜닝 값은 전부 로직 쪽 상수로 둔다.
	template <class Logic>
	class MonsterNode : public NodeBase<Logic>
	{
	public:
		MonsterNode(const std::string& name, const BT::NodeConfig& config, Monster* monster)
			: NodeBase<Logic>(name, config), monster_(monster)
		{
		}

		BT::NodeStatus tick() override
		{
			const monsterbt::TickResult result = logic_.Tick(monster_);
			const BT::NodeStatus status = ToNodeStatus(result.status);
			BT_DEBUG_RECORD(monster_, Logic::kDebugId, Logic::kName, status, result.reason);
			return status;
		}

	private:
		Monster* monster_;
		Logic logic_;
	};

	template <class... Logics>
	void RegisterNodes(BT::BehaviorTreeFactory& factory, Monster* monster, monsterbt::NodeList<Logics...>)
	{
		// XML 의 노드 ID 는 로직의 kName 이다. 몬스터 포인터를 캡처해 빌더로 넘긴다.
		(factory.registerBuilder<MonsterNode<Logics>>(
			Logics::kName,
			[monster](const std::string& name, const BT::NodeConfig& config) {
				return std::make_unique<MonsterNode<Logics>>(name, config, monster);
			}), ...);
	}

	std::string LoadFile(const std::string& filename)
	{
		try
		{
			std::ifstream file(filename);
			if (!file)
			{
				LOG.error("%s file not found.", filename.c_str());
				return std::string();
			}

			std::stringstream buffer;
			buffer << file.rdbuf();
			return buffer.str();
		}
		catch (std::exception& e)
		{
			LOG.error("Exception: " + std::string(e.what()));
			return std::string();
		}
	}

	//--- MonsterBTRunner 전략 구현 ---

	void* CreateTree(Monster* monster)
	{
		BT::Tree* tree = MonsterBT::createTree(monster);
#if defined(ENABLE_BT_DEBUG)
		BTDebugManager::Instance().PublishTreeDefinition(monster);
#endif
		return tree;
	}

	void TickTree(void* tree, Monster* monster)
	{
		(void)monster; // 디버그 뷰어를 끈 빌드에서는 쓰이지 않는다.
		BT_DEBUG_BEGIN_TICK(monster);
		static_cast<BT::Tree*>(tree)->tickOnce();
		BT_DEBUG_END_TICK(monster);
	}

	void DestroyTree(void* tree)
	{
		delete static_cast<BT::Tree*>(tree);
	}
}

BT::Tree* MonsterBT::createTree(Monster* monster)
{
	BT::BehaviorTreeFactory factory;
	RegisterNodes(factory, monster, monsterbt::AllNodes{});

	return new BT::Tree(factory.createTreeFromText(LoadFile(GameDataPath::Resolve() + "Monster.xml")));
}

const MonsterBTRunner::Ops& MonsterBT::Ops()
{
	static const MonsterBTRunner::Ops ops{ &CreateTree, &TickTree, &DestroyTree };
	return ops;
}
