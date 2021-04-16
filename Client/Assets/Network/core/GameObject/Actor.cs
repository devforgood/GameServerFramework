using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif
#if _USE_BEPU_PHYSICS
#elif _USE_BULLET_SHARP
#elif _USE_BEPU_PHYSICS_V2
using BepuPhysics;
using BepuPhysics.Collidables;
#endif 

namespace core
{
    public enum ActorState
    {
        Idle = 0,
        Teleport = 1,
        Ghost = 2,
        Dash = 3,
    }

    public partial class Actor : NetGameObject
    {
        //public static readonly float TeleportDuration = 2.0f;
        public static readonly float GhostDuration = 8f;
        public static readonly float MaxDeltaTime = 0.05f;


        //static readonly float HALF_WORLD_HEIGHT = 3.6f;
        //static readonly float HALF_WORLD_WIDTH = 6.4f;

        public UInt32 degree
        {
            get;
            set;
        }
        public bool is_move;
        protected Vector3 lastLocation;
        protected Vector3 lastVelocity;
        public bool IsSlow;
        public bool IsIce;
        public float tileSpeed;

#if _USE_BEPU_PHYSICS
#elif _USE_BEPU_PHYSICS_V2
        private System.Numerics.Vector2 newTargetVelocity;
        private System.Numerics.Vector3 viewDirection;
#endif

        private int path_length = 1;
        private Vector3[] path = new Vector3[3];

        public byte ActionToken = 0;
        public byte GetNewActionToken() { return ++ActionToken; }

        public ActorState LastStateServerSide;
        public ActorState StateServerSide;
        public float EndState;
        public float BeginState;
        public Vector3 TargetPos;
        public float JumpPower;
        public float JumpDuration;


        public int LastBombNetworkId;

        public byte SelectedCharacter = 0;
        public int CharacterLevel = 0;

        public int killPlayerId = 0;
        public bool isLastKilled = false;

        public core.Team Team { get; set; }

#if MOVEMENT_DEBUG
        public float debug_x = 0.0f;
        public float debug_delta = 0.0f;
        public int debug_cnt = 0;
#endif


        //public Character character { get; set; }

        public JCharacterData characterData { get; set; }
        public List<JSkillData> skills = new List<JSkillData>();

        public BuffManager _buff = new BuffManager();
        public BuffManager buff { get { return _buff; } }

        // 현재 플레이어가 할당한 AI 리스트
        // 네트워크 아이디를 보관
        public List<int> AIPlayers = new List<int>();

        public bool IsHide;
        public List<ushort> HiddenMapObjects = new List<ushort>();

        public void ChangeStateServerSide(ActorState state, float duration)
        {
            LastStateServerSide = StateServerSide;
            StateServerSide = state;
            EndState = Timing.sInstance.GetFrameStartTime() + duration;
        }

        public virtual void OnChangedCharacter()
        {

        }

        public override byte GetClassId() { return (byte)GameObjectClassId.Actor; }



        public Actor(byte worldId)
        {
            WorldId = worldId;

            //mMaxRotationSpeed = 5.0f;
            //mMaxLinearSpeed = 5f;
            mVelocity = Vector3.zero;
            //mWallRestitution = 0.1f;

            //mActorRestitution = 0.1f;

            mThrustDir = 0.0f;

            mPlayerId = 0;

            mIsShooting = false;
            mIsBomb = false;

            mHealth = 10;
            SetCollisionRadius(0.3f);

            CacheAttributes();



            is_move = false;

            tileSpeed = 0f;

            IsHide = false;
        }

        ~Actor()
        {
            RemoveCacheAttributes();
        }

        public override void HandleDying()
        {
            RemoveCacheAttributes();
        }

        public override bool HandleCollisionWithActor(Actor inActor)
        {
            return false;
        }


        public void SetPhysics()
        {
            LogHelper.LogInfo("character physics setting");

#if _USE_BEPU_PHYSICS
            mCharacterController = new BEPUphysics.Character.CharacterController(new BEPUutilities.Vector3(GetLocation().x, GetLocation().y, GetLocation().z), 1.0f, 1.0f * 0.7f, 1.0f * 0.3f, 0.5f, 0.001f, 10f, 0.8f, 1.3f
                , 5.0f // standing speed
                , 3f, 1.5f, 1000, 0f, 0f, 0f, 0f
                );

            World.Instance(WorldId).space.Add(mCharacterController);
#elif _USE_BULLET_SHARP
            /*
            m_collisionShape = new BulletSharp.CapsuleShape(0.5f, 0.5f);

            m_collisionObject = new BulletSharp.PairCachingGhostObject();
            m_collisionObject.CollisionShape = m_collisionShape;
            m_collisionObject.CollisionFlags = BulletSharp.CollisionFlags.CharacterObject;
            mCharacterController = new BulletSharp.KinematicCharacterController(m_collisionObject, (BulletSharp.ConvexShape)m_collisionShape, 0.01f);


            BulletSharp.Math.Matrix worldTrans = new BulletSharp.Math.Matrix();
            worldTrans.M41 = 7f;
            worldTrans.M42 = 10f;
            worldTrans.M43 = 0;
            m_collisionObject.WorldTransform = worldTrans;
            m_collisionObject.UserObject = this;

            World.Instance(worldId).world.AddCollisionObject(m_collisionObject, BulletSharp.CollisionFilterGroups.DefaultFilter, BulletSharp.CollisionFilterGroups.Everything);
            World.Instance(worldId).world.AddAction(mCharacterController);
            */


            //m_collisionObject = new BulletSharp.RigidBody(new BulletSharp.RigidBodyConstructionInfo(1f, new BulletSharp.DefaultMotionState(), new BulletSharp.CapsuleShape(0.5f, 0.5f), new BulletSharp.Math.Vector3(0, 1f, 0)));
            //m_collisionObject.CollisionFlags = BulletSharp.CollisionFlags.StaticObject;

            //BulletSharp.Math.Matrix worldTrans = new BulletSharp.Math.Matrix();
            //worldTrans.M41 = 0f;
            //worldTrans.M42 =1f;
            //worldTrans.M43 = 0;
            //m_collisionObject.WorldTransform = worldTrans;


            //World.Instance(worldId).world.AddRigidBody((BulletSharp.RigidBody)m_collisionObject, BulletSharp.CollisionFilterGroups.DefaultFilter, BulletSharp.CollisionFilterGroups.Everything);






            ghostObject = new BulletSharp.PairCachingGhostObject();
            ghostObject.WorldTransform = BulletSharp.Math.Matrix.Translation(7f, 10f, 0f);
            World.Instance(worldId).Broadphase.OverlappingPairCache.SetInternalGhostPairCallback(new BulletSharp.GhostPairCallback());

            const float characterHeight = 0.5f;
            const float characterWidth = 0.5f;
            var capsule = new BulletSharp.CapsuleShape(characterWidth, characterHeight);
            //capsule.Margin = 0f;
            ghostObject.CollisionShape = capsule;
            ghostObject.CollisionFlags = BulletSharp.CollisionFlags.CharacterObject;

            const float stepHeight = 0.35f;
            BulletSharp.Math.Vector3 up = new BulletSharp.Math.Vector3(Vector3.up.x, Vector3.up.y, Vector3.up.z);
            character = new BulletSharp.KinematicCharacterController(ghostObject, capsule, stepHeight, ref up);



            World.Instance(worldId).world.AddCollisionObject(ghostObject, BulletSharp.CollisionFilterGroups.CharacterFilter, BulletSharp.CollisionFilterGroups.StaticFilter | BulletSharp.CollisionFilterGroups.DefaultFilter);

            World.Instance(worldId).world.AddAction(character);
#elif _USE_BEPU_PHYSICS_V2

            var initialPosition = new System.Numerics.Vector3(7f, 10f, 0f);
            float speculativeMargin = 0.1f;
            float mass = 1;
            float maximumHorizontalForce = 20;
            float maximumVerticalGlueForce = 100;
            float jumpVelocity = 6;
            //float speed = 4;
            float maximumSlope = (float)Math.PI * 0.4f;

            var shape = new Capsule(0.5f, 1);

            var shapeIndex = World.Instance(worldId).characters.Simulation.Shapes.Add(shape);

            var bodyHandle = World.Instance(worldId).characters.Simulation.Bodies.Add(BodyDescription.CreateDynamic(initialPosition, new BodyInertia { InverseMass = 1f / mass }, new CollidableDescription(shapeIndex, speculativeMargin), new BodyActivityDescription(shape.Radius * 0.02f)));
            character = World.Instance(worldId).characters.AllocateCharacter(bodyHandle);
            character.LocalUp = new System.Numerics.Vector3(0, 1, 0);
            character.CosMaximumSlope = (float)Math.Cos(maximumSlope);
            character.JumpVelocity = jumpVelocity;
            character.MaximumVerticalForce = maximumVerticalGlueForce;
            character.MaximumHorizontalForce = maximumHorizontalForce;
            character.MinimumSupportDepth = shape.Radius * -0.01f;          
            character.MinimumSupportContinuationDepth = -speculativeMargin;

            body = World.Instance(worldId).characters.Simulation.Bodies.GetBodyReference(bodyHandle);

#endif
        }

        protected float GetSpeed()
        {
            //추가되는 스피트(아이템등)도 해당 함수에서 처리
            float currSpeed = characterData.Speed + buff.AddItemMoveSpeed + buff.AddSpeedSlowByPer;

            //LogHelper.LogInfo($"GetSpeed {tileSpeed}");

            return currSpeed  + (currSpeed * tileSpeed);
        }

        public void ProcessInput(float inDeltaTime, InputState inInputState)
        {
            //process our input....
            is_move = inInputState.mIsMove;
            if (is_move)
            {
                degree = inInputState.mDirection;
                mDirection = core.MathHelpers.DegreeToVector3Cached((int)inInputState.mDirection);
            }

            //LogHelper.LogInfo("direction " + mDirection);
            //turning...


            //moving...
            //float inputForwardDelta = inInputState.GetDesiredVerticalDelta();
            if (is_move)
                mThrustDir = 1.0f;
            else
                mThrustDir = 0f;


            mIsShooting = inInputState.IsShooting();
            mIsBomb = inInputState.mIsBomb;

        }

        public void RecordLastVelocity()
        {
            var velocity = new Vector3(lastVelocity.x, 0, lastVelocity.z);
            velocity.Normalize2D();
        }

        public void AdjustVelocityByThrust(float inDeltaTime)
        {
            //just set the velocity based on the thrust direction -- no thrust will lead to 0 velocity
            //simulating acceleration makes the client prediction a bit more complex
            Vector3 forwardVector = GetForwardVector();
            mVelocity = forwardVector * (mThrustDir * inDeltaTime * GetSpeed());

            if(mVelocity.sqrMagnitude > 0f)
            {
                lastVelocity = mVelocity;
            }

#if _USE_BEPU_PHYSICS
#elif _USE_BEPU_PHYSICS_V2
            newTargetVelocity.X = mVelocity.x;
            newTargetVelocity.Y = mVelocity.z;
            viewDirection.X = mDirection.x;
            viewDirection.Y = mDirection.y;
            viewDirection.Z = mDirection.z;
#endif
            //LogHelper.LogInfo("mVelocity " + mVelocity);

        }

        public void SimulateMovement(float inDeltaTime)
        {
            // 최대 처리 델타 타임 초과시 보정
            // todo : 텔타 타임 초과시 호출횟수를 늘리는 방향으로 수정이 필요.
            if (inDeltaTime > MaxDeltaTime)
                inDeltaTime = MaxDeltaTime;

            //simulate us...
            AdjustVelocityByThrust(inDeltaTime);

            lastLocation = GetLocation();
            IsSlow = false;
            IsIce = false;

            SetLocation(GetLocation() + mVelocity);

#if _USE_BEPU_PHYSICS || _USE_BULLET_SHARP || _USE_BEPU_PHYSICS_V2
#else
            ProcessCollisions();
#endif
        }


        void SolveCollision(float radius, Vector3 location, float rx, float ry, float rw, float rh)
        {
            {
                var curLocation = GetLocation();
                mVelocity = -1*mVelocity * 0.2f;

                SetLocation(lastLocation);

                LogHelper.LogInfo($"solve collision {curLocation} -> {GetLocation()}");

            }

            //float x = location.x;
            //float y = location.z;

            //float vx = mVelocity.x;
            //float vy = mVelocity.z;

            //float left = rx - (rw * 0.5f);
            //float top = ry + (rh * 0.5f);
            //float right = rx + (rw * 0.5f);
            //float bottom = ry - (rh * 0.5f);



            //if ((y+radius) >= bottom  && (y + radius) < top && vy > 0) // 위에 방향으로 이동
            //{
            //    var lastLocation = GetLocation();
            //    mVelocity.z = -vy * mWallRestitution;
            //    location.z = bottom - (radius+0.1f);
            //    SetLocation(location);
            //    LogHelper.LogInfo($"solve up {lastLocation} -> {location}");
            //}
            //else if ((y-radius) <= top && (y - radius) > bottom && vy < 0) // 아래 방향으로 이동
            //{
            //    var lastLocation = GetLocation();
            //    mVelocity.z = -vy * mWallRestitution;
            //    location.z = top + (radius+0.1f);
            //    SetLocation(location);
            //    LogHelper.LogInfo($"solve down {lastLocation} -> {location}");
            //}

            //if ((x + radius) >= left && (x + radius) < right && vx > 0) // 오른쪽 방향으로 이동
            //{
            //    var lastLocation = GetLocation();
            //    mVelocity.x = -vx * mWallRestitution;
            //    location.x = left - (radius+0.1f);
            //    SetLocation(location);
            //    LogHelper.LogInfo($"solve right {lastLocation} -> {location}");
            //}
            //else if ((x-radius) <= right  && (x - radius) > left && vx < 0) // 왼쪽 방향으로 이동
            //{
            //    var lastLocation = GetLocation();
            //    mVelocity.x = -vx * mWallRestitution;
            //    location.x = right + (radius+0.1f);
            //    SetLocation(location);
            //    LogHelper.LogInfo($"solve left {lastLocation} -> {location}");
            //}
        }

        public void ProcessCollisions()
        {
            float sourceRadius = GetCollisionRadius();


            //now let's iterate through the world and see what we hit...
            //note: since there's a small number of objects in our game, this is fine.
            //but in a real game, brute-force checking collisions against every other object is not efficient.
            //it would be preferable to use a quad tree or some other structure to minimize the
            //number of collisions that need to be tested.
            for (int i = 0; i<World.Instance(WorldId).GetGameObjectCount();  ++i)
            {
                Collision(sourceRadius, GetLocation(), World.Instance(WorldId).GetGameObject(i));
            }

            foreach (var target in World.mStaticGameObjects)
            {
                Collision(sourceRadius, GetLocation(), target);
            }
        }

        [Obsolete("This function is obsolete")]
        public void ProcessCollisionsOld()
        {
            float sourceRadius = GetCollisionRadius();

            if(GetSpeed() < 4)
            {
                path[0] = GetLocation();
                path_length = 1;

            }
            else
            {
                path[0] = Vector3.Lerp(lastLocation, GetLocation(), 0.25f);
                path[1] = Vector3.Lerp(lastLocation, GetLocation(), 0.6f);
                path[2] = GetLocation();
                path_length = 3;
            }


            bool isCollision = false;
            for (int i = 0; i < path_length; ++i)
            {
                //now let's iterate through the world and see what we hit...
                //note: since there's a small number of objects in our game, this is fine.
                //but in a real game, brute-force checking collisions against every other object is not efficient.
                //it would be preferable to use a quad tree or some other structure to minimize the
                //number of collisions that need to be tested.
                foreach (var target in World.Instance(WorldId).GetGameObjects())
                {
                    if (Collision(sourceRadius, path[i], target))
                        isCollision = true;
                }

                foreach (var target in World.mStaticGameObjects)
                {
                    if (Collision(sourceRadius, path[i], target))
                        isCollision = true;
                }

                if (isCollision)
                    break;
            }
        }

        private bool Collision(float sourceRadius, Vector3 sourceLocation, NetGameObject target)
        {            
            if (target.GetNetworkId() != this.GetNetworkId() && !target.DoesWantToDie())
            {
                // 같은 층이 아닌 경우는 충돌 검사에서 제외
                if (target.DetectCollision(sourceRadius, sourceLocation) && IsSameFloor(target))
                {
                    //first, tell the other guy there was a collision with a cat, so it can do something...
                    if (target.HandleCollisionWithActor(this))
                    {
#if _USE_INPUT_SYNC
                        //simple collision test for spheres- are the radii summed less than the distance?
                        Vector3 targetLocation = target.GetLocation();
                        float targetRadius = target.GetCollisionRadius();

                        Vector3 delta = targetLocation - sourceLocation;
                        float distSq = delta.sqrMagnitude;
                        float collisionDist = (sourceRadius + targetRadius);
                        //if (distSq < (collisionDist * collisionDist))
                        collisionDist = 1.02f;


#if UNITY_EDITOR || DEBUG
                        LogHelper.LogInfo($"collision source{sourceLocation}, target{targetLocation}, delta{delta}, dist{collisionDist}, mapID{target.GetMapId()}, objectCount{World.Instance(WorldId).GetGameObjects().Count+ World.mStaticGameObjects.Count}");
                        LogHelper.LogDrawRay(sourceLocation, delta, new Vector3(1, 0, 1), 1);
                        Vector3 startPos = target.GetLocation() + Vector3.up * (target.rh * 0.5f) + Vector3.left * 0.5f + Vector3.back * (0.5f + target.rh - 1);
                        LogHelper.LogDrawLine(startPos, startPos + Vector3.right * (target.rw), new Vector3(1, 1, 1), 1);
                        LogHelper.LogDrawLine(startPos, startPos + Vector3.forward * (target.rh), new Vector3(1, 1, 1), 1);
                        startPos = target.GetLocation() + Vector3.up * (target.rh * 0.5f) + Vector3.right * (0.5f + target.rw - 1) + Vector3.forward * 0.5f;
                        LogHelper.LogDrawLine(startPos, startPos + Vector3.left * (target.rw), new Vector3(1, 1, 1), 1);
                        LogHelper.LogDrawLine(startPos, startPos + Vector3.back * (target.rh), new Vector3(1, 1, 1), 1);
#endif

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
                        if (distSq < ((targetLocation - lastLocation).sqrMagnitude - 0.01f))
                        {
                            LogHelper.LogInfo($"cur {distSq}, last {(targetLocation - lastLocation).sqrMagnitude}");
                            SolveCollision(sourceRadius, sourceLocation, target.GetLocation().x, target.GetLocation().z, target.rw, target.rh);
                        }
                        return true;
#endif

                        //okay, you hit something!
                        //so, project your location far enough that you're not colliding
                        Vector3 dirToTarget = delta;
                        dirToTarget.Normalize();
                        Vector3 acceptableDeltaFromSourceToTarget = dirToTarget * collisionDist;
                        //important note- we only move this cat. the other cat can take care of moving itself
                        SetLocation(targetLocation - acceptableDeltaFromSourceToTarget);

                        // Collision 크기가 1보다 큰 경우 처리
                        if (target.rw > 1 || target.rh > 1)
                        {
                            Vector3 movePosRW = Vector3.zero;
                            Vector3 movePosRH = Vector3.zero;

                            if (target.rw > 1 && sourceLocation.x >= targetLocation.x + 0.5f)
                                movePosRW = Vector3.right;
                            if (target.rh > 1 && sourceLocation.z >= targetLocation.z + 0.5f)
                                movePosRH = Vector3.forward;

                            Vector3 deltaRect = (targetLocation + movePosRW + movePosRH) - sourceLocation;
                            Vector3 dirToTargetRect = deltaRect;
                            dirToTargetRect.Normalize();
                            Vector3 acceptableDeltaFromSourceToTargetRect = dirToTargetRect * collisionDist;
                            SetLocation((targetLocation + movePosRW + movePosRH) - acceptableDeltaFromSourceToTargetRect);
#if UNITY_EDITOR || DEBUG
                            LogHelper.LogDrawRay(sourceLocation, deltaRect, new Vector3(0, 1, 1), 1);
                            startPos = target.GetLocation() + Vector3.up * (target.rh * 0.5f) + Vector3.left * 0.5f + Vector3.back * (0.5f + target.rh - 1);
                            LogHelper.LogDrawLine(startPos, startPos + Vector3.right * (target.rw), new Vector3(1, 1, 1), 1);
                            LogHelper.LogDrawLine(startPos, startPos + Vector3.forward * (target.rh), new Vector3(1, 1, 1), 1);
                            startPos = target.GetLocation() + Vector3.up * (target.rh * 0.5f) + Vector3.right * (0.5f + target.rw - 1) + Vector3.forward * 0.5f;
                            LogHelper.LogDrawLine(startPos, startPos + Vector3.left * (target.rw), new Vector3(1, 1, 1), 1);
                            LogHelper.LogDrawLine(startPos, startPos + Vector3.back * (target.rh), new Vector3(1, 1, 1), 1);
#endif
                        }


                        Vector3 relVel = mVelocity;

                        //if other object is a cat, it might have velocity, so there might be relative velocity...
                        Actor targetActor = target.GetAsActor();
                        if (targetActor != null)
                        {
                            relVel -= targetActor.mVelocity;
                        }

                        //got vel with dir between objects to figure out if they're moving towards each other
                        //and if so, the magnitude of the impulse ( since they're both just balls )
                        float relVelDotDir = Vector3.Dot(relVel, dirToTarget);

                        if (relVelDotDir > 0.0f)
                        {
                            Vector3 impulse = relVelDotDir * dirToTarget;

                            if (targetActor != null)
                            {
                                mVelocity -= impulse;
                                mVelocity *= mActorRestitution;
                            }
                            else
                            {
                                mVelocity -= impulse * 2.0f;
                                mVelocity *= mWallRestitution;
                            }

                        }

                        return true;
#else
                        // Solve collision disable
                        // LogHelper.LogInfo($"collision source{sourceLocation}, objectCount{World.Instance(WorldId).GetGameObjects().Count + World.mStaticGameObjects.Count}");
                        return true;
#endif
                    }
                }
                else
                {
                    target.HandleExitCollisionWithActor(this);
                }
            }

            return false;
        }

        public static NetGameObject StaticCreate(byte worldId) { return new Actor(worldId); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }

        public override Actor GetAsActor() { return this; }


        public virtual void TryTeleport(Vector3 pos, float power, float duration, int map_uid)
        {

        }

        public override void CompleteRemove()
        {
#if _USE_BEPU_PHYSICS
            mCharacterController.OnRemovalFromSpace(World.Instance(WorldId).space);
#elif _USE_BULLET_SHARP
            World.Instance(WorldId).world.RemoveCollisionObject(ghostObject);
            World.Instance(WorldId).world.RemoveAction(character);
#elif _USE_BEPU_PHYSICS_V2
            World.Instance(WorldId).characters.RemoveCharacterByBodyHandle(character.BodyHandle);
#endif
        }

        public override bool LateUpdate()
        {
#if _USE_BEPU_PHYSICS
            Vector3 v = new Vector3(mCharacterController.Body.Position.X, mCharacterController.Body.Position.Y, mCharacterController.Body.Position.Z);
            if(v.Equals(GetLocation()) == false)
            {
                //LogHelper.LogInfo("old location " + GetLocation() + ", new location " + v);
                SetLocation(v);
                return true;
            }

            //Vector3 v2 = new Vector3(mCharacterController.Body.LinearVelocity.X, mCharacterController.Body.LinearVelocity.Y, mCharacterController.Body.LinearVelocity.Z);
            //if (v2.Equals(GetVelocity()) == false)
            //{
            //    SetVelocity(v2);
            //}

            //mCharacterController.HorizontalMotionConstraint.MovementDirection = BEPUutilities.Vector2.Zero;
#elif _USE_BULLET_SHARP

            if (Math.Abs(GetLocation().x - ghostObject.WorldTransform.M41) > 0.0001  
                || Math.Abs(GetLocation().y - ghostObject.WorldTransform.M42) > 0.0001 
                || Math.Abs(GetLocation().z - ghostObject.WorldTransform.M43) > 0.0001 )
            {
                Vector3 v = new Vector3(ghostObject.WorldTransform.M41, ghostObject.WorldTransform.M42, ghostObject.WorldTransform.M43);
                //LogHelper.LogInfo("old location " + GetLocation() + ", new location " + v + ", " + ghostObject.WorldTransform.ToString());
                SetLocation(v);
            }
#elif _USE_BEPU_PHYSICS_V2
            Vector3 v = new Vector3(body.Pose.Position.X, body.Pose.Position.Y, body.Pose.Position.Z);
            if (v.Equals(GetLocation()) == false)
            {
                //LogHelper.LogInfo("old location " + GetLocation() + ", new location " + v);
                SetLocation(v);
            }
#endif

            return false;
        }

        public void SetPlayerId(int inPlayerId) { mPlayerId = inPlayerId; }
        public void SetWorldId(byte worldId) {  WorldId = worldId; }
        public int GetPlayerId() { return mPlayerId; }

        public void SetVelocity(Vector3 inVelocity) { mVelocity = inVelocity; }
        public ref Vector3 GetVelocity() { return ref mVelocity; }

        public int GetHealth() { return mHealth; }

        public int GetCharacterHp()
        {
            return characterData.GetCharacterHp(CharacterLevel);
        }

        public void IncreHealth(int health, JSpellData data)
        {
            var lastHealth = mHealth;
            mHealth += health;
            if (GetCharacterHp() < mHealth)
            {
                mHealth = GetCharacterHp();
            }
            Dirty((uint)ReplicationState.Health);

            if (data != null)
            {
                buff.AddBuff((Buff)GameObjectRegistry.sInstance.CreateGameObject((UInt32)GameObjectClassId.Buff, true, (byte)WorldId), data, GetNetworkId(), GetCharacterHp());
            }

            var hp = mHealth - lastHealth;
            if (hp > 0)
            {
                LogHelper.LogInfo($"IncreHealth playerId:{GetPlayerId()}, hp:{hp}");
                InvokeClientRpcOnClient(NoticeHealth, GetPlayerId(), hp);
            }
        }

        public void ResetHealth(int health, JSpellData data)
        {
            mHealth = health;
            if (GetCharacterHp() < mHealth)
            {
                mHealth = GetCharacterHp();
            }
            Dirty((uint)ReplicationState.Health);

            if (data != null)
            {
                buff.AddBuff((Buff)GameObjectRegistry.sInstance.CreateGameObject((UInt32)GameObjectClassId.Buff, true, (byte)WorldId), data, GetNetworkId(), GetCharacterHp());
            }
        }


        Vector3 mVelocity = new Vector3();


        //float mMaxLinearSpeed;
        //float mMaxRotationSpeed;

        //bounce fraction when hitting various things
        //float mWallRestitution;
        //float mActorRestitution;


        int mPlayerId;
        public string UserId;


        ///move down here for padding reasons...

        protected float mLastMoveTimestamp;

        protected float mThrustDir;
        protected int mHealth; // max (4095) = 12bit

        protected bool mIsShooting;
        protected bool mIsBomb;

#if _USE_BEPU_PHYSICS
        public BEPUphysics.Character.CharacterController mCharacterController = null;
        protected BEPUutilities.Vector3 physicsLocation = new BEPUutilities.Vector3();
        protected BEPUutilities.Vector3 physicsVelocity = new BEPUutilities.Vector3();
        protected BEPUutilities.Vector2 physicsVelocity2 = new BEPUutilities.Vector2();



#elif _USE_BULLET_SHARP
        protected BulletSharp.PairCachingGhostObject ghostObject;
        protected BulletSharp.KinematicCharacterController character;
#elif _USE_BEPU_PHYSICS_V2
        public BepuPhysics.CharacterController character;
        public BodyReference body;
#endif
        protected void Awake()
        {
#if _USE_BEPU_PHYSICS

            //mCharacterController.TeleportToPosition(lastLocation.CopyTo(ref physicsLocation), 0f);
            //mCharacterController.Body.Position = lastLocation.CopyTo(ref physicsLocation);

            mCharacterController.HorizontalMotionConstraint.MovementDirection = GetVelocity().CopyTo(ref physicsVelocity2);
            //LogHelper.LogInfo($"net({GetLocation().x}, {GetLocation().y}, {GetLocation().z})");

            //mCharacterController.HorizontalMotionConstraint.SpeedScale

            //mCharacterController.Body.Position = GetLocation().CopyTo(ref physicsLocation);
            //mDirection.CopyTo(ref mCharacterController.HorizontalMotionConstraint.LastDirection);
            //if (mCharacterController.HorizontalMotionConstraint.MovementMode != BEPUphysics.Character.MovementMode.Floating)
            //{
            //    if (GetVelocity().IsZero() == false)
            //    {
            //        mCharacterController.Body.LinearVelocity = GetVelocity().CopyTo(ref physicsVelocity);
            //    }
            //}

#elif _USE_BULLET_SHARP
            //LogHelper.LogInfo($"net({GetLocation().x}, {GetLocation().y}, {GetLocation().z}), phz({ghostObject.WorldTransform.M41},{ghostObject.WorldTransform.M42},{ghostObject.WorldTransform.M43})");
            ghostObject.WorldTransform = BulletSharp.Math.Matrix.Translation(GetLocation().x, GetLocation().y, GetLocation().z); ;

#elif _USE_BEPU_PHYSICS_V2
            //LogHelper.LogInfo($"velocity old({character.TargetVelocity.X}, {character.TargetVelocity.Y}), new({newTargetVelocity.X},{newTargetVelocity.Y})");

            if (!body.Awake &&
                ((character.TryJump && character.Supported) ||
                newTargetVelocity != character.TargetVelocity ||
                (newTargetVelocity != System.Numerics.Vector2.Zero && character.ViewDirection != viewDirection)))
            {
                World.Instance(WorldId).characters.Simulation.Awakener.AwakenBody(character.BodyHandle);
            }
            character.TargetVelocity = newTargetVelocity;
            character.ViewDirection = viewDirection;

            body.Pose.Position.X = GetLocation().x;
            body.Pose.Position.Y = GetLocation().y;
            body.Pose.Position.Z = GetLocation().z;
            
#endif


        }

        public int GetDamage(int skill_id, GameObjectClassId target_id)
        {
            var skillData = ACDC.SkillData[ skill_id ];
            if (skillData == null)
                return 0;

            int damage;
            if (target_id == GameObjectClassId.Actor
                || target_id == GameObjectClassId.Castle)
            {
                var skillDamage = skillData.GetDamage(CharacterLevel);

                // 파워업 잼 버프 상태에 따라 추가 데미지가 발생
                // SiegeAtk 해당 버프가 적용되지 않는다. 이미 기본 데미지를 가지고 정해진 값이므로
                // 차후 하나의 버프에서 두개 이상의 스탯에 영향을 주도록 수정한다면 가능
                (var exist, var status) = _buff.GetBuff(BuffType.PowerUpGem);
                if (exist)
                {
                    damage = skillDamage + (int)(skillDamage * status);
                }
                else
                {
                    damage = skillDamage;
                }
            }
            else
                damage = skillData.SiegeAtk;



            return damage;
        }


        protected bool SetCharacterData(byte characterId, int characterLevel)
        {
            characterData = ACDC.CharacterData[characterId];
            if (characterData == null)
            {
                return false;
            }
            SelectedCharacter = characterId;
            CharacterLevel = characterLevel;

            skills.Add(ACDC.SkillData[characterData.BombID]);
            skills.Add(ACDC.SkillData[characterData.Skill1ID]);
            // link skill
            for (int i = 0; i < (int)eSkillKind.eNONE; ++i)
            {
                if (skills[i] != null)
                {
                    skills.Add(ACDC.SkillData[skills[i].linkSkillId]);
                }
            }
            return true;
        }

        protected int GetSkillId(bool IsBasicAttack)
        {
            if (IsBasicAttack)
                return characterData.BombID;

            return characterData.Skill1ID;
        }

        // 기본 공격
        public JSkillData BasicAttack { get { return skills[(int)eSkillKind.eBasicAttack]; } }

        // 필살기 (SpecialMove)
        public JSkillData SpecialAttack { get { return skills[(int)eSkillKind.eSpecialAttack]; } }
    }
}
