using Content.Server.Chat.Managers;
using Content.Server.Roles;
using Content.Shared.Roles;

namespace Content.Server.Cult
{
    public sealed class CultRole : Role
    {
        public AntagPrototype Prototype { get; }

        public Dictionary<string, string> CultWordsList = new Dictionary<string, string>();

        public CultRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind)
        {
            Prototype = antagPrototype;
            Name = Loc.GetString(antagPrototype.Name);
            Antagonist = antagPrototype.Antagonist;
        }

        public override string Name { get; }
        public override bool Antagonist { get; }

        public void GreetCultist()
        {
            if (Mind.TryGetSession(out var session))
            {
                var chatMgr = IoCManager.Resolve<IChatManager>();
                chatMgr.DispatchServerMessage(session, Loc.GetString("cult-role-greeting"));
                //chatMgr.DispatchServerMessage(session, Loc.GetString("cult-role-cultwords", ("cultwords", string.Join(", ", cultwords))));
            }
        }
    }
}
