﻿using BepInEx.Logging;
using ContentSettings.API.Settings;

namespace LazerHook.Settings
{
    internal class HeadshotSoundVolume : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => Plugin.InitialSettings.HeadshotSoundVolume = Value / 100f; 

        public string GetDisplayName() => "Headshot sound volume";

        protected override int GetDefaultValue() => 75;

        protected override (int, int) GetMinMaxValue() => new(0, 100);
    }
}
