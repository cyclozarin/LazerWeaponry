using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;
using System;
using Unity.Mathematics;
using Zorro.Settings;

namespace LazerHook.Settings
{
    [SettingRegister("CYCLOZARIN MODS", "LazerHook settings")]
    internal class MonsterHitForceMultiplier : FloatSetting, ICustomSetting
    {
        public override void ApplyValue() => Plugin.SyncedSettings.sync_MonsterHitForceMultiplier.SetValue((float)Math.Round(Value, 2));

        public string GetDisplayName() => "Monster force multiplier on hit";

        public override float GetDefaultValue() => 0.25f;

        public override float2 GetMinMaxValue() => new(0f, 2f);
    }
}
