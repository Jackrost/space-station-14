using Content.Server.Ghost.Roles;
using Content.Server.Cult.Components;
using Content.Server.Popups;
using Content.Server.Actions;
using Content.Shared.Cult;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;

namespace Content.Server.Cult
{
    public sealed partial class CultSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HumanoidComponent, CultCommuneActionEvent>(OnCommuneAction);

        }

        private void OnCommuneAction(EntityUid uid, HumanoidComponent component, CultCommuneActionEvent args)
        {
            _popup.PopupEntity(Loc.GetString("Hahahaha"), uid, args.Performer);

            // To-do
            // Make check if mob can speak

        }

    }
}
