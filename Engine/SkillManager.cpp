#include "SkillManager.h"
#include "Actor.h"
#include "../Engine/GridManager.h"

void useAoESkill(Actor* caster, GridManager& grid, float range, float dirDeg) {
    auto targets = grid.getEntitiesInAoEMask(caster->GetVecter2X(), caster->GetVecter2Y(), range, dirDeg);
    for (IGridActor* t : targets) {
        std::cout << "Skill hit Entity " << t->GetActorId() << "\n";
    }
    grid.broadcastToNearby(caster->GetVecter2X(), caster->GetVecter2Y(), range, "Skill Effect Shown");
}
