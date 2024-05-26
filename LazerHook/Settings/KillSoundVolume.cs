using ContentSettings.API.Settings;

namespace LazerHook.Settings
{
    internal class KillSoundVolume : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => Plugin.InitialSettings.KillSoundVolume = Value / 100;

        public string GetDisplayName() => "Kill sound volume";

        protected override int GetDefaultValue() => 100;

        protected override (int, int) GetMinMaxValue() => new(0, 100);
    }
}