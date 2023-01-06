using System.Linq;
using Content.Server.Ghost.Roles;
using Content.Server.Cult.Components;
using Content.Server.Popups;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.Cult;
using Content.Shared.Humanoid;
using Content.Shared.Stacks;
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Linguini.Syntax.Ast;
using System.Text;
using Content.Server.Construction.Completions;

namespace Content.Server.Cult
{
    public sealed partial class CultSystem : EntitySystem
    {
        [Dependency] private readonly EntityManager _entityManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly UserInterfaceSystem _userint = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly CultRuleSystem _cultrule = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HumanoidComponent, CultCommuneActionEvent>(OnCommuneAction);
            SubscribeLocalEvent<HumanoidComponent, CultCommuneSendMsgEvent>(OnCommuneSendMessage);
            SubscribeLocalEvent<HumanoidComponent, CultTwistedConstructionActionEvent>(OnTwistedConstructionAction);
            SubscribeLocalEvent<HumanoidComponent, CultHideSpellActionEvent>(OnHideAction);
            SubscribeLocalEvent<HumanoidComponent, CultRevealSpellActionEvent>(OnRevealAction);
            
            

        }

        private void OnCommuneAction(EntityUid uid, HumanoidComponent component, CultCommuneActionEvent args)
        {
            /*
             * Test
             */
            if (!TryComp<ActorComponent>(args.Performer, out var actor))
                return;

            string message = "Cult";
            // Collect cultists
            IEnumerable<INetChannel> cultists = GetCultChatClients();

            // Whisper your message in IC
            _chat.TrySendInGameICMessage(args.Performer, message, InGameICChatType.Whisper,false,false, null, null, null, false);
            // Wrap message
            var playerName = Name(args.Performer);

            var wrappedMessage = Loc.GetString("chat-manager-send-cult-chat-wrap-message",
                ("cultChannelName", Loc.GetString("chat-manager-cult-channel-name")),
                ("playerName", (playerName)),
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Cult chat from {actor.PlayerSession:Player}: {message}");
            // Send message to cult channel
            _chatManager.ChatMessageToMany(ChatChannel.Cult, message, wrappedMessage, args.Performer, false, false, cultists.ToList());


            /*
             * 
             * Please, refactor UserInterface! I beg you!
             * 
             * 
            if (!TryComp<ActorComponent>(args.Performer, out var actor))
                return;
            if (!_userint.TryToggleUi(component.Owner, CultCommuneUiKey.Key, actor.PlayerSession))
                return;
            */
        }

        private void OnCommuneSendMessage(EntityUid uid, HumanoidComponent component, CultCommuneSendMsgEvent args)
        {
            /*
             * Uncomment this after user interface refactor
             * 
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            string message = "Cult";
            // Collect cultists
            IEnumerable<INetChannel> cultists = GetCultChatClients();

            // Whisper your message in IC
            _chat.TrySendInGameICMessage(args.User, message, InGameICChatType.Whisper, false, false, null, null, null, false);
            // Wrap message
            var playerName = Name(args.User);

            var wrappedMessage = Loc.GetString("chat-manager-send-cult-chat-wrap-message",
                ("cultChannelName", Loc.GetString("chat-manager-cult-channel-name")),
                ("playerName", (playerName)),
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Cult chat from {actor.PlayerSession:Player}: {message}");
            // Send message to cult channel
            _chatManager.ChatMessageToMany(ChatChannel.Cult, message, wrappedMessage, args.User, false, false, cultists.ToList());
            */
        }

        private void OnTwistedConstructionAction(EntityUid uid, HumanoidComponent component, CultTwistedConstructionActionEvent args)
        {
            /*
             * TO-DO
             * 1) Stack of metal into Construction Shell
             * 2) Cyborg into Construct
             * 3) Cyborg shell into Construction Shell
             */


            if (!_entityManager.TryGetComponent<StackComponent>(args.Target, out var stack))
                return;

            // TO-DO - Remove this hardcoded thing
            if (stack.StackTypeId == "Plasteel")
            {
                var transform = Transform(args.Target);
                if (transform == null)
                    return;

                var count = stack.Count;
                var coord = transform.Coordinates;
                _entityManager.DeleteEntity(args.Target);
                var material = _entityManager.SpawnEntity("MaterialRunedMetal1",coord);
                if (!_entityManager.TryGetComponent<StackComponent>(material, out var stack_new))
                    return;
                stack_new.Count= count;

                // TO-DO - Add localization
                _popup.PopupEntity(Loc.GetString("Transform Plasteel into Runed metal"), args.Performer, args.Performer);
            }
        }

        private void OnHideAction(EntityUid uid, HumanoidComponent component, CultHideSpellActionEvent args)
        {
            // TO-DO - Transer range to somewhere
            var targets = _lookup.GetEntitiesInRange(uid, 2f, LookupFlags.StaticSundries);
            targets.RemoveWhere(x => !_entityManager.HasComponent<CultRuneBaseComponent>(x));       // Add component for cult structures
            foreach (var target in targets)
            {
                _appearance.SetData(target, CultRuneVisuals.Visible, false);
            }
            // TO-DO - Add logs
        }

        private void OnRevealAction(EntityUid uid, HumanoidComponent component, CultRevealSpellActionEvent args)
        {
            // TO-DO - Transer range to somewhere
            var targets = _lookup.GetEntitiesInRange(uid, 2f, LookupFlags.StaticSundries);
            targets.RemoveWhere(x => !_entityManager.HasComponent<CultRuneBaseComponent>(x));       // Add component for cult structures
            foreach (var target in targets)
            {
                _appearance.SetData(target, CultRuneVisuals.Visible, true);
            }
            // TO-DO - Add logs
        }

        public bool CheckCultistRole(EntityUid uid)
        {
            if (TryComp<MindComponent>(uid, out var mind))
            {
                if (!mind.HasMind)
                    return false;

                return mind.Mind!.HasRole<CultRole>();
            }
            return false;
        }

        private IEnumerable<INetChannel> GetCultChatClients()
        {
            return Filter.Empty()
                .AddWhereAttachedEntity(entity => CheckCultistRole(entity))
                .Recipients
                .Select(p => p.ConnectedClient);
        }
    }
}
