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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using MySqlConnector;

using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;

namespace PRoConEvents
{
    public partial class CChatGUIDStatsLogger : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Variables and Constructor

        private MatchCommand loggerStatusCommand;

        //Proconvariables
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        //Tablebuilder
        private readonly object tablebuilderlock;

        //other locks
        private readonly object chatloglock;
        private readonly object sqlquerylock;
        private readonly object sessionlock;
        private readonly object streamlock;
        private readonly object ConnectionStringBuilderlock;
        private readonly object registerallcomandslock;

        //Dateoffset
        private myDateTime_W MyDateTime;
        private double m_dTimeOffset;

        //Logging
        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
        //Chatlog
        private static List<CLogger> ChatLog = new List<CLogger>();
        private List<string> lstStrChatFilterRules;
        private List<Regex> lstChatFilterRules;
        //Statslog
        private Dictionary<string, CStats> StatsTracker = new Dictionary<string, CStats>();
        //Dogtags
        private Dictionary<CKillerVictim, int> m_dicKnifeKills = new Dictionary<CKillerVictim, int>();
        //Session
        private Dictionary<string, CStats> m_dicSession = new Dictionary<string, CStats>();
        private CMapstats Mapstats;
        private CMapstats Nextmapinfo;
        private List<CStats> lstpassedSessions = new List<CStats>();

        //GameMod
        //private string m_strGameMod;

        //Spamprotection
        private int numberOfAllowedRequests;
        private CSpamprotection Spamprotection;

        //Keywords
        private List<string> m_lstTableconfig = new List<string>();
        private Dictionary<string, List<string>> m_dicKeywords = new Dictionary<string, List<string>>();

        //Weapondic
        private Dictionary<string, Dictionary<string, CStats.CUsedWeapon>> weaponDic = new Dictionary<string, Dictionary<string, CStats.CUsedWeapon>>();

        //DamageClassDic
        private Dictionary<string, string> DamageClass = new Dictionary<string, string>();

        //WelcomeStatsDic
        private Dictionary<string, DateTime> welcomestatsDic = new Dictionary<string, DateTime>();

        //Weapon Mapping Dictionary
        private Dictionary<string, int> WeaponMappingDic = new Dictionary<string, int>();

        //ServerID
        private int ServerID;

        //Awards
        private List<string> m_lstAwardTable = new List<string>();

        //Tablenames
        private string tbl_playerdata;
        private string tbl_playerstats;
        private string tbl_weaponstats;
        private string tbl_dogtags;
        private string tbl_mapstats;
        private string tbl_chatlog;
        private string tbl_bfbcs;
        private string tbl_awards;
        private string tbl_server;
        private string tbl_server_player;
        private string tbl_server_stats;
        private string tbl_playerrank;
        private string tbl_sessions;
        private string tbl_currentplayers;
        private string tbl_weapons;
        private string tbl_weapons_stats;
        private string tbl_games;
        private string tbl_teamscores;

        // Timelogging
        private bool bool_roundStarted;
        private DateTime Time_RankingStarted;

        //Other
        private Dictionary<string, CPlayerInfo> m_dicPlayers = new Dictionary<string, CPlayerInfo>();   //Players

        //ID Cache
        private Dictionary<string, C_ID_Cache> m_ID_cache = new Dictionary<string, C_ID_Cache>();

        //Various Variables
        //private int m_strUpdateInterval;
        private bool isStreaming;
        private string serverName;
        private bool m_isPluginEnabled;
        private bool boolTableEXISTS;
        private bool boolKeywordDicReady;
        private string tableSuffix;
        private bool MySql_Connection_is_activ;
        //Last time Stat Logger actively interacted with the database
        private DateTime lastDBInteraction = DateTime.MinValue;

        //Update skipswitches
        private bool boolSkipGlobalUpdate;
        private bool boolSkipServerUpdate;
        private bool boolSkipServerStatsUpdate;

        //Transaction retry
        private int TransactionRetryCount;

        //Playerstartcount
        private int intRoundStartCount;
        private int intRoundRestartCount;

        //Webrequest
        private int m_requestIntervall;
        private string m_webAddress;

        //BFBCS
        //private double BFBCS_UpdateInterval;
        //private int BFBCS_Min_Request;

        //Database Connection Variables
        private string m_strHost;
        private string m_strDBPort;
        private string m_strDatabase;
        private string m_strUserName;
        private string m_strPassword;
        //private string m_strDatabaseDriver;

        //Stats Message Variables        
        private List<string> m_lstPlayerStatsMessage;
        private List<string> m_lstPlayerOfTheDayMessage;
        private List<string> m_lstPlayerWelcomeStatsMessage;
        private List<string> m_lstNewPlayerWelcomeMsg;
        private List<string> m_lstWeaponstatsMsg;
        private List<string> m_lstServerstatsMsg;
        //private string m_strPlayerWelcomeMsg;
        //private string m_strNewPlayerWelcomeMsg;
        private int int_welcomeStatsDelay;
        private string m_strTop10Header;
        private string m_strTop10RowFormat;
        private string m_strWeaponTop10Header;
        private string m_strWeaponTop10RowFormat;

        //top10 for Period
        private string m_strTop10HeaderForPeriod;

        //Session
        private List<string> m_lstSessionMessage;

        //Debug
        private string GlobalDebugMode;

        //ServerGroup
        private int intServerGroup;

        //Bools for switch on and off funktions
        private enumBoolYesNo m_enNoServerMsg;	//Logging of Server Messages
        private enumBoolYesNo m_enLogSTATS; 	//Statslogging
        private enumBoolYesNo m_enWelcomeStats;	//WelcomeStats
        private enumBoolYesNo m_enYellWelcomeMSG;	// Yell Welcome Message
        private enumBoolYesNo m_enTop10ingame;		//Top10 ingame
        private enumBoolYesNo m_enRankingByScore;	//Ranking by Score
        private enumBoolYesNo m_enInstantChatlogging;	//Realtime Chatlogging
        private enumBoolYesNo m_enChatloggingON;	// Chatlogging On
        private enumBoolYesNo m_enChatlogFilter;    //Turn on the Chatlogfilter
        private enumBoolYesNo m_enSendStatsToAll;	//All Player see the Stats if someone enter @stats  @rank
        private enumBoolYesNo m_mapstatsON;			//Mapstats
        private enumBoolYesNo m_sessionON; 			//Sessionstats
        private enumBoolYesNo m_weaponstatsON;		//Turn Weaponstats On and Off
        private enumBoolYesNo m_getStatsfromBFBCS;  //Turn Statsfetching from BFBCS On and Off
        private enumBoolYesNo m_awardsON;			//Turn Awards on or off
        private enumBoolYesNo m_enWebrequest;		// Webrequest
        private enumBoolYesNo m_enOverallRanking;   //Overall Ranking
        private enumBoolYesNo m_enableInGameCommands; // Turn InGame Commands on and off
        private enumBoolOnOff m_highPerformanceConnectionMode;
        private enumBoolYesNo m_enSessionTracking;
        private enumBoolYesNo m_kdrCorrection; //Kill death Ratio Correction
        private enumBoolYesNo m_enableCurrentPlayerstatsTable; // experimental
        private enumBoolYesNo m_enLogPlayerDataOnly;
        private enumBoolOnOff m_connectionPooling; //Connection Pooling
        private enumBoolOnOff m_Connectioncompression;

        private int m_maxPoolSize; //Connection Pooling
        private int m_minPoolSize; //Connection Pooling

        //More Database Variables
        //Commands
        //Transactions
        private MySqlConnector.MySqlTransaction MySqlTrans;
        //Connections
        private MySqlConnector.MySqlConnection MySqlCon; //Select Querys 1
        private MySqlConnector.MySqlConnection MySqlChatCon; //MySqlConnection for Chatlogging 
        private MySqlConnector.MySqlConnection MySqlConn; //StartStreaming 2 

        MySqlConnectionStringBuilder myCSB = new MySqlConnectionStringBuilder();

        //ServerInfo Event fix
        private DateTime dtLastServerInfoEvent;
        private int minIntervalllenght;

        //Double Roundendfix
        private DateTime dtLastRoundendEvent;
        private DateTime dtLastOnListPlayersEvent;

        //Top10 for Period
        private int m_intDaysForPeriodTop10;

        //New In-Game Command System
        private string m_IngameCommands_stats;
        private string m_IngameCommands_serverstats;
        private string m_IngameCommands_session;
        private string m_IngameCommands_dogtags;
        private string m_IngameCommands_top10;
        private string m_IngameCommands_playerOfTheDay;
        private string m_IngameCommands_top10ForPeriod;

        private Dictionary<string, CStatsIngameCommands> dicIngameCommands = new Dictionary<string, CStatsIngameCommands>();

        //ServerGametype
        private string strServerGameType = String.Empty;
        private int intServerGameType_ID;

        public CChatGUIDStatsLogger()
        {
            loggerStatusCommand = new MatchCommand("CChatGUIDStatsLogger", "GetStatus", new List<string>(), "CChatGUIDStatsLogger_Status", new List<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.None), "Useable by other plugins to determine the current status of this plugin.");

            //tablebuilderlock
            this.tablebuilderlock = new object();
            //other locks
            this.chatloglock = new object();
            this.sqlquerylock = new object();
            this.sessionlock = new object();
            this.streamlock = new object();
            this.ConnectionStringBuilderlock = new object();
            this.registerallcomandslock = new object();


            //update skipswitch
            this.boolSkipGlobalUpdate = false;
            this.boolSkipServerUpdate = false;
            this.boolSkipServerStatsUpdate = false;

            //Timeoffset
            this.m_dTimeOffset = 0;
            this.MyDateTime = new myDateTime_W(this.m_dTimeOffset);

            //this.m_strUpdateInterval = 30;
            this.isStreaming = true;
            this.serverName = String.Empty;
            this.m_ID_cache = new Dictionary<string, C_ID_Cache>();
            this.m_dicKeywords = new Dictionary<string, List<string>>();
            this.boolKeywordDicReady = false;
            this.tableSuffix = String.Empty;
            this.Mapstats = new CMapstats(MyDateTime.Now, "START", 0, 0, this.m_dTimeOffset);
            this.MySql_Connection_is_activ = false;
            this.numberOfAllowedRequests = 10;

            //Transaction retry count
            TransactionRetryCount = 3;

            //Chatlog
            this.lstStrChatFilterRules = new List<string>();
            this.lstChatFilterRules = new List<Regex>();

            //BFBCS
            //this.BFBCS_UpdateInterval = 72; // hours
            //this.BFBCS_Min_Request = 2; //min Packrate

            //Playerstartcount
            this.intRoundStartCount = 2;
            this.intRoundRestartCount = 1;

            //Webrequest
            this.m_webAddress = String.Empty;
            this.m_requestIntervall = 60;

            //Databasehost
            this.m_strHost = String.Empty;
            this.m_strDBPort = String.Empty;
            this.m_strDatabase = String.Empty;
            this.m_strUserName = String.Empty;
            this.m_strPassword = String.Empty;

            //Various Bools
            this.bool_roundStarted = false;
            this.m_isPluginEnabled = false;
            this.boolTableEXISTS = false;

            //ServerGroup
            this.intServerGroup = 0;

            //Debug
            this.GlobalDebugMode = "Error";

            //Functionswitches
            this.m_enLogSTATS = enumBoolYesNo.No;
            this.m_enWelcomeStats = enumBoolYesNo.No;
            this.m_enYellWelcomeMSG = enumBoolYesNo.No;
            this.m_enTop10ingame = enumBoolYesNo.No;
            this.m_enRankingByScore = enumBoolYesNo.Yes;
            this.m_enNoServerMsg = enumBoolYesNo.No;
            this.m_enInstantChatlogging = enumBoolYesNo.No;
            this.m_enChatloggingON = enumBoolYesNo.No;
            this.m_enChatlogFilter = enumBoolYesNo.No;
            this.m_enSendStatsToAll = enumBoolYesNo.No;
            this.m_mapstatsON = enumBoolYesNo.No;
            this.m_sessionON = enumBoolYesNo.No;
            this.m_weaponstatsON = enumBoolYesNo.Yes;
            this.m_getStatsfromBFBCS = enumBoolYesNo.No;
            this.m_awardsON = enumBoolYesNo.No;
            this.m_enOverallRanking = enumBoolYesNo.No;
            this.m_enableInGameCommands = enumBoolYesNo.Yes;
            this.m_highPerformanceConnectionMode = enumBoolOnOff.Off;

            this.m_kdrCorrection = enumBoolYesNo.Yes; //Kill death Ratio Correction
            this.m_enableCurrentPlayerstatsTable = enumBoolYesNo.No; // experimental

            this.m_enLogPlayerDataOnly = enumBoolYesNo.No;

            this.m_connectionPooling = enumBoolOnOff.On; //Connection Pooling
            this.m_Connectioncompression = enumBoolOnOff.Off;

            this.m_minPoolSize = 0; //Connection Pooling
            this.m_maxPoolSize = 10; //Connection Pooling


            //Welcomestats
            //this.m_strPlayerWelcomeMsg = "[yell,4]Nice to see you on our Server again, %playerName%";
            //this.m_strNewPlayerWelcomeMsg = "[yell,4]Welcome to the %serverName% Server, %playerName%";
            this.int_welcomeStatsDelay = 5;
            this.welcomestatsDic = new Dictionary<string, DateTime>();

            //Playerstats
            this.m_lstPlayerStatsMessage = new List<string>();
            this.m_lstPlayerStatsMessage.Add("Serverstats for %playerName%:");
            this.m_lstPlayerStatsMessage.Add("Score: %playerScore%  %playerKills% Kills %playerHeadshots% HS  %playerDeaths% Deaths K/D: %playerKDR%");
            this.m_lstPlayerStatsMessage.Add("Your Serverrank is: %playerRank% of %allRanks%");

            //Player of the day
            this.m_lstPlayerOfTheDayMessage = new List<string>();
            this.m_lstPlayerOfTheDayMessage.Add("%playerName% is the Player of the day");
            this.m_lstPlayerOfTheDayMessage.Add("Score: %playerScore%  %playerKills% Kills %playerHeadshots% HS  %playerDeaths% Deaths K/D: %playerKDR%");
            this.m_lstPlayerOfTheDayMessage.Add("His Serverrank is: %playerRank% of %allRanks%");
            this.m_lstPlayerOfTheDayMessage.Add("Overall playtime for today: %playerPlaytime%");

            //Welcomestats
            this.m_lstPlayerWelcomeStatsMessage = new List<string>();
            this.m_lstPlayerWelcomeStatsMessage.Add("Nice to see you on our Server again, %playerName%");
            this.m_lstPlayerWelcomeStatsMessage.Add("Serverstats for %playerName%:");
            this.m_lstPlayerWelcomeStatsMessage.Add("Score: %playerScore%  %playerKills% Kills %playerHeadshots% HS  %playerDeaths% Deaths K/D: %playerKDR%");
            this.m_lstPlayerWelcomeStatsMessage.Add("Your Serverrank is: %playerRank% of %allRanks%");

            //Welcomestats new Player
            this.m_lstNewPlayerWelcomeMsg = new List<string>();
            this.m_lstNewPlayerWelcomeMsg.Add("Welcome to the %serverName% Server, %playerName%");

            //Weaponstats
            this.m_lstWeaponstatsMsg = new List<string>();
            this.m_lstWeaponstatsMsg.Add("%playerName%'s Stats for %Weapon%:");
            this.m_lstWeaponstatsMsg.Add("%playerKills% Kills  %playerHeadshots% Headshots  Headshotrate: %playerKHR%%");
            this.m_lstWeaponstatsMsg.Add("Your Weaponrank is: %playerRank% of %allRanks%");

            //Serverstats
            this.m_lstServerstatsMsg = new List<string>();
            this.m_lstServerstatsMsg.Add("Serverstatistics for server %serverName%");
            this.m_lstServerstatsMsg.Add("Unique Players: %countPlayer%  Totalplaytime: %sumPlaytime%");
            this.m_lstServerstatsMsg.Add("Totalscore: %sumScore% Avg. Score: %avgScore% Avg. SPM: %avgSPM%");
            this.m_lstServerstatsMsg.Add("Totalkills: %sumKills% Avg. Kills: %avgKills% Avg. KPM: %avgKPM%");

            //Session
            this.m_lstSessionMessage = new List<string>();
            this.m_lstSessionMessage.Add("%playerName%'s Session Data  Session started %SessionStarted%");
            this.m_lstSessionMessage.Add("Score: %playerScore%  %playerKills% Kills  %playerHeadshots% HS  %playerDeaths% Deaths K/D: %playerKDR%");
            this.m_lstSessionMessage.Add("Your Rank: %playerRank% (%RankDif%)  Sessionlength: %SessionDuration% Minutes");

            //Top10 Headers
            this.m_strTop10Header = "Top 10 Player of the %serverName% Server";
            this.m_strTop10RowFormat = "%Rank%. %playerName%  Score: %playerScore%  %playerKills% Kills  %playerHeadshots% Headshots  %playerDeaths% Deaths  KDR: %playerKDR%";
            this.m_strWeaponTop10Header = "Top 10 Player with %Weapon% of the %serverName%";
            this.m_strWeaponTop10RowFormat = "%Rank%.  %playerName%  %playerKills% Kills  %playerHeadshots% Headshots  %playerDeaths% Deaths Headshotrate: %playerKHR%%";

            //Top10 for Period
            this.m_strTop10HeaderForPeriod = "Top 10 Player of the %serverName% Server over the last %intervaldays% days";

            //Awards
            this.m_lstAwardTable = new List<string>();
            this.m_lstAwardTable.Add("First");
            this.m_lstAwardTable.Add("Second");
            this.m_lstAwardTable.Add("Third");
            this.m_lstAwardTable.Add("Purple_Heart");
            this.m_lstAwardTable.Add("Best_Combat");
            this.m_lstAwardTable.Add("Killstreak_5");
            this.m_lstAwardTable.Add("Killstreak_10");
            this.m_lstAwardTable.Add("Killstreak_15");
            this.m_lstAwardTable.Add("Killstreak_20");

            //ServerID
            this.ServerID = 0;

            //Tableconfig Tweaks for friendly weapon names
            this.m_lstTableconfig = new List<string>();
            this.m_lstTableconfig.Add("870MCS{870,870MCS}");
            this.m_lstTableconfig.Add("AEK-971{AEK,AEK971,AEK-971}");
            this.m_lstTableconfig.Add("AKS-74u{AKSU,AKS-74,AKSU-74,AKS-74U}");
            this.m_lstTableconfig.Add("AN-94 Abakan{ABAKAN,AN94,AN-94}");
            this.m_lstTableconfig.Add("AS Val{ASVAL,AS-VAL,AS VAL}");
            this.m_lstTableconfig.Add("DAO-12{DAO12,DAO,DAO-12}");
            this.m_lstTableconfig.Add("death{DEATH}");
            this.m_lstTableconfig.Add("Defib{DEFIBRILLATOR,DEFIB,PADDLE,PADDLES}");
            this.m_lstTableconfig.Add("F2000{F2000}");
            this.m_lstTableconfig.Add("FAMAS{FAMAS}");
            this.m_lstTableconfig.Add("FGM-148{JAVELIN,FGM148,FGM-148}");
            this.m_lstTableconfig.Add("FIM92{STINGER,FIM92,FIM-92}");
            this.m_lstTableconfig.Add("Glock18{GLOCK,GLOCK18,GLOCK-18}");
            this.m_lstTableconfig.Add("HK53{HK53,HK-53,G53,G-53,HK-G53}");
            this.m_lstTableconfig.Add("jackhammer{JACKHAMMER,MK3A1,MK3}");
            this.m_lstTableconfig.Add("JNG90{JNG-90,JNG90,JNG}");
            this.m_lstTableconfig.Add("L96{L-96,L96}");
            this.m_lstTableconfig.Add("LSAT{LSAT}");
            this.m_lstTableconfig.Add("M416{M-416,M416}");
            this.m_lstTableconfig.Add("M417{M-417,M417}");
            this.m_lstTableconfig.Add("M1014{M-1014,1014,M1014}");
            this.m_lstTableconfig.Add("M15 AT Mine{M15,M15 MINE,AT MINE,ATMINE,ATM,M15-ATM}");
            this.m_lstTableconfig.Add("M16A4{M-16,M16,M16A3,M16-A3,M16A4,M16-A4}");
            this.m_lstTableconfig.Add("M1911{1911,M1911}");
            this.m_lstTableconfig.Add("M240{M-240,M240}");
            this.m_lstTableconfig.Add("M249{M-249,M249,SAW}");
            this.m_lstTableconfig.Add("M26Mass{M26,M-26,MASS,M26MASS}");
            this.m_lstTableconfig.Add("M27IAR{M27,M-27,M27IAR}");
            this.m_lstTableconfig.Add("M320{M-320,GRENADE LAUNCHER,M320}");
            this.m_lstTableconfig.Add("M39{M-39,M39}");
            this.m_lstTableconfig.Add("M40A5{M40,M-40,M40A5}");
            this.m_lstTableconfig.Add("M4A1{M4,M-4,M4A1}");
            this.m_lstTableconfig.Add("M60{M-60,M60}");
            this.m_lstTableconfig.Add("M67{HANDGRENADE,GRENADE,M67,M-67}");
            this.m_lstTableconfig.Add("M9{M-9,M9}");
            this.m_lstTableconfig.Add("M93R{M93,M93R}");
            this.m_lstTableconfig.Add("Medkit{MEDKIT}");
            this.m_lstTableconfig.Add("MG36{MG-36,MG36}");
            this.m_lstTableconfig.Add("Mk11{MK-11,MK11}");
            this.m_lstTableconfig.Add("Model98B{M98,M98B,MODEL98,MODEL-98,MODEL98B,MODEL-98B}");
            this.m_lstTableconfig.Add("MP7{MP-7,MP7}");
            this.m_lstTableconfig.Add("Pecheneg{PKP-PECHENEG,PKP,PECHENEG}");
            this.m_lstTableconfig.Add("PP-19{PP19,PP-19}");
            this.m_lstTableconfig.Add("PP-2000{PP2000,PP-2000}");
            this.m_lstTableconfig.Add("QBB-95{QBB,QBB95,QBB-95}");
            this.m_lstTableconfig.Add("QBU-88{QBU,QBU88,QBU-88}");
            this.m_lstTableconfig.Add("QBZ-95{QBZ,QBZ95,QBZ-95}");
            this.m_lstTableconfig.Add("Repair Tool{REPAIRTOOL,TOOL,TORCH,BLOWTORCH}");
            this.m_lstTableconfig.Add("RoadKill{ROADKILL}");
            this.m_lstTableconfig.Add("RPG-7{RPG,RPG7,RPG7V2,RPG-7V2}");
            this.m_lstTableconfig.Add("RPK-74M{RPK,RPK74,RPK-74,RPK74M,RPK-74M}");
            this.m_lstTableconfig.Add("Weapons/SCAR-H/SCAR-H{SCAR,SCAR-H,SCARH}");
            this.m_lstTableconfig.Add("SCAR-L{SCARL,SCAR-L}");
            this.m_lstTableconfig.Add("SG 553 LB{SG553,SG-553,SG-553LB}");
            this.m_lstTableconfig.Add("Siaga20k{SAIGA,SAIGA20K,SIAGA,SIAGA20K}");
            this.m_lstTableconfig.Add("SKS{SKS}");
            this.m_lstTableconfig.Add("SMAW{SMAW}");
            this.m_lstTableconfig.Add("SPAS-12{SPAS12,SPAS,SPAS-12}");
            this.m_lstTableconfig.Add("Suicide{SUICIDE}");
            this.m_lstTableconfig.Add("SV98{SV-98,SV98}");
            this.m_lstTableconfig.Add("SVD{SVD,DRAGUNOV}");
            this.m_lstTableconfig.Add("Steyr AUG{STEYR,AUGA3,AUG-A3,AUG}");
            this.m_lstTableconfig.Add("Taurus .44{TAURUS,.44MAGNUM,TAURUS.44,MAGNUM,.44}");
            this.m_lstTableconfig.Add("Type88{TYPE88,TYPE-88}");
            this.m_lstTableconfig.Add("USAS-12{USAS12,USAS}");
            this.m_lstTableconfig.Add("Weapons/A91/A91{A91,A-91}");
            this.m_lstTableconfig.Add("Weapons/AK74M/AK74{AK74,AK-74,AKM,AK-74M,AK74M}");
            this.m_lstTableconfig.Add("Weapons/G36C/G36C{G36,G36C,G-36,G-36C}");
            this.m_lstTableconfig.Add("Weapons/G3A3/G3A3{G3,G-3,G3A3,G3-A3}");
            this.m_lstTableconfig.Add("Weapons/Gadgets/C4/C4{C4,C-4}");
            this.m_lstTableconfig.Add("Weapons/Gadgets/Claymore/Claymore{CLAYMORE,LANDMINE,APMINE,AP-MINE,APM,M18,M-18,M18-CLAYMORE}");
            this.m_lstTableconfig.Add("Weapons/KH2002/KH2002{KH2002,KH-2002}");
            this.m_lstTableconfig.Add("Weapons/Knife/Knife{KNIFE,MELEE}");
            this.m_lstTableconfig.Add("Weapons/MagpulPDR/MagpulPDR{PDW-R,PDWR,PDR,PDW}");
            this.m_lstTableconfig.Add("Weapons/MP412Rex/MP412REX{MP412REX,REX,MP-412,MP412}");
            this.m_lstTableconfig.Add("Weapons/MP443/MP443{MP-443,MP443,GRACH}");
            this.m_lstTableconfig.Add("Weapons/P90/P90{P-90,P90}");
            this.m_lstTableconfig.Add("Weapons/Sa18IGLA/Sa18IGLA{SA18,SA-18,IGLA,SA18IGLA,SA18-IGLA,SA-18IGLA}");
            this.m_lstTableconfig.Add("Weapons/UMP45/UMP45{UMP45,UMP-45,UMP}");
            this.m_lstTableconfig.Add("Weapons/XP1_L85A2/L85A2{L85,L85A2,L-85,L-85A2,L85-A2}");
            this.m_lstTableconfig.Add("Weapons/XP2_ACR/ACR{ACWR,ACW-R,ACR,AC-R}");
            this.m_lstTableconfig.Add("Weapons/XP2_L86/L86{L86,L86A2,L-86,L-86A2,L86-A2}");
            this.m_lstTableconfig.Add("Weapons/XP2_MP5K/MP5K{MP5,MP5K,M5K,MP-5,MP-5K,M5-K}");
            this.m_lstTableconfig.Add("Weapons/XP2_MTAR/MTAR{MTAR,MTAR21,MTAR-21}");

            //ServerInfo Event fix
            this.dtLastServerInfoEvent = DateTime.Now;
            this.minIntervalllenght = 60;

            //Double Roundendfix
            this.dtLastRoundendEvent = DateTime.MinValue;
            this.dtLastOnListPlayersEvent = DateTime.MinValue;

            //Top10 for Period
            this.m_intDaysForPeriodTop10 = 7;

            //New In-Game Command System
            this.m_IngameCommands_stats = "stats,rank";
            this.m_IngameCommands_serverstats = "serverstats";
            this.m_IngameCommands_session = "session";
            this.m_IngameCommands_dogtags = "dogtags";
            this.m_IngameCommands_top10 = "top10";
            this.m_IngameCommands_playerOfTheDay = "playeroftheday,potd";
            this.m_IngameCommands_top10ForPeriod = "weektop10,wtop10";
        }

        #region PluginSetup
        public string GetPluginName()
        {
            return "PRoCon Chat, GUID, Stats and Map Logger";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.4";
        }

        public string GetPluginAuthor()
        {
            return "[GWC]XpKiller";
        }

        public string GetPluginWebsite()
        {
            return "www.german-wildcards.de";
        }

        public string GetPluginDescription()
        {
            return @"
If you like my Plugins, please feel free to donate<br>
<p><form action='https://www.paypal.com/cgi-bin/webscr' target='_blank' method='post'>
<input type='hidden' name='cmd' value='_s-xclick'>
<input type='hidden' name='hosted_button_id' value='3B2FEDDHHWUW8'>
<input type='image' src='https://www.paypal.com/en_US/i/btn/btn_donate_SM.gif' border='0' name='submit' alt='PayPal - The safer, easier way to pay online!'>
<img alt='' border='0' src='https://www.paypal.com/de_DE/i/scr/pixel.gif' width='1' height='1'>
</form></p>

   
<h2>Description</h2>
    <p>This plugin is used to log player chat, player GUID's, player Stats, Weaponstats and Mapstats.</p>
    <p>This inludes: Chat, PBGUID, EAGUID, IP, Stats, Weaponstats, Dogtags, Killstreaks, Country, ClanTag, ... to be continued.. ;-)</p>
    
<h2>Requirements</h2>
	<p>It requires the use of a MySQL database with INNODB engine, that allows remote connections.(MYSQL Version 5.1.x, 5.5.x or higher is recommendend!!!)</p>
	<p>Also you should give INNODB some more Ram because the plugin mainly uses this engine if you need help feel free to ask me</p>
	<p>The Plugin will create the tables by itself.</p>
	<p>Pls Give FEEDBACK !!!</p>

<h2>Installation</h2>
<p>Download and install this plugin</p>
<p>Setup your Database, this means create a database and the user for it. I highly recommend NOT to use your root user. Just create a user with all rights for your newly created database </p>
<p>I recommend MySQL 5.1.x, 5.5.x or greater (5.0.x could work too, not tested) Important: <b>Your database need INNODB Support</b></p>
<p>Start Procon</p>
<p>Go to Tools --> Options --> Plugins --> Enter you databaseserver under outgoing Connections and allow all outgoing connections</p>
<p>Restart Procon</p>
<p>Enter your settings into Plugin Settings and THEN enable the plugin</p>
<p>Now the plugin should work if not request help in the <a href='https://myrcon.net' target='_blank'>Forum</a></p>

	
<h2>Things you have to know:</h2>
You can add additional Names for weapons in the Pluginsettings 
Use comma to seperate the words. <br>
Example: M16A4{M16} --> 40MMGL{M16,M16A3}  <br><br>



<h2>Ingame Commands (defaults!)</h2>
	<blockquote><h4>[@,#,!]stats</h4>Tells the Player their own Serverstats</blockquote>
	<blockquote><h4>[@,#,!]rank</h4>Tells the Player their own Serverstats</blockquote>
	<blockquote><h4>[@,#,!]potd</h4>Show up the player of the day</blockquote>
	<blockquote><h4>[@,#,!]playeroftheday</h4>Show up the player of the day</blockquote>
	<blockquote><h4>[@,#,!]session</h4>Tells the Player their own Sessiondata</blockquote>
	<blockquote><h4>[@,#,!]top10</h4>Tells the Player the Top10 players of the server</blockquote>
	<blockquote><h4>[@,#,!]wtop10</h4>Tells the Player the Top10 players of the server for specific period</blockquote>
	<blockquote><h4>[@,#,!]weektop10</h4>Tells the Player the Top10 players of the server for specific period</blockquote>
	<blockquote><h4>[@,#,!]stats WeaponName</h4>Tells the Player their own Weaponstats for the specific Weapon</blockquote>
	<blockquote><h4>[@,#,!]rank WeaponName</h4>Privately Tells the Player their own Weaponstats for the specific Weapon</blockquote>
	<blockquote><h4>[@,#,!]top10 WeaponName</h4>Privately Tells the Player the Top10 Player for the specific Weapon of the server</blockquote>
	<blockquote><h4>[@,#,!]dogtags WeaponName</h4>Privately Tells the Player his Dogtagstats </blockquote>
	<blockquote><h4>[@,#,!]serverstats</h4>Tells the Player the Serverstats</blockquote>

<h2>Replacement Strings for Playerstats and Player of the day</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
	<tr><td>%playerName%</td><td>Will be replaced by the player's name</td></tr>
	<tr><td>%playerScore%</td><td>Will be replaced by the player's totalscore on this server</td></tr>
	<tr><td>%SPM%</td><td>Will be replaced by the Player's score per minute on this server</td></tr>
	<tr><td>%playerKills%</td><td>Will be replaced by the player's totalkills on this server</td></tr>
	<tr><td>%playerHeadshots%</td><td>Will be replaced by the player's totalheadshots on this server</td></tr>
	<tr><td>%playerDeaths%</td><td>Will be replaced by the player's totaldeaths on this server</td></tr>
	<tr><td>%playerKDR%</td><td>Will be replaced by the player's kill death ratio on this server</td></tr>
	<tr><td>%playerSucide%</td><td>Will be replaced by the player's sucides on this server</td></tr>
	<tr><td>%playerPlaytime%</td><td>Will be replaced by the player's totalplaytime on this server in hh:mm:ss format</td></tr>
	<tr><td>%rounds%</td><td>Will be replaced by the player's totalrounds played on this server</td></tr>
	<tr><td>%playerRank%</td><td>Will be replaced by the player's concurrent serverrank</td></tr>
	<tr><td>%allRanks%</td><td>Will be replaced by the player's concurrent serverrank</td></tr>
	<tr><td>%killstreak%</td><td>Will be replaced by the player's best Killstreak</td></tr>
	<tr><td>%deathstreak%</td><td>Will be replaced by the player's worst Deathstreak</td></tr>
	</table>
	<br>

<h2>Replacement Strings for Top10</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
    <tr><td>%serverName%</td><td>Will be replaced by the Server name (Header only)</td></tr>
    <tr><td>%Rank%</td><td>Will be replaced by the player's rank</td></tr>
	<tr><td>%playerName%</td><td>Will be replaced by the player's name</td></tr>
	<tr><td>%playerScore%</td><td>Will be replaced by the player's totalscore on this server</td></tr>
	<tr><td>%playerKills%</td><td>Will be replaced by the player's totalkills on this server</td></tr>
	<tr><td>%playerHeadshots%</td><td>Will be replaced by the player's totalheadshots on this server</td></tr>
	<tr><td>%playerDeaths%</td><td>Will be replaced by the player's totaldeaths on this server</td></tr>
	<tr><td>%playerKDR%</td><td>Will be replaced by the player's kill death ratio on this server</td></tr>
    <tr><td>%playerKHR%</td><td>Will be replaced by the player's Headshot Kill ratio on this server</td></tr>
    <tr><td>%intervaldays%</td><td>Will be replaced interval of days (top10 for a period only)</td></tr>
	</table>
	<br>
	
	<h2>Replacement Strings for Weaponstats</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
	<tr><td>%playerName%</td><td>Will be replaced by the player's name</td></tr>
	<tr><td>%playerKills%</td><td>Will be replaced by the player's Totalkills on this server with the specific Weapon</td></tr>
	<tr><td>%playerHeadshots%</td><td>Will be replaced by the player's Totalheadshotkills on this server the specific Weapon</td></tr>
	<tr><td>%playerDeaths%</td><td>Will be replaced by the player's totaldeaths on this server caused by this specific Weapon</td></tr>
	<tr><td>%playerKHR%</td><td>Will be replaced by the player's Headshotkill ratio on this server with the specific Weapon</td></tr>
	<tr><td>%playerKDR%</td><td>Will be replaced by the player's kill death ratio on this server with the specific Weapon</td></tr>
	<tr><td>%playerRank%</td><td>Will be replaced by the player's current Serverrank for the specific Weapon</td></tr>
	<tr><td>%allRanks%</td><td>Will be replaced by current Number of Player in Serverrank for the specific Weapon</td></tr>
	<tr><td>%killstreak%</td><td>Will be replaced by the player's best Killstreak</td></tr>
	<tr><td>%deathstreak%</td><td>Will be replaced by the player's worst Deathstreak</td></tr>
	</table>

    <h2>Replacement Strings for serverstats</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
	<tr><td>%countPlayer%</td><td>Will be replaced by the number of unique players on this server</td></tr>
    <tr><td>%sumScore%</td><td>Will be replaced by the Totalscore of all players combined on this server</td></tr>
    <tr><td>%sumKills%</td><td>Will be replaced by the Totalkills of all players combined on this server</td></tr>
    <tr><td>%sumHeadshots%</td><td>Will be replaced by the TotalHeadshots of all players combined on this server</td></tr>
    <tr><td>%sumDeaths%</td><td>Will be replaced by the Totaldeaths of all players combined on this server</td></tr>
    <tr><td>%sumTKs%</td><td>Will be replaced by the TotalTeamkills of all players combined on this server</td></tr>
    <tr><td>%sumRounds%</td><td>Will be replaced by the Totalrounds of all players combined on this server</td></tr>
    <tr><td>%sumSuicide%</td><td>Will be replaced by the Totalsuicide of all players combined on this server</td></tr>
    <tr><td>%avgScore%</td><td>Will be replaced by the average score of all players combined on this server</td></tr>
    <tr><td>%avgKills%</td><td>Will be replaced by the average kills of all players combined on this server</td></tr>
    <tr><td>%avgHeadshots%</td><td>Will be replaced by the average Headshots of all players combined on this server</td></tr>
    <tr><td>%avgDeaths%</td><td>Will be replaced by the average deaths of all players combined on this server</td></tr>
    <tr><td>%avgTKs%</td><td>Will be replaced by the average teamkills of all players combined on this server</td></tr>
    <tr><td>%avgSuicide</td><td>Will be replaced by the average suicides of all players combined on this server</td></tr>
    <tr><td>%avgRounds%</td><td>Will be replaced by the average rounds of all players combined on this server</td></tr>
    <tr><td>%avgSPM%</td><td>Will be replaced by the average Score per Minute of all players combined on this server</td></tr>
    <tr><td>%avgKPM%</td><td>Will be replaced by the average Kills per Minute of all players combined on this server</td></tr>
    <tr><td>%avgHPM%</td><td>Will be replaced by the average Headshots per Minute of all players combined on this server</td></tr>
    <tr><td>%avgHPK%</td><td>Will be replaced by the average Headshots per Kills (unit procent (%)) of all players combined on this server</td></tr>
    <tr><td>%sumPlaytime%</td><td>Will be replaced by the Total Playtime (format: dd:hh:mm:ss) of all players combined on this server</td></tr>
    <tr><td>%avgPlaytime%</td><td>Will be replaced by the average Playtime (format: dd:hh:mm:ss) of all players combined on this server</td></tr>
    <tr><td>%sumPlaytimeHours%</td><td>Will be replaced by the Total Playtime (format rounded hours) of all players combined on this server</td></tr>
    <tr><td>%avgPlaytimeHours%</td><td>Will be replaced by the average Playtime (format rounded hours) of all players combined on this server</td></tr>
    <tr><td>%sumPlaytimeDays%</td><td>Will be replaced by the Total Playtime (format rounded days) of all players combined on this server</td></tr>  
    <tr><td>%avgPlaytimeDays%</td><td>Will be replaced by the average Playtime (format rounded days) of all players combined on this server</td></tr>  
	</table>
	
	<h2>Replacement Strings for PlayerSession</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
	<tr><td>%playerName%</td><td>Will be replaced by the player's name</td></tr>
	<tr><td>%playerScore%</td><td>Will be replaced by the player's totalscore of the concurrent Session</td></tr>
	<tr><td>%playerKills%</td><td>Will be replaced by the player's totalkills of the concurrent Session</td></tr>
	<tr><td>%playerHeadshots%</td><td>Will be replaced by the player's totalheadshots of the concurrent Session</td></tr>
	<tr><td>%playerDeaths%</td><td>Will be replaced by the player's totaldeaths of the concurrent Session</td></tr>
	<tr><td>%playerKDR%</td><td>Will be replaced by the player's kill death ratio of the concurrent Session</td></tr>
	<tr><td>%playerSucide%</td><td>Will be replaced by the player's sucides of the concurrent Session</td></tr>
	<tr><td>%SessionDuration%</td><td>Will be replaced by the player's totalplaytime of the concurrent Session in Minutes</td></tr>
	<tr><td>%playerRank%</td><td>Will be replaced by the player's concurrent serverrank</td></tr>
	<tr><td>%RankDif%</td><td>Will be replaced by the player's rank change</td></tr>
	<tr><td>%SessionStarted%</td><td>Will be replaced by the player's start of the Session</td></tr>
	<tr><td>%killstreak%</td><td>Will be replaced by the player's best Killstreak of the Session</td></tr>
	<tr><td>%deathstreak%</td><td>Will be replaced by the player's worst Deathstreak of the Session</td></tr>
	</table>
	<br>
	
    <h2>How to yell/say messages</h2>
    <p>Every ingame messages can be yelled to the Player.</p>
    <p>Just add the yelltag in front of every line of you message which should be yelled.</p>
    <p>Usage:[yell,duration in seconds]Your messages</p>
    <p>Like:[yell,3]Welcome on our server!</p>
    <p>This would be yell for 3 seconds</p>
    <p>Hint: You can mixed normal say and yell without any problems.</p>
    <p>Messages without Tag will will be transmitted with the say command.</p>
<br>
    


	<h3>NOTE:</h3>
		<p>Tracked stats are: Kills, Headshots, Deaths, All Weapons, TKs, Suicides, Score, Playtime, Rounds, MapStats, Dogtags </p>
		<p>The Rank is created dynamical from Query in  my opinion much better than write it back to database.</p>
		<p>The Stats are written to the Database at the end of the round</p>
	
<h3>Known issues:</h3>
<p>Vehicles cannot be tracked due limitations in the Rcon Protocol blame EA/Dice for it</p>

		
<h3>Changelog:</h3><br>
<b>1.0.0.4</b><br>
Allow NON PB enabled Servers to use the Plugin for Stats tracking. See <a href='https://github.com/AdKats/ChatGUIDStatsAndMapstatsLogger/issues/5' target='_blank'>#5</a>. Thanks @icecoldme<br>
<b>1.0.0.2</b><br>
Bugfixes for column errors.<br>
Bugfixes for the sessions streaming bug<br>
Weaponstats working again. <br>
Bugfix for Identifier name is too long.<br>

<br>
<b>1.0.0.1</b><br>
Bugfixes for value too long for column errors.<br>
Bugfixes for some other bugs<br>
Changed deprecated Tracemessages<br>
Added an error prefix in pluginlog <br>
New feature: Tickets/teamscores are now tracked in tbl_teamscores<br>
New feature: Simple Stats (collects playerdata only)<br>
New feature: Switch for disabling weaponstats
<br><br>
<b>1.0.0.0</b><br>
First Release<br>
Multigame Support<br>
<br><br>


";
        }

        public void OnPluginLoadingEnv(List<string> lstPluginEnv)
        {
            this.strServerGameType = lstPluginEnv[1].ToUpper();
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerAuthenticated", "OnPlayerJoin", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnPunkbusterMessage", "OnPunkbusterPlayerInfo", "OnServerInfo", "OnLevelLoaded",
                                                     "OnPlayerKilled", "OnPlayerLeft", "OnRoundOverPlayers", "OnPlayerSpawned", "OnLoadingLevel", "OnCommandStats", "OnCommandTop10", "OnCommandDogtags", "OnCommandServerStats",
                                                     "OnRoundStartPlayerCount", "OnRoundRestartPlayerCount", "OnRoundOver");

            // Register the logger status match command
            // This command can be called for status whether logger is enabled or not
            this.RegisterCommand(loggerStatusCommand);
        }

        public void OnPluginEnable()
        {
            isStreaming = true;
            this.serverName = String.Empty;
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon Chat, GUID and Stats Logger ^2Enabled");
            this.Spamprotection = new CSpamprotection(numberOfAllowedRequests);
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon Chat, GUID and Stats Logger: ^2 Floodprotection set to " + this.numberOfAllowedRequests.ToString() + " Request per Round for each Player");
            // Register Commands
            this.m_isPluginEnabled = true;
            this.prepareTablenames();
            this.setGameMod();
            this.MyDateTime = new myDateTime_W(this.m_dTimeOffset);
            //Webrequest
            if (this.m_enWebrequest == enumBoolYesNo.Yes)
            {
                //this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", "30", (this.m_requestIntervall * 60).ToString(), "-1", "procon.protected.plugins.call", "CChatGUIDStatsLogger", "Threadstarter_Webrequest");
            }
            this.RegisterAllCommands();
            this.generateWeaponList();
            //Start intial tablebuilder thread
            ThreadPool.QueueUserWorkItem(delegate { this.tablebuilder(); });
        }

        public void OnPluginDisable()
        {
            isStreaming = false;
            if (MySqlCon != null)
                if (MySqlCon.State == ConnectionState.Open)
                {
                    try
                    {
                        MySqlCon.Close();
                    }
                    catch { }
                }
            if (MySqlConn != null)
                if (MySqlConn.State == ConnectionState.Open)
                {
                    try
                    {
                        MySqlConn.Close();
                    }
                    catch { }
                }

            //Destroying all current Coonection Pool if availble:
            try
            {
                MySqlConnection.ClearAllPools();
            }
            catch { }

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon Chat, GUID and Stats Logger ^1Disabled");

            //Unregister Commands
            this.m_isPluginEnabled = false;
            //Webrequest
            this.ExecuteCommand("procon.protected.tasks.remove", "CChatGUIDStatsLogger");
            this.UnregisterAllCommands();
        }

        private List<string> GetExcludedCommandStrings(string strAccountName)
        {
            List<string> lstReturnCommandStrings = new List<string>();
            List<MatchCommand> lstCommands = this.GetRegisteredCommands();
            CPrivileges privileges = this.GetAccountPrivileges(strAccountName);
            foreach (MatchCommand mtcCommand in lstCommands)
            {
                if (mtcCommand.Requirements.HasValidPermissions(privileges) == true && lstReturnCommandStrings.Contains(mtcCommand.Command) == false)
                {
                    lstReturnCommandStrings.Add(mtcCommand.Command);
                }
            }
            return lstReturnCommandStrings;
        }

        private List<string> GetCommandStrings()
        {
            List<string> lstReturnCommandStrings = new List<string>();
            List<MatchCommand> lstCommands = this.GetRegisteredCommands();
            foreach (MatchCommand mtcCommand in lstCommands)
            {
                if (lstReturnCommandStrings.Contains(mtcCommand.Command) == false)
                {
                    lstReturnCommandStrings.Add(mtcCommand.Command);
                }
            }
            return lstReturnCommandStrings;
        }

        private void UnregisterAllCommands()
        {
            this.setupIngameCommandDic();
            try
            {
                foreach (KeyValuePair<string, CStatsIngameCommands> kvp in this.dicIngameCommands)
                {
                    if (kvp.Value.commands != string.Empty)
                    {
                        foreach (string command in kvp.Value.commands.Split(','))
                        {
                            this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", kvp.Value.functioncall.ToString(), this.Listify<string>("@", "!", "#"), command.ToString(), this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), kvp.Value.description.ToString()));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.DebugInfo("Error", "Error in UnregisterAllCommands: " + e);
            }
        }

        private void SetupHelpCommands()
        {

        }

        private void RegisterAllCommands()
        {
            lock (this.registerallcomandslock)
            {
                this.setupIngameCommandDic();
                if (this.m_isPluginEnabled == true)
                {
                    if (this.m_enableInGameCommands == enumBoolYesNo.No)
                    {
                        this.UnregisterAllCommands();
                        return;
                    }
                    this.SetupHelpCommands();

                    try
                    {
                        foreach (KeyValuePair<string, CStatsIngameCommands> kvp in this.dicIngameCommands)
                        {
                            if (kvp.Value.commands != string.Empty)
                            {
                                foreach (string command in kvp.Value.commands.Split(','))
                                {
                                    if (kvp.Value.boolEnabled)
                                    {
                                        this.RegisterCommand(new MatchCommand("CChatGUIDStatsLogger", kvp.Value.functioncall, this.Listify<string>("@", "!", "#"), command, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), kvp.Value.description));
                                    }
                                    else
                                    {
                                        this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", kvp.Value.functioncall, this.Listify<string>("@", "!", "#"), command, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), kvp.Value.description));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        this.DebugInfo("Error", "Error in RegisterAllCommands: " + e);
                    }
                }
            }
        }

        private void setupIngameCommandDic()
        {
            lock (this.dicIngameCommands)
            {
                bool boolenable = false;
                this.dicIngameCommands.Clear();
                if (this.m_enLogSTATS == enumBoolYesNo.Yes && this.m_enableInGameCommands == enumBoolYesNo.Yes)
                {
                    boolenable = true;
                }
                this.dicIngameCommands.Add("playerstats", new CStatsIngameCommands(this.m_IngameCommands_stats, "OnCommandStats", boolenable, "Provides a player his personal serverstats"));
                this.dicIngameCommands.Add("serverstats", new CStatsIngameCommands(this.m_IngameCommands_serverstats, "OnCommandServerStats", boolenable, "Provides a player his personal serverstats"));
                this.dicIngameCommands.Add("dogtagstats", new CStatsIngameCommands(this.m_IngameCommands_dogtags, "OnCommandDogtags", boolenable, "Provides a player his personal dogtagstats"));
                this.dicIngameCommands.Add("session", new CStatsIngameCommands(this.m_IngameCommands_session, "OnCommandSession", boolenable, "Provides a player his personal sessiondata"));
                this.dicIngameCommands.Add("playeroftheday", new CStatsIngameCommands(this.m_IngameCommands_playerOfTheDay, "OnCommandPlayerOfTheDay", boolenable, "Provides the player of the day stats"));

                if (this.m_enLogSTATS == enumBoolYesNo.Yes && this.m_enTop10ingame == enumBoolYesNo.Yes && this.m_enableInGameCommands == enumBoolYesNo.Yes)
                {
                    this.dicIngameCommands.Add("top10", new CStatsIngameCommands(this.m_IngameCommands_top10, "OnCommandTop10", true, "Provides a player top10 Players"));
                    this.dicIngameCommands.Add("top10forperiode", new CStatsIngameCommands(this.m_IngameCommands_top10ForPeriod, "OnCommandTop10ForPeriod", true, "Provides a player top10 Players for a specific timeframe"));
                }
                else
                {
                    this.dicIngameCommands.Add("top10", new CStatsIngameCommands(this.m_IngameCommands_top10, "OnCommandTop10", false, "Provides a player top10 Players"));
                    this.dicIngameCommands.Add("top10forperiode", new CStatsIngameCommands(this.m_IngameCommands_top10ForPeriod, "OnCommandTop10ForPeriod", false, "Provides a player top10 Players for a specific timeframe"));
                }
            }
        }
        #endregion
        #endregion

        #region Classes
        /*==========Classes========*/
        class CLogger
        {
            private readonly string _Name;
            private string _Message = String.Empty;
            private string _Subset = String.Empty;
            private DateTime _Time;

            public string Name
            {
                get { return _Name; }
            }

            public string Message
            {
                get { return _Message; }
            }

            public string Subset
            {
                get { return _Subset; }
            }

            public DateTime Time
            {
                get { return _Time; }
            }

            public CLogger(DateTime time, string name, string message, string subset)
            {
                _Name = name;
                _Message = message;
                _Subset = subset;
                _Time = time;
            }
        }

        class CStats
        {
            private string _ClanTag;
            private string _Guid;
            private string _EAGuid;
            private string _IP;
            private string _PlayerCountryCode;
            private int _Score = 0;
            private int _HighScore = 0;
            private int _LastScore = 0;
            private int _Kills = 0;
            private int _Headshots = 0;
            private int _Deaths = 0;
            private int _Suicides = 0;
            private int _Teamkills = 0;
            private int _Playtime = 0;
            private int _Rounds = 0;
            private DateTime _Playerjoined;
            private DateTime _TimePlayerleft;
            private DateTime _TimePlayerjoined;
            private int _PlayerleftServerScore = 0;
            private bool _playerOnServer = true;
            private int _rank = 0;
            //KD Correction
            private int _beforeleftKills = 0;
            private int _beforeleftDeaths = 0;
            //Streaks
            private int _Killstreak;
            private int _Deathstreak;
            private int _Killcount;
            private int _Deathcount;
            //Wins&Loses
            private int _Wins = 0;
            private int _Losses = 0;
            //TeamID
            private int _TeamId = 0;
            //BFBCS
            private CBFBCS _BFBCS_Stats;
            private myDateTime MyDateTime = new myDateTime(0);
            public Dictionary<string, Dictionary<string, CStats.CUsedWeapon>> dicWeap = new Dictionary<string, Dictionary<string, CStats.CUsedWeapon>>();

            //Awards
            private CAwards _Awards;

            //global Rank
            private int _GlobalRank = 0;

            public string ClanTag
            {
                get { return _ClanTag; }
                set { _ClanTag = value; }
            }

            public string Guid
            {
                get { return _Guid; }
                set { _Guid = value; }
            }

            public string EAGuid
            {
                get { return _EAGuid; }
                set { _EAGuid = value; }
            }

            public string IP
            {
                get { return _IP; }
                set { _IP = value.Remove(value.IndexOf(":")); }
            }

            public string PlayerCountryCode
            {
                get { return _PlayerCountryCode; }
                set { _PlayerCountryCode = value; }
            }

            public int Score
            {
                get { return _Score; }
                set { _Score = value; }
            }

            public int HighScore
            {
                get { return _HighScore; }
                set { _HighScore = value; }
            }

            public int LastScore
            {
                get { return _LastScore; }
                set { _LastScore = value; }
            }

            public int Kills
            {
                get { return _Kills; }
                set { _Kills = value; }
            }

            public int BeforeLeftKills
            {
                get { return _beforeleftKills; }
                set { _beforeleftKills = value; }
            }

            public int Headshots
            {
                get { return _Headshots; }
                set { _Headshots = value; }
            }

            public int Deaths
            {
                get { return _Deaths; }
                set { _Deaths = value; }
            }

            public int BeforeLeftDeaths
            {
                get { return _beforeleftDeaths; }
                set { _beforeleftDeaths = value; }
            }

            public int Suicides
            {
                get { return _Suicides; }
                set { _Suicides = value; }
            }

            public int Teamkills
            {
                get { return _Teamkills; }
                set { _Teamkills = value; }
            }

            public int Playtime
            {
                get { return _Playtime; }
                set { _Playtime = value; }
            }

            public int Rounds
            {
                get { return _Rounds; }
                set { _Rounds = value; }
            }

            public DateTime Playerjoined
            {
                get { return _Playerjoined; }
                set { _Playerjoined = value; }
            }

            public DateTime TimePlayerleft
            {
                get { return _TimePlayerleft; }
                set { _TimePlayerleft = value; }
            }

            public DateTime TimePlayerjoined
            {
                get { return _TimePlayerjoined; }
                set { _TimePlayerjoined = value; }
            }

            public int PlayerleftServerScore
            {
                get { return _PlayerleftServerScore; }
                set { _PlayerleftServerScore = value; }
            }

            public bool PlayerOnServer
            {
                get { return _playerOnServer; }
                set { _playerOnServer = value; }
            }

            public int Rank
            {
                get { return _rank; }
                set { _rank = value; }
            }

            public int Killstreak
            {
                get { return _Killstreak; }
                set { _Killstreak = value; }
            }

            public int Deathstreak
            {
                get { return _Deathstreak; }
                set { _Deathstreak = value; }
            }

            public int Wins
            {
                get { return _Wins; }
                set { _Wins = value; }
            }

            public int Losses
            {
                get { return _Losses; }
                set { _Losses = value; }
            }

            public int TeamId
            {
                get { return _TeamId; }
                set { _TeamId = value; }
            }

            public int GlobalRank
            {
                get { return _GlobalRank; }
                set { _GlobalRank = value; }
            }

            //Methodes	
            public void AddScore(int intScore)
            {
                if (intScore != 0)
                {
                    this._Score = this._Score + (intScore - this._LastScore);
                    this._LastScore = intScore;
                    if (intScore > this._HighScore)
                    {
                        this._HighScore = intScore;
                    }
                }
                else
                {
                    this._LastScore = 0;
                }
            }

            public double KDR()
            {
                double ratio = 0;
                if (this._Deaths != 0)
                {
                    ratio = Math.Round(Convert.ToDouble(this._Kills) / Convert.ToDouble(this._Deaths), 2);
                }
                else
                {
                    ratio = this._Kills;
                }
                return ratio;
            }

            public Dictionary<string, Dictionary<string, CStats.CUsedWeapon>> getWeaponKills()
            {
                return this.dicWeap;
            }

            public void addKill(string strDmgType, string strweaponType, bool blheadshot)
            {
                this._Kills++;
                if (blheadshot)
                {
                    if (this.dicWeap.ContainsKey(strDmgType))
                    {
                        if (this.dicWeap[strDmgType].ContainsKey(strweaponType))
                        {
                            this.dicWeap[strDmgType][strweaponType].Kills++;
                            this.dicWeap[strDmgType][strweaponType].Headshots++;
                        }
                    }
                    this._Headshots++;
                }
                else
                {
                    if (this.dicWeap.ContainsKey(strDmgType))
                    {
                        if (this.dicWeap[strDmgType].ContainsKey(strweaponType))
                        {
                            this.dicWeap[strDmgType][strweaponType].Kills++;
                        }
                    }
                }
                //Killstreaks
                this._Killcount++;
                this._Deathcount = 0;
                if (this._Killcount > this._Killstreak)
                {
                    this._Killstreak = this._Killcount;
                }
                //Awardchecks
                this._Awards.CheckOnKill(_Kills, _Headshots, _Deaths, _Killcount, _Deathcount);
            }

            public void addDeath(string strDmgType, string strweaponType)
            {
                this._Deaths++;
                if (this.dicWeap.ContainsKey(strDmgType))
                {
                    if (this.dicWeap[strDmgType].ContainsKey(strweaponType))
                    {
                        this.dicWeap[strDmgType][strweaponType].Deaths++;
                    }
                }
                //Deathstreak
                this._Deathcount++;
                this._Killcount = 0;
                if (this._Deathcount > this._Deathstreak)
                {
                    this._Deathstreak = this._Deathcount;
                }
                //Awardchecks
                this._Awards.CheckOnDeath(_Kills, _Headshots, _Deaths, _Killcount, _Deathcount);
            }

            public void playerleft()
            {
                //Score
                this._PlayerleftServerScore += this._Score;
                this._Score = 0;
                //Kd Correction
                this._beforeleftKills += this._Kills;
                this._beforeleftDeaths += this._Deaths;

                //Time
                TimeSpan duration = MyDateTime.Now - this._Playerjoined;
                this._Playtime += Convert.ToInt32(duration.TotalSeconds);
                this._playerOnServer = false;
            }

            public int TotalScore
            {
                get { return (this._PlayerleftServerScore + this._Score); }
            }

            public int TotalPlaytime
            {
                get
                {
                    if (this._playerOnServer)
                    {
                        TimeSpan duration = MyDateTime.Now - this._Playerjoined;
                        return (this._Playtime + Convert.ToInt32(duration.TotalSeconds));
                    }
                    return this._Playtime;
                }
            }

            public CStats.CBFBCS BFBCS_Stats
            {
                get { return _BFBCS_Stats; }
                set { _BFBCS_Stats = value; }
            }

            public CStats.CAwards Awards
            {
                get { return _Awards; }
                set { _Awards = value; }
            }

            public class CUsedWeapon
            {
                private string _Name = "";
                private string _FieldName = "";
                private string _Slot = "";
                private string _KitRestriction = "";
                private int _Kills = 0;
                private int _Headshots = 0;
                private int _Deaths = 0;

                public int Kills
                {
                    get { return _Kills; }
                    set { _Kills = value; }
                }

                public int Headshots
                {
                    get { return _Headshots; }
                    set { _Headshots = value; }
                }

                public int Deaths
                {
                    get { return _Deaths; }
                    set { _Deaths = value; }
                }

                public string Name
                {
                    get { return _Name; }
                    set { _Name = value; }
                }

                public string FieldName
                {
                    get { return _FieldName; }
                    set { _FieldName = value; }
                }

                public string Slot
                {
                    get { return _Slot; }
                    set { _Slot = value; }
                }

                public string KitRestriction
                {
                    get { return _KitRestriction; }
                    set { _KitRestriction = value; }
                }

                public CUsedWeapon(string name, string fieldname, string slot, string kitrestriction)
                {
                    this._Name = name;
                    this._FieldName = fieldname;
                    this._Slot = slot;
                    this._KitRestriction = kitrestriction;
                    this._Kills = 0;
                    this._Headshots = 0;
                    this._Deaths = 0;
                }
            }

            public class CBFBCS
            {
                private int _rank;
                private int _kills;
                private int _deaths;
                private int _score;
                private double _skilllevel;
                private double _time;
                private double _elo;
                private bool _Updated;
                private bool _fetching;
                private bool _noUpdate;

                public int Rank
                {
                    get { return _rank; }
                    set { _rank = value; }
                }

                public int Kills
                {
                    get { return _kills; }
                    set { _kills = value; }
                }

                public int Deaths
                {
                    get { return _deaths; }
                    set { _deaths = value; }
                }

                public double KDR
                {
                    get
                    {
                        double ratio = 0;
                        if (this._deaths != 0)
                        {
                            ratio = Math.Round(Convert.ToDouble(this._kills) / Convert.ToDouble(this._deaths), 2);
                        }
                        else
                        {
                            ratio = this._kills;
                        }
                        return ratio;
                    }
                }
                public double SPM
                {
                    get
                    {
                        return Convert.ToDouble(this._score) / (this._time / 60);
                    }
                }

                public int Score
                {
                    get { return _score; }
                    set { _score = value; }
                }

                public double Skilllevel
                {
                    get { return _skilllevel; }
                    set { _skilllevel = value; }
                }

                public double Time
                {
                    get { return _time; }
                    set { _time = value; }
                }

                public double Elo
                {
                    get { return _elo; }
                    set { _elo = value; }
                }

                public bool Updated
                {
                    get { return _Updated; }
                    set { _Updated = value; }
                }

                public bool Fetching
                {
                    get { return _fetching; }
                    set { _fetching = value; }
                }

                public bool NoUpdate
                {
                    get { return _noUpdate; }
                    set { _noUpdate = value; }
                }

                public CBFBCS()
                {
                    this._rank = 0;
                    this._kills = 0;
                    this._deaths = 0;
                    this._score = 0;
                    this._skilllevel = 0;
                    this._time = 0;
                    this._elo = 0;
                    this._Updated = false;
                    this._fetching = false;
                    this._noUpdate = false;
                }
            }

            public class CAwards
            {
                //Awards
                private Dictionary<string, int> _dicAwards = new Dictionary<string, int>();

                //Constructor
                public CAwards()
                {
                    this._dicAwards = new Dictionary<string, int>();
                }

                //Get and Set
                public Dictionary<string, int> DicAwards
                {
                    get { return _dicAwards; }
                    set { _dicAwards = value; }
                }

                //Methodes
                public void dicAdd(string strAward, int count)
                {
                    if (this._dicAwards.ContainsKey(strAward))
                    {
                        this._dicAwards[strAward] = this._dicAwards[strAward] + count;
                    }
                    else
                    {
                        this._dicAwards.Add(strAward, count);
                    }
                }

                public void CheckOnKill(int kills, int hs, int deaths, int ks, int ds)
                {
                    //Purple Heart
                    if (kills >= 5 && deaths >= 20 && ((Double)kills / (Double)deaths) == 0.25)
                    {
                        this.dicAdd("Purple_Heart", 1);
                    }
                    //Killstreaks
                    if (ks == 5)
                    {
                        //5 Kills in a row
                        this.dicAdd("Killstreak_5", 1);
                    }
                    else if (ks == 10)
                    {
                        //10 kills in a row
                        this.dicAdd("Killstreak_10", 1);
                    }
                    else if (ks == 15)
                    {
                        //15 kills in a row
                        this.dicAdd("Killstreak_15", 1);
                    }
                    else if (ks == 20)
                    {
                        //20 kills in a row
                        this.dicAdd("Killstreak_20", 1);
                    }
                }

                public void CheckOnDeath(int kills, int hs, int deaths, int ks, int ds)
                {
                    //Purple Heart
                    if (kills >= 5 && deaths >= 20 && ((Double)kills / (Double)deaths) == 0.25)
                    {
                        this.dicAdd("Purple_Heart", 1);
                    }
                }
            }

            public class myDateTime
            {
                private double _offset = 0;

                public DateTime Now
                {
                    get
                    {
                        DateTime dateValue = DateTime.Now;
                        return dateValue.AddHours(_offset);
                    }
                }
                public myDateTime(double offset)
                {
                    this._offset = offset;
                }
            }

            public CStats(string guid, int score, int kills, int headshots, int deaths, int suicides, int teamkills, int playtime, double timeoffset, Dictionary<string, Dictionary<string, CStats.CUsedWeapon>> _weaponDic)
            {
                this.MyDateTime = new myDateTime(timeoffset);
                this._ClanTag = String.Empty;
                this._Guid = guid;
                this._EAGuid = String.Empty;
                this._IP = String.Empty;
                this._Score = score;
                this._LastScore = 0;
                this._HighScore = score;
                this._Kills = kills;
                this._Headshots = headshots;
                this._Deaths = deaths;
                this._Suicides = suicides;
                this._Teamkills = teamkills;
                this._Playtime = playtime;
                this._Rounds = 0;
                this._PlayerleftServerScore = 0;
                this._PlayerCountryCode = String.Empty;
                this._Playerjoined = MyDateTime.Now;
                this._TimePlayerjoined = this._Playerjoined;
                this._TimePlayerleft = DateTime.MinValue;
                this._rank = 0;
                this._Killcount = 0;
                this._Killstreak = 0;
                this._Deathcount = 0;
                this._Deathstreak = 0;
                this._Wins = 0;
                this._Losses = 0;
                this.BFBCS_Stats = new CStats.CBFBCS();
                this._Awards = new CAwards();
                //this.dicWeap = new Dictionary<string,Dictionary<string,CUsedWeapon>>(_weaponDic);
                foreach (KeyValuePair<string, Dictionary<string, CStats.CUsedWeapon>> pair in _weaponDic)
                {
                    this.dicWeap.Add(pair.Key, new Dictionary<string, CStats.CUsedWeapon>());
                    foreach (KeyValuePair<string, CStats.CUsedWeapon> subpair in pair.Value)
                    {
                        this.dicWeap[pair.Key].Add(subpair.Key, new CStats.CUsedWeapon(subpair.Value.Name, subpair.Value.FieldName, subpair.Value.Slot, subpair.Value.KitRestriction));
                    }
                }
            }
        }

        class C_ID_Cache
        {
            private int _Id;
            private int _StatsID;
            private bool _PlayeronServer;

            public int Id
            {
                get { return _Id; }
                set { _Id = value; }
            }

            public int StatsID
            {
                get { return _StatsID; }
                set { _StatsID = value; }
            }

            public bool PlayeronServer
            {
                get { return _PlayeronServer; }
                set { _PlayeronServer = value; }
            }
            //Constructor
            public C_ID_Cache(int statsid, int id, bool playeronServer)
            {
                this._Id = id;
                this._StatsID = statsid;
                this._PlayeronServer = playeronServer;
            }
        }

        class CKillerVictim
        {
            string _Killer = String.Empty;
            string _Victim = String.Empty;

            public string Killer
            {
                get { return _Killer; }
                set { _Killer = value; }
            }

            public string Victim
            {
                get { return _Victim; }
                set { _Victim = value; }
            }

            public CKillerVictim(string killer, string victim)
            {
                this._Killer = killer;
                this._Victim = victim;
            }
        }

        class CMapstats
        {
            private DateTime _timeMaploaded;
            private DateTime _timeMapStarted;
            private DateTime _timeRoundEnd;
            private string _strMapname = String.Empty;
            private string _strGamemode = String.Empty;
            private int _intRound;
            private int _intNumberOfRounds;
            private List<int> _lstPlayers;
            private int _intMinPlayers;
            private int _intMaxPlayers;
            private int _intServerplayermax;
            private double _doubleAvgPlayers;
            private int _intplayerleftServer;
            private int _intplayerjoinedServer;
            private myDateTime MyDateTime = new myDateTime(0);

            public DateTime TimeMaploaded
            {
                get { return _timeMaploaded; }
                set { _timeMaploaded = value; }
            }

            public DateTime TimeMapStarted
            {
                get { return _timeMapStarted; }
                set { _timeMapStarted = value; }
            }

            public DateTime TimeRoundEnd
            {
                get { return _timeRoundEnd; }
                set { _timeRoundEnd = value; }
            }

            public string StrMapname
            {
                get { return _strMapname; }
                set { _strMapname = value; }
            }

            public string StrGamemode
            {
                get { return _strGamemode; }
                set { _strGamemode = value; }
            }

            public int IntRound
            {
                get { return _intRound; }
                set { _intRound = value; }
            }

            public int IntNumberOfRounds
            {
                get { return _intNumberOfRounds; }
                set { _intNumberOfRounds = value; }
            }

            public List<int> LstPlayers
            {
                get { return _lstPlayers; }
                set { _lstPlayers = value; }
            }

            public int IntMinPlayers
            {
                get { return _intMinPlayers; }
                set { _intMinPlayers = value; }
            }

            public int IntMaxPlayers
            {
                get { return _intMaxPlayers; }
                set { _intMaxPlayers = value; }
            }

            public int IntServerplayermax
            {
                get { return _intServerplayermax; }
                set { _intServerplayermax = value; }
            }

            public double DoubleAvgPlayers
            {
                get { return _doubleAvgPlayers; }
                set { _doubleAvgPlayers = value; }
            }

            public int IntplayerleftServer
            {
                get { return _intplayerleftServer; }
                set { _intplayerleftServer = value; }
            }

            public int IntplayerjoinedServer
            {
                get { return _intplayerjoinedServer; }
                set { _intplayerjoinedServer = value; }
            }

            public void MapStarted()
            {
                this._timeMapStarted = MyDateTime.Now;
            }

            public void MapEnd()
            {
                this._timeRoundEnd = MyDateTime.Now;
            }

            public void ListADD(int entry)
            {
                this._lstPlayers.Add(entry);
            }

            public void calcMaxMinAvgPlayers()
            {
                this._intMaxPlayers = 0;
                this._intMinPlayers = _intServerplayermax;
                this._doubleAvgPlayers = 0;
                int entries = 0;
                foreach (int playercount in this._lstPlayers)
                {
                    if (playercount >= this._intMaxPlayers)
                        this._intMaxPlayers = playercount;

                    if (playercount <= this._intMinPlayers)
                        this._intMinPlayers = playercount;

                    this._doubleAvgPlayers = this._doubleAvgPlayers + playercount;
                    entries = entries + 1;

                }
                if (entries != 0)
                {
                    this._doubleAvgPlayers = this._doubleAvgPlayers / (Convert.ToDouble(entries));
                    this._doubleAvgPlayers = Math.Round(this._doubleAvgPlayers, 1);
                }
                else
                {
                    this._doubleAvgPlayers = 0;
                    this._intMaxPlayers = 0;
                    this._intMinPlayers = 0;
                }
            }

            public class myDateTime
            {
                private double _offset = 0;

                public DateTime Now
                {
                    get
                    {
                        DateTime dateValue = DateTime.Now;
                        return dateValue.AddHours(_offset);
                    }
                }
                public myDateTime(double offset)
                {
                    this._offset = offset;
                }
            }

            public CMapstats(DateTime timeMaploaded, string strMapname, int intRound, int intNumberOfRounds, double timeoffset)
            {
                this._timeMaploaded = timeMaploaded;
                this._strMapname = strMapname;
                this._intRound = intRound;
                this._intNumberOfRounds = intNumberOfRounds;
                this._intMaxPlayers = 32;
                this._intServerplayermax = 32;
                this._intMinPlayers = 0;
                this._intplayerjoinedServer = 0;
                this._intplayerleftServer = 0;
                this._lstPlayers = new List<int>();
                this._timeMapStarted = DateTime.MinValue;
                this._timeRoundEnd = DateTime.MinValue;
                this._strGamemode = String.Empty;
                this.MyDateTime = new myDateTime(timeoffset);
            }
        }

        class CSpamprotection
        {
            private Dictionary<string, int> dicplayer;
            private int _allowedRequests;

            public CSpamprotection(int allowedRequests)
            {
                this._allowedRequests = allowedRequests;
                this.dicplayer = new Dictionary<string, int>();
            }

            public bool isAllowed(string strSpeaker)
            {
                bool result = false;
                if (this.dicplayer.ContainsKey(strSpeaker) == true)
                {
                    int i = this.dicplayer[strSpeaker];
                    if (0 >= i)
                    {
                        //Player is blocked
                        result = false;
                        this.dicplayer[strSpeaker]--;
                    }
                    else
                    {
                        //Player is not blocked
                        result = true;
                        this.dicplayer[strSpeaker]--;
                    }
                }
                else
                {
                    this.dicplayer.Add(strSpeaker, this._allowedRequests);
                    result = true;
                    this.dicplayer[strSpeaker]--;
                }
                return result;
            }

            public void Reset()
            {
                this.dicplayer.Clear();
            }
        }

        class myDateTime_W
        {
            private double _offset = 0;

            public DateTime Now
            {
                get
                {
                    DateTime dateValue = DateTime.Now;
                    return dateValue.AddHours(_offset);
                }
            }
            public myDateTime_W(double offset)
            {
                this._offset = offset;
            }
        }

        class CStatsIngameCommands
        {
            //Class variables
            private string _functioncall;
            private string _commands;
            private string _description;
            private bool _boolEnabled;


            public CStatsIngameCommands(string commands, string functioncall, bool boolEnabled, string description)
            {
                this._commands = commands;
                this._functioncall = functioncall;
                this._boolEnabled = boolEnabled;
                this._description = description;

            }

            public string commands
            {
                get { return this._commands; }
                set { this._commands = value; }
            }

            public string functioncall
            {
                get { return this._functioncall; }
                set { this._functioncall = value; }
            }

            public string description
            {
                get { return this._description; }
                set { this._description = value; }
            }

            public bool boolEnabled
            {
                get { return this._boolEnabled; }
                set { this._boolEnabled = value; }
            }
        }

        #endregion
    }
}
