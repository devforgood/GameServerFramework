#include "World.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"
#include <iostream>
#include "Server.h"
#include "Monster.h"
#include "Character.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "DetourCommon.h"
#include "MathHelper.h"
#include "Player.h"
#include "GameObjectFactory.h"
#include "Common.h"
#include "../Engine/Components.h"
#include "../Engine/Systems.h"
#include "../Engine/SystemManager.h"
#include "Actor.h"
#include "Character.h"

//const float g_fDistance = std::powf(10.0f, 2);
const float g_fDistance = 10.0f;

World::World()
{
	map_ = nullptr;
	grid_manager_ = nullptr;
	time_stamp_ = nullptr;
	random_util_ = nullptr;
	system_manager_ = nullptr;
}

World::~World()
{
	if (map_)
	{
		delete map_;
		map_ = nullptr;
	}
	if (grid_manager_)
	{
		delete grid_manager_;
		grid_manager_ = nullptr;
	}
	if (time_stamp_)
	{
		delete time_stamp_;
		time_stamp_ = nullptr;
	}
	if (random_util_)
	{
		delete random_util_;
		random_util_ = nullptr;
	}
	if(system_manager_)
	{
		delete system_manager_;
		system_manager_ = nullptr;
	}
}

void World::Init()
{
	map_ = new Map();
	map_->Init();
	Monster::Initialize("mob.lua");
	Monster::registerLuaFunctionAll();
	grid_manager_ = new GridManager(100, 100, 2);
	time_stamp_ = new TimeStamp();
	random_util_ = new RandomUtil();
	system_manager_ = new Engine::SystemManager();

	auto& entityManager = system_manager_->GetEntityManager();

	// 모든 컴포넌트 타입 등록
	entityManager.RegisterComponent<Engine::PositionComponent>();
	entityManager.RegisterComponent<Engine::VelocityComponent>();
	entityManager.RegisterComponent<Engine::HealthComponent>();
	entityManager.RegisterComponent<Engine::PhysicsComponent>();
	entityManager.RegisterComponent<Engine::AIComponent>();
	entityManager.RegisterComponent<Engine::CollisionComponent>();
	entityManager.RegisterComponent<Engine::InputComponent>();
	entityManager.RegisterComponent<Engine::AnimationComponent>();
	entityManager.RegisterComponent<Engine::TimerComponent>();
	entityManager.RegisterComponent<Engine::ParticleComponent>();
	entityManager.RegisterComponent<Engine::NetworkComponent>();
	entityManager.RegisterComponent<Engine::DirtyComponent>();
	entityManager.RegisterComponent<Engine::ActorComponent>();
	entityManager.RegisterComponent<Engine::PositionSyncComponent>();

	// 기존 시스템 등록
	system_manager_->RegisterSystem<Engine::TimerComponent>(
		[](float deltaTime, Engine::TimerComponent& timer) {
			Engine::TimerSystem::Update(deltaTime, timer);
		});

	// 네트워크 동기화 시스템들 등록
	system_manager_->RegisterSystem<Engine::PositionComponent, Engine::PositionSyncComponent, Engine::DirtyComponent>(
		[](float deltaTime, Engine::PositionComponent& position, Engine::PositionSyncComponent& posSync, Engine::DirtyComponent& dirty) {
			Engine::PositionSyncSystem::Update(deltaTime, position, posSync, dirty);
		});

	system_manager_->RegisterSystem<Engine::ActorComponent, Engine::DirtyComponent, Engine::NetworkComponent>(
		[](float deltaTime, Engine::ActorComponent& actor, Engine::DirtyComponent& dirty, Engine::NetworkComponent& network) {
			Engine::NetworkSyncSystem::Update(deltaTime, actor, dirty, network);
		});

	system_manager_->RegisterSystem<Engine::DirtyComponent>(
		[](float deltaTime, Engine::DirtyComponent& dirty) {
			Engine::DirtyTrackingSystem::Update(deltaTime, dirty);
		});
}

void World::update(float deltaTime)
{
	time_stamp_->update();
	system_manager_->Update(deltaTime);

	//LOG.info("World update begin");
	for (std::list<std::shared_ptr<GameObject>>::iterator itr = game_object_list_.begin();itr!= game_object_list_.end();++itr)
	{
		(*itr)->update(deltaTime);
		
		// DetourCrowd 에이전트 위치를 GameObject와 ECS에 동기화
		const dtCrowdAgent* agent = this->map()->crowd()->getAgent((*itr)->agent_id());
		if (agent && agent->active)
		{
			// GameObject 위치 업데이트 (기존 로직)
			if ((*itr)->is_changed_position(agent->npos[0], agent->npos[1], agent->npos[2]))
			{
				(*itr)->set_position(agent->npos[0], agent->npos[1], agent->npos[2]);
				grid_manager_->move((Actor*)itr->get(), agent->npos[0], agent->npos[2]);
			}
		}
		
		// GameObject가 ECS 엔티티와 연결된 경우 동기화
		auto entity_itr = game_object_to_entity_map_.find((*itr)->agent_id());
		if (entity_itr != game_object_to_entity_map_.end())
		{
			SyncGameObjectToEntity(*itr, entity_itr->second);
			
			// DetourCrowd 위치를 ECS 컴포넌트에도 직접 반영
			if (agent && agent->active)
			{
				UpdateECSFromDetourAgent(*itr, entity_itr->second, agent);
			}
		}
	}

	map_->update(deltaTime);
	SendWorldStateECS(); // ECS 기반 월드 상태 동기화 사용
	//LOG.info("World update end");
}

void World::join(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("World::join error player is nullptr");
		return;
	}
	auto itr = players_.find(player->player_id());
	if (itr != players_.end())
	{
		LOG.error("World::join error player already exists");
		return;
	}
	players_.insert(std::make_pair(player->player_id(), player));

	// 유닛 상태 동기화
	auto builder_ptr = std::make_shared<send_message>();
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> agents;
	GetAgentsInfo(builder_ptr, agents);
	auto updateActorNotify = syncnet::CreateUpdateActorNotifyDirect(*builder_ptr, &agents, nullptr);
	auto send_msg = syncnet::CreateGameMessage(*builder_ptr, syncnet::GameMessages::GameMessages_UpdateActorNotify, updateActorNotify.Union());
	builder_ptr->Finish(send_msg);
	player->send(builder_ptr);
}

void World::leave(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("World::leave error player is nullptr");
		return;
	}
	auto itr = players_.find(player->player_id());
	if (itr == players_.end())
	{
		LOG.error("World::leave error player not found");
		return;
	}
	players_.erase(itr);
}

void World::GetAgentsInfo(std::shared_ptr<send_message>& msg, std::vector<flatbuffers::Offset<syncnet::ActorInfo>>& agent_info_vector)
{
	for (std::list<std::shared_ptr<GameObject>>::iterator itr = game_object_list_.begin(); itr != game_object_list_.end(); ++itr)
	{
		auto game_object = itr->get();
		agent_info_vector.push_back(game_object->get_actor_info(*msg, static_cast<long>(GameObjectChangeType::All)));
	}
}

void World::SendWorldState()
{
	// ECS 시스템들을 먼저 실행하여 변경 감지 및 동기화 처리
	// 이는 이미 update 함수에서 system_manager_->Update()로 호출됨

	auto builder_ptr = std::make_shared<send_message>();
	flatbuffers::Offset<syncnet::ActorInfo> agent_info;
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> agent_info_vector;
	std::vector<int> removed_agents;
	for (std::list<std::shared_ptr<GameObject>>::iterator itr = game_object_list_.begin(); itr != game_object_list_.end(); ++itr)
	{
		auto game_object = itr->get();
		if (game_object->state() == syncnet::AIState::AIState_Destroyed) {
			removed_agents.push_back(game_object->agent_id());
		}

		const dtCrowdAgent* agent = this->map()->crowd()->getAgent(game_object->agent_id());
		if (agent->active == false)
			continue;

		if (game_object->is_changed_position(agent->npos[0], agent->npos[1], agent->npos[2]))
		{
			game_object->set_position(agent->npos[0], agent->npos[1], agent->npos[2]);
			grid_manager_->move((Actor*)itr->get(), agent->npos[0], agent->npos[2]);
		}
		
		if (!game_object->is_changed()) 
			continue;


		agent_info_vector.push_back(game_object->get_actor_info(*builder_ptr, game_object->get_changed_flag()));

		game_object->reset_changed();
	}
	auto agents = builder_ptr->CreateVector(agent_info_vector);

	// ----------------------------
	flatbuffers::Offset<syncnet::DebugRaycast> debug_raycast;
	std::vector<flatbuffers::Offset<syncnet::DebugRaycast>> debug_raycast_vector;
	for (int i = 0; i < this->raycasts_.size(); ++i)
	{
		debug_raycast = syncnet::CreateDebugRaycast(*builder_ptr, 0, &this->raycasts_[i]);
		debug_raycast_vector.push_back(debug_raycast);
	}
	this->raycasts_.clear();
	auto debug_raycasts = builder_ptr->CreateVector(debug_raycast_vector);
	// ----------------------------

	auto updateActorNotify = syncnet::CreateUpdateActorNotify(*builder_ptr, agents, debug_raycasts);

	auto send_msg = syncnet::CreateGameMessage(*builder_ptr, syncnet::GameMessages::GameMessages_UpdateActorNotify, updateActorNotify.Union());
	builder_ptr->Finish(send_msg);

	SendBroadcast(builder_ptr);

	for (auto& agent_id : removed_agents)
	{
		OnRemoveAgent(agent_id);
	}
}

// ECS 기반의 새로운 SendWorldState 함수
void World::SendWorldStateECS()
{
	auto& entityManager = system_manager_->GetEntityManager();
	auto builder_ptr = std::make_shared<send_message>();
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> agent_info_vector;
	std::vector<int> removed_agents;

	// DirtyComponent를 가진 모든 엔티티들을 처리
	auto dirtyArray = entityManager.GetComponentArray<Engine::DirtyComponent>();
	auto actorArray = entityManager.GetComponentArray<Engine::ActorComponent>();
	auto positionArray = entityManager.GetComponentArray<Engine::PositionComponent>();
	auto networkArray = entityManager.GetComponentArray<Engine::NetworkComponent>();

	if (dirtyArray && actorArray && positionArray && networkArray)
	{
		size_t minSize = std::min({dirtyArray->GetSize(), actorArray->GetSize(), 
									positionArray->GetSize(), networkArray->GetSize()});

		for (size_t i = 0; i < minSize; ++i)
		{
			auto& dirty = dirtyArray->GetArray()[i];
			auto& actor = actorArray->GetArray()[i];
			auto& position = positionArray->GetArray()[i];
			auto& network = networkArray->GetArray()[i];

			// Only process dirty entities
			if (!dirty.isDirty)
				continue;

			// Check if entity should be destroyed
			if (actor.aiState == static_cast<uint32_t>(syncnet::AIState::AIState_Destroyed))
			{
				removed_agents.push_back(actor.agentID);
				continue;
			}

			// Create actor info for network sync
			auto actorInfo = CreateActorInfoFromECS(builder_ptr, actor, position, dirty);
			if (actorInfo.o != 0) // Check if valid offset
			{
				agent_info_vector.push_back(actorInfo);
			}

			// Reset dirty flags after processing using unified function
			// Note: We need to find the corresponding GameObject for proper reset
			for (auto& gameObj : game_object_list_)
			{
				if (gameObj->agent_id() == actor.agentID)
				{
					auto entity_itr = game_object_to_entity_map_.find(actor.agentID);
					if (entity_itr != game_object_to_entity_map_.end())
					{
						ResetChangeFlags(gameObj, entity_itr->second);
					}
					break;
				}
			}
		}
	}

	auto agents = builder_ptr->CreateVector(agent_info_vector);

	// Handle debug raycasts (same as before)
	flatbuffers::Offset<syncnet::DebugRaycast> debug_raycast;
	std::vector<flatbuffers::Offset<syncnet::DebugRaycast>> debug_raycast_vector;
	for (int i = 0; i < this->raycasts_.size(); ++i)
	{
		debug_raycast = syncnet::CreateDebugRaycast(*builder_ptr, 0, &this->raycasts_[i]);
		debug_raycast_vector.push_back(debug_raycast);
	}
	this->raycasts_.clear();
	auto debug_raycasts = builder_ptr->CreateVector(debug_raycast_vector);

	auto updateActorNotify = syncnet::CreateUpdateActorNotify(*builder_ptr, agents, debug_raycasts);
	auto send_msg = syncnet::CreateGameMessage(*builder_ptr, syncnet::GameMessages::GameMessages_UpdateActorNotify, updateActorNotify.Union());
	builder_ptr->Finish(send_msg);

	SendBroadcast(builder_ptr);

	// Process removed agents
	for (auto& agent_id : removed_agents)
	{
		OnRemoveAgent(agent_id);
	}
}

flatbuffers::Offset<syncnet::ActorInfo> World::CreateActorInfoFromECS(
	std::shared_ptr<send_message>& builder_ptr, 
	const Engine::ActorComponent& actor, 
	const Engine::PositionComponent& position, 
	const Engine::DirtyComponent& dirty)
{
	// Create position vector
	syncnet::Vec3 pos(Vector3::convert_x(position.x), Vector3::convert_y(position.y), Vector3::convert_z(position.z));
	
	// Create velocity vector (zero for now, could be extended with VelocityComponent)
	syncnet::Vec3 vel(0.0f, 0.0f, 0.0f);

	// Create actor state
	syncnet::ActorState actorState(static_cast<syncnet::AIState>(actor.aiState));
	
	// Create actor health (only takes one parameter - current health)
	syncnet::ActorHealth actorHealth(100); // current health
	
	// Create actor info
	auto actorInfo = syncnet::CreateActorInfo(
		*builder_ptr,
		actor.agentID,
		&pos,
		static_cast<syncnet::GameObjectType>(actor.gameObjectType),
		&actorState,
		&actorHealth,
		false // inputLocked
	);

	return actorInfo;
}

Engine::EntityID World::ConvertGameObjectToEntity(std::shared_ptr<GameObject> gameObject)
{
	auto& entityManager = system_manager_->GetEntityManager();
	
	// Create new entity
	Engine::EntityID entityID = entityManager.CreateEntity();
	
	// Cast to Actor to access position methods
	auto actor = std::dynamic_pointer_cast<Actor>(gameObject);
	if (!actor) return entityID; // Only Actor objects supported for now
	
	// Get player_id if this is a Character
	int playerId = -1;
	if (gameObject->type() == syncnet::GameObjectType_Character)
	{
		auto character = std::dynamic_pointer_cast<Character>(gameObject);
		if (character)
		{
			playerId = static_cast<int>(character->player_id());
		}
	}
	
	// Add components based on GameObject properties
	entityManager.AddComponent(entityID, Engine::ActorComponent{
		gameObject->agent_id(),
		static_cast<uint32_t>(gameObject->type()),
		static_cast<uint32_t>(gameObject->state()),
		playerId
	});
	
	const Vector3& pos = actor->get_position();
	entityManager.AddComponent(entityID, Engine::PositionComponent{
		pos.x,
		pos.y,
		pos.z
	});
	
	// Add velocity component (initially zero)
	entityManager.AddComponent(entityID, Engine::VelocityComponent{
		0.0f, 0.0f, 0.0f
	});
	
	entityManager.AddComponent(entityID, Engine::PositionSyncComponent{
		pos.x,
		pos.y,
		pos.z,
		0.01f // sync threshold
	});
	
	entityManager.AddComponent(entityID, Engine::DirtyComponent{
		static_cast<uint32_t>(gameObject->get_changed_flag()),
		gameObject->is_changed(),
		false, // isPositionDirty
		false  // isStateDirty
	});
	
	entityManager.AddComponent(entityID, Engine::NetworkComponent{
		static_cast<uint32_t>(gameObject->agent_id()),
		false, // isLocalPlayer
		0.0f,  // lastUpdateTime
		0.016f // updateInterval (60 FPS)
	});
	
	return entityID;
}

void World::SyncGameObjectToEntity(std::shared_ptr<GameObject> gameObject, Engine::EntityID entityID)
{
	auto& entityManager = system_manager_->GetEntityManager();
	
	// Cast to Actor to access position methods
	auto actor = std::dynamic_pointer_cast<Actor>(gameObject);
	if (!actor) return; // Only Actor objects supported for now
	
	long changeFlags = gameObject->get_changed_flag();
	bool hasAnyChange = gameObject->is_changed();
	
	// Update position component if position changed
	if (changeFlags & static_cast<long>(GameObjectChangeType::Position))
	{
		if (entityManager.HasComponent<Engine::PositionComponent>(entityID))
		{
			auto& positionComponent = entityManager.GetComponent<Engine::PositionComponent>(entityID);
			const Vector3& pos = actor->get_position();
			positionComponent.x = pos.x;
			positionComponent.y = pos.y;
			positionComponent.z = pos.z;
		}
		
		// Update position sync component with new position
		if (entityManager.HasComponent<Engine::PositionSyncComponent>(entityID))
		{
			auto& posSyncComponent = entityManager.GetComponent<Engine::PositionSyncComponent>(entityID);
			const Vector3& pos = actor->get_position();
			
			// Check if position changed beyond sync threshold
			float dx = pos.x - posSyncComponent.lastSyncX;
			float dy = pos.y - posSyncComponent.lastSyncY;
			float dz = pos.z - posSyncComponent.lastSyncZ;
			float distanceSqr = dx * dx + dy * dy + dz * dz;
			
			if (distanceSqr > posSyncComponent.syncThreshold * posSyncComponent.syncThreshold)
			{
				posSyncComponent.lastSyncX = pos.x;
				posSyncComponent.lastSyncY = pos.y;
				posSyncComponent.lastSyncZ = pos.z;
			}
		}
	}
	
	// Update actor component if state or health changed
	if (changeFlags & (static_cast<long>(GameObjectChangeType::State) | static_cast<long>(GameObjectChangeType::Health)))
	{
		if (entityManager.HasComponent<Engine::ActorComponent>(entityID))
		{
			auto& actorComponent = entityManager.GetComponent<Engine::ActorComponent>(entityID);
			actorComponent.aiState = static_cast<uint32_t>(gameObject->state());
			// Could also update health info here if needed
		}
	}
	
	// Update dirty component with current change flags
	if (entityManager.HasComponent<Engine::DirtyComponent>(entityID))
	{
		auto& dirtyComponent = entityManager.GetComponent<Engine::DirtyComponent>(entityID);
		dirtyComponent.changedFlags = static_cast<uint32_t>(changeFlags);
		dirtyComponent.isDirty = hasAnyChange;
		
		// Set specific dirty flags based on change type
		dirtyComponent.isPositionDirty = (changeFlags & static_cast<long>(GameObjectChangeType::Position)) != 0;
		dirtyComponent.isStateDirty = (changeFlags & static_cast<long>(GameObjectChangeType::State)) != 0;
	}
	
	// Update network component timing
	if (entityManager.HasComponent<Engine::NetworkComponent>(entityID))
	{
		auto& networkComponent = entityManager.GetComponent<Engine::NetworkComponent>(entityID);
		// Network update timing is handled by the NetworkSyncSystem
		// But we could update lastUpdateTime here if needed
	}
}

void World::UpdateECSFromDetourAgent(std::shared_ptr<GameObject> gameObject, Engine::EntityID entityID, const dtCrowdAgent* agent)
{
	auto& entityManager = system_manager_->GetEntityManager();
	
	// Update position component with DetourCrowd agent position
	if (entityManager.HasComponent<Engine::PositionComponent>(entityID))
	{
		auto& positionComponent = entityManager.GetComponent<Engine::PositionComponent>(entityID);
		positionComponent.x = agent->npos[0];
		positionComponent.y = agent->npos[1];
		positionComponent.z = agent->npos[2];
	}
	
	// Update velocity component if exists
	if (entityManager.HasComponent<Engine::VelocityComponent>(entityID))
	{
		auto& velocityComponent = entityManager.GetComponent<Engine::VelocityComponent>(entityID);
		velocityComponent.vx = agent->vel[0];
		velocityComponent.vy = agent->vel[1];
		velocityComponent.vz = agent->vel[2];
	}
	
	// Update position sync component
	if (entityManager.HasComponent<Engine::PositionSyncComponent>(entityID))
	{
		auto& posSyncComponent = entityManager.GetComponent<Engine::PositionSyncComponent>(entityID);
		
		// Check if position changed beyond sync threshold
		float dx = agent->npos[0] - posSyncComponent.lastSyncX;
		float dy = agent->npos[1] - posSyncComponent.lastSyncY;
		float dz = agent->npos[2] - posSyncComponent.lastSyncZ;
		float distanceSqr = dx * dx + dy * dy + dz * dz;
		
		if (distanceSqr > posSyncComponent.syncThreshold * posSyncComponent.syncThreshold)
		{
			posSyncComponent.lastSyncX = agent->npos[0];
			posSyncComponent.lastSyncY = agent->npos[1];
			posSyncComponent.lastSyncZ = agent->npos[2];
			
			// Mark position as dirty for network sync
			if (entityManager.HasComponent<Engine::DirtyComponent>(entityID))
			{
				auto& dirtyComponent = entityManager.GetComponent<Engine::DirtyComponent>(entityID);
				dirtyComponent.isPositionDirty = true;
				dirtyComponent.isDirty = true;
				dirtyComponent.changedFlags |= static_cast<uint32_t>(GameObjectChangeType::Position);
			}
		}
	}
}

void World::ResetChangeFlags(std::shared_ptr<GameObject> gameObject, Engine::EntityID entityID)
{
	auto& entityManager = system_manager_->GetEntityManager();
	
	// Reset GameObject change flags
	gameObject->reset_changed();
	
	// Reset ECS DirtyComponent flags
	if (entityManager.HasComponent<Engine::DirtyComponent>(entityID))
	{
		auto& dirtyComponent = entityManager.GetComponent<Engine::DirtyComponent>(entityID);
		dirtyComponent.isDirty = false;
		dirtyComponent.isPositionDirty = false;
		dirtyComponent.isStateDirty = false;
		dirtyComponent.changedFlags = 0;
	}
	
	// Reset network component timer if needed
	if (entityManager.HasComponent<Engine::NetworkComponent>(entityID))
	{
		auto& networkComponent = entityManager.GetComponent<Engine::NetworkComponent>(entityID);
		networkComponent.lastUpdateTime = 0.0f;
	}
}

void World::SendBroadcast(std::shared_ptr<send_message> msg) 
{
	for (auto itr = players_.begin(); itr != players_.end(); ++itr)
	{
		itr->second->send(msg);
	}
}
void World::SendBroadcast(std::shared_ptr<send_message> msg, std::shared_ptr<Player>& except)
{
	for (auto itr = players_.begin(); itr != players_.end(); ++itr)
	{
		if (itr->second.get() == except.get())
			continue;

		itr->second->send(msg);
	}
}

std::shared_ptr<GameObject> World::OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	auto game_object = GameObjectFactory::CreateGameObject(this, player, type, pos);
	if (game_object == nullptr)
	{
		LOG.error("OnAddAgent error in GameObjectFactory::CreateGameObject()");
		return nullptr;
	}

	auto itr = game_object_list_.insert(game_object_list_.end(), game_object);
	game_object_map_.insert(std::make_pair(game_object->agent_id(), itr));
	game_object->set_changed(static_cast<long>(GameObjectChangeType::All));

	// ECS 엔티티도 함께 생성
	Engine::EntityID entityID = ConvertGameObjectToEntity(game_object);
	game_object_to_entity_map_[game_object->agent_id()] = entityID;

	return game_object;
}

void World::OnRemoveAgent(int agent_id)
{
	auto itr = game_object_map_.find(agent_id);
	if (itr == game_object_map_.end())
	{
		LOG.error("OnRemoveAgent error not exist in monsters_map_");
		return;
	}

	if (itr->second->get()->type() == syncnet::GameObjectType_Character)
	{
		auto character = std::dynamic_pointer_cast<Character>(*itr->second);

		auto itr_player = players_.find(character->player_id());
		if (itr_player != players_.end())
		{
			players_.erase(itr_player);
		}
	}

	grid_manager_->remove((Actor*)itr->second->get());
	game_object_list_.erase(itr->second);
	game_object_map_.erase(itr);

	// ECS 엔티티도 함께 제거
	auto entity_itr = game_object_to_entity_map_.find(agent_id);
	if (entity_itr != game_object_to_entity_map_.end())
	{
		system_manager_->GetEntityManager().DestroyEntity(entity_itr->second);
		game_object_to_entity_map_.erase(entity_itr);
	}

	this->map()->removeAgent(agent_id);

}

void World::OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos)
{
	this->map()->setMoveTarget(Vector3(pos).pos(), false, agent_id);

}

void World::OnSetRaycast(const syncnet::Vec3* pos)
{
	float hitPoint[3];
	if (this->map()->raycast(0, Vector3(pos).pos(), hitPoint))
	{
		syncnet::Vec3 pos(hitPoint[0] * -1, hitPoint[1], hitPoint[2]);
		this->raycasts_.push_back(pos);
	}
}

int World::DetectEnemy(Actor * actor)
{
	const dtCrowdAgent* this_agent = this->map()->crowd()->getAgent(actor->agent_id());
	float hitPoint[3];

	auto targets = grid_manager_->getEntitiesInViewRange(actor, g_fDistance);

	for (auto itr = targets.begin(); itr != targets.end(); ++itr)
	{
		if (!(*itr)->isCharacter())
			continue;

		const dtCrowdAgent* agent = this->map()->crowd()->getAgent((*itr)->getAgentID());


		if (this->map()->raycast(actor->agent_id(), agent->npos, hitPoint) == false)
		{
			return (*itr)->getAgentID();
		}
	}
	return -1;
}




std::vector<IGridActor*> World::get_actors_in_range(Actor* actor, float range, float dirDeg, float angle) 
{ 
	return grid_manager_->getEntitiesInAoEMask(actor->get_vecter2_x(), actor->get_vecter2_y(), range, dirDeg, angle);
}