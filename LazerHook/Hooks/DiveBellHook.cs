using MyceliumNetworking;
using Steamworks;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using Zorro.Core;
using Random = UnityEngine.Random;

namespace LazerHook.Hooks
{
    internal class DiveBellHook
    {
        private static bool _triedToSumbergeWhenSomeoneIsAlive = false;

        private static string _currentMessageForDiveBellNotBeingReadyBecauseSomeoneIsAlive = "";

        private static string _currentWinMessageForDiveBell = "";

        private static int _currentDiveBellIndex;

        private static string[] _oldWorldEnterMessages =
        {
            "You're alone now, with fear of sudden death and any counter of your friend.",
            "Friends that you happily met before now would battle not for live, but for death.",
            "Jokes are over, you arrived to the battlefield."
        };

        private static string[] _notReadyBecauseSomeoneIsAliveMessages =
        {
            "cannot submerge when someone is alive, dumbass!",
            "you thought you can leave when <playerCount> player(-s) is alive, huh?",
            "you need to finish remaining people first. or just survive until you will be alone.",
            "<playerCount> people is certainly alive. submerge cancelled.",
            "error 0x0000000C: cannot submerge because diving bell is being severely damaged",
            "your diveos license is expired! come back later when you will be able to afford it by killing."
        };

        private static string FillPlayerCountPlaceholder(string stringToFill)
        {
            return stringToFill.Replace("<playerCount>", (PlayerHandler.instance.playersAlive.Count - 1).ToString());
        }

        private class NotReadyBecauseSomeoneIsAliveState : DivingBellState
        {
            public override void SetStatusText(TextMeshProUGUI statusText)
            {
                statusText.text = FillPlayerCountPlaceholder(_currentMessageForDiveBellNotBeingReadyBecauseSomeoneIsAlive);
                statusText.color = RED;
            }
        }

        internal static void Init()
        {
            On.DiveBellParent.GetSpawn += MMHook_Prefix_IndividualRandomDiveBell;
            On.DivingBell.Start += MMHook_Postfix_DiveBellStuff;
            On.DivingBell.Update += MMHook_Prefix_ForceSwitchState;
            On.UseDivingBellButton.Interact += MMHook_Prefix_RestictLeavingWhenSomeoneIsAlive;
            On.DivingBellReadyState.SetStatusText += MMHook_Postfix_SetWinText;
        }

        private static void MMHook_Postfix_SetWinText(On.DivingBellReadyState.orig_SetStatusText orig, DivingBellReadyState self, TextMeshProUGUI statusText)
        {
            orig(self, statusText);
            if (!self.OnSurface && Plugin.InitialSettings.PVPMode && PlayerHandler.instance.playersAlive.Count == 1)
                statusText.text = _currentWinMessageForDiveBell;
        }

        private static void MMHook_Prefix_ForceSwitchState(On.DivingBell.orig_Update orig, DivingBell self)
        {
            if (Plugin.InitialSettings.PVPMode && !self.onSurface)
            {
                PlayerHandler.instance.players.ForEach(delegate (Player p)
                {
                    p.data.isInDiveBell = self.playerDetector.CheckForPlayers().Contains(p);
                });
                if (PlayerHandler.instance.playersAlive.Count == 1)
                {
                    self.StateMachine.SwitchState<DivingBellReadyState>();
                    return;
                }
                if (_triedToSumbergeWhenSomeoneIsAlive)
                {
                    self.StateMachine.SwitchState<NotReadyBecauseSomeoneIsAliveState>();
                    return;
                }
            }
            orig(self);
        }

        private static void MMHook_Prefix_RestictLeavingWhenSomeoneIsAlive(On.UseDivingBellButton.orig_Interact orig, UseDivingBellButton self, Player player)
        {
            if (PlayerHandler.instance.playersAlive.Count != 1 && Plugin.InitialSettings.PVPMode && !self.divingBell.onSurface)
            {
                self.hoverText = "unable to submerge";
                self.divingBell.sfx.notAll.Play(self.divingBell.transform.position);
                _triedToSumbergeWhenSomeoneIsAlive = true;
                return;
            }
            orig(self, player);
        }

        private static IEnumerator WaitForOthersAndDoStuff(DivingBell bell)
        {
            yield return new WaitUntil(() => PlayerHandler.instance.playersAlive.Contains(Player.localPlayer));
            if (MyceliumNetwork.IsHost) 
            { 
                MyceliumNetwork.SetLobbyData("diveBellArrivingMessage", _oldWorldEnterMessages[Random.Range(0, _oldWorldEnterMessages.Length)]); // lame message sync via Mycelium's LobbyData
            }
            Plugin.AddRescueHookToPlayer(Player.localPlayer);
            yield return new WaitUntil(() => PlayerHandler.instance.playersAlive.Count == MyceliumNetwork.PlayerCount);
            bell.AttemptSetOpen(true);
            MyceliumNetwork.RPC(Plugin.MYCELIUM_ID, nameof(Plugin.RPC_SpawnBellOwnerText), ReliableType.Reliable, _currentDiveBellIndex);
            HelmetText.Instance.SetHelmetText(MyceliumNetwork.GetLobbyData<string>("diveBellArrivingMessage"), 3f);
            yield return new WaitForSecondsRealtime(10f);
            HelmetText.Instance.SetHelmetText("\n<color=red>Good luck.</color>", 3f);
        }

        private static void MMHook_Postfix_DiveBellStuff(On.DivingBell.orig_Start orig, DivingBell self)
        {
            orig(self);
            if (MyceliumNetwork.InLobby)
            {
                self.StateMachine.RegisterState(new NotReadyBecauseSomeoneIsAliveState());
                if (self.onSurface)
                    self.AttemptSetOpen(true);
                if (Plugin.InitialSettings.PVPMode && !self.onSurface)
                {
                    self.StartCoroutine(WaitForOthersAndDoStuff(self));
                }
            }
        }

        private static SpawnPoint MMHook_Prefix_IndividualRandomDiveBell(On.DiveBellParent.orig_GetSpawn orig, DiveBellParent self)
        {
            if (Plugin.InitialSettings.PVPMode)
            {
                SpawnPoint _spawnPoint = null!;
                foreach (var _player in MyceliumNetwork.Players)
                {
                    var _previousState = Random.state;
                    int _randomDiveBellSeed = Guid.NewGuid().GetHashCode();
                    Random.InitState(_randomDiveBellSeed);
                    Plugin.Logger.LogDebug($"generated game seed {_randomDiveBellSeed} for spawning dive bells on {SteamFriends.GetPlayerNickname(_player)} side");
                    _currentMessageForDiveBellNotBeingReadyBecauseSomeoneIsAlive = _notReadyBecauseSomeoneIsAliveMessages[Random.Range(0, _notReadyBecauseSomeoneIsAliveMessages.Length)];
                    _currentWinMessageForDiveBell = Random.Range(1, 2) == 1 ? "you win!" : "you're safe now.";
                    _currentDiveBellIndex = Random.Range(0, self.transform.childCount);
                    for (int i = 0; i < self.transform.childCount; i++)
                    {
                        Plugin.Logger.LogDebug($"disabling dive bell #{i}");
                        self.transform.GetChild(i).gameObject.SetActive(false);
                    }
                    Transform _diveBellTransform = self.transform.GetChild(_currentDiveBellIndex);
                    Plugin.Logger.LogDebug($"selected dive bell #{_currentDiveBellIndex}");
                    MyceliumNetwork.RPC(Plugin.MYCELIUM_ID, nameof(Plugin.RPC_ToggleBell), ReliableType.Reliable, _currentDiveBellIndex, true);
                    if (SteamUser.GetSteamID() == _player)
                    {
                        _spawnPoint = _diveBellTransform.GetComponentInChildren<SpawnPoint>();
                    }
                    Plugin.Logger.LogDebug("rolling back to our previous random state");
                    Random.state = _previousState;
                }
                return _spawnPoint;
            }
            return orig(self);
        }
    }
}
