
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class CActor : core.Actor
{
	public string model_name = "mickey2";
	public static new core.NetGameObject StaticCreate( byte worldId ) { return new CActor( worldId ); }

	public PlayerSkillSubState m_ePlaySubState = PlayerSkillSubState.Idle;
	public float m_DashSpeed = 0f;  // 대쉬 이동시 스피드 추후 스킬비헤이비어쪽으로 이동.

	public Vector3 position;
	public Vector3 velocity;

	public Vector3 mMoveDirection;

	private INetCharacter m_kBattleCharacter;
	//private ActorSkillManager m_kActorSkillManager;

	/// <summary>
	/// 캐릭터를 Behaviour 컴퍼넌트
	/// </summary>
	public float mTimeLocationBecameOutOfSync;
	float mTimeVelocityBecameOutOfSync;

	/// <summary>
	/// 기본 공격 버튼 interval
	/// </summary>
	public float _fBaseAttack_interval = 0.2f;

	private bool _bIsAIPlayer = false;

	public List<ushort> LastHiddenMapObjects = new List<ushort>();

	public bool IsLocalPlayer()
	{
		return GetPlayerId() == NetworkManagerClient.sInstance.GetPlayerId();
	}

	public bool IsAIPlayer()
	{
		return _bIsAIPlayer;
	}

	public override void Update()
	{
		//m_kBattleCharacter.UpdateState();
	}

	/// <summary>
	/// AI가 안들어가는 이슈로 임시로 적용 AI 적용시 없어져야할 코드
	/// </summary>
	public override void HandleDying()
	{
		NetworkManagerClient.sInstance.respawn = true;
		base.HandleDying();
		//m_kBattleCharacter.DisConnect();
		core.World.Instance().RemovePlayer( GetPlayerId() );
	}

	public override void OnAfterDeserialize( UInt32 readState )
	{
		// 캐릭터 생성을 가장 먼저 한다. 
		if( IsCreate == true )
		{
			OnCreate();
		}

		if( ( readState & (UInt32)ReplicationState.State ) != 0 )
		{
			if(StateServerSide == core.ActorState.Idle && LastStateServerSide == core.ActorState.Ghost )
			{
                //m_kBattleCharacter.OnRespawn( TargetPos );
				// 리스폰시 보간 및 예측을 하지 않는다.
				IsInterpolate = false;
			}

			if( StateServerSide == core.ActorState.Teleport )
			{
				IsInterpolate = false;
			}

			if( StateServerSide == core.ActorState.Dash )
			{
				Debug.Log( "dash begin" );
				m_ePlaySubState = PlayerSkillSubState.Dash;
				IsInterpolate = false;
			}
			else if( LastStateServerSide == core.ActorState.Dash && StateServerSide == core.ActorState.Dash )
			{
				Debug.Log( "dashing" );
				IsInterpolate = false;
			}
			else if( LastStateServerSide == core.ActorState.Dash && StateServerSide == core.ActorState.Idle )
			{
				Debug.Log( "dash end" );
				m_ePlaySubState = PlayerSkillSubState.Idle;
				IsInterpolate = false;
			}

			//m_kBattleCharacter.ChangeState( StateServerSide );
		}

		if( ( readState & (UInt32)ReplicationState.Health ) != 0 )
		{
			//if( m_kBattleCharacter.m_kMonoCharacter)
			//{
			//	var changeHealth = mHealth - mLastHealth;


			//	if (m_kBattleCharacter.m_ePlayState == PlayerState.Die)
   //                 m_kBattleCharacter.m_kUIPlayHeroHUD.SetShow(false);
   //             m_kBattleCharacter.m_kUIPlayHeroHUD.SetHp(mHealth);
			//	// HP 증가로 인한 아이템 효과 처리
			//	if( IsHealthUp && ( readState & (UInt32)ReplicationState.PlayerId ) == 0 && StateServerSide != core.ActorState.Ghost )
			//	{

			//	}
			//	//HP 감소로 인한 효과 처리
			//	else if( IsHealthUp == false && ( readState & (UInt32)ReplicationState.PlayerId ) == 0 )
			//	{
   //                 //캐릭터 데미지 처리 해줘야함
   //                 m_kBattleCharacter.SetDamage();
			//	}
			//}
		}

		if( ( readState & (UInt32)ReplicationState.AI ) != 0 )
		{
            OnChangedAI();
        }

		if ((readState & (UInt32)ReplicationState.Hide) != 0)
		{
			OnHide();
		}

#if _USE_INPUT_SYNC
#else
		if ( IsLocalPlayer() == false )
		{
			if( GetVelocity().x != 0 || GetVelocity().z != 0 )
				is_move = true;
			else
				is_move = false;
		}
#endif

		if( IsInterpolate )
		{
#if _USE_INPUT_SYNC
            PredictionInputSync(readState);
#else
			PredictionStateSync( readState );
#endif
		}

#if _USE_BEPU_PHYSICS
        if(mCharacterController !=null)
            mCharacterController.TeleportToPosition(GetLocation().CopyTo(ref physicsLocation), 0f);
#endif

#if !_USE_BEPU_PHYSICS && !_USE_BULLET_SHARP // CHANGE_Y_AXIS_CLIENT
		if( IsLocalPlayer() == false && oldLocation.y != GetLocation().y )
		{
			SetFloor( GetLocation().y );
		}
#endif
	}

	protected CActor( byte worldId ) : base( worldId )
	{
		mTimeVelocityBecameOutOfSync = 0.0f;
	}

	public void OnCreate()
	{
		Debug.Log($"CActor.OnCreate() {GetPlayerId()}");
		m_kBattleCharacter = (INetCharacter)FuncCreateGameObject?.Invoke();

		bool is_local_character = false;
		if(core.Engine.sInstance.ServerClientId == GetPlayerId())
        {
			NetworkManagerClient.sInstance.LocalCharacter = m_kBattleCharacter;
			is_local_character = true;
		}


		//m_kActorSkillManager	= m_kBattleCharacter.m_kActorSkillManager;

		//m_kBattleCharacter.SetActor( this );

		NetworkManagerClient.sInstance.ExecuteLinkedObjectEvent( GetNetworkId() );

		core.World.Instance().AddPlayer( GetPlayerId(), this );

		//m_kBattleCharacter.Position		= GetLocation();

		//      m_kBattleCharacter.m_kUIPlayHeroHUD.maxHp = mHealth;
		//      m_kBattleCharacter.m_kUIPlayHeroHUD.SetHp(mHealth);

		m_kBattleCharacter.OnCreate(this, is_local_character);
	}

	#region Skill Action
	// 밀기 폭탄 발사 액션
	public void ShootActionToServer( byte actionToken, bool IsBasicAttack, Vector3 targetPos )
	{
        Debug.Log($"ShootActionToServer {actionToken}, {IsBasicAttack}, {targetPos}, networkID {NetworkId}");

        InvokeServerRpc( ShootPushServer, actionToken, IsBasicAttack, targetPos );
	}

	// 제자리 발사 액션 (전기 스킬, 드라큘라 근접)
	public void ShootActionToServer( byte actionToken, bool IsBasicAttack, Vector3 origin, Vector3 dir )
	{
		Debug.Log( $"ShootActionToServer {actionToken}, {IsBasicAttack}, {origin}, {dir}, networkID {NetworkId}" );

		InvokeServerRpc( ShootSkillServer, actionToken, IsBasicAttack, origin, dir ); // origin은 리모트에서 사용하지 않음. 필요없는 파라미터
	}

	// 폭탄 폭발 액션 (던지기, 밀기) 
	public void OnCompletSkillAction( byte actionToken, List<int> detectedObjectList, Vector3 reachedPosition )
	{
		Debug.Log( $"InvokeServerRpc {reachedPosition.y} HitPos {reachedPosition.y}" + "\n hitNetwork Id: " + String.Join( ", ", detectedObjectList) );

        InvokeServerRpc( ActionResult, actionToken, detectedObjectList, reachedPosition );
	}

	// 발사체 hit 액션 (전기 스킬, 드라큘라 근접 기본공격, 제천대성 기본공격&스킬)
	public void OnHitSkillAction( byte actionToken, int hitProjectileIdx, List<int> hitNetworkIds )
	{
		Debug.Log( $"OnHitSkillAction - hitProjectileIdx : {hitProjectileIdx}, \n hitNetwork Id : " + String.Join( ", ", hitNetworkIds ) );

		InvokeServerRpc( ActionResult2, actionToken, hitNetworkIds, hitProjectileIdx );
	}
	#endregion Skill Action

	public override void CompleteRemove()
	{
		base.CompleteRemove();
		Debug.Log( "CompleteRemove" );
	}

	public void OnChangedBuff( core.BuffType buffType, core.Buff changedBuff )
	{
        //m_kBattleCharacter.OnChangedBuff(buffType, changedBuff);
	}

    /// <summary>
    /// ai 컨트롤이 필요한 player id 리스트 갱신되었음
    /// </summary>
    public void OnChangedAI()
    {
        if (IsLocalPlayer())
        {
            core.LogHelper.LogInfo($"OnChangedAI player ({string.Join(",", AIPlayers)})");

            NetworkManagerClient.sInstance.AIStates.Clear();
            for (int i = 0; i < AIPlayers.Count; ++i)
            {
                var aiActor = (CActor)NetworkManagerClient.sInstance.GetGameObject(AIPlayers[i], core.World.DefaultWorldIndex);
                if (aiActor != null)
                {
                    aiActor.SetAI();
                }
                else
                {
                    var networkId = AIPlayers[i];
                    core.LogHelper.LogInfo($"OnChangedAI cannot find player ({networkId})");
                    NetworkManagerClient.sInstance.RegisterLinkedObjectEvent(networkId, () =>
                    {
                        aiActor = (CActor)NetworkManagerClient.sInstance.GetGameObject(networkId, core.World.DefaultWorldIndex);
                        if (aiActor != null)
                        {
                            aiActor.SetAI();
                        }
                    });
                }
            }
        }
    }

	public void OnHide()
	{
		core.LogHelper.LogInfo($"OnHide {IsHide}, {string.Join(", ", HiddenMapObjects)}");

        //m_kBattleCharacter.OnRemoteHide(IsHide, HiddenMapObjects);

        LastHiddenMapObjects = HiddenMapObjects.ToList();
	}

	public void SetAI()
    {
        NetworkManagerClient.sInstance.AIStates.Add(GetPlayerId(),
            new core.AIState()
            {
                // 현재 로컬에 상태값을 기준으로 ai 상태를 초기화
                location = GetLocation(),
                velocity = GetVelocity(),
                playerId = GetPlayerId(),
            }
            );
        //ai 세팅.
        //AiManager.SetAiActor(GetPlayerId());
        _bIsAIPlayer = true;
    }

    public void Destory()
	{
	}

	public void OnApplicationQuit( bool pause )
	{
		InvokeServerRpc( ApplicationQuit, pause );
	}

	public void SendDebugCommand( string cmd, string param1, string param2, string param3, string param4 )
	{
		InvokeServerRpc( DebugCommand, cmd, param1, param2, param3, param4 );
	}

	public void SetHide(bool isHide, ushort map_object_uid)
	{
		core.LogHelper.LogInfo($"SetHide {isHide}, {map_object_uid}");

		InvokeServerRpc(Hide, isHide, map_object_uid);
	}
}
