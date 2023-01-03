using System.Linq;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
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
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly CultRuleSystem _cultrule = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        [ViewVariables(VVAccess.ReadWrite), DataField("sacrificeDeadMinCount")]
        public uint SacrificeDeadMinCount = 1;

        [ViewVariables(VVAccess.ReadWrite), DataField("convertMinCount")]
        public uint ConvertMinCount = 2;

        [ViewVariables(VVAccess.ReadWrite), DataField("sacrificeMinCount")]
        public uint SacrificeMinCount = 3;


        public override bool InvokeRune(EntityUid rune, EntityUid user, HashSet<EntityUid> cultists)
        {
            var targets = _lookup.GetEntitiesInRange(rune, 10f, LookupFlags.Dynamic);
            targets.RemoveWhere(x => !_entityManager.HasComponent<HumanoidComponent>(x) || _cult.CheckCultistRole(x));

            if (targets.Count == 0)
                return false;
            if (!_entityManager.TryGetComponent<TransformComponent>(rune, out var rune_transform))
                return false;

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

            var canBeConverted = false;
            _entityManager.TryGetComponent<MobStateComponent>(victim.Value, out var mobstate);
            if (_entityManager.TryGetComponent<MindComponent>(victim.Value, out var mind))
            {
                if (mind != null)
                    canBeConverted = mind!.Mind!.AllRoles.Any(role => role is Job { CanBeAntag: true });
            }

            /* 
             *  TO-DO Check if target is objective ---------------------------------------------------------------
             */

            if (mobstate!.CurrentState != DamageState.Dead)
            {
                if (canBeConverted)
                    return Sacrifice(victim.Value, user, cultists, false);
                else
                    return Convert(victim.Value, user, cultists);
            }
            return SacrificeNonOvjectiveDead(victim.Value, user, cultists);
        }

        public bool Sacrifice(EntityUid target, EntityUid user, HashSet<EntityUid> cultists, bool objective)
        {
            if (cultists.Count < SacrificeMinCount)
            {
                _popup.PopupEntity(Loc.GetString("cult-rune-offering-not-enought-to-sacrifice"), user, user);
                return false;
            }

            // Check if target is objective
            if (objective)
            {
                // SendMessage(sacrificed);
                _bodySystem.GibBody(target);
                // Logs - sacrificed-objective
                return true;
            }


            // SendMessage(sacrificed);
            _bodySystem.GibBody(target);
            // Logs - sacrificed
            return true;
        }

        public bool SacrificeNonOvjectiveDead(EntityUid target, EntityUid user, HashSet<EntityUid> cultists)
        {
            if (cultists.Count < SacrificeDeadMinCount)
            {
                _popup.PopupEntity(Loc.GetString("cult-rune-offering-not-enought-to-sacrifice"), user, user);
                return false;
            }

            // SendMessage(sacrificed);
            _bodySystem.GibBody(target);
            // Logs - sacrificed
            return true;
        }

        public bool Convert(EntityUid target, EntityUid user, HashSet<EntityUid> cultists)
        {
            if (cultists.Count < ConvertMinCount)
            {
                _popup.PopupEntity(Loc.GetString("cult-rune-offering-not-enought-to-convert"), user, user);
                return false;
            }

            if (_entityManager.TryGetComponent<MindComponent>(target, out var mind))
            {
                if (mind != null)
                {
                    // Error - no mind
                    return false;
                } 
            }
            _cultrule.MakeCultist(mind!.Mind!.Session!);
            // Logs - converted
            return true;
        }
    }
}
