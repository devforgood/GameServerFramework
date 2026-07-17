
// 스킬 클라이언트 기본 클래스.
// code_name 없는 데이터 전용 스킬(서버 effects 조합으로만 정의)은 파생 클래스가 없으므로,
// SkillFactory 가 이 기본 클래스를 그대로 인스턴스화할 수 있도록 non-abstract 로 두고
// 입력 훅은 기본 no-op 으로 제공한다. 클라 전용 연출/입력이 필요한 스킬만 오버라이드한다.
public class Skill
{
    public Gamedata.Skill gamedata { get; set; }
    public virtual void OnSelect(GameInputManager context) { }
    public virtual void OnDeselect(GameInputManager context) { }
    public virtual void OnSkillButtonDown(GameInputManager context) { }
    public virtual void OnSkillButtonUp(GameInputManager context) { }
    public virtual void Update(GameInputManager context) { }
}
