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
using Content.Shared.Access.Components;

namespace Content.Server.Cult
{
    public sealed partial class CultRuneSystem : EntitySystem
    {

        [Dependency] private readonly CultSystem _cult = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly EntityManager _entityManager = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly CultRuleSystem _cultrule = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CultRuneComponent, ActivateInWorldEvent>(OnActivate);
            //SubscribeLocalEvent<CultRuneComponent, CultRuneInvokeSuccessEvent>(OnAfterInvoke);
            SubscribeLocalEvent<CultOfferingRuneComponent, CultRuneInvokeEvent>(OnInvokeOffering);
        }

        private void OnActivate(EntityUid uid, CultRuneComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;

            if (!_cult.CheckCultistRole(args.User))
                return;

            HashSet<EntityUid> cultists = new HashSet<EntityUid>();
            cultists.Add(args.User);
            if (component.InvokersMinCount > 1)
                cultists = GatherCultists(uid);

            if (cultists.Count < component.InvokersMinCount)
            {
                _popup.PopupEntity(Loc.GetString("cult-rune-not-enought-cultist"), args.User, args.User);
                return;
            }

            RaiseLocalEvent(new CultRuneInvokeEvent(uid, args.User,cultists));   
        }

        /*
        private void OnAfterInvoke(EntityUid uid, CultRuneComponent component, CultRuneInvokeSuccessEvent args)
        {
            foreach (var cultist in args.Cultists)
            {
                _chat.TrySendInGameICMessage(cultist, component.InvokePhrase, InGameICChatType.Speak, false, false, null, null, null, false);
            }

            // TO-DO - add logs - "rune was invoke by X"

        }
        */

        private void OnAfterInvoke(EntityUid rune, EntityUid user, HashSet<EntityUid> cultists)
        {
            if (!_entityManager.TryGetComponent<CultRuneComponent>(rune, out var component))
                return;
            foreach (var cultist in cultists)
            {
                _chat.TrySendInGameICMessage(cultist, component.InvokePhrase, InGameICChatType.Speak, false, false, null, null, null, false);
            }

            // TO-DO - add logs - "rune was invoke by X"

        }

        public HashSet<EntityUid> GatherCultists(EntityUid uid)
        {
            // EntityLookupSystem
            var entities = _lookup.GetEntitiesInRange(uid,10f, LookupFlags.Dynamic);
            entities.RemoveWhere(x => !_cult.CheckCultistRole(x));
            return entities;
        }


        public void OnInvokeOffering(EntityUid uid, CultOfferingRuneComponent component, CultRuneInvokeEvent args)
        {
            var targets = _lookup.GetEntitiesInRange(uid, 10f, LookupFlags.Dynamic);
            targets.RemoveWhere(x => !_entityManager.HasComponent<HumanoidComponent>(x) || _cult.CheckCultistRole(x));

            if (targets.Count == 0)
                return;
            if (!_entityManager.TryGetComponent<TransformComponent>(uid, out var rune_transform))
                return;

            float range = 999f;
            EntityUid? victim = null;
            foreach (var target in targets)
            {
                if (!_entityManager.TryGetComponent<TransformComponent>(target, out var target_transform))
                    return;

                rune_transform.Coordinates.TryDistance(_entityManager, target_transform.Coordinates, out var new_range);

                if (range < new_range)
                {
                    range = new_range;
                    victim = target;
                }
            }

            if (victim == null)
                return;

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

            bool result = false;
            if (mobstate!.CurrentState != DamageState.Dead)
            {
                if (canBeConverted)
                    result = Sacrifice(uid,victim.Value, args.User, args.Cultists, false);
                else
                    result = Convert(uid,victim.Value, args.User, args.Cultists);
            } else
                result = SacrificeNonOvjectiveDead(uid,victim.Value, args.User, args.Cultists);

            if (result)
                OnAfterInvoke(uid, args.User, args.Cultists);
        }

        public bool Sacrifice(EntityUid rune,EntityUid target, EntityUid user, HashSet<EntityUid> cultists, bool objective)
        {
            if (!_entityManager.TryGetComponent<CultOfferingRuneComponent>(rune, out var offering))
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
            if (!_entityManager.TryGetComponent<CultOfferingRuneComponent>(rune, out var offering))
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
            if (!_entityManager.TryGetComponent<CultOfferingRuneComponent>(rune, out var offering))
                return false;
            if (cultists.Count < offering.ConvertMinCount)
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


    public sealed class CultRuneInvokeEvent : EntityEventArgs
    {
        public EntityUid Rune { get; }
        public EntityUid User { get; }
        public HashSet<EntityUid> Cultists { get; } = new HashSet<EntityUid>();

        public CultRuneInvokeEvent(EntityUid rune, EntityUid user, HashSet<EntityUid> cultists)
        {
            Rune = rune;
            User = user;
            Cultists = cultists;
        }
    }
}
