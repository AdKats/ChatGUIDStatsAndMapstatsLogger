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
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
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
        private String m_strHostName;
        private String m_strPort;
        private String m_strPRoConVersion;

        //Tablebuilder
        private readonly Object tablebuilderlock;

        //other locks
        private readonly Object chatloglock;
        private readonly Object sqlquerylock;
        private readonly Object sessionlock;
        private readonly Object streamlock;
        private readonly Object ConnectionStringBuilderlock;
        private readonly Object registerallcomandslock;

        //Dateoffset
        private myDateTime_W MyDateTime;
        private Double m_dTimeOffset;

        //Logging
        private Dictionary<String, CPunkbusterInfo> m_dicPbInfo = new Dictionary<String, CPunkbusterInfo>();
        //Chatlog
        private static List<CLogger> ChatLog = new List<CLogger>();
        private List<String> lstStrChatFilterRules;
        private List<Regex> lstChatFilterRules;
        //Statslog
        private Dictionary<String, CStats> StatsTracker = new Dictionary<String, CStats>();
        //Dogtags
        private Dictionary<CKillerVictim, Int32> m_dicKnifeKills = new Dictionary<CKillerVictim, Int32>();
        //Session
        private Dictionary<String, CStats> m_dicSession = new Dictionary<String, CStats>();
        private CMapstats Mapstats;
        private CMapstats Nextmapinfo;
        private List<CStats> lstpassedSessions = new List<CStats>();

        //GameMod
        //private string m_strGameMod;

        //Spamprotection
        private Int32 numberOfAllowedRequests;
        private CSpamprotection Spamprotection;

        //Keywords
        private List<String> m_lstTableconfig = new List<String>();
        private Dictionary<String, List<String>> m_dicKeywords = new Dictionary<String, List<String>>();

        //Weapondic
        private Dictionary<String, Dictionary<String, CStats.CUsedWeapon>> weaponDic = new Dictionary<String, Dictionary<String, CStats.CUsedWeapon>>();

        //DamageClassDic
        private Dictionary<String, String> DamageClass = new Dictionary<String, String>();

        //WelcomeStatsDic
        private Dictionary<String, DateTime> welcomestatsDic = new Dictionary<String, DateTime>();

        //Weapon Mapping Dictionary
        private Dictionary<String, Int32> WeaponMappingDic = new Dictionary<String, Int32>();

        //ServerID
        private Int32 ServerID;

        //Awards
        private List<String> m_lstAwardTable = new List<String>();

        //Tablenames
        private String tbl_playerdata;
        private String tbl_playerstats;
        private String tbl_weaponstats;
        private String tbl_dogtags;
        private String tbl_mapstats;
        private String tbl_chatlog;
        private String tbl_bfbcs;
        private String tbl_awards;
        private String tbl_server;
        private String tbl_server_player;
        private String tbl_server_stats;
        private String tbl_playerrank;
        private String tbl_sessions;
        private String tbl_currentplayers;
        private String tbl_weapons;
        private String tbl_weapons_stats;
        private String tbl_games;
        private String tbl_teamscores;

        // Timelogging
        private Boolean bool_roundStarted;
        private DateTime Time_RankingStarted;

        //Other
        private Dictionary<String, CPlayerInfo> m_dicPlayers = new Dictionary<String, CPlayerInfo>();   //Players

        //ID Cache
        private Dictionary<String, C_ID_Cache> m_ID_cache = new Dictionary<String, C_ID_Cache>();

        //Various Variables
        //private int m_strUpdateInterval;
        private Boolean isStreaming;
        private String serverName;
        private Boolean m_isPluginEnabled;
        private Boolean boolTableEXISTS;
        private Boolean boolKeywordDicReady;
        private String tableSuffix;
        private Boolean MySql_Connection_is_activ;
        //Last time Stat Logger actively interacted with the database
        private DateTime lastDBInteraction = DateTime.MinValue;

        //Update skipswitches
        private Boolean boolSkipGlobalUpdate;
        private Boolean boolSkipServerUpdate;
        private Boolean boolSkipServerStatsUpdate;

        //Transaction retry
        private Int32 TransactionRetryCount;

        //Playerstartcount
        private Int32 intRoundStartCount;
        private Int32 intRoundRestartCount;

        //Webrequest
        private Int32 m_requestIntervall;
        private String m_webAddress;

        //BFBCS
        //private double BFBCS_UpdateInterval;
        //private int BFBCS_Min_Request;

        //Database Connection Variables
        private String m_strHost;
        private String m_strDBPort;
        private String m_strDatabase;
        private String m_strUserName;
        private String m_strPassword;
        //private string m_strDatabaseDriver;

        //Stats Message Variables        
        private List<String> m_lstPlayerStatsMessage;
        private List<String> m_lstPlayerOfTheDayMessage;
        private List<String> m_lstPlayerWelcomeStatsMessage;
        private List<String> m_lstNewPlayerWelcomeMsg;
        private List<String> m_lstWeaponstatsMsg;
        private List<String> m_lstServerstatsMsg;
        //private string m_strPlayerWelcomeMsg;
        //private string m_strNewPlayerWelcomeMsg;
        private Int32 int_welcomeStatsDelay;
        private String m_strTop10Header;
        private String m_strTop10RowFormat;
        private String m_strWeaponTop10Header;
        private String m_strWeaponTop10RowFormat;

        //top10 for Period
        private String m_strTop10HeaderForPeriod;

        //Session
        private List<String> m_lstSessionMessage;

        //Debug
        private String GlobalDebugMode;

        //ServerGroup
        private Int32 intServerGroup;

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

        private Int32 m_maxPoolSize; //Connection Pooling
        private Int32 m_minPoolSize; //Connection Pooling

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
        private Int32 minIntervalllenght;

        //Double Roundendfix
        private DateTime dtLastRoundendEvent;
        private DateTime dtLastOnListPlayersEvent;

        //Top10 for Period
        private Int32 m_intDaysForPeriodTop10;

        //New In-Game Command System
        private String m_IngameCommands_stats;
        private String m_IngameCommands_serverstats;
        private String m_IngameCommands_session;
        private String m_IngameCommands_dogtags;
        private String m_IngameCommands_top10;
        private String m_IngameCommands_playerOfTheDay;
        private String m_IngameCommands_top10ForPeriod;

        private Dictionary<String, CStatsIngameCommands> dicIngameCommands = new Dictionary<String, CStatsIngameCommands>();

        //ServerGametype
        private String strServerGameType = String.Empty;
        private Int32 intServerGameType_ID;

        public CChatGUIDStatsLogger()
        {
            loggerStatusCommand = new MatchCommand("CChatGUIDStatsLogger", "GetStatus", new List<String>(), "CChatGUIDStatsLogger_Status", new List<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.None), "Useable by other plugins to determine the current status of this plugin.");

            //tablebuilderlock
            this.tablebuilderlock = new Object();
            //other locks
            this.chatloglock = new Object();
            this.sqlquerylock = new Object();
            this.sessionlock = new Object();
            this.streamlock = new Object();
            this.ConnectionStringBuilderlock = new Object();
            this.registerallcomandslock = new Object();

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
            this.m_ID_cache = new Dictionary<String, C_ID_Cache>();
            this.m_dicKeywords = new Dictionary<String, List<String>>();
            this.boolKeywordDicReady = false;
            this.tableSuffix = String.Empty;
            this.Mapstats = new CMapstats(MyDateTime.Now, "START", 0, 0, this.m_dTimeOffset);
            this.MySql_Connection_is_activ = false;
            this.numberOfAllowedRequests = 10;

            //Transaction retry count
            TransactionRetryCount = 3;

            //Chatlog
            this.lstStrChatFilterRules = new List<String>();
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
            this.welcomestatsDic = new Dictionary<String, DateTime>();

            //Playerstats
            this.m_lstPlayerStatsMessage = new List<String>();
            this.m_lstPlayerStatsMessage.Add("Serverstats for %playerName%:");
            this.m_lstPlayerStatsMessage.Add("Score: %playerScore%  %playerKills% Kills %playerHeadshots% HS  %playerDeaths% Deaths K/D: %playerKDR%");
            this.m_lstPlayerStatsMessage.Add("Your Serverrank is: %playerRank% of %allRanks%");

            //Player of the day
            this.m_lstPlayerOfTheDayMessage = new List<String>();
            this.m_lstPlayerOfTheDayMessage.Add("%playerName% is the Player of the day");
            this.m_lstPlayerOfTheDayMessage.Add("Score: %playerScore%  %playerKills% Kills %playerHeadshots% HS  %playerDeaths% Deaths K/D: %playerKDR%");
            this.m_lstPlayerOfTheDayMessage.Add("His Serverrank is: %playerRank% of %allRanks%");
            this.m_lstPlayerOfTheDayMessage.Add("Overall playtime for today: %playerPlaytime%");

            //Welcomestats
            this.m_lstPlayerWelcomeStatsMessage = new List<String>();
            this.m_lstPlayerWelcomeStatsMessage.Add("Nice to see you on our Server again, %playerName%");
            this.m_lstPlayerWelcomeStatsMessage.Add("Serverstats for %playerName%:");
            this.m_lstPlayerWelcomeStatsMessage.Add("Score: %playerScore%  %playerKills% Kills %playerHeadshots% HS  %playerDeaths% Deaths K/D: %playerKDR%");
            this.m_lstPlayerWelcomeStatsMessage.Add("Your Serverrank is: %playerRank% of %allRanks%");

            //Welcomestats new Player
            this.m_lstNewPlayerWelcomeMsg = new List<String>();
            this.m_lstNewPlayerWelcomeMsg.Add("Welcome to the %serverName% Server, %playerName%");

            //Weaponstats
            this.m_lstWeaponstatsMsg = new List<String>();
            this.m_lstWeaponstatsMsg.Add("%playerName%'s Stats for %Weapon%:");
            this.m_lstWeaponstatsMsg.Add("%playerKills% Kills  %playerHeadshots% Headshots  Headshotrate: %playerKHR%%");
            this.m_lstWeaponstatsMsg.Add("Your Weaponrank is: %playerRank% of %allRanks%");

            //Serverstats
            this.m_lstServerstatsMsg = new List<String>();
            this.m_lstServerstatsMsg.Add("Serverstatistics for server %serverName%");
            this.m_lstServerstatsMsg.Add("Unique Players: %countPlayer%  Totalplaytime: %sumPlaytime%");
            this.m_lstServerstatsMsg.Add("Totalscore: %sumScore% Avg. Score: %avgScore% Avg. SPM: %avgSPM%");
            this.m_lstServerstatsMsg.Add("Totalkills: %sumKills% Avg. Kills: %avgKills% Avg. KPM: %avgKPM%");

            //Session
            this.m_lstSessionMessage = new List<String>();
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
            this.m_lstAwardTable = new List<String>();
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
            this.m_lstTableconfig = new List<String>();
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
        public String GetPluginName()
        {
            return "PRoCon Chat, GUID, Stats and Map Logger";
        }

        public String GetPluginVersion()
        {
            return "1.0.0.4";
        }

        public String GetPluginAuthor()
        {
            return "[GWC]XpKiller (maintained by Prophet731)";
        }

        public String GetPluginWebsite()
        {
            return "www.german-wildcards.de";
        }

        public String GetPluginDescription()
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

        public void OnPluginLoadingEnv(List<String> lstPluginEnv)
        {
            this.strServerGameType = lstPluginEnv[1].ToUpper();
        }

        public void OnPluginLoaded(String strHostName, String strPort, String strPRoConVersion)
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

        private List<String> GetExcludedCommandStrings(String strAccountName)
        {
            List<String> lstReturnCommandStrings = new List<String>();
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

        private List<String> GetCommandStrings()
        {
            List<String> lstReturnCommandStrings = new List<String>();
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
                foreach (KeyValuePair<String, CStatsIngameCommands> kvp in this.dicIngameCommands)
                {
                    if (kvp.Value.commands != String.Empty)
                    {
                        foreach (String command in kvp.Value.commands.Split(','))
                        {
                            this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", kvp.Value.functioncall.ToString(), this.Listify<String>("@", "!", "#"), command.ToString(), this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), kvp.Value.description.ToString()));
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
                        foreach (KeyValuePair<String, CStatsIngameCommands> kvp in this.dicIngameCommands)
                        {
                            if (kvp.Value.commands != String.Empty)
                            {
                                foreach (String command in kvp.Value.commands.Split(','))
                                {
                                    if (kvp.Value.boolEnabled)
                                    {
                                        this.RegisterCommand(new MatchCommand("CChatGUIDStatsLogger", kvp.Value.functioncall, this.Listify<String>("@", "!", "#"), command, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), kvp.Value.description));
                                    }
                                    else
                                    {
                                        this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", kvp.Value.functioncall, this.Listify<String>("@", "!", "#"), command, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), kvp.Value.description));
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
                Boolean boolenable = false;
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
            private readonly String _Name;
            private String _Message = String.Empty;
            private String _Subset = String.Empty;
            private DateTime _Time;

            public String Name
            {
                get { return _Name; }
            }

            public String Message
            {
                get { return _Message; }
            }

            public String Subset
            {
                get { return _Subset; }
            }

            public DateTime Time
            {
                get { return _Time; }
            }

            public CLogger(DateTime time, String name, String message, String subset)
            {
                _Name = name;
                _Message = message;
                _Subset = subset;
                _Time = time;
            }
        }

        class CStats
        {
            private String _ClanTag;
            private String _Guid;
            private String _EAGuid;
            private String _IP;
            private String _PlayerCountryCode;
            private Int32 _Score = 0;
            private Int32 _HighScore = 0;
            private Int32 _LastScore = 0;
            private Int32 _Kills = 0;
            private Int32 _Headshots = 0;
            private Int32 _Deaths = 0;
            private Int32 _Suicides = 0;
            private Int32 _Teamkills = 0;
            private Int32 _Playtime = 0;
            private Int32 _Rounds = 0;
            private DateTime _Playerjoined;
            private DateTime _TimePlayerleft;
            private DateTime _TimePlayerjoined;
            private Int32 _PlayerleftServerScore = 0;
            private Boolean _playerOnServer = true;
            private Int32 _rank = 0;
            //KD Correction
            private Int32 _beforeleftKills = 0;
            private Int32 _beforeleftDeaths = 0;
            //Streaks
            private Int32 _Killstreak;
            private Int32 _Deathstreak;
            private Int32 _Killcount;
            private Int32 _Deathcount;
            //Wins&Loses
            private Int32 _Wins = 0;
            private Int32 _Losses = 0;
            //TeamID
            private Int32 _TeamId = 0;
            //BFBCS
            private CBFBCS _BFBCS_Stats;
            private myDateTime MyDateTime = new myDateTime(0);
            public Dictionary<String, Dictionary<String, CStats.CUsedWeapon>> dicWeap = new Dictionary<String, Dictionary<String, CStats.CUsedWeapon>>();

            //Awards
            private CAwards _Awards;

            //global Rank
            private Int32 _GlobalRank = 0;

            public String ClanTag
            {
                get { return _ClanTag; }
                set { _ClanTag = value; }
            }

            public String Guid
            {
                get { return _Guid; }
                set { _Guid = value; }
            }

            public String EAGuid
            {
                get { return _EAGuid; }
                set { _EAGuid = value; }
            }

            public String IP
            {
                get { return _IP; }
                set { _IP = value.Remove(value.IndexOf(":")); }
            }

            public String PlayerCountryCode
            {
                get { return _PlayerCountryCode; }
                set { _PlayerCountryCode = value; }
            }

            public Int32 Score
            {
                get { return _Score; }
                set { _Score = value; }
            }

            public Int32 HighScore
            {
                get { return _HighScore; }
                set { _HighScore = value; }
            }

            public Int32 LastScore
            {
                get { return _LastScore; }
                set { _LastScore = value; }
            }

            public Int32 Kills
            {
                get { return _Kills; }
                set { _Kills = value; }
            }

            public Int32 BeforeLeftKills
            {
                get { return _beforeleftKills; }
                set { _beforeleftKills = value; }
            }

            public Int32 Headshots
            {
                get { return _Headshots; }
                set { _Headshots = value; }
            }

            public Int32 Deaths
            {
                get { return _Deaths; }
                set { _Deaths = value; }
            }

            public Int32 BeforeLeftDeaths
            {
                get { return _beforeleftDeaths; }
                set { _beforeleftDeaths = value; }
            }

            public Int32 Suicides
            {
                get { return _Suicides; }
                set { _Suicides = value; }
            }

            public Int32 Teamkills
            {
                get { return _Teamkills; }
                set { _Teamkills = value; }
            }

            public Int32 Playtime
            {
                get { return _Playtime; }
                set { _Playtime = value; }
            }

            public Int32 Rounds
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

            public Int32 PlayerleftServerScore
            {
                get { return _PlayerleftServerScore; }
                set { _PlayerleftServerScore = value; }
            }

            public Boolean PlayerOnServer
            {
                get { return _playerOnServer; }
                set { _playerOnServer = value; }
            }

            public Int32 Rank
            {
                get { return _rank; }
                set { _rank = value; }
            }

            public Int32 Killstreak
            {
                get { return _Killstreak; }
                set { _Killstreak = value; }
            }

            public Int32 Deathstreak
            {
                get { return _Deathstreak; }
                set { _Deathstreak = value; }
            }

            public Int32 Wins
            {
                get { return _Wins; }
                set { _Wins = value; }
            }

            public Int32 Losses
            {
                get { return _Losses; }
                set { _Losses = value; }
            }

            public Int32 TeamId
            {
                get { return _TeamId; }
                set { _TeamId = value; }
            }

            public Int32 GlobalRank
            {
                get { return _GlobalRank; }
                set { _GlobalRank = value; }
            }

            //Methodes	
            public void AddScore(Int32 intScore)
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

            public Double KDR()
            {
                Double ratio = 0;
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

            public Dictionary<String, Dictionary<String, CStats.CUsedWeapon>> getWeaponKills()
            {
                return this.dicWeap;
            }

            public void addKill(String strDmgType, String strweaponType, Boolean blheadshot)
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

            public void addDeath(String strDmgType, String strweaponType)
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

            public Int32 TotalScore
            {
                get { return (this._PlayerleftServerScore + this._Score); }
            }

            public Int32 TotalPlaytime
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
                private String _Name = "";
                private String _FieldName = "";
                private String _Slot = "";
                private String _KitRestriction = "";
                private Int32 _Kills = 0;
                private Int32 _Headshots = 0;
                private Int32 _Deaths = 0;

                public Int32 Kills
                {
                    get { return _Kills; }
                    set { _Kills = value; }
                }

                public Int32 Headshots
                {
                    get { return _Headshots; }
                    set { _Headshots = value; }
                }

                public Int32 Deaths
                {
                    get { return _Deaths; }
                    set { _Deaths = value; }
                }

                public String Name
                {
                    get { return _Name; }
                    set { _Name = value; }
                }

                public String FieldName
                {
                    get { return _FieldName; }
                    set { _FieldName = value; }
                }

                public String Slot
                {
                    get { return _Slot; }
                    set { _Slot = value; }
                }

                public String KitRestriction
                {
                    get { return _KitRestriction; }
                    set { _KitRestriction = value; }
                }

                public CUsedWeapon(String name, String fieldname, String slot, String kitrestriction)
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
                private Int32 _rank;
                private Int32 _kills;
                private Int32 _deaths;
                private Int32 _score;
                private Double _skilllevel;
                private Double _time;
                private Double _elo;
                private Boolean _Updated;
                private Boolean _fetching;
                private Boolean _noUpdate;

                public Int32 Rank
                {
                    get { return _rank; }
                    set { _rank = value; }
                }

                public Int32 Kills
                {
                    get { return _kills; }
                    set { _kills = value; }
                }

                public Int32 Deaths
                {
                    get { return _deaths; }
                    set { _deaths = value; }
                }

                public Double KDR
                {
                    get
                    {
                        Double ratio = 0;
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
                public Double SPM
                {
                    get
                    {
                        return Convert.ToDouble(this._score) / (this._time / 60);
                    }
                }

                public Int32 Score
                {
                    get { return _score; }
                    set { _score = value; }
                }

                public Double Skilllevel
                {
                    get { return _skilllevel; }
                    set { _skilllevel = value; }
                }

                public Double Time
                {
                    get { return _time; }
                    set { _time = value; }
                }

                public Double Elo
                {
                    get { return _elo; }
                    set { _elo = value; }
                }

                public Boolean Updated
                {
                    get { return _Updated; }
                    set { _Updated = value; }
                }

                public Boolean Fetching
                {
                    get { return _fetching; }
                    set { _fetching = value; }
                }

                public Boolean NoUpdate
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
                private Dictionary<String, Int32> _dicAwards = new Dictionary<String, Int32>();

                //Constructor
                public CAwards()
                {
                    this._dicAwards = new Dictionary<String, Int32>();
                }

                //Get and Set
                public Dictionary<String, Int32> DicAwards
                {
                    get { return _dicAwards; }
                    set { _dicAwards = value; }
                }

                //Methodes
                public void dicAdd(String strAward, Int32 count)
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

                public void CheckOnKill(Int32 kills, Int32 hs, Int32 deaths, Int32 ks, Int32 ds)
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

                public void CheckOnDeath(Int32 kills, Int32 hs, Int32 deaths, Int32 ks, Int32 ds)
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
                private Double _offset = 0;

                public DateTime Now
                {
                    get
                    {
                        DateTime dateValue = DateTime.Now;
                        return dateValue.AddHours(_offset);
                    }
                }
                public myDateTime(Double offset)
                {
                    this._offset = offset;
                }
            }

            public CStats(String guid, Int32 score, Int32 kills, Int32 headshots, Int32 deaths, Int32 suicides, Int32 teamkills, Int32 playtime, Double timeoffset, Dictionary<String, Dictionary<String, CStats.CUsedWeapon>> _weaponDic)
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
                foreach (KeyValuePair<String, Dictionary<String, CStats.CUsedWeapon>> pair in _weaponDic)
                {
                    this.dicWeap.Add(pair.Key, new Dictionary<String, CStats.CUsedWeapon>());
                    foreach (KeyValuePair<String, CStats.CUsedWeapon> subpair in pair.Value)
                    {
                        this.dicWeap[pair.Key].Add(subpair.Key, new CStats.CUsedWeapon(subpair.Value.Name, subpair.Value.FieldName, subpair.Value.Slot, subpair.Value.KitRestriction));
                    }
                }
            }
        }

        class C_ID_Cache
        {
            private Int32 _Id;
            private Int32 _StatsID;
            private Boolean _PlayeronServer;

            public Int32 Id
            {
                get { return _Id; }
                set { _Id = value; }
            }

            public Int32 StatsID
            {
                get { return _StatsID; }
                set { _StatsID = value; }
            }

            public Boolean PlayeronServer
            {
                get { return _PlayeronServer; }
                set { _PlayeronServer = value; }
            }
            //Constructor
            public C_ID_Cache(Int32 statsid, Int32 id, Boolean playeronServer)
            {
                this._Id = id;
                this._StatsID = statsid;
                this._PlayeronServer = playeronServer;
            }
        }

        class CKillerVictim
        {
            String _Killer = String.Empty;
            String _Victim = String.Empty;

            public String Killer
            {
                get { return _Killer; }
                set { _Killer = value; }
            }

            public String Victim
            {
                get { return _Victim; }
                set { _Victim = value; }
            }

            public CKillerVictim(String killer, String victim)
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
            private String _strMapname = String.Empty;
            private String _strGamemode = String.Empty;
            private Int32 _intRound;
            private Int32 _intNumberOfRounds;
            private List<Int32> _lstPlayers;
            private Int32 _intMinPlayers;
            private Int32 _intMaxPlayers;
            private Int32 _intServerplayermax;
            private Double _doubleAvgPlayers;
            private Int32 _intplayerleftServer;
            private Int32 _intplayerjoinedServer;
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

            public String StrMapname
            {
                get { return _strMapname; }
                set { _strMapname = value; }
            }

            public String StrGamemode
            {
                get { return _strGamemode; }
                set { _strGamemode = value; }
            }

            public Int32 IntRound
            {
                get { return _intRound; }
                set { _intRound = value; }
            }

            public Int32 IntNumberOfRounds
            {
                get { return _intNumberOfRounds; }
                set { _intNumberOfRounds = value; }
            }

            public List<Int32> LstPlayers
            {
                get { return _lstPlayers; }
                set { _lstPlayers = value; }
            }

            public Int32 IntMinPlayers
            {
                get { return _intMinPlayers; }
                set { _intMinPlayers = value; }
            }

            public Int32 IntMaxPlayers
            {
                get { return _intMaxPlayers; }
                set { _intMaxPlayers = value; }
            }

            public Int32 IntServerplayermax
            {
                get { return _intServerplayermax; }
                set { _intServerplayermax = value; }
            }

            public Double DoubleAvgPlayers
            {
                get { return _doubleAvgPlayers; }
                set { _doubleAvgPlayers = value; }
            }

            public Int32 IntplayerleftServer
            {
                get { return _intplayerleftServer; }
                set { _intplayerleftServer = value; }
            }

            public Int32 IntplayerjoinedServer
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

            public void ListADD(Int32 entry)
            {
                this._lstPlayers.Add(entry);
            }

            public void calcMaxMinAvgPlayers()
            {
                this._intMaxPlayers = 0;
                this._intMinPlayers = _intServerplayermax;
                this._doubleAvgPlayers = 0;
                Int32 entries = 0;
                foreach (Int32 playercount in this._lstPlayers)
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
                private Double _offset = 0;

                public DateTime Now
                {
                    get
                    {
                        DateTime dateValue = DateTime.Now;
                        return dateValue.AddHours(_offset);
                    }
                }
                public myDateTime(Double offset)
                {
                    this._offset = offset;
                }
            }

            public CMapstats(DateTime timeMaploaded, String strMapname, Int32 intRound, Int32 intNumberOfRounds, Double timeoffset)
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
                this._lstPlayers = new List<Int32>();
                this._timeMapStarted = DateTime.MinValue;
                this._timeRoundEnd = DateTime.MinValue;
                this._strGamemode = String.Empty;
                this.MyDateTime = new myDateTime(timeoffset);
            }
        }

        class CSpamprotection
        {
            private Dictionary<String, Int32> dicplayer;
            private Int32 _allowedRequests;

            public CSpamprotection(Int32 allowedRequests)
            {
                this._allowedRequests = allowedRequests;
                this.dicplayer = new Dictionary<String, Int32>();
            }

            public Boolean isAllowed(String strSpeaker)
            {
                Boolean result = false;
                if (this.dicplayer.ContainsKey(strSpeaker) == true)
                {
                    Int32 i = this.dicplayer[strSpeaker];
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
            private Double _offset = 0;

            public DateTime Now
            {
                get
                {
                    DateTime dateValue = DateTime.Now;
                    return dateValue.AddHours(_offset);
                }
            }
            public myDateTime_W(Double offset)
            {
                this._offset = offset;
            }
        }

        class CStatsIngameCommands
        {
            //Class variables
            private String _functioncall;
            private String _commands;
            private String _description;
            private Boolean _boolEnabled;

            public CStatsIngameCommands(String commands, String functioncall, Boolean boolEnabled, String description)
            {
                this._commands = commands;
                this._functioncall = functioncall;
                this._boolEnabled = boolEnabled;
                this._description = description;

            }

            public String commands
            {
                get { return this._commands; }
                set { this._commands = value; }
            }

            public String functioncall
            {
                get { return this._functioncall; }
                set { this._functioncall = value; }
            }

            public String description
            {
                get { return this._description; }
                set { this._description = value; }
            }

            public Boolean boolEnabled
            {
                get { return this._boolEnabled; }
                set { this._boolEnabled = value; }
            }
        }

        #endregion
    }
}
