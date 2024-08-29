using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    [SettingRegister("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings")]
    internal class MonsterKillReward : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_MonsterKillReward.SetValue(Value);

        public string GetDisplayName() => "Money reward for killing monster";

        protected override int GetDefaultValue() => 15;

        protected override (int, int) GetMinMaxValue() => new(5, 35);
    }
}
