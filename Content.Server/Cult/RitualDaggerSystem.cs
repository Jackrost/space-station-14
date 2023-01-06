using Content.Server.Cult.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;

namespace Content.Server.Cult
{
    public sealed partial class RitualDaggerSystem : EntitySystem
    {
        [Dependency] private readonly CultSystem _cult = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RitualDaggerComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, RitualDaggerComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (args.Target == null || args.Target == args.User)
            {
                return;
            }

            if (HasComp<CultRuneBaseComponent>(args.Target))
            {
                if (_cult.CheckCultistRole(args.User))
                {
                    _popup.PopupEntity(Loc.GetString(("ritual-dagger-remove-rune"),("rune", args.Target.Value), ("dagger", uid)), args.User, args.User);
                    EntityManager.DeleteEntity(args.Target.Value);
                    return;
                }
            }
        }
    }
}
