using MyceliumNetworking;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LazerWeaponry.Hooks
{
    internal class DiveBellHook
    {
        private static bool _triedToSumbergeWhenSomeoneIsAlive = false;

        private static string _currentMessageForDiveBellNotBeingReadyBecauseSomeoneIsAlive = "";

        private static string _currentWinMessageForDiveBell = "you win!";

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
            "error 0x0000000C: cannot submerge because diving bell is severely damaged",
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
            On.DivingBell.TransitionGameFeel += MMHook_Postfix_GenerateDiveBellIndexes;
        }

        private static void MMHook_Postfix_SetWinText(On.DivingBellReadyState.orig_SetStatusText orig, DivingBellReadyState self, TextMeshProUGUI statusText)
        {
            orig(self, statusText);
            if (!self.OnSurface && LazerWeaponryPlugin.InitialSettings.PVPMode && PlayerHandler.instance.playersAlive.Count == 1)
                statusText.text = _currentWinMessageForDiveBell;
        }

        private static void MMHook_Prefix_ForceSwitchState(On.DivingBell.orig_Update orig, DivingBell self)
        {
            PlayerHandler.instance.players.ForEach(delegate (Player p)
            {
                p.data.isInDiveBell = self.playerDetector.CheckForPlayers().Contains(p);
            });
            if (LazerWeaponryPlugin.InitialSettings.PVPMode && !self.onSurface)
            {
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
            if (PlayerHandler.instance.playersAlive.Count != 1 && LazerWeaponryPlugin.InitialSettings.PVPMode && !self.divingBell.onSurface)
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
            yield return new WaitUntil(() => PlayerHandler.instance.playersAlive.Count == MyceliumNetwork.PlayerCount);
            bell.AttemptSetOpen(true);
            if (MyceliumNetwork.IsHost) LazerWeaponryPlugin.AddRescueHookToAll(); // we are running that just to be 100% sure that we will receive rescue hook.
            MyceliumNetwork.RPC(LazerWeaponryPlugin.MYCELIUM_ID, nameof(LazerWeaponryPlugin.RPC_SpawnBellOwnerText), ReliableType.Reliable, _currentDiveBellIndex);
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
            }
        }

        private static void MMHook_Postfix_GenerateDiveBellIndexes(On.DivingBell.orig_TransitionGameFeel orig, DivingBell self)
        {
            orig(self);
            if (MyceliumNetwork.IsHost)
            {
                var _previousState = Random.state;
                int _randomDiveBellSeed = Guid.NewGuid().GetHashCode();
                Random.InitState(_randomDiveBellSeed);
                LazerWeaponryPlugin.Logger.LogDebug($"generated seed {_randomDiveBellSeed} for generating dive bell spawn locations");
                byte[] _diveBellIndexes = new byte[MyceliumNetwork.PlayerCount];
                string _currentLevel = new List<string> { "FactoryScene", "MinesScene", "HarbourScene" }[SurfaceNetworkHandler.RoomStats.LevelToPlay];
                LazerWeaponryPlugin.Logger.LogDebug($"generating dive bell locations for {_currentLevel} on day {SurfaceNetworkHandler.RoomStats.CurrentDay}");
                for (int i = 0; i < MyceliumNetwork.PlayerCount; i++)
                {
                    int _randomDiveBellIndex = Random.Range(0, Plugin.MapNameToDiveBellCount[_currentLevel] - 1);
                    _diveBellIndexes[i] = (byte)_randomDiveBellIndex;
                    LazerWeaponryPlugin.Logger.LogDebug($"will spawn {SteamFriends.GetFriendPersonaName(MyceliumNetwork.Players[i])} at bell #{_randomDiveBellIndex}");
                }
                MyceliumNetwork.SetLobbyData("diveBellIndexes", Convert.ToBase64String(_diveBellIndexes)); // we serialize that via base64 because otherwise we will encounter InvalidCastException inside mycelium
                LazerWeaponryPlugin.Logger.LogDebug("done generating dive bell spawn locations, rolling back to previous random state");
                Random.state = _previousState;
            }
        }

        private static SpawnPoint MMHook_Prefix_IndividualRandomDiveBell(On.DiveBellParent.orig_GetSpawn orig, DiveBellParent self)
        {
            if (LazerWeaponryPlugin.InitialSettings.PVPMode)
            {
                byte[] _lobbyDiveBellIndexes = Convert.FromBase64String(MyceliumNetwork.GetLobbyData<string>("diveBellIndexes"));
                LazerWeaponryPlugin.Logger.LogDebug($"received dive bell indexes: {string.Join(" ", _lobbyDiveBellIndexes)}");
                for (int i = 0; i < self.transform.childCount; i++)
                {
                    self.transform.GetChild(i).gameObject.SetActive(false);
                }
                for (int i = 0; i < _lobbyDiveBellIndexes.Length; i++)
                {
                    self.transform.GetChild(_lobbyDiveBellIndexes[i]).gameObject.SetActive(true);
                }
                int _playerIndex = Array.IndexOf(MyceliumNetwork.Players, MyceliumNetwork.Players.First((player) => SteamFriends.GetFriendPersonaName(player) == SteamFriends.GetPersonaName()));
                _currentMessageForDiveBellNotBeingReadyBecauseSomeoneIsAlive = _notReadyBecauseSomeoneIsAliveMessages[Random.Range(0, _notReadyBecauseSomeoneIsAliveMessages.Length)];
                _currentDiveBellIndex = _lobbyDiveBellIndexes[_playerIndex];
                var _diveBell = self.transform.GetChild(_currentDiveBellIndex).gameObject.GetComponent<DivingBell>();
                self.StartCoroutine(WaitForOthersAndDoStuff(_diveBell));
                LazerWeaponryPlugin.Logger.LogDebug($"player index: {_playerIndex} bell index: {_currentDiveBellIndex}");
                return self.transform.GetChild(_currentDiveBellIndex).GetComponentInChildren<SpawnPoint>();
            }
            return orig(self);
        }
    }
}
