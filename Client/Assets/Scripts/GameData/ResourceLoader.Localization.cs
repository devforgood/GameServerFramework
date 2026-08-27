using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.GameData
{
    /// <summary>
    /// 로컬라이즈 문자열 표. 게임 데이터는 화면에 보일 글을 직접 담지 않고 키(text_id)만
    /// 담으며, 실제 문장은 여기서 찾는다.
    ///
    /// 표는 테이블 로더(ResourceLoader.Tables.cs)를 타지 않는다. 그쪽은 id 를 가진 배열을
    /// 다루는 생성 코드인데, 로컬라이즈 파일은 { "키": "문장" } 평평한 객체라 모양이 다르다.
    /// </summary>
    public partial class ResourceLoader
    {
        public Dictionary<string, string> Texts = new Dictionary<string, string>();

        /// <summary>
        /// 키에 해당하는 문장. 없으면 키를 그대로 돌려준다 — 빈칸으로 두면 화면에서
        /// "문장이 없다"와 "키가 틀렸다"를 구분할 수 없다.
        /// </summary>
        public string GetText(string textId)
        {
            if (string.IsNullOrEmpty(textId))
                return string.Empty;

            string value;
            return Texts.TryGetValue(textId, out value) ? value : textId;
        }

        /// <summary>
        /// 시스템 언어에 맞는 파일을 읽는다. 없으면 영어로 떨어진다.
        /// LoadAsync 가 테이블을 읽은 뒤 호출한다.
        /// </summary>
        private IEnumerator LoadLocalization()
        {
            string preferred = Application.systemLanguage == SystemLanguage.Korean
                ? "GameData/Localization/localization_ko"
                : "GameData/Localization/localization_en";

            yield return LoadLocalizationFile(preferred);

            if (Texts.Count == 0 && preferred != "GameData/Localization/localization_en")
                yield return LoadLocalizationFile("GameData/Localization/localization_en");

            if (Texts.Count == 0)
                Debug.LogWarning("ResourceLoader: 로컬라이즈 표가 비었다. 화면에 text_id 가 그대로 보인다.");
        }

        private IEnumerator LoadLocalizationFile(string path)
        {
            var request = Resources.LoadAsync<TextAsset>(path);
            yield return request;

            var asset = request.asset as TextAsset;
            if (asset == null)
            {
                Debug.LogWarning($"ResourceLoader: {path} 를 찾지 못했다.");
                yield break;
            }

            ParseFlatJson(asset.text, Texts);
        }

        /// <summary>
        /// { "키": "값", ... } 만 담긴 평평한 JSON 을 읽는다.
        ///
        /// JsonUtility 로는 안 된다 — Dictionary 를 다루지 못하고, 키가 미리 정해져 있지도
        /// 않기 때문이다. 그래서 문자열 두 개씩 짝지어 읽는 최소 스캐너를 둔다. 값이 문자열이
        /// 아닌 항목(중첩 객체 등)은 건너뛴다.
        /// </summary>
        internal static void ParseFlatJson(string text, Dictionary<string, string> outTable)
        {
            if (string.IsNullOrEmpty(text))
                return;

            int i = 0;
            while (true)
            {
                string key = ReadNextString(text, ref i);
                if (key == null)
                    return;

                // 키 다음에는 반드시 ':' 가 온다. 아니면 값 자리의 문자열을 키로 잘못 읽는다.
                int colon = i;
                while (colon < text.Length && char.IsWhiteSpace(text[colon]))
                    ++colon;

                if (colon >= text.Length || text[colon] != ':')
                    continue;

                i = colon + 1;
                string value = ReadNextString(text, ref i);
                if (value == null)
                    return;

                outTable[key] = value;
            }
        }

        /// <summary>
        /// index 부터 다음 JSON 문자열 하나를 읽고 index 를 그 뒤로 옮긴다. 없으면 null.
        /// </summary>
        private static string ReadNextString(string text, ref int index)
        {
            while (index < text.Length && text[index] != '"')
                ++index;

            if (index >= text.Length)
                return null;

            ++index; // 여는 따옴표
            var builder = new StringBuilder();

            while (index < text.Length)
            {
                char c = text[index++];
                if (c == '"')
                    return builder.ToString();

                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                if (index >= text.Length)
                    break;

                char escaped = text[index++];
                switch (escaped)
                {
                    case 'n': builder.Append('\n'); break;
                    case 't': builder.Append('\t'); break;
                    case 'r': builder.Append('\r'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'u':
                        if (index + 4 <= text.Length)
                        {
                            int code;
                            if (int.TryParse(text.Substring(index, 4),
                                System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture, out code))
                            {
                                builder.Append((char)code);
                            }
                            index += 4;
                        }
                        break;
                    default: builder.Append(escaped); break;   // \" \\ \/ 등
                }
            }

            return null;   // 닫는 따옴표를 못 만났다(파일이 잘렸다)
        }
    }
}
