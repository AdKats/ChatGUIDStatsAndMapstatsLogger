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
using System.Linq;
using System.Text;
using System.Threading;

using Dapper;

using MySqlConnector;

using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;

namespace PRoConEvents
{
    public partial class CChatGUIDStatsLogger
    {
        private String DBConnectionStringBuilder()
        {
            String conString = String.Empty;
            lock (this.ConnectionStringBuilderlock)
            {
                UInt32 uintport = 3306;
                UInt32.TryParse(m_strDBPort, out uintport);
                myCSB.Port = uintport;
                myCSB.Server = m_strHost;
                myCSB.UserID = m_strUserName;
                myCSB.Password = m_strPassword;
                myCSB.Database = m_strDatabase;
                //Connection Pool
                if (this.m_connectionPooling == enumBoolOnOff.On)
                {
                    myCSB.Pooling = true;
                    myCSB.MinimumPoolSize = Convert.ToUInt32(this.m_minPoolSize);
                    myCSB.MaximumPoolSize = Convert.ToUInt32(this.m_maxPoolSize);
                    myCSB.ConnectionLifeTime = 600;
                }
                else
                {
                    myCSB.Pooling = false;
                }
                //Compression
                if (this.m_Connectioncompression == enumBoolOnOff.On)
                {
                    myCSB.UseCompression = true;
                }
                else
                {
                    myCSB.UseCompression = false;
                }
                myCSB.AllowUserVariables = true;
                myCSB.DefaultCommandTimeout = 3600;
                myCSB.ConnectionTimeout = 50;
                conString = myCSB.ConnectionString;
            }
            return conString;
        }

        private DataTable SQLquery(String sql, Object param = null)
        {
            DataTable MyDataTable = new DataTable();
            try
            {
                this.tablebuilder();
                if (String.IsNullOrEmpty(sql))
                {
                    this.DebugInfo("Warning", "SQLquery: sql is null or empty");
                    return MyDataTable;
                }

                if (this.m_highPerformanceConnectionMode == enumBoolOnOff.On)
                {
                    try
                    {
                        using (MySqlConnection Connection = new MySqlConnection(this.DBConnectionStringBuilder()))
                        {
                            Connection.Open();
                            using (IDataReader reader = Connection.ExecuteReader(sql, param))
                            {
                                MyDataTable.Load(reader);
                            }
                            Connection.Close();
                        }
                    }
                    catch (MySqlException me)
                    {
                        this.DebugInfo("Error", "SQLQuery:");
                        this.DisplayMySqlErrorCollection(me);
                    }
                    catch (Exception c)
                    {
                        this.DebugInfo("Error", "SQLQuery:" + c);
                    }
                }
                else
                {
                    lock (this.sqlquerylock)
                    {
                        if (this.MySqlCon == null)
                        {
                            this.MySqlCon = new MySqlConnection(this.DBConnectionStringBuilder());
                        }
                        try
                        {
                            if (this.MySqlCon.State == ConnectionState.Closed)
                            {
                                this.MySqlCon.Open();
                            }
                            using (IDataReader reader = this.MySqlCon.ExecuteReader(sql, param))
                            {
                                MyDataTable.Load(reader);
                            }
                        }
                        catch (MySqlException oe)
                        {
                            this.DebugInfo("Error", "SQLQuery:");
                            this.DisplayMySqlErrorCollection(oe);
                            if (MySqlCon.State == ConnectionState.Open)
                            {
                                MySqlCon.Close();
                                MySqlCon = null;
                            }
                        }
                        catch (Exception c)
                        {
                            this.DebugInfo("Error", "SQLQuery:" + c);
                            if (MySqlCon.State == ConnectionState.Open)
                            {
                                MySqlCon.Close();
                                MySqlCon = null;
                            }
                        }
                        finally
                        {
                            try
                            {
                                this.MySqlCon.Close();
                            }
                            catch
                            {
                                this.MySqlCon = null;
                            }
                        }
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "SQLQuery OuterException:" + c);
            }
            return MyDataTable;
        }

        private void OpenMySqlConnection(Int32 type)
        {
            try
            {
                switch (type)
                {
                    //OdbcCon
                    case 1:

                        if (MySqlCon == null)
                        {
                            MySqlCon = new MySqlConnection(this.DBConnectionStringBuilder());
                            MySqlCon.Open();
                        }
                        if (MySqlCon.State == ConnectionState.Closed)
                        {
                            MySqlCon = new MySqlConnection(this.DBConnectionStringBuilder());
                            MySqlCon.Open();
                            //this.DebugInfo("Info", "MySqlCon was close Current State is open");
                        }

                        break;
                    //ODBCConn
                    case 2:
                        if (MySqlConn == null)
                        {
                            MySqlConn = new MySqlConnection(this.DBConnectionStringBuilder());
                            MySqlConn.Open();
                        }
                        if (MySqlConn.State == ConnectionState.Closed)
                        {
                            MySqlConn = new MySqlConnection(this.DBConnectionStringBuilder());
                            MySqlConn.Open();
                            //this.DebugInfo("Info", "MySqlConn was close, Reopen it, Current State is open");
                        }

                        break;

                    default:
                        break;
                }
            }
            catch (MySqlException oe)
            {
                this.DebugInfo("Error", "OpenConnection:");
                this.DisplayMySqlErrorCollection(oe);
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "OpenConnection: " + c);
            }
        }

        private void CloseMySqlConnection(Int32 type)
        {
            if (this.MySql_Connection_is_activ == false)
            {
                try
                {
                    switch (type)
                    {
                        case 1:
                            //OdbcCon
                            if (this.MySqlCon != null)
                            {
                                this.MySqlCon.Close();
                                this.DebugInfo("Info", "Connection MySqlCon closed");
                            }
                            break;

                        case 2:
                            //ODBCConn
                            if (this.MySqlConn != null)
                            {
                                this.MySqlConn.Close();
                                this.DebugInfo("Info", "Connection MySqlConn closed");
                            }
                            break;
                        default:
                            break;
                    }
                }
                catch (MySqlException oe)
                {
                    this.DebugInfo("Error", "CloseMySqlConnection:");
                    this.DisplayMySqlErrorCollection(oe);
                }
                catch (Exception c)
                {
                    this.ExecuteCommand("Error", "CloseMySqlConnection: " + c);
                }
            }
        }

        private void tablebuilder()
        {
            if (boolTableEXISTS)
            {
                return;
            }
            lock (this.tablebuilderlock)
            {
                Thread.Sleep(3000);
                if ((m_strHost.Length == 0) || (m_strDatabase.Length == 0) || (m_strDBPort.Length == 0) || (m_strUserName.Length == 0))
                {
                    this.DebugInfo("Error", "Check you MySQL Server Details:, hostname, port, databasename and your login credentials!");
                    this.ExecuteCommand("procon.protected.plugins.enable", "CChatGUIDStatsLogger", "False");
                    return;
                }
                if ((m_strHost != null) && (m_strDatabase != null) && (m_strDBPort != null) && (m_strUserName != null) && (m_strPassword != null) && (boolTableEXISTS == false))
                {
                    this.DebugInfo("Info", "Start tablebuilder");
                    //new
                    this.generateWeaponList();

                    try
                    {
                        using (MySqlConnection TablebuilderCon = new MySqlConnection(this.DBConnectionStringBuilder()))
                        {
                            MySqlConnector.MySqlTransaction TableTransaction = null;
                            try
                            {
                                this.MySql_Connection_is_activ = true;
                                TablebuilderCon.Open();
                                //Chatlog Table
                                String SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_chatlog + @"` (
                            					`ID` INT NOT NULL AUTO_INCREMENT ,
  												`logDate` DATETIME NULL DEFAULT NULL ,
  												`ServerID` SMALLINT UNSIGNED NOT NULL ,
  												`logSubset` VARCHAR(45) NULL DEFAULT NULL ,
  												`logSoldierName` VARCHAR(45) NULL DEFAULT NULL ,
  												`logMessage` TEXT NULL DEFAULT NULL ,
  													PRIMARY KEY (`ID`),
                                                    INDEX `INDEX_SERVERID` (`ServerID` ASC),
                                                    INDEX `INDEX_logDate` (`logDate` ASC))
													ENGINE = InnoDB";
                                TablebuilderCon.Execute(SQLTable);

                                //MapStats Table
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_mapstats + @"` (
  												      `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT ,
                                                      `ServerID` SMALLINT UNSIGNED NOT NULL DEFAULT '0' ,
                                                      `TimeMapLoad` DATETIME NULL DEFAULT NULL ,
                                                      `TimeRoundStarted` DATETIME NULL DEFAULT NULL ,
                                                      `TimeRoundEnd` DATETIME NULL DEFAULT NULL ,
                                                      `MapName` VARCHAR(45) NULL DEFAULT NULL ,
                                                      `Gamemode` VARCHAR(45) NULL DEFAULT NULL ,
                                                      `Roundcount` SMALLINT NOT NULL DEFAULT '0' ,
                                                      `NumberofRounds` SMALLINT NOT NULL DEFAULT '0' ,
                                                      `MinPlayers` SMALLINT NOT NULL DEFAULT '0' ,
                                                      `AvgPlayers` DOUBLE NOT NULL DEFAULT '0' ,
                                                      `MaxPlayers` SMALLINT NOT NULL DEFAULT '0' ,
                                                      `PlayersJoinedServer` SMALLINT NOT NULL DEFAULT '0' ,
                                                      `PlayersLeftServer` SMALLINT NOT NULL DEFAULT '0' ,
                                                      PRIMARY KEY (`ID`) ,
                                                      INDEX `ServerID_INDEX` (`ServerID` ASC) )
                                                    ENGINE = InnoDB";
                                TablebuilderCon.Execute(SQLTable);

                                //Start of the Transaction
                                TableTransaction = TablebuilderCon.BeginTransaction();


                                //Table tbl_games
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_games + @"` (
                                                   `GameID` tinyint(4) unsigned NOT NULL AUTO_INCREMENT,
                                                   `Name` varchar(45) DEFAULT NULL,
                                                   PRIMARY KEY (`GameID`),
                                                   UNIQUE KEY `name_unique` (`Name`)
                                                   ) ENGINE=InnoDB";
                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //Table playerdata
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_playerdata + @"` (
                                                  `PlayerID` INT UNSIGNED NOT NULL AUTO_INCREMENT ,
                                                  `GameID` tinyint(4)unsigned NOT NULL DEFAULT '0',
												  `ClanTag` VARCHAR(10) NULL DEFAULT NULL ,
												  `SoldierName` VARCHAR(45) NULL DEFAULT NULL ,
                                                  `GlobalRank` SMALLINT UNSIGNED NOT NULL DEFAULT '0',
												  `PBGUID` VARCHAR(32) NULL DEFAULT NULL ,
												  `EAGUID` VARCHAR(35) NULL DEFAULT NULL ,
												  `IP_Address` VARCHAR(15) NULL DEFAULT NULL ,
                                                  `IPv6_Address` VARBINARY(16) NULL DEFAULT NULL ,
												  `CountryCode` VARCHAR(2) NULL DEFAULT NULL ,
												  PRIMARY KEY (`PlayerID`) ,
												  UNIQUE INDEX `UNIQUE_playerdata` (`GameID` ASC,`EAGUID` ASC) ,
												  INDEX `INDEX_SoldierName` (`SoldierName` ASC) )
												ENGINE = InnoDB";
                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //Server Table
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_server + @"` (
  								      `ServerID` SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT ,
                                      `ServerGroup` TINYINT UNSIGNED NOT NULL DEFAULT 0 ,
									  `IP_Address` VARCHAR(45) NULL DEFAULT NULL ,
									  `ServerName` VARCHAR(200) NULL DEFAULT NULL ,
                                      `GameID` tinyint(4)unsigned NOT NULL DEFAULT '0',
									  `usedSlots` SMALLINT UNSIGNED NULL DEFAULT 0 ,
									  `maxSlots` SMALLINT UNSIGNED NULL DEFAULT 0 ,
									  `mapName` VARCHAR(45) NULL DEFAULT NULL ,
									  `fullMapName` TEXT NULL DEFAULT NULL ,

									  `Gamemode` VARCHAR(45) NULL DEFAULT NULL ,
									  `GameMod` VARCHAR(45) NULL DEFAULT NULL ,
									  `PBversion` VARCHAR(45) NULL DEFAULT NULL ,
									  `ConnectionState` VARCHAR(45) NULL DEFAULT NULL ,
									  PRIMARY KEY (`ServerID`) ,
                                      INDEX `INDEX_SERVERGROUP` (`ServerGroup` ASC) ,
									  UNIQUE INDEX `IP_Address_UNIQUE` (`IP_Address` ASC) )
									ENGINE = InnoDB";

                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //Server Player Table
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_server_player + @"` (
  								  `StatsID` INT UNSIGNED NOT NULL AUTO_INCREMENT ,
								  `ServerID` SMALLINT UNSIGNED NOT NULL ,
								  `PlayerID` INT UNSIGNED NOT NULL ,
								  PRIMARY KEY (`StatsID`) ,
								  UNIQUE INDEX `UNIQUE_INDEX` (`ServerID` ASC, `PlayerID` ASC) ,
								  INDEX `fk_tbl_server_player_tbl_playerdata" + this.tableSuffix + @"` (`PlayerID` ASC) ,
								  INDEX `fk_tbl_server_player_tbl_server" + this.tableSuffix + @"` (`ServerID` ASC) ,
								  CONSTRAINT `fk_tbl_server_player_tbl_playerdata" + this.tableSuffix + @"`
								    FOREIGN KEY (`PlayerID` )
								    REFERENCES `" + this.tbl_playerdata + @"` (`PlayerID` )
								    ON DELETE CASCADE
								    ON UPDATE NO ACTION,
								  CONSTRAINT `fk_tbl_server_player_tbl_server" + this.tableSuffix + @"`
								    FOREIGN KEY (`ServerID` )
								    REFERENCES `" + this.tbl_server + @"` (`ServerID` )
								    ON DELETE CASCADE
								    ON UPDATE NO ACTION)
								ENGINE = InnoDB";
                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);
                                //
                                //ServerStatistics Table
                                SQLTable = @"CREATE  TABLE IF NOT EXISTS `" + this.tbl_server_stats + @"` (
                                  `ServerID` SMALLINT(5) UNSIGNED NOT NULL ,
                                  `CountPlayers` BIGINT NOT NULL DEFAULT 0 ,
                                  `SumScore` BIGINT NOT NULL DEFAULT 0 ,
                                  `AvgScore` FLOAT NOT NULL DEFAULT 0 ,
                                  `SumKills` BIGINT NOT NULL DEFAULT 0 ,
                                  `AvgKills` FLOAT NOT NULL DEFAULT 0 ,
                                  `SumHeadshots` BIGINT NOT NULL DEFAULT 0 ,
                                  `AvgHeadshots` FLOAT NOT NULL DEFAULT 0 ,
                                  `SumDeaths` BIGINT NOT NULL DEFAULT 0 ,
                                  `AvgDeaths` FLOAT NOT NULL DEFAULT 0 ,
                                  `SumSuicide` BIGINT NOT NULL DEFAULT 0 ,
                                  `AvgSuicide` FLOAT NOT NULL DEFAULT 0 ,
                                  `SumTKs` BIGINT NOT NULL DEFAULT 0 ,
                                  `AvgTKs` FLOAT NOT NULL DEFAULT 0 ,
                                  `SumPlaytime` BIGINT NOT NULL DEFAULT 0 ,
                                  `AvgPlaytime` FLOAT NOT NULL DEFAULT 0 ,
                                  `SumRounds` BIGINT NOT NULL DEFAULT 0 ,
                                  `AvgRounds` FLOAT NOT NULL DEFAULT 0 ,
                                  PRIMARY KEY (`ServerID`) ,
                                  INDEX `fk_tbl_server_stats_tbl_server" + this.tableSuffix + @"` (`ServerID` ASC) ,
                                  CONSTRAINT `fk_tbl_server_stats_tbl_server" + this.tableSuffix + @"`
                                    FOREIGN KEY (`ServerID` )
                                    REFERENCES `" + this.tbl_server + @"` (`ServerID` )
                                    ON DELETE CASCADE
                                    ON UPDATE NO ACTION)
                                ENGINE = InnoDB";
                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //Stats Table
                                SQLTable = @"CREATE  TABLE IF NOT EXISTS `" + this.tbl_playerstats + @"` (
  								  `StatsID` INT UNSIGNED NOT NULL ,
								  `Score` INT NOT NULL DEFAULT '0' ,
								  `Kills` INT UNSIGNED NOT NULL DEFAULT '0' ,
								  `Headshots` INT UNSIGNED NOT NULL DEFAULT '0' ,
								  `Deaths` INT UNSIGNED NOT NULL DEFAULT '0' ,
								  `Suicide` INT UNSIGNED NOT NULL DEFAULT '0' ,
								  `TKs` INT UNSIGNED NOT NULL DEFAULT '0' ,
								  `Playtime` INT UNSIGNED NOT NULL DEFAULT '0' ,
								  `Rounds` INT UNSIGNED NOT NULL DEFAULT '0' ,
								  `FirstSeenOnServer` DATETIME NULL DEFAULT NULL ,
								  `LastSeenOnServer` DATETIME NULL DEFAULT NULL ,
								  `Killstreak` SMALLINT UNSIGNED NOT NULL DEFAULT '0' ,
								  `Deathstreak` SMALLINT UNSIGNED NOT NULL DEFAULT '0' ,
                                  `HighScore` MEDIUMINT UNSIGNED NOT NULL DEFAULT '0' ,
                                  `rankScore` INT UNSIGNED NOT NULL DEFAULT '0' ,
                                  `rankKills` INT UNSIGNED NOT NULL DEFAULT '0' ,
                                  `Wins` INT UNSIGNED NOT NULL DEFAULT '0' ,
                                  `Losses` INT UNSIGNED NOT NULL DEFAULT '0' ,
								  PRIMARY KEY (`StatsID`) ,
                                  INDEX `INDEX_Score" + this.tableSuffix + @"` (`Score`),
                                  KEY `INDEX_RANK_SCORE" + this.tableSuffix + @"` (`rankScore`),
                                  KEY `INDEX_RANK_KILLS" + this.tableSuffix + @"` (`rankKills`),
								  CONSTRAINT `fk_tbl_playerstats_tbl_server_player1" + this.tableSuffix + @"`
								    FOREIGN KEY (`StatsID` )
								    REFERENCES `" + this.tbl_server_player + @"` (`StatsID` )
								    ON DELETE CASCADE
								    ON UPDATE NO ACTION)
								ENGINE = InnoDB";

                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //Playerrank Table
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_playerrank + @"` (
                                      `PlayerID` INT UNSIGNED NOT NULL DEFAULT 0 ,
                                      `ServerGroup` SMALLINT UNSIGNED NOT NULL DEFAULT 0 ,
                                      `rankScore` INT UNSIGNED NOT NULL DEFAULT 0 ,
                                      `rankKills` INT UNSIGNED NOT NULL DEFAULT 0 ,
                                      INDEX `INDEX_SCORERANKING" + this.tableSuffix + @"` (`rankScore` ASC) ,
                                      INDEX `INDEX_KILLSRANKING" + this.tableSuffix + @"` (`rankKills` ASC) ,
                                      PRIMARY KEY (`PlayerID`,`ServerGroup`) ,
                                      CONSTRAINT `fk_tbl_playerrank_tbl_playerdata" + this.tableSuffix + @"`
                                        FOREIGN KEY (`PlayerID` )
                                        REFERENCES `" + this.tbl_playerdata + @"` (`PlayerID` )
                                        ON DELETE CASCADE
                                        ON UPDATE NO ACTION)
                                    ENGINE = InnoDB";

                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //Playersession Table
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_sessions + @"` (
                                          `SessionID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
                                          `StatsID` INT UNSIGNED NOT NULL,
                                          `StartTime` DATETIME NOT NULL,
                                          `EndTime` DATETIME NOT NULL,
                                          `Score` MEDIUMINT NOT NULL DEFAULT '0',
                                          `Kills` SMALLINT(5) UNSIGNED NOT NULL DEFAULT '0',
                                          `Headshots` SMALLINT(5) UNSIGNED NOT NULL DEFAULT '0',
                                          `Deaths` SMALLINT(5) UNSIGNED NOT NULL DEFAULT '0',
                                          `TKs` SMALLINT(5) UNSIGNED NOT NULL DEFAULT '0',
                                          `Suicide` SMALLINT(5) UNSIGNED NOT NULL DEFAULT '0',
                                          `RoundCount` TINYINT UNSIGNED NOT NULL DEFAULT '0',
                                          `Playtime` MEDIUMINT UNSIGNED NOT NULL DEFAULT '0',
                                          `Killstreak` SMALLINT(5) UNSIGNED NOT NULL DEFAULT '0' ,
								          `Deathstreak` SMALLINT(5) UNSIGNED NOT NULL DEFAULT '0' ,
                                          `HighScore` MEDIUMINT UNSIGNED NOT NULL DEFAULT '0' ,
                                          `Wins` TINYINT UNSIGNED NOT NULL DEFAULT '0' ,
                                          `Losses` TINYINT UNSIGNED NOT NULL DEFAULT '0' ,
                                          PRIMARY KEY (`SessionID`),
                                          INDEX `INDEX_STATSID" + this.tableSuffix + @"` (`StatsID` ASC),
                                          INDEX `INDEX_STARTTIME" + this.tableSuffix + @"` (`StartTime` ASC),
                                          CONSTRAINT `fk_tbl_sessions_tbl_server_player" + this.tableSuffix + @"`
                                            FOREIGN KEY (`StatsID`)
                                            REFERENCES `" + this.tbl_server_player + @"` (`StatsID`)
                                            ON DELETE CASCADE
                                            ON UPDATE NO ACTION)
                                         ENGINE=InnoDB";

                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //currentplayers
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_currentplayers + @"` (
                                                  `ServerID` smallint(6) NOT NULL,
                                                  `Soldiername` varchar(45) NOT NULL,
                                                  `GlobalRank` SMALLINT UNSIGNED NOT NULL DEFAULT '0',
                                                  `ClanTag` varchar(45) DEFAULT NULL,
                                                  `Score` int(11) NOT NULL DEFAULT '0',
                                                  `Kills` int(11) NOT NULL DEFAULT '0',
                                                  `Headshots` int(11) NOT NULL DEFAULT '0',
                                                  `Deaths` int(11) NOT NULL DEFAULT '0',
                                                  `Suicide` int(11) DEFAULT NULL,
                                                  `Killstreak` smallint(6) DEFAULT '0',
                                                  `Deathstreak` smallint(6) DEFAULT '0',
                                                  `TeamID` tinyint(4) DEFAULT NULL,
                                                  `SquadID` tinyint(4) DEFAULT NULL,
                                                  `EA_GUID` varchar(45) NOT NULL DEFAULT '',
                                                  `PB_GUID` varchar(45) NOT NULL DEFAULT '',
                                                  `IP_aton` int(11) unsigned DEFAULT NULL,
                                                  `CountryCode` varchar(2) DEFAULT '',
                                                  `Ping` smallint(6) DEFAULT NULL,
                                                  `PlayerJoined` datetime DEFAULT NULL,
                                              PRIMARY KEY (`ServerID`,`Soldiername`)
                                            ) ENGINE=InnoDB";
                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //Awards
                                /*
                                SQLTable = @"CREATE  TABLE IF NOT EXISTS `" + this.tbl_awards + @"` (
                                                      `AwardID` INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY ";
                                foreach (String strcolumn in this.m_lstAwardTable)
                                {
                                    SQLTable = String.Concat(SQLTable, ",`", strcolumn, "` mediumint(8) unsigned DEFAULT '0' ");
                                }
                                SQLTable = String.Concat(SQLTable, ")ENGINE = InnoDB DEFAULT CHARACTER SET = latin1");
                                if (this.m_awardsON == enumBoolYesNo.Yes)
                                {
                                    TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);
                                }
                                */

                                //New Weapon table
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_weapons + @"` (
                                              `WeaponID` int(11) unsigned NOT NULL AUTO_INCREMENT,
                                              `GameID` tinyint(4)unsigned NOT NULL,
                                              `Friendlyname` varchar(45) DEFAULT NULL,
                                              `Fullname` varchar(100) DEFAULT NULL,
                                              `Damagetype` varchar(45) DEFAULT NULL,
                                              `Slot` varchar(45) DEFAULT NULL,
                                              `Kitrestriction` varchar(45) DEFAULT NULL,
                                              PRIMARY KEY (`WeaponID`),
                                              UNIQUE KEY `unique` (`GameID`,`fullname`)
                                            ) ENGINE=InnoDB";
                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //New Weapon stats table
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_weapons_stats + @"` (
                                                  `StatsID` INT unsigned NOT NULL,
                                                  `WeaponID` int(11) unsigned NOT NULL,
                                                  `Kills` int(11) unsigned NOT NULL DEFAULT '0',
                                                  `Headshots` int(11) unsigned NOT NULL DEFAULT '0',
                                                  `Deaths` int(11) unsigned NOT NULL DEFAULT '0',
                                                  PRIMARY KEY (`StatsID`,`WeaponID`),
                                                  KEY `Kills_Death_idx` (`Kills`,`Deaths`),
                                                  KEY `Kills_Head_idx` (`Kills`,`Headshots`),
                                                  CONSTRAINT `fk_tbl_weapons_stats_tbl_server_player_" + this.tableSuffix + @"`
								                    FOREIGN KEY (`StatsID` )
								                    REFERENCES `" + this.tbl_server_player + @"` (`StatsID` )
								                    ON DELETE CASCADE
								                    ON UPDATE NO ACTION
                                                ) ENGINE=InnoDB";
                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //Dogtagstable
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_dogtags + @"` (
                                      `KillerID` INT UNSIGNED NOT NULL ,
									  `VictimID` INT UNSIGNED NOT NULL ,
									  `Count` SMALLINT UNSIGNED NOT NULL DEFAULT '0' ,
									  PRIMARY KEY (`KillerID`, `VictimID`) ,
									  INDEX `fk_tbl_dogtags_tbl_server_player1" + this.tableSuffix + @"` (`KillerID` ASC) ,
									  INDEX `fk_tbl_dogtags_tbl_server_player2" + this.tableSuffix + @"` (`VictimID` ASC) ,
									  CONSTRAINT `fk_tbl_dogtags_tbl_server_player1" + this.tableSuffix + @"`
									    FOREIGN KEY (`KillerID` )
									    REFERENCES `" + this.tbl_server_player + @"` (`StatsID` )
									    ON DELETE CASCADE
									    ON UPDATE NO ACTION,
									  CONSTRAINT `fk_tbl_dogtags_tbl_server_player2" + this.tableSuffix + @"`
									    FOREIGN KEY (`VictimID` )
									    REFERENCES `" + this.tbl_server_player + @"` (`StatsID` )
									    ON DELETE CASCADE
									    ON UPDATE NO ACTION)
									ENGINE = InnoDB";
                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //Score and Tickettable
                                SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_teamscores + @"` (
                                              `ServerID` smallint(5) unsigned NOT NULL,
                                              `TeamID` smallint(5) unsigned NOT NULL,
                                              `Score` int(11) DEFAULT NULL,
                                              `WinningScore` int(11) DEFAULT NULL,
                                              PRIMARY KEY (`ServerID`,`TeamID` )
                                             ) ENGINE=InnoDB";
                                TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);

                                //Commit the Transaction
                                TableTransaction.Commit();

                                this.boolTableEXISTS = true;

                                //fill weapon table
                                //get GameID
                                this.intServerGameType_ID = this.GetGameIDfromDB(this.strServerGameType);

                                List<String> addedWeaponList = new List<String>();

                                foreach (KeyValuePair<String, Dictionary<String, CStats.CUsedWeapon>> branch in this.weaponDic)
                                {
                                    String sqlCheckweapon = @"SELECT
                                                                `GameID`,
                                                                `Friendlyname`,
                                                                `Fullname`,
                                                                `Damagetype`
                                                            FROM `" + this.tbl_weapons + @"`
                                                            WHERE `GameID` = @GameID
                                                              AND `Damagetype` = @Damagetype";

                                    using (DataTable result = this.SQLquery(sqlCheckweapon, new { GameID = this.intServerGameType_ID, Damagetype = branch.Key.ToLower() }))
                                    {
                                        //this.DebugInfo("Info", "Rowcount:" + result.Rows.Count.ToString());
                                        if (result.Rows.Count >= 1)
                                        {
                                            result.PrimaryKey = new DataColumn[] { result.Columns["GameID"], result.Columns["Fullname"] };
                                        }
                                        TableTransaction = null;
                                        TableTransaction = TablebuilderCon.BeginTransaction();

                                        foreach (KeyValuePair<String, CStats.CUsedWeapon> leap in branch.Value)
                                        {
                                            if (result.Rows.Count == 0 || result.Rows.Contains(new Object[] { this.intServerGameType_ID, leap.Value.Name }) == false || addedWeaponList.Contains(leap.Value.Name) == true)
                                            {
                                                addedWeaponList.Add(leap.Value.Name);
                                                //add weapon entry
                                                String sqlInsertQuery = "INSERT INTO `" + this.tbl_weapons + @"` ( `GameID`, `Friendlyname`, `Fullname`,`Damagetype`,`Slot`,`Kitrestriction`) VALUES(@GameID, @Friendlyname, @Fullname, @Damagetype,@Slot,@Kitrestriction)
                                                                            ON DUPLICATE KEY UPDATE `Friendlyname` = @Friendlyname ,`Damagetype` =  @Damagetype,`Slot` = @Slot,`Kitrestriction` = @Kitrestriction";

                                                if (this.intServerGameType_ID != 0)
                                                {
                                                    TablebuilderCon.Execute(sqlInsertQuery, new
                                                    {
                                                        GameID = this.intServerGameType_ID,
                                                        Friendlyname = leap.Value.FieldName,
                                                        Fullname = leap.Value.Name,
                                                        Damagetype = branch.Key.ToLower(),
                                                        Slot = leap.Value.Slot,
                                                        Kitrestriction = leap.Value.KitRestriction
                                                    }, transaction: TableTransaction);
                                                }
                                            }
                                        }
                                        TableTransaction.Commit();
                                    }
                                }

                                //Create WeaponMapping
                                this.WeaponMappingDic = new Dictionary<String, Int32>(this.GetWeaponMappingfromDB());


                                //TableCheck & Adjustemnts tbl_playerstats
                                /*
                                String sqlCheckplayerstats = "DESC `" + this.tbl_playerstats + "`";
                                String sqlAltertableplayerstats = "ALTER TABLE `" + this.tbl_playerstats + "` ";
                                String sqlIndex = "";
                                this.DebugInfo("Trace", "Tablecheck playerstats");
                                Boolean column_Missing = false;
                                using (DataTable result = this.SQLquery(sqlCheckplayerstats))
                                {
                                    DataColumn[] key = new DataColumn[1];
                                    key[0] = result.Columns[0];
                                    result.PrimaryKey = key;
                                    column_Missing = false;

                                    if (result.Rows.Contains("rankScore") == false)
                                    {
                                        this.DebugInfo("Trace", "Column rankScore is missing, Adding it to the table!");
                                        sqlAltertableplayerstats = String.Concat(sqlAltertableplayerstats, "ADD COLUMN `rankScore` INT(10) UNSIGNED NOT NULL DEFAULT '0', ");
                                        column_Missing = true;
                                        sqlIndex = String.Concat(sqlIndex, "ADD INDEX `INDEX_RANK_SCORE" + this.tableSuffix + @"` (`rankScore` ASC), ");
                                    }
                                    if (result.Rows.Contains("rankKills") == false)
                                    {
                                        this.DebugInfo("Trace", "Column rankScore is missing, Adding it to the table!");
                                        sqlAltertableplayerstats = String.Concat(sqlAltertableplayerstats, "ADD COLUMN `rankKills` INT(10) UNSIGNED NOT NULL DEFAULT '0', ");
                                        column_Missing = true;
                                        sqlIndex = String.Concat(sqlIndex, "ADD INDEX `INDEX_RANK_KILLS" + this.tableSuffix + @"` (`rankKills` ASC), ");
                                    }
                                    //Wins & Losses
                                    if (result.Rows.Contains("Wins") == false)
                                    {
                                        this.DebugInfo("Trace", "Column Wins is missing, Adding it to the table!");
                                        sqlAltertableplayerstats = String.Concat(sqlAltertableplayerstats, "ADD COLUMN `Wins` MEDIUMINT UNSIGNED NOT NULL DEFAULT '0', ");
                                        column_Missing = true;
                                    }
                                    if (result.Rows.Contains("Losses") == false)
                                    {
                                        this.DebugInfo("Trace", "Column Losses is missing, Adding it to the table!");
                                        sqlAltertableplayerstats = String.Concat(sqlAltertableplayerstats, "ADD COLUMN `Losses` MEDIUMINT UNSIGNED NOT NULL DEFAULT '0', ");
                                        column_Missing = true;
                                    }
                                    //HighScore
                                    if (result.Rows.Contains("HighScore") == false)
                                    {
                                        this.DebugInfo("Trace", "Column HighScore is missing, Adding it to the table!");
                                        sqlAltertableplayerstats = String.Concat(sqlAltertableplayerstats, "ADD COLUMN `HighScore` MEDIUMINT UNSIGNED NOT NULL DEFAULT '0' , ");
                                        column_Missing = true;
                                    }
                                }
                                if (column_Missing == true)
                                {
                                    TableTransaction = null;
                                    TableTransaction = TablebuilderCon.BeginTransaction();
                                    //Adding Columns
                                    sqlAltertableplayerstats = String.Concat(sqlAltertableplayerstats, sqlIndex);
                                    Int32 charindex = sqlAltertableplayerstats.LastIndexOf(",");
                                    if (charindex > 0)
                                    {
                                        sqlAltertableplayerstats = sqlAltertableplayerstats.Remove(charindex);
                                    }
                                    TablebuilderCon.Execute(sqlAltertableplayerstats, transaction: TableTransaction);
                                    TableTransaction.Commit();
                                }


                                //TableCheck & Adjustemnts tbl_server
                                sqlCheckplayerstats = "DESC `" + this.tbl_server + "`";
                                sqlAltertableplayerstats = "ALTER TABLE `" + this.tbl_server + "` ";
                                sqlIndex = "";
                                this.DebugInfo("Trace", "Tablecheck tbl_server");
                                column_Missing = false;
                                using (DataTable result = this.SQLquery(sqlCheckplayerstats))
                                {
                                    DataColumn[] key = new DataColumn[1];
                                    key[0] = result.Columns[0];
                                    result.PrimaryKey = key;
                                    column_Missing = false;

                                    if (result.Rows.Contains("ServerGroup") == false)
                                    {
                                        this.DebugInfo("Trace", "Column ServerGroup is missing, Adding it to the table!");
                                        sqlAltertableplayerstats = String.Concat(sqlAltertableplayerstats, "ADD COLUMN `ServerGroup` TINYINT UNSIGNED NOT NULL DEFAULT 0 ,");
                                        column_Missing = true;
                                        sqlIndex = String.Concat(sqlIndex, "ADD INDEX `INDEX_SERVERGROUP" + this.tableSuffix + @"` (`ServerGroup` ASC) ,");
                                    }
                                }
                                if (column_Missing == true)
                                {
                                    TableTransaction = null;
                                    TableTransaction = TablebuilderCon.BeginTransaction();
                                    //Adding Columns
                                    sqlAltertableplayerstats = String.Concat(sqlAltertableplayerstats, sqlIndex);
                                    Int32 charindex = sqlAltertableplayerstats.LastIndexOf(",");
                                    if (charindex > 0)
                                    {
                                        sqlAltertableplayerstats = sqlAltertableplayerstats.Remove(charindex);
                                    }
                                    TablebuilderCon.Execute(sqlAltertableplayerstats, transaction: TableTransaction);
                                    TableTransaction.Commit();
                                }

                                */


                                //TableCheck  Adjustments playerstats
                                /*
                                //TableCheck Awards
                                sqlCheck = "DESC `" + this.tbl_awards + "`";
                                sqlAltertable = "ALTER TABLE `" + this.tbl_awards + "` ";
                                //result = new List<String>(this.SQLquery(sqlCheck,9));
                                this.DebugInfo("Tablecheck Awards");
                                using (DataTable result = this.SQLquery(sqlCheck))
                                {
                                    DataColumn[] key = new DataColumn[1];
                                    key[0] = result.Columns[0];
                                    result.PrimaryKey = key;
                                    fieldMissing = false;

                                    foreach (String strField in this.m_lstAwardTable)
                                    {
                                        if (result.Rows.Contains(strField) == false)
                                        {
                                            this.DebugInfo(strField + " is missing, Adding it to the table!");
                                            sqlAltertable = String.Concat(sqlAltertable, "ADD COLUMN `" + strField + "` mediumint(8) unsigned DEFAULT '0', ");
                                            fieldMissing = true;
                                        }
                                    }
                                }
                                if (fieldMissing == true)
                                {
                                    TableTransaction = null;
                                    TableTransaction = TablebuilderCon.BeginTransaction();
                                    SQLTable = "ALTER TABLE `" + this.tbl_awards + "` ENGINE = MyISAM";
                                    TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);
                                    //Adding Columns
                                    Int32 charindex = sqlAltertable.LastIndexOf(",");
                                    if (charindex > 0)
                                    {
                                        sqlAltertable = sqlAltertable.Remove(charindex);
                                    }
                                    TablebuilderCon.Execute(sqlAltertable, transaction: TableTransaction);
                                    SQLTable = "ALTER TABLE `" + this.tbl_awards + "` ENGINE = InnoDB";
                                    TablebuilderCon.Execute(SQLTable, transaction: TableTransaction);
                                    TableTransaction.Commit();
                                }
                                */
                            }

                            catch (MySqlException oe)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in Tablebuilder: ");
                                this.DisplayMySqlErrorCollection(oe);
                                TableTransaction.Rollback();
                            }
                            catch (Exception c)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error: " + c);
                                TableTransaction.Rollback();
                                this.boolTableEXISTS = false;
                                this.m_ID_cache.Clear();
                            }
                            finally
                            {
                                TablebuilderCon.Close();
                            }
                        }
                    }
                    catch (MySqlException oe)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in Tablebuilder: ");
                        this.DisplayMySqlErrorCollection(oe);
                    }
                    catch (Exception c)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error: " + c);
                    }
                }
            }
        }

        private C_ID_Cache GetID(String EAguid)
        {
            Int32 playerID = 0;
            Int32 StatsID = 0;
            if (GlobalDebugMode.Equals("Trace"))
            {
                this.DebugInfo("Trace", "Tying to get IDs form DB or cache for EAGuid: " + EAguid);
            }
            try
            {
                if (this.m_ID_cache.ContainsKey(EAguid) == true)
                {
                    if (this.m_ID_cache[EAguid].Id >= 1 && this.m_ID_cache[EAguid].StatsID >= 1)
                    {
                        //CacheHit
                        if (GlobalDebugMode.Equals("Trace"))
                        {
                            this.DebugInfo("Trace", "Status ID-Cache: used IDs(" + this.m_ID_cache[EAguid].Id + " | " + this.m_ID_cache[EAguid].StatsID + ") from cache for EAGuid " + EAguid);
                        }
                        return this.m_ID_cache[EAguid];
                    }
                    else
                    {
                        //Cachemiss
                        if (this.m_ID_cache[EAguid].Id <= 0)
                        {
                            String sqlPlayerID = @"SELECT `PlayerID` FROM `" + this.tbl_playerdata + "` WHERE `GameID` = @GameID AND `EAGUID` = @EAGUID ";
                            DataTable resultTable = this.SQLquery(sqlPlayerID, new { GameID = this.intServerGameType_ID, EAGUID = EAguid });
                            if (resultTable.Rows != null)
                            {
                                foreach (DataRow row in resultTable.Rows)
                                {
                                    playerID = Convert.ToInt32(row["PlayerID"]);
                                    this.m_ID_cache[EAguid].Id = playerID;
                                }
                            }
                        }
                        else
                        {
                            playerID = this.m_ID_cache[EAguid].Id;
                        }
                        if (playerID >= 1)
                        {
                            String sqlStatsID = @"SELECT `StatsID` FROM `" + this.tbl_server_player + "` WHERE `PlayerID` = @PlayerID AND `ServerID`= @ServerID ";
                            DataTable resultTable = this.SQLquery(sqlStatsID, new { PlayerID = playerID, ServerID = this.ServerID });
                            if (resultTable.Rows != null)
                            {
                                foreach (DataRow row in resultTable.Rows)
                                {
                                    StatsID = Convert.ToInt32(row["StatsID"]);
                                    this.m_ID_cache[EAguid].StatsID = StatsID;
                                }
                            }
                        }
                    }
                }
                else
                {
                    //Cache has no entry
                    String sqlPlayerID = @"SELECT `PlayerID` FROM `" + this.tbl_playerdata + "` WHERE `GameID` = @GameID AND `EAGUID` = @EAGUID ";
                    DataTable resultTable = this.SQLquery(sqlPlayerID, new { GameID = this.intServerGameType_ID, EAGUID = EAguid });
                    if (resultTable.Rows != null)
                    {
                        foreach (DataRow row in resultTable.Rows)
                        {
                            playerID = Convert.ToInt32(row["PlayerID"]);
                        }
                    }
                    if (playerID >= 1)
                    {
                        String sqlStatsID = @"SELECT `StatsID` FROM `" + this.tbl_server_player + "` WHERE `PlayerID` = @PlayerID AND `ServerID`= @ServerID";
                        resultTable = this.SQLquery(sqlStatsID, new { PlayerID = playerID, ServerID = this.ServerID });
                        if (resultTable.Rows != null)
                        {
                            foreach (DataRow row in resultTable.Rows)
                            {
                                StatsID = Convert.ToInt32(row["StatsID"]);
                            }
                        }
                    }
                    this.m_ID_cache.Add(EAguid, new C_ID_Cache(StatsID, playerID, true));
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "GetID: " + c);
                return null;
            }
            if (GlobalDebugMode.Equals("Trace"))
            {
                this.DebugInfo("Trace", "Returning ID: PlayerID: " + playerID + " StatsID: " + StatsID);
            }
            return this.m_ID_cache[EAguid];
        }

        private void UpdateIDCache(List<String> lstEAGUID)
        {
            foreach (String EAGUID in lstEAGUID)
            {
                if (this.m_ID_cache.ContainsKey(EAGUID) == false)
                {
                    //Adding an empty entry
                    this.m_ID_cache.Add(EAGUID, new C_ID_Cache(0, 0, true));
                }
            }
            if (lstEAGUID.Count == 0)
            {
                this.DebugInfo("Trace", "UpdateIDCache: Empty List!");
                return;
            }

            StringBuilder SQL = new StringBuilder(@"SELECT EAGUID, tpd.PlayerID, tsp.StatsID
                          FROM " + this.tbl_playerdata + @" tpd
                          LEFT JOIN " + this.tbl_server_player + @" tsp ON tpd.PlayerID = tsp.PlayerID AND ServerID = @ServerID
                          WHERE tpd.GameID = @GameID AND tpd.EAGUID IN (");
            DynamicParameters dynParams = new DynamicParameters();
            dynParams.Add("ServerID", this.ServerID);
            dynParams.Add("GameID", this.intServerGameType_ID);
            for (Int32 i = 1; i <= lstEAGUID.Count; i++)
            {
                SQL.Append("@EAGUID" + i + ",");
                dynParams.Add("EAGUID" + i, lstEAGUID[i - 1]);
            }
            SQL.Length = SQL.Length - 1;
            SQL.Append(')');
            try
            {
                DataTable result = this.SQLquery(SQL.ToString(), dynParams);
                if (result != null)
                {
                    foreach (DataRow row in result.Rows)
                    {
                        if (row[1] == Convert.DBNull)
                        {
                            this.m_ID_cache.Add(row[0].ToString(), new C_ID_Cache(0, 0, true));
                            continue;
                        }
                        if (this.m_ID_cache.ContainsKey(row[0].ToString()))
                        {
                            this.m_ID_cache[row[0].ToString()].Id = Convert.ToInt32(row[1]);
                            if (row[2] == Convert.DBNull)
                            {
                                this.m_ID_cache[row[0].ToString()].StatsID = 0;
                            }
                            else
                            {
                                this.m_ID_cache[row[0].ToString()].StatsID = Convert.ToInt32(row[2]);
                            }
                        }
                        else
                        {
                            if (row[2] == Convert.DBNull)
                            {
                                this.m_ID_cache.Add(row[0].ToString(), new C_ID_Cache(0, Convert.ToInt32(row[1]), true));
                            }
                            else
                            {
                                this.m_ID_cache.Add(row[0].ToString(), new C_ID_Cache(Convert.ToInt32(row[2]), Convert.ToInt32(row[1]), true));
                            }
                        }
                    }
                }
            }
            catch (MySqlException oe)
            {
                this.DebugInfo("Error", "Error in UpdateCacheID: ");
                this.DisplayMySqlErrorCollection(oe);
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "UpdateIDCache: " + c);
            }
        }

        private void UpdateCurrentPlayerTable(List<CPlayerInfo> lstPlayers)
        {
            if (this.boolTableEXISTS == false || ServerID <= 0)
            {
                return;
            }
            this.DebugInfo("Trace", "UpdateCurrentPlayerTable");
            Boolean success = false;
            Int32 attemptCount = 0;
            try
            {
                using (MySqlConnection DBConnection = new MySqlConnection(this.DBConnectionStringBuilder()))
                {
                    String deleteSQL = "DELETE FROM " + this.tbl_currentplayers + " WHERE ServerID = @ServerID";
                    MySqlConnector.MySqlTransaction Tx = null;

                    DBConnection.Open();
                    while (!success)
                    {
                        attemptCount++;
                        try
                        {
                            //Start of the Transaction
                            Tx = DBConnection.BeginTransaction();

                            DBConnection.Execute(deleteSQL, new { ServerID = this.ServerID }, transaction: Tx);

                            if (lstPlayers.Count > 0)
                            {
                                StringBuilder InsertSQL = new StringBuilder("INSERT INTO " + this.tbl_currentplayers + " (ServerID, SoldierName, ClanTag ,EA_GUID, PB_GUID, Score, Kills, Deaths, Headshots, Suicide, TeamID, SquadID, PlayerJoined, Ping, IP_aton, CountryCode, Killstreak, Deathstreak, GlobalRank ) VALUES ");
                                DynamicParameters dynParams = new DynamicParameters();
                                dynParams.Add("ServerID", this.ServerID);
                                Int32 i = 0;
                                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                                {
                                    InsertSQL.Append("( @ServerID, @SoldierName" + i + ",@ClanTag" + i + ", @EA_GUID" + i + ", @PB_GUID" + i + ", @Score" + i + ", @Kills" + i + ", @Deaths" + i + ", @Suicide" + i + ", @Headshots" + i + ",@TeamID" + i + ",@SquadID" + i + " ,@PlayerJoined" + i + ",@Ping" + i + ", INET_ATON( @IP_aton" + i + ")" + ",@CountryCode" + i + ",@Killstreak" + i + ",@Deathstreak" + i + ",@GlobalRank" + i + "),");

                                    dynParams.Add("SoldierName" + i, cpiPlayer.SoldierName);
                                    dynParams.Add("ClanTag" + i, cpiPlayer.ClanTag + "");
                                    dynParams.Add("EA_GUID" + i, cpiPlayer.GUID);
                                    if (this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true)
                                    {
                                        dynParams.Add("PB_GUID" + i, this.StatsTracker[cpiPlayer.SoldierName].Guid);
                                    }
                                    else
                                    {
                                        dynParams.Add("PB_GUID" + i, ""); //placeholder
                                    }
                                    dynParams.Add("Score" + i, cpiPlayer.Score);
                                    dynParams.Add("Kills" + i, cpiPlayer.Kills);
                                    dynParams.Add("Deaths" + i, cpiPlayer.Deaths);
                                    if (this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true)
                                    {
                                        dynParams.Add("Headshots" + i, this.StatsTracker[cpiPlayer.SoldierName].Headshots);
                                        dynParams.Add("PlayerJoined" + i, this.StatsTracker[cpiPlayer.SoldierName].Playerjoined);
                                        dynParams.Add("CountryCode" + i, this.StatsTracker[cpiPlayer.SoldierName].PlayerCountryCode);
                                        dynParams.Add("Killstreak" + i, this.StatsTracker[cpiPlayer.SoldierName].Killstreak);
                                        dynParams.Add("Deathstreak" + i, this.StatsTracker[cpiPlayer.SoldierName].Deathstreak);
                                        dynParams.Add("Suicide" + i, this.StatsTracker[cpiPlayer.SoldierName].Suicides);

                                        // Check if String is empty or null. If it is then send a 0.0.0.0 instead for ip address
                                        if (String.IsNullOrEmpty(this.StatsTracker[cpiPlayer.SoldierName].IP.Trim()))
                                        {
                                            dynParams.Add("IP_aton" + i, "0.0.0.0");
                                        }
                                        else
                                        {
                                            dynParams.Add("IP_aton" + i, this.StatsTracker[cpiPlayer.SoldierName].IP.Trim());
                                        }
                                    }
                                    else
                                    {
                                        dynParams.Add("Headshots" + i, 0); //Headshot placeholder
                                        dynParams.Add("PlayerJoined" + i, MyDateTime.Now);
                                        dynParams.Add("CountryCode" + i, "");
                                        dynParams.Add("Killstreak" + i, 0);
                                        dynParams.Add("Deathstreak" + i, 0);
                                        dynParams.Add("Suicide" + i, 0);
                                        dynParams.Add("IP_aton" + i, "0.0.0.0");
                                    }
                                    dynParams.Add("TeamID" + i, cpiPlayer.TeamID);
                                    dynParams.Add("SquadID" + i, cpiPlayer.SquadID);
                                    if (cpiPlayer.Ping >= 0 && cpiPlayer.Ping < 65000)
                                    {
                                        dynParams.Add("Ping" + i, cpiPlayer.Ping);
                                    }
                                    else
                                    {
                                        dynParams.Add("Ping" + i, 0);
                                    }
                                    if (cpiPlayer.Rank >= 0 && cpiPlayer.Rank < 6500)
                                    {
                                        dynParams.Add("GlobalRank" + i, cpiPlayer.Rank);
                                    }
                                    else
                                    {
                                        dynParams.Add("GlobalRank" + i, 0);
                                    }
                                    //Increment Index
                                    i++;
                                }
                                InsertSQL.Length = InsertSQL.Length - 1;
                                DBConnection.Execute(InsertSQL.ToString(), dynParams, transaction: Tx);
                            }
                            Tx.Commit();
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
                                        this.DebugInfo("Warning", "Warning in UpdateCurrentPlayerTable: Locktimeout or Deadlock occured restarting Transaction(delete and Insert). Attempt: " + attemptCount);
                                        try
                                        {
                                            if (Tx.Connection != null)
                                            {
                                                Tx.Rollback();
                                            }
                                        }
                                        catch { }
                                        Thread.Sleep(attemptCount * 1000);
                                    }
                                    else
                                    {
                                        this.DebugInfo("Error", "Error in UpdateCurrentPlayerTable: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction delete und Insert)");
                                        throw;
                                    }
                                    break;
                                default:
                                    throw; //Other exceptions
                            }
                        }
                    }
                    //Reset Boolean and counter
                    attemptCount = 0;
                    success = false;
                    try
                    {
                        DBConnection.Close();
                    }
                    catch { };
                }
            }
            catch (MySqlException oe)
            {
                this.DebugInfo("Error", "Error in UpdateCurrentPlayerTable: ");
                this.DisplayMySqlErrorCollection(oe);
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "Error in UpdateCurrentPlayerTable: " + c.ToString());
            }

        }

        private void getUpdateServerID(CServerInfo csiServerInfo)
        {
            try
            {
                //return;
                this.DebugInfo("Trace", "getUpdateServerID");
                this.tablebuilder();
                DataTable resultTable;
                String SQL = String.Empty;
                Int32 attemptCount = 0;
                Boolean success = false;
                using (MySqlConnection DBConnection = new MySqlConnection(this.DBConnectionStringBuilder()))
                {
                    MySqlConnector.MySqlTransaction Tx = null;
                    try
                    {
                        DBConnection.Open();
                        String sqlSelectServer = "SELECT `ServerID` FROM " + this.tbl_server + @" WHERE IP_Address = @IP_Address";
                        resultTable = this.SQLquery(sqlSelectServer, new { IP_Address = this.m_strHostName + ":" + this.m_strPort });
                        if (resultTable.Rows != null)
                        {
                            foreach (DataRow row in resultTable.Rows)
                            {
                                Int32.TryParse(row[0].ToString(), out this.ServerID);
                                this.DebugInfo("Trace", "DB returns ServerID = " + this.ServerID);
                            }
                        }
                        if (ServerID <= 0)
                        {
                            SQL = @"INSERT INTO " + tbl_server + @" (IP_Address, ServerName, ServerGroup, usedSlots, maxSlots, mapName, GameID, Gamemode) VALUES (@IP_Address, @ServerName, @ServerGroup, @usedSlots, @maxSlots, @mapName, @GameID, @Gamemode)";
                        }
                        else
                        {
                            SQL = @"UPDATE " + tbl_server + @" SET ServerName = @ServerName, ServerGroup = @ServerGroup , usedSlots = @usedSlots, maxSlots = @maxSlots, mapName = @mapName, GameID = @GameID, Gamemode = @Gamemode WHERE IP_Address = @IP_Address";
                        }
                        while (attemptCount < this.TransactionRetryCount && !success)
                        {
                            attemptCount++;
                            try
                            {
                                Tx = DBConnection.BeginTransaction();
                                Int32 lastInsertId = 0;
                                using (MySqlCommand MySqlCom = new MySqlCommand(SQL, DBConnection, Tx))
                                {
                                    MySqlCom.Parameters.AddWithValue("@IP_Address", this.m_strHostName + ":" + this.m_strPort);
                                    MySqlCom.Parameters.AddWithValue("@ServerName", csiServerInfo.ServerName);
                                    MySqlCom.Parameters.AddWithValue("@ServerGroup", this.intServerGroup);
                                    MySqlCom.Parameters.AddWithValue("@usedSlots", csiServerInfo.PlayerCount);
                                    MySqlCom.Parameters.AddWithValue("@maxSlots", csiServerInfo.MaxPlayerCount);
                                    MySqlCom.Parameters.AddWithValue("@mapName", csiServerInfo.Map);
                                    MySqlCom.Parameters.AddWithValue("@GameID", this.intServerGameType_ID);
                                    MySqlCom.Parameters.AddWithValue("@Gamemode", csiServerInfo.GameMode);
                                    MySqlCom.ExecuteNonQuery();
                                    if (ServerID == 0)
                                    {
                                        Int32.TryParse(MySqlCom.LastInsertedId.ToString(), out this.ServerID);
                                    }
                                }
                                if (ServerID > 0 && this.m_enableCurrentPlayerstatsTable == enumBoolYesNo.Yes && csiServerInfo.TeamScores.Count > 0)
                                {
                                    DBConnection.Execute("DELETE FROM `" + this.tbl_teamscores + "` WHERE `ServerID` = @ServerID",
                                        new { ServerID = this.ServerID }, transaction: Tx);

                                    foreach (TeamScore teamscore in csiServerInfo.TeamScores)
                                    {
                                        DBConnection.Execute("INSERT INTO `" + this.tbl_teamscores + "` (`ServerID`,`TeamID`,`Score`,`WinningScore`) VALUES(@ServerID, @TeamID, @Score, @WinningScore)",
                                            new { ServerID = this.ServerID, TeamID = teamscore.TeamID, Score = teamscore.Score, WinningScore = teamscore.WinningScore },
                                            transaction: Tx);
                                    }
                                }
                                Tx.Commit();
                                success = true;
                            }
                            catch (MySqlException ex)
                            {
                                switch (ex.Number)
                                {
                                    case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                    case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                        this.DebugInfo("Warning", "Warning in getUpdateServer: Lock timeout or Deadlock occured restarting Transaction #1. Attempt: " + attemptCount);
                                        try
                                        {
                                            Tx.Rollback();
                                        }
                                        catch { }
                                        Thread.Sleep(attemptCount * 1000);
                                        break;
                                    default:
                                        throw; //Other exceptions
                                }
                            }
                        }
                    }
                    catch (Exception c)
                    {
                        this.DebugInfo("Error", "getUpdateServerID1: " + c);
                        try
                        {
                            Tx.Rollback();
                        }
                        catch { }
                    }
                    finally
                    {
                        try
                        {
                            DBConnection.Close();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "getUpdateServerID1: " + c);
            }
        }

        private void UpdateRanking()
        {
            try
            {
                //retrycount
                Int32 attemptCount = 0;
                Boolean success = false;

                String sqlupdate1 = @"UPDATE " + this.tbl_playerstats + @" tps
                                INNER JOIN (
                                            SELECT (@num := @num+1) AS rankScore, innersub.StatsID FROM
                                                (
                                                    SELECT tsp.StatsID
                                                    FROM " + this.tbl_playerstats + @" tps
                                                    INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.StatsID = tps.StatsID ,(SELECT @num := 0) x
                                                    WHERE tsp.ServerID = @ServerID
                                                    ORDER BY tps.Score DESC, tps.StatsID ASC
                                                ) innersub
                                            ) sub
                                ON sub.StatsID = tps.StatsID
                                SET tps.rankScore = sub.rankScore
                                WHERE sub.rankScore != tps.rankScore";

                String sqlupdate2 = @"UPDATE " + this.tbl_playerstats + @" tps
                                INNER JOIN (
                                            SELECT (@num := @num+1) AS rankKills, innersub.StatsID FROM
                                                (
                                                    SELECT tsp.StatsID
                                                    FROM " + this.tbl_playerstats + @" tps
                                                    INNER JOIN " + this.tbl_server_player + @" tsp ON tsp.StatsID = tps.StatsID ,(SELECT @num := 0) x
                                                    WHERE tsp.ServerID = @ServerID
                                                    ORDER BY tps.Kills DESC, tps.Deaths ASC , tps.StatsID ASC

                                                ) innersub
                                            ) sub
                                ON sub.StatsID = tps.StatsID
                                SET tps.rankKills = sub.rankKills
                                WHERE sub.rankKills != tps.rankKills";

                // Global Updates
                String sqlInsert = @"INSERT INTO " + this.tbl_playerrank + @" (PlayerID, ServerGroup)
                                    SELECT PlayerID, (" + this.intServerGroup + @") AS ServerGroup
                                    FROM " + this.tbl_playerdata + @"
                                    WHERE PlayerID NOT IN (SELECT PlayerID FROM " + this.tbl_playerrank + @" WHERE ServerGroup = @ServerGroup)";


                String sqlupdate3 = @"  UPDATE " + this.tbl_playerrank + @" tpr
                                    INNER JOIN (SELECT (@num := @num + 1) AS rankKills, sub1.PlayerID ,sub1.ServerGroup
                                                      FROM(SELECT tsp.PlayerID, ts.ServerGroup
                                                           FROM " + this.tbl_server_player + @" tsp
                                                           INNER JOIN " + this.tbl_server + @" ts ON tsp.ServerID = ts.ServerID
                                                           INNER JOIN " + this.tbl_playerstats + @" tps  ON  tsp.StatsID = tps.StatsID ,(SELECT @num := 0) x
                                                           WHERE ts.ServerGroup = @ServerGroup
                                                           GROUP BY tsp.PlayerID, ts.ServerGroup
                                                           ORDER BY SUM(tps.Kills) DESC, SUM(tps.Deaths) ASC, tsp.PlayerID ASC
                                                     ) sub1
                                                ) sub
                                    ON sub.PlayerID = tpr.PlayerID
                                    SET tpr.rankKills = sub.rankKills
                                    WHERE tpr.rankKills != sub.rankKills AND sub.ServerGroup = tpr.ServerGroup";


                String sqlupdate4 = @"  UPDATE " + this.tbl_playerrank + @" tpr
                                    INNER JOIN (SELECT (@num := @num + 1) AS rankScore, sub1.PlayerID ,sub1.ServerGroup
                                                      FROM(SELECT tsp.PlayerID, ts.ServerGroup
                                                           FROM " + this.tbl_server_player + @" tsp
                                                           INNER JOIN " + this.tbl_server + @" ts ON tsp.ServerID = ts.ServerID
                                                           INNER JOIN " + this.tbl_playerstats + @" tps  ON  tsp.StatsID = tps.StatsID ,(SELECT @num := 0) y
                                                           WHERE ts.ServerGroup = @ServerGroup
                                                           GROUP BY tsp.PlayerID, ts.ServerGroup
                                                           ORDER BY SUM(tps.Score) DESC, tsp.PlayerID ASC
                                                     ) sub1
                                                ) sub
                                    ON sub.PlayerID = tpr.PlayerID AND sub.ServerGroup = tpr.ServerGroup
                                    SET tpr.rankScore = sub.rankScore
                                    WHERE tpr.rankScore != sub.rankScore";

                MySqlConnector.MySqlTransaction Tx = null;
                using (MySqlConnection Con = new MySqlConnection(this.DBConnectionStringBuilder()))
                {
                    try
                    {
                        if (Con.State == ConnectionState.Closed)
                        {
                            Con.Open();
                        }

                        if (boolSkipServerUpdate == false)
                        {
                            while (attemptCount < this.TransactionRetryCount && !success)
                            {
                                attemptCount++;
                                try
                                {
                                    Tx = Con.BeginTransaction();
                                    Con.Execute(sqlupdate1, new { ServerID = this.ServerID }, transaction: Tx);
                                    //Commit
                                    Tx.Commit();
                                    success = true;
                                }
                                catch (MySqlException ex)
                                {
                                    switch (ex.Number)
                                    {
                                        case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                        case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                            this.DebugInfo("Warning", "Warning in UpdateRanking: Lock timeout or Deadlock occured restarting Transaction #1. Attempt: " + attemptCount);
                                            try
                                            {
                                                Tx.Rollback();
                                            }
                                            catch { }
                                            Thread.Sleep(attemptCount * 1000);
                                            break;
                                        default:
                                            throw; //Other exceptions
                                    }
                                }
                            }
                            if (attemptCount > this.TransactionRetryCount)
                            {
                                this.DebugInfo("Error", "Error in UpdateRanking: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction #1)");
                            }
                            attemptCount = 0;
                            success = false;

                            //Next query
                            while (attemptCount < this.TransactionRetryCount && !success)
                            {
                                attemptCount++;
                                try
                                {
                                    //Start new Transaction
                                    Tx = Con.BeginTransaction();
                                    Con.Execute(sqlupdate2, new { ServerID = this.ServerID }, transaction: Tx);
                                    //Commit
                                    Tx.Commit();
                                    success = true;
                                }
                                catch (MySqlException ex)
                                {
                                    switch (ex.Number)
                                    {
                                        case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                        case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                            this.DebugInfo("Warning", "Warning in UpdateRanking: Lock timeout or Deadlock occured restarting Transaction #2. Attempt: " + attemptCount);
                                            try
                                            {
                                                Tx.Rollback();
                                            }
                                            catch { }
                                            Thread.Sleep(attemptCount * 1000);
                                            break;
                                        default:
                                            throw; //Other exceptions
                                    }
                                }
                            }
                            if (attemptCount > this.TransactionRetryCount)
                            {
                                this.DebugInfo("Error", "Error in UpdateRanking: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction #2)");
                            }
                        }
                        attemptCount = 0;
                        success = false;

                        //Next query
                        if (boolSkipGlobalUpdate == false)
                        {
                            while (attemptCount < this.TransactionRetryCount && !success)
                            {
                                attemptCount++;
                                try
                                {
                                    //Start new Transaction
                                    Tx = Con.BeginTransaction();
                                    Con.Execute(sqlInsert, new { ServerGroup = this.intServerGroup }, transaction: Tx);
                                    //Commit
                                    Tx.Commit();
                                    success = true;
                                }
                                catch (MySqlException ex)
                                {
                                    switch (ex.Number)
                                    {
                                        case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                        case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                            this.DebugInfo("Warning", "Warning in UpdateRanking: Lock timeout or Deadlock occured restarting Transaction #3. Attempt: " + attemptCount);
                                            try
                                            {
                                                Tx.Rollback();
                                            }
                                            catch { }
                                            Thread.Sleep(attemptCount * 1000);
                                            break;
                                        default:
                                            throw; //Other exceptions
                                    }
                                }
                            }
                            if (attemptCount > this.TransactionRetryCount)
                            {
                                this.DebugInfo("Error", "Error in UpdateRanking: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction #3)");
                            }
                            attemptCount = 0;
                            success = false;

                            //Next query
                            while (attemptCount < this.TransactionRetryCount && !success)
                            {
                                attemptCount++;
                                try
                                {
                                    //Start new Transaction
                                    Tx = Con.BeginTransaction();
                                    Con.Execute(sqlupdate3, new { ServerGroup = this.intServerGroup }, transaction: Tx);
                                    //Commit
                                    Tx.Commit();
                                    success = true;
                                }
                                catch (MySqlException ex)
                                {
                                    switch (ex.Number)
                                    {
                                        case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                        case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                            this.DebugInfo("Warning", "Warning in UpdateRanking: Lock timeout or Deadlock occured restarting Transaction #4. Attempt: " + attemptCount);
                                            try
                                            {
                                                Tx.Rollback();
                                            }
                                            catch { }
                                            Thread.Sleep(attemptCount * 1000);
                                            break;
                                        default:
                                            throw; //Other exceptions
                                    }
                                }
                            }
                            if (attemptCount > this.TransactionRetryCount)
                            {
                                this.DebugInfo("Error", "Error in UpdateRanking: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction #4)");
                            }
                            attemptCount = 0;
                            success = false;

                            //Next query
                            while (attemptCount < this.TransactionRetryCount && !success)
                            {
                                attemptCount++;
                                try
                                {
                                    //Start new Transaction
                                    Tx = Con.BeginTransaction();
                                    Con.Execute(sqlupdate4, new { ServerGroup = this.intServerGroup }, transaction: Tx);
                                    Tx.Commit();
                                    success = true;
                                }
                                catch (MySqlException ex)
                                {
                                    switch (ex.Number)
                                    {
                                        case 1205: //(ER_LOCK_WAIT_TIMEOUT) Lock wait timeout exceeded
                                        case 1213: //(ER_LOCK_DEADLOCK) Deadlock found when trying to get lock
                                            this.DebugInfo("Warning", "Warning in UpdateRanking: Lock timeout or Deadlock occured restarting Transaction #5. Attempt: " + attemptCount);
                                            try
                                            {
                                                Tx.Rollback();
                                            }
                                            catch { }
                                            Thread.Sleep(attemptCount * 1000);
                                            break;
                                        default:
                                            throw; //Other exceptions
                                    }
                                }
                            }
                            if (attemptCount > this.TransactionRetryCount)
                            {
                                this.DebugInfo("Error", "Error in UpdateRanking: Maximum number of " + this.TransactionRetryCount + " transaction retrys exceeded (Transaction #5)");
                            }
                        }
                        attemptCount = 0;
                        success = false;
                    }
                    catch (MySqlException oe)
                    {
                        this.DebugInfo("Error", "Error in UpdateRanking: ");
                        this.DisplayMySqlErrorCollection(oe);
                        if (Tx != null)
                        {
                            try
                            {
                                Tx.Rollback();
                            }
                            catch { };
                        }
                    }
                    catch (Exception c)
                    {
                        this.DebugInfo("Error", "Error in UpdateRanking: " + c);
                        if (Tx != null)
                        {
                            try
                            {
                                Tx.Rollback();
                            }
                            catch { };
                        }
                    }
                    finally
                    {
                        try
                        {
                            Con.Close();
                        }
                        catch { };
                    }
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "Error in UpdateRanking: " + c);
            }
        }


        public void DisplayMySqlErrorCollection(MySqlException myException)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Message: " + myException.Message);
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Native: " + myException.ErrorCode.ToString());
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Source: " + myException.Source.ToString());
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^1StackTrace: " + myException.StackTrace.ToString());
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^1InnerException: " + myException.InnerException.ToString());
            // this.ExecuteCommand("procon.protected.pluginconsole.write", "^1SQL: " + myException.);
        }

        private void prepareTablenames()
        {
            this.tbl_playerdata = "tbl_playerdata" + this.tableSuffix;
            this.tbl_playerstats = "tbl_playerstats" + this.tableSuffix;
            this.tbl_weaponstats = "tbl_weaponstats" + this.tableSuffix;
            this.tbl_dogtags = "tbl_dogtags" + this.tableSuffix;
            this.tbl_mapstats = "tbl_mapstats" + this.tableSuffix;
            this.tbl_chatlog = "tbl_chatlog" + this.tableSuffix;
            this.tbl_bfbcs = "tbl_bfbcs" + this.tableSuffix;
            this.tbl_awards = "tbl_awards" + this.tableSuffix;
            this.tbl_server = "tbl_server" + this.tableSuffix;
            this.tbl_server_player = "tbl_server_player" + this.tableSuffix;
            this.tbl_server_stats = "tbl_server_stats" + this.tableSuffix;
            this.tbl_playerrank = "tbl_playerrank" + this.tableSuffix;
            this.tbl_sessions = "tbl_sessions" + this.tableSuffix;
            this.tbl_currentplayers = "tbl_currentplayers" + this.tableSuffix;
            this.tbl_weapons = "tbl_weapons" + this.tableSuffix;
            this.tbl_weapons_stats = "tbl_weapons_stats" + this.tableSuffix;
            this.tbl_games = "tbl_games" + this.tableSuffix;
            this.tbl_teamscores = "tbl_teamscores" + this.tableSuffix;

        }

        private void setGameMod()
        {
            //this.PrepareKeywordDic();
            this.boolTableEXISTS = false;
        }

        public void generateWeaponList()
        {
            this.DebugInfo("Trace", "generateWeaponList");
            List<String> weapList = new List<String>();
            this.weaponDic.Clear();
            this.DamageClass.Clear();
            try
            {
                WeaponDictionary weapons = this.GetWeaponDefines();
                foreach (PRoCon.Core.Players.Items.Weapon weapon in weapons)
                {
                    String[] weaponName = System.Text.RegularExpressions.Regex.Replace(weapon.Name.Replace("Weapons/", "").Replace("Gadgets/", ""), @"XP\d_", "").Split('/');
                    if (weapList.Contains(weaponName[0].Replace(' ', '_').Replace(".", "").Replace("U_", "")) == false)
                    {
                        weapList.Add(weaponName[0].Replace(' ', '_').Replace(".", "").Replace("U_", ""));
                    }
                    if (this.weaponDic.ContainsKey(weapon.Damage.ToString()) == false)
                    {
                        this.weaponDic.Add(weapon.Damage.ToString(), new Dictionary<String, CStats.CUsedWeapon>());
                    }
                    if (this.weaponDic[weapon.Damage.ToString()].ContainsKey(weapon.Name) == false)
                    {
                        this.weaponDic[weapon.Damage.ToString()].Add(weapon.Name, new CStats.CUsedWeapon(weapon.Name, weaponName[0].Replace(' ', '_').Replace(".", "").Replace("U_", ""), weapon.Slot.ToString(), weapon.KitRestriction.ToString()));
                    }
                    this.DamageClass.Add(weapon.Name, weapon.Damage.ToString());
                }
                this.PrepareKeywordDic();
            }
            catch (Exception e)
            {
                this.DebugInfo("Error", "generateWeaponList: " + e.ToString());
            }
            foreach (KeyValuePair<String, Dictionary<String, CStats.CUsedWeapon>> branch in this.weaponDic)
            {
                foreach (KeyValuePair<String, CStats.CUsedWeapon> leap in branch.Value)
                {
                    this.DebugInfo("Trace", "Weaponlist: DamageType: " + branch.Key + " Name: " + leap.Key);
                }
            }
        }

        public void DebugInfo(String debuglevel, String DebugMessage)
        {
            switch (this.GlobalDebugMode)
            {
                case "Trace":
                    //Post every Message
                    break;
                case "Info":
                    if (String.Equals(debuglevel, "Trace") == true)
                    {
                        return;
                    }
                    break;
                case "Warning":
                    if (String.Equals(debuglevel, "Trace") == true || String.Equals(debuglevel, "Info") == true)
                    {
                        return;
                    }
                    break;
                case "Error":
                    if (String.Equals(debuglevel, "Error") == false)
                    {
                        return;
                    }
                    break;
            }
            // Post error Message in correct Format
            if (String.Equals(debuglevel, "Trace"))
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "[Statslogger]Trace: " + DebugMessage);
            }
            else if (String.Equals(debuglevel, "Info"))
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^2" + "[Statslogger]Info: " + DebugMessage);
            }
            else if (String.Equals(debuglevel, "Warning"))
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3" + "[Statslogger]Warning: " + DebugMessage);
            }
            else if (String.Equals(debuglevel, "Error"))
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^8" + "[Statslogger]Error: " + DebugMessage);
            }
        }

        private Int32 GetGameIDfromDB(String strGame)
        {
            this.DebugInfo("Trace", "GetGameIDfromDB Game: " + strGame);
            Int32 intGameID = 0;
            try
            {
                String sqlSelect = "SELECT `GameID` FROM `" + this.tbl_games + @"` WHERE `Name` = @Name";
                DataTable result = this.SQLquery(sqlSelect, new { Name = strGame });
                if (result.Rows.Count != 0)
                {
                    intGameID = Convert.ToInt32(result.Rows[0][0]);
                }
                else
                {
                    this.DebugInfo("Trace", "GetGameIDfromDB Game:  no gameID found");
                    //Insert Game
                    using (MySqlConnection Con = new MySqlConnection(this.DBConnectionStringBuilder()))
                    {
                        Con.Open();
                        MySqlTransaction Transaction = null;
                        //Start of the Transaction
                        Transaction = Con.BeginTransaction();

                        String SQL = @"INSERT INTO `" + this.tbl_games + @"` (`Name`) VALUES (@Name)";
                        using (MySqlCommand MyCom = new MySqlCommand(SQL, Con, Transaction))
                        {
                            MyCom.Parameters.AddWithValue("@Name", this.strServerGameType);
                            MyCom.ExecuteNonQuery();
                            this.DebugInfo("Trace", "GetGameIDfromDB LastInsertedId: " + MyCom.LastInsertedId.ToString());
                            intGameID = Convert.ToInt32(MyCom.LastInsertedId);
                        }
                        Transaction.Commit();
                    }
                }
            }
            catch (MySqlException oe)
            {
                this.DebugInfo("Error", "Error in GetGameIDfromDB: ");
                this.DisplayMySqlErrorCollection(oe);
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "Error in GetGameIDfromDB: " + c);
            }
            this.DebugInfo("Trace", "GetGameIDfromDB GameID: " + intGameID);
            return intGameID;
        }

        private Dictionary<String, Int32> GetWeaponMappingfromDB()
        {
            Dictionary<String, Int32> mappingDic = new Dictionary<String, Int32>();
            try
            {
                String sqlSelect = "SELECT `WeaponID`,`Fullname` FROM `" + this.tbl_weapons + @"` WHERE `GameID` = @GameID";
                DataTable result = this.SQLquery(sqlSelect, new { GameID = this.intServerGameType_ID });
                if (result != null || result.Rows.Count != 0)
                {
                    foreach (DataRow row in result.Rows)
                    {
                        mappingDic.Add(row["Fullname"].ToString(), Convert.ToInt32(row["WeaponID"]));
                        this.DebugInfo("Trace", "WeaponMapping: ID: " + Convert.ToInt32(row["WeaponID"]).ToString() + " <--> Weapon:" + row["Fullname"].ToString());
                    }
                }
            }
            catch (MySqlException oe)
            {
                this.DebugInfo("Error", "Error in GetWeaponMappingfromDB: ");
                this.DisplayMySqlErrorCollection(oe);
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "Error in GetWeaponMappingfromDB: " + c);
            }

            return mappingDic;
        }
    }
}
