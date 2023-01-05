using Content.Shared.AlertLevel;
using Content.Shared.Cult;
using Robust.Client.GameObjects;

namespace Content.Client.Cult
{
    public sealed partial class CultSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CultRuneBaseComponent, AppearanceChangeEvent>(OnAppearanceChange);
        }
        private void OnAppearanceChange(EntityUid uid, CultRuneBaseComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
            {
                return;
            }

            if (_appearance.TryGetData(uid, CultRuneVisuals.Visible, out var data))
                args.Sprite.Visible = (bool)data!;
        }
    }
}
