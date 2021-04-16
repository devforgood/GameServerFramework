
public enum PlayerState
{
    Idle,
    Move,
    Fly,
    Landing,
    AttackCasting,
    Attack,
    Hit,
    Die,
    SkillCasting,
    Skill,
    TrainHit,
    IdleAfterTrainHit,
    MoveAfterTrainHit,

    MAX,
}

public enum PlayerSkillSubState
{
    Idle,
    Dash,

    MAX,
}
