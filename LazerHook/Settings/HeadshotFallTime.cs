using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;
using System;
using Unity.Mathematics;
using Zorro.Settings;

namespace LazerWeaponry.Settings
{
    [SettingRegister("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings")]
    internal class HeadshotFallTime : FloatSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_HeadshotFallTime.SetValue((float)Math.Round(Value, 2));

        public string GetDisplayName() => "Player fall time upon headshot";

        public override float GetDefaultValue() => 1.5f;

        public override float2 GetMinMaxValue() => new(0f, 3f);
    }
}
