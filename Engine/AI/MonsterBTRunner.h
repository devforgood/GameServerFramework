#pragma once

class Monster;

//---------------------------------------------------------------------------------------
// 선택된 BT 백엔드의 트리를 담고 생성/틱/해제를 위임하는 얇은 홀더.
//
// 백엔드마다 트리 타입(BT::Tree vs BT::BehaviorTree)과 해제 방식이 달라서, 예전에는 Monster 가
// 두 종류의 포인터를 각각 들고 매 틱 어느 쪽이 살아 있는지 분기했다. 여기서는 스폰 시점에
// 선택된 전략의 함수 테이블만 굳혀 두므로, Monster 는 백엔드를 전혀 몰라도 된다.
//
// 백엔드를 추가하려면 Ops 세 함수를 채운 테이블을 노출하고 Create() 의 선택 분기에 한 줄
// 등록하면 된다. 틱 비용은 간접 호출 1회로, 예전의 분기 + 가상 호출과 실질적으로 같다.
//
// 기본값인 ECS 백엔드에는 개체마다 갖는 트리가 아예 없다 — create 는 컴포넌트 등록이고
// tick 은 무동작이며, 실행은 Map::UpdateActors 가 부르는 MonsterAISystem 이 일괄로 한다.
//
// 백엔드는 개체마다 다를 수 있다. 한 맵 안에서 섞여도 서로를 방해하지 않는다 —
// MonsterAISystem 은 등록된 컴포넌트만 훑으므로 트리 백엔드 몬스터는 그 배열에 없고,
// 반대로 ECS 몬스터의 Tick 은 무동작이다. 누가 어느 백엔드를 쓰는지는
// Monster::ResolveBTBackend 한 곳이 정한다.
//---------------------------------------------------------------------------------------
class MonsterBTRunner
{
public:
	// 몬스터 하나를 돌릴 AI 백엔드. 셋은 같은 노드 로직(MonsterBTNodes.h)을 공유하고
	// 트리 구조도 1:1 로 같지만 실행 방식이 다르다(Benchmark/PERFORMANCE.md).
	enum class Backend
	{
		BTCpp,    // behaviortree_cpp (GameData/Monster.xml). BT 디버그 뷰어를 지원한다.
		CodeBase, // ../BehaviorTree (인하우스, MonsterCodeBaseBT). 마리마다 노드 객체를 만든다.
		Ecs,      // 트리를 컴파일한 결정표 + 개체별 컴포넌트 (MonsterAISystem) — 기본값.
	};

	// 백엔드 전략 테이블. 트리 인스턴스는 백엔드만 아는 타입이므로 void* 로 들고 다닌다.
	struct Ops
	{
		void* (*create)(Monster* monster);
		void  (*tick)(void* tree, Monster* monster);
		void  (*destroy)(void* tree);
	};

	MonsterBTRunner() = default;
	~MonsterBTRunner() { Destroy(); }

	MonsterBTRunner(const MonsterBTRunner&) = delete;
	MonsterBTRunner& operator=(const MonsterBTRunner&) = delete;

	// 지정한 백엔드의 트리를 만든다. 이미 만들어져 있으면 먼저 그것을 정리하므로,
	// 몬스터의 종류가 뒤늦게 정해져 백엔드가 바뀌는 경우에도 다시 부르면 된다
	// (Monster::ApplyAIProfile). 이전 백엔드에서 완전히 빠져나온 뒤 새로 붙는다.
	void Create(Monster* monster, Backend backend);

	void Tick(Monster* monster)
	{
		if (tree_ != nullptr)
			ops_->tick(tree_, monster);
	}

	void Destroy();

	bool IsValid() const { return tree_ != nullptr; }

private:
	void* tree_ = nullptr;
	const Ops* ops_ = nullptr;
};
