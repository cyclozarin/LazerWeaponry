using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;

namespace LazerHook.Settings
{
    [SettingRegister("CYCLOZARIN MODS", "LazerHook settings")]
    internal class RecoilForce : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => Plugin.SyncedSettings.sync_RecoilForce.SetValue(Value);

        public string GetDisplayName() => "Recoil force";

        protected override int GetDefaultValue() => 25;

        protected override (int, int) GetMinMaxValue() => new(10, 50);
    }
}
