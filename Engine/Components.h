#pragma once
#include "ECS.h"
#include <cstdint>

namespace Engine
{
    // Position component - only primitive types for cache optimization
    struct PositionComponent
    {
        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;
    };
    
    // Velocity component - only primitive types
    struct VelocityComponent
    {
        float vx = 0.0f;
        float vy = 0.0f;
        float vz = 0.0f;
    };
    
    // Health component - only primitive types
    struct HealthComponent
    {
        float currentHealth = 100.0f;
        float maxHealth = 100.0f;
        bool isAlive = true;
    };
    
    // Transform component - only primitive types
    struct TransformComponent
    {
        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;
        float rotationX = 0.0f;
        float rotationY = 0.0f;
        float rotationZ = 0.0f;
        float scaleX = 1.0f;
        float scaleY = 1.0f;
        float scaleZ = 1.0f;
    };
    
    // Collision component - only primitive types
    struct CollisionComponent
    {
        float radius = 1.0f;
        bool isCollidable = true;
        uint32_t collisionLayer = 0;
    };
    
    // AI component - only primitive types
    struct AIComponent
    {
        uint32_t aiState = 0;
        float aiTimer = 0.0f;
        float aiUpdateInterval = 1.0f;
    };
    
    // Render component - only primitive types
    struct RenderComponent
    {
        uint32_t meshID = 0;
        uint32_t textureID = 0;
        bool isVisible = true;
        float alpha = 1.0f;
    };
    
    // Input component - only primitive types
    struct InputComponent
    {
        bool moveForward = false;
        bool moveBackward = false;
        bool moveLeft = false;
        bool moveRight = false;
        bool jump = false;
        float mouseX = 0.0f;
        float mouseY = 0.0f;
    };
    
    // Physics component - only primitive types
    struct PhysicsComponent
    {
        float mass = 1.0f;
        float friction = 0.1f;
        float restitution = 0.5f;
        bool isStatic = false;
        bool useGravity = true;
    };
    
    // Animation component - only primitive types
    struct AnimationComponent
    {
        uint32_t currentAnimation = 0;
        float animationTime = 0.0f;
        float animationSpeed = 1.0f;
        bool isLooping = true;
    };
    
    // Audio component - only primitive types
    struct AudioComponent
    {
        uint32_t soundID = 0;
        float volume = 1.0f;
        float pitch = 1.0f;
        bool isPlaying = false;
        bool isLooping = false;
    };
    
    // Particle component - only primitive types
    struct ParticleComponent
    {
        float lifeTime = 1.0f;
        float currentLife = 1.0f;
        float emissionRate = 10.0f;
        uint32_t maxParticles = 100;
    };
    
    // Network component - only primitive types
    struct NetworkComponent
    {
        uint32_t networkID = 0;
        bool isLocalPlayer = false;
        float lastUpdateTime = 0.0f;
        float updateInterval = 0.016f; // 60 FPS
    };
    
    // Timer component - only primitive types
    struct TimerComponent
    {
        float duration = 1.0f;
        float currentTime = 0.0f;
        bool isActive = false;
        bool isRepeating = false;
    };
    
    // Tag component - only primitive types
    struct TagComponent
    {
        uint32_t tagID = 0;
        uint32_t layerID = 0;
    };

    struct StateComponent
    {
        uint32_t stateID = 0; // 상태 ID
		uint32_t changeFlag = 0; // 변경 플래그
		uint32_t agentID = 0; // 에이전트 ID
	};
} 