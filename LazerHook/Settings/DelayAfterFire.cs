using ContentSettings.API.Settings;
using System;
using Unity.Mathematics;
using Zorro.Settings;

namespace LazerWeaponry.Settings
{
    internal class DelayAfterFire : FloatSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_DelayAfterFire.SetValue((float)Math.Round(Value, 2));

        public string GetDisplayName() => "Delay after firing bullet";

        public override float GetDefaultValue() => .15f;

        public override float2 GetMinMaxValue() => new(.01f, .5f);
    }
}
