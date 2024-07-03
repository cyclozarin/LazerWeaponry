using ContentSettings.API.Settings;

namespace LazerWeaponry.Settings
{
    internal class PlayerKillReward : IntSetting, ICustomSetting
    {
        public override void ApplyValue() => LazerWeaponryPlugin.SyncedSettings.sync_PlayerKillReward.SetValue(Value);

        public string GetDisplayName() => "Money reward for killing player";

        protected override int GetDefaultValue() => 35;

        protected override (int, int) GetMinMaxValue() => new(20, 75);
    }
}