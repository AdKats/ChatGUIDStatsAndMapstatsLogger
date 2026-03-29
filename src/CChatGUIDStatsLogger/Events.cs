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
using System.Threading;

using MySqlConnector;

using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;

namespace PRoConEvents
{
    public partial class CChatGUIDStatsLogger
    {
        #region IPRoConPluginInterface
        /*=======ProCon Events========*/
        // Player events
        public override void OnPlayerJoin(string strSoldierName)
        {
            if (this.StatsTracker.ContainsKey(strSoldierName) == false)
            {
                CStats newEntry = new CStats(String.Empty, 0, 0, 0, 0, 0, 0, 0, this.m_dTimeOffset, this.weaponDic);
                StatsTracker.Add(strSoldierName, newEntry);
            }
            ThreadPool.QueueUserWorkItem(delegate { this.CreateSession(strSoldierName, 0, String.Empty); });

            if (bool_roundStarted == true && StatsTracker.ContainsKey(strSoldierName) == true)
            {
                if (StatsTracker[strSoldierName].PlayerOnServer == false)
                {
                    if (this.StatsTracker[strSoldierName].TimePlayerjoined == null)
                    {
                        this.StatsTracker[strSoldierName].TimePlayerjoined = MyDateTime.Now;
                    }
                    this.StatsTracker[strSoldierName].Playerjoined = MyDateTime.Now;
                    this.StatsTracker[strSoldierName].PlayerOnServer = true;
                }
            }
            //Mapstatscounter for Player who joined the server
            this.Mapstats.IntplayerjoinedServer++;

            if (this.m_enWelcomeStats == enumBoolYesNo.Yes)
            {
                if (this.welcomestatsDic.ContainsKey(strSoldierName))
                {
                    //Update jointime
                    this.welcomestatsDic[strSoldierName] = MyDateTime.Now;
                }
                else
                {
                    //Insert
                    this.DebugInfo("Trace", "Added Player: " + strSoldierName + " to welcomestatslist");
                    this.welcomestatsDic.Add(strSoldierName, MyDateTime.Now);
                }
            }
        }

        public override void OnPlayerAuthenticated(string soldierName, string guid)
        {
            if (this.StatsTracker.ContainsKey(soldierName) == false)
            {
                CStats newEntry = new CStats(String.Empty, 0, 0, 0, 0, 0, 0, 0, this.m_dTimeOffset, this.weaponDic);
                StatsTracker.Add(soldierName, newEntry);
                if (guid.Length > 0)
                {
                    StatsTracker[soldierName].EAGuid = guid;
                }
            }
        }

        // Will receive ALL chat global/team/squad in R3.
        public override void OnGlobalChat(string strSpeaker, string strMessage)
        {
            if (strMessage.Length > 0)
            {
                ThreadPool.QueueUserWorkItem(delegate { this.LogChat(strSpeaker, strMessage, "Global"); });
            }
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public override void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            if (strMessage.Length > 0)
            {
                ThreadPool.QueueUserWorkItem(delegate { this.LogChat(strSpeaker, strMessage, "Team"); });
            }
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public override void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            if (strMessage.Length > 0)
            {
                ThreadPool.QueueUserWorkItem(delegate { this.LogChat(strSpeaker, strMessage, "Squad"); });
            }
        }

        public override void OnPunkbusterMessage(string strPunkbusterMessage)
        {
            try
            {
                // This piece of code gets the number of player out of Punkbustermessages
                string playercount = String.Empty;
                if (strPunkbusterMessage.Contains("End of Player List"))
                {
                    playercount = strPunkbusterMessage.Remove(0, 1 + strPunkbusterMessage.LastIndexOf("("));
                    playercount = playercount.Replace(" ", "");
                    playercount = playercount.Remove(playercount.LastIndexOf("P"), playercount.LastIndexOf(")"));
                    //this.DebugInfo("EoPl: "+playercount);
                    int players = Convert.ToInt32(playercount);
                    if (players >= intRoundStartCount && bool_roundStarted == false)
                    {
                        bool_roundStarted = true;
                        Time_RankingStarted = MyDateTime.Now;
                        //Mapstats Roundstarted
                        this.Mapstats.MapStarted();
                    }
                    else if (players >= intRoundStartCount && this.Mapstats.TimeMapStarted == DateTime.MinValue)
                    {
                        this.Mapstats.MapStarted();
                    }
                    //MapStats Playercount
                    this.Mapstats.ListADD(players);
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnPunkbusterMessage: " + c);
            }
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {
            this.RegisterAllCommands();
            if (this.m_enLogSTATS == enumBoolYesNo.Yes)
            {
                try
                {
                    this.AddPBInfoToStats(cpbiPlayer);
                    if (this.StatsTracker.ContainsKey(cpbiPlayer.SoldierName))
                    {
                        if (this.StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined == null)
                        {
                            this.StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined = MyDateTime.Now;
                        }
                        this.StatsTracker[cpbiPlayer.SoldierName].IP = cpbiPlayer.Ip;
                    }
                }
                catch (Exception c)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnPunkbusterPlayerInfo: " + c);
                }
            }
        }

        // Query Events
        public override void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.serverName = csiServerInfo.ServerName;
            this.Mapstats.StrGamemode = csiServerInfo.GameMode;
            this.Mapstats.ListADD(csiServerInfo.PlayerCount);
            //Mapstats
            if (csiServerInfo.PlayerCount >= intRoundStartCount && this.Mapstats.TimeMapStarted == DateTime.MinValue)
            {
                this.Mapstats.MapStarted();
            }
            this.Mapstats.StrMapname = csiServerInfo.Map;
            this.Mapstats.IntRound = csiServerInfo.CurrentRound;
            this.Mapstats.IntNumberOfRounds = csiServerInfo.TotalRounds;
            this.Mapstats.IntServerplayermax = csiServerInfo.MaxPlayerCount;

            if (this.ServerID == 0 || this.minIntervalllenght <= (DateTime.Now.Subtract(this.dtLastServerInfoEvent).TotalSeconds))
            {
                this.dtLastServerInfoEvent = DateTime.Now;
                try
                {
                    ThreadPool.QueueUserWorkItem(delegate { this.getUpdateServerID(csiServerInfo); });
                }
                catch { };
            }
        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            //List<CPlayerInfo> PlayerList = new List<CPlayerInfo>();
            //Mapstats Add Playercount to list
            this.Mapstats.ListADD(lstPlayers.Count);
            if (bool_roundStarted == false)
            {
                if (lstPlayers.Count >= intRoundStartCount)
                {
                    bool_roundStarted = true;
                    Time_RankingStarted = MyDateTime.Now;
                    this.DebugInfo("Trace", "OLP: roundstarted");
                    //Mapstats Roundstarted
                    this.Mapstats.MapStarted();
                }
            }
            if (lstPlayers.Count >= intRoundStartCount && this.Mapstats.TimeMapStarted == DateTime.MinValue)
            {
                this.Mapstats.MapStarted();
            }
            try
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (this.m_dicPlayers.ContainsKey(cpiPlayer.SoldierName) == true)
                    {
                        this.m_dicPlayers[cpiPlayer.SoldierName] = cpiPlayer;
                    }
                    else
                    {
                        this.m_dicPlayers.Add(cpiPlayer.SoldierName, cpiPlayer);
                    }
                    //Timelogging
                    if (this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true)
                    {
                        if (this.StatsTracker[cpiPlayer.SoldierName].PlayerOnServer == false)
                        {
                            this.StatsTracker[cpiPlayer.SoldierName].Playerjoined = MyDateTime.Now;
                            this.StatsTracker[cpiPlayer.SoldierName].PlayerOnServer = true;
                        }
                        //EA-GUID, ClanTag, usw.
                        if (cpiPlayer.GUID.Length > 3)
                        {
                            this.StatsTracker[cpiPlayer.SoldierName].EAGuid = cpiPlayer.GUID;
                            //ID - Cache
                            if (this.m_ID_cache.ContainsKey(cpiPlayer.GUID))
                            {
                                this.m_ID_cache[cpiPlayer.GUID].PlayeronServer = true;
                            }
                        }
                        this.StatsTracker[cpiPlayer.SoldierName].ClanTag = cpiPlayer.ClanTag;
                        //TeamId
                        this.StatsTracker[cpiPlayer.SoldierName].TeamId = cpiPlayer.TeamID;
                        if (cpiPlayer.Score != 0)
                        {
                            this.StatsTracker[cpiPlayer.SoldierName].Score = cpiPlayer.Score;
                        }
                        //GlobalRank
                        if (cpiPlayer.Rank >= 0)
                        {
                            this.StatsTracker[cpiPlayer.SoldierName].GlobalRank = cpiPlayer.Rank;
                        }

                        //KDR Correction
                        if (this.m_kdrCorrection == enumBoolYesNo.Yes && ((cpiPlayer.Deaths == 0 && cpiPlayer.Kills == 0 && cpiPlayer.Score == 0) == false))
                        {
                            if (this.StatsTracker[cpiPlayer.SoldierName].Deaths > (cpiPlayer.Deaths + this.StatsTracker[cpiPlayer.SoldierName].BeforeLeftDeaths))
                            {
                                this.DebugInfo("Trace", "OnListPlayers Player: " + cpiPlayer.SoldierName + " has " + this.StatsTracker[cpiPlayer.SoldierName].Deaths + " deaths; correcting to " + cpiPlayer.Deaths + " deaths now");
                                this.StatsTracker[cpiPlayer.SoldierName].Deaths = (cpiPlayer.Deaths + this.StatsTracker[cpiPlayer.SoldierName].BeforeLeftDeaths);
                            }
                            if (this.StatsTracker[cpiPlayer.SoldierName].Kills > (cpiPlayer.Kills + this.StatsTracker[cpiPlayer.SoldierName].BeforeLeftKills))
                            {
                                this.StatsTracker[cpiPlayer.SoldierName].Kills = (cpiPlayer.Kills + this.StatsTracker[cpiPlayer.SoldierName].BeforeLeftKills);
                            }
                        }

                    }
                    //Session Score
                    if (this.m_sessionON == enumBoolYesNo.Yes)
                    {
                        lock (this.sessionlock)
                        {
                            if (this.m_dicSession.ContainsKey(cpiPlayer.SoldierName))
                            {
                                this.m_dicSession[cpiPlayer.SoldierName].AddScore(cpiPlayer.Score);
                                //KDR Correction
                                if (this.m_kdrCorrection == enumBoolYesNo.Yes && ((cpiPlayer.Deaths == 0 && cpiPlayer.Kills == 0 && cpiPlayer.Score == 0) == false))
                                {
                                    if (this.m_dicSession[cpiPlayer.SoldierName].Deaths > (cpiPlayer.Deaths + this.m_dicSession[cpiPlayer.SoldierName].BeforeLeftDeaths))
                                    {
                                        this.DebugInfo("Trace", "Player: " + cpiPlayer.SoldierName + " has " + this.m_dicSession[cpiPlayer.SoldierName].Deaths + " deaths; correcting to " + cpiPlayer.Deaths + " deaths now");
                                        this.m_dicSession[cpiPlayer.SoldierName].Deaths = (cpiPlayer.Deaths + this.m_dicSession[cpiPlayer.SoldierName].BeforeLeftDeaths);
                                    }
                                    if (this.m_dicSession[cpiPlayer.SoldierName].Kills > (cpiPlayer.Kills + this.m_dicSession[cpiPlayer.SoldierName].BeforeLeftKills))
                                    {
                                        this.m_dicSession[cpiPlayer.SoldierName].Kills = (cpiPlayer.Kills + this.m_dicSession[cpiPlayer.SoldierName].BeforeLeftKills);
                                    }
                                }
                                if (cpiPlayer.GUID.Length > 2)
                                {
                                    this.m_dicSession[cpiPlayer.SoldierName].EAGuid = cpiPlayer.GUID;
                                }
                                //TeamId
                                this.m_dicSession[cpiPlayer.SoldierName].TeamId = cpiPlayer.TeamID;
                            }
                            else
                            {
                                ThreadPool.QueueUserWorkItem(delegate { this.CreateSession(cpiPlayer.SoldierName, cpiPlayer.Score, cpiPlayer.GUID); });
                            }
                        }
                    }
                    //Checking the sessiondic
                    //ThreadPool.QueueUserWorkItem(delegate { this.CheckSessionDic(lstPlayers); });
                    //this.CreateSession(cpiPlayer.SoldierName, cpiPlayer.Score); 
                }

                if (this.m_enableCurrentPlayerstatsTable == enumBoolYesNo.Yes && this.ServerID > 0 && this.minIntervalllenght <= (DateTime.Now.Subtract(this.dtLastOnListPlayersEvent).TotalSeconds))
                {
                    ThreadPool.QueueUserWorkItem(delegate { this.UpdateCurrentPlayerTable(lstPlayers); });
                    this.dtLastOnListPlayersEvent = DateTime.Now;
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnListPlayers: " + c);
            }
        }

        public override void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            bool_roundStarted = true;
            if (bool_roundStarted == true)
            {
                this.playerKilled(kKillerVictimDetails);
            }
        }

        public override void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {
            this.playerLeftServer(cpiPlayer);
            this.RegisterAllCommands();
        }

        public override void OnRoundOverPlayers(List<CPlayerInfo> lstPlayers)
        {
            this.DebugInfo("Trace", "OnRoundOverPlayers Event");
            foreach (CPlayerInfo cpiPlayer in lstPlayers)
            {
                if (this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true)
                {
                    this.StatsTracker[cpiPlayer.SoldierName].Score = cpiPlayer.Score;
                    //EA-GUID, ClanTag, usw.
                    if (cpiPlayer.GUID.Length > 3)
                    {
                        this.StatsTracker[cpiPlayer.SoldierName].EAGuid = cpiPlayer.GUID;
                        //ID - Cache
                        if (this.m_ID_cache.ContainsKey(cpiPlayer.GUID))
                        {
                            this.m_ID_cache[cpiPlayer.GUID].PlayeronServer = true;
                        }
                    }
                    this.StatsTracker[cpiPlayer.SoldierName].ClanTag = cpiPlayer.ClanTag;
                    //TeamId
                    this.StatsTracker[cpiPlayer.SoldierName].TeamId = cpiPlayer.TeamID;

                    //KDR Correction
                    if (this.m_kdrCorrection == enumBoolYesNo.Yes && ((cpiPlayer.Deaths == 0 && cpiPlayer.Kills == 0 && cpiPlayer.Score == 0) == false))
                    {
                        if (this.StatsTracker[cpiPlayer.SoldierName].Deaths > (cpiPlayer.Deaths + this.StatsTracker[cpiPlayer.SoldierName].BeforeLeftDeaths))
                        {
                            this.DebugInfo("Trace", "Player: " + cpiPlayer.SoldierName + " has " + this.StatsTracker[cpiPlayer.SoldierName].Deaths + " deaths; correcting to " + cpiPlayer.Deaths + " deaths now");
                            this.StatsTracker[cpiPlayer.SoldierName].Deaths = (cpiPlayer.Deaths + this.StatsTracker[cpiPlayer.SoldierName].BeforeLeftDeaths);
                        }
                        if (this.StatsTracker[cpiPlayer.SoldierName].Kills > (cpiPlayer.Kills + this.StatsTracker[cpiPlayer.SoldierName].BeforeLeftKills))
                        {
                            this.StatsTracker[cpiPlayer.SoldierName].Kills = (cpiPlayer.Kills + this.StatsTracker[cpiPlayer.SoldierName].BeforeLeftKills);
                        }
                    }
                    //GlobalRank
                    if (cpiPlayer.Rank >= 0)
                    {
                        this.StatsTracker[cpiPlayer.SoldierName].GlobalRank = cpiPlayer.Rank;
                    }
                }
                //Session Score
                lock (this.sessionlock)
                {
                    if (this.m_dicSession.ContainsKey(cpiPlayer.SoldierName) && this.m_sessionON == enumBoolYesNo.Yes)
                    {
                        //KDR Correction
                        if (this.m_kdrCorrection == enumBoolYesNo.Yes && ((cpiPlayer.Deaths == 0 && cpiPlayer.Kills == 0 && cpiPlayer.Score == 0) == false))
                        {
                            if (this.m_dicSession[cpiPlayer.SoldierName].Deaths > (cpiPlayer.Deaths + this.m_dicSession[cpiPlayer.SoldierName].BeforeLeftDeaths))
                            {
                                this.DebugInfo("Trace", "Player: " + cpiPlayer.SoldierName + " has " + this.m_dicSession[cpiPlayer.SoldierName].Deaths + " deaths; correcting to " + cpiPlayer.Deaths + " deaths now");
                                this.m_dicSession[cpiPlayer.SoldierName].Deaths = (cpiPlayer.Deaths + this.m_dicSession[cpiPlayer.SoldierName].BeforeLeftDeaths);
                            }
                            if (this.m_dicSession[cpiPlayer.SoldierName].Kills > (cpiPlayer.Kills + this.m_dicSession[cpiPlayer.SoldierName].BeforeLeftKills))
                            {
                                this.m_dicSession[cpiPlayer.SoldierName].Kills = (cpiPlayer.Kills + this.m_dicSession[cpiPlayer.SoldierName].BeforeLeftKills);
                            }
                        }
                        this.m_dicSession[cpiPlayer.SoldierName].AddScore(cpiPlayer.Score);
                        this.m_dicSession[cpiPlayer.SoldierName].LastScore = 0;
                        this.m_dicSession[cpiPlayer.SoldierName].Rounds++;
                        this.m_dicSession[cpiPlayer.SoldierName].BeforeLeftKills += this.m_dicSession[cpiPlayer.SoldierName].Kills;
                        this.m_dicSession[cpiPlayer.SoldierName].BeforeLeftDeaths += this.m_dicSession[cpiPlayer.SoldierName].Deaths;
                        //TeamId
                        this.m_dicSession[cpiPlayer.SoldierName].TeamId = cpiPlayer.TeamID;
                        if (cpiPlayer.GUID.Length > 2)
                        {
                            this.m_dicSession[cpiPlayer.SoldierName].EAGuid = cpiPlayer.GUID;
                        }
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(delegate { this.CreateSession(cpiPlayer.SoldierName, cpiPlayer.Score, cpiPlayer.GUID); });
                    }
                }
            }
            this.Mapstats.MapEnd();
        }

        public override void OnRoundOver(int winningTeamId)
        {
            this.DebugInfo("Trace", "OnRoundOver: TeamId -> " + winningTeamId);
            //StatsTracker
            foreach (KeyValuePair<string, CStats> kvp in this.StatsTracker)
            {
                if (kvp.Value.PlayerOnServer == true)
                {
                    if (kvp.Value.TeamId == winningTeamId)
                    {
                        this.StatsTracker[kvp.Key].Wins++;
                    }
                    else
                    {
                        this.StatsTracker[kvp.Key].Losses++;
                    }
                }
            }
            //Session
            lock (this.sessionlock)
            {
                foreach (KeyValuePair<string, CStats> kvp in this.m_dicSession)
                {
                    if (kvp.Value.PlayerOnServer == true)
                    {
                        if (kvp.Value.TeamId == winningTeamId)
                        {
                            this.m_dicSession[kvp.Key].Wins++;
                        }
                        else
                        {
                            this.m_dicSession[kvp.Key].Losses++;
                        }
                    }
                }
            }
        }

        public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            if (bool_roundStarted == true && StatsTracker.ContainsKey(soldierName) == true)
            {
                if (StatsTracker[soldierName].PlayerOnServer == false)
                {
                    this.StatsTracker[soldierName].Playerjoined = MyDateTime.Now;
                    this.StatsTracker[soldierName].PlayerOnServer = true;
                }
            }
            if (this.m_enWelcomeStats == enumBoolYesNo.Yes)
            {
                if (this.welcomestatsDic.ContainsKey(soldierName))
                {
                    //Call of the Welcomstatsfunction
                    ThreadPool.QueueUserWorkItem(delegate { this.WelcomeStats(soldierName); });
                    lock (this.welcomestatsDic)
                    {
                        this.welcomestatsDic.Remove(soldierName);
                    }
                }
            }
        }

        public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal)
        {
            if ((DateTime.Now.Subtract(this.dtLastRoundendEvent)).TotalSeconds > 30)
            {
                this.dtLastRoundendEvent = DateTime.Now;
                this.DebugInfo("Info", "OnLevelLoaded: " + mapFileName + " Gamemode: " + Gamemode + " Round: " + (roundsPlayed + 1) + "/" + roundsTotal);
                this.DebugInfo("Info", "update sql server");
                this.Nextmapinfo = new CMapstats(MyDateTime.Now, mapFileName, (roundsPlayed + 1), roundsTotal, this.m_dTimeOffset);
                //Calculate Awards
                this.calculateAwards();
                new Thread(StartStreaming).Start();
                m_dicPlayers.Clear();
                this.Spamprotection.Reset();
            }
        }

        public override void OnRoundStartPlayerCount(int limit)
        {
            this.intRoundStartCount = limit;
        }

        public override void OnRoundRestartPlayerCount(int limit)
        {
            this.intRoundRestartCount = limit;
        }

        #endregion
    }
}
