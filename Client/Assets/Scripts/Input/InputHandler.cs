using UnityEngine;
using System.Collections.Generic;
using syncnet;

/// <summary>
/// 입력 이벤트 처리 클래스
/// </summary>
public class InputHandler : MonoBehaviour
{
    [SerializeField] private SessionChannelSO _addAgentChannel;
    [SerializeField] private SessionChannelSO _removeAgentChannel;
    [SerializeField] private SessionChannelSO _setMoveTargetChannel;
    [SerializeField] private SessionChannelSO _setRaycastChannel;
    [SerializeField] private SessionChannelSO _setMoveCharacterChannel;
    [SerializeField] private SessionChannelSO _setLoginChannel;
    [SerializeField] private SessionChannelSO _setUseSkillChannel;

    private Dictionary<string, System.Action<MouseInputEvent>> actionHandlers;

    private void Start()
    {
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
            { "spawn_monster", evt => _addAgentChannel.RaiseEvent(0, evt.HitPoint, (int)GameObjectType.Monster) },
            { "remove_monster", evt => 
                {
                    if (evt.HitInfo.HasValue && evt.HitInfo.Value.transform != null)
                    {
                        var monster = evt.HitInfo.Value.transform.GetComponent<Monster>();
                        if (monster != null)
                        {
                            _removeAgentChannel.RaiseEvent(monster.agnet_id, Vector3.zero, 0);
                        }
                    }
                }
            },
            { "use_skill", evt => _setUseSkillChannel.RaiseEvent(0, evt.HitPoint, 1) },
            { "move_target", evt => _setMoveTargetChannel.RaiseEvent(-1, evt.HitPoint, 0) },
            { "spawn_character", evt => _addAgentChannel.RaiseEvent(0, evt.HitPoint, (int)GameObjectType.Character) },
            { "set_raycast", evt => _setRaycastChannel.RaiseEvent(0, evt.HitPoint, 0) },
            { "move_character", evt => _setMoveCharacterChannel.RaiseEvent(0, evt.HitPoint, 0) }
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
            _setLoginChannel.RaiseEvent(0, evt.HitPoint, 0);
        }
    }
} 