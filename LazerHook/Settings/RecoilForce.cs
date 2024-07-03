using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    internal class RecoilForce : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_RecoilForce.SetValue(Value);

        public string GetDisplayName() => "Recoil force";

        protected override int GetDefaultValue() => 25;

        protected override (int, int) GetMinMaxValue() => new(10, 50);
    }
}
