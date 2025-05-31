
public interface BaseSkill
{
    string Name { get; }
    void OnSelect(GameInputManager context);
    void OnDeselect(GameInputManager context);
    void OnSkillButtonDown(GameInputManager context);
    void OnSkillButtonUp(GameInputManager context);
    void Update(GameInputManager context);
}