using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

#if _USE_BEPU_PHYSICS
using BEPUphysics.Entities.Prefabs;
#elif _USE_BULLET_SHARP
#elif _USE_BEPU_PHYSICS_V2
using BepuPhysics;
using BepuUtilities.Memory;
using BepuUtilities;
using BepuPhysics.Collidables;
#endif


public static class SpawnPositionExtensions
{
    public static void SortSpawnPosition(this List<JSpawnData> spawn)
    {
        spawn.Sort(delegate (JSpawnData x, JSpawnData y)
        {
            if (x.spawnNum == y.spawnNum)
            {
                if (x.spawnTeam == y.spawnTeam)
                {
                    return 0;
                }
                else if (x.spawnTeam > y.spawnTeam)
                {
                    return 1;
                }
                return -1;

            }
            else if (x.spawnNum > y.spawnNum)
            {
                return 1;
            }
            return -1;
        });
    }
}

namespace core
{
    public class World
    {
        public const int DefaultWorldIndex = 0;
        public const int DefaultWorldCount = 1;

        public static JMapData Map;

        List<NetGameObject> mGameObjects = new List<NetGameObject>();
        //public WorldMap mWorldMap;
        private byte mWorldId = 0;
        public static List<StaticGameObject> mStaticGameObjects = new List<StaticGameObject>();

        public static List<JSpawnData> spawn_position = new List<JSpawnData>();
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        public static Dictionary<int, JMapObjectData> mapGameObject = new Dictionary<int, JMapObjectData>();

        // playerID, Actor
        public Dictionary<int, Actor> playerList = new Dictionary<int, Actor>();
        // playerID, Actor
        public Dictionary<int, Actor> aiList = new Dictionary<int, Actor>();

        public int RemovePlayerCount = 0;
        public int TimeoutPlayerCount = 0;
        public bool ReadyPlay = false;
        public float TimeoutStartPlay;

        public GameMode GameMode = new GameMode();

        // team, castle
        public Dictionary<int, Castle> castleList = new Dictionary<int, Castle>();

        // GameObjectClassId, map_uid, NetGameObject
        Dictionary<GameObjectClassId, Dictionary<int, NetGameObject>> mapNetGameObject = new Dictionary<GameObjectClassId, Dictionary<int, NetGameObject>>();


        public TickCounter mTickCounter = new TickCounter();

#if _USE_BEPU_PHYSICS
        public BEPUphysics.Space space { get; set; }
#elif _USE_BULLET_SHARP

        public BulletSharp.MultiBodyDynamicsWorld world;
        public BulletSharp.AxisSweep3 Broadphase;
        public BulletSharp.RigidBody floor;
#elif _USE_BEPU_PHYSICS_V2
        public Simulation Simulation;
        public BufferPool BufferPool = new BufferPool();
        public CharacterControllers characters;
#endif

        /// <summary>
        /// Global instance of GameObjectRegistry
        /// </summary>
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        private static World[] sInstance = new World[] { new World(), new World() {mWorldId = 1, } };

        public static void SetInstance(World [] world)
        {
            sInstance = world;
        }

        public static World Instance(byte worldId = 0)
        {
            return sInstance[worldId];
        }

        /// <summary>
        /// 월드 데이터 서버 초기화
        /// </summary>
        public static void InitServer(byte world_count, byte start_index, NetworkManager networkManager)
        {
            sInstance = new World [world_count];
            for (int i = start_index; i < sInstance.Length; i++)
            {
                sInstance[i] = new World();
                sInstance[i].mWorldId = (byte)i;
                sInstance[i].Reset(true, networkManager);
            }
        }

        public static void AddSpawnPosition(List<JSpawnData> spawn, JMapObjectData mapObjectData)
        {
            if ((mapObjectData.mapTypes & (1 << (int)MapPropertyType.Spawn)) != 0)
            {
                mapObjectData.jSpawnData[0].mapPos = mapObjectData.mapPos;
                mapObjectData.jSpawnData[0].mapPos.y = mapObjectData.mapPos.y + 1f;
                spawn.Add(mapObjectData.jSpawnData[0]);
            }
        }





        /// <summary>
        /// 월드 초기화
        /// </summary>
        public static bool StaticInit(string mapFilename)
        {
            // 맵데이터에서 로드된 정보 캐싱
            // 전체 월드가 공유 (multi thread로 변경시 수정이 필요)

            try
            {
#if _USE_THREAD_STATIC
                if(mapGameObject == null)
                    mapGameObject = new Dictionary<int, JMapObjectData>();
#endif
                spawn_position.Clear();
                mapGameObject.Clear();
                var mMapData = JsonManager.LoadJsonArray<JMapObjectData>("JsonData", mapFilename);
                foreach (var block in mMapData)
                {
                    mapGameObject[block.uID] = block;
                    AddSpawnPosition(spawn_position, block);
                }

                spawn_position.SortSpawnPosition();

                LogHelper.LogInfo($"Load Map Object {mapGameObject.Count}");
                return true;
            }
            catch(Exception ex)
            {
                LogHelper.LogError(ex.ToString());
            }
            return false;

#if _USE_BEPU_PHYSICS
#else
            //InitStaticObject();
#endif
        }


#if _USE_BEPU_PHYSICS
        public BEPUphysics.ISpaceObject CreateCollision(JMapObjectData block)
        {
            BEPUphysics.ISpaceObject collision = null;
            if (block.colliderTypes == 0)
            {
                BEPUphysics.EntityStateManagement.MotionState ms = new BEPUphysics.EntityStateManagement.MotionState();
                ms.Position = new BEPUutilities.Vector3(block.mapPos.x, block.mapPos.y, block.mapPos.z);
                ms.Orientation = new BEPUutilities.Quaternion(block.mapRot.x, block.mapRot.y, block.mapRot.z, block.mapRot.w);

                collision = new Box(ms, block.mapScale.x, 1f, block.mapScale.y);

            }
            else
            {
                var vertices = new BEPUutilities.Vector3[block.meshColliderVertices.Length];
                int[] indices = new int[block.meshColliderIndices.Length];
                for (int i = 0; i < vertices.Length; ++i)
                    vertices[i] = new BEPUutilities.Vector3(block.meshColliderVertices[i].x, block.meshColliderVertices[i].y, block.meshColliderVertices[i].z);
                for (int i = 0; i < indices.Length; ++i)
                    indices[i] = block.meshColliderIndices[i];

                var Position = new BEPUutilities.Vector3(block.mapPos.x, block.mapPos.y, block.mapPos.z);
                var Orientation = new BEPUutilities.Quaternion(block.mapRot.x, block.mapRot.y, block.mapRot.z, block.mapRot.w);

                var staticMesh = new BEPUphysics.BroadPhaseEntries.StaticMesh(vertices, indices, new BEPUutilities.AffineTransform(new BEPUutilities.Vector3(0.1f, 0.1f, 0.1f), Orientation, Position));
                //staticMesh.Sidedness = BEPUutilities.TriangleSidedness.DoubleSided;
                collision = staticMesh;

            }
            space.Add(collision);
            return collision;
        }

        public void InitStaticObject()
        {
            LogHelper.LogInfo("InitStaticObject");
            int count = 0;
            foreach (var block in mapGameObject.Values)
            {

                // 파괴 되지 않는 오브젝트만 관리
                if ((block.mapTypes & (1 << (int)MapPropertyType.MapDestroy)) == 0)
                {

                    if ( (block.mapTypes & (1 << (int)MapPropertyType.Ramp)) != 0 
                        || (block.mapTypes & (1 << (int)MapPropertyType.Jump)) != 0
                        || (block.mapTypes & (1 << (int)MapPropertyType.Tile)) != 0)
                    {
                    }
                    else
                    {
                        CreateCollision(block);
                        ++count;
                    }

                }
            }
            LogHelper.LogInfo($"InitStaticObject finish count{count}");
        }

#else

        public static void InitStaticObject()
        {
            LogHelper.LogInfo("InitStaticObject");
            int count = 0;
            bool IsCollision = false;
            mStaticGameObjects.Clear();
            foreach (var block in mapGameObject.Values)
            {

                // 파괴 되지 않는 오브젝트만 관리
                if ((block.mapTypes & (1 << (int)MapPropertyType.MapDestroy)) == 0)
                {

                    // 보물상자 제외
                    if ((block.mapTypes & (1 << (int)MapPropertyType.AutoCreateItem)) != 0)
                        continue;

                    // 바닥 아래는 제외
                    if (block.mapPos.y < 1f)
                        continue;

                    // 파괴 되지 않는 오브젝트만 관리
                    if ((block.mapTypes & (1 << (int)MapPropertyType.MapDestroy)) == 0)
                    {
                        if ((block.mapTypes & (1 << (int)MapPropertyType.Ramp)) != 0
                            || (block.mapTypes & (1 << (int)MapPropertyType.Jump)) != 0
                            || (block.mapTypes & (1 << (int)MapPropertyType.Tile)) != 0
                            || (block.mapTypes & (1 << (int)MapPropertyType.WarpZone)) != 0
                            || (block.mapTypes & (1 << (int)MapPropertyType.AutoCreateBomb)) != 0
                            || (block.mapTypes & (1 << (int)MapPropertyType.IceFloor)) != 0
                            || (block.mapTypes & (1 << (int)MapPropertyType.SlowFloor)) != 0
                            )
                        {
                            IsCollision = false;

                        }
                        else
                        {
                            IsCollision = true;
                        }

                        for (int x = 0; x < block.mapScale.x; ++x)
                        {
                            for (int y = 0; y < block.mapScale.y; ++y)
                            {
                                Vector3 pos = block.mapPos;
                                if (block.mapRotY == 90f)
                                    pos.z -= x;
                                else if (block.mapRotY == 270f || block.mapRotY == -90f)
                                    pos.z += x;
                                else if (block.mapRotY == 180f)
                                    pos.x -= x;
                                else if (block.mapRotY == 0.0f)
                                    pos.x += x;
                                else
                                {
                                    throw new System.Exception("error angle value");
                                }

                                if (block.mapRotY == 90f)
                                    pos.x -= y;
                                else if (block.mapRotY == 270f || block.mapRotY == -90f)
                                    pos.x += y;
                                else if (block.mapRotY == 180f)
                                    pos.z += y;
                                else if (block.mapRotY == 0.0f)
                                    pos.z -= y;
                                else
                                {
                                    throw new System.Exception("error angle value");
                                }

                                var game_object = new StaticGameObject();
                                game_object.Uid = block.uID;
                                game_object.SetLocation(pos);
                                //game_object.mDirection = block.mapRot;

                                if ((block.mapTypes & (1 << (int)MapPropertyType.Jump)) != 0
                                    || (block.mapTypes & (1 << (int)MapPropertyType.WarpZone)) != 0
                                    )
                                {
                                    game_object.Scale.x = 0.85f;
                                    game_object.Scale.y = 0.85f;
                                    game_object.widthHalf = 0.425f;
                                    game_object.heightHalf = 0.425f;
                                }
                                else
                                {
                                    game_object.Scale.x = 1f;
                                    game_object.Scale.y = 1f;
                                }
                                game_object.MapData = block;
                                game_object.IsCollision = IsCollision;

                                mStaticGameObjects.Add(game_object);

                                ++count;

                                if((block.mapTypes & (1 << (int)MapPropertyType.Castle)) != 0)
                                {
                                    //LogHelper.LogInfo($"castle {pos}");
                                }

                                if ((block.mapTypes & (1 << (int)MapPropertyType.IceFloor)) != 0)
                                {
                                    LogHelper.LogInfo($"Ice Floor {block.tileSpeed}");
                                }
                            }
                        }
                    }
                }
            }
            LogHelper.LogInfo($"InitStaticObject finish count{count}");
        }
#endif


        /// <summary>
        /// 프랍 생성
        /// </summary>
        public Dictionary<int, int> InitProb(byte worldId, bool isServer, bool isOnlyCounting)
        {
            core.LogHelper.LogInfo($"InitProb {worldId}, {isServer}, {isOnlyCounting}");

            /////////////////////////
            /// test prob
            //for (int i = 0; i < 20; ++i)
            //{
            //    var go = GameObjectRegistry.sInstance.CreateGameObject((int)GameObjectClassId.kProp, isServer, worldId);
            //    Vector3 mouseLocation = core.Utility.GetRandomVector(-10, 10, 0.0f);
            //    go.SetLocation(mouseLocation);
            //}

            //space.Add(new Box(new BEPUutilities.Vector3(0, 1.0f, 0), 30, 1, 30));

            // 기지 목록 초기화
            castleList.Clear();

            Dictionary<int, int> counts = null;
            if(isOnlyCounting)
                counts = new Dictionary<int, int>();

            foreach (var block in mapGameObject.Values)
            {
                // 파괴되는 오브젝트만 관리
                if ((block.mapTypes & (1 << (int)MapPropertyType.MapDestroy)) != 0)
                {
                    if (isOnlyCounting)
                    {
                        counts.Increment((int)GameObjectClassId.Prop);
                    }
                    else
                    {
                        Prop go = (Prop)GameObjectRegistry.sInstance.CreateGameObject((int)GameObjectClassId.Prop, isServer, worldId, false);
                        go.Set(block);
                    }
#if _USE_BEPU_PHYSICS
                    go.collision = CreateCollision(block);
#endif

                    // 프랍의 HP
                    if ((block.mapTypes & (1 << (int)MapPropertyType.Health)) != 0)
                    {
                        if (isOnlyCounting)
                        {
                            counts.Increment((int)GameObjectClassId.PropHealth);
                        }
                        else
                        {
                            var prop = (PropHealth)GameObjectRegistry.sInstance.CreateGameObject((int)GameObjectClassId.PropHealth, isServer, worldId, false);
                            prop.InitFrom(block);
                        }
                    }
                }

                // 파괴 되지 않는 오브젝트만 관리
                if ((block.mapTypes & (1 << (int)MapPropertyType.MapDestroy)) == 0)
                {
                    // 기지는 static, prop 둘다 생성 충돌은 static, 이벤트및 상태관리는 prop
                    if ((block.mapTypes & (1 << (int)MapPropertyType.Castle)) != 0)
                    {
                        if (isOnlyCounting)
                        {
                            counts.Increment((int)GameObjectClassId.Castle);
                        }
                        else
                        {
                            var go = (Castle)GameObjectRegistry.sInstance.CreateGameObject((int)GameObjectClassId.Castle, isServer, worldId, false);
                            go.Set(block);

                            // 팀구분이 필요
                            castleList.Add(go.Team, go);
                        }
                    }

                    // 보물상자는 prop만 생성
                    if ((block.mapTypes & (1 << (int)MapPropertyType.AutoCreateItem)) != 0)
                    {
                        if (isOnlyCounting)
                        {
                            counts.Increment((int)GameObjectClassId.TreasureBox);
                        }
                        else
                        {
                            var go = (TreasureBox)GameObjectRegistry.sInstance.CreateGameObject((int)GameObjectClassId.TreasureBox, isServer, worldId, false);
                            go.Set(block);
                        }
                    }

                    if ((block.mapTypes & (1 << (int)MapPropertyType.AutoCreateBomb)) != 0)
                    {
                        if (isOnlyCounting)
                        {
                            counts.Increment((int)GameObjectClassId.Trap);
                        }
                        else
                        {
                            var go = (Trap)GameObjectRegistry.sInstance.CreateGameObject((int)GameObjectClassId.Trap, isServer, worldId, false);
                            go.Set(block);
                        }
                    }

                    if ((block.mapTypes & (1 << (int)MapPropertyType.MovePathStart)) != 0)
                    {
                        if (isOnlyCounting)
                        {
                            counts.Increment((int)GameObjectClassId.Train);
                        }
                        else
                        {
                            // 기차 생성
                            var train = (Train)GameObjectRegistry.sInstance.CreateGameObject((UInt32)GameObjectClassId.Train, isServer, (byte)worldId, false);
                            train.Set(block);
                        }
                    }

                    if ((block.mapTypes & (1 << (int)MapPropertyType.MoveObject)) != 0)
                    {

                    }

                    if ((block.mapTypes & (1 << (int)MapPropertyType.Health)) != 0)
                    {
                        if (isOnlyCounting)
                        {
                            counts.Increment((int)GameObjectClassId.PropHealth);
                        }
                        else
                        {
                            var prop = (PropHealth)GameObjectRegistry.sInstance.CreateGameObject((int)GameObjectClassId.PropHealth, isServer, worldId, false);
                            prop.InitFrom(block);
                        }
                    }

                    if ((block.mapTypes & (1 << (int)MapPropertyType.MapSwitch)) != 0)
                    {
                        if (isOnlyCounting)
                        {
                            counts.Increment((int)GameObjectClassId.Button);
                        }
                        else
                        {
                            var prop = (Button)GameObjectRegistry.sInstance.CreateGameObject((int)GameObjectClassId.Button, isServer, worldId, false);
                            prop.InitFrom(block);
                        }
                    }
                }
            }

            //LogHelper.LogInfo($"InitProb finish count{count}");
            return counts;
        }


        public World()
        {
        }

        public void InitializeWorld()
        {
            mGameObjects = new List<NetGameObject>();
            //mWorldMap = new WorldMap();

#if _USE_BEPU_PHYSICS
            space = new BEPUphysics.Space();
            space.ForceUpdater.Gravity = new BEPUutilities.Vector3(0, -9.81f, 0);

#elif _USE_BULLET_SHARP
            Broadphase = new BulletSharp.AxisSweep3(new BulletSharp.Math.Vector3(-1000, -1000, -1000), new BulletSharp.Math.Vector3(1000, 1000, 1000));

            var conf = new BulletSharp.DefaultCollisionConfiguration();
            var dispatcher = new BulletSharp.CollisionDispatcher(conf);
            world = new BulletSharp.MultiBodyDynamicsWorld(dispatcher, Broadphase, new BulletSharp.MultiBodyConstraintSolver(), conf);
            world.Gravity = new BulletSharp.Math.Vector3(0, -9.81f, 0);

            var box = new BulletSharp.BoxShape(50f, 1f, 50f);
            //box.Margin = 0f;
            floor = new BulletSharp.RigidBody(new BulletSharp.RigidBodyConstructionInfo(0f, new BulletSharp.DefaultMotionState(), box, new BulletSharp.Math.Vector3(0, -2f, 0)));
            floor.CollisionFlags = BulletSharp.CollisionFlags.KinematicObject;
            

            BulletSharp.Math.Matrix worldTrans = new BulletSharp.Math.Matrix();
            worldTrans.M41 = 0f;
            worldTrans.M42 = -2f;
            worldTrans.M43 = 0;
            floor.WorldTransform = worldTrans;
            //floor.Friction = 0.5f;


            world.AddRigidBody(floor, BulletSharp.CollisionFilterGroups.DefaultFilter, //BulletSharp.CollisionFilterGroups.Everything);
            (BulletSharp.CollisionFilterGroups.DefaultFilter
            | BulletSharp.CollisionFilterGroups.StaticFilter
            | BulletSharp.CollisionFilterGroups.KinematicFilter
            | BulletSharp.CollisionFilterGroups.DebrisFilter
            | BulletSharp.CollisionFilterGroups.SensorTrigger
            | BulletSharp.CollisionFilterGroups.CharacterFilter
            )
                );
#elif _USE_BEPU_PHYSICS_V2

            characters = new BepuPhysics.CharacterControllers(BufferPool);
            Simulation = Simulation.Create(BufferPool, new BepuPhysics.CharacterNarrowphaseCallbacks(characters), new DemoPoseIntegratorCallbacks(new System.Numerics.Vector3(0, -10, 0)));

            Simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(0, 0, 0), new CollidableDescription(Simulation.Shapes.Add(new Box(50, 1, 50)), 0.1f)));

#endif

        }



        public void AddGameObject(NetGameObject inGameObject)
        {
            mGameObjects.Add(inGameObject);
            inGameObject.SetIndexInWorld(mGameObjects.Count - 1);

            //mWorldMap.InsertObject(inGameObject);
        }
        public void RemoveGameObject(NetGameObject inGameObject)
        {
            int index = inGameObject.GetIndexInWorld();

            int lastIndex = mGameObjects.Count - 1;
            if (index != lastIndex)
            {
                mGameObjects[index] = mGameObjects[lastIndex];
                mGameObjects[index].SetIndexInWorld(index);
            }

            inGameObject.SetIndexInWorld(-1);

            mGameObjects.RemoveAt(lastIndex);

            inGameObject.CompleteRemove();

            //mWorldMap.RemoveObject(inGameObject);

            if (inGameObject.GetMapId() != 0)
                RemoveNetGameObject((GameObjectClassId)inGameObject.GetClassId(), inGameObject.GetMapId());
        }

        public List<NetGameObject> GetGameObjects(GameObjectClassId id)
        {
            return mGameObjects.Where(x => x.GetClassId() == (byte)id).ToList();
        }

        public NetGameObject GetGameObject(int index)
        {
            return mGameObjects[index];
        }
        public int GetGameObjectCount()
        {
            return mGameObjects.Count;
        }


        public static void Update()
        {
            for (byte worldId = 0; worldId < sInstance.Length; ++worldId)
            {
                World world = sInstance[worldId];

                world.mTickCounter.Update();

                //update all game objects- sometimes they want to die, so we need to tread carefully...

                for (int i = 0, c = world.mGameObjects.Count; i < c; ++i)
                {
                    NetGameObject go = world.mGameObjects[i];
                    if (!go.DoesWantToDie())
                    {
                        go.Update();
                    }
                    //you might suddenly want to die after your update, so check again
                    if (go.DoesWantToDie())
                    {
                        world.RemoveGameObject(go);
                        go.HandleDying();
                        --i;
                        --c;
                    }
                }
            }
        }

        public static void LateUpdate()
        {
#if _USE_BEPU_PHYSICS
            for (byte worldId = 0; worldId < sInstance.Length; ++worldId)
            {
                World world = sInstance[worldId];

                world.space.Update();


                for (int i = 0, c = world.mGameObjects.Count; i < c; ++i)
                {
                    NetGameObject go = world.mGameObjects[i];
                    if (!go.DoesWantToDie())
                    {
                        go.LateUpdate();
                    }
                }
            }
#elif _USE_BULLET_SHARP
            for (byte worldId = 0; worldId < sInstance.Length; ++worldId)
            {
                World world = sInstance[worldId];

                world.world.StepSimulation(Timing.sInstance.GetDeltaTime());

                //BulletSharp.Math.Matrix trans;
                //world.floor.GetWorldTransform(out trans);
                //LogHelper.LogInfo($"floor {trans.ToString()}, {world.floor.CollisionShape}");


                for (int i = 0, c = world.mGameObjects.Count; i < c; ++i)
                {
                    NetGameObject go = world.mGameObjects[i];
                    if (!go.DoesWantToDie())
                    {
                        go.LateUpdate();
                    }
                }
            }
#elif _USE_BEPU_PHYSICS_V2
            for (byte worldId = 0; worldId < sInstance.Length; ++worldId)
            {
                World world = sInstance[worldId];

                world.Simulation.Timestep(Timing.sInstance.GetDeltaTime());

                for (int i = 0, c = world.mGameObjects.Count; i < c; ++i)
                {
                    NetGameObject go = world.mGameObjects[i];
                    if (!go.DoesWantToDie())
                    {
                        go.LateUpdate();
                    }
                }
            }
#endif
        }

        public List<NetGameObject> GetGameObjects() { return mGameObjects; }


        public void Reset(bool isServer, NetworkManager networkManager)
        {
            InitializeWorld();
#if _USE_BEPU_PHYSICS
            InitStaticObject();
#endif
            // 월드에 생성되는 프랍은 서버에서만 생성하고, 이후 동기화로 클라에 생성

            castleList.Clear();
            mapNetGameObject.Clear();

            InitProb(mWorldId, isServer, false);


            // 월드에 게임 모드를 생성한다.
            GameMode = (GameMode)GameObjectRegistry.sInstance.CreateGameObject((UInt32)GameObjectClassId.GameMode, isServer, (byte)mWorldId);
            GameMode.mNetworkManager = networkManager;

            aiList.Clear();
            playerList.Clear();
        }

        public void Clear()
        {
#if _USE_BEPU_PHYSICS
#elif _USE_BULLET_SHARP
            if (world !=null)
            {
                LogHelper.LogInfo("world clear");
                world.RemoveCollisionObject(floor);
                world.ClearForces();
            }
#elif _USE_BEPU_PHYSICS_V2

#endif
        }

        public void ClearByClient()
        {
            playerList.Clear();
            aiList.Clear();
            RemovePlayerCount = 0;
            TimeoutPlayerCount = 0;
            ReadyPlay = false;
            GameMode = new GameMode();
            castleList.Clear();
            mGameObjects.Clear();
            mapNetGameObject.Clear();
            InputManager.Instance.Clear();
        }

        public void AddPlayer(int playerId, Actor actor)
        {
            playerList[playerId] = actor;
        }

        public void RemovePlayer(int playerId)
        {
            if (playerList.ContainsKey(playerId) == true)
            {
                playerList.Remove(playerId);
                ++RemovePlayerCount;
            }
        }

        public Actor GetPlayer(int playerId)
        {
            Actor actor = null;
            playerList.TryGetValue(playerId, out actor);
            return actor;
        }

        public void SetStartPlayTimeout()
        {
            TimeoutStartPlay = Timing.sInstance.GetFrameStartTime() + 15f;
        }

        /// <summary>
        /// 게임이 시작할 준비가 되었는지 확인
        /// 월드 오브젝트 동기화 완료 및 모든 플레이어가 참여 완료
        /// </summary>
        /// <returns></returns>
        public bool CheckStartPlay(int player_count)
        {
            if (ReadyPlay == false)
                return false;
#if false // UNITY_EDITOR || UNITY_STANDALONE_WIN
            Actor actor = null;
            playerList.TryGetValue(NetworkManagerClient.sInstance.GetPlayerId(), out actor);
            if (actor == null)
                return false;
#else
            if(player_count > playerList.Count + RemovePlayerCount + TimeoutPlayerCount)
            {
                if(Timing.sInstance.GetFrameStartTime()<TimeoutStartPlay)
                {
                    return false;
                }
                else
                {
                    LogHelper.LogInfo($"StartPlay timeout");
                }
            }
#endif


            return true;
        }


        public void RegisterNetGameObject(GameObjectClassId class_id, int map_uid, NetGameObject gameObject)
        {
            //core.LogHelper.LogInfo($"RegisterNetGameObject class_id:{class_id}, map_uid:{map_uid}");
            Dictionary<int, NetGameObject> map;
            if (mapNetGameObject.TryGetValue(class_id, out map))
            {
                map.Add(map_uid, gameObject);
            }
            else
            {
                map = new Dictionary<int, NetGameObject>();
                map.Add(map_uid, gameObject);
                mapNetGameObject.Add(class_id, map);
            }
        }

        private void RemoveNetGameObject(GameObjectClassId class_id, int map_uid)
        {
            Dictionary<int, NetGameObject> map;
            if (mapNetGameObject.TryGetValue(class_id, out map))
            {
                map.Remove(map_uid);
            }
        }

        public NetGameObject GetNetGameObject(GameObjectClassId class_id, int map_uid)
        {
            Dictionary<int, NetGameObject> map;
            NetGameObject gameObject;
            if (mapNetGameObject.TryGetValue(class_id, out map))
            {
                if (map.TryGetValue(map_uid, out gameObject))
                {
                    return gameObject;
                }
            }
            return null;
        }

        public Castle GetCastle(Team team)
        {
            if (castleList != null)
            {
                Castle castle = null;
                if (castleList.TryGetValue((int)team, out castle))
                {
                    return castle;
                }
            }
            return null;
        }

        //public List<Actor> GetMyTeam(Team team)
        //{
        //    var player_list = playerList.Values.Where(x => x.Team == team).ToList();
        //    var ai_list = aiList.Values.Where(x => x.Team == team).ToList();
        //    player_list.AddRange(ai_list);

        //    return player_list;
        //}

        public Actor GetKing(Team team)
        {

            return null;
        }
         
        public int GetKillKingCount(Team team)
        {
            return 0;
        }
    }
}
