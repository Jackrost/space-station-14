using Robust.Shared.GameObjects;

namespace Content.Server.Cult.Components
{
    [RegisterComponent]
    public class CultRuneComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("invokersMinCount")]
        public uint InvokersMinCount = 1;

        [ViewVariables(VVAccess.ReadWrite), DataField("invokePhrase")]
        public string InvokePhrase = "";

        public bool TryInvokeRune(EntityUid uid, uint count)
        {
            if (count < InvokersMinCount)
                return false;
            return true;
        }

        public virtual bool InvokeRune(EntityUid rune, EntityUid user, HashSet<EntityUid> cultists)
        {
            return true;
        }
    }
}
