using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    [SettingRegister("<size=75%>LAZERWEAPONRY SETTINGS</size>", "PvP settings")]
    internal class VulnerableEnemies : BoolSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_VulnerableEnemies.SetValue(Value);

        public string GetDisplayName() => "Vulnerable enemies";

        protected override bool GetDefaultValue() => false;
    }
}
