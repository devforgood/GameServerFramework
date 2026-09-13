using System.Collections.Generic;
using UnityEngine;

// 채팅 창 — 입력 한 줄을 서버로 보내고, 서버가 돌려준 한 줄을 로그에 쌓아 보여준다.
//
// 지금 이 통로로 서버가 하는 일은 치트 명령 처리뿐이다(syncnet.fbs 의 Chat 주석 참고).
// 그래서 여기 있는 것은 입력창과 로그, 그리고 '/' 로 시작하는 줄의 자동완성이 전부고,
// 채널/귓속말 같은 것은 없다.
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
//
// 키 규칙:
//   Enter  후보를 골라 둔 상태면 그 후보로 채우고, 아니면 지금 줄을 보낸다
//   Tab    후보 하나면 바로 완성, 여럿이면 공통 접두사까지 채운다
//   ↑ ↓    후보가 떠 있으면 후보 고르기, 아니면 보낸 줄 되불러오기
//   Esc    후보가 떠 있으면 목록만 닫고, 아니면 창을 닫는다
public class ChatWindow
{
    private const int MaxLines = 8;        // 화면에 남겨 두는 줄 수
    private const int MaxHistory = 20;     // 되불러올 수 있는 입력 줄 수
    private const string InputControlName = "chat_input";

    private readonly System.Action<string> send;
    private readonly System.Action requestCommands;
    private readonly CheatCompletion completion;
    private readonly List<string> lines = new List<string>();
    private readonly List<string> history = new List<string>();

    private bool open;
    private string draft = "";
    private bool focusRequested;

    // 지금 떠 있는 후보. draft 가 바뀔 때만 다시 만든다.
    private List<CheatCompletion.Suggestion> suggestions = new List<CheatCompletion.Suggestion>();
    private string suggestedFor;           // suggestions 를 만들 때 쓴 draft
    private int selected = -1;             // 고른 후보(-1 이면 고르지 않음)
    private bool suggestionsHidden;        // Esc 로 목록만 닫은 상태

    private int historyIndex = -1;         // 되불러오기 위치(-1 이면 지금 치는 줄)
    private bool caretToEnd;               // 프로그램이 draft 를 바꿨으니 커서를 끝으로

    /// <summary>입력창이 열려 있어 키보드를 가로채는 중인가. 게임 입력은 이때 쉰다.</summary>
    public bool IsCapturing { get { return open; } }

    public ChatWindow(System.Action<string> send, CheatCompletion completion, System.Action requestCommands)
    {
        this.send = send;
        this.completion = completion ?? new CheatCompletion();
        this.requestCommands = requestCommands;
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
                Open();

            DrawLog();   // 닫혀 있어도 최근 줄은 남긴다. 엔터와 함께 사라지면 못 읽는다.
            DrawHint();  // 채팅 창이 있다는 것 자체를 알려 준다(스킬 HUD 와 같은 톤).
            return;
        }

        // 후보는 draft 에서 다시 만든다. 타이핑은 TextField 안에서 일어나므로
        // 그 결과를 다음 프레임 첫머리에서 받아 계산한다(한 프레임 뒤에 뜬다).
        RefreshSuggestions();

        // ── 열려 있을 때: 그리기 전에 키를 먼저 본다 ──
        // TextField 는 포커스를 가진 동안 키 이벤트를 소비하므로, 그린 뒤에 보면 오지 않는다.
        bool tab = IsTabEvent(e);
        if (tab)
        {
            e.Use();
            if (tabFrame == Time.frameCount)
                tab = false;
            else
                tabFrame = Time.frameCount;
        }

        bool cancel = false;
        bool up = false;
        bool down = false;
        if (!enter && !tab && e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Escape) { cancel = true; e.Use(); }
            else if (e.keyCode == KeyCode.UpArrow) { up = true; e.Use(); }
            else if (e.keyCode == KeyCode.DownArrow) { down = true; e.Use(); }
        }

        if (cancel)
        {
            // 목록이 떠 있으면 목록만 닫는다. 창까지 닫으면 치던 줄이 사라진다.
            if (SuggestionsVisible)
                suggestionsHidden = true;
            else
                Close();
        }
        else if (tab)
        {
            ApplyTab();
        }
        else if (up || down)
        {
            if (SuggestionsVisible)
                MoveSelection(up ? -1 : 1);
            else
                RecallHistory(up ? 1 : -1);
        }
        else if (enter && SuggestionsVisible && selected >= 0)
        {
            // 고른 후보가 있으면 엔터는 '채우기' 다. 보내는 것은 그 다음 엔터.
            Accept(suggestions[selected].Completion);
            enter = false;
        }

        if (enter)
        {
            Submit();
            DrawLog();
            DrawHint();
            return;
        }

        DrawLog();
        DrawSuggestions();
        DrawInput();
    }

    // 이 프레임에서 이미 처리한 키. 한 번의 입력이 이벤트 둘로 오기 때문에 필요하다.
    private int enterFrame = -1;
    private int tabFrame = -1;

    private static bool IsEnterEvent(Event e)
    {
        if (e == null || e.type != EventType.KeyDown)
            return false;

        return e.keyCode == KeyCode.Return
            || e.keyCode == KeyCode.KeypadEnter
            || e.character == '\n'
            || e.character == '\r';
    }

    // Tab 도 엔터와 같이 keyCode 이벤트와 character('\t') 이벤트로 두 번 온다.
    // 소비하지 않으면 IMGUI 가 다음 컨트롤로 포커스를 옮겨 입력창에서 커서가 빠져나간다.
    private static bool IsTabEvent(Event e)
    {
        if (e == null || e.type != EventType.KeyDown)
            return false;

        return e.keyCode == KeyCode.Tab || e.character == '\t';
    }

    private void Open()
    {
        open = true;
        focusRequested = true;
        draft = "";
        historyIndex = -1;
        ResetSuggestions();

        // 명령표가 아직 없으면 서버에 한 번 물어본다. 치트가 꺼진 서버는 빈 목록을 주고,
        // 그러면 자동완성은 조용히 꺼진 채로 채팅만 된다.
        if (requestCommands != null && !completion.HasCommands)
            requestCommands();
    }

    private bool SuggestionsVisible
    {
        get { return !suggestionsHidden && suggestions != null && suggestions.Count > 0; }
    }

    private void RefreshSuggestions()
    {
        if (draft == suggestedFor)
            return;

        suggestedFor = draft;
        suggestions = completion.Suggest(draft);
        selected = -1;
        suggestionsHidden = false;
    }

    private void ResetSuggestions()
    {
        suggestions = new List<CheatCompletion.Suggestion>();
        suggestedFor = draft;
        selected = -1;
        suggestionsHidden = false;
    }

    private void MoveSelection(int delta)
    {
        if (suggestions.Count == 0)
            return;

        selected += delta;
        if (selected < 0)
            selected = suggestions.Count - 1;
        else if (selected >= suggestions.Count)
            selected = 0;
    }

    // Tab: 후보가 하나면 그것으로 완성하고, 여럿이면 공통 접두사까지만 채운다.
    // 공통 접두사가 지금 친 것과 같으면(더 좁힐 수 없으면) 목록을 그대로 둔다.
    private void ApplyTab()
    {
        if (!SuggestionsVisible)
        {
            suggestionsHidden = false; // Esc 로 닫아 둔 목록은 Tab 으로 다시 연다
            return;
        }

        if (selected >= 0)
        {
            Accept(suggestions[selected].Completion);
            return;
        }

        if (suggestions.Count == 1)
        {
            Accept(suggestions[0].Completion);
            return;
        }

        string common = CheatCompletion.CommonPrefix(suggestions);
        if (!string.IsNullOrEmpty(common) && common.Length > draft.Length)
            Accept(common);
    }

    private void Accept(string completionText)
    {
        draft = completionText;
        caretToEnd = true;

        // 채운 결과로 후보를 다시 만든다. "/spawn " 까지 채웠으면 이어서 몬스터 id 목록이
        // 떠야 한다 — 여기서 목록을 비워 버리면 글자를 한 번 더 쳐야 다시 뜬다.
        suggestedFor = null;
        selected = -1;
        suggestionsHidden = false;
    }

    // ↑ 는 더 오래된 줄, ↓ 는 더 최근 줄. 끝까지 내려오면 치던 자리(빈 줄)로 돌아온다.
    private void RecallHistory(int step)
    {
        if (history.Count == 0)
            return;

        historyIndex += step;
        if (historyIndex >= history.Count)
            historyIndex = history.Count - 1;

        if (historyIndex < 0)
        {
            historyIndex = -1;
            draft = "";
        }
        else
        {
            draft = history[history.Count - 1 - historyIndex];
        }

        caretToEnd = true;
        suggestedFor = null; // 되불러온 줄에 대한 후보를 다시 만든다
    }

    private const float Width = 520f;
    private const float RowHeight = 18f;
    private const float InputHeight = 22f;

    // 화면 맨 아래 한 줄은 항상 입력창(열렸을 때) 또는 안내(닫혔을 때) 자리다.
    // 로그는 그 위에 쌓이므로, 창을 열고 닫아도 로그가 위아래로 튀지 않는다.
    private static float BottomRowY() { return Screen.height - 10f - InputHeight; }

    // 후보 목록이 떠 있으면 로그는 그 위로 밀린다.
    private float LogBottomY()
    {
        if (SuggestionsVisible)
            return SuggestionBoxY() - 4f;

        string hint = HintLine();
        if (hint != null)
            return BottomRowY() - 4f - RowHeight;

        return BottomRowY() - 4f;
    }

    private float SuggestionBoxHeight() { return suggestions.Count * RowHeight + 8f; }
    private float SuggestionBoxY() { return BottomRowY() - 4f - SuggestionBoxHeight(); }

    private void DrawLog()
    {
        if (lines.Count == 0)
            return;

        float logH = lines.Count * RowHeight + 8f;
        float y = (open ? LogBottomY() : BottomRowY() - 4f) - logH;

        GUI.DrawTexture(new Rect(10f, y, Width, logH), BoxTexture(), ScaleMode.StretchToFill);
        for (int i = 0; i < lines.Count; i++)
            GUI.Label(new Rect(18f, y + 4f + i * RowHeight, Width - 16f, RowHeight), lines[i], RowStyle());
    }

    // 닫혀 있을 때의 안내 한 줄. 채팅 창이 있다는 것을 알리는 유일한 표시다.
    private void DrawHint()
    {
        GUI.Label(new Rect(14f, BottomRowY(), Width, InputHeight), "Enter: 채팅 / 치트 (/ 로 시작하면 자동완성)", HintStyle());
    }

    // 후보가 없을 때 입력창 위에 뜨는 한 줄. 인자를 치는 중이면 그 명령의 사용법이,
    // 아니면 키 안내가 나온다 — 자동완성이 어떻게 동작하는지 아무 데도 안 적혀 있으면
    // 아무도 Tab 을 눌러 보지 않는다.
    private string HintLine()
    {
        if (string.IsNullOrEmpty(draft) || draft[0] != '/')
            return null;

        string usage = completion.UsageHint(draft);
        if (usage != null)
            return usage;

        if (!completion.HasCommands)
            return "치트 명령 목록을 받지 못했습니다(서버에서 꺼져 있을 수 있습니다).";

        return "Tab: 완성   ↑↓: 후보   Enter: 전송";
    }

    private void DrawSuggestions()
    {
        if (!SuggestionsVisible)
        {
            string hint = HintLine();
            if (hint != null)
            {
                float hintY = BottomRowY() - 4f - RowHeight;
                GUI.DrawTexture(new Rect(10f, hintY, Width, RowHeight), BoxTexture(), ScaleMode.StretchToFill);
                GUI.Label(new Rect(18f, hintY, Width - 16f, RowHeight), hint, HintStyle());
            }
            return;
        }

        float y = SuggestionBoxY();
        GUI.DrawTexture(new Rect(10f, y, Width, SuggestionBoxHeight()), BoxTexture(), ScaleMode.StretchToFill);

        for (int i = 0; i < suggestions.Count; i++)
        {
            float rowY = y + 4f + i * RowHeight;
            if (i == selected)
                GUI.DrawTexture(new Rect(12f, rowY, Width - 4f, RowHeight), SelectedTexture(), ScaleMode.StretchToFill);

            GUI.Label(new Rect(18f, rowY, Width * 0.45f, RowHeight), suggestions[i].Label, RowStyle());
            GUI.Label(new Rect(18f + Width * 0.45f, rowY, Width * 0.55f - 16f, RowHeight),
                suggestions[i].Help, HintStyle());
        }
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

        // 자동완성/되불러오기로 글자를 바꾼 경우, 커서는 바뀌기 전 위치에 남는다.
        // 그대로 두면 다음 글자가 줄 한가운데에 끼어 들어간다.
        if (caretToEnd)
        {
            var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (editor != null)
            {
                editor.text = draft;
                editor.MoveTextEnd();
            }
            caretToEnd = false;
        }
    }

    private void Submit()
    {
        string text = (draft ?? "").Trim();

        if (text.Length > 0)
        {
            Append("> " + text);
            RememberInHistory(text);
            if (send != null)
                send(text);
        }

        Close();
    }

    private void RememberInHistory(string text)
    {
        // 같은 줄을 연달아 치면 하나만 남긴다(치트는 같은 명령을 반복해서 친다).
        if (history.Count > 0 && history[history.Count - 1] == text)
            return;

        history.Add(text);
        if (history.Count > MaxHistory)
            history.RemoveRange(0, history.Count - MaxHistory);
    }

    private void Close()
    {
        open = false;
        draft = "";
        focusRequested = false;
        historyIndex = -1;
        ResetSuggestions();
        GUI.FocusControl(null);
    }

    // ── 스타일(InputHandler 의 HUD 와 같은 톤) ──
    private static Texture2D boxTex;
    private static Texture2D selectedTex;
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

    private static Texture2D SelectedTexture()
    {
        if (selectedTex == null)
        {
            selectedTex = new Texture2D(1, 1);
            selectedTex.SetPixel(0, 0, new Color(0.25f, 0.45f, 0.75f, 0.75f));
            selectedTex.Apply();
        }
        return selectedTex;
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
