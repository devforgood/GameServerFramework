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
//---------------------------------------------------------------------------------------
class MonsterBTRunner
{
public:
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

	// Monster::btBackend_ 가 가리키는 백엔드의 트리를 만든다.
	// 백엔드는 스폰 시점에 고정되므로, 바꾸려면 몬스터 생성 전에 btBackend_ 를 설정해야 한다.
	void Create(Monster* monster);

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
