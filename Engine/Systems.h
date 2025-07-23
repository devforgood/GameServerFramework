#pragma once
#include "SystemManager.h"
#include "Components.h"
#include <algorithm>
#include <cmath>

namespace Engine
{
    // Movement system - processes Position and Velocity components
    class MovementSystem
    {
    public:
        static void Update(float deltaTime, PositionComponent& position, VelocityComponent& velocity)
        {
            // Update position based on velocity
            position.x += velocity.vx * deltaTime;
            position.y += velocity.vy * deltaTime;
            position.z += velocity.vz * deltaTime;
            
            // Apply simple friction
            const float friction = 0.95f;
            velocity.vx *= friction;
            velocity.vy *= friction;
            velocity.vz *= friction;
        }
    };
    
    // Physics system - processes Position, Velocity, and Physics components
    class PhysicsSystem
    {
    public:
        static void Update(float deltaTime, PositionComponent& position, VelocityComponent& velocity, PhysicsComponent& physics)
        {
            if (physics.isStatic) return;
            
            // Apply gravity
            if (physics.useGravity)
            {
                const float gravity = -9.81f;
                velocity.vy += gravity * deltaTime;
            }
            
            // Update position
            position.x += velocity.vx * deltaTime;
            position.y += velocity.vy * deltaTime;
            position.z += velocity.vz * deltaTime;
            
            // Simple ground collision
            if (position.y < 0.0f)
            {
                position.y = 0.0f;
                velocity.vy = -velocity.vy * physics.restitution;
            }
        }
    };
    
    // Health system - processes Health components
    class HealthSystem
    {
    public:
        static void Update(float deltaTime, HealthComponent& health)
        {
            // Check if entity is still alive
            if (health.currentHealth <= 0.0f)
            {
                health.isAlive = false;
            }
            
            // Regenerate health over time (example)
            if (health.isAlive && health.currentHealth < health.maxHealth)
            {
                health.currentHealth += 5.0f * deltaTime; // 5 HP per second
                health.currentHealth = std::min(health.currentHealth, health.maxHealth);
            }
        }
    };
    
    // AI system - processes AI components
    class AISystem
    {
    public:
        static void Update(float deltaTime, AIComponent& ai)
        {
            ai.aiTimer += deltaTime;
            
            if (ai.aiTimer >= ai.aiUpdateInterval)
            {
                ai.aiTimer = 0.0f;
                
                // Simple AI state machine
                switch (ai.aiState)
                {
                case 0: // Idle
                    ai.aiState = 1;
                    break;
                case 1: // Patrol
                    ai.aiState = 2;
                    break;
                case 2: // Attack
                    ai.aiState = 0;
                    break;
                }
            }
        }
    };
    
    // Collision system - processes Position and Collision components
    class CollisionSystem
    {
    public:
        static void Update(float deltaTime, PositionComponent& position, CollisionComponent& collision)
        {
            // Simple collision bounds checking
            if (!collision.isCollidable) return;
            
            // Check if entity is below ground level
            if (position.y < collision.radius)
            {
                position.y = collision.radius;
            }
            
            // Simple boundary checking (example: keep within 100x100 area)
            const float boundary = 50.0f;
            if (position.x < -boundary) position.x = -boundary;
            if (position.x > boundary) position.x = boundary;
            if (position.z < -boundary) position.z = -boundary;
            if (position.z > boundary) position.z = boundary;
        }
    };
    
    // Animation system - processes Animation components
    class AnimationSystem
    {
    public:
        static void Update(float deltaTime, AnimationComponent& animation)
        {
            const float animationDuration = 1.0f; // Default animation duration
            
            if (!animation.isLooping && animation.animationTime >= animationDuration)
            {
                return;
            }
            
            animation.animationTime += deltaTime * animation.animationSpeed;
            
            if (animation.isLooping)
            {
                animation.animationTime = std::fmod(animation.animationTime, animationDuration);
            }
        }
    };
    
    // Timer system - processes Timer components
    class TimerSystem
    {
    public:
        static void Update(float deltaTime, TimerComponent& timer)
        {
            if (!timer.isActive) return;
            
            timer.currentTime += deltaTime;
            
            if (timer.currentTime >= timer.duration)
            {
                if (timer.isRepeating)
                {
                    timer.currentTime = 0.0f;
                }
                else
                {
                    timer.isActive = false;
                }
            }
        }
    };
    
    // Input processing system - processes Input components
    class InputSystem
    {
    public:
        static void Update(float deltaTime, InputComponent& input, VelocityComponent& velocity)
        {
            const float moveSpeed = 5.0f;
            
            // Reset velocity
            velocity.vx = 0.0f;
            velocity.vz = 0.0f;
            
            // Apply input
            if (input.moveForward) velocity.vz -= moveSpeed;
            if (input.moveBackward) velocity.vz += moveSpeed;
            if (input.moveLeft) velocity.vx -= moveSpeed;
            if (input.moveRight) velocity.vx += moveSpeed;
        }
    };
    
    // Particle system - processes Particle components
    class ParticleSystem
    {
    public:
        static void Update(float deltaTime, ParticleComponent& particle)
        {
            particle.currentLife -= deltaTime;
            
            if (particle.currentLife <= 0.0f)
            {
                if (particle.maxParticles > 0)
                {
                    particle.currentLife = particle.lifeTime;
                    particle.maxParticles--;
                }
            }
        }
    };
    
    // Network synchronization system - processes Network components
    class NetworkSystem
    {
    public:
        static void Update(float deltaTime, NetworkComponent& network)
        {
            network.lastUpdateTime += deltaTime;
            
            if (network.lastUpdateTime >= network.updateInterval)
            {
                network.lastUpdateTime = 0.0f;
                
                // Here you would send network updates
                // For now, just mark that an update is needed
            }
        }
    };
} 