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
using System.Data;
using System.Threading;

using Dapper;

using MySqlConnector;

using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;

namespace PRoConEvents
{
    public partial class CChatGUIDStatsLogger
    {
        #region External Commands (ColColonCleaner)

        public void GetStatus(params String[] commands)
        {
            this.DebugInfo("Info", "GetStatus starting!");
            if (commands.Length < 1)
            {
                this.DebugInfo("Error", "Status fetch request canceled, no parameters provided.");
                return;
            }

            new Thread(new ParameterizedThreadStart(SendStatus)).Start(commands[0]);
            this.DebugInfo("Info", "GetStatus finished!");
        }

        private void SendStatus(Object clientInformation)
        {
            this.DebugInfo("Info", "SendStatus starting!");
            try
            {
                //Set current thread name
                Thread.CurrentThread.Name = "SendStatus";

                //Parse client plugin information
                Hashtable parsedClientInformation = (Hashtable)JSON.JsonDecode((String)clientInformation);
                String pluginName = String.Empty;
                String pluginMethod = String.Empty;
                if (!parsedClientInformation.ContainsKey("pluginName"))
                {
                    this.DebugInfo("Error", "Parsed command didn't contain a pluginName!");
                    return;
                }
                else
                {
                    pluginName = (String)parsedClientInformation["pluginName"];
                }

                if (!parsedClientInformation.ContainsKey("pluginMethod"))
                {
                    this.DebugInfo("Error", "Parsed command didn't contain a pluginMethod!");
                    return;
                }
                else
                {
                    pluginMethod = (String)parsedClientInformation["pluginMethod"];
                }

                //Check for active connection to the database using a simple query
                Boolean activeConnection = false;
                this.tablebuilder();
                if ((m_strHost != null) || (m_strDatabase != null) || (m_strDBPort != null) || (m_strUserName != null) || (m_strPassword != null))
                {
                    try
                    {
                        using (MySqlConnection Connection = new MySqlConnection(this.DBConnectionStringBuilder()))
                        {
                            Connection.Open();
                            if (Connection.State == ConnectionState.Open)
                            {
                                String query = "SELECT `ServerID` from `" + this.tbl_server + "` LIMIT 1";
                                DataTable resultTable = this.SQLquery(query);
                                if (resultTable.Rows != null)
                                {
                                    activeConnection = true;
                                }
                            }
                            //Connection automatically closed by end of 'using' clause
                        }
                    }
                    catch (Exception e)
                    {
                        this.DebugInfo("Error", "Query could not be performed while sending plugin status.");
                    }
                }

                //Create response hashtable
                Hashtable response = new Hashtable();

                //Add Plugin General Settings
                response["pluginVersion"] = this.GetPluginVersion();
                response["pluginEnabled"] = this.m_isPluginEnabled.ToString();
                //Add Database connection info, without username and password
                response["DBHost"] = this.m_strHost;
                response["DBPort"] = this.m_strDBPort;
                response["DBName"] = this.m_strDatabase;
                //Add Database time offset
                response["DBTimeOffset"] = this.m_dTimeOffset.ToString();
                //Add Whether the connection is active
                response["DBConnectionActive"] = activeConnection.ToString();
                //Add Specific logging settings
                response["ChatloggingEnabled"] = (this.m_enChatloggingON == enumBoolYesNo.Yes).ToString();
                response["InstantChatLoggingEnabled"] = (this.m_enInstantChatlogging == enumBoolYesNo.Yes).ToString();
                response["StatsLoggingEnabled"] = (this.m_enLogSTATS == enumBoolYesNo.Yes).ToString();
                response["DBliveScoreboardEnabled"] = (this.m_enableCurrentPlayerstatsTable == enumBoolYesNo.Yes).ToString();
                //Add Plugin Debug Mode
                response["DebugMode"] = this.GlobalDebugMode;
                //Add Error as "no error"
                response["Error"] = false.ToString();

                //Encode JSON response
                String JSONResponse = JSON.JsonEncode(response);

                //Send the response
                this.ExecuteCommand("procon.protected.plugins.call", pluginName, pluginMethod, JSONResponse);
            }
            catch (Exception e)
            {
                //Log the error in console
                this.DebugInfo("Error", e.ToString());
            }

            this.DebugInfo("Info", "SendStatus finished!");
        }

        #endregion

        #region In Game Commands

        public void OnCommandStats(String strSpeaker, String strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            /*
            this.DebugInfo("Trace", "MatchCommand:" + mtcCommand.Command);
            this.DebugInfo("Trace", "CapturedCommand:" + capCommand.Command);
            this.DebugInfo("Trace", "CapturedCommand:" + capCommand.ResposeScope);
            this.DebugInfo("Trace", "CapturedCommand:" + capCommand.ExtraArguments);
            */
            if ((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
            {
                String scope = String.Empty;
                if (capCommand.ResposeScope.Contains("!") == true)
                {
                    if (this.m_enSendStatsToAll == enumBoolYesNo.Yes)
                    {
                        scope = "all";
                    }
                    else
                    {
                        scope = "player";
                    }
                }
                else
                {
                    scope = "player";
                }

                if (capCommand.ExtraArguments.Length > 0)
                {
                    ThreadPool.QueueUserWorkItem(delegate { this.GetWeaponStats(this.FindKeyword(capCommand.ExtraArguments.Trim().ToUpper()), strSpeaker, scope); });
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(delegate { this.GetPlayerStats(strSpeaker, 0, scope); });
                }
            }
        }

        public void OnCommandTop10(String strSpeaker, String strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if ((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
            {
                String scope = String.Empty;
                if (capCommand.ResposeScope.Contains("!") == true)
                {
                    if (this.m_enSendStatsToAll == enumBoolYesNo.Yes)
                    {
                        scope = "all";
                    }
                    else
                    {
                        scope = "player";
                    }
                }
                else
                {
                    scope = "player";
                }

                if (capCommand.ExtraArguments.Length > 0)
                {
                    ThreadPool.QueueUserWorkItem(delegate { this.GetWeaponTop10(this.FindKeyword(capCommand.ExtraArguments.Trim().ToUpper()), strSpeaker, 2, scope); });
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(delegate { this.GetTop10(strSpeaker, 2, scope); });
                }
            }
        }

        public void OnCommandDogtags(String strSpeaker, String strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if ((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
            {
                String scope = String.Empty;
                if (capCommand.ResposeScope.Contains("!") == true)
                {
                    if (this.m_enSendStatsToAll == enumBoolYesNo.Yes)
                    {
                        scope = "all";
                    }
                    else
                    {
                        scope = "player";
                    }
                }
                else
                {
                    scope = "player";
                }
                ThreadPool.QueueUserWorkItem(delegate { this.GetDogtags(strSpeaker, 1, scope); });
            }
        }

        public void OnCommandSession(String strSpeaker, String strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if ((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
            {
                String scope = String.Empty;
                if (capCommand.ResposeScope.Contains("!") == true)
                {
                    if (this.m_enSendStatsToAll == enumBoolYesNo.Yes)
                    {
                        scope = "all";
                    }
                    else
                    {
                        scope = "player";
                    }
                }
                else
                {
                    scope = "player";
                }
                ThreadPool.QueueUserWorkItem(delegate { this.GetSession(strSpeaker, 1, scope); });
            }
        }

        public void OnCommandServerStats(String strSpeaker, String strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if ((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
            {
                String scope = String.Empty;
                if (capCommand.ResposeScope.Contains("!") == true)
                {
                    if (this.m_enSendStatsToAll == enumBoolYesNo.Yes)
                    {
                        scope = "all";
                    }
                    else
                    {
                        scope = "player";
                    }
                }
                else
                {
                    scope = "player";
                }
                ThreadPool.QueueUserWorkItem(delegate { this.GetServerStats(strSpeaker, 1, scope); });
            }
        }

        public void OnCommandPlayerOfTheDay(String strSpeaker, String strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if ((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
            {
                String scope = String.Empty;
                if (capCommand.ResposeScope.Contains("!") == true)
                {
                    if (this.m_enSendStatsToAll == enumBoolYesNo.Yes)
                    {
                        scope = "all";
                    }
                    else
                    {
                        scope = "player";
                    }
                }
                else
                {
                    scope = "player";
                }
                ThreadPool.QueueUserWorkItem(delegate { this.GetPlayerOfTheDay(strSpeaker, 1, scope); });
            }
        }

        public void OnCommandTop10ForPeriod(String strSpeaker, String strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if ((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
            {
                String scope = String.Empty;
                if (capCommand.ResposeScope.Contains("!") == true)
                {
                    if (this.m_enSendStatsToAll == enumBoolYesNo.Yes)
                    {
                        scope = "all";
                    }
                    else
                    {
                        scope = "player";
                    }
                }
                else
                {
                    scope = "player";
                }
                ThreadPool.QueueUserWorkItem(delegate { this.GetTop10ForPeriod(strSpeaker, 2, scope, this.m_intDaysForPeriodTop10); });
            }
        }

        #endregion
    }
}
