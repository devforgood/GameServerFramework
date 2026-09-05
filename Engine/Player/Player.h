#pragma once
#include <cstdint>
#include <string>
#include <memory>
#include <boost/uuid/uuid.hpp>
#include <boost/uuid/uuid_generators.hpp>
#include <boost/uuid/uuid_io.hpp>
#include "syncnet_generated.h"
#include "SendMessage.h"
#include "SendMessagePool.h"
#include "GameObject.h"
#include "PlayerSaveBufferPool.h"

class GameSession;
class GameServer;
class Character;
struct PlayerLoadData;
class Player : public GameObject, public std::enable_shared_from_this<Player>
{
private:
	long playerId_;
	long long dbPlayerId_ = 0; // player.id (영속). 인증 후 확정된다.
	std::string name_;
	int level_;
	boost::uuids::uuid uuid_;

	// 로그인 시 클라가 보낸 계정 식별자. 재접속 핸드오버(끊긴 세션의 캐릭터를
	// 같은 userId 로 재접속했을 때 넘겨받기)의 키로 사용한다.
	std::string userId_;


	std::shared_ptr<Character> character_;
	std::weak_ptr<GameSession> session_;
	GameServer* server_;

	float playerLazySaveAcc_;

	// 저장 버퍼는 매번 할당하지 않고 여기서 돌려쓴다.
	PlayerSaveBufferPool saveBuffers_;

	// 게이트 이동 쿨타임: 마지막으로 게이트 이동에 성공한 시각(ms, epoch). 0 = 이동 이력 없음.
	// Character 는 맵 이동 때마다 재생성되므로, 이동에도 유지되는 Player 에 보관한다.
	uint64_t lastGateMoveMs_ = 0;

	// 로그인 시 결정된 스폰 맵/좌표(기본 맵의 스폰 마커 또는 저장된 마지막 위치).
	// 클라가 보내는 AddAgent(Character) 를 이 맵/좌표로 처리한다. 0 = 미정(기본 맵).
	int spawnMapId_ = 0;
	syncnet::Vec3 spawnPos_{ 0, 0, 0 };

public:
	Player();
	~Player();

	void SetName(std::string name)
	{
		name_ = name;
	}

	void SetLevel(int level)
	{
		level_ = level;
	}
	void SetSession(std::shared_ptr<GameSession> session);
	void SetServer(GameServer* server);

	std::shared_ptr<GameSession> GetSession() { return session_.lock(); }
	GameServer* GetServer() { return server_; }

	// 런타임 핸들. 프로세스 로컬 카운터라 재시작하면 값이 달라진다.
	// 월드/파티/브로드캐스트 같은 "지금 접속 중" 자료구조의 키로만 쓴다.
	long GetPlayerId() { return playerId_; }
	std::string GetName() { return name_; }

	// 영속 캐릭터 행 id(player.id). 로그인 인증이 끝나야 정해진다(0 = 아직 미확정).
	// 저장/로드는 반드시 이 값을 키로 써야 한다 — playerId_ 를 쓰면 접속 순서에 따라
	// 남의 행을 읽고 덮어쓴다.
	long long GetDbPlayerId() const { return dbPlayerId_; }
	void SetDbPlayerId(long long id) { dbPlayerId_ = id; }

	void SetUserId(const std::string& userId) { userId_ = userId; }
	const std::string& GetUserId() const { return userId_; }

	// 재접속 토큰으로 쓰는 플레이어 고유 식별자(생성자에서 1회 생성). 최초 로그인 응답으로
	// 클라에 내려주고, 클라가 재접속 시 되돌려 보내면 유예 대기 플레이어를 이 값으로 찾는다.
	std::string GetUuid() const { return boost::uuids::to_string(uuid_); }

	// 게이트 이동 쿨타임 검사/갱신. nowMs/cooldownMs 는 ms 단위.
	bool IsGateOnCooldown(uint64_t nowMs, uint64_t cooldownMs) const
	{
		return lastGateMoveMs_ != 0 && (nowMs - lastGateMoveMs_) < cooldownMs;
	}
	void MarkGateMoved(uint64_t nowMs) { lastGateMoveMs_ = nowMs; }

	// 로그인 응답에서 서버가 정한 스폰 맵/좌표. 클라의 AddAgent 는 요청에 담긴 좌표 대신
	// 이 값을 쓴다 — 클라가 좌표를 고르면 임의 지점으로 들어올 수 있고, 응답으로 알려준
	// 위치와 실제 스폰이 어긋나기도 한다.
	void SetSpawnLocation(int mapId, const syncnet::Vec3& pos)
	{
		spawnMapId_ = mapId;
		spawnPos_ = pos;
	}
	int GetSpawnMapId() const { return spawnMapId_; }
	const syncnet::Vec3& GetSpawnPos() const { return spawnPos_; }

	void Possess(std::shared_ptr<Character> character);
	void UnPossess();

	// PlayerSkill(배운 스킬)을 현재 캐릭터의 SkillSet 에 반영한다.
	// 빙의 시점과, 스킬을 새로 배운 뒤(퀘스트 보상 등)에 호출한다.
	void ApplyOwnedSkillsToCharacter();

	// 가상 함수인 이유는 테스트 때문이다. "브로드캐스트 순회 도중에 세션이 끊긴다" 는
	// 상황은 실제로는 송신 큐가 넘칠 때만 생겨서 소켓 없이 재현할 수 없는데, 그 순간
	// 맵이 무너지지 않는지는 반드시 고정해야 한다(BroadcastScope). 비용은 무시할 수
	// 있다 - 이 함수는 이미 매 호출 weak_ptr::lock 을 지불한다.
	virtual void Send(std::shared_ptr<send_message>& msg);
	void Close();

	std::shared_ptr<Character> & GetCharacter() { return character_; }

	bool SwitchSession(std::shared_ptr<Player> player);

	template<typename CreateFunc, typename... Args>
	void Send(
		CreateFunc createFunc,
		syncnet::GameMessages msgType,
		int32_t id,
		syncnet::StatusCode result,
		Args&&... args)
	{
		auto builder_ptr = SendMessagePool::Acquire();
		auto msgOffset = createFunc(*builder_ptr, std::forward<Args>(args)...);
		auto send_msg = syncnet::CreateGameMessage(
			*builder_ptr,
			msgType,
			msgOffset.Union(),
			id,
			result
		);
		builder_ptr->Finish(send_msg);
		this->Send(builder_ptr);
	}
	
	void OnLoadedData(const PlayerLoadData & data);

	virtual void Update(float dt) override;

	std::optional<boost::asio::strand<boost::asio::thread_pool::executor_type>> GetStrand();

	void SavePlayerData();

};

