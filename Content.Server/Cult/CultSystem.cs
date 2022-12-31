using Content.Server.Ghost.Roles;
using Content.Server.Cult.Components;
using Content.Server.Popups;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Shared.Cult;
using Content.Shared.Humanoid;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Robust.Shared.GameObjects;
using Robust.Server.GameObjects;

namespace Content.Server.Cult
{
    public sealed partial class CultSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly UserInterfaceSystem _userint = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HumanoidComponent, CultCommuneActionEvent>(OnCommuneAction);
            SubscribeLocalEvent<HumanoidComponent, CultCommuneSendMsgEvent>(OnCommuneSendMessage);

        }

        private void OnCommuneAction(EntityUid uid, HumanoidComponent component, CultCommuneActionEvent args)
        {
            if (!TryComp<ActorComponent>(args.Performer, out var actor))
                return;

            if (!_userint.TryToggleUi(component.Owner, CultCommuneUiKey.Key, actor.PlayerSession))
                return;
        }

        private void OnCommuneSendMessage(EntityUid uid, HumanoidComponent component, CultCommuneSendMsgEvent args)
        {

            //args.
            //_chat.SendEntityWhisper(args.Performer,args.);
        }

        private bool CheckCultistRole(EntityUid uid)
        {
            if (TryComp<MindComponent>(uid, out var mind))
            {
                return mind.Mind!.HasRole<CultRole>();
            }
            return false;
        }
    }
}
