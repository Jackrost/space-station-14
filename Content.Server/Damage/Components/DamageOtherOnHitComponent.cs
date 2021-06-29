using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public class DamageOtherOnHitComponent : Component, IThrowCollide
    {
        public override string Name => "DamageOtherOnHit";

        [DataField("damageType",required: true)]
        private readonly string _damageType = default!;

        [DataField("amount")]
        private int _amount = 1;

        [DataField("ignoreResistances")]
        private bool _ignoreResistances;

        void IThrowCollide.DoHit(ThrowCollideEventArgs eventArgs)
        {
            if (!eventArgs.Target.TryGetComponent(out IDamageableComponent? damageable))
                return;
            damageable.ChangeDamage(damageable.GetDamageType(_damageType), _amount, _ignoreResistances, eventArgs.User);
        }
    }
}
