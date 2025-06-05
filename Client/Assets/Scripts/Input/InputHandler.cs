using UnityEngine;
using System.Collections.Generic;
using syncnet;

/// <summary>
/// 입력 이벤트 처리 클래스
/// </summary>
public class InputHandler : MonoBehaviour
{
    [SerializeField] private Session session;  // Session 참조

    private Dictionary<string, System.Action<MouseInputEvent>> actionHandlers;

    private void Start()
    {
        if (session == null)
        {
            session = FindObjectOfType<Session>();
            if (session == null)
            {
                Debug.LogError("Session component not found!");
                enabled = false;
                return;
            }
        }

        InitializeActionHandlers();
        InputManager.Instance.OnInputTriggered += HandleInput;
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInputTriggered -= HandleInput;
        }
    }

    private void InitializeActionHandlers()
    {
        actionHandlers = new Dictionary<string, System.Action<MouseInputEvent>>
        {
            // 몬스터 생성
            { "spawn_monster", evt => 
                session.AddAgent(0, evt.HitPoint, GameObjectType.Monster) 
            },

            // 몬스터 제거
            { "remove_monster", evt => 
                {
                    if (evt.HitInfo.HasValue && evt.HitInfo.Value.transform != null)
                    {
                        var monster = evt.HitInfo.Value.transform.GetComponent<Monster>();
                        if (monster != null)
                        {
                            session.RemoveAgent(monster.agnet_id);
                        }
                    }
                }
            },

            // 스킬 사용
            { "use_skill", evt => 
                session.UseSkill(1, evt.HitPoint, 1) 
            },

            // 이동 타겟 설정
            { "move_target", evt => 
                session.SetMoveTarget(session.player_agnet_id, evt.HitPoint) 
            },

            // 캐릭터 생성
            { "spawn_character", evt => 
                session.AddAgent(0, evt.HitPoint, GameObjectType.Character) 
            },

            // 레이캐스트 설정
            { "set_raycast", evt => 
                session.SetRaycast(evt.HitPoint) 
            },

            // 캐릭터 이동
            { "move_character", evt => 
                session.SetMoveTarget(session.player_agnet_id, evt.HitPoint) 
            }
        };
    }

    private void HandleInput(string actionId, MouseInputEvent evt)
    {
        if (actionHandlers.TryGetValue(actionId, out var handler))
        {
            handler(evt);
        }
    }

    private void HandleKeyboardInput(KeyboardInputEvent evt)
    {
        if (evt.Key == KeyCode.Space && evt.IsPressed)
        {
            session.Login();
        }
    }
} 