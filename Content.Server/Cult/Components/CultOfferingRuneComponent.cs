using Robust.Shared.GameObjects;

namespace Content.Server.Cult.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(CultRuneComponent))]
    public sealed class CultOfferingRuneComponent : CultRuneComponent
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("sacrificeDeadMinCount")]
        public uint SacrificeDeadMinCount = 1;

        [ViewVariables(VVAccess.ReadWrite), DataField("convertMinCount")]
        public uint ConvertMinCount = 2;

        [ViewVariables(VVAccess.ReadWrite), DataField("sacrificeMinCount")]
        public uint SacrificeMinCount = 3;
    }
}
