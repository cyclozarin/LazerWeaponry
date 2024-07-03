using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    internal class Damage : IntSetting, ICustomSetting
    {

        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_Damage.SetValue(Value);

        public string GetDisplayName() => "Bullet damage";

        protected override int GetDefaultValue() => 10;

        protected override (int, int) GetMinMaxValue() => (5, 25);
    }
}
