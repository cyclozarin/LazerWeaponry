global using Plugin = LazerHook.LazerWeaponry;
using BepInEx;
using BepInEx.Logging;
using LazerHook.Hooks;
using MyceliumNetworking;
using ConfigSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zorro.Core.CLI;
using UnityEngine.SceneManagement;
using MortalEnemies;

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
        }
        
        internal static GameObject Projectile = null!;

        internal const uint MYCELIUM_ID = 391309;

        internal AssetBundle Bundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("LazerHook.Bundles.sfxbundle"));

        internal static AudioClip HeadshotSound = null!;

        internal static AudioClip KillSound = null!;

        internal static int LocalPhotonViewID => Player.localPlayer.refs.view.ViewID;

        internal static bool IsOnSurface => SceneManager.GetActiveScene().name.Contains("Surface");

        private static Item _rescueHookItem = ItemDatabase.Instance.Objects.Where(item => item.displayName == "Rescue Hook").FirstOrDefault();

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            Projectile = Resources.Load<GameObject>("Dog").GetComponentInChildren<Attack_Dog>().projectile;
            RescueHookHook.ProjectileRenderer = Projectile.GetComponentInChildren<MeshRenderer>();
            RescueHookHook.ProjectileHitRenderer = Projectile.GetComponent<Projectile_SpawnObject>().objectToSpawn.GetComponent<ParticleSystemRenderer>();

            HookAll();

            HeadshotSound = Bundle.LoadAsset<AudioClip>("headshot");
            KillSound = Bundle.LoadAsset<AudioClip>("killsound");

            SyncedSettings.sync_PVPMode.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed PvP mode to {SyncedSettings.sync_PVPMode.CurrentValue}!"); };
            SyncedSettings.sync_Damage.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed damage to {SyncedSettings.sync_Damage.CurrentValue}!"); };
            SyncedSettings.sync_MaxAmmo.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed max ammo to {SyncedSettings.sync_PVPMode.CurrentValue}!"); };
            SyncedSettings.sync_DelayAfterFire.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed delay after fire to {SyncedSettings.sync_DelayAfterFire.CurrentValue}!"); };
            SyncedSettings.sync_HeadshotDamageMultiplier.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed headshot damage multiplier to {SyncedSettings.sync_HeadshotDamageMultiplier.CurrentValue}!"); };
            SyncedSettings.sync_MonsterFallTime.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed monster fall time to {SyncedSettings.sync_MonsterFallTime.CurrentValue}!"); };
            SyncedSettings.sync_MonsterHitForceMultiplier.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed monster force multiplier on hit to {SyncedSettings.sync_MonsterHitForceMultiplier.CurrentValue}!"); };
            SyncedSettings.sync_RecoilForce.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed recoil force to {SyncedSettings.sync_RecoilForce.CurrentValue}!"); };
            SyncedSettings.sync_KillReward.ConfigChanged += delegate { Logger.LogWarning($"Host dude changed kill reward to {SyncedSettings.sync_KillReward.CurrentValue}!"); };
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
        internal void RPC_PlayLocalSound(int soundNumber, float volume)
        {
            Dictionary<int, AudioClip> _numberToAudioClip = new()
            {
                [0] = HeadshotSound,
                [1] = KillSound
            };
            RescueHookHook.PlaySoundEffect(_numberToAudioClip[soundNumber], volume);
        }

        [CustomRPC]
        internal void RPC_SetLocalHelmetText()
        {
            HelmetText.Instance.m_TextObject.text = RescueHookHook.HelmetTextString;
            HelmetText.Instance.m_TextObject.SetAllDirty();
            StartCoroutine(RescueHookHook.WaitAndWipeHelmetTextAndCombo(5f));
        }

        [CustomRPC]
        internal void RPC_SetLocalKillReward(string killedPlayerNickname)
        {
            if (!IsOnSurface)
                UserInterface.ShowMoneyNotification($"Reward for killing {killedPlayerNickname}", $"${RescueHookHook.KillReward}", MoneyCellUI.MoneyCellType.HospitalBill);
            SurfaceNetworkHandler.RoomStats.AddMoney(RescueHookHook.KillReward);
        }

        [CustomRPC]
        internal void RPC_SpawnBullet(Vector3 position, Quaternion rotation, RPCInfo shooterInfo)
        {
            var _spawnedProjectileObj = Instantiate(Projectile, position, rotation); // grab dog's projectile (that we colored b4!) to fire it in lazer mode
            var _spawnedProjectile = _spawnedProjectileObj.GetComponent<Projectile>();
            _spawnedProjectile.damage = RescueHookHook.Damage;
            _spawnedProjectile.hitAction = (Action<RaycastHit>)Delegate.Combine(_spawnedProjectile.hitAction, delegate (RaycastHit hit)
            {
                var _hitPlayer = hit.collider.GetComponentInParent<Player>();
                var _hitMortality = hit.collider.GetComponentInParent<Mortality>();
                if (_hitMortality != null)
                {
                    bool _hitHead = _hitPlayer.refs.ragdoll.GetBodypartFromCollider(hit.collider).bodypartType == BodypartType.Head;
                    int _headshotDamage = (int)(RescueHookHook.Damage * RescueHookHook.HeadshotDamageMultiplier) - RescueHookHook.Damage;
                    if (_hitHead)
                    {
                        Logger.LogDebug($"hit head: {_hitHead} collider name: {hit.collider.name} headshot dmg: {_headshotDamage}");
                        RescueHookHook.HeadshotCombo++;
                        RescueHookHook.HelmetTextString = $"headshot! (x{RescueHookHook.HeadshotCombo})\n";
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_PlayLocalSound), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, 0, IsOnSurface ? 0.75f : 1f);
                        _hitMortality.Damage(_headshotDamage);
                    }
                    if (!_hitPlayer.ai && !_hitPlayer.refs.view.IsMine && !_hitPlayer.data.dead && _hitPlayer.data.health <= RescueHookHook.Damage || _hitPlayer.data.health <= (RescueHookHook.Damage + _headshotDamage))
                    {
                        string _hitPlayerNickname = _hitPlayer.refs.view.Owner.NickName;
                        RescueHookHook.HelmetTextString += $"<color=red>killed {_hitPlayerNickname}!</color>\n";
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_PlayLocalSound), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, 1, 1f);
                        MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_SetLocalKillReward), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID, _hitPlayerNickname);
                    }
                    if (_hitPlayer.ai)
                    {
                        _hitPlayer.CallTakeDamageAndAddForceAndFall(0f, hit.point * RescueHookHook.MonsterHitForceMultiplier, RescueHookHook.MonsterFallTime * (_hitHead ? 2 : 1)); // basically we implement a cheaper shock stick that works only on monsters
                        _hitMortality.Damage(RescueHookHook.Damage);
                    }
                    MyceliumNetwork.RPCTargetMasked(MYCELIUM_ID, nameof(RPC_SetLocalHelmetText), shooterInfo.SenderSteamID, ReliableType.Reliable, LocalPhotonViewID);
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
