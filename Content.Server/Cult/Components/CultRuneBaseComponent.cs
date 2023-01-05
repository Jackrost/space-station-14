using Robust.Shared.GameObjects;

namespace Content.Server.Cult.Components
{
    [RegisterComponent]
    public sealed class CultRuneBaseComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("invokersMinCount")]
        public uint InvokersMinCount = 1;

        [ViewVariables(VVAccess.ReadWrite), DataField("gatherInvokers")]
        public bool GatherInvokers = true;

        [ViewVariables(VVAccess.ReadWrite), DataField("cultistGatheringRange")]
        public float CultistGatheringRange = 0.7f;

        [ViewVariables(VVAccess.ReadWrite), DataField("invokePhrase")]
        public string InvokePhrase = "";
    }
}
