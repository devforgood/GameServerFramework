
public abstract class Skill
{
    public Gamedata.Skill gamedata { get; set; }
    string Name { get; }
    public abstract void OnSelect(GameInputManager context);
    public abstract void OnDeselect(GameInputManager context);
    public abstract void OnSkillButtonDown(GameInputManager context);
    public abstract void OnSkillButtonUp(GameInputManager context);
    public abstract void Update(GameInputManager context);
}