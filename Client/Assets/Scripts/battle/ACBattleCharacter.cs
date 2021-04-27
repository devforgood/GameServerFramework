using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ACBattleCharacter : MonoBehaviour, INetCharacter
{
    public Vector3 m_vMoveDirection = Vector3.zero;
    CActor m_kCActor;
    bool IsLocalCharacter;

    public CharacterController m_kCharacterController;
    public float m_playerSpeed = 5f;

    // Start is called before the first frame update
    void Start()
    {
        m_kCharacterController = gameObject.AddComponent<CharacterController>();
        m_kCharacterController.slopeLimit = 50f;
        m_kCharacterController.skinWidth = 0.01f;
        m_kCharacterController.center = new Vector3(0f, 0.75f, 0f);
        m_kCharacterController.height = 1.5f;
        m_kCharacterController.radius = 0.45f;


    }

    // Update is called once per frame
    void Update()
    {
        MoveViewStateSync();
    }
    public virtual void MoveViewStateSync()
    {
        if (IsLocalCharacter)
        {
            if (InputManager.Instance.GetState().mIsMove)
            {
                m_kCActor.mDirection = core.MathHelpers.DegreeToVector3Cached((int)InputManager.Instance.GetState().mDirection);
                m_vMoveDirection = m_kCActor.mDirection * m_playerSpeed;
                m_kCActor.is_move = true;
            }
            else
            {
                m_vMoveDirection = Vector3.zero;
                m_kCActor.is_move = false;
            }

            if (m_vMoveDirection != Vector3.zero)
            {
                m_kCharacterController.Move(m_vMoveDirection * UnityEngine.Time.deltaTime);
            }
        }
        else
        {
            if (transform.position != m_kCActor.GetLocation())
            {
                transform.position = Vector3.Lerp(transform.position, m_kCActor.GetLocation(), UnityEngine.Time.deltaTime * m_playerSpeed);
            }
        }
    }


    public void OnCreate(CActor actor, bool is_local_character)
    {
        m_kCActor = actor;
        IsLocalCharacter = is_local_character;
        Debug.Log("Create Character");
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public Vector3 GetVelocity()
    {
        return m_vMoveDirection;
    }
}
