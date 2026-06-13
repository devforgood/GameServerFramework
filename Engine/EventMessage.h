#pragma once
#include <variant>
#include <string>
#include <iostream>

struct EventActorDead
{
	int killer_actor_id;
	int victim_actor_id;
};

struct EventPlayerJoined
{
	int player_id;
};

using EventMessage = std::variant<
	EventActorDead
	, EventPlayerJoined
>;
