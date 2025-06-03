using UnityEngine;
using System;

[Flags]
public enum InputModifier
{
    None = 0,
    Ctrl = 1 << 0,
    Alt = 1 << 1,
    Shift = 1 << 2
}

public enum MouseButton
{
    None = -1,
    Left = 0,
    Right = 1,
    Middle = 2
}

[System.Serializable]
public struct InputBinding
{
    public string id;              // 입력 식별자 (예: "monster_spawn", "character_move")
    public MouseButton button;     // 마우스 버튼
    public InputModifier modifier; // 수정자 키 조합
    public string[] validTags;     // 유효한 태그 목록

    // 문자열로 변환 (디버깅 및 로깅용)
    public override string ToString()
    {
        string modString = modifier == InputModifier.None ? "" : modifier.ToString() + "+";
        string buttonString = button.ToString();
        string tagString = validTags != null && validTags.Length > 0 ? " on " + string.Join("/", validTags) : "";
        return $"{modString}{buttonString}{tagString}";
    }

    // 문자열에서 InputBinding 생성
    public static InputBinding Parse(string bindingString)
    {
        // 예: "Ctrl+Alt+Left on floor/monster" 형식의 문자열 파싱
        InputBinding binding = new InputBinding();
        string[] parts = bindingString.Split(new[] { " on " }, StringSplitOptions.None);
        
        // 키 조합 파싱
        string[] keys = parts[0].Split('+');
        binding.modifier = InputModifier.None;
        
        // 마지막 토큰은 마우스 버튼
        if (Enum.TryParse(keys[keys.Length - 1], true, out MouseButton button))
        {
            binding.button = button;
        }

        // 나머지는 수정자 키
        for (int i = 0; i < keys.Length - 1; i++)
        {
            if (Enum.TryParse(keys[i], true, out InputModifier mod))
            {
                binding.modifier |= mod;
            }
        }

        // 태그 파싱
        if (parts.Length > 1)
        {
            binding.validTags = parts[1].Split('/');
        }

        return binding;
    }

    // 현재 입력 상태와 바인딩이 일치하는지 확인
    public bool Matches(MouseButton currentButton, InputModifier currentModifier, string hitTag)
    {
        bool buttonMatches = button == currentButton;
        bool modifierMatches = modifier == currentModifier;
        bool tagMatches = string.IsNullOrEmpty(hitTag) || 
                         validTags == null || 
                         validTags.Length == 0 || 
                         Array.Exists(validTags, tag => tag == hitTag);

        return buttonMatches && modifierMatches && tagMatches;
    }
} 