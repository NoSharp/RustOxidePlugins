using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{

    [Info("Clans Rebirthed", "Serenity 3", "1.0", ResourceId = 0)]
    [Description("Serenity's Clans Rebirthed")]
    internal class ClansRebirthed : RustPlugin
    {
        #region Fields

        public static StoreData data = new StoreData();

        public HashSet<Timer> ActiveTimers = new HashSet<Timer>();

        [PluginReference] public Plugin BetterChat, Friends, FriendlyFire;

        #endregion Fields

        #region Saving Data

        public enum Rank { Normal = 0, Moderator = 1, Council = 2, Owner = 3 };

        public struct DataFile
        {
            public const string ClanInviteData = "ClanInviteData";
            public const string ClanData = "ClanData";
        }

        public class StoreData
        {
            public HashSet<Clan> ClanData = new HashSet<Clan>();
            public Dictionary<ulong, Clan> ClanInviteData = new Dictionary<ulong, Clan>();

            public StoreData()
            {
                ReadData(ref ClanData, DataFile.ClanData);
                ReadData(ref ClanInviteData, DataFile.ClanInviteData);
            }

            public void ReadData<T>(ref T data, string filename ) => data = Interface.Oxide.DataFileSystem.ReadObject<T>($"ClansRebirthed/{filename}");

            public void SaveData<T>(T data, string filename) => Interface.Oxide.DataFileSystem.WriteObject($"ClansRebirthed/{filename}", ClanData);
        }

        public class Clan
        {
            public string ClanName;
            public string Description;
            public int Id;
            public Vector3 ClanHome;
            public Dictionary<ulong, Rank> MemberList;
            public List<int> AllianceList;
            public List<int> EnemyList;

            public Clan(string name, string desc, ulong playerWhoMade)
            {
                var currentId = 0;
                var playerFromId = BasePlayer.FindByID(playerWhoMade);
                var allianceList = new List<int>();
                var enemyList = new List<int>();
                var memberList = new Dictionary<ulong, Rank>
                {
                    { playerWhoMade, Rank.Owner }
                };

                foreach (var e in data.ClanData)
                {
                    currentId = e.Id;
                }

                ClanName = name;
                Id = currentId + 1;
                MemberList = memberList;
                AllianceList = allianceList;
                EnemyList = enemyList;
                Description = desc;
                ClanHome = new Vector3();
            }
        }

        #endregion Saving Data

        #region Helpers

        public struct Permission
        {
            public const string Admin = "clansrebirthed.admin";
            public const string Create = "clansrebirthed.clan.create";
            public const string Invite = "clansrebirthed.clan.invite";
            public const string Leave = "clansrebirthed.clan.leave";
            public const string SetHome = "clansrebirthed.clan.sethome";
            public const string Promote = "clansrebirthed.clan.promote";
            public const string Home = "clansrebirthed.clan.home";
            public const string Join = "clansrebirthed.clan.join";
            public const string AllyChat = "clansrebirthed.clan.allychat";
            public const string ClanChat = "clansrebirthed.clan.clanchat";
            public const string Kick = "clansrebirthed.clan.kick";
        }

        public void RegisterAllPerms()
        {
            foreach (var info in typeof(Permission).GetFields().Where(x => x.IsLiteral))
            {
                var perm = info.GetValue(info) as string;
                permission.RegisterPermission(perm, this);
            }
        }

        public bool CheckIfPlayerHasPerm(string playerId, string permName)
        {
            var player = BasePlayer.FindByID(ulong.Parse(playerId));
            if (permission.UserHasPermission(playerId, permName))
            {
                return true;
            }

            return false;
        }

        public struct LangMessages
        {
            public const string SendInvite = "SendInvitesToFriends";
            public const string RecieveInviteFromFriend = "RecieveInviteFromFriend";
            public const string NoPermissions = "NoPermissions";
            public const string NotEnoughArguments = "NotEnoughArguments";
            public const string DeleteClan = "DeleteClan";
            public const string ClanNotFound = "ClanNotFound";
            public const string InvitePlayerToClan = "InvitePlayerToClan";
            public const string SendInviteToPlayer = "SendInviteToPlayer";
            public const string CantPurge = "CantPurge";
            public const string AlreadyAClan = "AlreadyAClan";
            public const string AlreadyInAClan = "AlreadyInAClan";
            public const string CreateClan = "CreateClan";
            public const string NotInClan = "NotInClan";
            public const string NotHighEnoughClanRank = "NotHighEnoughClanRank";
            public const string NoInvites = "NoInvites";
            public const string ClanWasRemoved = "ClanWasRemoved";
            public const string ClanJoinPlayer = "ClanJoinPlayer";
            public const string PlayerJoinClan = "PlayerJoinClan";
            public const string PlayerInvites = "PlayerInvites";
            public const string ClanInvitesPlayer = "ClanInvitesPlayer";
            public const string NotAValidPlayer = "NotAValidPlayer";
            public const string PlayerAlreadyInClan = "PlayerAlreadyInClan";
            public const string PlayerNotInClan = "PlayerNotInClan";
            public const string PlayerUnableToRankUp = "PlayerUnableToRankUp";
            public const string PlayerRankedUp = "PlayerRankedUp";
            public const string PlayerNotInYourClan = "PlayerNotInYourClan";
            public const string PlayerCantKickPlayer = "PlayerCantKickPlayer";
            public const string KickedPlayerFromClan = "KickedPlayerFromClan";
            public const string NoAllies = "NoAllies";
        }

        public void RegisterLangMessages()
        {

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"RecieveInviteFromFriend", "Recieved and invite from your friend: {0}"},
                {"NoPermissions", "You don't have the correct permissions."},
                {"NotEnoughArguments", "You have not used the correct amount of arguments."},
                {"DeleteClan", "Deleted clan: {0}."},
                {"ClanNotFound", "Clan {0} not found!."},
                {"InvitePlayerToClan", "Clan {0} invited you to join!"},
                {"SendInviteToPlayer","Player {0} invited {1} to join {2}"},
                {"CantPurge","You can't purge data."},
                {"AlreadyAClan","There is already clan by this name!"},
                {"AlreadyInAClan","You are already in a clan! You must leave the clan {0}."},
                {"CreateClan","Created the clan {0} : {1}"},
                {"NotInClan","You are not in a clan."},
                {"NotHighEnoughClanRank","You are not a high enough position in your clan to invite."},
                {"NoInvites","You were not invite to any clan. :("},
                {"ClanWasRemoved", "This Clan is no longer valid"},
                {"ClanJoinPlayer", "You Have joined the clan {0}"},
                {"PlayerJoinClan", "{0} has joined the clan!"},
                {"PlayerInvites", "{0} has been invited the clan!"},
                {"ClanInvitesPlayer", "You have been invited to {0}."},
                {"NotAValidPlayer","The player is not found."},
                {"PlayerAlreadyInClan","The player is already in a clan."},
                {"PlayerNotInClan","The player is not in the clan."},
                {"PlayerUnableToRankUp","You are not able to rankup."},
                {"PlayerRankedUp","Ranked up {0}"},
                {"PlayerNotInYourClan"," This player is not in your clan :("},
                {"PlayerCantKickPlayer", "You can't kick someone who is higher in rank than you!"},
                {"KickedPlayerFromClan", "You Kicked {0} from the clan!"},
                {"NoAllies", "Your clan has no allies."}
            }, this);
            
        }

        public string GetMessage(string TargetMessage)
        {
            return lang.GetMessage(TargetMessage, this);
        }

        public class ConfigData
        {
            public List<ulong> PurgeAllowedPlayers { get; set; }
            public int SaveTime { get; set; }
            public bool AnnounceCreation { get; set; }
            public bool AnnounceDeletion { get; set; }
            public bool UseFriends { get; set; }
            public bool SendRequestToFriends { get; set; }
            public bool Enabled { get; set; }

            public ConfigData()
            {
                PurgeAllowedPlayers = new List<ulong>();
                SaveTime = 120;
                AnnounceCreation = true;
                AnnounceDeletion = true;
                UseFriends = true;
                SendRequestToFriends = true;
                Enabled = true;
            }
        }

        public void SaveToInviteList(ulong userId, Clan clan)
        {
            data.ClanInviteData.Add(userId, clan);
        }

        public void SaveToClanData(Clan clan)
        {
            data.ClanData.Add(clan);
        }

        public void SaveAllDataTimer()
        {
            var cfgData = Config.ReadObject<ConfigData>();
            
            ActiveTimers.Add(timer.Repeat(cfgData.SaveTime, 0, () =>
            {
                Puts("STARTING SAVE OF CLAN DATA...");
                data.SaveData(data.ClanData, DataFile.ClanData);
                data.SaveData(data.ClanInviteData, DataFile.ClanInviteData);
                Puts("SAVED ALL CLAN DATA");
            }));


        }

        public void DestroyAllTimers()
        {
            foreach (var e in ActiveTimers)
            {
                e.Destroy();
            }
        }

        public BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.displayName == nameOrId) return activePlayer;
                if (activePlayer.displayName.ToLower().Contains(nameOrId.ToLower())) return activePlayer;
                if (activePlayer.UserIDString == nameOrId) return activePlayer;
            }
            return null;
        }

        public void LangMessageToPlayer(BasePlayer player, string message)
        {
            player.SendMessage($"{GetMessage(message)}");
        }

        #region Clan Managment

        private string FindClanTagOf(ulong playerId)
        {
            foreach (var e in data.ClanData)
            {
                if (e.MemberList.ContainsKey(playerId)) return e.ClanName;
            }
            return string.Empty;
        }

        private bool CheckIfPlayerInClan(ulong playerId)
        {
            foreach (var e in data.ClanData)
            {
                if (e.MemberList.ContainsKey(playerId))
                {
                    return true;
                }
            }
            return false;
        }

        private bool SendInviteTo(ulong playerId, Clan clan)
        {
            var playerFromId = BasePlayer.FindByID(playerId);

            if (playerFromId == null)
            {
                return false;
            }

            data.ClanInviteData.Add(playerId, clan);
            playerFromId.SendMessage($"{string.Format(GetMessage("ClanInvitesPlayer"), clan.ClanName)}");
            data.SaveData(data.ClanInviteData, DataFile.ClanInviteData);
            return true;
        }

        private bool PlayerInClan(ulong playerId, Clan clan)
        {
            if (clan.MemberList.ContainsKey(playerId))
            {
                return true;
            }
            return false;
        }

        private void AddPlayerToClan(ulong playerId, string clanName)
        {
            var playerFromId = BasePlayer.FindByID(playerId);

            foreach (var e in data.ClanData)
            {
                if (e.ClanName == clanName)
                {
                    e.MemberList.Add(playerId, Rank.Normal);
                }
            }
        }

        private void DeleteClan(string name)
        {
            data.ReadData(ref data.ClanData, DataFile.ClanData);

            foreach (var e in data.ClanData)
            {
                if (e.ClanName == name)
                {
                    data.ClanData.Remove(e);
                    data.SaveData(data.ClanData, DataFile.ClanData);
                }
            }
        }

        private bool CheckIfClanExists(string name)
        {
            foreach (var e in data.ClanData)
            {
                if (e.ClanName == name)
                {
                    return true;
                }
            }
            return false;
        }

        private Clan GetClanOf(ulong playerId)
        {
            foreach (var e in data.ClanData)
            {
                if (e.MemberList.ContainsKey(playerId))
                {
                    return e;
                }
            }
            return null;
        }

        private Clan ClanFindById(int clanId)
        {
            foreach (var e in data.ClanData)
            {
                if (e.Id == clanId)
                {
                    return e;
                }
            }
            return null;
        }

        private void ClanBroadcast(Clan targetClan, string message)
        {
            foreach (var e in targetClan.MemberList)
            {
                BasePlayer player = BasePlayer.FindByID(e.Key);
                player.SendMessage(message);
            }
        }

        private void AllyBroadcast(Clan TargetClan, string message)
        {
            foreach (var e in TargetClan.AllianceList)
            {
                var allyClan = ClanFindById(e);
                ClanBroadcast(allyClan, message);
            }

        }

        private bool DoesClanHaveAllies(Clan targetClan)
        {
            if (targetClan.AllianceList.Count == 0)
            {
                return false;
            }
            return true;
        }

        #endregion Clan Managment

        #endregion Helpers

        #region Hooks

        private void OnBetterChat(Dictionary<string, object> data)
        {
            var betterChatTitles = data["Titles"] as List<string>;
            var betterChatPlayer = data["Player"] as IPlayer;
            var findClanOfPlayer = FindClanTagOf(ulong.Parse(betterChatPlayer.Id));

            betterChatTitles.Add(findClanOfPlayer);
        }

        private void Loaded()
        {  
            RegisterLangMessages();
            RegisterAllPerms();
            SaveAllDataTimer();
        }

        private void UnLoad()
        {
            DestroyAllTimers();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            ConfigData cfgData = new ConfigData();
            Config.WriteObject(cfgData);
            Config.Save();
            Config.Load();
        }

        #endregion Hooks

        #region Command

        [ChatCommand("clan")]
        private void ClanCommand(BasePlayer player, string command, string[] args)
        {
            var cfgData = Config.ReadObject<ConfigData>();

            switch (args.Length)
            {
                case 3:
                    if (args[0] == "create")
                    {
                        var name = args[1];
                        var desc = args[2];
                        var clanClass = new Clan(name, desc, player.userID);

                        if (!CheckIfPlayerHasPerm(player.UserIDString, Permission.Create))
                        {
                            LangMessageToPlayer(player, LangMessages.NoPermissions);
                            return;
                        }

                        if (CheckIfClanExists(name))
                        {
                            LangMessageToPlayer(player, LangMessages.ClanNotFound);
                            return;
                        }

                        if (CheckIfPlayerInClan(player.userID))
                        {
                            player.ChatMessage($"{string.Format(GetMessage("AlreadyInAClan"), GetClanOf(player.userID))}");
                            return;
                        }

                        data.ClanData.Add(clanClass);
                        data.SaveData(data.ClanData, DataFile.ClanData);
                        player.ChatMessage($"{string.Format(GetMessage("CreateClan"), clanClass.ClanName, clanClass.Description)}");
                        return;
                    }
                    break;

                case 2:

                    if (args[0].ToLower() == "deleteclan")
                    {
                        if (!CheckIfPlayerHasPerm(player.UserIDString, Permission.Admin))
                        {
                            LangMessageToPlayer(player, LangMessages.NoPermissions);
                            return;
                        }
                        if (CheckIfClanExists(args[1]))
                        {
                            DeleteClan(args[1]);
                            player.ChatMessage(string.Format(lang.GetMessage("DeleteClan", this), args[1]));
                            return;
                        }
                        player.ChatMessage(string.Format(lang.GetMessage("ClanNotFound", this), args[1]));
                        return;
                    }

                    if (args[0].ToLower() == "invite")
                    {
                        if (!CheckIfPlayerHasPerm(player.UserIDString, Permission.Invite))
                        {
                            LangMessageToPlayer(player, LangMessages.NoPermissions);
                            return;
                        }

                        if (!CheckIfPlayerInClan(player.userID))
                        {
                            LangMessageToPlayer(player, LangMessages.NotInClan);
                            return;
                        }

                        var targetPlayer = FindPlayer(player.UserIDString);

                        if (targetPlayer == false)
                        {
                            LangMessageToPlayer(player, LangMessages.NotAValidPlayer);
                            return;
                        }

                        var playerClanClass = GetClanOf(player.userID);
                        var getRankFromPlayer = playerClanClass.MemberList[player.userID];

                        if (GetClanOf(targetPlayer.userID) == null)
                        {
                            LangMessageToPlayer(player, LangMessages.NotAValidPlayer);
                            return;
                        }

                        switch (getRankFromPlayer)
                        {
                            case Rank.Council:
                                break;
                            case Rank.Moderator:
                                break;
                            case Rank.Owner:
                                break;
                            default:
                                LangMessageToPlayer(player, LangMessages.NotHighEnoughClanRank);
                                return;
                        }

                        SendInviteTo(targetPlayer.userID, playerClanClass);

                    }

                    if (args[0].ToLower() == "join")
                    {
                        if (!data.ClanInviteData.ContainsKey(player.userID))
                        {
                            LangMessageToPlayer(player, LangMessages.NoPermissions);
                            return;
                        }

                        var targetClan = data.ClanInviteData[player.userID];

                        if (CheckIfPlayerInClan(player.userID))
                        {
                            LangMessageToPlayer(player, LangMessages.AlreadyInAClan);
                        }

                        if (!data.ClanData.Contains(targetClan))
                        {
                            player.SendMessage($"{string.Format(GetMessage("ClanNotFound"), targetClan.ClanName)}");
                            return;
                        }
                        targetClan.MemberList.Add(player.userID, Rank.Normal);

                        player.SendMessage($"{string.Format(GetMessage("ClanJoinPlayer"), targetClan.ClanName)}");

                        ClanBroadcast(targetClan, $"{string.Format(GetMessage("PlayerJoinClan"), player.displayName)}");
                        
                        return;
                    }

                    if (args[0].ToLower() == "promote")
                    {
                        if (!CheckIfPlayerInClan(player.userID))
                        {
                            LangMessageToPlayer(player, LangMessages.NotInClan);
                            return;
                        }
                        if (!CheckIfPlayerHasPerm(player.UserIDString, Permission.Promote))
                        {
                            player.SendMessage($"{GetMessage(LangMessages.NoPermissions)}");
                            return;
                        }

                        if (!CheckIfPlayerInClan(player.userID))
                        {
                            LangMessageToPlayer(player, LangMessages.NotInClan);
                            return;
                        }

                        var targetPlayer = FindPlayer(args[1]);
                        var playerClan = GetClanOf(targetPlayer.userID);

                        if (!PlayerInClan(targetPlayer.userID, playerClan))
                        {
                            LangMessageToPlayer(player, LangMessages.PlayerNotInClan);
                            return;
                        }

                        var targetPlayerRank = playerClan.MemberList[targetPlayer.userID];
                        playerClan.MemberList.Remove(targetPlayer.userID);

                        switch (targetPlayerRank)
                        {
                            case (Rank.Normal):
                                playerClan.MemberList.Add(targetPlayer.userID, Rank.Moderator);
                                data.SaveData(data.ClanData, DataFile.ClanData);
                                player.SendMessage($"{string.Format(GetMessage("PlayerRankedUp"), player.displayName)}");
                                return;

                            case (Rank.Moderator):
                                playerClan.MemberList.Add(targetPlayer.userID, Rank.Council);
                                data.SaveData(data.ClanData, DataFile.ClanData);
                                player.SendMessage($"{string.Format(GetMessage("PlayerRankedUp"), player.displayName)}");
                                return;
                            case (Rank.Council):
                                LangMessageToPlayer(player, LangMessages.PlayerUnableToRankUp);
                                return;
                            case (Rank.Owner):
                                LangMessageToPlayer(player, LangMessages.PlayerUnableToRankUp);
                                return;
                            default:
                                return;
                        }
                        
                    }

                    if (args[0].ToLower() == "kick")
                    {
                        if (!CheckIfPlayerHasPerm(player.UserIDString, Permission.Admin))
                        {
                            player.SendMessage($"{GetMessage(LangMessages.NoPermissions)}");
                            return;
                        }

                        if (!CheckIfPlayerInClan(player.userID))
                        {
                            LangMessageToPlayer(player, LangMessages.NotInClan);
                            return;
                        }

                        var targetPlayer = FindPlayer(args[1]);

                        if (targetPlayer == null)
                        {
                            LangMessageToPlayer(player, LangMessages.NotAValidPlayer);
                            return;
                        }

                        var playerClan = GetClanOf(player.userID);

                        if (playerClan == null)
                        {
                            LangMessageToPlayer(player, LangMessages.NotInClan);
                            return;
                        }

                        var targetPlayerClan = GetClanOf(targetPlayer.userID);

                        if (targetPlayerClan == null)
                        {
                            LangMessageToPlayer(player, LangMessages.PlayerNotInClan);
                            return;
                        }

                        if (targetPlayerClan != playerClan)
                        {
                            LangMessageToPlayer(player, LangMessages.PlayerNotInYourClan);
                            return;
                        }

                        if (playerClan.MemberList[player.userID] <= Rank.Normal)
                        {
                            LangMessageToPlayer(player, LangMessages.NotHighEnoughClanRank);
                            return;
                        }

                        if (playerClan.MemberList[targetPlayer.userID] > playerClan.MemberList[player.userID])
                        {
                            LangMessageToPlayer(player, LangMessages.PlayerCantKickPlayer);
                        }

                        playerClan.MemberList[targetPlayer.userID] = playerClan.MemberList[targetPlayer.userID] ++ ;
                        data.SaveData(data.ClanData, DataFile.ClanData);

                        player.SendMessage(string.Format($"{GetMessage("KickedPlayerFromClan")}", targetPlayer.displayName));
                        return;
                    }
                    break;

                case 1:
                    if (args[0].ToLower() == "purgeallclans")
                    {
                        if (!CheckIfPlayerHasPerm(player.UserIDString, Permission.Admin))
                        {
                            LangMessageToPlayer(player, LangMessages.NoPermissions);
                            return;
                        }
                        if (!cfgData.PurgeAllowedPlayers.Contains(player.userID))
                        {
                            LangMessageToPlayer(player, LangMessages.CantPurge);
                            return;
                        }

                        data.ReadData(ref data.ClanData, DataFile.ClanData);
                        data.ReadData(ref data.ClanInviteData, DataFile.ClanInviteData);

                        foreach (var e in data.ClanData)
                        {
                            data.ClanData.Remove(e);
                        }

                        foreach (var e in data.ClanInviteData)
                        {
                            data.ClanInviteData.Remove(e.Key);
                        }
                        data.SaveData(data.ClanData, DataFile.ClanData);
                        data.SaveData(data.ClanData, DataFile.ClanData);
                    }
                    break;

                default:
                    player.ChatMessage(GetMessage("NotEnoughArguments"));
                    break;
            }

        }

        [ChatCommand("c")]
        private void ClanChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!CheckIfPlayerHasPerm(player.UserIDString, Permission.ClanChat))
            {
                LangMessageToPlayer(player, LangMessages.NoPermissions);
                return;

            }

            if (!CheckIfPlayerInClan(player.userID))
            {
                LangMessageToPlayer(player, LangMessages.NotInClan);
                return;
            }

            var targetClan = GetClanOf(player.userID);
            ClanBroadcast(targetClan, args.ToString());
            Puts(args.ToString());
        }

        [ChatCommand("a")]
        private void AllianceChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!CheckIfPlayerHasPerm(player.UserIDString, Permission.ClanChat))
            {
                LangMessageToPlayer(player, LangMessages.NoPermissions);
                return;

            }

            if (!CheckIfPlayerInClan(player.userID))
            {
                LangMessageToPlayer(player, LangMessages.NotInClan);
                return;
            }

            var targetClan = GetClanOf(player.userID);

            if (!DoesClanHaveAllies(targetClan))
            {
                LangMessageToPlayer(player, LangMessages.NoAllies);
                return;
            }

            AllyBroadcast(targetClan, args.ToString());
            
        }

        #endregion Command
    }
}