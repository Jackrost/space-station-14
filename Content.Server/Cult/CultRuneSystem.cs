using Content.Server.Cult.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Cult;
using Content.Shared.Interaction;
using System.Collections.Generic;

namespace Content.Server.Cult
{
    public sealed partial class CultRuneSystem : EntitySystem
    {

        [Dependency] private readonly CultSystem _cult = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CultRuneComponent, ActivateInWorldEvent>(OnActivate);
            //SubscribeLocalEvent<CultRuneComponent, CultCommuneSendMsgEvent>(OnCommuneSendMessage);

        }

        private void OnActivate(EntityUid uid, CultRuneComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;

            if (!_cult.CheckCultistRole(args.User))
                return;

            HashSet<EntityUid> cultists = new HashSet<EntityUid>();
            if (component.InvokersMinCount > 1)
            {
                cultists = GatherCultists(uid);
                cultists.Add(args.User);
            }

            if (cultists.Count < component.InvokersMinCount)
            {
                // TO-DO - Add Popup alert
                _popup.PopupEntity(Loc.GetString("cult-rune-not-enought-cultist"), args.User, args.User);
                return;
            }

            if (component.InvokeRune(uid, args.User, cultists))
                return;

            foreach (var cultist in cultists)
            {
                _chat.TrySendInGameICMessage(cultist, component.InvokePhrase, InGameICChatType.Speak, false, false, null, null, null, false);
            }

            /*
             * TO-DO - add logs - "rune was invoke by X"
             * 
             */
        }

        public HashSet<EntityUid> GatherCultists(EntityUid uid)
        {
            // EntityLookupSystem
            var entities = _lookup.GetEntitiesInRange(uid,10f, LookupFlags.Dynamic);
            entities.RemoveWhere(x => !_cult.CheckCultistRole(x));
            return entities;
        }
    }
}
