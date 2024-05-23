using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;

namespace LazerHook.Settings
{
    [SettingRegister("CYCLOZARIN MODS", "LazerHook settings")]
    internal class Damage : IntSetting, ICustomSetting
    {

        public override void ApplyValue() => Plugin.SyncedSettings.sync_Damage.SetValue(Value);

        public string GetDisplayName() => "Bullet damage";

        protected override int GetDefaultValue() => 10;

        protected override (int, int) GetMinMaxValue() => (5, 25);
    }
}
