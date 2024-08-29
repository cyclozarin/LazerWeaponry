using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    [SettingRegister("<size=75%>LAZERWEAPONRY SETTINGS</size>", "PvP settings")]
    internal class PVPMode : BoolSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_PVPMode.SetValue(Value);

        public string GetDisplayName() => "PvP mode";

        protected override bool GetDefaultValue() => false;
    }
}
