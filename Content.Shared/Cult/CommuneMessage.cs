using Content.Shared.Actions;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared.Cult;

[Serializable, NetSerializable]
public sealed class CultCommuneSendMsgEvent : BoundUserInterfaceMessage
{
    public EntityUid User;
    public string Message;

    public CultCommuneSendMsgEvent(EntityUid userUid, string message)
    {
        User = userUid;
        Message = message;
    }
}

[Serializable, NetSerializable]
public enum CultCommuneUiKey : byte
{
    Key
}

public sealed class CultCommuneActionEvent : InstantActionEvent { };
