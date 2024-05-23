using MyceliumNetworking;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LazerHook.Hooks
{
    internal class DiveBellHook
    {
        private static bool _triedToSumbergeWhenSomeoneIsAlive = false;

        private static string _currentMessageForDiveBellNotBeingReadyBecauseSomeoneIsAlive = "";

        private static string _currentWinMessageForDiveBell = "";

        private static string[] _oldWorldEnterMessages =
        {
            "You're alone now, with fear of sudden death and any counter of your friend.",
            "Friends that you happily met before now would battle not for live, but for death.",
            "Jokes are over, you're arrived to the battlefield."
        };

        private static string[] _notReadyBecauseSomeoneIsAliveMessages =
        {
            "cannot submerge when someone is alive, dumbass!",
            "you thought you can leave when <playerCount> player(-s) is alive, huh?",
            "you need to finish remaining people first. or just survive until you will be alone.",
            "<playerCount> people is certainly alive. submerge cancelled.",
            "error 0x0000000C: cannot submerge because diving bell is being severely damaged"
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
            if (!self.OnSurface && RescueHookHook.PVPMode && PlayerHandler.instance.playersAlive.Count == 1)
                statusText.text = _currentWinMessageForDiveBell;
        }

        private static void MMHook_Prefix_ForceSwitchState(On.DivingBell.orig_Update orig, DivingBell self)
        {
            if (RescueHookHook.PVPMode && !self.onSurface)
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
            if (PlayerHandler.instance.playersAlive.Count != 1 && RescueHookHook.PVPMode && !self.divingBell.onSurface)
            {
                _triedToSumbergeWhenSomeoneIsAlive = true;
                return;
            }
            orig(self, player);
        }

        private static IEnumerator WaitForOthersAndDoStuff()
        {
            yield return new WaitUntil(() => PlayerHandler.instance.playersAlive.Contains(Player.localPlayer));
            if (MyceliumNetwork.IsHost) 
            { 
                MyceliumNetwork.SetLobbyData("diveBellArrivingMessage", _oldWorldEnterMessages[Random.Range(0, _oldWorldEnterMessages.Length)]); // lame message sync via Mycelium's LobbyData
            }
            Plugin.AddRescueHookToPlayer(Player.localPlayer);
            yield return new WaitUntil(() => PlayerHandler.instance.playersAlive.Count == MyceliumNetwork.PlayerCount);
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
                self.AttemptSetOpen(true);
                if (RescueHookHook.PVPMode && !self.onSurface)
                {
                    self.StartCoroutine(WaitForOthersAndDoStuff());
                }
            }
        }

        private static SpawnPoint MMHook_Prefix_IndividualRandomDiveBell(On.DiveBellParent.orig_GetSpawn orig, DiveBellParent self)
        {
            if (RescueHookHook.PVPMode)
            {
                SpawnPoint _spawnPoint = null!;
                foreach (var _player in MyceliumNetwork.Players)
                {
                    var _previousState = Random.state;
                    int _randomDiveBellSeed = Guid.NewGuid().GetHashCode();
                    Random.InitState(_randomDiveBellSeed);
                    Plugin.Logger.LogDebug($"generated game seed {_randomDiveBellSeed} for spawning dive bells on {Player.localPlayer.refs.view.Owner.NickName} side");
                    _currentMessageForDiveBellNotBeingReadyBecauseSomeoneIsAlive = _notReadyBecauseSomeoneIsAliveMessages[Random.Range(0, _notReadyBecauseSomeoneIsAliveMessages.Length)];
                    _currentWinMessageForDiveBell = (Random.Range(1, 2) == 1) ? "you win!" : "you're safe now.";
                    int _randomDiveBellIndex = Random.Range(0, self.transform.childCount);
                    for (int i = 0; i < self.transform.childCount; i++)
                    {
                        Plugin.Logger.LogDebug($"disabling dive bell #{i}");
                        self.transform.GetChild(i).gameObject.SetActive(false);
                    }
                    Transform _diveBellTransform = self.transform.GetChild(_randomDiveBellIndex);
                    Plugin.Logger.LogDebug($"selected dive bell #{_randomDiveBellIndex}");
                    MyceliumNetwork.RPC(Plugin.MYCELIUM_ID, nameof(Plugin.RPC_ToggleBell), ReliableType.Reliable, _randomDiveBellIndex, true);
                    if (SteamUser.GetSteamID() == _player)
                    {
                        _spawnPoint = _diveBellTransform.GetComponentInChildren<SpawnPoint>();
                    }
                    Plugin.Logger.LogDebug("rolling back to our previous random state");
                }
                return _spawnPoint;
            }
            return orig(self);
        }
    }
}
