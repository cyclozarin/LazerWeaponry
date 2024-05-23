using System.Collections;
using System.Linq;
using UnityEngine;
using MyceliumNetworking;

namespace LazerHook.Hooks
{
    public class RescueHookHook
    {
        #region Rescue hook's lazer mode fields
        private static bool _lazerMode = false;

        private static bool _ableToFire = true;

        private static bool _firedWhileAgo = false;

        private static bool _recharging = false;

        private static float _rechargeTime = 3f;

        private static AudioSource _chargeSource = null!;

        private static AudioSource _customSource = null!;

        private static string _sourceName = "LazerWeaponry source";

        private static Coroutine? _hookRechargeCoroutine = null!;
        #endregion

        #region Settings and internal fields
        internal static int Damage => (int)Plugin.SyncedSettings.sync_Damage.CurrentValue;

        internal static int MaxAmmo => (int)Plugin.SyncedSettings.sync_MaxAmmo.CurrentValue;

        internal static float DelayAfterFire => (float)Plugin.SyncedSettings.sync_DelayAfterFire.CurrentValue;

        internal static float HeadshotDamageMultiplier => (float)Plugin.SyncedSettings.sync_HeadshotDamageMultiplier.CurrentValue;

        internal static bool PVPMode => (bool)Plugin.SyncedSettings.sync_PVPMode.CurrentValue;

        internal static float MonsterFallTime => (float)Plugin.SyncedSettings.sync_MonsterFallTime.CurrentValue;

        internal static float MonsterHitForceMultiplier => (float)Plugin.SyncedSettings.sync_MonsterHitForceMultiplier.CurrentValue;

        internal static int RecoilForce => (int)Plugin.SyncedSettings.sync_RecoilForce.CurrentValue;

        internal static int KillReward => (int)Plugin.SyncedSettings.sync_KillReward.CurrentValue;

        internal static int HeadshotCombo = 0;

        internal static string HelmetTextString = "";

        internal static MeshRenderer ProjectileRenderer = null!;

        internal static ParticleSystemRenderer ProjectileHitRenderer = null!;
        #endregion

        #region Methods to use in hooks
        private static IEnumerator StartDelayAfterFire()
        {
            _ableToFire = false;
            yield return new WaitForSecondsRealtime(DelayAfterFire);
            _ableToFire = true;
        }

        private static IEnumerator DelayAndHookRecharge(RescueHook hook)
        {
            yield return new WaitForSecondsRealtime(_rechargeTime);
            _recharging = true;
            while (hook.m_batteryEntry.m_charge != hook.m_batteryEntry.m_maxCharge)
            {
                hook.m_batteryEntry.AddCharge(1f);
                _chargeSource.enabled = true;
                _chargeSource.PlayOneShot(_chargeSource.clip);
                _chargeSource.volume = Mathf.Lerp(_chargeSource.volume, 0.1f, Time.deltaTime * 10f);
                _chargeSource.pitch = Mathf.Lerp(_chargeSource.pitch, 0.25f + hook.m_batteryEntry.m_charge * 4f, Time.deltaTime * 10f);
                yield return new WaitForSecondsRealtime(0.01f);
            }
            _recharging = false;
            _chargeSource.pitch = 1f;
            _chargeSource.volume = 0.01f;
            _chargeSource.enabled = false;
            _hookRechargeCoroutine = null;
            yield break;
        }

        internal static IEnumerator WaitAndWipeHelmetTextAndCombo(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            HelmetTextString = "";
            HelmetText.Instance.m_TextObject.text = "";
            HeadshotCombo = 0;
        }

        private static void ChangeProjectileColor(Color color)
        {
            var _projectileMaterial = ProjectileRenderer.material;
            var _projectileHitMaterial = ProjectileHitRenderer.materials[1]; // we need to get M_BrightRed material and nothing else
            _projectileMaterial.color = color;
            _projectileMaterial.SetColor("_BaseMap", color);
            _projectileMaterial.SetColor("_EmissionColor", color * 30);
            _projectileHitMaterial.color = color;
            _projectileHitMaterial.SetColor("_BaseMap", color);
            _projectileHitMaterial.SetColor("_EmissionColor", color * 30);
            ProjectileRenderer.sharedMaterial = _projectileMaterial;
            ProjectileHitRenderer.sharedMaterial = _projectileHitMaterial;
        }

        private static void ChangeRescueHookBeamColor(RescueHook hook)
        {
            hook.lightObject.GetComponentInChildren<MeshRenderer>().material.color = _lazerMode ? Color.red with { a = 0.2f } : new Color(1f, 0.570f, 0f, 0.027f);
        }

        internal static void PlaySoundEffect(AudioClip clip, float volume)
        {
            Plugin.Logger.LogDebug($"playing {clip.name} at volume {volume}");
            _customSource.PlayOneShot(clip, volume);
        }
        #endregion

        #region MonoMod hooks
        internal static void Init()
        {
            On.PlayerVisor.Update += MMHook_Prefix_RecolorDogProjectileAndToggleChargeSource; // idrk why its working only when i hook update and not applyvisorcolor...
            On.RescueHook.Start += MMHook_Postfix_SetRescueHookDataOnStart;
            On.RescueHook.Update += MMHook_Postfix_ToggleRescueHookModeAndCheckForIt;
            On.RescueHook.Fire += MMHook_Prefix_LazersInRescueHook;
            On.Player.Start += MMHook_Postfix_CreateCustomAudioSourceAndReregisterInMycelium;
        }

        private static IEnumerator MMHook_Postfix_CreateCustomAudioSourceAndReregisterInMycelium(On.Player.orig_Start orig, Player self)
        {
            var _orig = orig(self);
            while (_orig.MoveNext()) { yield return _orig.Current; }
            if (!self.ai)
            {
                _chargeSource = self.GetComponent<PlayerDataSounds>().throwCharge;
                var _customSourceObject = self.refs.headPos.Find(_sourceName + "(Clone)"); // clone voodoo...
                if (_customSourceObject == null)
                {
                    var _sourceObject = Object.Instantiate(new GameObject(_sourceName), self.HeadPosition(), self.refs.headPos.rotation, self.refs.headPos.transform);
                    _sourceObject.hideFlags = HideFlags.HideAndDontSave;
                    _customSource = _sourceObject.AddComponent<AudioSource>();
                    Object.Destroy(GameObject.Find(_sourceName)); // destroy original object and keep its clone that placed in the right hierarchy
                }
                else { _customSource = _customSourceObject!.GetComponent<AudioSource>(); }
                MyceliumNetwork.RegisterNetworkObject(Plugin.Instance, Plugin.MYCELIUM_ID, Plugin.LocalPhotonViewID); // before we registered plugin class in Mycelium without mask because we hadn't it, so here we got mask and re-register plugin in Mycelium with it
            }
        }

        private static void MMHook_Postfix_SetRescueHookDataOnStart(On.RescueHook.orig_Start orig, RescueHook self)
        {
            orig(self);
            var _tooltips = self.itemInstance.item.Tooltips;
            if (!_tooltips.Any(x => x.m_key == "[R] Toggle lazer mode"))
                _tooltips.Add(new("[R] Toggle lazer mode"));
            ChangeRescueHookBeamColor(self);
            if (_recharging || self.m_batteryEntry.m_charge == 0) 
            {
                Plugin.Logger.LogDebug("rescue hook was switched to different slot, we'll charge it");
                self.StartCoroutine(DelayAndHookRecharge(self)); 
            }
        }

        private static void MMHook_Prefix_RecolorDogProjectileAndToggleChargeSource(On.PlayerVisor.orig_Update orig, PlayerVisor self)
        {
            try
            {
                _chargeSource.enabled = !_firedWhileAgo;
                if (!self.hue.IsNone && self.m_player.refs.view.IsMine)
                    ChangeProjectileColor(self.visorColor.value);
            }
            catch { } // dont yell at me when i drop rescue hook, bastard!
            orig(self);
        }

        private static void MMHook_Postfix_ToggleRescueHookModeAndCheckForIt(On.RescueHook.orig_Update orig, RescueHook self)
        {
            orig(self);
            try
            {
                if (_lazerMode) self.isPulling = false;
                if (self.isHeldByMe && self.playerHoldingItem.input.toggleCameraFlipWasPressed && !self.playerHoldingItem.HasLockedInput() && GlobalInputHandler.CanTakeInput())
                {
                    _lazerMode = !_lazerMode;
                    ChangeRescueHookBeamColor(self);
                }
            }
            catch { } // you too!
        }

        private static void MMHook_Prefix_LazersInRescueHook(On.RescueHook.orig_Fire orig, RescueHook self)
        {
            if (PVPMode)
            {
                _firedWhileAgo = true;
                if (_hookRechargeCoroutine != null)
                {
                    self.StopCoroutine(_hookRechargeCoroutine);
                }
                _hookRechargeCoroutine = self.StartCoroutine(DelayAndHookRecharge(self));
            }
            if (_lazerMode)
            {
                if (!_ableToFire) return;
                self.m_batteryEntry.AddCharge(-self.m_batteryEntry.m_maxCharge / MaxAmmo);
                MyceliumNetwork.RPC(Plugin.MYCELIUM_ID, nameof(Plugin.RPC_SpawnBullet), ReliableType.Reliable, self.dragPoint.position + (self.dragPoint.forward * 1.5f) + (Vector3.down * 0.15f) + (Vector3.left * 0.05f), Quaternion.LookRotation(self.dragPoint.forward));
                self.playerHoldingItem.CallAddForceToBodyParts([self.playerHoldingItem.refs.ragdoll.GetBodyPartID(BodypartType.Hand_R)], [-self.dragPoint.forward * RecoilForce]);
                self.StartCoroutine(StartDelayAfterFire());
                return;
            }
            orig(self);
        }
        #endregion
    }
}
