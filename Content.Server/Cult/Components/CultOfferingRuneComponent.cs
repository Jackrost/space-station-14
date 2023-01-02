using System.Linq;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Shared.Access.Components;
using Content.Shared.Humanoid;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Cult.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(CultRuneComponent))]
    public sealed class CultOfferingRuneComponent : CultRuneComponent
    {
        [Dependency] private readonly EntityManager _entityManager = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly CultSystem _cult = default!;

        [ViewVariables(VVAccess.ReadWrite), DataField("sacrificeDeadMinCount")]
        public uint SacrificeDeadMinCount = 1;

        [ViewVariables(VVAccess.ReadWrite), DataField("convertMinCount")]
        public uint ConvertMinCount = 2;

        [ViewVariables(VVAccess.ReadWrite), DataField("sacrificeMinCount")]
        public uint SacrificeMinCount = 3;

        /*
        public override bool InvokeRune(EntityUid uid, uint count)
        {
            /*
            if (!TryInvokeRune(uid, count))
                return false;
            return true;
        }
        */

        public override bool GroupInvokeRune(EntityUid rune, EntityUid user, HashSet<EntityUid> cultists)
        {
            var targets = _lookup.GetEntitiesInRange(rune, 10f, LookupFlags.Dynamic);
            targets.RemoveWhere(x => !_entityManager.HasComponent<HumanoidComponent>(x) || _cult.CheckCultistRole(x));

            if (targets.Count == 0)
                return false;
            if (!_entityManager.TryGetComponent<TransformComponent>(rune, out var rune_transform))
                return false;

            //Vector2 rune_pos = new Vector2(rune_transform.WorldPosition.X, rune_transform.WorldPosition.Y);

            float range = 999f;
            EntityUid? victim = null;
            foreach (var target in targets)
            {
                if (!_entityManager.TryGetComponent<TransformComponent>(target, out var target_transform))
                    return false;

                rune_transform.Coordinates.TryDistance(_entityManager,target_transform.Coordinates, out var new_range);

                if (range < new_range)
                {
                    range = new_range;
                    victim = target;
                }
            }

            if (victim == null)
                return false;

            return ProcessRune(victim.Value, cultists.Count);
        }

        public bool ProcessRune(EntityUid target, int cultCount)
        {
            var canBeConverted = false;
            _entityManager.TryGetComponent<MobStateComponent>(target, out var mobstate);
            if (_entityManager.TryGetComponent<MindComponent>(target, out var mind))
            {
                if (mind != null)
                    canBeConverted = mind!.Mind!.AllRoles.Any(role => role is Job { CanBeAntag: true });
            }

            /* Check if target is objective
             * 
             * Is it objective or it's alive?
             * if (cultCount < SacrificeMinCount)
             * {
             * SendMessage(fail);
             * return false;
             * } else
             * {
             *      Sacrifice(target);
             *      return true;
             * }
             * 
             */


            if (mobstate!.CurrentState == DamageState.Dead)
            {
                // If mob dead - sacrifice
            }
            // If mob alive and mind-shilded or is objective - sacrifice

            // Else - convert
            return true;
        }

        public void Sacrifice(EntityUid target, EntityUid user, HashSet<EntityUid> cultists)
        {
            // Check if target is objective

            // Gib target

            // SendMessage(sacrificed);
        }

    }
}
