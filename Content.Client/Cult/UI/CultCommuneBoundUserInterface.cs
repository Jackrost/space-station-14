using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Client.Player;
using Content.Shared.Cult;

namespace Content.Client.Cult.Commune.UI
{
    [UsedImplicitly]
    public sealed class CultCommuneBoundUserInterface : BoundUserInterface
    {
        private CultCommuneWindow? _window;

        public CultCommuneBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new CultCommuneWindow
            {
                Title = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Owner).EntityName,
            };
            _window.OnClose += Close;
            _window.Input.OnTextEntered += Input_OnTextEntered;
            _window.OpenCentered();

        }

        /*
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _window?.Populate((CultCommuneBoundUserInterfaceState) state);
        }
        */

        
        private void Input_OnTextEntered(LineEdit.LineEditEventArgs obj)
        {
            if (!string.IsNullOrEmpty(obj.Text))
            {

                if (_window != null)
                {
                    _window.Input.Text = string.Empty;
                }

                var localPlayer = IoCManager.Resolve<IPlayerManager>()?.LocalPlayer?.ControlledEntity;
                if (localPlayer != null)
                    SendMessage(new CultCommuneSendMsgEvent(localPlayer.Value, obj.Text));
            }

            _window?.Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
