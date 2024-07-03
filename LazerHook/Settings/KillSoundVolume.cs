using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    internal class KillSoundVolume : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.InitialSettings.KillSoundVolume = Value / 100f;

        public string GetDisplayName() => "Kill sound volume";

        protected override int GetDefaultValue() => 100;

        protected override (int, int) GetMinMaxValue() => new(0, 100);
    }
}