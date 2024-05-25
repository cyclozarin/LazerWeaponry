using ContentSettings.API.Settings;

namespace LazerHook.Settings
{
    internal class PVPMode : BoolSetting, ICustomSetting
    {
        public override void ApplyValue() => Plugin.SyncedSettings.sync_PVPMode.SetValue(Value);

        public string GetDisplayName() => "PvP mode";

        protected override bool GetDefaultValue() => false;
    }
}
