using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 입력 관리자 클래스
/// </summary>
public class InputManager : MonoBehaviour
{
    private static InputManager instance;
    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("InputManager");
                instance = go.AddComponent<InputManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // 이벤트 델리게이트 정의
    public event Action<string, MouseInputEvent> OnInputTriggered;

    public Camera MainCamera { get; private set; }
    private RaycastHit currentHit;
    private bool isHit;

    [System.Serializable]
    public class InputAction
    {
        public string id;
        public string bindingString;
        public InputBinding binding;
    }

    [SerializeField] private InputAction[] inputActions;
    private Dictionary<string, InputBinding> inputBindings = new Dictionary<string, InputBinding>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        MainCamera = Camera.main;

        // 바인딩 초기화
        InitializeBindings();
    }

    private void InitializeBindings()
    {
        // 기본 바인딩 설정
        inputActions = new InputAction[]
        {
            new InputAction { id = "spawn_monster", bindingString = "Ctrl+Left on floor" },
            new InputAction { id = "remove_monster", bindingString = "Alt+Left on monster" },
            new InputAction { id = "use_skill", bindingString = "Shift+Left on floor/monster" },
            new InputAction { id = "move_target", bindingString = "Left on floor" },
            new InputAction { id = "spawn_character", bindingString = "Ctrl+Right on floor" },
            new InputAction { id = "set_raycast", bindingString = "Alt+Right on floor" },
            new InputAction { id = "move_character", bindingString = "Right on floor" }
        };

        // 바인딩 파싱 및 등록
        foreach (var action in inputActions)
        {
            action.binding = InputBinding.Parse(action.bindingString);
            inputBindings[action.id] = action.binding;
        }
    }

    private bool GetHitPoint(string tag)
    {
        // InputManager 는 DontDestroyOnLoad 싱글톤이라 씬 전환 시 캐시한 카메라가 파괴된다.
        // 캐시가 무효(null/파괴됨)이면 현재 씬의 메인 카메라를 다시 가져온다.
        if (MainCamera == null)
            MainCamera = Camera.main;
        if (MainCamera == null)
            return false; // 아직 메인 카메라가 없음(씬 로드 중 등)

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = MainCamera.farClipPlane;

        Vector3 dir = MainCamera.ScreenToWorldPoint(mousePos);

        isHit = Physics.Raycast(MainCamera.transform.position, dir, out currentHit, mousePos.z);
        if (isHit && !string.IsNullOrEmpty(tag))
        {
            return currentHit.transform.CompareTag(tag);
        }
        return isHit;
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // 현재 입력 상태 확인
        InputModifier currentModifier = InputModifier.None;
        if (Input.GetKey(KeyCode.LeftControl)) currentModifier |= InputModifier.Ctrl;
        if (Input.GetKey(KeyCode.LeftAlt)) currentModifier |= InputModifier.Alt;
        if (Input.GetKey(KeyCode.LeftShift)) currentModifier |= InputModifier.Shift;

        // 마우스 버튼 확인
        for (int i = 0; i < 3; i++)
        {
            if (Input.GetMouseButtonDown(i))
            {
                MouseButton button = (MouseButton)i;
                GetHitPoint(""); // 레이캐스트 수행

                // 모든 바인딩 검사
                foreach (var action in inputActions)
                {
                    if (action.binding.Matches(button, currentModifier, isHit ? currentHit.transform.tag : ""))
                    {
                        var inputEvent = new MouseInputEvent(
                            currentHit.point,
                            isHit,
                            isHit ? currentHit.transform.tag : "",
                            isHit ? currentHit : (RaycastHit?)null,
                            (int)button,
                            true,
                            (currentModifier & InputModifier.Ctrl) != 0,
                            (currentModifier & InputModifier.Alt) != 0,
                            (currentModifier & InputModifier.Shift) != 0
                        );

                        OnInputTriggered?.Invoke(action.id, inputEvent);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 키 바인딩 변경
    /// </summary>
    public void ChangeKeyBinding(string action, KeyCode newKey)
    {
        // Implementation needed
    }

    /// <summary>
    /// 현재 키 바인딩 가져오기
    /// </summary>
    public KeyCode GetKeyBinding(string action)
    {
        // Implementation needed
        return KeyCode.None;
    }
} 