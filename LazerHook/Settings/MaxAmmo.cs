using ContentSettings.API.Settings;

namespace LazerHook.Settings
{
    internal class MaxAmmo : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => Plugin.SyncedSettings.sync_MaxAmmo.SetValue(Value);

        public string GetDisplayName() => "Maximum ammo";

        protected override int GetDefaultValue() => 10;

        protected override (int, int) GetMinMaxValue() => (2, 20);
    }
}
