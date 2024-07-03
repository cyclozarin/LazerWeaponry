using System.Collections;
using System.Linq;
using UnityEngine;
using MyceliumNetworking;

namespace LazerWeaponry.Hooks
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

        #region Internal fields
        internal static int HeadshotCombo = 0;

        internal static string HelmetTextString = "";
        #endregion

        #region Methods to use in hooks
        private static IEnumerator StartDelayAfterFire()
        {
            _ableToFire = false;
            yield return new WaitForSecondsRealtime(LazerWeaponryPlugin.InitialSettings.DelayAfterFire);
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

        internal static void ChangeProjectileColor(GameObject projectile, Color color)
        {
            var _projectileRenderer = projectile.GetComponentInChildren<MeshRenderer>();
            var _projectileHitRenderer = projectile.GetComponent<Projectile_SpawnObject>().objectToSpawn.GetComponent<ParticleSystemRenderer>(); // i am doin' that curse shit to change color of an INSTANCE of a bullet, not of its ORIGINAL
            var _projectileMaterial = _projectileRenderer.material;
            var _projectileHitMaterial = _projectileHitRenderer.materials[1]; // projectile hit have 2 materials but color changing works only on second material
            _projectileMaterial.color = color;
            _projectileMaterial.SetColor("_BaseMap", color);
            _projectileMaterial.SetColor("_Emission", color * 30);
            _projectileHitMaterial.color = color;
            _projectileHitMaterial.SetColor("_BaseMap", color);
            _projectileHitMaterial.SetColor("_Emission", color * 30);
            _projectileRenderer.material = _projectileMaterial;
            _projectileHitRenderer.material = _projectileHitMaterial;
        }

        private static void ChangeRescueHookBeamColor(RescueHook hook)
        {
            hook.lightObject.GetComponentInChildren<MeshRenderer>().material.color = _lazerMode ? Color.red with { a = 0.2f } : new Color(1f, 0.570f, 0f, 0.027f);
        }

        internal static void PlaySoundEffect(AudioClip clip, float volume)
        {
            _customSource.PlayOneShot(clip, volume);
        }
        #endregion

        #region MonoMod hooks
        internal static void Init()
        {
            On.RescueHook.Start += MMHook_Postfix_SetRescueHookDataOnStart;
            On.RescueHook.Update += MMHook_Postfix_ToggleRescueHookModeAndCheckForIt;
            On.RescueHook.Fire += MMHook_Prefix_LazersInRescueHook;
            On.Player.Start += MMHook_Postfix_CreateCustomSourcesAndReregisterInMycelium;
        }

        private static IEnumerator MMHook_Postfix_CreateCustomSourcesAndReregisterInMycelium(On.Player.orig_Start orig, Player self)
        {
            var _orig = orig(self);
            while (_orig.MoveNext()) { yield return _orig.Current; }
            if (!self.ai)
            {
                _chargeSource = self.GetComponent<PlayerDataSounds>().throwCharge;
                var _customSourceObject = GameObject.Find(_sourceName);
                if (GameObject.Find(_sourceName) == null)
                {
                    var _sourceObject = Object.Instantiate(new GameObject(_sourceName), self.HeadPosition(), self.refs.headPos.rotation);
                    _sourceObject.hideFlags = HideFlags.HideAndDontSave;
                    _customSource = _sourceObject.AddComponent<AudioSource>();
                    Object.DontDestroyOnLoad(_sourceObject);
                }
                else { _customSource = _customSourceObject!.GetComponent<AudioSource>(); }
                MyceliumNetwork.RegisterNetworkObject(LazerWeaponryPlugin.Instance, LazerWeaponryPlugin.MYCELIUM_ID, LazerWeaponryPlugin.LocalPhotonViewID); // before we registered plugin class in Mycelium without mask because we hadn't it, so here we got mask and re-register plugin in Mycelium with it
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
                LazerWeaponryPlugin.Logger.LogDebug("rescue hook was switched to different slot, we'll charge it");
                self.StartCoroutine(DelayAndHookRecharge(self)); 
            }
        }

        private static void MMHook_Postfix_ToggleRescueHookModeAndCheckForIt(On.RescueHook.orig_Update orig, RescueHook self)
        {
            orig(self);
            try
            {
                Player.localPlayer.TryGetInventory(out var _localPlayerInventory);
                _localPlayerInventory.TryGetItemInSlot(Player.localPlayer.data.selectedItemSlot, out var _item); // checks for disabling charge sound when switched to another slot
                _chargeSource.enabled = _item.item == self.itemInstance.item && !_firedWhileAgo;
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
            if (LazerWeaponryPlugin.InitialSettings.PVPMode)
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
                self.m_batteryEntry.AddCharge(-self.m_batteryEntry.m_maxCharge / LazerWeaponryPlugin.InitialSettings.MaxAmmo);
                MyceliumNetwork.RPC(LazerWeaponryPlugin.MYCELIUM_ID, nameof(LazerWeaponryPlugin.RPC_SpawnBullet), ReliableType.Reliable, self.dragPoint.position + (self.dragPoint.forward * 1.5f) + (Vector3.down * 0.15f) + (Vector3.left * 0.05f), Quaternion.LookRotation(self.dragPoint.forward));
                self.playerHoldingItem.CallAddForceToBodyParts([self.playerHoldingItem.refs.ragdoll.GetBodyPartID(BodypartType.Hand_R)], [-self.dragPoint.forward * LazerWeaponryPlugin.InitialSettings.RecoilForce]);
                self.StartCoroutine(StartDelayAfterFire());
                return;
            }
            orig(self);
        }
        #endregion
    }
}
