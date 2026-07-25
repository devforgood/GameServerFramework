using UnityEngine;
using System.Collections.Generic;
using syncnet;

/// <summary>
/// 입력 이벤트 처리 클래스
/// </summary>
public class InputHandler : MonoBehaviour
{
    [SerializeField] private Session session;  // Session 참조

    // 스킬 핫바: 숫자키(1~9)로 선택하고, Shift+좌클릭으로 선택된 스킬을 시전한다.
    // 씬에 직렬화된 값이 없으면 이 기본값이 쓰인다(패시브 200~ 은 시전 불가라 제외).
    // 기본값은 서로 다른 메커니즘/연출을 한 번씩 보여주는 조합이다:
    // 근접 → 돌진(dash) → 넉백 → 넉백 노바 → 번개 → 빛기둥 → 회오리 → 낙하 광역 → 회복.
    [SerializeField] private int[] skillBar = { 1, 119, 120, 123, 131, 122, 139, 102, 112 };
    private int selectedIndex = 0;

    /// <summary>현재 선택된(=Shift+좌클릭으로 시전될) 스킬 id.</summary>
    public int SelectedSkillId =>
        (skillBar != null && skillBar.Length > 0)
            ? skillBar[Mathf.Clamp(selectedIndex, 0, skillBar.Length - 1)]
            : 1;

    private Dictionary<string, System.Action<MouseInputEvent>> actionHandlers;

    // HUD 스타일 캐시
    private GUIStyle hudTitleStyle;
    private GUIStyle hudRowStyle;
    private Texture2D hudBoxTex;

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

    private void Update()
    {
        // 숫자키 1~9 로 핫바 스킬 선택.
        if (skillBar == null) return;
        int count = Mathf.Min(skillBar.Length, 9);
        for (int i = 0; i < count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                selectedIndex = i;
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

            // 스킬 사용 — 핫바에서 현재 선택된 스킬을 시전한다(과거에는 1 로 고정이었다).
            { "use_skill", evt =>
                session.UseSkill(SelectedSkillId, evt.HitPoint, 1)
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

    // ── 스킬 핫바 HUD (IMGUI — 씬/캔버스 세팅 없이 표시) ──
    // 좌상단에 선택된 스킬을 크게, 아래에 핫바([1]~[N])를 그리고 선택 항목을 강조한다.
    void OnGUI()
    {
        if (skillBar == null || skillBar.Length == 0)
            return;

        EnsureHudStyles();

        const float w = 250f;
        const float rowH = 20f;
        float x = 10f, y = 10f;
        float h = 30f + skillBar.Length * rowH + 26f;

        GUI.DrawTexture(new Rect(x, y, w, h), hudBoxTex, ScaleMode.StretchToFill);

        GUI.Label(new Rect(x + 10, y + 6, w - 20, 22),
            $"선택된 스킬: {PrettyName(SelectedSkillId)}", hudTitleStyle);

        float ly = y + 32f;
        for (int i = 0; i < skillBar.Length; i++)
        {
            bool selected = (i == selectedIndex);
            // 쿨다운 중인 스킬은 남은 시간을 함께 보여준다(시전해도 로컬에서 걸러지는 이유가 드러나게).
            float remaining = (session != null) ? session.SkillCooldowns.Remaining(skillBar[i]) : 0f;
            bool onCooldown = remaining > 0f;

            if (onCooldown)
                hudRowStyle.normal.textColor = selected ? new Color(0.8f, 0.6f, 0.3f) : new Color(0.55f, 0.55f, 0.55f);
            else
                hudRowStyle.normal.textColor = selected ? new Color(1f, 0.9f, 0.3f) : Color.white;

            string mark = selected ? "▶ " : "   ";
            string cooldownText = onCooldown ? $"  ({remaining:F1}s)" : "";
            GUI.Label(new Rect(x + 10, ly, w - 20, rowH),
                $"{mark}[{i + 1}] {PrettyName(skillBar[i])}{cooldownText}", hudRowStyle);
            ly += rowH;
        }

        hudRowStyle.normal.textColor = new Color(0.7f, 0.85f, 1f);
        GUI.Label(new Rect(x + 10, ly + 4, w - 20, rowH), "선택: 숫자키 1~N / 시전: Shift+좌클릭", hudRowStyle);
    }

    private void EnsureHudStyles()
    {
        if (hudBoxTex == null)
        {
            hudBoxTex = new Texture2D(1, 1);
            hudBoxTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            hudBoxTex.Apply();
        }
        if (hudTitleStyle == null)
            hudTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        if (hudRowStyle == null)
            hudRowStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
    }

    // 스킬 표시 이름: name_id("fireball_name"→"fireball"), 없으면 "skill <id>".
    private string PrettyName(int skillId)
    {
        if (GameManager.Instance != null && GameManager.Instance.resource != null
            && GameManager.Instance.resource.Skills.TryGetValue(skillId, out var d)
            && !string.IsNullOrEmpty(d.name_id))
        {
            return d.name_id.Replace("_name", "");
        }
        return $"skill {skillId}";
    }
}
