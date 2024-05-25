using ContentSettings.API.Settings;

namespace LazerHook.Settings
{
    internal class VulnerableEnemies : BoolSetting, ICustomSetting
    {
        public override void ApplyValue() => Plugin.SyncedSettings.sync_VulnerableEnemies.SetValue(Value);

        public string GetDisplayName() => "Vulnerable enemies";

        protected override bool GetDefaultValue() => false;
    }
}
