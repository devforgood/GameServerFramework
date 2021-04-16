using core;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

public class InputManager 
{

    /// <summary>
    /// Global instance of NetworkManagerClient
    /// </summary>
    //public static InputManager sInstance = new InputManager();

#region     INSTANCE
    private static InputManager sInstance;
    public  static InputManager Instance
    {
        get
        {
            if (sInstance == null)
            {
                sInstance = new InputManager();
                sInstance.Initialize();
            }

            return sInstance;
        }
    }
#endregion  INSTANCE

    static readonly float				s_fKTimeBetweenInputSamples     = 0.03f;
    private         core.InputState		m_kCurrentState                 = new core.InputState();
    private         float				m_fNextTimeToSampleInput;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

	Dictionary<KeyCode, bool> key_event = new Dictionary<KeyCode, bool>()
	{
		{ KeyCode.A , false },
		{ KeyCode.D , false },
		{ KeyCode.W , false },
		{ KeyCode.S , false },
		{ KeyCode.K , false },
		{ KeyCode.R , false },
		{ KeyCode.B , false },
	};

	bool mIsLeft = false;
    bool mIsRight = false;
    bool mIsForward = false;
    bool mIsBack = false;

#endif

    SkillType m_eBombSkillType;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID

	//public		UiJoyStick				m_kMoveJoyStick			= null;
	//public		ACUISkillJoyStick		m_kBombJoyStick			= null;
	//public		ACUISkillJoyStick		m_kSkillJoyStick		= null; // 스킬에 따라 조이스틱이냐 버튼이냐로 변경필요.

#endif

    public static	bool			IsNull					=> ( sInstance == null );

    public void SetBombSkillType(SkillType bombSkillType) { m_eBombSkillType = bombSkillType; }

    private void Initialize()
    {
        m_fNextTimeToSampleInput        = 0.0f;
    }

    private bool IsTimeToSampleInput()
    {
        float time = core.Timing.sInstance.GetFrameStartTime();
        if (time > m_fNextTimeToSampleInput)
        {
            m_fNextTimeToSampleInput = m_fNextTimeToSampleInput + s_fKTimeBetweenInputSamples;

            //Debug.Log($"time {time}");

            return true;
        }

        return false;
    }

    private void UpdateDesireVariableFromKey(core.InputAction inInputAction, out bool ioVariable )
    {
        ioVariable = false;
        if (inInputAction == core.InputAction.Pressed)
        {
            ioVariable = true;
        }
        else if (inInputAction == core.InputAction.Released)
        {
            ioVariable = false;
        }
    }

    private void UpdateDesireFloatFromKey(core.InputAction inInputAction, out float ioVariable )
    {
        ioVariable = 0.0f;
        if (inInputAction == core.InputAction.Pressed)
        {
            ioVariable = 1.0f;
        }
        else if (inInputAction == core.InputAction.Released)
        {
            ioVariable = 0.0f;
        }
    }

    public void HandleInputStick(core.InputAction inInputAction, eJoyStickType eJoyStick,float fAngle)
    {
        
        if(inInputAction == InputAction.Released)
        {
            HandleInputSticPointUp(eJoyStick);
            return;
        }

        switch (eJoyStick)
        {
            case eJoyStickType.eMOVESTICK:
                m_kCurrentState.mDirection = (uint)fAngle;
                m_kCurrentState.mIsMove = true;
                break;
            case eJoyStickType.eBOMBSTICK:
                break;
        }


    }
    public void HandleInputSticPointUp(eJoyStickType eJoyStick)
    {
        switch(eJoyStick)
        {
            case eJoyStickType.eMOVESTICK:
                m_kCurrentState.mIsMove = false;
                //m_kPendingMove = SampleInputAsMove();
                break;
            case eJoyStickType.eBOMBSTICK:
                break;
        }
    }
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

	public void KeyEvent()
	{
		KeyEvent( KeyCode.A );
		KeyEvent( KeyCode.D );
		KeyEvent( KeyCode.W );
		KeyEvent( KeyCode.S );
		KeyEvent( KeyCode.K );
		KeyEvent( KeyCode.R );
		KeyEvent( KeyCode.B );
	}

	void KeyEvent( KeyCode k )
	{
		bool last_key_state = key_event[ k ];
		if( Input.GetKey( k ) )
		{
			key_event[ k ] = true;
		}
		else if( key_event[ k ] == true )
		{
			key_event[ k ] = false;
		}

		if( last_key_state != key_event[ k ] )
		{
			if( key_event[ k ] )
			{
				Debug.Log( "key down " + k );
				InputManager.Instance.HandleInput( core.InputAction.Pressed, k );
			}
			else
			{
				Debug.Log( "key up " + k );
				InputManager.Instance.HandleInput( core.InputAction.Released, k );
			}
		}
	}

	public void HandleInput(core.InputAction inInputAction, KeyCode inKeyCode)
    {
        //ACBattleCharacter battleCharacter = m_kMoveJoyStick._battleCharacter;

        switch (inKeyCode)
        {
            case KeyCode.A:
                UpdateDesireVariableFromKey(inInputAction, out mIsLeft);
                break;
            case KeyCode.D:
                UpdateDesireVariableFromKey(inInputAction, out mIsRight);
                break;
            case KeyCode.W:
                UpdateDesireVariableFromKey(inInputAction, out mIsForward);
                break;
            case KeyCode.S:
                UpdateDesireVariableFromKey(inInputAction, out mIsBack);
                break;
            case KeyCode.K:
                //battleCharacter.m_kActorSkillManager.IsSkillState(SkillStateMachine.StateType.SkillAutoTargetState, eJoyStickType.eSKILLSTICK);
                //bool isShoot = battleCharacter.m_kActorSkillManager.SkillCasting(eJoyStickType.eSKILLSTICK, true);
                //if (isShoot)
                //    battleCharacter.m_kActorSkillManager.SetAttackButtonFlag(false);
                //UpdateDesireVariableFromKey(inInputAction, out m_kCurrentState.mIsShooting);
                break;
            case KeyCode.B:
                //battleCharacter.m_kActorSkillManager.IsSkillState(SkillStateMachine.StateType.SkillAutoTargetState, eJoyStickType.eBOMBSTICK);
                //isShoot = battleCharacter.m_kActorSkillManager.SkillCasting(eJoyStickType.eBOMBSTICK, true);
                //if (isShoot)
                //    battleCharacter.m_kActorSkillManager.SetAttackButtonFlag(false);
                //UpdateDesireVariableFromKey(inInputAction, out m_kCurrentState.mIsBomb);
                break;
        }

        if (mIsForward && mIsRight)
        {
            m_kCurrentState.mDirection = 45;
            m_kCurrentState.mIsMove = true;
        }
        else if (mIsForward && mIsLeft)
        {
            m_kCurrentState.mDirection = 135;
            m_kCurrentState.mIsMove = true;
        }
        else if (mIsBack && mIsRight)
        {
            m_kCurrentState.mDirection = 315;
            m_kCurrentState.mIsMove = true;
        }
        else if (mIsBack && mIsLeft)
        {
            m_kCurrentState.mDirection = 225;
            m_kCurrentState.mIsMove = true;
        }
        else if (mIsForward)
        {
            m_kCurrentState.mDirection = 90;
            m_kCurrentState.mIsMove = true;
        }
        else if (mIsBack)
        {
            m_kCurrentState.mDirection = 270;
            m_kCurrentState.mIsMove = true;
        }
        else if (mIsLeft)
        {
            m_kCurrentState.mDirection = 180;
            m_kCurrentState.mIsMove = true;
        }
        else if (mIsRight)
        {
            m_kCurrentState.mDirection = 0;
            m_kCurrentState.mIsMove = true;
        }
        else
        {
            m_kCurrentState.mIsMove = false;
            //m_kPendingMove = SampleInputAsMove();
        }

        //진영별 카메라 세팅 처리.
        if(m_kCurrentState.mIsMove == true)
        {
            m_kCurrentState.mDirection += (uint)Camera.main.transform.localEulerAngles.y;
            if (m_kCurrentState.mDirection >= 360)
                m_kCurrentState.mDirection -= 360;
        }

        //if (!battleCharacter.m_bisDied && !battleCharacter.IsNoMove)
        //{
        //    if (m_kCurrentState.mIsMove == false)
        //    {
        //        if (battleCharacter.IsPlayerStateInSkill())
        //            SmbManager.SetState(battleCharacter.m_kAnimator, PlayerState.Idle);  // m_ePlayState은 바꾸지 않고 하체 animation 만 바꿈  
        //        else
        //            battleCharacter.SetActionState(PlayerState.Idle);
        //    }
        //    else
        //    {
        //        if (battleCharacter.IsPlayerStateInSkill())
        //            SmbManager.SetState(battleCharacter.m_kAnimator, battleCharacter.IsNoMove ? PlayerState.Idle : PlayerState.Move);  // m_ePlayState은 바꾸지 않고 하체 animation 만 바꿈  
        //        else
        //            battleCharacter.SetActionState(PlayerState.Move);
        //    }
        //}
        //UiJoyStick.ResetGameOverCount();
    }

#endif

    public core.InputState GetState() { return m_kCurrentState; }

    public void Update()
    {

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID

        //if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        //{
        //    ACGameStateEntity a_kGameStateEntity = ACGameStateManager.Instance.GetState();
        //    Debug.Log($"[InputManager] : {m_kCurrentState} EntityState : {a_kGameStateEntity}");
        //    if (a_kGameStateEntity.State == EACGameState.LOBBY)
        //    {
        //        Debug.Log($"[InputManager] : {m_kCurrentState}");
        //        UIManager.Instance.BackUI();
        //    }
        //}

#endif

    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // DestoryJoystick()
    //--------------------------------------------------------------------------------------------------
    //	Desc.
    //
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public void DestoryJoystick()
	{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID

		//m_kBombJoyStick.Destroy();
		//m_kBombJoyStick		= null;

		//m_kSkillJoyStick.Destroy();
		//m_kSkillJoyStick	= null;

		//m_kMoveJoyStick		= null;
#endif
    }

    public void ClearPress()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
        //m_kBombJoyStick.OnPress(false);
        //m_kSkillJoyStick.OnPress(false);
#endif
    }

    public void Clear()
    {
        m_kCurrentState.Clear();
	}
}
