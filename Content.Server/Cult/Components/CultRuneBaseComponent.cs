using Robust.Shared.GameObjects;

namespace Content.Server.Cult.Components
{
    [RegisterComponent]
    public class CultRuneBaseComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("invokersMinCount")]
        public uint InvokersMinCount = 1;

        [ViewVariables(VVAccess.ReadWrite), DataField("invokePhrase")]
        public string InvokePhrase = "";
    }
}
