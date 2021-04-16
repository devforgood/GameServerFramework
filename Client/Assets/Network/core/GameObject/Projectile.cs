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
    public class Projectile : NetGameObject
    {
        public override byte GetClassId() { return (byte)GameObjectClassId.Projectile; }


        enum ReplicationState
        {
            Pose = 1 << 0,
            ProjectileType = 1 << 1,
            PlayerId = 1 << 2,

            AllState = Pose | ProjectileType | PlayerId
        };

        public enum ProjectileType
        {
            SphericalLinear,
            linear,
        }


        public static NetGameObject StaticCreate(byte worldId) { return new Projectile(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }


        protected Vector3 mVelocity;
        protected Vector3 mStartLocation;

        protected float mMuzzleSpeed;
        protected int mPlayerId;
        protected float mCreateTime;
        public ProjectileType projectileType;

        protected Projectile()
        {
            mMuzzleSpeed = 5.0f;
            mVelocity = Vector3.zero;
            mPlayerId = 0;
            SetScale(GetScale() * 0.25f);
            SetCollisionRadius(0.125f);

            mCreateTime = Timing.sInstance.GetFrameStartTime();
            projectileType = ProjectileType.SphericalLinear;
        }


        public void SetVelocity(Vector3 inVelocity) { mVelocity = inVelocity; }
        public ref Vector3 GetVelocity() { return ref mVelocity; }

        public void SetPlayerId(int inPlayerId) { mPlayerId = inPlayerId; }
        public int GetPlayerId() { return mPlayerId; }

        public override UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {
            UInt32 writtenState = 0;

            if ((inDirtyState & (UInt32)ReplicationState.ProjectileType) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write((UInt32)projectileType, 2);

                writtenState |= (UInt32)ReplicationState.ProjectileType;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.Pose) != 0)
            {
                inOutputStream.Write((bool)true);

                if (projectileType == ProjectileType.linear)
                {
                    inOutputStream.Write(ref GetLocation());
                }
                else
                {
                    inOutputStream.Write(ref mStartLocation);
                }

                inOutputStream.Write(ref GetVelocity());

                inOutputStream.Write(ref mDirection);

                writtenState |= (UInt32)ReplicationState.Pose;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.PlayerId) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write(mPlayerId);

                writtenState |= (UInt32)ReplicationState.PlayerId;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            return writtenState;
        }

        public override void Read(NetIncomingMessage inInputStream)
        {
            bool stateBit;

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                projectileType = (ProjectileType)inInputStream.ReadUInt32(2);
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                Vector3 location = default(Vector3);
                inInputStream.Read(ref location);


                Vector3 velocity = default(Vector3);
                inInputStream.Read(ref velocity);
                SetVelocity(velocity);

                if (projectileType == ProjectileType.linear)
                {
                    //dead reckon ahead by rtt, since this was spawned a while ago!
                    SetLocation(location + velocity * NetworkManager.Instance.GetRoundTripTimeClientSide());
                    //Debug.Log("linear");
                }
                else if (projectileType == ProjectileType.SphericalLinear)
                {
                    //core.LogHelper.LogInfo($"read location x{location.x}, y{location.y}, z{location.z}");

                    SetLocation(location);
                    mCreateTime = core.Timing.sInstance.GetFrameStartTime() - NetworkManager.Instance.GetRoundTripTimeClientSide();
                }
                else
                {
                    SetLocation(location);
                }

                inInputStream.Read(ref mDirection);
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mPlayerId = inInputStream.ReadInt32();
            }

        }
        public override bool HandleCollisionWithActor(Actor inActor)
        {
             return false;
        }


        public void InitFromShooter(Actor inShooter)
        {
            SetPlayerId((int)inShooter.GetPlayerId());

            Vector3 forward = inShooter.GetForwardVector();
            SetVelocity(inShooter.mDirection + forward * mMuzzleSpeed);
            SetLocation(inShooter.GetLocation());

            mDirection = inShooter.GetRotation();
            mStartLocation = inShooter.GetLocation();

            LogHelper.LogInfo($"start x{mStartLocation.x}, y{mStartLocation.y}, z{mStartLocation.z}");

        }

        public override void Update()
        {
            // 밀어서 설치
            switch(projectileType)
            {
                case ProjectileType.linear:
                    // 밀어서 설치
                    float deltaTime = Timing.sInstance.GetDeltaTime();
                    SetLocation(GetLocation() + mVelocity * deltaTime);
                    break;


                case ProjectileType.SphericalLinear:
                    // 던저서 설치
                    float gravity = 25f;
                    float radians = (float)core.MathHelpers.DegreeToRadian(70);
                    float yOffset = 0.0001f; // 발사 지점과 목표 지점간의 y축 차이
                    float distance = 6.5f;

                    // 발사 속도 계산
                    float speed = (distance * (float)Math.Sqrt(gravity) * (float)Math.Sqrt(1 / (float)Math.Cos(radians))) / (float)Math.Sqrt(2 * distance * Math.Sin(radians) + 2 * yOffset * (float)Math.Cos(radians));



                    float ySpeed = speed * (float)Math.Sin(radians);
                    float flight_time = (ySpeed + (float)Math.Sqrt((ySpeed * ySpeed) + 2 * gravity * yOffset)) / gravity;
                    float elapse_time = (Timing.sInstance.GetFrameStartTime() - mCreateTime) * 1.2f; // 속도 조절 상수값
                    if (flight_time < elapse_time)
                        elapse_time = flight_time;

                    float x = distance * elapse_time / flight_time;

                    float t = x / (speed * (float)Math.Cos(radians));
                    float y = -0.5f * gravity * (t * t) + speed * (float)Math.Sin(radians) * t;

                    //Vector3 loc = GetLocation();
                    //loc.z = x;
                    //loc.y = y+2.0f;

                    Vector3 loc;
                    loc.x = 0;
                    loc.y = y;
                    loc.z = x;


                    // 발사 방향에 따른 회전
                    var quat = Quaternion.LookRotation(mDirection);
                    loc = quat.Transform(loc);

                    // 케릭터 위치 기준으로 발사 지점
                    SetLocation(loc + mStartLocation);

                    //LogHelper.LogInfo($"x{x}, y{y},   real x{GetLocation().x}, y{GetLocation().y}, z{GetLocation().z}");

                    break;

            }
        }
    }
}
