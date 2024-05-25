using ContentSettings.API.Settings;

namespace LazerHook.Settings
{
    internal class KillReward : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => Plugin.SyncedSettings.sync_KillReward.SetValue(Value);

        public string GetDisplayName() => "Money reward for killing";

        protected override int GetDefaultValue() => 35;

        protected override (int, int) GetMinMaxValue() => new(20, 75);
    }
}