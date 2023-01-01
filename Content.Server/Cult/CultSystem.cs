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
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Linguini.Syntax.Ast;


namespace Content.Server.Cult
{
    public sealed partial class CultSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly UserInterfaceSystem _userint = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly CultRuleSystem _cult = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HumanoidComponent, CultCommuneActionEvent>(OnCommuneAction);
            SubscribeLocalEvent<HumanoidComponent, CultCommuneSendMsgEvent>(OnCommuneSendMessage);

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

        private bool CheckCultistRole(EntityUid uid)
        {
            if (TryComp<MindComponent>(uid, out var mind))
            {
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
