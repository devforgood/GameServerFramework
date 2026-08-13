#pragma once

// 파티 전역 정책.
//
// 최대 인원이나 경험치 분배는 밸런스 값이라 서버를 재시작하지 않고 바꿀 수 있어야 한다
// (이벤트 기간에 파티 보너스를 올리는 식). QuestPolicy 와 같은 규칙으로, 월드 로직이
// 한 스레드에서 도는 것을 전제로 잠금을 두지 않는다 — 운영 명령도 게임 스레드에서 처리한다.
class PartyPolicy
{
public:
	static PartyPolicy& Instance()
	{
		static PartyPolicy instance;
		return instance;
	}

	// 파티 최대 인원. 1이면 파티라는 것이 성립하지 않으므로 2 아래로는 내려가지 않는다.
	// 상한은 프로토콜/클라 UI 가 감당하는 선(kHardMaxMembers)에서 자른다.
	void SetMaxMembers(int value)
	{
		maxMembers_ = value < 2 ? 2 : (value > kHardMaxMembers ? kHardMaxMembers : value);
	}
	int GetMaxMembers() const { return maxMembers_; }

	// 초대/퀘스트 공유 제안이 살아 있는 시간(초). 응답 없이 지나면 서버가 지운다.
	void SetOfferTimeoutSec(float value) { offerTimeoutSec_ = value > 1.0f ? value : 1.0f; }
	float GetOfferTimeoutSec() const { return offerTimeoutSec_; }

	// 킬 크레딧 반경(서버 좌표, xz 평면). 처치 지점에서 이보다 멀리 있는 파티원은
	// 사냥에 참여하지 않은 것으로 보고 크레딧을 주지 않는다.
	void SetKillCreditRadius(float value) { killCreditRadius_ = value > 0.0f ? value : 0.0f; }
	float GetKillCreditRadius() const { return killCreditRadius_; }

	// 크레딧을 나눠 갖는 인원이 1명 늘 때마다 총 경험치에 더해지는 비율.
	void SetExpBonusPerMember(double value) { expBonusPerMember_ = value >= 0.0 ? value : 0.0; }
	double GetExpBonusPerMember() const { return expBonusPerMember_; }

	// share_count 명이 나눠 가질 때의 1인분 경험치.
	//
	// 총량 = base * (1 + bonus*(n-1)) 이고 이를 n 으로 나눈다. 파티가 커질수록 총량은
	// 늘지만 1인분은 줄어든다 — 나눠 먹는 대신 사냥 속도를 얻는 구조다. 보너스가 1인분
	// 감소를 완전히 메우게 하면 혼자 하는 쪽이 손해가 되어 모두가 파티를 강요당한다.
	int ShareExp(int base_exp, int share_count) const
	{
		if (base_exp <= 0)
			return 0;
		if (share_count <= 1)
			return base_exp;

		const double total = static_cast<double>(base_exp)
			* (1.0 + expBonusPerMember_ * static_cast<double>(share_count - 1));
		const double each = total / static_cast<double>(share_count);

		// 인원이 많다고 0이 되면 파티가 벌이 아니라 벌칙이 된다. 최소 1은 준다.
		return each < 1.0 ? 1 : static_cast<int>(each);
	}

	void Reset()
	{
		maxMembers_ = kDefaultMaxMembers;
		offerTimeoutSec_ = kDefaultOfferTimeoutSec;
		killCreditRadius_ = kDefaultKillCreditRadius;
		expBonusPerMember_ = kDefaultExpBonusPerMember;
	}

private:
	PartyPolicy() = default;

	static constexpr int kDefaultMaxMembers = 5;
	static constexpr int kHardMaxMembers = 12;
	static constexpr float kDefaultOfferTimeoutSec = 30.0f;
	static constexpr float kDefaultKillCreditRadius = 60.0f;
	static constexpr double kDefaultExpBonusPerMember = 0.1;

	int maxMembers_ = kDefaultMaxMembers;
	float offerTimeoutSec_ = kDefaultOfferTimeoutSec;
	float killCreditRadius_ = kDefaultKillCreditRadius;
	double expBonusPerMember_ = kDefaultExpBonusPerMember;
};
