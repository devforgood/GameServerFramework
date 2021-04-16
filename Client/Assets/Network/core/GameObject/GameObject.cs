using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif 


namespace core
{
    public enum GameObjectClassId
    {
        GameObject,
        Prop,
        Actor,
        Projectile,
        Bomb,
        GameMode,
        Item,
        AreaOfEffect,
        Castle,
        TreasureBox,
        Trap,
        PropCooldown,
        Train,
        PropHealth,
        Button,
        Buff,
    }

    public partial class NetGameObject
    {
        public static readonly int NetworkIdSize = 16; // bit

        Vector3 mLocation;
        public Vector3 mDirection;
        public Vector3 GetRotation() { return mDirection; }

        float mCollisionRadius;

        float mScale;
        int mIndexInWorld;

        bool mDoesWantToDie;
        public bool Dead;

        int mNetworkId;

        public Vector2 Scale;

        public float floor;

        public float widthHalf;
        public float heightHalf;
        public bool IsCreate;

        public NetGameObject()
        {
            Scale.x = 1f;
            Scale.y = 1f;

            floor = 0f;
            widthHalf = 0.5f;
            heightHalf = 0.5f;
            IsCreate = true;
        }

        public delegate object CreateGameObject();

        public static CreateGameObject FuncCreateGameObject = null;

        public float rx { get { return mLocation.x - widthHalf; } }
        public float ry { get { return mLocation.z - heightHalf; } }
        public float rw { get { return Scale.x; } }
        public float rh { get { return Scale.y; } }


        public int NetworkId { get { return mNetworkId; } }

        public byte WorldId { get; set; }

        public virtual byte GetClassId() { return (byte)GameObjectClassId.GameObject; }


        public virtual Actor GetAsActor() { return null; }

        public virtual AreaOfEffect GetAsAreaOfEffect() { return null; }
        public virtual UInt32 GetAllStateMask() { return 0; }

        public virtual bool HandleCollisionWithActor(Actor inActor) { return true; }
        public virtual void HandleExitCollisionWithActor(Actor inActor) { }

        public void OnUpdate()
        {
            if (!DoesWantToDie())
            {
                Update();
            }
            //you might suddenly want to die after your update, so check again
            if (DoesWantToDie())
            {
                if (Dead == false)
                {
                    World.Instance(0).RemoveGameObject(this);
                    HandleDying();
                    Dead = true;
                }
            }
        }

        public virtual void Update() { }
        public virtual bool LateUpdate() { return false; }
        public virtual void HandleDying() { }

        public void SetIndexInWorld(int inIndex) { mIndexInWorld = inIndex; }
        public int GetIndexInWorld() { return mIndexInWorld; }



        public void SetScale(float inScale) { mScale = inScale; }
        public float GetScale() { return mScale; }


        public ref Vector3 GetLocation() { return ref mLocation; }
        public void SetLocation(Vector3 inLocation)
        {
            //if (inLocation.Equals(mLocation) == false)
            //{
            //    World.Instance(WorldId).mWorldMap.ChangeLocation(this, mLocation, inLocation);
            //}
            mLocation = inLocation;
//#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
//            Debug.Log($"set loc x{mLocation.x}, z{mLocation.z}");
//#endif 

        }

        public float GetCollisionRadius() { return mCollisionRadius; }
        public void SetCollisionRadius(float inRadius) { mCollisionRadius = inRadius; }

        public Vector3 GetForwardVector()
        {
            return mDirection;
        }

        public bool DoesWantToDie() { return mDoesWantToDie; }
        public void SetDoesWantToDie(bool inWants) { mDoesWantToDie = inWants; Dead = false; }

        public int GetNetworkId() { return mNetworkId; }
        public void SetNetworkId(int inNetworkId)
        {
            //this doesn't put you in the map or remove you from it
            mNetworkId = inNetworkId;

        }

        public virtual UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {
            return 0;
        }
        public virtual void Read(NetIncomingMessage inInputStream) { }

        protected virtual void Dirty(uint state) { }
        protected virtual void DirtyExcept(int playerId, uint state) { }


        public virtual void CompleteCreate()
        {

        }

        public virtual void CompleteRemove()
        {

        }

        public virtual int OnExplode(int player_id, int parentNetworkId, int damage)
        {
            return 0;
        }

        public void SetFloor(float y)
        {
            floor = (float)Math.Round(y);
        }

        public bool IsSameFloor(NetGameObject other)
        {
            if (other.GetAsAreaOfEffect() != null)
            {
                return IsSameFloorWithHeight(floor, other);
            }
            else
            {
                return IsSameFloor(floor, other);
            }
        }


        public static bool IsSameFloor(float current_floor, NetGameObject other)
        {
            var target_floor = other.GetLocation().y;

            // 비교 대상이 플레이어이면 층 보정 x
            // 이전에 구해놓은 층으로 비교
            if (other.GetAsActor() != null)
                target_floor = other.floor;

            return current_floor == target_floor;
        }

        public static bool IsSameFloorWithHeight(float current_floor, NetGameObject other)
        {
            var target_floor = other.GetLocation().y;

            float heightMin = target_floor - other.heightHalf;
            float heightMax = target_floor + other.heightHalf;
#if UNITY_EDITOR || DEBUG
            LogHelper.LogInfo($"curr {current_floor} heightMin {heightMin} heightMax {heightMax} target {target_floor}");
#endif
            return (current_floor <= heightMax && current_floor >= heightMin);
        }

        public virtual bool DetectCollision(float sourceRadius, Vector3 sourceLocation)
        {
            return MathHelpers.circleRect(sourceLocation.x, sourceLocation.z, sourceRadius, this.rx, this.ry, this.rw, this.rh);
        }

        public virtual int GetMapId() { return 0; }
    }
}
