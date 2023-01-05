using System.Linq;
using System.Collections.Generic;
using Content.Server.Roles;
using Content.Server.GameTicking.Rules;
using Content.Server.Popups;
using Content.Server.Chat.Systems;
using Content.Server.Body.Systems;
using Content.Server.Cult.Components;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Shared.Cult;
using Content.Shared.Interaction;
using Content.Shared.Humanoid;
using Content.Shared.MobState.Components;
using Content.Shared.MobState;
using Content.Shared.Damage;
using Content.Shared.Access.Components;


namespace Content.Server.Cult
{
    public sealed partial class CultRuneSystem : EntitySystem
    {
        [Dependency] private readonly EntityManager _entityManager = default!;
        [Dependency] private readonly CultRuleSystem _cultrule = default!;
        [Dependency] private readonly CultSystem _cult = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly DamageableSystem _damage = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CultRuneBaseComponent, ActivateInWorldEvent>(OnActivate);
            //SubscribeLocalEvent<CultRuneComponent, CultRuneInvokeSuccessEvent>(OnAfterInvoke);
            SubscribeLocalEvent<CultRuneOfferingComponent, CultRuneInvokeEvent>(OnInvokeOffering);
        }

        private void OnActivate(EntityUid uid, CultRuneBaseComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;

            if (!_cult.CheckCultistRole(args.User))
                return;

            HashSet<EntityUid> cultists = new HashSet<EntityUid>();
            cultists.Add(args.User);
            if (component.InvokersMinCount > 1 || component.GatherInvokers)
                cultists = GatherCultists(uid, component.CultistGatheringRange);

            if (cultists.Count < component.InvokersMinCount)
            {
                _popup.PopupEntity(Loc.GetString("cult-rune-not-enought-cultist"), args.User, args.User);
                return;
            }

            var ev = new CultRuneInvokeEvent(uid, args.User, cultists);
            RaiseLocalEvent(uid, ev,false);
            if (ev.Result)
            {
                // Raise Shared message to other clients - CultRuneInvokeSuccessEvent(uid, args.User, cultists)
                OnAfterInvoke(uid, args.User, cultists);
            }
        }

        private void OnAfterInvoke(EntityUid rune, EntityUid user, HashSet<EntityUid> cultists)
        {
            if (!_entityManager.TryGetComponent<CultRuneBaseComponent>(rune, out var component))
                return;
            foreach (var cultist in cultists)
            {
                _chat.TrySendInGameICMessage(cultist, component.InvokePhrase, InGameICChatType.Speak, false, false, null, null, null, false);
            }

            // TO-DO - add logs - "rune was invoke by X"

        }

        public HashSet<EntityUid> GatherCultists(EntityUid uid, float range)
        {
            // EntityLookupSystem
            var entities = _lookup.GetEntitiesInRange(uid,range, LookupFlags.Dynamic);
            entities.RemoveWhere(x => !_cult.CheckCultistRole(x));
            return entities;
        }


        public void OnInvokeOffering(EntityUid uid, CultRuneOfferingComponent component, CultRuneInvokeEvent args)
        {
            var targets = _lookup.GetEntitiesInRange(uid, component.RangeTarget, LookupFlags.Dynamic | LookupFlags.Sundries);
            targets.RemoveWhere(x => !_entityManager.HasComponent<HumanoidComponent>(x) || _cult.CheckCultistRole(x));

            if (targets.Count == 0)
                return;
            if (!_entityManager.TryGetComponent<TransformComponent>(uid, out var rune_transform))
                return;

            float range = 999f;
            EntityUid? victim = null;
            if (targets.Count > 1)
            {
                foreach (var target in targets)
                {
                    if (!_entityManager.TryGetComponent<TransformComponent>(target, out var target_transform))
                        return;

                    rune_transform.Coordinates.TryDistance(_entityManager, target_transform.Coordinates, out var new_range);

                    if (new_range < range)
                    {
                        range = new_range;
                        victim = target;
                    }
                }
            }
            else
                victim = targets.First();

            if (victim == null)
                return;

            _entityManager.TryGetComponent<MobStateComponent>(victim.Value, out var mobstate);

            /* 
             *  TO-DO Check if target is objective ---------------------------------------------------------------
             */

            bool result = false;
            if (mobstate!.CurrentState != DamageState.Dead)
            {
                var canBeConverted = false;
                if (_entityManager.TryGetComponent<MindComponent>(victim.Value, out var mind))
                {
                    if (mind.HasMind)
                        canBeConverted = mind!.Mind!.AllRoles.Any(role => role is Job { CanBeAntag: true });
                }

                if (canBeConverted)
                    result = Convert(uid, victim.Value, args.User, args.Cultists);
                else
                    result = Sacrifice(uid, victim.Value, args.User, args.Cultists, false);

            } else
                result = SacrificeNonOvjectiveDead(uid,victim.Value, args.User, args.Cultists);

            args.Result = result;
        }

        public bool Sacrifice(EntityUid rune,EntityUid target, EntityUid user, HashSet<EntityUid> cultists, bool objective)
        {
            if (!_entityManager.TryGetComponent<CultRuneOfferingComponent>(rune, out var offering))
                return false;
            if (cultists.Count < offering.SacrificeMinCount)
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

        public bool SacrificeNonOvjectiveDead(EntityUid rune, EntityUid target, EntityUid user, HashSet<EntityUid> cultists)
        {
            if (!_entityManager.TryGetComponent<CultRuneOfferingComponent>(rune, out var offering))
                return false;
            if (cultists.Count < offering.SacrificeDeadMinCount)
            {
                _popup.PopupEntity(Loc.GetString("cult-rune-offering-not-enought-to-sacrifice"), user, user);
                return false;
            }

            // SendMessage(sacrificed);
            _bodySystem.GibBody(target);
            // Logs - sacrificed
            return true;
        }

        public bool Convert(EntityUid rune, EntityUid target, EntityUid user, HashSet<EntityUid> cultists)
        {
            if (!_entityManager.TryGetComponent<CultRuneOfferingComponent>(rune, out var offering))
                return false;
            if (cultists.Count < offering.ConvertMinCount)
            {
                _popup.PopupEntity(Loc.GetString("cult-rune-offering-not-enought-to-convert"), user, user);
                return false;
            }

            if (_entityManager.TryGetComponent<MindComponent>(target, out var mind))
            {
                if (!mind.HasMind)
                {
                    // Error - no mind
                    return false;
                }
            }

            _cultrule.MakeCultist(mind!.Mind!.Session!, false);
            /*
             * TO-DO
             * 
             * 1) Heal - DamageableSystem
             * 2) Give him dagger
             * 3) Logs - converted
             * 
             */

            // Get DamageSpecifier from entity
            // damage = 
            //_damage.TryChangeDamage(target, damage, true);

            return true;
        }
    }

    public sealed class CultRuneInvokeEvent : EntityEventArgs
    {
        public EntityUid Rune { get; set; }
        public EntityUid User { get; set; }
        public HashSet<EntityUid> Cultists { get; set; } = new HashSet<EntityUid>();
        public bool Result { get; set; }

        public CultRuneInvokeEvent(EntityUid rune, EntityUid user, HashSet<EntityUid> cultists)
        {
            Rune = rune;
            User = user;
            Cultists = cultists;
        }
    }

}
