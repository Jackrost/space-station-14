using Content.Shared.Actions;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared.Cult;

public sealed class CultCommuneEvent : EntityEventArgs
{
    public EntityUid User;
    public string Message;

    public CultCommuneEvent(EntityUid userUid, string message)
    {
        User = userUid;
        Message = message;
    }
}

public sealed class CultCommuneActionEvent : InstantActionEvent { };
