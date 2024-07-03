using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    internal class PVPMode : BoolSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_PVPMode.SetValue(Value);

        public string GetDisplayName() => "PvP mode";

        protected override bool GetDefaultValue() => false;
    }
}
