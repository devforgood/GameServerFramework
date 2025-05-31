// This file is auto-generated. Do not modify directly.

using System;

public static class SkillFactory
{
    public static BaseSkill CreateSkill(int id)
    {
        switch (id)
        {
            case 2: return new JumpSkill();
            case 1: return new NormalAttackSkill();
            default: throw new ArgumentException($"Unknown skill id: {id}");
        }
    }
}