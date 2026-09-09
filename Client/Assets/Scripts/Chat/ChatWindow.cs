using System.Collections.Generic;
using UnityEngine;

// 채팅 창 — 입력 한 줄을 서버로 보내고, 서버가 돌려준 한 줄을 로그에 쌓아 보여준다.
//
// 지금 이 통로로 서버가 하는 일은 치트 명령 처리뿐이다(syncnet.fbs 의 Chat 주석 참고).
// 그래서 여기 있는 것은 입력창과 로그가 전부고, 채널/귓속말 같은 것은 없다.
//
// 씬에 캔버스를 두지 않고 IMGUI 로 그리는 이유는 스킬 핫바 HUD(InputHandler.OnGUI)와 같다 —
// 게임 필드 씬이 여럿이라, 씬마다 UI 를 배치하면 하나 빠뜨렸을 때 그 씬에서만 조용히 사라진다.
//
// 키 입력을 Update(Input.GetKeyDown)가 아니라 OnGUI 의 이벤트로만 읽는 이유가 있다.
// Update 에서 엔터로 창을 열면, 같은 프레임의 OnGUI 가 그 엔터를 한 번 더 본다 —
// 창이 열리자마자 빈 줄로 닫힌다. 열기/보내기/닫기를 한 이벤트 흐름에서 처리하고
// 처리한 이벤트는 Use() 로 소비하면 그 겹침이 생기지 않는다.
//
// 입력을 가로채는 동안(IsCapturing)에는 게임 입력이 쉬어야 한다. 숫자키로 스킬을 고르는
// InputHandler 가 "1" 을 타이핑으로 보지 않고 핫바 선택으로 먹어 버리기 때문이다.
public class ChatWindow
{
    private const int MaxLines = 8;        // 화면에 남겨 두는 줄 수
    private const string InputControlName = "chat_input";

    private readonly System.Action<string> send;
    private readonly List<string> lines = new List<string>();

    private bool open;
    private string draft = "";
    private bool focusRequested;

    /// <summary>입력창이 열려 있어 키보드를 가로채는 중인가. 게임 입력은 이때 쉰다.</summary>
    public bool IsCapturing { get { return open; } }

    public ChatWindow(System.Action<string> send)
    {
        this.send = send;
    }

    /// <summary>서버가 돌려준 한 줄(치트 결과/거부 사유)을 로그에 넣는다.</summary>
    public void AddServerLine(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // 서버가 여러 줄(치트 목록)을 한 메시지로 보내기도 한다.
        foreach (var line in text.Split('\n'))
            Append(line.TrimEnd('\r'));
    }

    private void Append(string line)
    {
        lines.Add(line);
        if (lines.Count > MaxLines)
            lines.RemoveRange(0, lines.Count - MaxLines);
    }

    /// <summary>Session.OnGUI 에서 부른다. 좌하단에 로그와 입력창을 그린다.</summary>
    public void Draw()
    {
        Event e = Event.current;

        // 엔터 한 번은 한 프레임에 이벤트 두 개로 온다 — keyCode=Return 하나,
        // keyCode=None 이고 character='\n' 하나. 둘 다 '엔터'로 받으면 첫 이벤트로 연 창이
        // 두 번째 이벤트에서 곧바로 빈 줄 보내기가 되어 같은 프레임에 닫힌다(=창이 안 뜬다).
        // 그래서 둘 다 소비하되, 실제 동작은 프레임당 한 번만 한다.
        bool enter = IsEnterEvent(e);
        if (enter)
        {
            e.Use();
            if (enterFrame == Time.frameCount)
                enter = false;      // 같은 엔터의 두 번째 이벤트 — 소비만 하고 넘어간다
            else
                enterFrame = Time.frameCount;
        }

        // ── 닫혀 있을 때: 엔터로 연다 ──
        if (!open)
        {
            if (enter)
            {
                open = true;
                focusRequested = true;
                draft = "";
            }

            DrawLog();   // 닫혀 있어도 최근 줄은 남긴다. 엔터와 함께 사라지면 못 읽는다.
            DrawHint();  // 채팅 창이 있다는 것 자체를 알려 준다(스킬 HUD 와 같은 톤).
            return;
        }

        // ── 열려 있을 때: 그리기 전에 키를 먼저 본다 ──
        // TextField 는 포커스를 가진 동안 키 이벤트를 소비하므로, 그린 뒤에 보면 엔터가 오지 않는다.
        bool cancel = false;
        if (!enter && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            cancel = true;
            e.Use();
        }

        DrawLog();
        DrawInput();

        if (enter)
            Submit();
        else if (cancel)
            Close();
    }

    // 이 프레임에서 엔터를 이미 처리했는지. 한 번의 키 입력이 이벤트 둘로 오기 때문에 필요하다.
    private int enterFrame = -1;

    private static bool IsEnterEvent(Event e)
    {
        if (e == null || e.type != EventType.KeyDown)
            return false;

        return e.keyCode == KeyCode.Return
            || e.keyCode == KeyCode.KeypadEnter
            || e.character == '\n'
            || e.character == '\r';
    }

    private const float Width = 520f;
    private const float RowHeight = 18f;
    private const float InputHeight = 22f;

    // 화면 맨 아래 한 줄은 항상 입력창(열렸을 때) 또는 안내(닫혔을 때) 자리다.
    // 로그는 그 위에 쌓이므로, 창을 열고 닫아도 로그가 위아래로 튀지 않는다.
    private static float BottomRowY() { return Screen.height - 10f - InputHeight; }

    private void DrawLog()
    {
        if (lines.Count == 0)
            return;

        float logH = lines.Count * RowHeight + 8f;
        float y = BottomRowY() - 4f - logH;

        GUI.DrawTexture(new Rect(10f, y, Width, logH), BoxTexture(), ScaleMode.StretchToFill);
        for (int i = 0; i < lines.Count; i++)
            GUI.Label(new Rect(18f, y + 4f + i * RowHeight, Width - 16f, RowHeight), lines[i], RowStyle());
    }

    // 닫혀 있을 때의 안내 한 줄. 채팅 창이 있다는 것을 알리는 유일한 표시다.
    private void DrawHint()
    {
        GUI.Label(new Rect(14f, BottomRowY(), Width, InputHeight), "Enter: 채팅 / 치트 (help)", HintStyle());
    }

    private void DrawInput()
    {
        float y = BottomRowY();

        GUI.SetNextControlName(InputControlName);
        draft = GUI.TextField(new Rect(10f, y, Width, InputHeight), draft ?? "", 200);

        // 창을 연 프레임에는 아직 컨트롤이 없어서 포커스를 줄 수 없다. 그린 다음에 준다.
        if (focusRequested)
        {
            GUI.FocusControl(InputControlName);
            focusRequested = false;
        }
    }

    private void Submit()
    {
        string text = (draft ?? "").Trim();

        if (text.Length > 0)
        {
            Append("> " + text);
            if (send != null)
                send(text);
        }

        Close();
    }

    private void Close()
    {
        open = false;
        draft = "";
        focusRequested = false;
        GUI.FocusControl(null);
    }

    // ── 스타일(InputHandler 의 HUD 와 같은 톤) ──
    private static Texture2D boxTex;
    private static GUIStyle rowStyle;

    private static Texture2D BoxTexture()
    {
        if (boxTex == null)
        {
            boxTex = new Texture2D(1, 1);
            boxTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            boxTex.Apply();
        }
        return boxTex;
    }

    private static GUIStyle RowStyle()
    {
        if (rowStyle == null)
            rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = false };
        return rowStyle;
    }

    private static GUIStyle hintStyle;

    private static GUIStyle HintStyle()
    {
        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            hintStyle.normal.textColor = new Color(0.7f, 0.85f, 1f, 0.75f);
        }
        return hintStyle;
    }
}
