using Content.Server.Cult.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Chat.Systems;
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

            bool result = false;
            HashSet<EntityUid> cultists = new HashSet<EntityUid>();
            if (component.InvokersMinCount > 1)
            {
                cultists = GatherCultists(uid);
                cultists.Add(args.User);
                if (cultists.Count < component.InvokersMinCount)
                    return;

                result = component.GroupInvokeRune(uid, args.User, cultists);
                foreach (var cultist in cultists)
                {
                    _chat.TrySendInGameICMessage(cultist, component.InvokePhrase, InGameICChatType.Speak, false, false, null, null, null, false);
                }

            }
            else
            {
                result = component.InvokeRune(uid, args.User);
                _chat.TrySendInGameICMessage(args.User, component.InvokePhrase, InGameICChatType.Speak, false, false, null, null, null, false);
            }

            if (!result)
                return;

            
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
