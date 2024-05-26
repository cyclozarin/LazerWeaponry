global using Plugin = LazerHook.LazerWeaponry;
using BepInEx;
using BepInEx.Logging;
using LazerHook.Hooks;
using MyceliumNetworking;
using ConfigSync;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zorro.Core.CLI;
using UnityEngine.SceneManagement;
using MortalEnemies;
using TMPro;
using Steamworks;
using ContentSettings.API;
using LazerHook.Settings;

namespace LazerHook
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION), ContentWarningPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION, false)]
    [BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
    [BepInDependency(ContentSettings.MyPluginInfo.PLUGIN_GUID)]
    [BepInDependency(ConfigSync.MyPluginInfo.PLUGIN_GUID)]
    public class LazerWeaponry : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;

        internal new static ManualLogSource Logger { get; private set; } = null!;

        internal static class InitialSettings
        {
            internal static int Damage => (int)SyncedSettings.sync_Damage.CurrentValue;
            internal static int MaxAmmo => (int)SyncedSettings.sync_MaxAmmo.CurrentValue;
            internal static float DelayAfterFire => (float)SyncedSettings.sync_DelayAfterFire.CurrentValue;
            internal static float HeadshotDamageMultiplier => (float)SyncedSettings.sync_HeadshotDamageMultiplier.CurrentValue;
            internal static bool PVPMode => (bool)SyncedSettings.sync_PVPMode.CurrentValue;
            internal static float MonsterFallTime => (float)SyncedSettings.sync_MonsterFallTime.CurrentValue;
            internal static float MonsterHitForceMultiplier => (float)SyncedSettings.sync_MonsterHitForceMultiplier.CurrentValue;
            internal static int RecoilForce => (int)SyncedSettings.sync_RecoilForce.CurrentValue;
            internal static int KillReward => (int)SyncedSettings.sync_KillReward.CurrentValue;
            internal static bool VulnerableEnemies => (bool)SyncedSettings.sync_VulnerableEnemies.CurrentValue;
            internal static float HeadshotFallTime => (float)SyncedSettings.sync_HeadshotFallTime.CurrentValue;
            internal static float HeadshotSoundVolume = .75f;
            internal static float KillSoundVolume = 1f;
        }

        internal static class SyncedSettings 
        {
            internal static Configuration sync_PVPMode = new(nameof(LazerWeaponry), "LW_PvPMode", false);
            internal static Configuration sync_Damage = new(nameof(LazerWeaponry), "LW_Damage", 10);
            internal static Configuration sync_MaxAmmo = new(nameof(LazerWeaponry), "LW_MaxAmmo", 10);
            internal static Configuration sync_DelayAfterFire = new(nameof(LazerWeaponry), "LW_DelayAfterFire", .1f);
            internal static Configuration sync_HeadshotDamageMultiplier = new(nameof(LazerWeaponry), "LW_HeadshotDamageMultiplier", 2f);
            internal static Configuration sync_MonsterFallTime = new(nameof(LazerWeaponry), "LW_MonsterFallTime", 2f);
            internal static Configuration sync_MonsterHitForceMultiplier = new(nameof(LazerWeaponry), "LW_MonsterHitForceMultiplier", .25f);
            internal static Configuration sync_RecoilForce = new(nameof(LazerWeaponry), "LW_RecoilForce", 25);
            internal static Configuration sync_KillReward = new(nameof(LazerWeaponry), "LW_KillReward", 35);
            internal static Configuration sync_HeadshotFallTime = new(nameof(LazerWeaponry), "LW_HeadshotFallTime", 1.5f);
            internal static Configuration sync_VulnerableEnemies = new(nameof(LazerWeaponry), "LW_VulnerableEnemies", false);
        }
        
        internal static GameObject Projectile = null!;

        internal const uint MYCELIUM_ID = 391309;

        internal AssetBundle Bundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("LazerHook.Bundles.sfxbundle"));

        internal static AudioClip HeadshotSound = null!;

        internal static AudioClip KillSound = null!;

        internal static int LocalPhotonViewID => Player.localPlayer.refs.view.ViewID;

        internal static bool IsOnSurface => SceneManager.GetActiveScene().name.Contains("Surface");

        private enum SoundType { Headshot, Kill };

        private static readonly Item _rescueHookItem = ItemDatabase.Instance.Objects.Where(item => item.displayName == "Rescue Hook").FirstOrDefault();

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            Projectile = Resources.Load<GameObject>("Dog").GetComponentInChildren<Attack_Dog>().projectile;

            HeadshotSound = Bundle.LoadAsset<AudioClip>("headshot");
            KillSound = Bundle.LoadAsset<AudioClip>("killsound");

            HookAll();

            RegisterSettings();
            RegisterSyncedSettings();
           
            MyceliumNetwork.RegisterNetworkObject(this, MYCELIUM_ID);
            MyceliumNetwork.RegisterLobbyDataKey("diveBellArrivingMessage");

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} by {MyPluginInfo.PLUGIN_GUID.Split(".")[0]} has loaded!");
        }

        private static void HookAll()
        {
            Logger.LogInfo("Hooking...");

            RescueHookHook.Init();
            DiveBellHook.Init();

            Logger.LogInfo("Finished hooking!");
        }

        private static void RegisterSettings()
        {
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new Damage());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new MaxAmmo());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new RecoilForce());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new DelayAfterFire());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new HeadshotDamageMultiplier());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new HeadshotFallTime());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new KillReward());

            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Sound settings", new HeadshotSoundVolume());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Sound settings", new KillSoundVolume());

            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Monster behaviour on hit", new MonsterFallTime());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Monster behaviour on hit", new MonsterHitForceMultiplier());

            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "PvP settings", new PVPMode());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "PvP settings", new VulnerableEnemies());
        }

        private static void RegisterSyncedSettings()
        {
            SyncedSettings.sync_PVPMode.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed PvP mode to {SyncedSettings.sync_PVPMode.CurrentValue}!"); };
            SyncedSettings.sync_Damage.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed damage to {SyncedSettings.sync_Damage.CurrentValue}!"); };
            SyncedSettings.sync_MaxAmmo.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed max ammo to {SyncedSettings.sync_PVPMode.CurrentValue}!"); };
            SyncedSettings.sync_DelayAfterFire.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed delay after fire to {SyncedSettings.sync_DelayAfterFire.CurrentValue}!"); };
            SyncedSettings.sync_HeadshotDamageMultiplier.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed headshot damage multiplier to {SyncedSettings.sync_HeadshotDamageMultiplier.CurrentValue}!"); };
            SyncedSettings.sync_MonsterFallTime.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed monster fall time to {SyncedSettings.sync_MonsterFallTime.CurrentValue}!"); };
            SyncedSettings.sync_MonsterHitForceMultiplier.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed monster force multiplier on hit to {SyncedSettings.sync_MonsterHitForceMultiplier.CurrentValue}!"); };
            SyncedSettings.sync_RecoilForce.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed recoil force to {SyncedSettings.sync_RecoilForce.CurrentValue}!"); };
            SyncedSettings.sync_KillReward.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed kill reward to {SyncedSettings.sync_KillReward.CurrentValue}!"); };
            SyncedSettings.sync_VulnerableEnemies.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed vulnerable enemies to {SyncedSettings.sync_VulnerableEnemies.CurrentValue}!"); };
            SyncedSettings.sync_HeadshotFallTime.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed headshot fall time to {SyncedSettings.sync_HeadshotFallTime.CurrentValue}!"); };
        }

        private CSteamID GetSteamIdFromString(string rawSteamId)
        {
            if (ulong.TryParse(rawSteamId, out ulong result))
            {
                return new CSteamID(result);
            }
            return default;
        }

        internal static void AddRescueHookToPlayer(Player player)
        {
            if (player.TryGetInventory(out var _inventory) && !_inventory.TryGetSlotWithItem(_rescueHookItem, out _) && _inventory.TryAddItem(new(_rescueHookItem, new(Guid.NewGuid())), out var _slot))
            {
                Logger.LogInfo($"Added rescue hook to {player.refs.view.Owner.NickName}");
                player.refs.view.RPC("RPC_SelectSlot", player.refs.view.Owner, _slot.SlotID);
            }
        }

        internal static void AddRescueHookToAll()
        {
            foreach (Player _player in PlayerHandler.instance.playersAlive)
            {
                AddRescueHookToPlayer(_player);
            }
        }

        [CustomRPC]
        internal void RPC_ToggleBell(int bellIndex, bool toggle)
        {
            DiveBellParent.instance.transform.GetChild(bellIndex).gameObject.SetActive(toggle);
        }

        [CustomRPC]
        internal void RPC_SpawnBellOwnerText(int bellIndex)
        {
            Transform _diveBellTransform = DiveBellParent.instance.transform.GetChild(bellIndex);
            GameObject _activeText = _diveBellTransform.Find("Tools/Screen/ScreenPivot/Canvas/ACTIVE").gameObject;
            GameObject _bellOwnerTextObj = Instantiate(_activeText, _activeText.transform.position, new Quaternion(0, 0, 0, 0), _activeText.transform.parent);
            _bellOwnerTextObj.transform.localPosition = _bellOwnerTextObj.transform.localPosition with { x = 0 };
            Destroy(_bellOwnerTextObj.GetComponent<UnityEngine.Localization.PropertyVariants.GameObjectLocalizer>()); // gtfo, we dont need localizations
            TextMeshProUGUI _bellOwnerText = _bellOwnerTextObj.GetComponent<TextMeshProUGUI>();
            Player _playerInBell = _diveBellTransform.Find("Detector").GetComponent<DiveBellPlayerDetector>().CheckForPlayers().First();
            _bellOwnerText.text = $"{_playerInBell.refs.view.Owner.NickName}'s bell";
            _bellOwnerText.color = _playerInBell.refs.visor.visorColor.value;
        }

        [CustomRPC]
        internal void RPC_SetLocalHelmetText(string stringToAdd, bool append, bool addCombo)
        {
            if (addCombo)
                RescueHookHook.HeadshotCombo++;
            if (append)
                RescueHookHook.HelmetTextString += stringToAdd.Replace("<insertCombo>", RescueHookHook.HeadshotCombo.ToString());
            else
                RescueHookHook.HelmetTextString = stringToAdd.Replace("<insertCombo>", RescueHookHook.HeadshotCombo.ToString());
        }

        [CustomRPC]
        internal void RPC_PlayLocalSound(int soundNumber, float volume)
        {
            SoundType _soundType = (SoundType)soundNumber;
            Logger.LogDebug($"sound number: {soundNumber} volume: {volume}");
            switch (_soundType)
            {
                case SoundType.Headshot:
                    RescueHookHook.PlaySoundEffect(HeadshotSound, volume);
                    break;
                case SoundType.Kill:
                    RescueHookHook.PlaySoundEffect(KillSound, volume);
                    break;
            }
        }

        [CustomRPC]
        internal void RPC_ApplyLocalHelmetText()
        {
            HelmetText.Instance.m_TextObject.text = RescueHookHook.HelmetTextString;
            HelmetText.Instance.m_TextObject.SetAllDirty();
            StartCoroutine(RescueHookHook.WaitAndWipeHelmetTextAndCombo(5f));
        }

        [CustomRPC]
        internal void RPC_SetLocalKillReward(string killedPlayerNickname)
        {
            if (!IsOnSurface)
            {
                UserInterface.ShowMoneyNotification($"Reward for killing {killedPlayerNickname}", $"${InitialSettings.KillReward}", MoneyCellUI.MoneyCellType.HospitalBill);
                SurfaceNetworkHandler.RoomStats.AddMoney(InitialSettings.KillReward);
            }
        }

        [CustomRPC]
        internal void RPC_SpawnBullet(Vector3 position, Quaternion rotation, RPCInfo shooterInfo)
        {
            var _spawnedProjectileObj = Instantiate(Projectile, position, rotation);
            var _spawnedProjectile = _spawnedProjectileObj.GetComponent<Projectile>();
            Player _shooterPlayer = PlayerHandler.instance.players.First((player) => (CSteamID)ulong.Parse((string)player.refs.view.Owner.CustomProperties["SteamID"]) == shooterInfo.SenderSteamID);
            RescueHookHook.ChangeProjectileColor(_spawnedProjectileObj, _shooterPlayer.refs.visor.visorColor.value);
            _spawnedProjectile.damage = InitialSettings.Damage;
            _spawnedProjectile.hitAction = (Action<RaycastHit>)Delegate.Combine(_spawnedProjectile.hitAction, delegate (RaycastHit hit)
            {
                var _hitPlayer = hit.collider.GetComponentInParent<Player>();
                var _hitMortality = hit.collider.GetComponentInParent<Mortality>();
                Logger.LogDebug($"player null: {_hitPlayer == null} mortality null: {_hitMortality == null}");
                if (_hitPlayer != null)
                {
                    bool _hitHead = _hitPlayer.refs.ragdoll.GetBodypartFromCollider(hit.collider).bodypartType == BodypartType.Head;
                    int _headshotDamage = (int)(InitialSettings.Damage * InitialSettings.HeadshotDamageMultiplier) - InitialSettings.Damage;
                    if (_hitHead && !_hitPlayer.data.dead)
                    {
                        Logger.LogDebug($"hit head: {_hitHead} collider name: {hit.collider.name} headshot dmg: {_headshotDamage}");
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_SetLocalHelmetText), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, $"headshot! (x<insertCombo>)\n", false, true);
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_PlayLocalSound), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, (int)SoundType.Headshot, InitialSettings.HeadshotSoundVolume);
                        if (!_hitPlayer.ai)
                            _hitPlayer.CallTakeDamageAndTase(0f, InitialSettings.HeadshotFallTime); // we dont deal any damage here because we deal it later on
                        _hitMortality!.Damage(_headshotDamage);
                    }
                    if (!_hitPlayer.ai && !_hitPlayer.refs.view.IsMine && _hitPlayer.data.sinceDied <= 0.5f && (_hitPlayer.data.health <= InitialSettings.Damage || _hitPlayer.data.health <= (InitialSettings.Damage + _headshotDamage)) && _hitHead)
                    {
                        Logger.LogDebug(_hitPlayer.ai ? "why the fuck are you calling???" : "ok sanity check completed");
                        string _hitPlayerNickname = _hitPlayer.refs.view.Owner.NickName;
                        string _hitPlayerHexColor = ColorUtility.ToHtmlStringRGB(_hitPlayer.refs.visor.visorColor.value);
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_SetLocalHelmetText), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, $"<color=red>killed</color> <color=#{_hitPlayerHexColor}>{_hitPlayerNickname}!</color>", true, false);
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_PlayLocalSound), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, (int)SoundType.Kill, InitialSettings.KillSoundVolume);
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_SetLocalKillReward), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, _hitPlayerNickname);
                    }
                    if (_hitPlayer.ai)
                    {
                        _hitPlayer.CallTakeDamageAndAddForceAndFall(0f, hit.point * InitialSettings.MonsterHitForceMultiplier, InitialSettings.MonsterFallTime * (_hitHead ? 2 : 1)); // basically we implement a cheaper shock stick that works only on monsters
                        if (InitialSettings.VulnerableEnemies)
                            _hitMortality!.Damage(_headshotDamage);
                    }
                    MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_ApplyLocalHelmetText), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID);
                }
            });
        }

        [ConsoleCommand]
        public static void GiveEveryoneRescueHook()
        {
            if (MyceliumNetwork.IsHost) // fuck photon, all my homies use mycelium!
                AddRescueHookToAll();
        }
    }
}
