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
using System.Text.RegularExpressions;
using System.Threading;

using MySqlConnector;

namespace PRoConEvents
{
    public partial class CChatGUIDStatsLogger
    {
        private void LogChat(string strSpeaker, string strMessage, string strType)
        {
            try
            {
                if (this.m_enChatloggingON == enumBoolYesNo.No)
                {
                    return;
                }
                if (this.m_enNoServerMsg == enumBoolYesNo.No && strSpeaker.CompareTo("Server") == 0)
                {
                    return;
                }
                if (this.m_enChatlogFilter == enumBoolYesNo.Yes)
                {
                    //Filter Messages
                    foreach (Regex FilterRule in this.lstChatFilterRules)
                    {
                        if (FilterRule.IsMatch(strMessage))
                        {
                            //dont log
                            this.DebugInfo("Trace", "Chatmessage: '" + strMessage + "' was filtered out by the Regex rule: " + FilterRule.ToString());
                            return;
                        }
                    }
                }
                if (m_enInstantChatlogging == enumBoolYesNo.Yes)
                {
                    string query = "INSERT INTO " + this.tbl_chatlog + @" (logDate, ServerID, logSubset, logSoldierName, logMessage) VALUES (@logDate, @ServerID, @logSubset, @logSoldierName, @logMessage)";
                    this.tablebuilder();
                    if ((m_strHost != null) || (m_strDatabase != null) || (m_strDBPort != null) || (m_strUserName != null) || (m_strPassword != null))
                    {
                        if (this.m_highPerformanceConnectionMode == enumBoolOnOff.On)
                        {
                            try
                            {
                                using (MySqlConnection Connection = new MySqlConnection(this.DBConnectionStringBuilder()))
                                {
                                    Connection.Open();
                                    if (Connection.State == ConnectionState.Open)
                                    {
                                        using (MySqlCommand OdbcCom = new MySqlCommand(query, Connection))
                                        {
                                            OdbcCom.Parameters.AddWithValue("@logDate", MyDateTime.Now);
                                            OdbcCom.Parameters.AddWithValue("@ServerID", this.ServerID);
                                            OdbcCom.Parameters.AddWithValue("@logSubset", strType);
                                            OdbcCom.Parameters.AddWithValue("@logSoldierName", strSpeaker);
                                            OdbcCom.Parameters.AddWithValue("@logMessage", strMessage);
                                            OdbcCom.ExecuteNonQuery();
                                        }
                                    }
                                    Connection.Close();
                                }
                            }
                            catch (MySqlException oe)
                            {
                                this.DebugInfo("Error", "LogChat: ");
                                this.DisplayMySqlErrorCollection(oe);
                            }
                            catch (Exception c)
                            {
                                this.DebugInfo("Error", "LogChat: " + c);
                            }
                        }
                        else
                        {
                            lock (this.chatloglock)
                            {
                                try
                                {
                                    if (this.MySqlChatCon == null)
                                    {
                                        this.MySqlChatCon = new MySqlConnection(this.DBConnectionStringBuilder());
                                    }
                                    if (MySqlChatCon.State != ConnectionState.Open)
                                    {
                                        this.MySqlChatCon.Open();
                                    }
                                    if (MySqlChatCon.State == ConnectionState.Open)
                                    {
                                        using (MySqlCommand OdbcCom = new MySqlCommand(query, MySqlChatCon))
                                        {
                                            OdbcCom.Parameters.AddWithValue("@logDate", MyDateTime.Now);
                                            OdbcCom.Parameters.AddWithValue("@ServerID", this.ServerID);
                                            OdbcCom.Parameters.AddWithValue("@logSubset", strType);
                                            OdbcCom.Parameters.AddWithValue("@logSoldierName", strSpeaker);
                                            OdbcCom.Parameters.AddWithValue("@logMessage", strMessage);
                                            OdbcCom.ExecuteNonQuery();
                                        }
                                    }
                                }
                                catch (MySqlException oe)
                                {
                                    this.DebugInfo("Error", "LogChat: ");
                                    this.DisplayMySqlErrorCollection(oe);
                                    try
                                    {
                                        if (MySqlChatCon.State == ConnectionState.Open)
                                        {
                                            MySqlChatCon.Dispose();
                                        }
                                    }
                                    catch { }
                                }
                                catch (Exception c)
                                {
                                    this.DebugInfo("Error", "LogChat: " + c);
                                    try
                                    {
                                        if (MySqlChatCon.State == ConnectionState.Open)
                                        {
                                            MySqlChatCon.Close();
                                        }
                                    }
                                    catch { }
                                }
                                finally
                                {
                                    try
                                    {
                                        if (MySqlChatCon != null)
                                        {
                                            MySqlChatCon.Close();
                                        }
                                    }
                                    catch { }

                                }
                            }
                        }
                    }
                }
                else
                {
                    CLogger chat = new CLogger(MyDateTime.Now, strSpeaker, strMessage, strType);
                    ChatLog.Add(chat);
                }
            }
            catch (Exception c)
            {
                this.DebugInfo("Error", "LogChat_2: " + c);
            }
        }
    }
}
