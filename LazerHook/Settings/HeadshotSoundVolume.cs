using BepInEx.Logging;
using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    [SettingRegister("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Sound settings")]
    internal class HeadshotSoundVolume : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.InitialSettings.HeadshotSoundVolume = Value / 100f; 

        public string GetDisplayName() => "Headshot sound volume";

        protected override int GetDefaultValue() => 75;

        protected override (int, int) GetMinMaxValue() => new(0, 100);
    }
}
