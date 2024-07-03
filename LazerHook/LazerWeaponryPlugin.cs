global using Plugin = LazerWeaponry.LazerWeaponryPlugin;
using BepInEx;
using BepInEx.Logging;
using LazerWeaponry.Hooks;
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
using LazerWeaponry.Settings;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;

namespace LazerWeaponry
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION), ContentWarningPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION, false)]
    [BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
    [BepInDependency(ContentSettings.MyPluginInfo.PLUGIN_GUID)]
    [BepInDependency(ConfigSync.MyPluginInfo.PLUGIN_GUID)]
    [BepInDependency(MortalEnemies.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class LazerWeaponryPlugin : BaseUnityPlugin
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
            internal static int PlayerKillReward => (int)SyncedSettings.sync_PlayerKillReward.CurrentValue;
            internal static int MonsterKillReward => (int)SyncedSettings.sync_MonsterKillReward.CurrentValue;
            internal static bool VulnerableEnemies => (bool)SyncedSettings.sync_VulnerableEnemies.CurrentValue;
            internal static float HeadshotFallTime => (float)SyncedSettings.sync_HeadshotFallTime.CurrentValue;

            internal static float HeadshotSoundVolume = .75f;

            internal static float KillSoundVolume = 1f;
        }

        internal static class SyncedSettings 
        {
            internal static Configuration sync_PVPMode = new(nameof(LazerWeaponryPlugin), "LW_PvPMode", false);
            internal static Configuration sync_Damage = new(nameof(LazerWeaponryPlugin), "LW_Damage", 10);
            internal static Configuration sync_MaxAmmo = new(nameof(LazerWeaponryPlugin), "LW_MaxAmmo", 10);
            internal static Configuration sync_DelayAfterFire = new(nameof(LazerWeaponryPlugin), "LW_DelayAfterFire", .1f);
            internal static Configuration sync_HeadshotDamageMultiplier = new(nameof(LazerWeaponryPlugin), "LW_HeadshotDamageMultiplier", 2f);
            internal static Configuration sync_MonsterFallTime = new(nameof(LazerWeaponryPlugin), "LW_MonsterFallTime", 2f);
            internal static Configuration sync_MonsterHitForceMultiplier = new(nameof(LazerWeaponryPlugin), "LW_MonsterHitForceMultiplier", .25f);
            internal static Configuration sync_RecoilForce = new(nameof(LazerWeaponryPlugin), "LW_RecoilForce", 25);
            internal static Configuration sync_PlayerKillReward = new(nameof(LazerWeaponryPlugin), "LW_PlayerKillReward", 35);
            internal static Configuration sync_MonsterKillReward = new(nameof(LazerWeaponryPlugin), "LW_MonsterKillReward", 15);
            internal static Configuration sync_HeadshotFallTime = new(nameof(LazerWeaponryPlugin), "LW_HeadshotFallTime", 1.5f);
            internal static Configuration sync_VulnerableEnemies = new(nameof(LazerWeaponryPlugin), "LW_VulnerableEnemies", false);
        }
        
        internal static GameObject Projectile = null!;

        internal const uint MYCELIUM_ID = 391309;

        internal AssetBundle Bundle = null!;

        internal AudioClip HeadshotSound = null!;

        internal AudioClip KillSound = null!;

        internal static int LocalPhotonViewID => Player.localPlayer.refs.view.ViewID;

        internal static bool IsOnSurface => SceneManager.GetActiveScene().name.Contains("Surface");

        internal static Dictionary<string, int> MapNameToDiveBellCount = new()
        {
            { "FactoryScene", 20 },
            { "HarbourScene", 8 },
            { "MinesScene", 7 }
        };

        internal static bool MortalityLoaded => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(MortalEnemies.MyPluginInfo.PLUGIN_GUID);

        private enum SoundType { Headshot, Kill };

        private static Item _rescueHookItem = null!;

        private int _totalRewardForAllKills;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            if (!MortalityLoaded)
                Logger.LogWarning("MortalEnemies is not loaded. Vulnerable enemies setting won't work.");

            HookAll();

            RegisterSettings();
            RegisterSyncedSettings();
           
            MyceliumNetwork.RegisterNetworkObject(this, MYCELIUM_ID);
            MyceliumNetwork.RegisterLobbyDataKey("diveBellArrivingMessage");
            MyceliumNetwork.RegisterLobbyDataKey("diveBellIndexes");

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} by {MyPluginInfo.PLUGIN_GUID.Split(".")[0]} has loaded!");
        }

        private void Start()
        {
            Logger.LogInfo("Assigning all important stuff...");

            Projectile = Resources.Load<GameObject>("Dog").GetComponentInChildren<Attack_Dog>().projectile;
            Bundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("LazerWeaponry.Bundles.sfxbundle"));
            HeadshotSound = Bundle.LoadAsset<AudioClip>("headshot");
            KillSound = Bundle.LoadAsset<AudioClip>("killsound");
            _rescueHookItem = ItemDatabase.Instance.Objects.Where(item => item.displayName == "Rescue Hook").FirstOrDefault();
            On.Player.Start += MMHook_Postfix_AddKillingRevenue;

            Logger.LogInfo("Done!");
        }

        private static void HookAll()
        {
            Logger.LogInfo("Hooking...");

            RescueHookHook.Init();
            DiveBellHook.Init();

            Logger.LogInfo("Done!");
        }

        private IEnumerator MMHook_Postfix_AddKillingRevenue(On.Player.orig_Start orig, Player self)
        {
            var _orig = orig(self);
            while (_orig.MoveNext()) { yield return _orig.Current; }

            if (IsOnSurface && _totalRewardForAllKills > 0)
            {
                UserInterface.ShowMoneyNotification($"Killing revenue", $"${_totalRewardForAllKills}", MoneyCellUI.MoneyCellType.Revenue);
                SurfaceNetworkHandler.RoomStats.AddMoney(_totalRewardForAllKills);
                _totalRewardForAllKills = 0;
            }
        }

        private void RegisterSettings()
        {
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new Damage());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new MaxAmmo());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new RecoilForce());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new DelayAfterFire());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new HeadshotDamageMultiplier());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new HeadshotFallTime());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new PlayerKillReward());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Rescue hook's lazer mode settings", new MonsterKillReward());

            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Sound settings", new HeadshotSoundVolume());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Sound settings", new KillSoundVolume());

            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Monster behaviour on hit", new MonsterFallTime());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "Monster behaviour on hit", new MonsterHitForceMultiplier());

            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "PvP settings", new PVPMode());
            SettingsLoader.RegisterSetting("<size=75%>LAZERWEAPONRY SETTINGS</size>", "PvP settings", new VulnerableEnemies());
        }

        private void RegisterSyncedSettings()
        {
            SyncedSettings.sync_PVPMode.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed PvP mode to {SyncedSettings.sync_PVPMode.CurrentValue}!"); };
            SyncedSettings.sync_Damage.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed damage to {SyncedSettings.sync_Damage.CurrentValue}!"); };
            SyncedSettings.sync_MaxAmmo.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed max ammo to {SyncedSettings.sync_PVPMode.CurrentValue}!"); };
            SyncedSettings.sync_DelayAfterFire.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed delay after fire to {SyncedSettings.sync_DelayAfterFire.CurrentValue}!"); };
            SyncedSettings.sync_HeadshotDamageMultiplier.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed headshot damage multiplier to {SyncedSettings.sync_HeadshotDamageMultiplier.CurrentValue}!"); };
            SyncedSettings.sync_MonsterFallTime.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed monster fall time to {SyncedSettings.sync_MonsterFallTime.CurrentValue}!"); };
            SyncedSettings.sync_MonsterHitForceMultiplier.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed monster force multiplier on hit to {SyncedSettings.sync_MonsterHitForceMultiplier.CurrentValue}!"); };
            SyncedSettings.sync_RecoilForce.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed recoil force to {SyncedSettings.sync_RecoilForce.CurrentValue}!"); };
            SyncedSettings.sync_PlayerKillReward.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed player kill reward to {SyncedSettings.sync_PlayerKillReward.CurrentValue}!"); };
            SyncedSettings.sync_MonsterKillReward.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed monster kill reward to {SyncedSettings.sync_MonsterKillReward.CurrentValue}!"); };
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

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void HandleDamageToMortality(Collider mortalityCollider, float damage)
        {
            var _mortality = mortalityCollider.GetComponentInParent<Mortality>();
            _mortality.Damage(damage);
        }

        internal static void AddRescueHookToPlayer(Player player)
        {
            if (player.TryGetInventory(out var _inventory) && !_inventory.TryGetSlotWithItem(_rescueHookItem, out _) && _inventory.TryAddItem(new(_rescueHookItem, new(Guid.NewGuid())), out var _slot))
            {
                Logger.LogInfo($"Added rescue hook to {player.photonView.Owner.NickName}");
                player.refs.view.RPC("RPC_SelectSlot", player.photonView.Owner, _slot.SlotID);
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
            _bellOwnerText.text = $"{Player.localPlayer.photonView.Owner.NickName}'s bell";
            _bellOwnerText.color = Player.localPlayer.refs.visor.visorColor.value;
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
            AudioClip _clip = (SoundType)soundNumber switch
            {
                SoundType.Headshot => HeadshotSound,
                SoundType.Kill => KillSound
            };
            RescueHookHook.PlaySoundEffect(_clip, volume);
        }

        [CustomRPC]
        internal void RPC_ApplyLocalHelmetText()
        {
            HelmetText.Instance.m_TextObject.text = RescueHookHook.HelmetTextString;
            HelmetText.Instance.m_TextObject.SetAllDirty();
            StartCoroutine(RescueHookHook.WaitAndWipeHelmetTextAndCombo(5f));
        }

        [CustomRPC]
        internal void RPC_AppendLocalKillReward(string killedObjectName, int reward)
        {
            if (!IsOnSurface)
            {
                UserInterface.ShowMoneyNotification($"Reward for killing {killedObjectName}", $"${reward}", MoneyCellUI.MoneyCellType.Revenue);
                _totalRewardForAllKills += reward;
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
                if (_hitPlayer != null)
                {
                    string _hitPlayerGOName = hit.collider.transform.root.name.Replace("(Clone)", string.Empty).Replace("Toolkit_", string.Empty).Replace("2", string.Empty);
                    string _hitPlayerNickname = _hitPlayer.photonView.Owner.NickName;
                    bool _hitHead = _hitPlayer.refs.ragdoll.GetBodypartFromCollider(hit.collider).bodypartType == BodypartType.Head;
                    int _headshotDamage = (int)(InitialSettings.Damage * InitialSettings.HeadshotDamageMultiplier) - InitialSettings.Damage;
                    if (_hitHead && !_hitPlayer.data.dead)
                    {
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_SetLocalHelmetText), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, $"headshot! (x<insertCombo>)\n", false, true);
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_PlayLocalSound), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, (int)SoundType.Headshot, InitialSettings.HeadshotSoundVolume);
                        if (!_hitPlayer.ai)
                            _hitPlayer.CallTakeDamageAndTase(MortalityLoaded ? 0f : _headshotDamage, InitialSettings.HeadshotFallTime); // we dont deal any damage here because we deal it later on
                        if (MortalityLoaded)
                            HandleDamageToMortality(hit.collider, _headshotDamage);
                    }
                    if (_hitPlayer.ai)
                    {
                        _hitPlayer.CallTakeDamageAndAddForceAndFall(0f, hit.point * InitialSettings.MonsterHitForceMultiplier, InitialSettings.MonsterFallTime * (_hitHead ? 2 : 1)); // basically we implement a cheaper shock stick that works only on monsters
                        if (InitialSettings.VulnerableEnemies && MortalityLoaded)
                            HandleDamageToMortality(hit.collider, InitialSettings.Damage);
                    }
                    if (_hitPlayer.data.sinceDied == 1000 && _hitPlayer.data.dead)
                    {
                        Bot? _bot = null;
                        if (!_hitPlayer.ai)
                        {
                            string _hitPlayerHexColor = ColorUtility.ToHtmlStringRGB(_hitPlayer.refs.visor.visorColor.value);
                            MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_SetLocalHelmetText), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, $"<color=red>killed</color> <color=#{_hitPlayerHexColor}>{_hitPlayerNickname}!</color>", true, false);
                        }
                        else
                        {
                            _bot = _hitPlayer.GetComponentInChildren<Bot>();
                            string _monsterDangerLevelColor = string.Empty;
                            switch (_bot.jumpScareLevel)
                            {
                                case 1:
                                    _monsterDangerLevelColor = "white";
                                    break;
                                case 0: // assuming that bot who have 0 jumpscare lvl is either a streamer or a infiltrator, which can't be killed easily and can be considered as mid-tier enemy
                                case 2:
                                    _monsterDangerLevelColor = "yellow";
                                    break;
                                case 3:
                                    _monsterDangerLevelColor = "red";
                                    break;
                            }
                            MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_SetLocalHelmetText), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, $"<color=red>killed</color> <color={_monsterDangerLevelColor}>{_hitPlayerGOName}!</color>", true, false);
                        }
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_PlayLocalSound), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, (int)SoundType.Kill, InitialSettings.KillSoundVolume);
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_AppendLocalKillReward), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, _hitPlayer.ai ? _hitPlayerGOName : _hitPlayerNickname, _hitPlayer.ai ? InitialSettings.MonsterKillReward * (_bot?.jumpScareLevel == 0 ? 2 : _bot?.jumpScareLevel) : InitialSettings.PlayerKillReward);
                    }
                    MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_ApplyLocalHelmetText), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID);
                }
            });
        }

        [ConsoleCommand]
        public static void GiveEveryoneRescueHook()
        {
            if (MyceliumNetwork.IsHost && !MainMenuHandler.SteamLobbyHandler.IsPlayingWithRandoms()) // fuck photon, all my homies use mycelium!
                AddRescueHookToAll();
        }
    }
}
