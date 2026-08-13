#pragma once
#include <algorithm>
#include <cstdint>
#include <vector>

// 파티 하나.
//
// 생성과 변경은 PartyManager 만 한다. 멤버 목록과 "플레이어 -> 파티" 색인은 항상 같이
// 움직여야 하는데, 파티가 스스로를 바꿀 수 있으면 그 둘이 어긋난 상태를 만들 수 있다.
class Party
{
public:
	int GetId() const { return id_; }
	long GetLeaderId() const { return leaderId_; }

	// 가입 순서대로. 리더가 항상 앞이라고 가정하면 안 된다(위임하면 순서와 무관해진다).
	const std::vector<long>& GetMembers() const { return members_; }

	size_t Size() const { return members_.size(); }
	bool HasMember(long player_id) const
	{
		return std::find(members_.begin(), members_.end(), player_id) != members_.end();
	}
	bool IsLeader(long player_id) const { return leaderId_ == player_id; }

	// 로스터(멤버 목록/리더)가 바뀔 때마다 오른다. 각 플레이어는 마지막으로 클라에 보낸
	// 값과 비교해 "보낼 것이 있는지"를 이 정수 하나로 판정한다 — 파티 쪽에서 변경을
	// 밀어 주지 않아도 되므로, 파티가 멤버의 수명을 알 필요가 없다.
	uint32_t GetVersion() const { return version_; }

private:
	friend class PartyManager;

	Party(int id, long leader_id)
		: id_(id), leaderId_(leader_id)
	{
		members_.push_back(leader_id);
	}

	bool AddMember(long player_id)
	{
		if (HasMember(player_id))
			return false;
		members_.push_back(player_id);
		++version_;
		return true;
	}

	bool RemoveMember(long player_id)
	{
		auto it = std::find(members_.begin(), members_.end(), player_id);
		if (it == members_.end())
			return false;
		members_.erase(it);
		++version_;
		return true;
	}

	// 멤버가 아닌 대상은 리더가 될 수 없다.
	bool SetLeader(long player_id)
	{
		if (!HasMember(player_id) || leaderId_ == player_id)
			return false;
		leaderId_ = player_id;
		++version_;
		return true;
	}

	int id_ = 0;
	long leaderId_ = 0;
	std::vector<long> members_;
	uint32_t version_ = 1;
};
