using Rocket.API;
using Rocket.Core.Commands;
using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using Rocket.Unturned.Events;
using Rocket.Core.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SDG.Unturned;
using Rocket.API.Collections;
using Rocket.Unturned.Chat;
using UnityEngine;
using Steamworks;
using Rocket.Unturned;
using Math = System.Math;

namespace NightFish.Emergency
{
    public class Emergency : RocketPlugin<RocketEmergencyConfiguration>
    {
        private readonly List<CSteamID> silent = new List<CSteamID>();
        private readonly Dictionary<CSteamID, DateTime> emergencies = new Dictionary<CSteamID, DateTime>();
        private readonly DateTime lastCheck = DateTime.Now;

        protected override void Load()
        {
            U.Events.OnPlayerDisconnected += (UnturnedPlayer player) => {
                if (silent.Contains(player.CSteamID)) silent.Remove(player.CSteamID);
            };
        }

        protected override void Unload()
        {
            
        }

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList(){
                    { "emergency", "[911 Emergency]: {0} Has requested help: \"{1}\"" },
                    { "emergencycoords", "[911 Emergency at X:{2} Y:{3} Z:{4}]: {0} Has requested help: \"{1}\""},
                    { "pos", "Your position is: X:{0} Y:{1} Z:{2}"},
                    { "mute", "You are now set to NOT receive 911 calls"},
                    { "unmute", "You are now set to receive 911 calls"},
                    { "respond", "Ten four, {0} is responding to the emergency for {1}."},
                    { "noplayer", "Nobody with that name is online!" },
                    { "noemergency", "{0} is not in an emergency!"}
                };
            }
        }

        public void FixedUpdate()
        {
            if((DateTime.Now - lastCheck).TotalSeconds > 10)
            {
                try
                {
                    List<CSteamID> toDel = new List<CSteamID>();

                    foreach (KeyValuePair<CSteamID, DateTime> kvp in emergencies)
                    {
                        if ((DateTime.Now - kvp.Value).TotalMinutes > 10)
                        {
                            toDel.Add(kvp.Key);
                        }
                    }

                    foreach (CSteamID del in toDel)
                    {
                        emergencies.Remove(del);
                    }
                }
                catch { }
            }
        }

        [RocketCommandAlias("pos")]
        [RocketCommand("position", "Returns current location in co-ordinates", "/pos", AllowedCaller.Player)]
        public void ExecuteCommandPos(IRocketPlayer caller, string[] _)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            string x = Math.Round(player.Position.x).ToString();
            string y = Math.Round(player.Position.y).ToString();
            string z = Math.Round(player.Position.z).ToString();
            UnturnedChat.Say(player, Translate("pos", x, y, z), Color.cyan);
        }

        [RocketCommandPermission("emergency.mute")]
        [RocketCommandAlias("unmute")]
        [RocketCommand("mute", "Mutes/Unmutes the receiving of 911 messages.", "/mute", AllowedCaller.Player)]
        public void ExecuteCommandMute(IRocketPlayer caller, string[] _)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (silent.Contains(player.CSteamID))
            {
                silent.Remove(player.CSteamID);
                UnturnedChat.Say(player, Translate("unmute"), Color.yellow);
            }
            else
            {
                silent.Add(player.CSteamID);
                UnturnedChat.Say(player, Translate("mute"), Color.yellow);
            }
        }

        [RocketCommandPermission("emergency.respond")]
        [RocketCommand("respond", "Responds to a 911 emergency", "/respond <player>", AllowedCaller.Player)]
        public void ExecuteCommandRespond(IRocketPlayer caller, string[] parameters)
        {
            if (parameters.Length < 1)
            {
                UnturnedChat.Say(caller, "Usage: /respond <player>", Color.yellow);
                return;
            }

            UnturnedPlayer player = (UnturnedPlayer)caller;
            UnturnedPlayer toRescue = UnturnedPlayer.FromName(string.Join(" ", parameters));
            if(toRescue == null)
            {
                UnturnedChat.Say(caller, Translate("noplayer"), Color.yellow);
                return;
            } 

            if(emergencies.ContainsKey(toRescue.CSteamID))
            {
                emergencies.Remove(toRescue.CSteamID);
                foreach (SteamPlayer sp in Provider.clients)
                {
                    UnturnedPlayer tmpPlayer = UnturnedPlayer.FromSteamPlayer(sp);
                    if ((tmpPlayer.HasPermission("emergency.receive") && !silent.Contains(tmpPlayer.CSteamID)) || tmpPlayer.CSteamID == player.CSteamID || tmpPlayer.CSteamID == toRescue.CSteamID)
                    {
                        UnturnedChat.Say(tmpPlayer, Translate("respond", player.CharacterName, toRescue.CharacterName), Color.cyan);
                    }
                }
            }
            else
            {
                UnturnedChat.Say(caller, Translate("noemergency", toRescue.CharacterName), Color.yellow);
            }
        }

        [RocketCommandPermission("emergency.send")]
        [RocketCommandAlias("emergency")]
        [RocketCommand("police","Requests for emergency help from other players", "/emergency <message>", AllowedCaller.Player)]
        public void ExecuteCommand911(IRocketPlayer caller, string[] parameters)
        {
            if(parameters.Length < 1)
            {
                UnturnedChat.Say(caller, "Usage: /emergency <message>", Color.yellow);
                return;
            }

            string msg = string.Join(" ", parameters);

            UnturnedPlayer player = (UnturnedPlayer)caller;
            if(!emergencies.ContainsKey(player.CSteamID)) emergencies.Add(player.CSteamID, DateTime.Now);
            foreach(SteamPlayer sp in Provider.clients)
            {
                UnturnedPlayer tmpPlayer = UnturnedPlayer.FromSteamPlayer(sp);
                if((tmpPlayer.HasPermission("emergency.receive") && !silent.Contains(tmpPlayer.CSteamID)) || tmpPlayer.CSteamID == player.CSteamID)
                {
                    if (Configuration.Instance.ShowCoordinates)
                    {
                        string x = Math.Round(player.Position.x).ToString();
                        string y = Math.Round(player.Position.y).ToString();
                        string z = Math.Round(player.Position.z).ToString();
                        UnturnedChat.Say(tmpPlayer, Translate("emergencycoords", player.CharacterName, msg, x, y, z), Color.red);
                    }
                    else
                    {
                        UnturnedChat.Say(tmpPlayer, Translate("emergency", player.CharacterName, msg), Color.red);
                    }
                }
            }

        }
    }
}
