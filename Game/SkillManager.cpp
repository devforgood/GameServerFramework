#include "SkillManager.h"
#include "Actor.h"
#include "GridManager.h"

void useAoESkill(Actor* caster, GridManager& grid, float range, float dirDeg) {
    auto targets = grid.getEntitiesInAoEMask(caster->get_vecter2_x(), caster->get_vecter2_y(), range, dirDeg);
    for (Actor* t : targets) {
        std::cout << "Skill hit Entity " << t->agent_id() << "\n";
    }
    grid.broadcastToNearby(caster->get_vecter2_x(), caster->get_vecter2_y(), range, "Skill Effect Shown");
}
