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
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;

namespace PRoConEvents
{
    public partial class CChatGUIDStatsLogger
    {
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Server Details|Host", this.m_strHost.GetType(), this.m_strHost));
            lstReturn.Add(new CPluginVariable("Server Details|Port", this.m_strDBPort.GetType(), this.m_strDBPort));
            lstReturn.Add(new CPluginVariable("Server Details|Database Name", this.m_strDatabase.GetType(), this.m_strDatabase));
            lstReturn.Add(new CPluginVariable("Server Details|UserName", this.m_strUserName.GetType(), this.m_strUserName));
            lstReturn.Add(new CPluginVariable("Server Details|Password", this.m_strPassword.GetType(), this.m_strPassword));
            if (this.m_connectionPooling == enumBoolOnOff.Off)
            {
                lstReturn.Add(new CPluginVariable("Server Details|High performance mode(no connection limit!)", typeof(enumBoolOnOff), this.m_highPerformanceConnectionMode));
            }
            lstReturn.Add(new CPluginVariable("Server Details|Connection Pooling", typeof(enumBoolOnOff), this.m_connectionPooling));
            if (this.m_connectionPooling == enumBoolOnOff.On)
            {
                lstReturn.Add(new CPluginVariable("Server Details|Min Connection Pool Size", this.m_minPoolSize.GetType(), this.m_minPoolSize));
                lstReturn.Add(new CPluginVariable("Server Details|Max Connection Pool Size", this.m_maxPoolSize.GetType(), this.m_maxPoolSize));
            }
            lstReturn.Add(new CPluginVariable("Server Details|Failed Transaction retry attempts", this.TransactionRetryCount.GetType(), this.TransactionRetryCount));
            lstReturn.Add(new CPluginVariable("Server Details|Minimum time(sec) between ServerInfo Updates", this.minIntervalllenght.GetType(), this.minIntervalllenght));

            lstReturn.Add(new CPluginVariable("Chatlogging|Enable Chatlogging?", typeof(enumBoolYesNo), this.m_enChatloggingON));
            if (this.m_enChatloggingON == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Chatlogging|Log ServerSPAM?", typeof(enumBoolYesNo), this.m_enNoServerMsg));
                lstReturn.Add(new CPluginVariable("Chatlogging|Instant Logging of Chat Messages?", typeof(enumBoolYesNo), this.m_enInstantChatlogging));
                lstReturn.Add(new CPluginVariable("Chatlogging|Enable chatlog filter(Regex)?", typeof(enumBoolYesNo), this.m_enChatlogFilter));
                if (this.m_enChatlogFilter == enumBoolYesNo.Yes)
                {
                    this.lstStrChatFilterRules = new List<string>(this.ListReplace(this.lstStrChatFilterRules, "&#124", "|"));
                    this.lstStrChatFilterRules = this.ListReplace(this.lstStrChatFilterRules, "&#43", "+");
                    lstReturn.Add(new CPluginVariable("Chatlogging|Chatfilterrules(Regex)", typeof(string[]), this.lstStrChatFilterRules.ToArray()));
                }
            }
            lstReturn.Add(new CPluginVariable("Stats|Enable Statslogging?", typeof(enumBoolYesNo), this.m_enLogSTATS));
            if (this.m_enLogSTATS == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Stats|Enable Weaponstats?", typeof(enumBoolYesNo), this.m_weaponstatsON));
                lstReturn.Add(new CPluginVariable("Stats|Ranking by Score?", typeof(enumBoolYesNo), this.m_enRankingByScore));
                lstReturn.Add(new CPluginVariable("Stats|Enable ingame commands?", typeof(enumBoolYesNo), this.m_enableInGameCommands));
                lstReturn.Add(new CPluginVariable("Stats|Overall ranking(merged Serverranking)", typeof(enumBoolYesNo), this.m_enOverallRanking));
                lstReturn.Add(new CPluginVariable("Stats|Server group (0 - 128)", this.intServerGroup.GetType(), this.intServerGroup));
                lstReturn.Add(new CPluginVariable("Stats|Send Stats to all Players?", typeof(enumBoolYesNo), this.m_enSendStatsToAll));
                lstReturn.Add(new CPluginVariable("Stats|Enable KDR correction?", typeof(enumBoolYesNo), this.m_kdrCorrection));
                lstReturn.Add(new CPluginVariable("Stats|PlayerMessage", typeof(string[]), this.m_lstPlayerStatsMessage.ToArray()));
                //Player of the day
                lstReturn.Add(new CPluginVariable("Stats|Player of the day Message", typeof(string[]), this.m_lstPlayerOfTheDayMessage.ToArray()));
                lstReturn.Add(new CPluginVariable("Stats|Weaponstats Message ", typeof(string[]), this.m_lstWeaponstatsMsg.ToArray()));
                //Serverstats
                lstReturn.Add(new CPluginVariable("Stats|Serverstats Message", typeof(string[]), this.m_lstServerstatsMsg.ToArray()));
                lstReturn.Add(new CPluginVariable("Stats|Enable Livescoreboard in DB?", typeof(enumBoolYesNo), this.m_enableCurrentPlayerstatsTable));
                //Simplestats
                lstReturn.Add(new CPluginVariable("Stats|Log playerdata only (no playerstats)?", typeof(enumBoolYesNo), this.m_enLogPlayerDataOnly));
                //lstReturn.Add(new CPluginVariable("Stats|Awards ON?", typeof(enumBoolYesNo), this.m_awardsON));
                lstReturn.Add(new CPluginVariable("WelcomeStats|Enable Welcomestats?", typeof(enumBoolYesNo), this.m_enWelcomeStats));
                if (this.m_enWelcomeStats == enumBoolYesNo.Yes)
                {
                    //lstReturn.Add(new CPluginVariable("WelcomeStats|Yell Welcome Message(not the stats)?", typeof(enumBoolYesNo), this.m_enYellWelcomeMSG));
                    //lstReturn.Add(new CPluginVariable("WelcomeStats|Welcome Message", this.m_strPlayerWelcomeMsg.GetType(), this.m_strPlayerWelcomeMsg));
                    lstReturn.Add(new CPluginVariable("WelcomeStats|Welcome Message", typeof(string[]), this.m_lstPlayerWelcomeStatsMessage.ToArray()));
                    lstReturn.Add(new CPluginVariable("WelcomeStats|Welcome Message for new Player", typeof(string[]), this.m_lstNewPlayerWelcomeMsg.ToArray()));
                    lstReturn.Add(new CPluginVariable("WelcomeStats|Welcomestats Delay", this.int_welcomeStatsDelay.GetType(), this.int_welcomeStatsDelay));
                }
                //top10
                lstReturn.Add(new CPluginVariable("Stats|Top10 ingame", this.m_enTop10ingame.GetType(), this.m_enTop10ingame));
                if (this.m_enTop10ingame == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("Stats|Top10 header line", this.m_strTop10Header.GetType(), this.m_strTop10Header));
                    lstReturn.Add(new CPluginVariable("Stats|Top10 row format", this.m_strTop10RowFormat.GetType(), this.m_strTop10RowFormat));
                    //top10 for period
                    lstReturn.Add(new CPluginVariable("Stats|Top10 for period header line", this.m_strTop10HeaderForPeriod.GetType(), this.m_strTop10HeaderForPeriod));
                    lstReturn.Add(new CPluginVariable("Stats|Top10 for period interval days", this.m_intDaysForPeriodTop10.GetType(), this.m_intDaysForPeriodTop10));
                    //Weapontop10
                    lstReturn.Add(new CPluginVariable("Stats|WeaponTop10 header line", this.m_strWeaponTop10Header.GetType(), this.m_strWeaponTop10Header));
                    lstReturn.Add(new CPluginVariable("Stats|WeaponTop10 row format", this.m_strWeaponTop10RowFormat.GetType(), this.m_strWeaponTop10RowFormat));
                }
            }
            lstReturn.Add(new CPluginVariable("Debug|DebugLevel", "enum.Actions(Trace|Info|Warning|Error)", this.GlobalDebugMode));
            lstReturn.Add(new CPluginVariable("Table|Keywordlist", typeof(string[]), this.m_lstTableconfig.ToArray()));
            lstReturn.Add(new CPluginVariable("Table|tableSuffix", this.tableSuffix.GetType(), this.tableSuffix));
            lstReturn.Add(new CPluginVariable("MapStats|MapStats ON?", typeof(enumBoolYesNo), this.m_mapstatsON));
            lstReturn.Add(new CPluginVariable("Session|Session ON?", typeof(enumBoolYesNo), this.m_sessionON));
            lstReturn.Add(new CPluginVariable("Session|SessionMessage", typeof(string[]), this.m_lstSessionMessage.ToArray()));
            lstReturn.Add(new CPluginVariable("Session|Save Sessiondata to DB?", typeof(enumBoolYesNo), this.m_enSessionTracking));
            lstReturn.Add(new CPluginVariable("FloodProtection|Playerrequests per Round", this.numberOfAllowedRequests.GetType(), this.numberOfAllowedRequests));
            lstReturn.Add(new CPluginVariable("TimeOffset|Servertime Offset", this.m_dTimeOffset.GetType(), this.m_dTimeOffset));
            //Ingame Command Setup
            /*
            this.m_IngameCommands_stats = "stats,rank";
            this.m_IngameCommands_serverstats = "serverstats";
            this.m_IngameCommands_session = "session";
            this.m_IngameCommands_dogtags = "dogtags";
            this.m_IngameCommands_top10 = "top10";
            this.m_IngameCommands_playerOfTheDay = "playeroftheday,potd";
            this.m_IngameCommands_top10ForPeriod = "weektop10,wtop10";
             */
            lstReturn.Add(new CPluginVariable("Ingame Command Setup|Stats Command:", this.m_IngameCommands_stats.GetType(), this.m_IngameCommands_stats));
            lstReturn.Add(new CPluginVariable("Ingame Command Setup|ServerStats Command:", this.m_IngameCommands_serverstats.GetType(), this.m_IngameCommands_serverstats));
            lstReturn.Add(new CPluginVariable("Ingame Command Setup|Session Command:", this.m_IngameCommands_session.GetType(), this.m_IngameCommands_session));
            lstReturn.Add(new CPluginVariable("Ingame Command Setup|Dogtags Command:", this.m_IngameCommands_dogtags.GetType(), this.m_IngameCommands_dogtags));
            lstReturn.Add(new CPluginVariable("Ingame Command Setup|Top10 Command:", this.m_IngameCommands_top10.GetType(), this.m_IngameCommands_top10));
            lstReturn.Add(new CPluginVariable("Ingame Command Setup|Player Of The Day Command:", this.m_IngameCommands_playerOfTheDay.GetType(), this.m_IngameCommands_playerOfTheDay));
            lstReturn.Add(new CPluginVariable("Ingame Command Setup|Top10 for period Command:", this.m_IngameCommands_top10ForPeriod.GetType(), this.m_IngameCommands_top10ForPeriod));


            //lstReturn.Add(new CPluginVariable("BFBCS|Fetch Stats from BFBCS", typeof(enumBoolYesNo), this.m_getStatsfromBFBCS));

            if (this.m_getStatsfromBFBCS == enumBoolYesNo.Yes)
            {
                //lstReturn.Add(new CPluginVariable("BFBCS|Updateinterval (hours)", this.BFBCS_UpdateInterval.GetType(), this.BFBCS_UpdateInterval));
                //lstReturn.Add(new CPluginVariable("BFBCS|Request Packrate", this.BFBCS_Min_Request.GetType(), this.BFBCS_Min_Request));
                //lstReturn.Add(new CPluginVariable("Cheaterprotection|Statsbased Protection", typeof(enumBoolYesNo), this.m_cheaterProtection));
                //lstReturn.Add(new CPluginVariable("Ranklimiter|Ranklimiter ON?", typeof(enumBoolYesNo), this.m_enRanklimiter));
            }
            /*
            lstReturn.Add(new CPluginVariable("Webrequest|Periodical Webrequest On?(P&S Stats)", typeof(enumBoolYesNo), this.m_enWebrequest));
            if (this.m_enWebrequest == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Webrequest|Webaddress", this.m_webAddress.GetType(), this.m_webAddress));
                lstReturn.Add(new CPluginVariable("Webrequest|Webrequest Intervall", this.m_requestIntervall.GetType(), this.m_requestIntervall));
            }
            */
            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Host", this.m_strHost.GetType(), this.m_strHost));
            lstReturn.Add(new CPluginVariable("Port", this.m_strDBPort.GetType(), this.m_strDBPort));
            lstReturn.Add(new CPluginVariable("Database Name", this.m_strDatabase.GetType(), this.m_strDatabase));
            lstReturn.Add(new CPluginVariable("UserName", this.m_strUserName.GetType(), this.m_strUserName));
            lstReturn.Add(new CPluginVariable("Password", this.m_strPassword.GetType(), this.m_strPassword));
            if (this.m_connectionPooling == enumBoolOnOff.Off)
            {
                lstReturn.Add(new CPluginVariable("High performance mode(no connection limit!)", typeof(enumBoolOnOff), this.m_highPerformanceConnectionMode));
            }
            lstReturn.Add(new CPluginVariable("Connection Pooling", typeof(enumBoolOnOff), this.m_connectionPooling));
            if (this.m_connectionPooling == enumBoolOnOff.On)
            {
                lstReturn.Add(new CPluginVariable("Min Connection Pool Size", this.m_minPoolSize.GetType(), this.m_minPoolSize));
                lstReturn.Add(new CPluginVariable("Max Connection Pool Size", this.m_maxPoolSize.GetType(), this.m_maxPoolSize));
            }
            lstReturn.Add(new CPluginVariable("Failed Transaction retry attempts", this.TransactionRetryCount.GetType(), this.TransactionRetryCount));
            lstReturn.Add(new CPluginVariable("Minimum time(sec) between ServerInfo Updates", this.minIntervalllenght.GetType(), this.minIntervalllenght));
            // Switch for Stats Logging
            lstReturn.Add(new CPluginVariable("Enable Chatlogging?", typeof(enumBoolYesNo), this.m_enChatloggingON));
            if (this.m_enChatloggingON == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Log ServerSPAM?", typeof(enumBoolYesNo), this.m_enNoServerMsg));
                lstReturn.Add(new CPluginVariable("Instant Logging of Chat Messages?", typeof(enumBoolYesNo), this.m_enInstantChatlogging));
                lstReturn.Add(new CPluginVariable("Enable chatlog filter(Regex)?", typeof(enumBoolYesNo), this.m_enChatlogFilter));

                if (this.m_enChatlogFilter == enumBoolYesNo.Yes)
                {
                    this.lstStrChatFilterRules = new List<string>(this.ListReplace(this.lstStrChatFilterRules, "|", "&#124"));
                    this.lstStrChatFilterRules = this.ListReplace(this.lstStrChatFilterRules, "&#43", "+");
                    lstReturn.Add(new CPluginVariable("Chatfilterrules(Regex)", typeof(string[]), this.lstStrChatFilterRules.ToArray()));
                }
            }
            lstReturn.Add(new CPluginVariable("Enable Statslogging?", typeof(enumBoolYesNo), this.m_enLogSTATS));
            lstReturn.Add(new CPluginVariable("Enable Weaponstats?", typeof(enumBoolYesNo), this.m_weaponstatsON));
            //lstReturn.Add(new CPluginVariable("Update EA GUID?", typeof(enumBoolYesNo), this.m_UpdateEA_GUID));
            //lstReturn.Add(new CPluginVariable("Update PB-GUID (NOT recommended!!!)?", typeof(enumBoolYesNo), this.m_UpdatePB_GUID));
            lstReturn.Add(new CPluginVariable("Ranking by Score?", typeof(enumBoolYesNo), this.m_enRankingByScore));
            lstReturn.Add(new CPluginVariable("Enable ingame commands?", typeof(enumBoolYesNo), this.m_enableInGameCommands));
            lstReturn.Add(new CPluginVariable("Overall ranking(merged Serverranking)", typeof(enumBoolYesNo), this.m_enOverallRanking));
            lstReturn.Add(new CPluginVariable("Server group (0 - 128)", this.intServerGroup.GetType(), this.intServerGroup));
            lstReturn.Add(new CPluginVariable("Send Stats to all Players?", typeof(enumBoolYesNo), this.m_enSendStatsToAll));
            lstReturn.Add(new CPluginVariable("Enable Livescoreboard in DB?", typeof(enumBoolYesNo), this.m_enableCurrentPlayerstatsTable));
            lstReturn.Add(new CPluginVariable("Enable KDR correction?", typeof(enumBoolYesNo), this.m_kdrCorrection));
            lstReturn.Add(new CPluginVariable("PlayerMessage", typeof(string[]), this.m_lstPlayerStatsMessage.ToArray()));
            //Player of the day
            lstReturn.Add(new CPluginVariable("Player of the day Message", typeof(string[]), this.m_lstPlayerOfTheDayMessage.ToArray()));
            lstReturn.Add(new CPluginVariable("Weaponstats Message ", typeof(string[]), this.m_lstWeaponstatsMsg.ToArray()));
            //Serverstats
            lstReturn.Add(new CPluginVariable("Serverstats Message", typeof(string[]), this.m_lstServerstatsMsg.ToArray()));

            lstReturn.Add(new CPluginVariable("Awards ON?", typeof(enumBoolYesNo), this.m_awardsON));
            lstReturn.Add(new CPluginVariable("Enable Welcomestats?", typeof(enumBoolYesNo), this.m_enWelcomeStats));
            lstReturn.Add(new CPluginVariable("Yell Welcome Message(not the stats)?", typeof(enumBoolYesNo), this.m_enYellWelcomeMSG));
            lstReturn.Add(new CPluginVariable("Welcome Message", typeof(string[]), this.m_lstPlayerWelcomeStatsMessage.ToArray()));
            lstReturn.Add(new CPluginVariable("Welcome Message for new Player", typeof(string[]), this.m_lstNewPlayerWelcomeMsg.ToArray()));
            lstReturn.Add(new CPluginVariable("Welcomestats Delay", this.int_welcomeStatsDelay.GetType(), this.int_welcomeStatsDelay));
            lstReturn.Add(new CPluginVariable("Top10 ingame", this.m_enTop10ingame.GetType(), this.m_enTop10ingame));
            lstReturn.Add(new CPluginVariable("Top10 header line", this.m_strTop10Header.GetType(), this.m_strTop10Header));
            lstReturn.Add(new CPluginVariable("Top10 row format", this.m_strTop10RowFormat.GetType(), this.m_strTop10RowFormat));
            //top10 for period
            lstReturn.Add(new CPluginVariable("Top10 for period header line", this.m_strTop10HeaderForPeriod.GetType(), this.m_strTop10HeaderForPeriod));
            lstReturn.Add(new CPluginVariable("Top10 for period interval days", this.m_intDaysForPeriodTop10.GetType(), this.m_intDaysForPeriodTop10));
            //Weapontop10
            lstReturn.Add(new CPluginVariable("WeaponTop10 header line", this.m_strWeaponTop10Header.GetType(), this.m_strWeaponTop10Header));
            lstReturn.Add(new CPluginVariable("WeaponTop10 row format", this.m_strWeaponTop10RowFormat.GetType(), this.m_strWeaponTop10RowFormat));
            //
            lstReturn.Add(new CPluginVariable("DebugLevel", "enum.Actions(Trace|Info|Warning|Error)", this.GlobalDebugMode));
            lstReturn.Add(new CPluginVariable("Keywordlist", typeof(string[]), this.m_lstTableconfig.ToArray()));
            lstReturn.Add(new CPluginVariable("tableSuffix", this.tableSuffix.GetType(), this.tableSuffix));
            lstReturn.Add(new CPluginVariable("MapStats ON?", typeof(enumBoolYesNo), this.m_mapstatsON));
            lstReturn.Add(new CPluginVariable("Session ON?", typeof(enumBoolYesNo), this.m_sessionON));
            lstReturn.Add(new CPluginVariable("SessionMessage", typeof(string[]), this.m_lstSessionMessage.ToArray()));
            lstReturn.Add(new CPluginVariable("Save Sessiondata to DB?", typeof(enumBoolYesNo), this.m_enSessionTracking));
            lstReturn.Add(new CPluginVariable("Playerrequests per Round", this.numberOfAllowedRequests.GetType(), this.numberOfAllowedRequests));
            lstReturn.Add(new CPluginVariable("Servertime Offset", this.m_dTimeOffset.GetType(), this.m_dTimeOffset));

            //Ingame Command Setup
            /*
            this.m_IngameCommands_stats = "stats,rank";
            this.m_IngameCommands_serverstats = "serverstats";
            this.m_IngameCommands_session = "session";
            this.m_IngameCommands_dogtags = "dogtags";
            this.m_IngameCommands_top10 = "top10";
            this.m_IngameCommands_playerOfTheDay = "playeroftheday,potd";
            this.m_IngameCommands_top10ForPeriod = "weektop10,wtop10";
             */
            lstReturn.Add(new CPluginVariable("Stats Command:", this.m_IngameCommands_stats.GetType(), this.m_IngameCommands_stats));
            lstReturn.Add(new CPluginVariable("ServerStats Command:", this.m_IngameCommands_serverstats.GetType(), this.m_IngameCommands_serverstats));
            lstReturn.Add(new CPluginVariable("Session Command:", this.m_IngameCommands_session.GetType(), this.m_IngameCommands_session));
            lstReturn.Add(new CPluginVariable("Dogtags Command:", this.m_IngameCommands_dogtags.GetType(), this.m_IngameCommands_dogtags));
            lstReturn.Add(new CPluginVariable("Top10 Command:", this.m_IngameCommands_top10.GetType(), this.m_IngameCommands_top10));
            lstReturn.Add(new CPluginVariable("Player Of The Day Command:", this.m_IngameCommands_playerOfTheDay.GetType(), this.m_IngameCommands_playerOfTheDay));
            lstReturn.Add(new CPluginVariable("Top10 for period Command:", this.m_IngameCommands_top10ForPeriod.GetType(), this.m_IngameCommands_top10ForPeriod));

            lstReturn.Add(new CPluginVariable("Periodical Webrequest On?(P&S Stats)", typeof(enumBoolYesNo), this.m_enWebrequest));
            if (this.m_enWebrequest == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Webaddress", this.m_webAddress.GetType(), this.m_webAddress));
                lstReturn.Add(new CPluginVariable("Webrequest Intervall", this.m_requestIntervall.GetType(), this.m_requestIntervall));
            }
            //Simple Stats
            lstReturn.Add(new CPluginVariable("Log playerdata only (no playerstats)?", typeof(enumBoolYesNo), this.m_enLogPlayerDataOnly));
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            if (strVariable.CompareTo("Host") == 0)
            {
                this.m_strHost = strValue;
            }
            else if (strVariable.CompareTo("Port") == 0)
            {
                this.m_strDBPort = strValue;
            }
            else if (strVariable.CompareTo("Database Name") == 0)
            {
                this.m_strDatabase = strValue;
            }
            else if (strVariable.CompareTo("UserName") == 0)
            {
                this.m_strUserName = strValue;
            }
            else if (strVariable.CompareTo("Password") == 0)
            {
                this.m_strPassword = strValue;
            }
            else if (strVariable.CompareTo("High performance mode(no connection limit!)") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.m_highPerformanceConnectionMode = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("Connection Pooling") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.m_connectionPooling = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
                this.m_highPerformanceConnectionMode = enumBoolOnOff.On;
            }
            else if (strVariable.CompareTo("Min Connection Pool Size") == 0)
            {
                Int32.TryParse(strValue, out this.m_minPoolSize);
                if (this.m_minPoolSize < 0 || this.m_minPoolSize > this.m_maxPoolSize)
                {
                    this.m_minPoolSize = 0;
                }
            }
            else if (strVariable.CompareTo("Max Connection Pool Size") == 0)
            {
                Int32.TryParse(strValue, out this.m_maxPoolSize);
                if (this.m_maxPoolSize < 1 || this.m_minPoolSize > this.m_maxPoolSize)
                {
                    this.m_maxPoolSize = 10;
                }
            }
            else if (strVariable.CompareTo("Failed Transaction retry attempts") == 0)
            {
                Int32.TryParse(strValue, out TransactionRetryCount);
                if (TransactionRetryCount < 1)
                {
                    TransactionRetryCount = 3;
                }
            }
            else if (strVariable.CompareTo("Minimum time(sec) between ServerInfo Updates") == 0)
            {
                if (Int32.TryParse(strValue, out this.minIntervalllenght))
                {
                    if (this.minIntervalllenght < 1)
                    {
                        this.minIntervalllenght = 30;
                    }
                }
                else
                {
                    this.minIntervalllenght = 30;
                }
            }
            else if (strVariable.CompareTo("Enable Chatlogging?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enChatloggingON = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Log ServerSPAM?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enNoServerMsg = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Instant Logging of Chat Messages?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enInstantChatlogging = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("Enable chatlog filter(Regex)?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enChatlogFilter = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Chatfilterrules(Regex)") == 0)
            {
                this.lstStrChatFilterRules = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                this.BuildRegexRuleset();
            }
            else if (strVariable.CompareTo("Enable Statslogging?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enLogSTATS = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            //Log playerdata only (no playerstats)?
            else if (strVariable.CompareTo("Log playerdata only (no playerstats)?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enLogPlayerDataOnly = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Enable Weaponstats?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_weaponstatsON = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Ranking by Score?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enRankingByScore = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Enable ingame commands?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enableInGameCommands = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Overall ranking(merged Serverranking)") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enOverallRanking = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Server group (0 - 128)") == 0)
            {
                if (Int32.TryParse(strValue, out this.intServerGroup))
                {
                    if (this.intServerGroup > 128 || this.intServerGroup < 0)
                    {
                        this.intServerGroup = 0;
                    }
                }
                else
                {
                    this.intServerGroup = 0;
                }
            }
            else if (strVariable.CompareTo("Send Stats to all Players?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enSendStatsToAll = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Enable Livescoreboard in DB?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enableCurrentPlayerstatsTable = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Enable KDR correction?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_kdrCorrection = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("PlayerMessage") == 0)
            {
                this.m_lstPlayerStatsMessage = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            // player of the day
            else if (strVariable.CompareTo("Player of the day Message") == 0)
            {
                this.m_lstPlayerOfTheDayMessage = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Weaponstats Message ") == 0)
            {
                this.m_lstWeaponstatsMsg = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            //Serverstats
            else if (strVariable.CompareTo("Serverstats Message") == 0)
            {
                this.m_lstServerstatsMsg = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Enable Welcomestats?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enWelcomeStats = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Awards ON?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_awardsON = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Yell Welcome Message(not the stats)?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enYellWelcomeMSG = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Welcome Message") == 0)
            {
                //this.m_strPlayerWelcomeMsg = strValue;
                this.m_lstPlayerWelcomeStatsMessage = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Welcome Message for new Player") == 0)
            {
                this.m_lstNewPlayerWelcomeMsg = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Welcomestats Delay") == 0 && Int32.TryParse(strValue, out int_welcomeStatsDelay) == true)
            {
                this.int_welcomeStatsDelay = Convert.ToInt32(strValue);
            }
            else if (strVariable.CompareTo("Top10 ingame") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enTop10ingame = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            //top10
            else if (strVariable.CompareTo("Top10 header line") == 0)
            {
                this.m_strTop10Header = strValue;
            }
            else if (strVariable.CompareTo("Top10 row format") == 0)
            {
                this.m_strTop10RowFormat = strValue;
            }
            // top10 for period
            else if (strVariable.CompareTo("Top10 for period header line") == 0)
            {
                this.m_strTop10HeaderForPeriod = strValue;
            }
            else if (strVariable.CompareTo("Top10 for period interval days") == 0)
            {
                if (Int32.TryParse(strValue, out this.m_intDaysForPeriodTop10) == false)
                {
                    this.m_intDaysForPeriodTop10 = 7;
                }
            }
            else if (strVariable.CompareTo("WeaponTop10 header line") == 0)
            {
                this.m_strWeaponTop10Header = strValue;
            }
            else if (strVariable.CompareTo("WeaponTop10 row format") == 0)
            {
                this.m_strWeaponTop10RowFormat = strValue;
            }
            else if (strVariable.CompareTo("DebugLevel") == 0)
            {
                this.GlobalDebugMode = strValue;
            }
            else if (strVariable.CompareTo("Keywordlist") == 0)
            {
                this.m_lstTableconfig = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("tableSuffix") == 0)
            {
                this.tableSuffix = strValue;
                this.prepareTablenames();
                this.setGameMod();
                this.boolTableEXISTS = false;
            }
            else if (strVariable.CompareTo("MapStats ON?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_mapstatsON = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Session ON?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_sessionON = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("SessionMessage") == 0)
            {
                this.m_lstSessionMessage = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Save Sessiondata to DB?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enSessionTracking = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Playerrequests per Round") == 0 && Int32.TryParse(strValue, out numberOfAllowedRequests) == true)
            {
                this.numberOfAllowedRequests = Convert.ToInt32(strValue);
            }
            else if (strVariable.CompareTo("Servertime Offset") == 0 && Double.TryParse(strValue, out m_dTimeOffset) == true)
            {
                this.m_dTimeOffset = Convert.ToDouble(strValue);
                this.MyDateTime = new myDateTime_W(this.m_dTimeOffset);
            }

            //Webrequest
            /*
        else if (strVariable.CompareTo("Periodical Webrequest On?(P&S Stats)") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
        {
            this.m_enWebrequest = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
        }
        else if (strVariable.CompareTo("Webaddress") == 0)
        {
            this.m_webAddress = strValue;
        }
        else if (strVariable.CompareTo("Webrequest Intervall") == 0 && Int32.TryParse(strValue, out this.m_requestIntervall) == true)
        {
            this.m_requestIntervall = Convert.ToInt32(strValue);
        }
             */
            else if (strVariable.CompareTo("Stats Command:") == 0)
            {
                this.m_IngameCommands_stats = strValue;
            }
            else if (strVariable.CompareTo("ServerStats Command:") == 0)
            {
                this.m_IngameCommands_serverstats = strValue;
            }
            else if (strVariable.CompareTo("Session Command:") == 0)
            {
                this.m_IngameCommands_session = strValue;
            }
            else if (strVariable.CompareTo("Dogtags Command:") == 0)
            {
                this.m_IngameCommands_dogtags = strValue;
            }
            else if (strVariable.CompareTo("Top10 Command:") == 0)
            {
                this.m_IngameCommands_top10 = strValue;
            }
            else if (strVariable.CompareTo("Player Of The Day Command:") == 0)
            {
                this.m_IngameCommands_playerOfTheDay = strValue;
            }
            else if (strVariable.CompareTo("Top10 for period Command:") == 0)
            {
                this.m_IngameCommands_top10ForPeriod = strValue;
            }
            this.RegisterAllCommands();
        }
    }
