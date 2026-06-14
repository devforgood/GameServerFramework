#pragma once
#include <vector>
#include <unordered_map>
#include <unordered_set>
#include <memory>
#include <typeindex>
#include <cstdint>
#include <stdexcept>

namespace engine
{
    // Entity is just an ID
    using EntityID = uint32_t;
    
    // Component base class - all components must inherit from this
    struct Component
    {
        virtual ~Component() = default;
    };
    
    // Component array interface
    class IComponentArray
    {
    public:
        virtual ~IComponentArray() = default;
        virtual void EntityDestroyed(EntityID entity) = 0;
    };
    
    // Component array for specific component type
    template<typename T>
    class ComponentArray : public IComponentArray
    {
    public:
        ComponentArray() : m_Size(0) {}
        
        // Pre-allocate some space for better performance
        void Reserve(size_t capacity)
        {
            m_ComponentArray.reserve(capacity);
        }
        void InsertData(EntityID entity, T component)
        {
            // Ensure entity doesn't already exist
            if (m_EntityToIndexMap.find(entity) != m_EntityToIndexMap.end())
            {
                return;
            }
            
            // Put new entry at end
            size_t newIndex = m_Size;
            
            // Ensure vector has enough capacity
            if (newIndex >= m_ComponentArray.size())
            {
                m_ComponentArray.resize(newIndex + 1);
            }
            
            m_EntityToIndexMap[entity] = newIndex;
            m_IndexToEntityMap[newIndex] = entity;
            m_ComponentArray[newIndex] = component;
            m_Size++;
        }
        
        void RemoveData(EntityID entity)
        {
            // Ensure entity exists
            if (m_EntityToIndexMap.find(entity) == m_EntityToIndexMap.end())
            {
                return;
            }
            
            // Copy element at end into deleted element's place to maintain density
            size_t indexOfRemovedEntity = m_EntityToIndexMap[entity];
            size_t indexOfLastElement = m_Size - 1;
            
            // Ensure we don't access invalid indices
            if (indexOfRemovedEntity < m_ComponentArray.size() && indexOfLastElement < m_ComponentArray.size())
            {
                m_ComponentArray[indexOfRemovedEntity] = m_ComponentArray[indexOfLastElement];
            }
            
            // Update map to point to moved spot
            EntityID entityOfLastElement = m_IndexToEntityMap[indexOfLastElement];
            m_EntityToIndexMap[entityOfLastElement] = indexOfRemovedEntity;
            m_IndexToEntityMap[indexOfRemovedEntity] = entityOfLastElement;
            
            m_EntityToIndexMap.erase(entity);
            m_IndexToEntityMap.erase(indexOfLastElement);
            
            m_Size--;
        }
        
        T& GetData(EntityID entity)
        {
            // Ensure entity exists
            if (m_EntityToIndexMap.find(entity) == m_EntityToIndexMap.end())
            {
                throw std::runtime_error("Entity does not exist in component array");
            }
            
            size_t index = m_EntityToIndexMap[entity];
            if (index >= m_ComponentArray.size())
            {
                throw std::runtime_error("Component array index out of bounds");
            }
            
            return m_ComponentArray[index];
        }
        
        bool HasData(EntityID entity)
        {
            return m_EntityToIndexMap.find(entity) != m_EntityToIndexMap.end();
        }
        
        void EntityDestroyed(EntityID entity) override
        {
            if (m_EntityToIndexMap.find(entity) != m_EntityToIndexMap.end())
            {
                RemoveData(entity);
            }
        }
        
        // Get raw array for cache-friendly iteration
        T* GetArray() { return m_ComponentArray.data(); }
        size_t GetSize() const { return m_Size; }
        size_t GetCapacity() const { return m_ComponentArray.size(); }
        
    private:
        // Packed array of components (of type T)
        std::vector<T> m_ComponentArray;
        
        // Map from entity ID to array index
        std::unordered_map<EntityID, size_t> m_EntityToIndexMap;
        
        // Map from array index to entity ID
        std::unordered_map<size_t, EntityID> m_IndexToEntityMap;
        
        // Total size of valid entries in the array
        size_t m_Size = 0;
    };
    
    // System base class
    class ISystem
    {
    public:
        virtual ~ISystem() = default;
        virtual void Update(float deltaTime) = 0;
    };
    
    // Entity Manager
    class EntityManager
    {
    public:
        EntityID CreateEntity()
        {
            EntityID entityID = m_NextEntityID++;
            m_Entities.insert(entityID);
            return entityID;
        }
        
        void DestroyEntity(EntityID entity)
        {
            m_Entities.erase(entity);
            
            // Notify each component array that an entity has been destroyed
            for (auto const& pair : m_ComponentArrays)
            {
                auto const& component = pair.second;
                component->EntityDestroyed(entity);
            }
        }
        
        template<typename T>
        void RegisterComponent()
        {
            const char* typeName = typeid(T).name();
            
            // Ensure component type hasn't been registered before
            if (m_ComponentTypes.find(typeName) != m_ComponentTypes.end())
            {
                return;
            }
            
            // Add this component type to the component type map
            m_ComponentTypes.insert({typeName, m_NextComponentType});
            
            // Create a ComponentArray pointer and add it to the component arrays map
            m_ComponentArrays.insert({typeName, std::make_shared<ComponentArray<T>>()});
            
            m_NextComponentType++;
        }
        
        template<typename T>
        void AddComponent(EntityID entity, T component)
        {
            GetComponentArray<T>()->InsertData(entity, component);
        }
        
        template<typename T>
        void RemoveComponent(EntityID entity)
        {
            GetComponentArray<T>()->RemoveData(entity);
        }
        
        template<typename T>
        T& GetComponent(EntityID entity)
        {
            return GetComponentArray<T>()->GetData(entity);
        }
        
        template<typename T>
        bool HasComponent(EntityID entity)
        {
            return GetComponentArray<T>()->HasData(entity);
        }
        
        template<typename T>
        ComponentArray<T>* GetComponentArray()
        {
            const char* typeName = typeid(T).name();
            
            // Ensure component type exists
            if (m_ComponentTypes.find(typeName) == m_ComponentTypes.end())
            {
                throw std::runtime_error("Component type not registered");
            }
            
            return static_cast<ComponentArray<T>*>(m_ComponentArrays[typeName].get());
        }
        
        // Get all entities that have all of the specified components
        template<typename... T>
        std::vector<EntityID> GetEntitiesWithComponents()
        {
            std::vector<EntityID> entities;
            
            for (auto const& entity : m_Entities)
            {
                if ((HasComponent<T>(entity) && ...))
                {
                    entities.push_back(entity);
                }
            }
            
            return entities;
        }
        
    private:
        // Map to keep track of the component type IDs
        std::unordered_map<const char*, size_t> m_ComponentTypes;
        
        // Map to keep track of the component arrays
        std::unordered_map<const char*, std::shared_ptr<IComponentArray>> m_ComponentArrays;
        
        // The entity ID counter
        EntityID m_NextEntityID = 0;
        
        // The component type counter
        size_t m_NextComponentType = 0;
        
        // Set of entities that are currently active
        std::unordered_set<EntityID> m_Entities;
    };
} 