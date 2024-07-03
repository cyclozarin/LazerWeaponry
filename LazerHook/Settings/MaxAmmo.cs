using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    internal class MaxAmmo : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_MaxAmmo.SetValue(Value);

        public string GetDisplayName() => "Ammo";

        protected override int GetDefaultValue() => 10;

        protected override (int, int) GetMinMaxValue() => (2, 20);
    }
}
