using Assets.Scripts.GameData;
using System.Collections.Generic;

// 채팅 입력의 치트 자동완성.
//
// 명령표는 서버에서 받는다(syncnet.CheatList). 클라에 목록을 적어 두면 서버에서 지운 명령이
// 자동완성에 남아 "있는 줄 알고 친 명령" 이 되기 때문이다. 반대로 인자로 쓰는 id 목록은
// 서버에서 받지 않는다 — 몬스터 종류만 수백 개라 목록을 통째로 보내는 것은 낭비이고,
// 클라도 같은 GameData 를 읽으므로(단일 소스) 종류만 알면 같은 후보를 만들 수 있다.
//
// 입력 상태(무엇을 치는 중인가)는 전부 draft 문자열 하나에서 다시 계산한다. 커서 위치나
// 입력 이력 같은 것을 따로 들고 있으면 창을 닫았다 열거나 붙여넣기를 했을 때 어긋난다.
public class CheatCompletion
{
    /// <summary>서버가 알려준 명령 하나.</summary>
    public class Command
    {
        public string Name = "";
        public string Args = "";      // "<monsterId> [count]" — 인자 힌트
        public string Help = "";
        public string Complete = "";  // 첫 인자를 고를 데이터 종류("monster"/"item"/...)
    }

    /// <summary>후보 한 줄.</summary>
    public struct Suggestion
    {
        public string Completion; // 고르면 입력창에 그대로 들어가는 문자열
        public string Label;      // 화면에 보이는 이름
        public string Help;       // 오른쪽에 흐리게 붙는 설명
    }

    private const int MaxSuggestions = 8;

    private readonly List<Command> commands = new List<Command>();

    /// <summary>서버에서 명령표를 받았는가. 못 받았으면 자동완성이 조용히 꺼진다.</summary>
    public bool HasCommands { get { return commands.Count > 0; } }

    public void SetCommands(List<Command> received)
    {
        commands.Clear();
        if (received != null)
            commands.AddRange(received);
    }

    /// <summary>
    /// 지금 입력 중인 줄에 대한 후보. '/' 로 시작하지 않으면(일반 채팅) 빈 목록이다.
    /// </summary>
    public List<Suggestion> Suggest(string draft)
    {
        var result = new List<Suggestion>();
        if (string.IsNullOrEmpty(draft) || draft[0] != '/')
            return result;

        string body = draft.Substring(1);
        bool typingNextWord = body.Length > 0 && body[body.Length - 1] == ' ';
        string[] words = body.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

        // ── 명령 이름을 치는 중 ──
        if (words.Length == 0 || (words.Length == 1 && !typingNextWord))
        {
            string prefix = words.Length == 1 ? words[0] : "";
            foreach (var command in commands)
            {
                if (!StartsWithIgnoreCase(command.Name, prefix))
                    continue;

                result.Add(new Suggestion
                {
                    // 인자가 있는 명령은 공백까지 붙여 준다 — 고른 다음 바로 인자를 친다.
                    Completion = "/" + command.Name + (string.IsNullOrEmpty(command.Args) ? "" : " "),
                    Label = "/" + command.Name + (string.IsNullOrEmpty(command.Args) ? "" : " " + command.Args),
                    Help = command.Help,
                });

                if (result.Count >= MaxSuggestions)
                    break;
            }
            return result;
        }

        // ── 첫 인자(id)를 치는 중 ──
        Command target = Find(words[0]);
        if (target == null || string.IsNullOrEmpty(target.Complete))
            return result;

        // 두 번째 인자부터는 완성할 데이터가 없다(개수/반경 같은 숫자다).
        int argIndex = typingNextWord ? words.Length - 1 : words.Length - 2;
        if (argIndex != 0)
            return result;

        string argPrefix = typingNextWord ? "" : words[words.Length - 1];
        AppendDataSuggestions(result, target, argPrefix);
        return result;
    }

    /// <summary>
    /// 후보가 없을 때 보여 줄 인자 힌트. 명령 이름을 다 치고 인자를 넣는 중이면
    /// 그 명령의 사용법 한 줄을 돌려준다(무엇을 더 써야 하는지가 보여야 한다). 없으면 null.
    /// </summary>
    public string UsageHint(string draft)
    {
        if (string.IsNullOrEmpty(draft) || draft[0] != '/')
            return null;

        string[] words = draft.Substring(1).Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return null;

        Command target = Find(words[0]);
        if (target == null)
            return null;

        string usage = "/" + target.Name;
        if (!string.IsNullOrEmpty(target.Args))
            usage += " " + target.Args;
        return usage + "   " + target.Help;
    }

    /// <summary>후보들이 공통으로 가진 접두사. Tab 한 번에 여기까지 채운다.</summary>
    public static string CommonPrefix(List<Suggestion> suggestions)
    {
        if (suggestions == null || suggestions.Count == 0)
            return null;

        string prefix = suggestions[0].Completion;
        for (int i = 1; i < suggestions.Count; i++)
        {
            string other = suggestions[i].Completion;
            int length = 0;
            while (length < prefix.Length && length < other.Length
                && char.ToLowerInvariant(prefix[length]) == char.ToLowerInvariant(other[length]))
            {
                length++;
            }
            prefix = prefix.Substring(0, length);
        }
        return prefix;
    }

    private Command Find(string name)
    {
        foreach (var command in commands)
            if (string.Equals(command.Name, name, System.StringComparison.OrdinalIgnoreCase))
                return command;
        return null;
    }

    // 서버가 알려준 종류에 맞는 데이터에서 id 후보를 만든다.
    // 이름은 화면에 보이는 말로 붙인다(name_id 는 지역화 키라 그대로 두면 못 읽는다).
    //
    // 사전 순회 순서는 보장되지 않으므로 id 로 정렬한 뒤 앞에서 잘라 낸다 — 같은 입력에
    // 매번 다른 후보가 나오면 Tab 을 믿을 수 없다.
    private void AppendDataSuggestions(List<Suggestion> result, Command command, string prefix)
    {
        var resource = GameManager.Instance != null ? GameManager.Instance.resource : null;
        if (resource == null)
            return;

        var matched = new List<KeyValuePair<int, string>>();

        switch (command.Complete)
        {
            case "monster":
                foreach (var entry in resource.MonsterDatas)
                    Collect(matched, prefix, entry.Key, entry.Value != null ? entry.Value.name : "");
                break;

            case "item":
                foreach (var entry in resource.Items)
                    Collect(matched, prefix, entry.Key,
                        entry.Value != null ? resource.GetText(entry.Value.name_id) : "");
                break;

            case "skill":
                foreach (var entry in resource.Skills)
                {
                    if (entry.Value != null && entry.Value.monster_only)
                        continue; // 배울 수 없는 것을 후보로 내면 안 된다
                    Collect(matched, prefix, entry.Key,
                        entry.Value != null ? resource.GetText(entry.Value.name_id) : "");
                }
                break;

            case "map":
                foreach (var entry in resource.Maps)
                    Collect(matched, prefix, entry.Key, entry.Value != null ? entry.Value.name : "");
                break;

            case "quest":
                foreach (var entry in resource.Quests)
                    Collect(matched, prefix, entry.Key,
                        entry.Value != null ? resource.GetText(entry.Value.name_id) : "");
                break;
        }

        matched.Sort((left, right) => left.Key.CompareTo(right.Key));

        int shown = matched.Count < MaxSuggestions ? matched.Count : MaxSuggestions;
        for (int i = 0; i < shown; i++)
        {
            string idText = matched[i].Key.ToString();
            result.Add(new Suggestion
            {
                Completion = "/" + command.Name + " " + idText + " ",
                Label = idText + "  " + matched[i].Value,
                Help = command.Help,
            });
        }
    }

    // id 로 시작하거나 이름에 검색어가 들어 있으면 후보다. 이름으로도 찾을 수 있어야
    // "고블린이 몇 번이더라" 를 id 표를 뒤지지 않고 넘어갈 수 있다.
    private static void Collect(List<KeyValuePair<int, string>> matched, string prefix, int id, string name)
    {
        string idText = id.ToString();
        if (!string.IsNullOrEmpty(prefix)
            && !idText.StartsWith(prefix)
            && (string.IsNullOrEmpty(name) || name.ToLowerInvariant().IndexOf(prefix.ToLowerInvariant()) < 0))
        {
            return;
        }

        matched.Add(new KeyValuePair<int, string>(id, name));
    }

    private static bool StartsWithIgnoreCase(string text, string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return true;
        return text != null && text.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
    }
}
