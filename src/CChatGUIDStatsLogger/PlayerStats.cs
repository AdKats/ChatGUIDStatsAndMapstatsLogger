/*  Copyright 2013 [GWC]XpKillerhx

    This plugin file is part of PRoCon Frostbite.

    This plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This plugin is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with PRoCon Frostbite.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Dapper;

using Flurl.Http;

using MySqlConnector;

using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;

namespace PRoConEvents
{
    public partial class CChatGUIDStatsLogger
    {
        private Int32 GetPlayerTeamID(String strSoldierName)
        {
            Int32 iTeamID = 0; // Neutral Team ID
            if (this.m_dicPlayers.ContainsKey(strSoldierName) == true)
            {
                iTeamID = this.m_dicPlayers[strSoldierName].TeamID;
            }
            return iTeamID;
        }

        private void playerLeftServer(CPlayerInfo cpiPlayer)
        {
            try
            {
                this.DebugInfo("Trace", "playerLeftServer: " + cpiPlayer.SoldierName + " EAGUID: " + cpiPlayer.GUID);
                if (this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true)
                {
                    this.StatsTracker[cpiPlayer.SoldierName].Score = cpiPlayer.Score;
                    this.StatsTracker[cpiPlayer.SoldierName].TimePlayerleft = MyDateTime.Now;
                    this.StatsTracker[cpiPlayer.SoldierName].playerleft();
                    //EA-GUID, ClanTag, usw.
                    if (cpiPlayer.GUID.Length > 2)
                    {
                        this.StatsTracker[cpiPlayer.SoldierName].EAGuid = cpiPlayer.GUID;
                    }
                    //ID cache System
                    if (this.StatsTracker[cpiPlayer.SoldierName].EAGuid.Length > 2)
                    {
                        if (this.m_ID_cache.ContainsKey(this.StatsTracker[cpiPlayer.SoldierName].EAGuid) == true)
                        {
                            this.m_ID_cache[this.StatsTracker[cpiPlayer.SoldierName].EAGuid].PlayeronServer = false;
                        }
                    }
                    this.StatsTracker[cpiPlayer.SoldierName].ClanTag = cpiPlayer.ClanTag;
                }
                //Mapstats
                this.Mapstats.IntplayerleftServer++;
                //Session
                if (this.m_dicSession.ContainsKey(cpiPlayer.SoldierName) == true)
                {
                    if (cpiPlayer.Score > 0)
                    {
                        this.m_dicSession[cpiPlayer.SoldierName].AddScore(cpiPlayer.Score);
                    }
                    this.m_dicSession[cpiPlayer.SoldierName].TimePlayerleft = MyDateTime.Now;
                    this.m_dicSession[cpiPlayer.SoldierName].playerleft();
                    this.DebugInfo("Trace", "Score: " + this.m_dicSession[cpiPlayer.SoldierName].TotalScore.ToString() + " Playtime: " + this.m_dicSession[cpiPlayer.SoldierName].TotalPlaytime.ToString());
                    if (this.m_dicSession[cpiPlayer.SoldierName].TotalScore > 10 || this.m_dicSession[cpiPlayer.SoldierName].Kills > 0 || this.m_dicSession[cpiPlayer.SoldierName].Deaths > 0)
                    {
                        if ((this.m_dicSession[cpiPlayer.SoldierName].EAGuid.Length < 2) && (this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true))
                        {
                            if (this.StatsTracker[cpiPlayer.SoldierName].EAGuid.Length > 2)
                            {
                                this.m_dicSession[cpiPlayer.SoldierName].EAGuid = this.StatsTracker[cpiPlayer.SoldierName].EAGuid;
                            }
                        }
                        this.DebugInfo("Trace", "Adding Session of Player " + cpiPlayer.SoldierName + " to passed sessions");
                        //Adding passed session to list if player has a Score greater than 0 or a Player greater than 120 sec
                        this.lstpassedSessions.Add(this.m_dicSession[cpiPlayer.SoldierName]);
                    }
                    //Removing old session
                    lock (this.sessionlock)
                    {
                        this.m_dicSession.Remove(cpiPlayer.SoldierName);
                    }
                }
                else
                {
                    this.DebugInfo("Trace", "playerLeftServer: " + cpiPlayer.SoldierName + " not in session dic");
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "playerLeftServer:" + c);
            }
        }

        private void playerKilled(Kill kKillerVictimDetails)
        {
            if (this.DamageClass.ContainsKey(kKillerVictimDetails.DamageType) == false && !kKillerVictimDetails.DamageType.Equals("Death"))
            {
                this.DebugInfo("Trace", "Weapon: " + kKillerVictimDetails.DamageType + " is missing in the " + this.strServerGameType + ".def file!!!");
            }
            //this.DebugInfo("Trace","PlayerKilled Killer: "+ kKillerVictimDetails.Killer.SoldierName + "Victim: " + kKillerVictimDetails.Victim.SoldierName + "Weapon: " + kKillerVictimDetails.DamageType);
            //TEAMKILL OR SUICID
            if (String.Compare(kKillerVictimDetails.Killer.SoldierName, kKillerVictimDetails.Victim.SoldierName) == 0)
            {		//  A Suicide
                this.AddSuicideToStats(kKillerVictimDetails.Killer.SoldierName, this.DamageClass[kKillerVictimDetails.DamageType], kKillerVictimDetails.DamageType);
            }
            else
            {
                if (this.GetPlayerTeamID(kKillerVictimDetails.Killer.SoldierName) == this.GetPlayerTeamID(kKillerVictimDetails.Victim.SoldierName))
                { 	//TeamKill
                    this.AddTeamKillToStats(kKillerVictimDetails.Killer.SoldierName);
                    this.AddDeathToStats(kKillerVictimDetails.Victim.SoldierName, this.DamageClass[kKillerVictimDetails.DamageType], kKillerVictimDetails.DamageType);
                }
                else
                {
                    //this.DebugInfo("Trace","PlayerKilled: Regular Kill");
                    //Regular Kill: Player killed an Enemy
                    this.AddKillToStats(kKillerVictimDetails.Killer.SoldierName, this.DamageClass[kKillerVictimDetails.DamageType], kKillerVictimDetails.DamageType, kKillerVictimDetails.Headshot);
                    this.AddDeathToStats(kKillerVictimDetails.Victim.SoldierName, this.DamageClass[kKillerVictimDetails.DamageType], kKillerVictimDetails.DamageType);
                    if (String.Equals(kKillerVictimDetails.DamageType, "Melee"))
                    {	//Dogtagstracking
                        CKillerVictim KnifeKill = new CKillerVictim(kKillerVictimDetails.Killer.SoldierName, kKillerVictimDetails.Victim.SoldierName);
                        if (m_dicKnifeKills.ContainsKey(KnifeKill) == true)
                        {
                            m_dicKnifeKills[KnifeKill]++;
                        }
                        else
                        {
                            m_dicKnifeKills.Add(KnifeKill, 1);
                        }
                    }
                }
            }
        }

        private void StartStreaming()
        {
            lock (this.streamlock)
            {
                Boolean success = false;
                Int32 attemptCount = 0;
                try
                {
                    DateTime StartStreamingTime = MyDateTime.Now;
                    //Make a copy of Statstracker to prevent unwanted errors
                    Dictionary<String, CStats> StatsTrackerCopy = new Dictionary<String, CStats>(this.StatsTracker);
                    //C_ID_Cache id_cache;
                    List<String> lstEAGUIDs = new List<String>();
                    //Clearing the old Dictionary
                    StatsTracker.Clear();
                    if (isStreaming)
                    {
                        this.DebugInfo("Info", "Started streaming to the DB-Server");
                        // Uploads chat logs and Stats for round to database
                        if (ChatLog.Count > 0 || this.m_enLogSTATS == enumBoolYesNo.Yes)
                        {
                            this.tablebuilder(); //Build the tables if not exists
                            if ((m_strHost != null) && (m_strDatabase != null) && (m_strDBPort != null) && (m_strUserName != null) && (m_strPassword != null))
                            {
                                try
                                {
                                    this.OpenMySqlConnection(2);
                                    this.MySql_Connection_is_activ = true;

                                    if (ChatLog.Count > 0 && MySqlConn.State == ConnectionState.Open)
                                    {
                                        String ChatSQL = @"INSERT INTO " + this.tbl_chatlog + @" (logDate, ServerID, logSubset, logSoldierName, logMessage) VALUES ";
                                        lock (ChatLog)
                                        {
                                            Int32 i = 0;
                                            DynamicParameters chatParams = new DynamicParameters();
                                            foreach (CLogger log in ChatLog)
                                            {
                                                ChatSQL = String.Concat(ChatSQL, "(@logDate" + i + ", @ServerID" + i + ", @logSubset" + i + ", @logSoldierName" + i + ", @logMessage" + i + "),");
                                                chatParams.Add("logDate" + i, log.Time);
                                                chatParams.Add("ServerID" + i, this.ServerID);
                                                chatParams.Add("logSubset" + i, log.Subset);
                                                chatParams.Add("logSoldierName" + i, log.Name);
                                                chatParams.Add("logMessage" + i, log.Message);
                                                i++;
                                            }
                                            ChatSQL = ChatSQL.Remove(ChatSQL.LastIndexOf(","));
                                            MySqlConn.Execute(ChatSQL, chatParams);
                                            ChatLog.Clear();
                                        }
                                    }
                                    if (this.m_mapstatsON == enumBoolYesNo.Yes && MySqlConn.State == ConnectionState.Open)
                                    {
                                        this.DebugInfo("Trace", "Mapstats Write querys");
                                        this.Mapstats.calcMaxMinAvgPlayers();
                                        String MapSQL = @"INSERT INTO " + tbl_mapstats + @" (ServerID, TimeMapLoad, TimeRoundStarted, TimeRoundEnd, MapName, Gamemode, Roundcount, NumberofRounds, MinPlayers, AvgPlayers, MaxPlayers, PlayersJoinedServer, PlayersLeftServer)
													VALUES (@ServerID, @TimeMapLoad, @TimeRoundStarted, @TimeRoundEnd, @MapName, @Gamemode, @Roundcount, @NumberofRounds, @MinPlayers, @AvgPlayers, @MaxPlayers, @PlayersJoinedServer, @PlayersLeftServer)";
                                        MySqlConn.Execute(MapSQL, new
                                        {
                                            ServerID = this.ServerID,
                                            TimeMapLoad = this.Mapstats.TimeMaploaded,
                                            TimeRoundStarted = this.Mapstats.TimeMapStarted,
                                            TimeRoundEnd = this.Mapstats.TimeRoundEnd,
                                            MapName = this.Mapstats.StrMapname,
                                            Gamemode = this.Mapstats.StrGamemode,
                                            Roundcount = this.Mapstats.IntRound,
                                            NumberofRounds = this.Mapstats.IntNumberOfRounds,
                                            MinPlayers = this.Mapstats.IntMinPlayers,
                                            AvgPlayers = this.Mapstats.DoubleAvgPlayers,
                                            MaxPlayers = this.Mapstats.IntMaxPlayers,
                                            PlayersJoinedServer = this.Mapstats.IntplayerjoinedServer,
                                            PlayersLeftServer = this.Mapstats.IntplayerleftServer
                                        });
                                    }
                                    if (this.m_enLogSTATS == enumBoolYesNo.Yes && MySqlConn.State == ConnectionState.Open)
                                    {
                                        this.DebugInfo("Trace", "PlayerStats Write querys");
                                        //Prepare EAGUID List
                                        foreach (KeyValuePair<String, CStats> kvp in StatsTrackerCopy)
                                        {
                                            if (kvp.Value.EAGuid.Length > 1)
                                            {
                                                if (GlobalDebugMode.Equals("Trace"))
                                                {
                                                    this.DebugInfo("Trace", "Adding EAGUID " + kvp.Value.EAGuid + " to searchlist");
                                                }
                                                lstEAGUIDs.Add(kvp.Value.EAGuid);
                                            }
                                        }
                                        //Perform Cache Update
                                        this.UpdateIDCache(lstEAGUIDs);

                                        while (!success)
                                        {
                                            attemptCount++;
                                            try
                                            {
                                                MySqlTrans = MySqlConn.BeginTransaction();
                                                foreach (KeyValuePair<String, CStats> kvp in StatsTrackerCopy)
                                                {
                                                    if (kvp.Key.Length > 0 && kvp.Value.EAGuid.Length > 1)
                                                    {
                                                        if (this.m_ID_cache.ContainsKey(kvp.Value.EAGuid) == false)
                                                        {
                                                            if (GlobalDebugMode.Equals("Trace"))
                                                            {
                                                                this.DebugInfo("Trace", kvp.Value.EAGuid + " is not in Cache!");
                                                            }
                                                            continue;
                                                        }
                                                        if (this.m_ID_cache[kvp.Value.EAGuid].Id >= 1)
                                                        {
                                                            String UpdatedataSQL = @"UPDATE " + this.tbl_playerdata + @" SET SoldierName = @SoldierName, ClanTag = @ClanTag, PBGUID = @PBGUID, IP_Address = @IP_Address, CountryCode = @CountryCode, GlobalRank = @GlobalRank  WHERE PlayerID = @PlayerID";
                                                            //Update
                                                            if (GlobalDebugMode.Equals("Trace"))
                                                            {
                                                                this.DebugInfo("Trace", "Update for Player " + kvp.Key);
                                                                this.DebugInfo("Trace", "ClanTag " + kvp.Value.ClanTag);
                                                                this.DebugInfo("Trace", "SoldierName " + kvp.Key);
                                                                this.DebugInfo("Trace", "PBGUID " + kvp.Value.Guid);
                                                                this.DebugInfo("Trace", "EAGUID " + kvp.Value.EAGuid);
                                                                this.DebugInfo("Trace", "IP_Address " + kvp.Value.IP);
                                                                this.DebugInfo("Trace", "CountryCode " + kvp.Value.PlayerCountryCode);
                                                                this.DebugInfo("Trace", "GlobalRank " + kvp.Value.GlobalRank);
                                                            }
                                                            Object clanTagVal = (kvp.Value.ClanTag != null && kvp.Value.ClanTag.Length > 0) ? (Object)kvp.Value.ClanTag : Convert.DBNull;
                                                            MySqlConn.Execute(UpdatedataSQL, new
                                                            {
                                                                SoldierName = kvp.Key,
                                                                ClanTag = clanTagVal,
                                                                PBGUID = kvp.Value.Guid,
                                                                IP_Address = kvp.Value.IP,
                                                                CountryCode = kvp.Value.PlayerCountryCode,
                                                                PlayerID = this.m_ID_cache[kvp.Value.EAGuid].Id,
                                                                GlobalRank = kvp.Value.GlobalRank
                                                            }, transaction: MySqlTrans);
                                                        }
                                                        else if (this.m_ID_cache[kvp.Value.EAGuid].Id <= 0)
                                                        {
                                                            String InsertdataSQL = @"INSERT INTO " + this.tbl_playerdata + @" (ClanTag, SoldierName, PBGUID, GameID, EAGUID, IP_Address, CountryCode, GlobalRank) VALUES(@ClanTag, @SoldierName, @PBGUID, @GameID, @EAGUID, @IP_Address, @CountryCode, @GlobalRank)";
                                                            //Insert
                                                            if (GlobalDebugMode.Equals("Trace"))
                                                            {
                                                                this.DebugInfo("Trace", "Insert for Player " + kvp.Key);
                                                                this.DebugInfo("Trace", "ClanTag " + kvp.Value.ClanTag);
                                                                this.DebugInfo("Trace", "SoldierName " + kvp.Key);
                                                                this.DebugInfo("Trace", "PBGUID " + kvp.Value.Guid);
                                                                this.DebugInfo("Trace", "EAGUID " + kvp.Value.EAGuid);
                                                                this.DebugInfo("Trace", "IP_Address " + kvp.Value.IP);
                                                                this.DebugInfo("Trace", "CountryCode " + kvp.Value.PlayerCountryCode);
                                                                this.DebugInfo("Trace", "GlobalRank " + kvp.Value.GlobalRank);
                                                            }
                                                            Object clanTagVal = (kvp.Value.ClanTag != null && kvp.Value.ClanTag.Length > 0) ? (Object)kvp.Value.ClanTag : Convert.DBNull;
                                                            MySqlConn.Execute(InsertdataSQL, new
                                                            {
                                                                ClanTag = clanTagVal,
                                                                SoldierName = kvp.Key,
                                                                PBGUID = kvp.Value.Guid,
                                                                GameID = this.intServerGameType_ID,
                                                                EAGUID = kvp.Value.EAGuid,
                                                                IP_Address = kvp.Value.IP,
                                                                CountryCode = kvp.Value.PlayerCountryCode,
                                                                GlobalRank = kvp.Value.GlobalRank
                                                            }, transaction: MySqlTrans);
                                                        }
                                                    }
                                                }
                                                MySqlTrans.Commit();
                                                success = true;
                                            }
                                            catch (MySqlException ex)
                                            {
                                                switch (ex.Number)
                                                {
                                                    case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                                    case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                                        if (attemptCount < this.TransactionRetryCount)
                                                        {
                                                            this.DebugInfo("Warning", "Warning in StartStreaming: Locktimeout or Deadlock occured restarting Transaction(Playerdata). Attempt: " + attemptCount);
                                                            try
                                                            {
                                                                MySqlTrans.Rollback();
                                                            }
                                                            catch { }
                                                            Thread.Sleep(attemptCount * 1000);
                                                        }
                                                        else
                                                        {
                                                            this.DebugInfo("Error", "Error in StartStreaming: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction Playerdata)");
                                                            throw;
                                                        }
                                                        break;
                                                    default:
                                                        throw; //Other exceptions
                                                }
                                            }
                                        }
                                        //Reset bool and counter
                                        attemptCount = 0;
                                        success = false;

                                        //tbl_server_player

                                        this.DebugInfo("Trace", "tbl_server_player Write querys");
                                        this.UpdateIDCache(lstEAGUIDs);
                                        while (!success)
                                        {
                                            attemptCount++;
                                            try
                                            {
                                                //Start of the Transaction
                                                MySqlTrans = MySqlConn.BeginTransaction();
                                                foreach (KeyValuePair<String, CStats> kvp in StatsTrackerCopy)
                                                {
                                                    if (kvp.Value.EAGuid.Length > 0)
                                                    {
                                                        if (this.m_ID_cache.ContainsKey(kvp.Value.EAGuid) == false)
                                                        {
                                                            this.DebugInfo("Trace", kvp.Value.EAGuid + " is not in Cache!");
                                                            continue;
                                                        }
                                                        if (GlobalDebugMode.Equals("Trace"))
                                                        {
                                                            this.DebugInfo("Trace", "PlayerID: " + this.m_ID_cache[kvp.Value.EAGuid].Id);
                                                            this.DebugInfo("Trace", "StatsID: " + this.m_ID_cache[kvp.Value.EAGuid].StatsID);
                                                        }
                                                        if (this.m_ID_cache[kvp.Value.EAGuid].Id > 0 && this.m_ID_cache[kvp.Value.EAGuid].StatsID == 0)
                                                        {
                                                            String InsertdataSQL = @"INSERT INTO " + this.tbl_server_player + @" (ServerID, PlayerID) VALUES(@ServerID, @PlayerID)";
                                                            //Insert
                                                            this.DebugInfo("Trace", "Insert PlayerID " + this.m_ID_cache[kvp.Value.EAGuid].Id + "into tbl_server_player");
                                                            MySqlConn.Execute(InsertdataSQL, new { ServerID = this.ServerID, PlayerID = this.m_ID_cache[kvp.Value.EAGuid].Id }, transaction: MySqlTrans);
                                                        }
                                                    }
                                                }
                                                MySqlTrans.Commit();
                                                success = true;
                                            }
                                            catch (MySqlException ex)
                                            {
                                                switch (ex.Number)
                                                {
                                                    case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                                    case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                                        if (attemptCount < this.TransactionRetryCount)
                                                        {
                                                            this.DebugInfo("Warning", "Warning in StartStreaming: Locktimeout or Deadlock occured restarting Transaction(server_player). Attempt: " + attemptCount);
                                                            try
                                                            {
                                                                MySqlTrans.Rollback();
                                                            }
                                                            catch { }
                                                            Thread.Sleep(attemptCount * 1000);
                                                        }
                                                        else
                                                        {
                                                            this.DebugInfo("Error", "Error in StartStreaming: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction server_player)");
                                                            throw;
                                                        }
                                                        break;
                                                    default:
                                                        throw; //Other exceptions
                                                }
                                            }
                                        }
                                        //Reset bool and counter
                                        attemptCount = 0;
                                        success = false;

                                        this.DebugInfo("Trace", "Combatstats Write querys");
                                        //Perform Cache Update
                                        this.UpdateIDCache(lstEAGUIDs);
                                        if (this.m_enLogPlayerDataOnly == enumBoolYesNo.No)
                                        {
                                            while (!success)
                                            {
                                                attemptCount++;
                                                try
                                                {
                                                    //Start of the Transaction
                                                    MySqlTrans = MySqlConn.BeginTransaction();

                                                    foreach (KeyValuePair<String, CStats> kvp in StatsTrackerCopy)
                                                    {
                                                        if (this.m_ID_cache.ContainsKey(kvp.Value.EAGuid) == false)
                                                        {
                                                            if (GlobalDebugMode.Equals("Trace"))
                                                            {
                                                                this.DebugInfo("Trace", kvp.Value.EAGuid + " is not in Cache!(empty GUID?)");
                                                            }
                                                            continue;
                                                        }
                                                        Dictionary<String, Dictionary<String, CStats.CUsedWeapon>> tempdic;
                                                        //tempdic = StatsTrackerCopy[kvp.Key].getWeaponKills();
                                                        tempdic = new Dictionary<String, Dictionary<String, CStats.CUsedWeapon>>(kvp.Value.getWeaponKills());
                                                        if (GlobalDebugMode.Equals("Trace"))
                                                        {
                                                            this.DebugInfo("Trace", "PlayerID: " + this.m_ID_cache[kvp.Value.EAGuid].Id);
                                                            this.DebugInfo("Trace", "StatsID: " + this.m_ID_cache[kvp.Value.EAGuid].StatsID);
                                                        }

                                                        if (this.m_ID_cache[kvp.Value.EAGuid].StatsID >= 1)
                                                        {
                                                            String playerstatsSQL = @"INSERT INTO " + this.tbl_playerstats + @"(StatsID, Score, Kills, Headshots, Deaths, Suicide, TKs, Playtime, Rounds, FirstSeenOnServer, LastSeenOnServer, Killstreak, Deathstreak, HighScore , Wins, Losses)
																VALUES(@StatsID, @Score, @Kills, @Headshots, @Deaths, @Suicide, @TKs, @Playtime, @Rounds, @FirstSeenOnServer, @LastSeenOnServer, @Killstreak, @Deathstreak, @HighScore , @Wins, @Losses) 
                                                                ON DUPLICATE KEY UPDATE Score = Score + @Score, Kills = Kills + @Kills,Headshots = Headshots + @Headshots, Deaths = Deaths + @Deaths, Suicide = Suicide + @Suicide, TKs = TKs + @TKs, Playtime = Playtime + @Playtime, Rounds = Rounds + @Rounds, LastSeenOnServer = @LastSeenOnServer, Killstreak = GREATEST(Killstreak,@Killstreak),Deathstreak = GREATEST(Deathstreak, @Deathstreak) ,HighScore = GREATEST(HighScore, @HighScore), Wins = Wins + @Wins, Losses = Losses + @Losses ";

                                                            MySqlConn.Execute(playerstatsSQL, new
                                                            {
                                                                StatsID = this.m_ID_cache[kvp.Value.EAGuid].StatsID,
                                                                Score = kvp.Value.TotalScore,
                                                                Kills = kvp.Value.Kills,
                                                                Headshots = kvp.Value.Headshots,
                                                                Deaths = (kvp.Value.Deaths >= 0) ? kvp.Value.Deaths : 0,
                                                                Suicide = kvp.Value.Suicides,
                                                                TKs = kvp.Value.Teamkills,
                                                                Playtime = kvp.Value.TotalPlaytime,
                                                                Rounds = 1,
                                                                FirstSeenOnServer = kvp.Value.TimePlayerjoined,
                                                                LastSeenOnServer = (kvp.Value.TimePlayerleft != DateTime.MinValue) ? kvp.Value.TimePlayerleft : MyDateTime.Now,
                                                                Killstreak = kvp.Value.Killstreak,
                                                                Deathstreak = kvp.Value.Deathstreak,
                                                                HighScore = kvp.Value.TotalScore,
                                                                Wins = kvp.Value.Wins,
                                                                Losses = kvp.Value.Losses
                                                            }, transaction: MySqlTrans);

                                                            if (this.m_weaponstatsON == enumBoolYesNo.Yes)
                                                            {
                                                                this.DebugInfo("Trace", "Weaponstats Write querys");

                                                                String NewWeaponStatsSQL = @"INSERT INTO `" + this.tbl_weapons_stats + @"` (`StatsID`,`WeaponID`,`Kills`,`Headshots`,`Deaths`)
                                                                                         VALUES(@StatsID, @WeaponID, @Kills, @Headshots, @Deaths)
                                                                                         ON DUPLICATE KEY UPDATE  `Kills` = `Kills` + @Kills ,`Headshots` = `Headshots` + @Headshots,`Deaths` = `Deaths` + @Deaths";

                                                                foreach (KeyValuePair<String, Dictionary<String, CStats.CUsedWeapon>> branch in tempdic)
                                                                {
                                                                    //Build Query for Weaponstats
                                                                    if (tempdic != null)
                                                                    {
                                                                        foreach (KeyValuePair<String, CStats.CUsedWeapon> leaf in branch.Value)
                                                                        {
                                                                            if (leaf.Value.Kills != 0 || leaf.Value.Kills != 0 || leaf.Value.Deaths != 0)
                                                                            {
                                                                                if (this.WeaponMappingDic.ContainsKey(leaf.Value.Name))
                                                                                {
                                                                                    MySqlConn.Execute(NewWeaponStatsSQL, new
                                                                                    {
                                                                                        StatsID = this.m_ID_cache[kvp.Value.EAGuid].StatsID,
                                                                                        WeaponID = this.WeaponMappingDic[leaf.Value.Name],
                                                                                        Kills = leaf.Value.Kills,
                                                                                        Headshots = leaf.Value.Headshots,
                                                                                        Deaths = leaf.Value.Deaths
                                                                                    }, transaction: MySqlTrans);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }

                                                            //Awards
                                                            /*
                                                            if (this.m_awardsON == enumBoolYesNo.Yes && StatsTrackerCopy[kvp.Key].Awards.DicAwards.Count > 0)
                                                            {
                                                                string awardsInsert = "INSERT INTO " + tbl_awards + @" (`AwardID` ";
                                                                string awardsValues = ") VALUES (" + int_id;
                                                                string awardsUpdate = " ON DUPLICATE KEY UPDATE ";
                                                                foreach (KeyValuePair<string, int> award in StatsTrackerCopy[kvp.Key].Awards.DicAwards)
                                                                {
                                                                    awardsInsert = String.Concat(awardsInsert, ", `", award.Key, "`");
                                                                    awardsValues = String.Concat(awardsValues, ", ", award.Value.ToString());
                                                                    awardsUpdate = String.Concat(awardsUpdate, " `", award.Key, "` = `", award.Key, "` + ", award.Value.ToString(), ", ");
                                                                }
                                                                // Remove the last comma
                                                                int charindex2 = awardsUpdate.LastIndexOf(",");
                                                                if (charindex2 > 0)
                                                                {
                                                                    awardsUpdate = awardsUpdate.Remove(charindex2);
                                                                }
                                                                awardsInsert = String.Concat(awardsInsert, awardsValues, ") ", awardsUpdate);
                                                                //Sent Query to the Server
                                                                this.DebugInfo("Awardquery: " + awardsInsert);
                                                                using (MySqlCommand OdbcCom = new MySqlCommand(awardsInsert, OdbcConn, OdbcTrans))
                                                                {
                                                                    OdbcCom.ExecuteNonQuery();
                                                                }
                                                            }
                                                            */
                                                        }
                                                    }
                                                    MySqlTrans.Commit();
                                                    success = true;
                                                }
                                                catch (MySqlException ex)
                                                {
                                                    switch (ex.Number)
                                                    {
                                                        case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                                        case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                                            if (attemptCount < this.TransactionRetryCount)
                                                            {
                                                                this.DebugInfo("Warning", "Warning in StartStreaming: Locktimeout or Deadlock occured restarting Transaction(Stats). Attempt: " + attemptCount);
                                                                try
                                                                {
                                                                    MySqlTrans.Rollback();
                                                                }
                                                                catch { }
                                                                Thread.Sleep(attemptCount * 1000);
                                                            }
                                                            else
                                                            {
                                                                this.DebugInfo("Error", "Error in StartStreaming: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction Stats)");
                                                                throw;
                                                            }
                                                            break;
                                                        default:
                                                            throw; //Other exceptions
                                                    }
                                                }
                                            }
                                        }
                                        //Reset bool and counter
                                        attemptCount = 0;
                                        success = false;
                                        if (this.m_enLogPlayerDataOnly == enumBoolYesNo.No)
                                        {
                                            while (!success)
                                            {
                                                attemptCount++;
                                                try
                                                {
                                                    //Start of the Transaction
                                                    MySqlTrans = MySqlConn.BeginTransaction();
                                                    this.DebugInfo("Trace", "Dogtagstats Write querys");
                                                    String KnifeSQL = String.Empty;
                                                    foreach (KeyValuePair<CKillerVictim, Int32> kvp in m_dicKnifeKills)
                                                    {
                                                        if (StatsTrackerCopy.ContainsKey(kvp.Key.Killer) == false || StatsTrackerCopy.ContainsKey(kvp.Key.Victim) == false)
                                                        {
                                                            continue;
                                                        }
                                                        if (this.m_ID_cache.ContainsKey(StatsTrackerCopy[kvp.Key.Killer].EAGuid) == false || this.m_ID_cache.ContainsKey(StatsTrackerCopy[kvp.Key.Victim].EAGuid) == false)
                                                        {
                                                            continue;
                                                        }
                                                        if (this.m_ID_cache[StatsTrackerCopy[kvp.Key.Killer].EAGuid].StatsID > 0 && this.m_ID_cache[StatsTrackerCopy[kvp.Key.Victim].EAGuid].StatsID > 0)
                                                        {
                                                            KnifeSQL = "INSERT INTO " + this.tbl_dogtags + @"(KillerID, VictimID, Count) VALUES(@KillerID, @VictimID, @Count)
                            						ON DUPLICATE KEY UPDATE Count = Count + @Count";
                                                            MySqlConn.Execute(KnifeSQL, new
                                                            {
                                                                KillerID = this.m_ID_cache[StatsTrackerCopy[kvp.Key.Killer].EAGuid].StatsID,
                                                                VictimID = this.m_ID_cache[StatsTrackerCopy[kvp.Key.Victim].EAGuid].StatsID,
                                                                Count = m_dicKnifeKills[kvp.Key]
                                                            }, transaction: MySqlTrans);
                                                        }
                                                    }
                                                    MySqlTrans.Commit();
                                                    success = true;
                                                }
                                                catch (MySqlException ex)
                                                {
                                                    switch (ex.Number)
                                                    {
                                                        case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                                        case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                                            if (attemptCount < this.TransactionRetryCount)
                                                            {
                                                                this.DebugInfo("Warning", "Warning in StartStreaming: Locktimeout or Deadlock occured restarting Transaction(Dogtags). Attempt: " + attemptCount);
                                                                try
                                                                {
                                                                    MySqlTrans.Rollback();
                                                                }
                                                                catch { }
                                                                Thread.Sleep(attemptCount * 1000);
                                                            }
                                                            else
                                                            {
                                                                this.DebugInfo("Error", "Error in StartStreaming: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction Dogtags)");
                                                                throw;
                                                            }
                                                            break;
                                                        default:
                                                            throw; //Other exceptions
                                                    }
                                                }
                                            }
                                        }
                                        //Reset bool and counter
                                        attemptCount = 0;
                                        success = false;

                                        while (!success)
                                        {
                                            attemptCount++;
                                            try
                                            {
                                                //Start of the Transaction
                                                MySqlTrans = MySqlConn.BeginTransaction();

                                                //Write the Player Sessions

                                                if (this.m_sessionON == enumBoolYesNo.Yes && this.m_enSessionTracking == enumBoolYesNo.Yes)
                                                {
                                                    Boolean containsvaildsessions = false;
                                                    StringBuilder InsertSQLSession = new StringBuilder(500);
                                                    InsertSQLSession.Append(@"INSERT INTO " + this.tbl_sessions + @" (`StatsID`, `StartTime`,`EndTime`, `Score`, `Kills`, `Headshots`, `Deaths`, `TKs`, `Suicide`,`RoundCount`, `Playtime`, `HighScore`, `Killstreak`, `Deathstreak`, `Wins`, `Losses`) VALUES");

                                                    this.DebugInfo("Trace", this.lstpassedSessions.Count + " Sessions to write to Sessiontable");
                                                    Int32 i = 0;
                                                    foreach (CStats session in this.lstpassedSessions)
                                                    {
                                                        if (this.m_ID_cache.ContainsKey(session.EAGuid) == true)
                                                        {
                                                            if (this.m_ID_cache[session.EAGuid].StatsID > 0)
                                                            {
                                                                //write session
                                                                containsvaildsessions = true;
                                                                InsertSQLSession.Append(" (@StatsID" + i + ", @StartTime" + i + ", @EndTime" + i + ", @Score" + i + ", @Kills" + i + ", @Headshots" + i + ", @Deaths" + i + ", @TKs" + i + ", @Suicide" + i + ", @RoundCount" + i + ", @Playtime" + i + ", @HighScore" + i + ", @Killstreak" + i + ", @Deathstreak" + i + ", @Wins" + i + ", @Losses" + i + "),");
                                                                i++;
                                                            }
                                                        }
                                                    }
                                                    if (containsvaildsessions)
                                                    {
                                                        //remove last comma
                                                        InsertSQLSession.Length = InsertSQLSession.Length - 1;
                                                        {
                                                            DynamicParameters sessionParams = new DynamicParameters();
                                                            i = 0;
                                                            foreach (CStats session in this.lstpassedSessions)
                                                            {
                                                                if (this.m_ID_cache.ContainsKey(session.EAGuid) == true)
                                                                {
                                                                    if (this.m_ID_cache[session.EAGuid].StatsID > 0)
                                                                    {
                                                                        sessionParams.Add("StatsID" + i, this.m_ID_cache[session.EAGuid].StatsID);
                                                                        sessionParams.Add("StartTime" + i, session.TimePlayerjoined);
                                                                        sessionParams.Add("EndTime" + i, session.TimePlayerleft);
                                                                        sessionParams.Add("Score" + i, session.TotalScore);
                                                                        sessionParams.Add("Kills" + i, session.Kills);
                                                                        sessionParams.Add("Headshots" + i, session.Headshots);
                                                                        sessionParams.Add("Deaths" + i, (session.Deaths >= 0) ? session.Deaths : 0);
                                                                        sessionParams.Add("TKs" + i, session.Teamkills);
                                                                        sessionParams.Add("Suicide" + i, session.Suicides);
                                                                        sessionParams.Add("RoundCount" + i, session.Rounds);
                                                                        sessionParams.Add("Playtime" + i, session.TotalPlaytime);
                                                                        sessionParams.Add("HighScore" + i, session.HighScore);
                                                                        sessionParams.Add("Killstreak" + i, session.Killstreak);
                                                                        sessionParams.Add("Deathstreak" + i, session.Deathstreak);
                                                                        sessionParams.Add("Wins" + i, session.Wins);
                                                                        sessionParams.Add("Losses" + i, session.Losses);
                                                                        i++;
                                                                    }
                                                                }
                                                            }
                                                            MySqlConn.Execute(InsertSQLSession.ToString(), sessionParams, transaction: MySqlTrans);
                                                            this.lstpassedSessions.Clear();
                                                        }
                                                    }
                                                }
                                                //Commit the Transaction for the Playerstats
                                                MySqlTrans.Commit();
                                                success = true;
                                            }
                                            catch (MySqlException ex)
                                            {
                                                switch (ex.Number)
                                                {
                                                    case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                                    case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                                        if (attemptCount < this.TransactionRetryCount)
                                                        {
                                                            this.DebugInfo("Warning", "Warning in StartStreaming: Locktimeout or Deadlock occured restarting Transaction(Sessions). Attempt: " + attemptCount);
                                                            try
                                                            {
                                                                MySqlTrans.Rollback();
                                                            }
                                                            catch { }
                                                            Thread.Sleep(attemptCount * 1000);
                                                        }
                                                        else
                                                        {
                                                            this.DebugInfo("Error", "Error in StartStreaming: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction Sessions)");
                                                            throw;
                                                        }
                                                        break;
                                                    default:
                                                        throw; //Other exceptions
                                                }
                                            }
                                        }
                                        //Reset bool and counter
                                        attemptCount = 0;
                                        success = false;

                                        //Calculate ServerStats
                                        if (this.boolSkipServerStatsUpdate == false && this.m_enLogPlayerDataOnly == enumBoolYesNo.No)
                                        {
                                            this.DebugInfo("Trace", "Serverstats Write query");
                                            while (!success)
                                            {
                                                attemptCount++;
                                                try
                                                {
                                                    MySqlTrans = MySqlConn.BeginTransaction();
                                                    String serverstats = @"REPLACE INTO " + this.tbl_server_stats + @" SELECT tsp.ServerID, Count(*) AS CountPlayers, SUM(tps.Score) AS SumScore, AVG(tps.Score) AS AvgScore, SUM(tps.Kills) AS SumKills,  AVG(tps.Kills) AS AvgKills, SUM(tps.Headshots) AS SumHeadshots,
                                                        AVG(tps.Headshots) AS AvgHeadshots, SUM(tps.Deaths) AS SumDeaths, AVG(tps.Deaths) AS AvgDeaths, SUM(tps.Suicide) AS SumSuicide, AVG(tps.Suicide) AS AvgSuicide, SUM(tps.TKs) AS SumTKs, AVG(tps.TKs) AS AvgTKs,
                                                        SUM(tps.Playtime) AS SumPlaytime, AVG(tps.Playtime) AS AvgPlaytime, SUM(tps.Rounds) AS SumRounds, AVG(tps.Rounds) AS AvgRounds 
                                                        FROM " + this.tbl_playerstats + @" tps
                                                        INNER JOIN " + this.tbl_server_player + @" tsp ON tps.StatsID = tsp.StatsID
                                                        WHERE tsp.ServerID = @ServerID GROUP BY tsp.ServerID";
                                                    MySqlConn.Execute(serverstats, new { ServerID = this.ServerID }, transaction: MySqlTrans);
                                                    MySqlTrans.Commit();
                                                    success = true;
                                                }
                                                catch (MySqlException ex)
                                                {
                                                    switch (ex.Number)
                                                    {
                                                        case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                                        case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                                            if (attemptCount < this.TransactionRetryCount)
                                                            {
                                                                this.DebugInfo("Warning", "Warning in StartStreaming: Locktimeout or Deadlock occured restarting Transaction(Serverstats). Attempt: " + attemptCount);
                                                                try
                                                                {
                                                                    MySqlTrans.Rollback();
                                                                }
                                                                catch { }
                                                                Thread.Sleep(attemptCount * 1000);
                                                            }
                                                            else
                                                            {
                                                                this.DebugInfo("Error", "Error in StartStreaming: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction Serverstats)");
                                                                throw;
                                                            }
                                                            break;
                                                        default:
                                                            throw; //Other exceptions
                                                    }
                                                }
                                            }
                                        }
                                        //Reset bool and counter
                                        attemptCount = 0;
                                        success = false;

                                        StatsTrackerCopy.Clear();
                                        this.m_dicKnifeKills.Clear();

                                        List<String> leftplayerlist = new List<String>();

                                        foreach (KeyValuePair<String, C_ID_Cache> kvp in this.m_ID_cache)
                                        {
                                            if (this.m_ID_cache[kvp.Key].PlayeronServer == false)
                                            {
                                                leftplayerlist.Add(kvp.Key);
                                            }
                                            // Because so playerleft event seems not been reported by the server
                                            this.m_ID_cache[kvp.Key].PlayeronServer = false;
                                        }
                                        foreach (String player in leftplayerlist)
                                        {
                                            this.m_ID_cache.Remove(player);
                                            //this.DebugInfo("Removed " + player);
                                        }
                                        this.DebugInfo("Info", "Status ID-Cache: " + m_ID_cache.Count + " ID's in cache");
                                        if (this.m_ID_cache.Count > 500)
                                        {
                                            this.DebugInfo("Warning", "Forced Cache clear due the Nummber of cached IDs reached over 500 entries(overflowProtection)");
                                            this.m_ID_cache.Clear();
                                        }
                                    }
                                    else
                                    {
                                        StatsTracker.Clear();
                                    }
                                }
                                catch (MySqlException oe)
                                {
                                    this.DebugInfo("Error", "Error in Startstreaming: ");
                                    this.DisplayMySqlErrorCollection(oe);
                                    this.m_ID_cache.Clear();
                                    this.m_dicKnifeKills.Clear();
                                    try { MySqlTrans.Rollback(); }
                                    catch { }
                                }
                                catch (Exception c)
                                {
                                    this.DebugInfo("Error", "Error in Startstreaming: " + c);
                                    this.m_ID_cache.Clear();
                                    this.m_dicKnifeKills.Clear();
                                    try { MySqlTrans.Rollback(); }
                                    catch { }
                                }
                                finally
                                {
                                    StatsTrackerCopy = null;
                                    this.Mapstats = this.Nextmapinfo;
                                    this.MySql_Connection_is_activ = false;
                                    this.CloseMySqlConnection(2);

                                    //Update Serverranking
                                    this.UpdateRanking();
                                    //Welcomestats dic
                                    this.checkWelcomeStatsDic();
                                    if (GlobalDebugMode.Equals("Info"))
                                    {
                                        TimeSpan duration = MyDateTime.Now - StartStreamingTime;
                                        this.DebugInfo("Info", "Streamingprocess duration: " + Math.Round(duration.TotalSeconds, 3) + " seconds");
                                    }
                                }
                            }
                            else
                            {
                                this.DebugInfo("Error", "Streaming cancelled.  Please enter all database information");
                            }
                        }
                    }
                }
                catch (MySqlException oe)
                {
                    this.DebugInfo("Error", "Error in Startstreaming OuterException: ");
                    this.DisplayMySqlErrorCollection(oe);
                    this.m_ID_cache.Clear();
                    this.m_dicKnifeKills.Clear();
                }
                catch (Exception c)
                {
                    this.DebugInfo("Error", "Error in Startstreaming OuterException: " + c);
                    this.m_ID_cache.Clear();
                    this.m_dicKnifeKills.Clear();
                }
            }
        }

        private void WelcomeStats(String strSpeaker)
        {
            List<String> result = new List<String>();
            if (this.m_enWelcomeStats == enumBoolYesNo.Yes)
            {
                if (this.m_enLogSTATS == enumBoolYesNo.Yes)
                {
                    String SQL = String.Empty;
                    String strMSG = String.Empty;
                    //Statsquery with KDR
                    //Rankquery
                    if (m_enRankingByScore == enumBoolYesNo.Yes)
                    {
                        if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                        {
                            SQL = @"SELECT tpd.SoldierName, SUM(tps.Score) AS Score, SUM(tps.Kills) AS Kills, SUM(tps.Deaths) AS Deaths, SUM(tps.Suicide) AS Suicide, SUM(tps.TKs) AS TKs, tpr.rankScore AS RankScore, (SELECT SUM(tss.CountPlayers) FROM " + this.tbl_server_stats + @" tss INNER JOIN " + this.tbl_server + @" ts ON tss.ServerID = ts.ServerID AND ServerGroup = @ServerGroup GROUP BY ts.ServerGroup ) AS allrank , SUM(tps.Playtime) AS Playtime, SUM(tps.Headshots) AS Headshots, 
                                    SUM(tps.Rounds) AS Rounds, MAX(tps.Killstreak) AS Killstreak, MAX(tps.Deathstreak) AS Deathstreak   
                                    FROM " + this.tbl_playerdata + @" tpd
                                    INNER JOIN " + this.tbl_playerrank + @" tpr ON tpd.PlayerID = tpr.PlayerID AND tpr.ServerGroup = @ServerGroup 
                                    INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.PlayerID = tpd.PlayerID
                                    INNER JOIN " + this.tbl_playerstats + @" tps ON tsp.StatsID = tps.StatsID
                                    WHERE tpd.SoldierName = @SoldierName
                                    GROUP BY tpd.PlayerID";
                        }
                        else
                        {
                            SQL = @"SELECT tpd.SoldierName AS SoldierName, tps.Score AS Score, tps.Kills AS Kills, tps.Deaths AS Deaths, tps.Suicide AS Suicide, tps.TKs AS TKs, tps.rankScore AS RankScore, (SELECT tss.CountPlayers FROM " + this.tbl_server_stats + @" tss WHERE ServerID = @ServerID ) AS allrank ,
                                    tps.Playtime AS Playtime, tps.Headshots AS Headshots, tps.Rounds AS Rounds, tps.Killstreak AS Killstreak, tps.Deathstreak AS Deathstreak
                                    FROM " + this.tbl_playerdata + @" tpd
                                    INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.PlayerID = tpd.PlayerID
                                    INNER JOIN " + this.tbl_playerstats + @" tps ON tps.StatsID = tsp.StatsID
                                    WHERE  tsp.ServerID = @ServerID AND tpd.SoldierName = @SoldierName";
                        }
                    }
                    else
                    {
                        if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                        {
                            SQL = @"SELECT tpd.SoldierName, SUM(tps.Score) AS Score, SUM(tps.Kills) AS Kills, SUM(tps.Deaths) AS Deaths, SUM(tps.Suicide) AS Suicide, SUM(tps.TKs) AS TKs, tpr.rankKills AS RankScore, (SELECT SUM(tss.CountPlayers) FROM " + this.tbl_server_stats + @" tss INNER JOIN " + this.tbl_server + @" ts ON tss.ServerID = ts.ServerID AND ServerGroup = @ServerGroup GROUP BY ts.ServerGroup ) AS allrank , SUM(tps.Playtime) AS Playtime, SUM(tps.Headshots) AS Headshots, 
                                    SUM(tps.Rounds) AS Rounds, MAX(tps.Killstreak) AS Killstreak, MAX(tps.Deathstreak) AS Deathstreak   
                                    FROM " + this.tbl_playerdata + @" tpd
                                    INNER JOIN " + this.tbl_playerrank + @" tpr ON tpd.PlayerID = tpr.PlayerID AND tpr.ServerGroup = @ServerGroup 
                                    INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.PlayerID = tpd.PlayerID
                                    INNER JOIN " + this.tbl_playerstats + @" tps ON tsp.StatsID = tps.StatsID
                                    WHERE tpd.SoldierName = @SoldierName
                                    GROUP BY tpd.PlayerID";
                        }
                        else
                        {
                            SQL = @"SELECT tpd.SoldierName AS SoldierName, tps.Score AS Score, tps.Kills AS Kills, tps.Deaths AS Deaths, tps.Suicide AS Suicide, tps.TKs AS TKs, tps.rankKills AS RankScore, (SELECT tss.CountPlayers FROM " + this.tbl_server_stats + @" tss WHERE ServerID = @ServerID ) AS allrank ,
                                    tps.Playtime AS Playtime, tps.Headshots AS Headshots, tps.Rounds AS Rounds, tps.Killstreak AS Killstreak, tps.Deathstreak AS Deathstreak
                                    FROM " + this.tbl_playerdata + @" tpd
                                    INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.PlayerID = tpd.PlayerID
                                    INNER JOIN " + this.tbl_playerstats + @" tps ON tps.StatsID = tsp.StatsID
                                    WHERE  tsp.ServerID = @ServerID AND tpd.SoldierName = @SoldierName";
                        }
                    }
                    {
                        DataTable resultTable;
                        Double kdr = 0;
                        DynamicParameters dynParams = new DynamicParameters();
                        if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                        {
                            dynParams.Add("ServerGroup", this.intServerGroup);
                            dynParams.Add("SoldierName", strSpeaker);
                        }
                        else
                        {
                            dynParams.Add("ServerID", ServerID);
                            dynParams.Add("SoldierName", strSpeaker);
                        }
                        try
                        {
                            resultTable = this.SQLquery(SQL, dynParams);
                            if (resultTable.Rows != null)
                            {
                                foreach (DataRow row in resultTable.Rows)
                                {
                                    result = new List<String>(this.m_lstPlayerWelcomeStatsMessage);
                                    result = this.ListReplace(result, "%serverName%", this.serverName);
                                    result = this.ListReplace(result, "%playerName%", row["SoldierName"].ToString());
                                    result = this.ListReplace(result, "%playerScore%", row["Score"].ToString());
                                    result = this.ListReplace(result, "%playerKills%", row["Kills"].ToString());
                                    result = this.ListReplace(result, "%playerDeaths%", row["Deaths"].ToString());
                                    result = this.ListReplace(result, "%playerSuicide%", row["Suicide"].ToString());
                                    result = this.ListReplace(result, "%playerTKs%", row["TKs"].ToString());
                                    result = this.ListReplace(result, "%playerRank%", row["RankScore"].ToString());
                                    result = this.ListReplace(result, "%allRanks%", row["allrank"].ToString());
                                    result = this.ListReplace(result, "%playerHeadshots%", row["Headshots"].ToString());
                                    result = this.ListReplace(result, "%rounds%", row["Rounds"].ToString());
                                    result = this.ListReplace(result, "%killstreak%", row["Killstreak"].ToString());
                                    result = this.ListReplace(result, "%deathstreak%", row["Deathstreak"].ToString());
                                    //KDR
                                    if (Convert.ToInt32(row["Deaths"]) != 0)
                                    {
                                        kdr = Convert.ToDouble(row["Kills"]) / Convert.ToDouble(row["Deaths"]);
                                        kdr = Math.Round(kdr, 2);
                                        result = this.ListReplace(result, "%playerKDR%", kdr.ToString());
                                    }
                                    else
                                    {
                                        kdr = Convert.ToDouble(row["Kills"]);
                                        result = this.ListReplace(result, "%playerKDR%", kdr.ToString());
                                    }
                                    //Playtime
                                    TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(row["Playtime"]));
                                    result = this.ListReplace(result, "%playerPlaytime%", span.ToString());
                                    //SPM
                                    Double SPM;
                                    if (Convert.ToDouble(row["Playtime"]) != 0)
                                    {
                                        SPM = (Convert.ToDouble(row["Score"]) / (Convert.ToDouble(row["Playtime"]) / 60));
                                        SPM = Math.Round(SPM, 2);
                                        result = this.ListReplace(result, "%SPM%", SPM.ToString());
                                    }
                                    else
                                    {
                                        result = this.ListReplace(result, "%SPM%", "0");
                                    }
                                }
                            }
                        }
                        catch (Exception c)
                        {
                            this.DebugInfo("Error", "WelcomeStats: " + c);
                        }
                    }
                    if (result.Count > 0)
                    {
                        //result.Insert(0, m_strPlayerWelcomeMsg.Replace("%serverName%", this.serverName).Replace("%playerName%", strSpeaker));
                    }
                    else
                    {
                        result.Clear();
                        result = new List<String>(this.m_lstNewPlayerWelcomeMsg);
                        result = this.ListReplace(result, "%serverName%", this.serverName);
                        result = this.ListReplace(result, "%playerName%", strSpeaker);
                        //result.Add(m_strNewPlayerWelcomeMsg.Replace("%serverName%", this.serverName).Replace("%playerName%", strSpeaker));
                    }
                    this.SendMultiLineChatMessage(result, int_welcomeStatsDelay, 0, "player", strSpeaker);
                }
            }
        }

        private void GetPlayerStats(String strSpeaker, Int32 delay, String scope)
        {
            List<String> result = new List<String>();
            if (this.m_enLogSTATS == enumBoolYesNo.Yes)
            {
                String SQL = String.Empty;
                String strMSG = String.Empty;
                Double kdr = 0;
                //Statsquery with KDR
                //Rankquery
                if (m_enRankingByScore == enumBoolYesNo.Yes)
                {
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        SQL = @"SELECT tpd.SoldierName, SUM(tps.Score) AS Score, SUM(tps.Kills) AS Kills, SUM(tps.Deaths) AS Deaths, SUM(tps.Suicide) AS Suicide, SUM(tps.TKs) AS TKs, tpr.rankScore AS RankScore, (SELECT SUM(tss.CountPlayers) FROM " + this.tbl_server_stats + @" tss INNER JOIN " + this.tbl_server + @" ts ON tss.ServerID = ts.ServerID AND ServerGroup = @ServerGroup GROUP BY ts.ServerGroup ) AS allrank , SUM(tps.Playtime) AS Playtime, SUM(tps.Headshots) AS Headshots, 
                                SUM(tps.Rounds) AS Rounds, MAX(tps.Killstreak) AS Killstreak, MAX(tps.Deathstreak) AS Deathstreak   
                                FROM " + this.tbl_playerdata + @" tpd
                                INNER JOIN " + this.tbl_playerrank + @" tpr ON tpd.PlayerID = tpr.PlayerID AND tpr.ServerGroup = @ServerGroup 
                                INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.PlayerID = tpd.PlayerID
                                INNER JOIN " + this.tbl_playerstats + @" tps ON tsp.StatsID = tps.StatsID
                                WHERE tpd.SoldierName = @SoldierName
                                GROUP BY tpd.PlayerID";
                    }
                    else
                    {
                        SQL = @"SELECT tpd.SoldierName AS SoldierName, tps.Score AS Score, tps.Kills AS Kills, tps.Deaths AS Deaths, tps.Suicide AS Suicide, tps.TKs AS TKs, tps.rankScore AS RankScore, (SELECT tss.CountPlayers FROM " + this.tbl_server_stats + @" tss WHERE ServerID = @ServerID ) AS allrank ,
                                tps.Playtime AS Playtime, tps.Headshots AS Headshots, tps.Rounds AS Rounds, tps.Killstreak AS Killstreak, tps.Deathstreak AS Deathstreak
                                FROM " + this.tbl_playerdata + @" tpd
                                INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.PlayerID = tpd.PlayerID
                                INNER JOIN " + this.tbl_playerstats + @" tps ON tps.StatsID = tsp.StatsID
                                WHERE  tsp.ServerID = @ServerID AND tpd.SoldierName = @SoldierName";
                    }
                }
                else
                {
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        SQL = @"SELECT tpd.SoldierName, SUM(tps.Score) AS Score, SUM(tps.Kills) AS Kills, SUM(tps.Deaths) AS Deaths, SUM(tps.Suicide) AS Suicide, SUM(tps.TKs) AS TKs, tpr.rankKills AS RankScore, (SELECT SUM(tss.CountPlayers) FROM " + this.tbl_server_stats + @" tss INNER JOIN " + this.tbl_server + @" ts ON tss.ServerID = ts.ServerID AND ServerGroup = @ServerGroup GROUP BY ts.ServerGroup ) AS allrank , SUM(tps.Playtime) AS Playtime, SUM(tps.Headshots) AS Headshots, 
                                SUM(tps.Rounds) AS Rounds, MAX(tps.Killstreak) AS Killstreak, MAX(tps.Deathstreak) AS Deathstreak   
                                FROM " + this.tbl_playerdata + @" tpd
                                INNER JOIN " + this.tbl_playerrank + @" tpr ON tpd.PlayerID = tpr.PlayerID AND tpr.ServerGroup = @ServerGroup 
                                INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.PlayerID = tpd.PlayerID
                                INNER JOIN " + this.tbl_playerstats + @" tps ON tsp.StatsID = tps.StatsID
                                WHERE tpd.SoldierName = @SoldierName
                                GROUP BY tpd.PlayerID";
                    }
                    else
                    {
                        SQL = @"SELECT tpd.SoldierName AS SoldierName, tps.Score AS Score, tps.Kills AS Kills, tps.Deaths AS Deaths, tps.Suicide AS Suicide, tps.TKs AS TKs, tps.rankKills AS RankScore, (SELECT tss.CountPlayers FROM " + this.tbl_server_stats + @" tss WHERE ServerID = @ServerID ) AS allrank ,
                                tps.Playtime AS Playtime, tps.Headshots AS Headshots, tps.Rounds AS Rounds, tps.Killstreak AS Killstreak, tps.Deathstreak AS Deathstreak
                                FROM " + this.tbl_playerdata + @" tpd
                                INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.PlayerID = tpd.PlayerID
                                INNER JOIN " + this.tbl_playerstats + @" tps ON tps.StatsID = tsp.StatsID
                                WHERE  tsp.ServerID = @ServerID AND tpd.SoldierName = @SoldierName";
                    }
                }
                {
                    DataTable resultTable;
                    DynamicParameters dynParams = new DynamicParameters();
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        dynParams.Add("ServerGroup", this.intServerGroup);
                        dynParams.Add("SoldierName", strSpeaker);
                    }
                    else
                    {
                        dynParams.Add("ServerID", ServerID);
                        dynParams.Add("SoldierName", strSpeaker);
                    }
                    try
                    {
                        resultTable = this.SQLquery(SQL, dynParams);
                        if (resultTable.Rows != null)
                        {
                            foreach (DataRow row in resultTable.Rows)
                            {
                                result = new List<String>(m_lstPlayerStatsMessage);
                                result = this.ListReplace(result, "%playerName%", row["SoldierName"].ToString());
                                result = this.ListReplace(result, "%playerScore%", row["Score"].ToString());
                                result = this.ListReplace(result, "%playerKills%", row["Kills"].ToString());
                                result = this.ListReplace(result, "%playerDeaths%", row["Deaths"].ToString());
                                result = this.ListReplace(result, "%playerSuicide%", row["Suicide"].ToString());
                                result = this.ListReplace(result, "%playerTKs%", row["TKs"].ToString());
                                result = this.ListReplace(result, "%playerRank%", row["RankScore"].ToString());
                                result = this.ListReplace(result, "%allRanks%", row["allrank"].ToString());
                                result = this.ListReplace(result, "%playerHeadshots%", row["Headshots"].ToString());
                                result = this.ListReplace(result, "%rounds%", row["Rounds"].ToString());
                                result = this.ListReplace(result, "%killstreak%", row["Killstreak"].ToString());
                                result = this.ListReplace(result, "%deathstreak%", row["Deathstreak"].ToString());
                                //KDR
                                if (Convert.ToInt32(row["Deaths"]) != 0)
                                {
                                    kdr = Convert.ToDouble(row["Kills"]) / Convert.ToDouble(row["Deaths"]);
                                    kdr = Math.Round(kdr, 2);
                                    result = this.ListReplace(result, "%playerKDR%", kdr.ToString());
                                }
                                else
                                {
                                    kdr = Convert.ToDouble(row["Kills"]);
                                    result = this.ListReplace(result, "%playerKDR%", kdr.ToString());
                                }
                                //Playtime
                                TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(row["Playtime"]));
                                result = this.ListReplace(result, "%playerPlaytime%", span.ToString());
                                //SPM
                                Double SPM;
                                if (Convert.ToDouble(row["Playtime"]) != 0)
                                {
                                    SPM = (Convert.ToDouble(row["Score"]) / (Convert.ToDouble(row["Playtime"]) / 60));
                                    SPM = Math.Round(SPM, 2);
                                    result = this.ListReplace(result, "%SPM%", SPM.ToString());
                                }
                                else
                                {
                                    result = this.ListReplace(result, "%SPM%", "0");
                                }
                            }
                        }
                    }
                    catch (Exception c)
                    {
                        this.DebugInfo("Error", "GetPlayerStats: " + c);
                    }
                }
                if (result.Count != 0)
                {
                    this.SendMultiLineChatMessage(result, delay, 0, scope, strSpeaker);
                }
                else
                {
                    result.Clear();
                    result.Add("No Stats are available yet! Please wait one Round!");
                    this.SendMultiLineChatMessage(result, delay, 0, scope, strSpeaker);
                }
            }
        }

        private void GetTop10(String strSpeaker, Int32 delay, String scope)
        {
            List<String> result = new List<String>();
            if (this.m_enTop10ingame == enumBoolYesNo.Yes)
            {
                String SQL = String.Empty;
                Int32 rank = 0;
                //Top10 Query
                if (this.m_enRankingByScore == enumBoolYesNo.Yes)
                {
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        SQL = @"SELECT tpd.SoldierName, SUM(tps.Score) AS Score, SUM(tps.Kills) AS Kills, SUM(tps.Deaths) AS Deaths , SUM(tps.Headshots) AS Headshots  
                             FROM " + this.tbl_playerstats + @" tps
                             INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.StatsID = tps.StatsID
                             INNER JOIN " + this.tbl_playerdata + @" tpd ON tsp.PlayerID = tpd.PlayerID
                             INNER JOIN " + this.tbl_playerrank + @" tpr ON tpr.PlayerID = tsp.PlayerID
                             WHERE tpr.ServerGroup = @ServerGroup AND tpr.rankScore BETWEEN 1 AND 10
                             GROUP BY tsp.PlayerID 
                             ORDER BY tpr.rankScore ASC";
                    }
                    else
                    {
                        SQL = @"SELECT tpd.SoldierName, tps.Score, tps.Kills, tps.Deaths, tps.Headshots 
                             FROM " + this.tbl_playerstats + @" tps
                             INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.StatsID = tps.StatsID
                             INNER JOIN " + this.tbl_playerdata + @" tpd ON tsp.PlayerID = tpd.PlayerID
                             WHERE tsp.ServerID = @ServerID AND tps.rankScore BETWEEN 1 AND 10
                             ORDER BY tps.rankScore ASC";
                    }
                }
                else
                {
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        SQL = @"SELECT tpd.SoldierName, SUM(tps.Score) AS Score, SUM(tps.Kills) AS Kills, SUM(tps.Deaths) AS Deaths , SUM(tps.Headshots) AS Headshots  
                             FROM " + this.tbl_playerstats + @" tps
                             INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.StatsID = tps.StatsID
                             INNER JOIN " + this.tbl_playerdata + @" tpd ON tsp.PlayerID = tpd.PlayerID
                             INNER JOIN " + this.tbl_playerrank + @" tpr ON tpr.PlayerID = tsp.PlayerID
                             WHERE tpr.ServerGroup = @ServerGroup AND tpr.rankKills BETWEEN 1 AND 10
                             GROUP BY tsp.PlayerID 
                             ORDER BY tpr.rankKills ASC";
                    }
                    else
                    {
                        SQL = @"SELECT tpd.SoldierName, tps.Score, tps.Kills, tps.Deaths, tps.Headshots 
                             FROM " + this.tbl_playerstats + @" tps
                             INNER JOIN " + this.tbl_server_player + @" tsp ON  tsp.StatsID = tps.StatsID
                             INNER JOIN " + this.tbl_playerdata + @" tpd ON tsp.PlayerID = tpd.PlayerID
                             WHERE tsp.ServerID = @ServerID AND tps.rankKills BETWEEN 1 AND 10 
                             ORDER BY tps.rankKills ASC";
                    }
                }
                {
                    DynamicParameters dynParams = new DynamicParameters();
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        dynParams.Add("ServerGroup", this.intServerGroup);
                    }
                    else
                    {
                        dynParams.Add("ServerID", this.ServerID);
                    }
                    DataTable resultTable;
                    try
                    {
                        resultTable = this.SQLquery(SQL, dynParams);
                        result = new List<String>();
                        //Top 10 Header
                        result.Add(this.m_strTop10Header.Replace("%serverName%", this.serverName));
                        StringBuilder Top10Row = new StringBuilder();
                        Double kdr1;
                        Double khr;
                        if (resultTable.Rows != null)
                        {
                            foreach (DataRow row in resultTable.Rows)
                            {
                                Top10Row.Append(this.m_strTop10RowFormat);

                                if (Convert.ToDouble(row["Deaths"]) != 0)
                                {
                                    kdr1 = Convert.ToDouble(row["Kills"]) / Convert.ToDouble(row["Deaths"]);
                                    Top10Row.Replace("%playerKDR%", Math.Round(kdr1, 2).ToString());
                                }
                                else
                                {
                                    Top10Row.Replace("%playerKDR%", Convert.ToDouble(row["Kills"]).ToString());
                                }

                                if (Convert.ToDouble(row["Headshots"]) != 0)
                                {
                                    khr = Convert.ToDouble(row["Kills"]) / Convert.ToDouble(row["Headshots"]);
                                    khr = Math.Round(khr, 4);
                                    khr = khr * 100;
                                    Top10Row.Replace("%playerKHR%", khr.ToString());
                                }
                                else
                                {
                                    khr = 0;
                                }
                                rank = rank + 1;
                                Top10Row.Replace("%Rank%", rank.ToString()).Replace("%playerName%", row["SoldierName"].ToString()).Replace("%playerScore%", row["Score"].ToString()).Replace("%playerKills%", row["Kills"].ToString());
                                Top10Row.Replace("%playerDeaths%", row["Deaths"].ToString()).Replace("%playerHeadshots%", row["Headshots"].ToString());
                                result.Add(Top10Row.ToString());
                                Top10Row.Length = 0;
                            }
                        }
                    }
                    catch (Exception c)
                    {
                        this.DebugInfo("Error", "GetTop10: " + c);
                    }
                }
                if (result.Count > 0)
                {
                    this.SendMultiLineChatMessage(result, 0, delay, scope, strSpeaker);
                }
            }
        }

        private void GetWeaponStats(String strWeapon, String strPlayer, String scope)
        {

            this.DebugInfo("Trace", "GetWeaponStats: " + strPlayer + " " + strWeapon);
            Int32 delay = 0;
            String SQL = String.Empty;
            List<String> result = new List<String>();

            if (this.DamageClass.ContainsKey(strWeapon) == true)
            //if (this.WeaponMappingDic.ContainsKey(strWeapon) == true)
            {
                if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                {
                    SQL = @"SELECT `Kills`, `Headshots`, `Deaths`, ScoreRank, (SELECT COUNT(DISTINCT PlayerID) FROM " + this.tbl_server_player + @" tsp INNER JOIN " + this.tbl_server + @" ts  ON ts.ServerID = tsp.ServerID AND ts.ServerGroup = @ServerGroup) AS allrank
                            FROM (SELECT sub.PlayerID, (@num := @num + 1) AS ScoreRank, `Kills`, `Headshots`, `Deaths`
                                 FROM
                                 (SELECT tsp.PlayerID, SUM(`Kills`) AS `Kills`, SUM(`Headshots`) AS `Headshots`, SUM(`Deaths`) AS `Deaths`
                                 FROM " + this.tbl_weapons_stats + @" tw
                                 INNER JOIN " + this.tbl_server_player + @" tsp ON tw.StatsID = tsp.StatsID 
                                 INNER JOIN " + this.tbl_server + @" tserver ON tsp.ServerID = tserver.ServerID AND tserver.ServerGroup = @ServerGroup ,(SELECT @num := 0) x 
                                 WHERE tw.WeaponID = @WeaponID
                                 GROUP BY tsp.PlayerID
                                 ORDER BY `Kills` DESC, `Headshots` DESC) sub )sub2
                            INNER JOIN " + this.tbl_playerdata + @" tpd ON tpd.PlayerID = sub2.PlayerID
                            WHERE tpd.SoldierName = @SoldierName AND tpd.GameID = @GameID LIMIT 1";
                }
                else
                {
                    SQL = @"SELECT 
                            Kills,
                            Headshots,
                            Deaths,
                            (SELECT 
                                    ScoreRank
                                FROM
                                    (SELECT 
                                        @rownum:=@rownum + 1 AS ScoreRank, sub.PlayerID
                                    FROM
                                        (SELECT @rownum:=0) r, (SELECT 
                                        tsp.PlayerID
                                    FROM
                                         " + this.tbl_weapons_stats + @" tw
                                    INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.ServerID = @ServerID
                                        AND tw.StatsID = tsp.StatsID
                                    WHERE
                                        tw.WeaponID = @WeaponID
                                    ORDER BY tw.Kills DESC , tw.Headshots DESC) sub) sub2
                                WHERE
                                    PlayerID = tpd.PlayerID
                                LIMIT 1) AS ScoreRank,
                            (SELECT 
                                    COUNT(*)
                                FROM
                                    " + this.tbl_server_player + @"
                                WHERE
                                    ServerID = @ServerID) AS allrank
                        FROM
                            " + this.tbl_weapons_stats + @" tw
                                INNER JOIN
                            " + this.tbl_server_player + @" tsp ON tw.StatsID = tsp.StatsID
                                AND tsp.ServerID = @ServerID
                                INNER JOIN
                            " + this.tbl_playerdata + @" tpd ON tpd.PlayerID = tsp.PlayerID
                        WHERE
                            tpd.SoldierName = @SoldierName
                                AND tpd.GameID = @GameID
                                AND WeaponID = @WeaponID
                        LIMIT 1";
                }

                this.DebugInfo("Trace", "GetWeaponStats: Query:" + SQL);
                {
                    DynamicParameters dynParams = new DynamicParameters();
                    dynParams.Add("WeaponID", this.WeaponMappingDic[this.weaponDic[this.DamageClass[strWeapon]][strWeapon].Name]);
                    dynParams.Add("GameID", this.intServerGameType_ID);
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        dynParams.Add("SoldierName", strPlayer);
                        dynParams.Add("ServerGroup", this.intServerGroup);
                    }
                    else
                    {
                        dynParams.Add("ServerID", this.ServerID);
                        dynParams.Add("SoldierName", strPlayer);
                    }
                    try
                    {
                        DataTable resultTable = this.SQLquery(SQL, dynParams);
                        if (resultTable.Rows != null)
                        {
                            foreach (DataRow row in resultTable.Rows)
                            {
                                result = new List<String>(this.m_lstWeaponstatsMsg);
                                if (row[0] != Convert.DBNull || row[1] != Convert.DBNull || row[2] != Convert.DBNull)
                                {
                                    result = this.ListReplace(result, "%playerKills%", row[0].ToString());
                                    result = this.ListReplace(result, "%playerHeadshots%", row[1].ToString());
                                    result = this.ListReplace(result, "%playerDeaths%", row[2].ToString());
                                    result = this.ListReplace(result, "%playerRank%", row[3].ToString());
                                    result = this.ListReplace(result, "%allRanks%", row[4].ToString());

                                    Double khr = 0;
                                    if (Convert.ToDouble(row[0]) != 0)
                                    {
                                        khr = Convert.ToDouble(row[1]) / Convert.ToDouble(row[0]);
                                        khr = Math.Round(khr, 2);
                                        khr = khr * 100;
                                    }
                                    else
                                    {
                                        khr = 0;
                                    }
                                    Double kdr = 0;
                                    if (Convert.ToDouble(row[2]) != 0)
                                    {
                                        kdr = Convert.ToDouble(row[0]) / Convert.ToDouble(row[2]);
                                        kdr = Math.Round(kdr, 2);
                                    }
                                    else
                                    {
                                        kdr = Convert.ToDouble(row[2]);
                                    }
                                    result = this.ListReplace(result, "%playerKHR%", khr.ToString());
                                    result = this.ListReplace(result, "%playerKDR%", kdr.ToString());
                                }
                                else
                                {
                                    result.Clear();
                                }
                            }
                        }
                    }
                    catch (Exception c)
                    {
                        this.DebugInfo("Error", "GetWeaponStats: " + c);
                    }
                }
                result = this.ListReplace(result, "%playerName%", strPlayer);
                result = this.ListReplace(result, "%Weapon%", this.weaponDic[this.DamageClass[strWeapon]][strWeapon].FieldName);
                if (result.Count > 0)
                {
                    this.SendMultiLineChatMessage(result, delay, 0, scope, strPlayer);
                }
                else
                {
                    result.Clear();
                    result.Add("No Stats are available for this Weapon!!!");
                    this.SendMultiLineChatMessage(result, delay, 0, scope, strPlayer);
                }
            }
            else
            {
                result.Clear();
                result.Add("Specific Weapon not found!!");
                this.SendMultiLineChatMessage(result, delay, 0, scope, strPlayer);
            }
        }

        private void GetWeaponTop10(String strWeapon, String strPlayer, Int32 delay, String scope)
        {
            this.DebugInfo("Trace", "GetWeaponTop10: strWeapon = " + strWeapon);
            Int32 delaytop10 = 0;
            Double kdr = 0;
            Double khr = 0;
            Int32 rank = 0;
            String SQL = String.Empty;
            List<String> result = new List<String>();
            if (this.DamageClass.ContainsKey(strWeapon) == true)
            {
                //string tbl_weapons = "tbl_weapons_" + this.DamageClass[strWeapon].ToLower() + this.tableSuffix;
                if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                {
                    SQL = @"SELECT tpd.SoldierName, SUM(`Kills`) AS `Kills`, SUM(`Headshots`) AS `Headshots`, SUM(`Deaths`) AS `Deaths`
							FROM " + this.tbl_weapons_stats + @" tw
							INNER JOIN " + this.tbl_server_player + @" tsp ON tw.StatsID = tsp.StatsID
							INNER JOIN " + this.tbl_playerdata + @" tpd ON tpd.PlayerID = tsp.PlayerID
                            INNER JOIN " + this.tbl_server + @" ts ON ts.ServerID = tsp.ServerID
                            WHERE ts.ServerGroup = @ServerGroup AND tw.WeaponID = @WeaponID
							GROUP BY tsp.PlayerID
							ORDER BY SUM(`Kills`) DESC, SUM(`Headshots`) DESC
							LIMIT 10";
                }
                else
                {
                    SQL = @"SELECT tpd.SoldierName, `Kills`, `Headshots`, `Deaths`
							FROM " + this.tbl_weapons_stats + @" tw
							INNER JOIN " + this.tbl_server_player + @" tsp ON tw.StatsID = tsp.StatsID
							INNER JOIN " + this.tbl_playerdata + @" tpd ON tpd.PlayerID = tsp.PlayerID
							WHERE tsp.ServerID = @ServerID AND tw.WeaponID = @WeaponID
							ORDER BY `Kills` DESC, `Headshots` DESC
							LIMIT 10";
                }

                //SQL = SQL.Replace("%Weapon%", this.weaponDic[this.DamageClass[strWeapon]][strWeapon].FieldName);

                this.DebugInfo("Trace", "GetWeaponTop10: Query:" + SQL);

                {
                    DynamicParameters dynParams = new DynamicParameters();
                    dynParams.Add("WeaponID", this.WeaponMappingDic[this.weaponDic[this.DamageClass[strWeapon]][strWeapon].Name]);
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        dynParams.Add("ServerGroup", this.intServerGroup);
                    }
                    else
                    {
                        dynParams.Add("ServerID", this.ServerID);
                    }
                    DataTable resultTable;
                    try
                    {
                        result = new List<String>();
                        //result.Add("Top 10 Killers with %Weapon%");
                        result.Add(this.m_strWeaponTop10Header.Replace("%serverName%", this.serverName));
                        resultTable = this.SQLquery(SQL, dynParams);
                        StringBuilder Top10Row = new StringBuilder(this.m_strWeaponTop10RowFormat);
                        if (resultTable.Rows != null)
                        {
                            foreach (DataRow row in resultTable.Rows)
                            {
                                if (Convert.ToDouble(row[3]) != 0)
                                {
                                    kdr = Convert.ToDouble(row[1]) / Convert.ToDouble(row[3]);
                                    kdr = Math.Round(kdr, 2);
                                }
                                else
                                {
                                    kdr = Convert.ToDouble(row[1]);
                                }
                                if (Convert.ToDouble(row[1]) != 0)
                                {
                                    khr = Convert.ToDouble(row[2]) / Convert.ToDouble(row[1]);
                                    khr = Math.Round(khr, 4);
                                    khr = khr * 100;
                                }
                                else
                                {
                                    khr = 0;
                                }
                                Top10Row.Length = 0;
                                Top10Row.Append(this.m_strWeaponTop10RowFormat);
                                rank = rank + 1;
                                Top10Row.Replace("%Rank%", rank.ToString()).Replace("%playerName%", row[0].ToString()).Replace("%playerKills%", row[1].ToString()).Replace("%playerHeadshots%", row[2].ToString()).Replace("%playerDeaths%", row[3].ToString()).Replace("%playerKHR%", khr.ToString());
                                result.Add(Top10Row.ToString());
                            }
                        }
                    }
                    catch (Exception c)
                    {
                        this.DebugInfo("Error", "GetWeaponTop10: " + c);
                    }
                }
                result = this.ListReplace(result, "%Player%", strPlayer);
                result = this.ListReplace(result, "%Weapon%", this.weaponDic[this.DamageClass[strWeapon]][strWeapon].FieldName);
                if (result.Count > 0)
                {
                    this.SendMultiLineChatMessage(result, 0, delay, scope, strPlayer);
                }
                else
                {
                    result.Clear();
                    result.Add("No Stats are available for this Weapon!!!");
                    this.SendMultiLineChatMessage(result, 0, delay, scope, strPlayer);
                }
            }
            else
            {
                result.Clear();
                result.Add("Specific Weapon not found!!");
                this.SendMultiLineChatMessage(result, 0, delay, scope, strPlayer);
            }
        }

        private void GetDogtags(String strPlayer, Int32 delay, String scope)
        {
            Int32 delaydogtags = 0;
            String SQL = String.Empty;
            String SQL2 = String.Empty;
            if (this.m_enOverallRanking == enumBoolYesNo.Yes)
            {
                SQL = @"SELECT pd.SoldierName, SUM(dt.Count) AS Count
                        FROM " + this.tbl_server_player + @" sp
                        INNER JOIN " + this.tbl_server + @" ts ON ts.ServerID = sp.ServerID AND ts.ServerGroup = @ServerGroup 
                        INNER JOIN " + this.tbl_dogtags + @" dt ON sp.StatsID = dt.VictimID 
                        INNER JOIN " + this.tbl_playerdata + @" pd ON sp.PlayerID = pd.PlayerID
                        WHERE KillerID IN (SELECT StatsID AS KillerID FROM " + this.tbl_server_player + @" WHERE PlayerID = @KillerID)
                        GROUP BY pd.PlayerID ORDER BY Count DESC Limit 3";

                SQL2 = @"SELECT pd.SoldierName, SUM(dt.Count) AS Count
                         FROM " + this.tbl_server_player + @"  sp
                         INNER JOIN " + this.tbl_server + @" ts ON ts.ServerID = sp.ServerID AND ts.ServerGroup = @ServerGroup 
                         INNER JOIN " + this.tbl_dogtags + @"  dt ON sp.StatsID = dt.KillerID 
                         INNER JOIN " + this.tbl_playerdata + @"  pd ON sp.PlayerID = pd.PlayerID
                         WHERE VictimID IN (SELECT StatsID AS VictimID FROM " + this.tbl_server_player + @"  WHERE PlayerID = @VictimID)
                         GROUP BY pd.PlayerID ORDER BY Count DESC Limit 3";
            }
            else
            {
                SQL = @"SELECT pd.SoldierName, dt.Count
                        FROM " + this.tbl_server_player + @" sp
                        INNER JOIN " + this.tbl_dogtags + @" dt ON sp.StatsID = dt.VictimID 
                        INNER JOIN " + this.tbl_playerdata + @" pd ON sp.PlayerID = pd.PlayerID
                        WHERE KillerID = @KillerID AND sp.ServerID = @ServerID
                        ORDER BY Count DESC Limit 3";

                SQL2 = @"SELECT pd.SoldierName, dt.Count
                         FROM " + this.tbl_server_player + @" sp
                         INNER JOIN " + this.tbl_dogtags + @" dt ON sp.StatsID = dt.KillerID 
                         INNER JOIN " + this.tbl_playerdata + @" pd ON sp.PlayerID = pd.PlayerID
                         WHERE VictimID = @VictimID AND sp.ServerID = @ServerID
                         ORDER BY Count DESC Limit 3";
            }

            List<String> result = new List<String>();
            List<String> result2 = new List<String>();

            if (this.StatsTracker.ContainsKey(strPlayer) == false)
            {
                return;
            }

            {
                DynamicParameters dynParams = new DynamicParameters();
                if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                {
                    dynParams.Add("ServerGroup", this.intServerGroup);
                    dynParams.Add("KillerID", this.GetID(this.StatsTracker[strPlayer].EAGuid).Id);
                }
                else
                {
                    dynParams.Add("KillerID", this.GetID(this.StatsTracker[strPlayer].EAGuid).StatsID);
                    dynParams.Add("ServerID", this.ServerID);
                }
                try
                {
                    result = new List<String>();
                    result.Add("Your favorite Victims:");
                    DataTable resultTable = this.SQLquery(SQL, dynParams);
                    if (resultTable.Rows.Count > 0)
                    {
                        foreach (DataRow row in resultTable.Rows)
                        {
                            result.Add(" " + row["Count"] + "x  " + row["SoldierName"]);
                        }
                    }
                    else
                    {
                        result.Add("None - Get some dogtags!!");
                    }
                    resultTable.Dispose();
                }
                catch (Exception c)
                {
                    this.DebugInfo("Error", "GetDogtags: " + c);
                }
            }
            {
                DynamicParameters dynParams2 = new DynamicParameters();
                if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                {
                    dynParams2.Add("ServerGroup", this.intServerGroup);
                    dynParams2.Add("VictimID", this.GetID(this.StatsTracker[strPlayer].EAGuid).Id);
                }
                else
                {
                    dynParams2.Add("VictimID", this.GetID(this.StatsTracker[strPlayer].EAGuid).StatsID);
                    dynParams2.Add("ServerID", this.ServerID);
                }
                try
                {
                    result2 = new List<String>();
                    result2.Add("Your worst Enemies:");
                    DataTable resultTable = this.SQLquery(SQL2, dynParams2);
                    if (resultTable.Rows.Count > 0)
                    {
                        foreach (DataRow row in resultTable.Rows)
                        {
                            result2.Add(" " + row["Count"] + "x  " + row["SoldierName"]);
                        }
                    }
                    else
                    {
                        result2.Add("Nobody got your Tag yet!");
                    }
                    resultTable.Dispose();
                }
                catch (Exception c)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "Error in GetDogtags: " + c);
                }
            }
            this.CloseMySqlConnection(1);
            result.AddRange(result2);
            if (result[0].Equals("0") == false)
            {
                this.SendMultiLineChatMessage(result, delaydogtags, delay, scope, strPlayer);
            }
            else
            {
                result.Clear();
                result.Add("No Stats are available!!!");
                this.SendMultiLineChatMessage(result, delaydogtags, delay, scope, strPlayer);
            }
        }

        private void GetPlayerOfTheDay(String strSpeaker, Int32 delay, String scope)
        {
            List<String> result = new List<String>();
            if (this.m_enLogSTATS == enumBoolYesNo.Yes)
            {
                String SQL = String.Empty;

                String SQL_SELECT = @"SELECT 
                                tpd.SoldierName AS SoldierName,
                                SUM(ts.Score) AS Score, 
                                SUM(ts.Kills) AS Kills,
                                SUM(ts.Headshots) AS Headshots,
                                SUM(ts.Deaths) AS Deaths, 
                                SUM(ts.TKs) AS TKs,
                                SUM(ts.Suicide) AS Suicide,
                                SUM(ts.RoundCount ) AS RoundCount,
                                SUM(ts.Playtime) AS Playtime,
                                MAX(ts.Killstreak) AS Killstreak,
                                MAX(ts.Deathstreak) AS Deathstreak,
                                MAX(ts.HighScore) AS HighScore,
                                SUM(ts.Wins ) AS Wins,
                                SUM(ts.Losses ) AS Losses, ";


                String SQL_JOINS = @" FROM " + this.tbl_sessions + @" ts 
                                      INNER JOIN " + this.tbl_server_player + @" tsp USING(StatsID)
                                      INNER JOIN " + this.tbl_playerdata + @" tpd USING(PlayerID) ";

                String SQL_CONDS = String.Empty;

                String strMSG = String.Empty;
                Double kdr = 0;
                //Statsquery with KDR
                //Rankquery
                if (m_enRankingByScore == enumBoolYesNo.Yes)
                {
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        //Ranking by Score overall Server
                        SQL_SELECT = SQL_SELECT + @"(SELECT SUM(tss.CountPlayers) FROM " + this.tbl_server_stats + @" tss INNER JOIN " + this.tbl_server + @" ts ON tss.ServerID = ts.ServerID AND ServerGroup = @ServerGroup GROUP BY ts.ServerGroup ) AS allrank, tpr.rankScore AS ScoreRank ";

                        SQL_JOINS = SQL_JOINS + @" INNER JOIN " + this.tbl_server + @" ts2 USING(ServerID) 
                                                   INNER JOIN " + this.tbl_playerrank + @" tpr USING(PlayerID)";

                        SQL_CONDS = @" WHERE ts.StartTime >= CURRENT_DATE() AND ts2.ServerGroup = @ServerGroup
                                       Group BY tsp.StatsID
                                       ORDER BY SUM(ts.Score) DESC ";
                    }
                    else
                    {
                        //Ranking by Score specfic Server
                        SQL_SELECT = SQL_SELECT + @"(SELECT tss.CountPlayers FROM " + this.tbl_server_stats + @" tss WHERE ServerID = @ServerID ) AS allrank, tps.rankScore AS ScoreRank ";

                        SQL_JOINS = SQL_JOINS + @" INNER JOIN " + this.tbl_playerstats + @" tps USING(StatsID) ";

                        SQL_CONDS = @" WHERE ts.StartTime >= CURRENT_DATE() AND tsp.ServerID = @ServerID
                                       Group BY tsp.StatsID
                                       ORDER BY SUM(ts.Score) DESC ";
                    }
                }
                else
                {
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {

                        //Ranking by Kills overall Server
                        SQL_SELECT = SQL_SELECT + @"(SELECT SUM(tss.CountPlayers) FROM " + this.tbl_server_stats + @" tss INNER JOIN " + this.tbl_server + @" ts ON tss.ServerID = ts.ServerID AND ServerGroup = @ServerGroup GROUP BY ts.ServerGroup ) AS allrank, tpr.rankKills AS ScoreRank ";

                        SQL_JOINS = SQL_JOINS + @" INNER JOIN " + this.tbl_server + @" ts2 USING(ServerID)  
                                                   INNER JOIN " + this.tbl_playerrank + @" tpr USING(PlayerID)";

                        SQL_CONDS = @" WHERE ts.StartTime >= CURRENT_DATE() AND ts2.ServerGroup = @ServerGroup
                                       Group BY tsp.StatsID
                                       ORDER BY SUM(ts.Kills) DESC, SUM(ts.Deaths) ASC ";
                    }
                    else
                    {
                        //Ranking by Kills specfic Server
                        SQL_SELECT = SQL_SELECT + @"(SELECT tss.CountPlayers FROM " + this.tbl_server_stats + @" tss WHERE ServerID = @ServerID ) AS allrank , tps.rankKills AS ScoreRank  ";

                        SQL_JOINS = SQL_JOINS + @" INNER JOIN " + this.tbl_playerstats + @" tps USING(StatsID) ";

                        SQL_CONDS = @" WHERE ts.StartTime >= CURRENT_DATE() AND tsp.ServerID = @ServerID
                                       Group BY tsp.StatsID
                                       ORDER BY SUM(ts.Kills) DESC, SUM(ts.Deaths) ASC ";
                    }
                }
                //Add LIMIT
                SQL = SQL_SELECT + SQL_JOINS + SQL_CONDS + @" LIMIT 1";
                {
                    DataTable resultTable;
                    DynamicParameters dynParams = new DynamicParameters();
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        dynParams.Add("ServerGroup", this.intServerGroup);
                    }
                    else
                    {
                        dynParams.Add("ServerID", this.ServerID);
                    }
                    try
                    {
                        resultTable = this.SQLquery(SQL, dynParams);
                        if (resultTable.Rows != null)
                        {
                            foreach (DataRow row in resultTable.Rows)
                            {
                                result = new List<String>(m_lstPlayerOfTheDayMessage);
                                result = this.ListReplace(result, "%playerName%", row["SoldierName"].ToString());
                                result = this.ListReplace(result, "%playerScore%", row["Score"].ToString());
                                result = this.ListReplace(result, "%playerKills%", row["Kills"].ToString());
                                result = this.ListReplace(result, "%playerDeaths%", row["Deaths"].ToString());
                                result = this.ListReplace(result, "%playerSuicide%", row["Suicide"].ToString());
                                result = this.ListReplace(result, "%playerTKs%", row["TKs"].ToString());
                                result = this.ListReplace(result, "%playerRank%", row["ScoreRank"].ToString());
                                result = this.ListReplace(result, "%allRanks%", row["allrank"].ToString());
                                result = this.ListReplace(result, "%playerHeadshots%", row["Headshots"].ToString());
                                result = this.ListReplace(result, "%rounds%", row["RoundCount"].ToString());
                                result = this.ListReplace(result, "%killstreak%", row["Killstreak"].ToString());
                                result = this.ListReplace(result, "%deathstreak%", row["Deathstreak"].ToString());
                                //KDR
                                if (Convert.ToInt32(row["Deaths"]) != 0)
                                {
                                    kdr = Convert.ToDouble(row["Kills"]) / Convert.ToDouble(row["Deaths"]);
                                    kdr = Math.Round(kdr, 2);
                                    result = this.ListReplace(result, "%playerKDR%", kdr.ToString());
                                }
                                else
                                {
                                    kdr = Convert.ToDouble(row["Kills"]);
                                    result = this.ListReplace(result, "%playerKDR%", kdr.ToString());
                                }
                                //Playtime
                                TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(row["Playtime"]));
                                result = this.ListReplace(result, "%playerPlaytime%", span.ToString());
                                //SPM
                                Double SPM;
                                if (Convert.ToDouble(row["Playtime"]) != 0)
                                {
                                    SPM = (Convert.ToDouble(row["Score"]) / (Convert.ToDouble(row["Playtime"]) / 60));
                                    SPM = Math.Round(SPM, 2);
                                    result = this.ListReplace(result, "%SPM%", SPM.ToString());
                                }
                                else
                                {
                                    result = this.ListReplace(result, "%SPM%", "0");
                                }
                            }
                        }
                    }
                    catch (Exception c)
                    {
                        this.DebugInfo("Error", "GetPlayerOfTheDay: " + c);
                    }
                }
                if (result.Count != 0)
                {
                    this.SendMultiLineChatMessage(result, delay, 0, scope, strSpeaker);
                }
                else
                {
                    result.Clear();
                    result.Add("No Stats are available yet! Please wait one Round!");
                    this.SendMultiLineChatMessage(result, delay, 0, scope, strSpeaker);
                }
            }
        }

        private void GetTop10ForPeriod(String strSpeaker, Int32 delay, String scope, Int32 intdays)
        {
            List<String> result = new List<String>();
            if (this.m_enTop10ingame == enumBoolYesNo.Yes)
            {
                String SQL = @"SELECT 
                                tpd.SoldierName AS SoldierName,
                                SUM(ts.Score) AS Score, 
                                SUM(ts.Kills) AS Kills,
                                SUM(ts.Headshots) AS Headshots,
                                SUM(ts.Deaths) AS Deaths, 
                                SUM(ts.TKs) AS TKs,
                                SUM(ts.Suicide) AS Suicide,
                                SUM(ts.RoundCount ) AS RoundCount,
                                SUM(ts.Playtime) AS Playtime
                                FROM " + this.tbl_sessions + @" ts 
                                INNER JOIN " + this.tbl_server_player + @" tsp USING(StatsID)
                                INNER JOIN " + this.tbl_playerdata + @" tpd USING(PlayerID) ";
                Int32 rank = 0;
                //Top10 Query
                if (m_enRankingByScore == enumBoolYesNo.Yes)
                {
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        //Ranking by Score overall Server
                        SQL = SQL + @" INNER JOIN " + this.tbl_server + @" ts2 USING(ServerID)
                                       WHERE ts.StartTime >= DATE_SUB(CURRENT_DATE(),INTERVAL @DAYS DAY) AND ts2.ServerGroup = @ServerGroup
                                       Group BY tsp.StatsID
                                       ORDER BY SUM(ts.Score) DESC ";
                    }
                    else
                    {
                        //Ranking by Score specfic Server
                        SQL = SQL + @" WHERE ts.StartTime >= DATE_SUB(CURRENT_DATE(),INTERVAL @DAYS DAY) AND tsp.ServerID = @ServerID
                                       Group BY tsp.StatsID
                                       ORDER BY SUM(ts.Score) DESC ";
                    }
                }
                else
                {
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        //Ranking by Kills overall Server
                        SQL = SQL + @" INNER JOIN " + this.tbl_server + @" ts2 USING(ServerID)
                                       WHERE ts.StartTime >= DATE_SUB(CURRENT_DATE(),INTERVAL @DAYS DAY) AND ts2.ServerGroup = @ServerGroup
                                       Group BY tsp.StatsID
                                       ORDER BY SUM(ts.Kills) DESC, SUM(ts.Deaths) ASC ";
                    }
                    else
                    {
                        //Ranking by Kills specfic Server
                        SQL = SQL + @" WHERE ts.StartTime >= DATE_SUB(CURRENT_DATE(),INTERVAL @DAYS DAY) AND tsp.ServerID = @ServerID
                                       Group BY tsp.StatsID
                                       ORDER BY SUM(ts.Kills) DESC, SUM(ts.Deaths) ASC ";
                    }
                }
                //Add LIMIT
                SQL = SQL + @" LIMIT 10";

                {
                    DynamicParameters dynParams = new DynamicParameters();
                    dynParams.Add("DAYS", intdays);
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        dynParams.Add("ServerGroup", this.intServerGroup);
                    }
                    else
                    {
                        dynParams.Add("ServerID", this.ServerID);
                    }
                    DataTable resultTable;
                    try
                    {
                        resultTable = this.SQLquery(SQL, dynParams);
                        result = new List<String>();
                        //Top 10 Header
                        result.Add(this.m_strTop10HeaderForPeriod.Replace("%serverName%", this.serverName).Replace("%intervaldays%", intdays.ToString()));
                        StringBuilder Top10Row = new StringBuilder();
                        Double kdr1;
                        Double khr;
                        if (resultTable.Rows != null)
                        {
                            foreach (DataRow row in resultTable.Rows)
                            {
                                Top10Row.Append(this.m_strTop10RowFormat);

                                if (Convert.ToDouble(row["Deaths"]) != 0)
                                {
                                    kdr1 = Convert.ToDouble(row["Kills"]) / Convert.ToDouble(row["Deaths"]);
                                    Top10Row.Replace("%playerKDR%", Math.Round(kdr1, 2).ToString());
                                }
                                else
                                {
                                    Top10Row.Replace("%playerKDR%", Convert.ToDouble(row["Kills"]).ToString());
                                }

                                if (Convert.ToDouble(row["Headshots"]) != 0)
                                {
                                    khr = Convert.ToDouble(row["Kills"]) / Convert.ToDouble(row["Headshots"]);
                                    khr = Math.Round(khr, 4);
                                    khr = khr * 100;
                                    Top10Row.Replace("%playerKHR%", khr.ToString());
                                }
                                else
                                {
                                    khr = 0;
                                }
                                rank = rank + 1;
                                Top10Row.Replace("%Rank%", rank.ToString()).Replace("%playerName%", row["SoldierName"].ToString()).Replace("%playerScore%", row["Score"].ToString()).Replace("%playerKills%", row["Kills"].ToString());
                                Top10Row.Replace("%playerDeaths%", row["Deaths"].ToString()).Replace("%playerHeadshots%", row["Headshots"].ToString());
                                result.Add(Top10Row.ToString());
                                Top10Row.Length = 0;
                            }
                        }
                    }
                    catch (Exception c)
                    {
                        this.DebugInfo("Error", "GetTop10ForPeriod: " + c);
                    }
                }
                if (result.Count > 0)
                {
                    this.SendMultiLineChatMessage(result, 0, delay, scope, strSpeaker);
                }
            }
        }

        //Add to stats

        private void AddKillToStats(String strPlayerName, String DmgType, String weapon, Boolean headshot)
        {
            if (StatsTracker.ContainsKey(strPlayerName))
            {
                StatsTracker[strPlayerName].addKill(DmgType, weapon, headshot);
            }
            else
            {
                CStats newEntry = new CStats(String.Empty, 0, 0, 0, 0, 0, 0, 0, this.m_dTimeOffset, this.weaponDic);
                StatsTracker.Add(strPlayerName, newEntry);
                StatsTracker[strPlayerName].addKill(DmgType, weapon, headshot);
            }
            //Session
            if (m_dicSession.ContainsKey(strPlayerName) && this.m_sessionON == enumBoolYesNo.Yes)
            {
                m_dicSession[strPlayerName].addKill(DmgType, weapon, headshot);
            }
        }

        public void AddDeathToStats(String strPlayerName, String DmgType, String weapon)
        {
            if (StatsTracker.ContainsKey(strPlayerName))
            {
                StatsTracker[strPlayerName].addDeath(DmgType, weapon);
            }
            else
            {
                CStats newEntry = new CStats(String.Empty, 0, 0, 0, 0, 0, 0, 0, this.m_dTimeOffset, this.weaponDic);
                StatsTracker.Add(strPlayerName, newEntry);
                StatsTracker[strPlayerName].addDeath(DmgType, weapon);
            }

            //Session
            if (m_dicSession.ContainsKey(strPlayerName) && this.m_sessionON == enumBoolYesNo.Yes)
            {
                m_dicSession[strPlayerName].addDeath(DmgType, weapon);
            }
        }

        private void AddSuicideToStats(String strPlayerName, String DmgType, String weapon)
        {
            if (StatsTracker.ContainsKey(strPlayerName))
            {
                StatsTracker[strPlayerName].addDeath(DmgType, weapon);
                StatsTracker[strPlayerName].Suicides++;
            }
            else
            {
                CStats newEntry = new CStats(String.Empty, 0, 0, 0, 0, 1, 0, 0, this.m_dTimeOffset, this.weaponDic);
                StatsTracker.Add(strPlayerName, newEntry);
                StatsTracker[strPlayerName].addDeath(DmgType, weapon);
            }

            //Session
            if (m_dicSession.ContainsKey(strPlayerName) && this.m_sessionON == enumBoolYesNo.Yes)
            {
                m_dicSession[strPlayerName].addDeath(DmgType, weapon);
                m_dicSession[strPlayerName].Suicides++;
            }
        }

        private void AddTeamKillToStats(String strPlayerName)
        {
            if (StatsTracker.ContainsKey(strPlayerName))
            {
                StatsTracker[strPlayerName].Teamkills++;
            }
            else
            {
                CStats newEntry = new CStats(String.Empty, 0, 0, 0, 0, 0, 1, 0, this.m_dTimeOffset, this.weaponDic);
                StatsTracker.Add(strPlayerName, newEntry);
            }

            //Session
            if (m_dicSession.ContainsKey(strPlayerName) && this.m_sessionON == enumBoolYesNo.Yes)
            {
                m_dicSession[strPlayerName].Teamkills++;
            }
        }
        //Misc
        private void AddPBInfoToStats(CPunkbusterInfo cpbiPlayer)
        {
            if (StatsTracker.ContainsKey(cpbiPlayer.SoldierName))
            {
                StatsTracker[cpbiPlayer.SoldierName].Guid = cpbiPlayer.GUID;
                if (cpbiPlayer.PlayerCountryCode.Length <= 2)
                {
                    StatsTracker[cpbiPlayer.SoldierName].PlayerCountryCode = cpbiPlayer.PlayerCountryCode;
                }
                else
                {
                    StatsTracker[cpbiPlayer.SoldierName].PlayerCountryCode = "--";
                }
                if (StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined == null)
                    StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined = MyDateTime.Now;
            }
            else
            {
                CStats newEntry = new CStats(cpbiPlayer.GUID, 0, 0, 0, 0, 0, 0, 0, this.m_dTimeOffset, this.weaponDic);
                StatsTracker.Add(cpbiPlayer.SoldierName, newEntry);
                if (cpbiPlayer.PlayerCountryCode.Length <= 2)
                {
                    StatsTracker[cpbiPlayer.SoldierName].PlayerCountryCode = cpbiPlayer.PlayerCountryCode;
                }
                else
                {
                    StatsTracker[cpbiPlayer.SoldierName].PlayerCountryCode = "--";
                }
            }
        }

        private void PrepareKeywordDic()
        {
            if (boolKeywordDicReady == false)
            {
                this.DebugInfo("Trace", "PrepareKeywordDic: Preparing");
                this.m_dicKeywords.Clear();
                try
                {
                    foreach (KeyValuePair<String, String> kvp in this.DamageClass)
                    {
                        if (this.m_dicKeywords.ContainsKey(kvp.Key) == false)
                        {
                            this.m_dicKeywords.Add(kvp.Key, new List<String>());
                            this.m_dicKeywords[kvp.Key].Add(kvp.Key.ToUpper());

                            String[] weaponName = Regex.Replace(kvp.Key.Replace("Weapons/", "").Replace("Gadgets/", ""), @"XP\d_", "").Split('/');
                            String friendlyname = weaponName[0].Replace(' ', '_').Replace(".", "").Replace("U_", "").ToUpper();
                            if (this.m_dicKeywords.ContainsKey(friendlyname) == false)
                            {
                                this.m_dicKeywords[kvp.Key].Add(friendlyname);
                            }
                        }
                    }
                    String dicKey = String.Empty;
                    String dicValue = String.Empty;
                    foreach (String line in m_lstTableconfig)
                    {
                        if (line.Contains("{") && line.Contains("}"))
                        {
                            dicKey = line.Remove(line.IndexOf("{"));
                            dicValue = line.Replace("{", ",");
                            dicValue = dicValue.Replace("}", "").ToUpper();
                            String[] arrStrings = dicValue.Split(',');
                            if (this.m_dicKeywords.ContainsKey(dicKey))
                            {
                                //Prüfen
                                this.m_dicKeywords[dicKey].AddRange(arrStrings);
                                /*
                                foreach (string entry in this.m_dicKeywords[dicKey])
                                {
                                    this.DebugInfo("Trace", "PrepareKeywordDic: " + entry);
                                }
                                */
                            }
                            else
                            {
                                this.DebugInfo("Warning", "PrepareKeywordDic: Mainkey " + dicKey + " not found!");
                            }
                        }
                    }
                }
                catch (Exception c)
                {
                    this.DebugInfo("Error", "Error in PrepareKeywordDic: " + c);
                }
            }
        }

        public String FindKeyword(String strToFind)
        {
            try
            {
                this.DebugInfo("Trace", "FindKeyword: " + strToFind);
                foreach (KeyValuePair<String, List<String>> kvp in this.m_dicKeywords)
                {
                    if (kvp.Value.Contains(strToFind.Replace(" ", "")))
                    {
                        this.DebugInfo("Trace", "FindKeyword: Returning Key " + kvp.Key);
                        return kvp.Key;
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "FindKeyword: " + c);
            }
            return String.Empty;
        }

        public List<String> ListReplace(List<String> targetlist, String wordToReplace, String replacement)
        {
            List<String> lstResult = new List<String>();
            foreach (String substring in targetlist)
            {
                lstResult.Add(substring.Replace(wordToReplace, replacement));
            }
            return lstResult;
        }

        private void CheckMessageLength(String strMessage, Int32 intMessagelength)
        {
            if (strMessage.Length > intMessagelength)
            {
                //Send Warning
                this.DebugInfo("Warning", strMessage);
                this.DebugInfo("Warning", "This Ingamemessage is too long and wont sent to Server!!!");
                this.DebugInfo("Warning", "The Message has a Length of " + strMessage.Length.ToString() + " Chars, Allow are 128 Chars");
            }
        }

        private void CreateSession(String SoldierName, Int32 intScore, String EAGUID)
        {
            if (this.ServerID == 0)
            {
                return;
            }
            try
            {
                if (this.m_sessionON == enumBoolYesNo.Yes)
                {
                    //Session
                    lock (this.sessionlock)
                    {
                        if (this.m_dicSession.ContainsKey(SoldierName) == false)
                        {
                            //this.DebugInfo("Trace", "Session for Player: " + SoldierName + " created");
                            this.m_dicSession.Add(SoldierName, new CStats(String.Empty, intScore, 0, 0, 0, 0, 0, 0, this.m_dTimeOffset, this.weaponDic));
                            this.m_dicSession[SoldierName].Rank = this.GetRank(SoldierName);
                        }
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "CreateSession: " + c);
            }
            finally
            {
                lock (this.sessionlock)
                {
                    //Session Score
                    if (this.m_dicSession.ContainsKey(SoldierName) && this.m_sessionON == enumBoolYesNo.Yes)
                    {
                        this.m_dicSession[SoldierName].AddScore(intScore);
                        if (EAGUID.Length > 2)
                        {
                            this.m_dicSession[SoldierName].EAGuid = EAGUID;
                        }
                    }
                }
            }
        }
        /*
        private void RemoveSession(string SoldierName)
        {
            try
            {
                if (m_sessionON == enumBoolYesNo.Yes)
                {
                    if (this.m_dicSession.ContainsKey(SoldierName) == true)
                    {
                        //Passed seesion to list
                        this.lstpassedSessions.Add(m_dicSession[SoldierName]);
                        this.m_dicSession.Remove(SoldierName);
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "RemoveSession: " + c);
            }
        }
        */
        private void GetServerStats(String SoldierName, Int32 delay, String scope)
        {
            String SQL = @"SELECT * FROM " + this.tbl_server_stats + @" WHERE ServerID = @ServerID";
            List<String> result = new List<String>(this.m_lstServerstatsMsg);
            try
            {
                {
                    DataTable sqlresult = this.SQLquery(SQL, new { ServerID = this.ServerID });
                    if (sqlresult != null)
                    {
                        foreach (DataRow row in sqlresult.Rows)
                        {
                            result = this.ListReplace(result, "%serverName%", this.serverName);
                            //COUNT
                            result = this.ListReplace(result, "%countPlayer%", Convert.ToInt64(row["CountPlayers"]).ToString());
                            //SUM
                            result = this.ListReplace(result, "%sumScore%", Convert.ToInt64(row["SumScore"]).ToString());
                            result = this.ListReplace(result, "%sumKills%", Convert.ToInt64(row["SumKills"]).ToString());
                            result = this.ListReplace(result, "%sumHeadshots%", Convert.ToInt64(row["SumHeadshots"]).ToString());
                            result = this.ListReplace(result, "%sumDeaths%", Convert.ToInt64(row["SumDeaths"]).ToString());
                            result = this.ListReplace(result, "%sumSuicide%", Convert.ToInt64(row["SumSuicide"]).ToString());
                            result = this.ListReplace(result, "%sumTKs%", Convert.ToInt64(row["SumTKs"]).ToString());
                            result = this.ListReplace(result, "%sumRounds%", Convert.ToInt64(row["SumRounds"]).ToString());

                            //AVG
                            result = this.ListReplace(result, "%avgScore%", Convert.ToInt64(row["AvgScore"]).ToString());
                            result = this.ListReplace(result, "%avgKills%", Convert.ToInt64(row["AvgKills"]).ToString());
                            result = this.ListReplace(result, "%avgHeadshots%", Convert.ToInt64(row["AvgHeadshots"]).ToString());
                            result = this.ListReplace(result, "%avgDeaths%", Convert.ToInt64(row["AvgDeaths"]).ToString());
                            result = this.ListReplace(result, "%avgSuicide%", Convert.ToInt64(row["AvgSuicide"]).ToString());
                            result = this.ListReplace(result, "%avgTKs%", Convert.ToInt64(row["AvgTKs"]).ToString());
                            result = this.ListReplace(result, "%avgRounds%", Convert.ToInt64(row["AvgRounds"]).ToString());
                            //MISC.
                            //SPM
                            result = this.ListReplace(result, "%avgSPM%", Math.Round(Convert.ToDouble(row["SumScore"]) / (Convert.ToDouble(row["SumPlaytime"]) / 60), 2).ToString());
                            //KPM
                            result = this.ListReplace(result, "%avgKPM%", Math.Round(Convert.ToDouble(row["SumKills"]) / (Convert.ToDouble(row["SumPlaytime"]) / 60), 2).ToString());
                            //HPM
                            result = this.ListReplace(result, "%avgHPM%", Math.Round(Convert.ToDouble(row["SumHeadshots"]) / (Convert.ToDouble(row["SumPlaytime"]) / 60), 2).ToString());
                            //HPK
                            result = this.ListReplace(result, "%avgHPK%", Math.Round(Convert.ToDouble(row["SumHeadshots"]) / (Convert.ToDouble(row["SumKills"])), 2).ToString());
                            //Playtime
                            TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(row["SumPlaytime"]), 0, 0);
                            result = this.ListReplace(result, "%sumPlaytime%", span.Days + "d:" + span.Hours + "h:" + span.Minutes + "m:" + span.Seconds + "s");
                            result = this.ListReplace(result, "%sumPlaytimeHours%", Math.Round(span.TotalHours, 2).ToString());
                            result = this.ListReplace(result, "%sumPlaytimeDays%", Math.Round(span.TotalDays, 2).ToString());
                            //avg. Playtime
                            span = new TimeSpan(0, 0, Convert.ToInt32(row["AvgPlaytime"]), 0, 0);
                            result = this.ListReplace(result, "%avgPlaytime%", span.Days + "d:" + span.Hours + "h:" + span.Minutes + "m:" + span.Seconds + "s");
                            result = this.ListReplace(result, "%avgPlaytimeHours%", Math.Round(span.TotalHours, 2).ToString());
                            result = this.ListReplace(result, "%avgPlaytimeDays%", Math.Round(span.TotalDays, 2).ToString());
                        }

                        if (result.Count != 0)
                        {
                            this.SendMultiLineChatMessage(result, delay, 0, scope, SoldierName);
                        }
                        else
                        {
                            result.Clear();
                            result.Add("No Serverdata available!");
                            this.SendMultiLineChatMessage(result, delay, 0, scope, SoldierName);
                        }
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "GetServerStats: " + c);
            }

        }

        private void GetSession(String SoldierName, Int32 delay, String scope)
        {
            if (this.ServerID == 0)
            {
                return;
            }
            try
            {
                if (this.m_dicSession.ContainsKey(SoldierName) && this.m_sessionON == enumBoolYesNo.Yes)
                {
                    List<String> result = new List<String>();
                    result = m_lstSessionMessage;
                    result = ListReplace(result, "%playerName%", SoldierName);
                    result = ListReplace(result, "%playerScore%", this.m_dicSession[SoldierName].Score.ToString());
                    result = ListReplace(result, "%playerKills%", this.m_dicSession[SoldierName].Kills.ToString());
                    result = ListReplace(result, "%killstreak%", this.m_dicSession[SoldierName].Killstreak.ToString());
                    result = ListReplace(result, "%playerDeaths%", this.m_dicSession[SoldierName].Deaths.ToString());
                    result = ListReplace(result, "%deathstreak%", this.m_dicSession[SoldierName].Deathstreak.ToString());
                    result = ListReplace(result, "%playerKDR%", this.m_dicSession[SoldierName].KDR().ToString());
                    result = ListReplace(result, "%playerHeadshots%", this.m_dicSession[SoldierName].Headshots.ToString());
                    result = ListReplace(result, "%playerSuicide%", this.m_dicSession[SoldierName].Suicides.ToString());
                    result = ListReplace(result, "%playerTK%", this.m_dicSession[SoldierName].Teamkills.ToString());
                    result = ListReplace(result, "%startRank%", this.m_dicSession[SoldierName].Rank.ToString());
                    //Rankdiff
                    Int32 playerRank = this.GetRank(SoldierName);
                    //int playerRank = 0;
                    result = ListReplace(result, "%playerRank%", playerRank.ToString());
                    Int32 Rankdif = this.m_dicSession[SoldierName].Rank;
                    Rankdif = Rankdif - playerRank;
                    if (Rankdif == 0)
                    {
                        result = ListReplace(result, "%RankDif%", "0");
                    }
                    else if (Rankdif > 0)
                    {
                        result = ListReplace(result, "%RankDif%", "+" + Rankdif.ToString());
                    }
                    else
                    {
                        result = ListReplace(result, "%RankDif%", Rankdif.ToString());
                    }
                    result = ListReplace(result, "%SessionStarted%", this.m_dicSession[SoldierName].TimePlayerjoined.ToString());
                    TimeSpan duration = MyDateTime.Now - this.m_dicSession[SoldierName].TimePlayerjoined;
                    result = ListReplace(result, "%SessionDuration%", Math.Round(duration.TotalMinutes, 2).ToString());

                    if (result.Count != 0)
                    {
                        this.SendMultiLineChatMessage(result, delay, 0, scope, SoldierName);
                    }
                    else
                    {
                        result.Clear();
                        result.Add("No Sessiondata are available!");
                        this.SendMultiLineChatMessage(result, delay, 0, scope, SoldierName);
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "GetSession: " + c);
            }
        }

        private Int32 GetRank(String SoldierName)
        {
            //this.DebugInfo("Trace", "GetRank: " + SoldierName);
            Int32 rank = 0;
            try
            {
                String SQL = String.Empty;
                if (m_enRankingByScore == enumBoolYesNo.Yes)
                {
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        SQL = @"SELECT tpr.rankScore AS ScoreRank
                                FROM " + this.tbl_playerrank + @" tpr
                                INNER JOIN " + this.tbl_playerdata + @" tpd ON tpr.PlayerID = tpd.PlayerID
                                WHERE tpd.SoldierName = @SoldierName AND tpr.ServerGroup = @ServerGroup";
                    }
                    else
                    {
                        SQL = @"SELECT tps.rankScore AS ScoreRank
                                FROM " + this.tbl_playerstats + @" tps
                                INNER JOIN " + this.tbl_server_player + @" tsp ON tps.StatsID = tsp.StatsID
                                INNER JOIN " + this.tbl_playerdata + @" tpd ON tsp.PlayerID = tpd.PlayerID
                                WHERE  tpd.SoldierName = @SoldierName AND tsp.ServerID = @ServerID";
                    }
                }
                else
                {
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        SQL = @"SELECT tpr.rankKills AS ScoreRank
                                FROM " + this.tbl_playerrank + @" tpr
                                INNER JOIN " + this.tbl_playerdata + @" tpd ON tpr.PlayerID = tpd.PlayerID
                                WHERE tpd.SoldierName = @SoldierName AND tpr.ServerGroup = @ServerGroup";
                    }
                    else
                    {
                        SQL = @"SELECT tps.rankKills AS ScoreRank
                                FROM " + this.tbl_playerstats + @" tps
                                INNER JOIN " + this.tbl_server_player + @" tsp ON tps.StatsID = tsp.StatsID
                                INNER JOIN " + this.tbl_playerdata + @" tpd ON tsp.PlayerID = tpd.PlayerID
                                WHERE  tpd.SoldierName = @SoldierName AND tsp.ServerID = @ServerID";
                    }
                }
                {
                    DynamicParameters dynParams = new DynamicParameters();
                    if (this.m_enOverallRanking == enumBoolYesNo.Yes)
                    {
                        dynParams.Add("SoldierName", SoldierName);
                        dynParams.Add("ServerGroup", this.intServerGroup);
                    }
                    else
                    {
                        dynParams.Add("SoldierName", SoldierName);
                        dynParams.Add("ServerID", this.ServerID);
                    }
                    DataTable result = this.SQLquery(SQL, dynParams);
                    if (result != null)
                    {
                        foreach (DataRow row in result.Rows)
                        {
                            if (Convert.DBNull.Equals(row[0]) == false)
                            {
                                rank = Convert.ToInt32(row[0]);
                                this.DebugInfo("Trace", SoldierName + " Rank: " + row[0].ToString());
                            }
                        }
                    }
                }
            }
            catch (MySqlException oe)
            {
                this.DebugInfo("Error", "Error in GetRank: ");
                this.DisplayMySqlErrorCollection(oe);
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "Error in GetRank: " + c);
            }
            return rank;
        }

        public void PluginInfo(String strPlayer)
        {
            //this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger","0", "1", "1", "procon.protected.send", "admin.say","This Server running the PRoCon plugin "+this.GetPluginName+" "+this.GetPluginVersion+"running by "+ this.GetPluginAuthor,"player", strPlayer);
        }

        /*private void getBFBCStats(List<CPlayerInfo> lstPlayers)
        {
            //Disabled temp
            return;
            try
            {
                List<string> lstSoldierName = new List<string>();
                foreach (CPlayerInfo Player in lstPlayers)
                {
                    DateTime lastUpdate = DateTime.MinValue;
                    if (this.m_getStatsfromBFBCS == enumBoolYesNo.Yes && Player.SoldierName != null && this.StatsTracker.ContainsKey(Player.SoldierName) == true && this.StatsTracker[Player.SoldierName].BFBCS_Stats.Updated == false && this.StatsTracker[Player.SoldierName].BFBCS_Stats.Fetching == false)
                    {
                        string SQL = @"SELECT b.LastUpdate, b.Rank, b.Kills, b.Deaths, b.Score, b.Time
	  								 FROM " + tbl_playerdata + @" a
	  								 INNER JOIN " + tbl_bfbcs + @" b ON a.PlayerID = b.bfbcsID
	               					 WHERE a.SoldierName = @SoldierName";

                        {
                            DataTable result = this.SQLquery(SQL, new { SoldierName = Player.SoldierName });

                            foreach (DataRow row in result.Rows)
                            {
                                //this.DebugInfo("Last Update: " + row[0].ToString());
                                lastUpdate = Convert.ToDateTime(row[0]);
                                TimeSpan TimeDifference = MyDateTime.Now.Subtract(lastUpdate);
                                //this.DebugInfo(TimeDifference.TotalHours.ToString());
                                if (TimeDifference.TotalHours >= this.BFBCS_UpdateInterval && this.StatsTracker[Player.SoldierName].BFBCS_Stats.Fetching == false)
                                {
                                    this.StatsTracker[Player.SoldierName].BFBCS_Stats.Fetching = true;
                                    lstSoldierName.Add(Player.SoldierName);
                                }
                                else if (this.StatsTracker.ContainsKey(Player.SoldierName) == true && this.StatsTracker[Player.SoldierName].BFBCS_Stats.Fetching == true)
                                {
                                    //Do nothing
                                }
                                else
                                {
                                    if (this.StatsTracker.ContainsKey(Player.SoldierName) == true)
                                    {
                                        //this.DebugInfo("No Update needed");
                                        this.StatsTracker[Player.SoldierName].BFBCS_Stats.Updated = true;
                                        this.StatsTracker[Player.SoldierName].BFBCS_Stats.Rank = Convert.ToInt32(row[1]);
                                        this.StatsTracker[Player.SoldierName].BFBCS_Stats.Kills = Convert.ToInt32(row[2]);
                                        this.StatsTracker[Player.SoldierName].BFBCS_Stats.Deaths = Convert.ToInt32(row[3]);
                                        this.StatsTracker[Player.SoldierName].BFBCS_Stats.Score = Convert.ToInt32(row[4]);
                                        this.StatsTracker[Player.SoldierName].BFBCS_Stats.Time = Convert.ToDouble(row[5]);
                                        this.StatsTracker[Player.SoldierName].BFBCS_Stats.NoUpdate = true;
                                        this.checkPlayerStats(Player.SoldierName, this.m_strReasonMsg);
                                    }
                                }
                            }
                        }
                    }
                }
                if (lstSoldierName != null && lstSoldierName.Count > 0 && lstSoldierName.Count >= this.BFBCS_Min_Request)
                {
                    //Start Fetching
                    specialArrayObject ListObject = new specialArrayObject(lstSoldierName);
                    Thread newThread = new Thread(new ParameterizedThreadStart(this.DownloadBFBCS));
                    newThread.Start(ListObject);
                }
                else
                {
                    foreach (string player in lstSoldierName)
                    {
                        this.StatsTracker[player].BFBCS_Stats.Fetching = false;
                        this.StatsTracker[player].BFBCS_Stats.Updated = false;
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", " getBFBCStats: " + c);
            }
        }

        private void DownloadBFBCS(object ListObject)
        {
            specialArrayObject ListString = (specialArrayObject)ListObject;
            List<string> lstSoldierName = new List<string>();
            lstSoldierName = ListString.LstString;
            //Define a empty string for parameter
            string ParameterString = String.Empty;
            string result = String.Empty;
            foreach (string SoldierName in lstSoldierName)
            {
                if (this.StatsTracker[SoldierName].BFBCS_Stats.Updated == false)
                {
                    ParameterString = String.Concat(ParameterString, SoldierName, ",");
                    this.StatsTracker[SoldierName].BFBCS_Stats.Updated = true;
                }
            }
            ParameterString = ParameterString.Remove(ParameterString.LastIndexOf(","));
            try
            {
                this.DebugInfo("Trace", "Thread started and fetching Stats from BFBCS for Players: " + ParameterString);
                //Thx to IIIAVIII
                ParameterString = ParameterString.Replace("&", "%26");
                ParameterString = ParameterString.Replace(" ", "%20");
                ParameterString = ParameterString.Replace("$", "%24");
                ParameterString = ParameterString.Replace("+", "%2B");
                ParameterString = ParameterString.Replace("/", "%2F");
                ParameterString = ParameterString.Replace("?", "%3F");
                ParameterString = ParameterString.Replace("%", "%25");
                ParameterString = ParameterString.Replace("#", "%23");
                //ParameterString = ParameterString.Replace(",","%2C");
                ParameterString = ParameterString.Replace(":", "%3A");
                ParameterString = ParameterString.Replace(";", "%3B");
                ParameterString = ParameterString.Replace("=", "%3D");
                ParameterString = ParameterString.Replace("@", "%40");
                ParameterString = ParameterString.Replace("<", "%3C");
                ParameterString = ParameterString.Replace(">", "%3E");
                ParameterString = ParameterString.Replace("{", "%7B");
                ParameterString = ParameterString.Replace("}", "%7D");
                ParameterString = ParameterString.Replace("|", "%7C");
                ParameterString = ParameterString.Replace(@"\", @"%5C");
                ParameterString = ParameterString.Replace("^", "%5E");
                ParameterString = ParameterString.Replace("~", "%7E");
                ParameterString = ParameterString.Replace("[", "%5B");
                ParameterString = ParameterString.Replace("]", "%5D");
                ParameterString = ParameterString.Replace("`", "%60");

                result = ("http://api.bfbcs.com/api/pc?players=" + ParameterString + "&fields=basic").GetStringAsync().Result;
                if (result == null || result.StartsWith("{") == false)
                {
                    this.DebugInfo("Trace", "the String returned by BFBCS was invalid");
                    this.DebugInfo("Trace", "Trying to repair the String...");
                    if (result != null)
                    {
                        //result = result.Remove(result.IndexOf("<"),(result.LastIndexOf(">")+1));
                        if (result.IndexOf("{") > 0)
                        {
                            result = result.Substring(result.IndexOf("{"));
                        }
                        if (result == null || result.StartsWith("{") == false)
                        {
                            this.DebugInfo("Trace", "Repair failed!!!");
                            return;
                        }
                        else
                        {
                            this.DebugInfo("Trace", "Repair (might be) successful");
                        }
                    }
                    else
                    {
                        this.DebugInfo("Trace", "Empty String...");
                        return;
                    }
                }
                //JSON DECODE
                Hashtable jsonHash = (Hashtable)JSON.JsonDecode(result);
                if (jsonHash["players"] != null)
                {
                    ArrayList jsonResults = (ArrayList)jsonHash["players"];
                    //Player with Stats
                    foreach (object objResult in jsonResults)
                    {
                        string stringvalue = String.Empty;
                        int intvalue = 0;
                        double doublevalue = 0;
                        Hashtable playerData = (Hashtable)objResult;
                        if (playerData != null && lstSoldierName.Contains(playerData["name"].ToString()) == true)
                        {
                            stringvalue = playerData["name"].ToString();
                            this.DebugInfo("Info", "Got BFBC2 stats for " + stringvalue);
                            int.TryParse(playerData["rank"].ToString(), out intvalue);
                            this.StatsTracker[stringvalue].BFBCS_Stats.Rank = intvalue;
                            int.TryParse(playerData["kills"].ToString(), out intvalue);
                            this.StatsTracker[stringvalue].BFBCS_Stats.Kills = intvalue;
                            int.TryParse(playerData["deaths"].ToString(), out intvalue);
                            this.StatsTracker[stringvalue].BFBCS_Stats.Deaths = intvalue;
                            int.TryParse(playerData["score"].ToString(), out intvalue);
                            this.StatsTracker[stringvalue].BFBCS_Stats.Score = intvalue;
                            double.TryParse(playerData["elo"].ToString(), out doublevalue);
                            this.StatsTracker[stringvalue].BFBCS_Stats.Elo = doublevalue;
                            double.TryParse(playerData["level"].ToString(), out doublevalue);
                            this.StatsTracker[stringvalue].BFBCS_Stats.Skilllevel = doublevalue;
                            double.TryParse(playerData["time"].ToString(), out doublevalue);
                            this.StatsTracker[stringvalue].BFBCS_Stats.Time = doublevalue;
                            this.StatsTracker[stringvalue].BFBCS_Stats.Updated = true;
                            // check Stats
                            this.checkPlayerStats(stringvalue, this.m_strReasonMsg);

                        }
                    }
                }
                if (jsonHash["players_unknown"] != null)
                {
                    //Player without Stats
                    ArrayList jsonResults_2 = (ArrayList)jsonHash["players_unknown"];
                    foreach (object objResult in jsonResults_2)
                    {
                        Hashtable playerData = (Hashtable)objResult;
                        if (playerData != null && lstSoldierName.Contains(playerData["name"].ToString()) == true)
                        {
                            this.DebugInfo("Info", "No Stats found for Player: " + playerData["name"].ToString());
                        }
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", " DownloadBFBCS: " + c);
                foreach (string SoldierName in lstSoldierName)
                {
                    this.StatsTracker[SoldierName].BFBCS_Stats.Updated = false;
                }
            }
        }


        public void RemovePlayerfromServer(string targetSoldierName, string strReason, string removeAction)
        {
            try
            {
                if (targetSoldierName == string.Empty)
                {
                    return;
                }
                switch (removeAction)
                {
                    case "Kick":
                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", targetSoldierName, strReason);
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Kicked Player: " + targetSoldierName + " - " + strReason);
                        break;

                    case "PBBan":
                        this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", String.Format("pb_sv_ban \"{0}\" \"{1}\"", targetSoldierName, "BC2! " + strReason));
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1PB-Ban for Player: " + targetSoldierName + " - " + strReason);
                        break;

                    case "EAGUIDBan":
                        this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.StatsTracker[targetSoldierName].EAGuid, "perm", strReason);
                        this.ExecuteCommand("procon.protected.send", "banList.save");
                        this.ExecuteCommand("procon.protected.send", "banList.list");
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1EA-GUID Ban for Player: " + targetSoldierName + " - " + strReason);
                        break;
                    case "Nameban":
                        this.ExecuteCommand("procon.protected.send", "banList.add", "name", targetSoldierName, "perm", strReason);
                        this.ExecuteCommand("procon.protected.send", "banList.save");
                        this.ExecuteCommand("procon.protected.send", "banList.list");
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Nameban for Player: " + targetSoldierName + " - " + strReason);
                        break;
                    case "Warn":
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Warning Player: " + targetSoldierName + " - " + strReason);
                        break;
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", " RemovePlayerfromServer: " + c);
            }
        }*/

        private void calculateAwards()
        {
            //Disabled temp
            return;
            /*
            string[] arrPlace = new string[] { "None", "None", "None" };
            int[] arrScores = new int[] { 0, 0, 0 };
            string BestCombat = "None";
            int BestCombat_kills = 0;
            // Place 1 to 3
            if (this.bool_roundStarted == true && this.StatsTracker.Count >= 4)
            {
                foreach (KeyValuePair<string, CStats> kvp in this.StatsTracker)
                {
                    //Place 1. to 3.
                    if (kvp.Value.Score > arrScores[0])
                    {
                        // 2. to 3.
                        arrScores[2] = arrScores[1];
                        arrPlace[2] = arrPlace[1];
                        // 1. to 2.
                        arrScores[1] = arrScores[0];
                        arrPlace[1] = arrPlace[0];
                        // New 1.
                        arrScores[0] = kvp.Value.Score;
                        arrPlace[0] = kvp.Key;
                    }
                    else if (kvp.Value.Score > arrScores[1])
                    {
                        // 2. to 3.
                        arrScores[2] = arrScores[1];
                        arrPlace[2] = arrPlace[1];
                        //New 2. 
                        arrScores[1] = kvp.Value.Score;
                        arrPlace[1] = kvp.Key;
                    }
                    else if (kvp.Value.Score > arrScores[2])
                    {
                        //New 3. 
                        arrScores[2] = kvp.Value.Score;
                        arrPlace[2] = kvp.Key;
                    }
                    //Most Kills - Best Combat
                    if (kvp.Value.Kills >= 5 && BestCombat_kills < kvp.Value.Kills)
                    {
                        BestCombat = kvp.Key;
                        BestCombat_kills = kvp.Value.Kills;
                    }
                    
                }
                //Set Awards
                //1.Place
                if (arrPlace[0] != null && String.Equals(arrPlace[0], "None") == false)
                {
                    this.StatsTracker[arrPlace[0]].Awards.dicAdd("First", 1);
                }
                //2.Place
                if (arrPlace[1] != null && String.Equals(arrPlace[1], "None") == false)
                {
                    this.StatsTracker[arrPlace[1]].Awards.dicAdd("Second", 1);
                }
                //3.Place
                if (arrPlace[1] != null && String.Equals(arrPlace[2], "None") == false)
                {
                    this.StatsTracker[arrPlace[2]].Awards.dicAdd("Third", 1);
                }
                //Best Combat
                if (BestCombat != null && String.Equals(BestCombat, "None") == false)
                {
                    this.StatsTracker[BestCombat].Awards.dicAdd("Best_Combat", 1);
                }
            }
            */
        }

        public void Threadstarter_Webrequest()
        {
            if (this.m_enWebrequest == enumBoolYesNo.Yes)
            {
                //Temp disabled
                // new Thread(Webrequest).Start();
            }
        }

        public void Webrequest()
        {
            /*
            try
            {
                this.DebugInfo("Info", "Thread started and calling the Website:  " + this.m_webAddress);
                String result = this.m_webAddress.GetStringAsync().Result;
                if (result.Length > 0)
                {
                    this.DebugInfo("Info", "Got response from Webserver!");
                }
                else
                {
                    this.DebugInfo("Warning", "Webrequest: Page(" + this.m_webAddress + ") not found!");
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "Webrequest: " + c);
            }
             */
        }


        private void checkWelcomeStatsDic()
        {
            try
            {
                lock (this.welcomestatsDic)
                {
                    TimeSpan duration = new TimeSpan(0, 10, 0);
                    List<String> entryToRemove = new List<String>();
                    foreach (KeyValuePair<String, DateTime> kvp in this.welcomestatsDic)
                    {
                        if (duration < (MyDateTime.Now - kvp.Value))
                        {
                            entryToRemove.Add(kvp.Key);
                        }
                    }
                    foreach (String entry in entryToRemove)
                    {
                        this.DebugInfo("Trace", "Removing Player " + entry + " from welcomestatslist  Timeoutlimit of 10 minutes was exceeded!");
                        this.welcomestatsDic.Remove(entry);
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "Error in checkWelcomeStatsDic: " + c);
            }
        }

        private void BuildRegexRuleset()
        {
            try
            {
                this.lstChatFilterRules = new List<Regex>();
                foreach (String strRule in this.lstStrChatFilterRules)
                {
                    this.lstChatFilterRules.Add(new Regex(strRule.Replace("&#124", "|").Replace("&#124", "+")));
                }

                if (this.GlobalDebugMode.Equals("Trace"))
                {
                    this.DebugInfo("Trace", "Active Regex-Ruleset:");
                    foreach (Regex regexrule in this.lstChatFilterRules)
                    {
                        this.DebugInfo("Trace", regexrule.ToString());
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "Error in BuildRegexRuleset: " + c);
            }
        }

        private void SendMultiLineChatMessage(List<String> lstMultiLineChatMSG, Int32 intDelay, Int32 delayIncreasePerLine, String strScope, String targetPlayerName)
        {
            Int32 totalDelay = intDelay;
            Int32 yellduration = 8;
            String duration = String.Empty;
            String yelltagwithduration = @"^\[[y|Y][e|E][l|L]{2,2},\d+\]";
            //string yelltag = @"^\[[y|Y][e|E][l|L]{2,2},";
            try
            {
                switch (strScope)
                {
                    case "all":
                        foreach (String line in lstMultiLineChatMSG)
                        {

                            if (Regex.IsMatch(line, yelltagwithduration))
                            {
                                MatchCollection matches = Regex.Matches(line, yelltagwithduration);
                                foreach (Match match in matches)
                                {
                                    foreach (Capture capture in match.Captures)
                                    {
                                        if (Int32.TryParse(Regex.Replace(match.Value, @"\D", ""), out yellduration) == false)
                                        {
                                            this.DebugInfo("Trace", "SendMultiLineChatMessage: Could not parse Duration, using default");
                                            yellduration = 8;
                                        }
                                    }
                                }
                                //yell this!
                                this.CheckMessageLength(Regex.Replace(line, yelltagwithduration, ""), 255);
                                this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", totalDelay.ToString(), "1", "1", "procon.protected.send", "admin.yell", Regex.Replace(line, yelltagwithduration, ""), yellduration.ToString(), strScope);
                                totalDelay += delayIncreasePerLine;
                                totalDelay += yellduration;
                            }
                            else
                            {
                                //default say
                                this.CheckMessageLength(line, 128);
                                this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", totalDelay.ToString(), "1", "1", "procon.protected.send", "admin.say", line, strScope);
                                totalDelay += delayIncreasePerLine;
                            }
                        }
                        break;

                    default:
                        foreach (String line in lstMultiLineChatMSG)
                        {
                            if (Regex.IsMatch(line, yelltagwithduration))
                            {
                                MatchCollection matches = Regex.Matches(line, yelltagwithduration);
                                foreach (Match match in matches)
                                {
                                    foreach (Capture capture in match.Captures)
                                    {
                                        if (Int32.TryParse(Regex.Replace(match.Value, @"\D", ""), out yellduration) == false)
                                        {
                                            this.DebugInfo("Trace", "SendMultiLineChatMessage: Could not parse Duration, using default");
                                            yellduration = 8;
                                        }
                                    }
                                }
                                //yell this!
                                this.CheckMessageLength(line, 255);
                                this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", totalDelay.ToString(), "1", "1", "procon.protected.send", "admin.yell", Regex.Replace(line, yelltagwithduration, ""), yellduration.ToString(), "player", targetPlayerName);
                                totalDelay += delayIncreasePerLine;
                                totalDelay += yellduration;
                            }
                            else
                            {
                                //default say
                                this.CheckMessageLength(line, 128);
                                this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", totalDelay.ToString(), "1", "1", "procon.protected.send", "admin.say", line, "player", targetPlayerName);
                                totalDelay += delayIncreasePerLine;
                            }
                        }
                        break;
                }

            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "Error in PostMultiLineChat: " + c);
            }
        }
    }
}
