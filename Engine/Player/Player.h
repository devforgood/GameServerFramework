#pragma once
#include <cstdint>
#include <string>
#include <memory>
#include <boost/uuid/uuid.hpp>
#include <boost/uuid/uuid_generators.hpp>
#include <boost/uuid/uuid_io.hpp>
#include "syncnet_generated.h"
#include "SendMessage.h"
#include "GameObject.h"

class GameSession;
class GameServer;
class Character;
struct PlayerLoadData;
class Player : public GameObject, public std::enable_shared_from_this<Player>
{
private:
	long playerId_;
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

	// 게이트 이동 쿨타임: 마지막으로 게이트 이동에 성공한 시각(ms, epoch). 0 = 이동 이력 없음.
	// Character 는 맵 이동 때마다 재생성되므로, 이동에도 유지되는 Player 에 보관한다.
	uint64_t lastGateMoveMs_ = 0;

	// 로그인 시 결정된 스폰 맵 id(기본 맵 또는 마지막 로그아웃 위치의 맵).
	// 클라가 보내는 AddAgent(Character) 를 이 맵으로 라우팅한다. 0 = 미정(기본 맵).
	int spawnMapId_ = 0;

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

	long GetPlayerId() { return playerId_; }
	std::string GetName() { return name_; }

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

	void SetSpawnMapId(int mapId) { spawnMapId_ = mapId; }
	int GetSpawnMapId() const { return spawnMapId_; }

	void Possess(std::shared_ptr<Character> character);
	void UnPossess();

	void Send(std::shared_ptr<send_message>& msg);
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
		auto builder_ptr = std::make_shared<send_message>();
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

