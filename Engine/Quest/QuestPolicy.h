#pragma once

// 운영이 런타임에 바꾸는 퀘스트 전역 정책.
//
// 데이터(quest.json)는 배포 단위라 이벤트 기간 동안 보상만 올리고 싶을 때 쓰기 어렵다.
// 여기 값은 서버를 재시작하지 않고 바꿀 수 있고, 보상 지급 시점에 곱해진다.
//
// 월드 로직은 한 스레드에서 도므로 잠금은 두지 않는다. 운영 명령도 같은 스레드에서
// 처리되어야 한다(다른 스레드에서 바꾸려면 게임 스레드로 던져야 한다).
class QuestPolicy
{
public:
	static QuestPolicy& Instance()
	{
		static QuestPolicy instance;
		return instance;
	}

	void SetExpMultiplier(double value) { expMultiplier_ = value > 0.0 ? value : 1.0; }
	void SetGoldMultiplier(double value) { goldMultiplier_ = value > 0.0 ? value : 1.0; }

	double GetExpMultiplier() const { return expMultiplier_; }
	double GetGoldMultiplier() const { return goldMultiplier_; }

	int ApplyExp(int base_exp) const { return scale(base_exp, expMultiplier_); }
	int ApplyGold(int base_gold) const { return scale(base_gold, goldMultiplier_); }

	void Reset()
	{
		expMultiplier_ = 1.0;
		goldMultiplier_ = 1.0;
	}

private:
	QuestPolicy() = default;

	static int scale(int base_value, double multiplier)
	{
		if (base_value <= 0)
			return 0;
		const double scaled = static_cast<double>(base_value) * multiplier;
		// 배율을 걸었는데 0 이 되면 "보상 없음"이 되어 버린다. 최소 1 은 준다.
		return scaled < 1.0 ? 1 : static_cast<int>(scaled);
	}

	double expMultiplier_ = 1.0;
	double goldMultiplier_ = 1.0;
};
