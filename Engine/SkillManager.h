#pragma once

class Actor;
class GridManager;
class SkillManager
{
public:
    void useAoESkill(Actor* caster, GridManager& grid, float range, float dirDeg);
};

