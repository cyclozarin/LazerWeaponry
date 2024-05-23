using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;
using System;
using Unity.Mathematics;
using Zorro.Settings;

namespace LazerHook.Settings
{
    [SettingRegister("CYCLOZARIN MODS", "LazerHook settings")]
    internal class HeadshotDamageMultiplier : FloatSetting, ICustomSetting
    {
        public override void ApplyValue() => Plugin.SyncedSettings.sync_HeadshotDamageMultiplier.SetValue((float)Math.Round(Value, 2));

        public string GetDisplayName() => "Damage multiplier on headshot";

        public override float GetDefaultValue() => 2f;

        public override float2 GetMinMaxValue() => new(1f, 4f);
    }
}
