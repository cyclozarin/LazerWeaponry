using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    [SettingRegister("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Sound settings")]
    internal class KillSoundVolume : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.InitialSettings.KillSoundVolume = Value / 100f;

        public string GetDisplayName() => "Kill sound volume";

        protected override int GetDefaultValue() => 100;

        protected override (int, int) GetMinMaxValue() => new(0, 100);
    }
}