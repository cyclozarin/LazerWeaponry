using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;
using System;
using Unity.Mathematics;
using Zorro.Settings;

namespace LazerHook.Settings
{
    [SettingRegister("CYCLOZARIN MODS", "LazerHook settings")]
    internal class MonsterFallTime : FloatSetting, ICustomSetting
    {
        public override void ApplyValue() => Plugin.SyncedSettings.sync_MonsterFallTime.SetValue((float)Math.Round(Value, 2));

        public string GetDisplayName() => "Monster fall time on hit";

        public override float GetDefaultValue() => 2f;

        public override float2 GetMinMaxValue() => new(0f, 5f);
    }
}
