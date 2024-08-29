using ContentSettings.API.Settings;
using ContentSettings.API.Attributes;

namespace LazerWeaponry.Settings
{
    [SettingRegister("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings")]
    internal class Damage : IntSetting, ICustomSetting
    {

        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_Damage.SetValue(Value);

        public string GetDisplayName() => "Bullet damage";

        protected override int GetDefaultValue() => 10;

        protected override (int, int) GetMinMaxValue() => (5, 25);
    }
}
