/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2008-2014, www.metabolt.net (METAbolt)
 * Copyright(c) 2021, Sjofn, LLC
 * All rights reserved.
 *  
 * Radegast is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.If not, see<https://www.gnu.org/licenses/>.
 */


using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenMetaverse;
using MEGAcrypto;
using System.Timers;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;
using MEGAbolt.NetworkComm;

namespace MEGAbolt
{
    public class ChatTextManager
    {
        private MEGAboltInstance instance;
        private MEGAboltNetcom netcom;
        private GridClient client;
        //private frmMain mainForm;

        //private List<ChatBufferItem> textBuffer;

        private bool showTimestamps;

        //added by GM on 3-JUL-2009 for chair group IM calling
        private string gmu = string.Empty;
        private string imu = string.Empty;
        private UUID cau = UUID.Zero;
        private int chairAnnouncerInterval;
        private int chairAnnouncerActives;
        private UUID[] chairAnnouncerGroups;
        private string[] chairAnnouncerGroupNames;
        private int indexGroup;
        private DateTime nextCallTime;
        private int targetIndex;
        private ManualResetEvent waitGroupIMSession = new ManualResetEvent(false);
        private ManualResetEvent waitGroupIMLeaveSession = new ManualResetEvent(false);
        private bool chairAnnEnabled = false;
        private bool chairAnnChat = true;
        //added by GM on 1-APR-2010
        private string chairAnnAdvert;

        private UUID GroupRequestID;
        //private ManualResetEvent GroupsEvent = new ManualResetEvent(false);
        private UUID irole = UUID.Zero;
        private List<UUID> roles = null;
        UUID igroup = UUID.Zero;
        UUID iperson = UUID.Zero;
        private string gavname = string.Empty;
        private string gmanlocation = string.Empty;
        //private GroupManager.GroupMembersCallback callback;
        private bool ChatLogin = true;

        private string commandin = string.Empty;
        //private bool TEnabled = false;
        //private string tName = string.Empty;
        //private string tPwd = string.Empty;
        //private bool tweet = true;
        //private string tweetname = string.Empty;
        private string lastspeaker = string.Empty;
        private System.Timers.Timer aTimer;
        private bool ismember = false;
        private int invitecounter = 0;
        private bool classiclayout = false;
        private bool reprinting = false;
        private RingBufferProtection scriptbuffer = new RingBufferProtection();
        private RingBufferProtection urlbuffer = new RingBufferProtection();

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                if (!String.IsNullOrEmpty(Generated.BugsplatDatabase))
                {
                    BugSplat crashReporter = new BugSplat(Generated.BugsplatDatabase, "MEGAbolt",
                        Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                    {
                        User = Generated.BugsplatUser,
                        ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                    };
                    crashReporter.Post(e.Exception);
                }
            }
        }

        public ChatTextManager(MEGAboltInstance instance, ITextPrinter textPrinter)
        {
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            TextPrinter = textPrinter;
            //this.textBuffer = new List<ChatBufferItem>();

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            AddNetcomEvents();

            //TEnabled = this.instance.Config.CurrentConfig.EnableTweeter;
            //tName = this.instance.Config.CurrentConfig.TweeterName;
            //tPwd = this.instance.Config.CurrentConfig.TweeterPwd;
            //tweet = this.instance.Config.CurrentConfig.Tweet;
            //tweetname = this.instance.Config.CurrentConfig.TweeterUser;
            classiclayout = this.instance.Config.CurrentConfig.ClassicChatLayout;

            //added by GM on 2-JUL-2009
            gmu = this.instance.Config.CurrentConfig.GroupManagerUID;
            imu = this.instance.Config.CurrentConfig.IgnoreUID;
            commandin = this.instance.Config.CurrentConfig.CommandInID;

            cau = this.instance.Config.CurrentConfig.ChairAnnouncerUUID;
            chairAnnouncerInterval = this.instance.Config.CurrentConfig.ChairAnnouncerInterval;
            chairAnnEnabled = this.instance.Config.CurrentConfig.ChairAnnouncerEnabled;
            chairAnnChat = this.instance.Config.CurrentConfig.ChairAnnouncerChat;
            chairAnnouncerGroups = new UUID[6];
            chairAnnouncerGroupNames = new string[6]; //filled as joined
            chairAnnouncerGroups[0] = this.instance.Config.CurrentConfig.ChairAnnouncerGroup1;
            chairAnnouncerGroups[1] = this.instance.Config.CurrentConfig.ChairAnnouncerGroup2;
            chairAnnouncerGroups[2] = this.instance.Config.CurrentConfig.ChairAnnouncerGroup3;
            chairAnnouncerGroups[3] = this.instance.Config.CurrentConfig.ChairAnnouncerGroup4;
            chairAnnouncerGroups[4] = this.instance.Config.CurrentConfig.ChairAnnouncerGroup5;
            chairAnnouncerGroups[5] = this.instance.Config.CurrentConfig.ChairAnnouncerGroup6;
            CountActives();
            indexGroup = 0;
            nextCallTime = DateTime.Now;
            //added by GM on 1-APR-2010
            chairAnnAdvert = this.instance.Config.CurrentConfig.ChairAnnouncerAdvert;

            client.Groups.GroupMembersReply += GroupMembersHandler;


            showTimestamps = this.instance.Config.CurrentConfig.ChatTimestamps;
            this.instance.Config.ConfigApplied += Config_ConfigApplied;

            scriptbuffer.SetBuffer(instance.Config.CurrentConfig.ScriptUrlBufferLimit);
            urlbuffer.SetBuffer(instance.Config.CurrentConfig.ScriptUrlBufferLimit);
            instance.chatbuffer.SetBuffer(instance.Config.CurrentConfig.ChatBufferLimit);
        }

        private void CountActives()
        {
            chairAnnouncerActives = 0;
            foreach (UUID cag in chairAnnouncerGroups)
            {
                if (cag != UUID.Zero) chairAnnouncerActives++;
            }
        }

        private void Config_ConfigApplied(object sender, ConfigAppliedEventArgs e)
        {
            showTimestamps = e.AppliedConfig.ChatTimestamps;

            classiclayout = instance.Config.CurrentConfig.ClassicChatLayout;

            //TEnabled = e.AppliedConfig.EnableTweeter;
            //tName = e.AppliedConfig.TweeterName;
            //tPwd = e.AppliedConfig.TweeterPwd;
            //tweet = this.instance.Config.CurrentConfig.Tweet;
            //tweetname = this.instance.Config.CurrentConfig.TweeterUser;

            //update with new config
            gmu = e.AppliedConfig.GroupManagerUID;
            cau = e.AppliedConfig.ChairAnnouncerUUID;
            chairAnnouncerInterval = e.AppliedConfig.ChairAnnouncerInterval;
            chairAnnEnabled = e.AppliedConfig.ChairAnnouncerEnabled;
            chairAnnChat = e.AppliedConfig.ChairAnnouncerChat;
            chairAnnouncerGroups[0] = e.AppliedConfig.ChairAnnouncerGroup1;
            chairAnnouncerGroups[1] = e.AppliedConfig.ChairAnnouncerGroup2;
            chairAnnouncerGroups[2] = e.AppliedConfig.ChairAnnouncerGroup3;
            chairAnnouncerGroups[3] = e.AppliedConfig.ChairAnnouncerGroup4;
            chairAnnouncerGroups[4] = e.AppliedConfig.ChairAnnouncerGroup5;
            chairAnnouncerGroups[5] = e.AppliedConfig.ChairAnnouncerGroup6;
            CountActives();

            if (instance.Config.CurrentConfig.BufferApplied)
            {
                //ReprintAllText();
                CheckBufferSize();
                instance.Config.CurrentConfig.BufferApplied = false;
            }

            scriptbuffer.SetBuffer(instance.Config.CurrentConfig.ScriptUrlBufferLimit);
            urlbuffer.SetBuffer(instance.Config.CurrentConfig.ScriptUrlBufferLimit);
            instance.chatbuffer.SetBuffer(instance.Config.CurrentConfig.ChatBufferLimit);
        }

        private void AddNetcomEvents()
        {
            netcom.ClientLoginStatus += netcom_ClientLoginStatus;
            netcom.ClientLoggedOut += netcom_ClientLoggedOut;
            netcom.ClientDisconnected += netcom_ClientDisconnected;
            netcom.ChatReceived += netcom_ChatReceived;
            netcom.ScriptDialogReceived += netcom_ScriptDialogReceived;
            netcom.LoadURLReceived += netcom_LoadURLReceived;
            netcom.ScriptQuestionReceived += netcom_ScriptQuestionReceived;
            netcom.ChatSent += netcom_ChatSent;
            netcom.AlertMessageReceived += netcom_AlertMessageReceived;
        }

        private void RemoveNetcomEvents()
        {
            netcom.ClientLoginStatus -= netcom_ClientLoginStatus;
            netcom.ClientLoggedOut -= netcom_ClientLoggedOut;
            netcom.ClientDisconnected -= netcom_ClientDisconnected;
            netcom.ChatReceived -= netcom_ChatReceived;
            netcom.ScriptDialogReceived -= netcom_ScriptDialogReceived;
            netcom.LoadURLReceived -= netcom_LoadURLReceived;
            netcom.ScriptQuestionReceived -= netcom_ScriptQuestionReceived;
            netcom.ChatSent -= netcom_ChatSent;
            netcom.AlertMessageReceived -= netcom_AlertMessageReceived;

            client.Groups.GroupMembersReply -= GroupMembersHandler;
            instance.Config.ConfigApplied -= Config_ConfigApplied;
        }

        private void netcom_ChatSent(object sender, ChatSentEventArgs e)
        {
            if (e.Channel == 0) return;

            ProcessOutgoingChat(e);
        }

        private void netcom_ClientLoginStatus(object sender, LoginProgressEventArgs e)
        {
            if (e.Status == LoginStatus.Success)
            {
                ChatBufferItem loggedIn = new ChatBufferItem(
                    DateTime.Now,
                    " Logged into Second Life as " + netcom.LoginOptions.FullName + ".",
                    ChatBufferTextStyle.StatusBlue);

                ChatBufferItem loginReply = new ChatBufferItem(
                    DateTime.Now, Environment.NewLine + e.Message, ChatBufferTextStyle.LoginReply);

                string avid = client.Self.AgentID.ToString();

                ChatBufferItem avuuid = new ChatBufferItem(
                    DateTime.Now, " " + netcom.LoginOptions.FullName + "'s UUID is " + avid + " " + Environment.NewLine, ChatBufferTextStyle.StatusBlue);

                //ChatBufferItem avuuid1 = new ChatBufferItem(
                //    DateTime.Now, " Waiting for avatar to rezz... ", ChatBufferTextStyle.Alert);

                ProcessBufferItem(loggedIn, true);
                ProcessBufferItem(avuuid, true);
                ProcessBufferItem(loginReply, true);
                //ProcessBufferItem(avuuid1, true);
            }
            else if (e.Status == LoginStatus.Failed)
            {
                ChatBufferItem loginError = new ChatBufferItem(
                    DateTime.Now, " Login error: " + e.Message, ChatBufferTextStyle.Error);

                ProcessBufferItem(loginError, true);
            }
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            ChatBufferItem item = new ChatBufferItem(
                DateTime.Now, " Logged out of Second Life.\n", ChatBufferTextStyle.StatusBlue);

            ProcessBufferItem(item, true);

            RemoveNetcomEvents();
        }

        private void netcom_AlertMessageReceived(object sender, AlertMessageEventArgs e)
        {
            if (e.Message.ToLower(CultureInfo.CurrentCulture).Contains("autopilot canceled")) return; //workaround the stupid autopilot alerts

            string emsg = e.Message.Trim();

            if (emsg.Contains("RESTART_X_MINUTES"))
            {
                string[] mins = emsg.Split(new Char[] { ' ' });
                emsg = "Region is restarting in " + mins[1].Trim() + " minutes. If you remain in this region you will be logged out.";
                netcom.ChatOut(emsg, ChatType.Normal, 123456);
            }

            ChatBufferItem item = new ChatBufferItem(
                DateTime.Now, " Alert message: " + emsg, ChatBufferTextStyle.Alert);

            ProcessBufferItem(item, true);
        }

        private void netcom_ClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            if (e.Reason == NetworkManager.DisconnectType.ClientInitiated) return;

            ChatBufferItem item = new ChatBufferItem(
                DateTime.Now, " Client disconnected. Message: " + e.Message, ChatBufferTextStyle.Error);

            ProcessBufferItem(item, true);

            RemoveNetcomEvents();
        }

        private void netcom_ChatReceived(object sender, ChatEventArgs e)
        {
            ProcessIncomingChat(e);
        }

        private void netcom_ScriptDialogReceived(object sender, ScriptDialogEventArgs e)
        {
            if (instance.IsObjectMuted(e.ObjectID, e.ObjectName))
                return;

            if (string.IsNullOrEmpty(e.Message)) return;

            // Count the ones already on display
            // to avoid flood attacks

            if (instance.DialogCount < 9)
            {
                instance.DialogCount += 1;
            }

            if (instance.DialogCount < 9)
            {
                (new frmDialogLoadURL(instance, e)).ShowDialog(instance.MainForm);

                if (instance.DialogCount == 8)
                {
                    UUID objID = e.ObjectID;
                    string objname = e.ObjectName;

                    string objinfo = "\nObject UUID: " + objID;
                    objinfo += "\nObject Name: " + objname;

                    PrintDialogWarning(e.FirstName + " " + e.LastName, objinfo);
                }
            }
        }

        public void PrintDialogWarning(string avname, string oinfo)
        {
            ChatBufferItem dalert = new ChatBufferItem(
                DateTime.Now, " There is a total of 8 dialogs open. All new dialogs are now being blocked for your security. Close currently open dialogs to resume normal conditions.", ChatBufferTextStyle.Alert);

            ChatBufferItem warn1 = new ChatBufferItem(
                DateTime.Now, " Last dialog was from " + avname + ". Object details below..." + oinfo, ChatBufferTextStyle.Alert);

            ProcessBufferItem(dalert, false);
            ProcessBufferItem(warn1, false);
        }

        private void netcom_ScriptQuestionReceived(object sender, ScriptQuestionEventArgs e)
        {
            if (instance.IsObjectMuted(e.ItemID, e.ObjectName))
                return;

            if (scriptbuffer.RingBuffer(instance))
            {
                ChatBufferItem dalert = new ChatBufferItem(
                DateTime.Now, "Too many script dialogues are coming in too quickly. Flood Protection is in operation for your security...", ChatBufferTextStyle.Alert);

                ProcessBufferItem(dalert, true);

                return;
            }

            //e.ObjectName.ToString();
            //e.ObjectOwner.ToString();
            //e.Questions.ToString();

            ScriptPermission scriptPerm = ScriptPermission.None;
            string scriptMsg = string.Empty;

            switch (e.Questions)
            {
                case ScriptPermission.Attach:
                    scriptPerm = ScriptPermission.Attach;
                    scriptMsg = "Wants permission to ATTACH.";
                    break;

                case ScriptPermission.Debit:
                    scriptPerm = ScriptPermission.Debit;
                    scriptMsg = "Wants permission to DEBIT.";
                    break;

                case ScriptPermission.TakeControls:
                    scriptPerm = ScriptPermission.TakeControls;
                    scriptMsg = "Wants permission to TAKE CONTROLS.";
                    break;

                case ScriptPermission.TriggerAnimation:
                    scriptPerm = ScriptPermission.TriggerAnimation;
                    scriptMsg = "Wants permission to TRIGGER ANIMATION.";
                    break;

                case ScriptPermission.Teleport:
                    scriptPerm = ScriptPermission.Teleport;
                    scriptMsg = "Wants permission to TELEPORT.";
                    break;
            }
            
            DialogResult sret = MessageBoxEx.Show(
                $"{e.ObjectName.ToString(CultureInfo.CurrentCulture)}\nowned by {e.ObjectOwnerName}:\n\n{scriptMsg}",
                "Script permission...", MessageBoxButtons.OKCancel, 15000);


            client.Self.ScriptQuestionReply(client.Network.CurrentSim, e.ItemID, e.TaskID,
                sret == DialogResult.OK ? scriptPerm : ScriptPermission.None);
        }

        private void netcom_LoadURLReceived(object sender, LoadUrlEventArgs e)
        {
            //e.Message;
            //e.ObjectName;
            //e.url;

            if (instance.IsObjectMuted(e.ObjectID, e.ObjectName))
                return;

            if (urlbuffer.RingBuffer(instance))
            {
                ChatBufferItem dalert = new ChatBufferItem(
                DateTime.Now, "Too many URL offers are coming in too quickly. Flood Protection is in operation for your security...", ChatBufferTextStyle.Alert);

                ProcessBufferItem(dalert, true);

                return;
            }

            DialogResult sret = MessageBoxEx.Show(
                $"{e.ObjectName.ToString(CultureInfo.CurrentCulture)}\nowned by {e.OwnerID} is offering you a URL.\n\nClick 'OK' to visit.", "URL offer...", MessageBoxButtons.OKCancel, 15000);

            if (sret == DialogResult.OK)
            {
                //ShellExecute(this.Handle, "open", e.url.ToString(), null, null, 0);
                Utilities.OpenBrowser(@e.URL.ToString(CultureInfo.CurrentCulture));
            }
        }

        public void PrintStartupMessage()
        {
            ChatBufferItem title = new ChatBufferItem(
                DateTime.Now, $"{Assembly.GetExecutingAssembly().GetName().Name} " +
                              $"v{Assembly.GetExecutingAssembly().GetName().Version}" + Environment.NewLine, 
                ChatBufferTextStyle.StartupTitle);

            ChatBufferItem ready = new ChatBufferItem(
                DateTime.Now, " Ready to login...\n", ChatBufferTextStyle.StatusBlue);

            ProcessBufferItem(title, true);
            ProcessBufferItem(ready, true);
        }

        public void PrintAlertMessage(string msg)
        {
            ChatBufferItem ready = new ChatBufferItem(
                DateTime.Now, msg, ChatBufferTextStyle.Alert);

            ProcessBufferItem(ready, true);
        }

        public void PrintUUID()
        {
            string avid = client.Self.AgentID.ToString();

            ChatBufferItem avuuid = new ChatBufferItem(
                DateTime.Now, $" My UUID is {avid}", ChatBufferTextStyle.Alert);

            ProcessBufferItem(avuuid, true);
        }

        public void PrintMsg(string Msg)
        {
            //textPrinter.SetSelectionForeColor(Color.Brown);
            //textPrinter.PrintText(Msg);

            ChatBufferItem ready = new ChatBufferItem(
               DateTime.Now, Msg, ChatBufferTextStyle.StatusBrown);

            ProcessBufferItem(ready, true);
        }

        private void CheckBufferSize()
        {
            //int lines = textBuffer.Count;
            //int maxlines = this.instance.Config.CurrentConfig.lineMax;

            //if (maxlines == 0) return;

            ////if (lines > maxlines)
            ////{
            ////    textBuffer.RemoveAt(0);

            ////    ReprintAllText();
            ////}

            //if (lines > maxlines)
            //{
            //    int lineno = maxlines / 2;

            //    for (int a = 0; a < lineno; a++)
            //    {
            //        textBuffer.RemoveAt(a);
            //    }

            //    reprinting = true;

            //    ReprintAllText();
            //}
        }

        public void ProcessBufferItem(ChatBufferItem item, bool addToBuffer)
        {
            try
            {
                if (instance.IsAvatarMuted(item.FromUUID, item.FromName))
                    return;
            }
            catch
            {
                ;
            }

            string smsg = item.Text;
            string prefix = string.Empty;

            //if (addToBuffer)
            //{
            //    textBuffer.Add(item);

            //    //int lines = textBuffer.Count;
            //    //int maxlines = this.instance.Config.CurrentConfig.lineMax;

            //    //if (lines > maxlines && maxlines > 0)
            //    //{
            //    //    CheckBufferSize();
            //    //    return;
            //    //}
            //}

            DateTime dte = item.Timestamp;

            if (classiclayout)
            {
                if (showTimestamps)
                {
                    dte = instance.State.GetTimeStamp(dte);
                    //if (instance.Config.CurrentConfig.UseSLT)
                    //{
                    //    string _timeZoneId = "Pacific Standard Time";
                    //    DateTime startTime = DateTime.UtcNow;
                    //    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                    //    dte = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
                    //}

                    TextPrinter.SetSelectionForeColor(Color.Gray);

                    if (item.Style is ChatBufferTextStyle.StatusDarkBlue or ChatBufferTextStyle.Alert)
                    {
                        //textPrinter.PrintText("\n" + dte.ToString("[HH:mm] "));
                        prefix = "\n" + dte.ToString("[HH:mm] ", CultureInfo.CurrentCulture);
                    }
                    else
                    {
                        if (item.Style != ChatBufferTextStyle.StartupTitle)
                        {
                            //textPrinter.PrintText(dte.ToString("[HH:mm] "));
                            prefix = dte.ToString("[HH:mm] ", CultureInfo.CurrentCulture);
                        }
                    }
                }
                else
                {
                    prefix = string.Empty;
                }
            }
            else
            {

                try
                {
                    if (lastspeaker != item.FromName)
                    {
                        //textPrinter.SetFontStyle(FontStyle.Bold);
                        TextPrinter.PrintHeader(item.FromName);   // + buff);

                        lastspeaker = item.FromName;
                    }
                }
                catch
                {
                    ;
                }

                TextPrinter.SetFontStyle(FontStyle.Regular);
                TextPrinter.SetSelectionBackColor(Color.White);
                //textPrinter.SetSelectionBackColor(instance.Config.CurrentConfig.BgColour);

                if (showTimestamps)
                {
                    if (item.Style != ChatBufferTextStyle.StartupTitle)
                    {
                        dte = instance.State.GetTimeStamp(dte);

                        //if (instance.Config.CurrentConfig.UseSLT)
                        //{
                        //    string _timeZoneId = "Pacific Standard Time";
                        //    DateTime startTime = DateTime.UtcNow;
                        //    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                        //    dte = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
                        //}

                        //textPrinter.ForeColor = Color.Gray;

                        if (item.Style is ChatBufferTextStyle.StatusDarkBlue or ChatBufferTextStyle.Alert)
                        {
                            prefix = "\n" + dte.ToString("[HH:mm] ", CultureInfo.CurrentCulture);
                            //prefix = dte.ToString("[HH:mm] ");
                        }
                        else
                        {
                            // if (item.FromName == client.Self.Name)   //FirstName + " " + client.Self.LastName)
                            //  {
                            prefix = dte.ToString("[HH:mm] ", CultureInfo.CurrentCulture);
                            //  }
                            //  else
                            //   {
                            //        prefix = dte.ToString("[HH:mm] ", CultureInfo.CurrentCulture);
                            //   }
                        }
                    }
                }
                else
                {
                    prefix = string.Empty;
                }
            }

            // Check to see if it is an in-world plugin first
            //current possibilities are GroupManager or ChairAnnouncer
            if (smsg.Contains(gmu))
            {
                roles = new List<UUID>();

                char[] deli = ",".ToCharArray();
                string[] sGrp = smsg.Split(deli);

                try //trap the cast to UUID bug if passed string is malformed
                {
                    igroup = (UUID)sGrp[2];
                    iperson = (UUID)sGrp[1];
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message, Helpers.LogLevel.Error);
                    return;
                }

                int ielems = sGrp.Length;

                // This is a hack for backward GroupMan Pro compatibility
                if (ielems > 6)
                {
                    gavname = sGrp[5];
                    gmanlocation = sGrp[6];
                }
                else
                {
                    gavname = string.Empty;
                    gmanlocation = string.Empty;
                }

                string pwd = string.Empty;

                try
                {
                    pwd = sGrp[3];
                }
                catch
                {
                    pwd = "invalid";
                }

                // the MD5 stuff (as of V 0.9.3)
                string str = instance.Config.CurrentConfig.GroupManPro;
                MEGAMD5 md5 = new MEGAMD5();
                string metapwd = md5.MD5(str);

                if (pwd != metapwd)
                {
                    string gmsg = string.Empty;

                    if (pwd == "invalid")
                    {
                        gmsg = "IMPORTANT WARNING: A group invite could not be sent out. Your GroupMan Pro is out of date.";
                    }
                    else
                    {
                        gmsg = "IMPORTANT WARNING: A group invite request with an invalid password has been received. Either the passwords in MEGAbolt & GroupMan Pro don't match or someone is trying to get unauthorised access. This is for information purposes only. DO NOT PANIC. The request has been discarded.  \n\nRequest received from: " + item.FromName + " (" + item.FromUUID + ")\nThe password used is: " + pwd + ".\nThe requested invite is for group: " + igroup;
                    }

                    TextPrinter.PrintTextLine(gmsg);
                    return;
                }

                //roles.Add("00000000-0000-0000-0000-000000000000");

                // Check if being invited to a specific role
                if (ielems > 4)
                {
                    try //trap the cast to UUID bug if passed string is malformed 
                    {
                        irole = (UUID)sGrp[4].Trim();
                    }
                    catch {; }

                    roles.Add(irole);
                }
                else
                {
                    roles.Add(UUID.Zero);
                }

                try
                {
                    TextPrinter.SetSelectionForeColor(Color.DarkCyan);

                    if (instance.State.Groups.ContainsKey(igroup))
                    {
                        // check if already member of the group
                        client.Groups.GroupMembersReply += GroupMembersHandler;
                        GroupRequestID = client.Groups.RequestGroupMembers(igroup);
                    }

                    return;
                }
                catch (Exception excp)
                {
                    TextPrinter.PrintTextLine(String.Format(CultureInfo.CurrentCulture, "\n(GroupMan Pro @ " + gmanlocation + ")\nGroupMan Pro has encountered an error and a group invite could not be sent to: " + gavname));
                    Logger.Log(String.Format(CultureInfo.CurrentCulture, "GroupMan Pro Error: {0}", excp), Helpers.LogLevel.Error);
                    return;
                }
            }
            //added by GM on 2-JUL-2009
            else if (item.FromUUID == cau && cau != UUID.Zero)
            {
                if (!chairAnnEnabled) return;
                if (nextCallTime <= DateTime.Now)
                {
                    TextPrinter.SetSelectionForeColor(Color.DarkOrange);
                    if (chairAnnChat) TextPrinter.PrintText(prefix);

                    // the MD5 stuff (as of V 0.9.3)
                    //chop out the Password if there is one
                    string[] chops = smsg.Split(new char[] { '|' }, StringSplitOptions.None);
                    string pwd;
                    pwd = chops.Length == 3 ? chops[1] : "invalid";

                    string str = instance.Config.CurrentConfig.GroupManPro; //GM: actually it is the MEGAbolt password
                    MEGAMD5 md5 = new MEGAMD5();
                    string metapwd = md5.MD5(str);

                    if (pwd != metapwd)
                    {
                        string gmsg = string.Empty;

                        if (pwd == "invalid")
                        {
                            gmsg = "IMPORTANT WARNING: A chair announcement could not be sent out. Your Chair Announcer is out of date. \n" +
                                "The password used is: " + pwd + ".";
                        }
                        else
                        {
                            gmsg = "IMPORTANT WARNING: A chair announcement with an invalid password has been received. " +
                                "Either the passwords in MEGAbolt & Chair Announcer don't match or someone is trying to get unauthorised access. " +
                                "This is for information purposes only. DO NOT PANIC. The request has been discarded.  \n" +
                                "The password used is: " + pwd;
                        }

                        TextPrinter.PrintTextLine(gmsg); //always print even if chairAnnChat is turned off
                        return;
                    }

                    //fixup the text
                    //chop off before the ChairPrefix
                    string cp = Properties.Resources.ChairPrefix;
                    int pos = smsg.IndexOf(cp, 2, StringComparison.CurrentCultureIgnoreCase);
                    pos = pos < 0 ? 0 : pos;
                    StringBuilder sb = new StringBuilder(smsg.Substring(pos));

                    string ca = chairAnnAdvert;
                    sb.Append(" \n");
                    sb.Append(ca);

                    //work out which group to IM between 0 and 5
                    indexGroup++;
                    if (indexGroup == 6 || chairAnnouncerGroups[indexGroup] == UUID.Zero) indexGroup = 0;
                    nextCallTime = DateTime.Now.AddMinutes(1);
                    //callbacks
                    client.Self.GroupChatJoined += Self_OnGroupChatJoin;

                    //calculate the interval
                    int perGroupInterval = (int)Math.Round(((decimal)(chairAnnouncerInterval / chairAnnouncerActives)));
                    perGroupInterval = perGroupInterval < 1 ? 1 : perGroupInterval;
                    //find if already in the group
                    UUID grp = chairAnnouncerGroups[indexGroup];
                    if (client.Self.GroupChatSessions.ContainsKey(grp))
                    {
                        client.Self.InstantMessageGroup(grp, sb.ToString());
                        if (chairAnnChat) TextPrinter.PrintTextLine("Chair Announcer: IM to existing group " + chairAnnouncerGroupNames[indexGroup]);
                        nextCallTime = nextCallTime.AddMinutes(perGroupInterval - 1);
                    }
                    else
                    {
                        targetIndex = indexGroup;
                        client.Self.RequestJoinGroupChat(grp);
                        waitGroupIMSession.Reset();
                        if (waitGroupIMSession.WaitOne(30000, false)) //30 seconds
                        {
                            client.Self.InstantMessageGroup(grp, sb.ToString());
                            if (chairAnnChat) TextPrinter.PrintTextLine("Chair Announcer: IM to new group " + chairAnnouncerGroupNames[indexGroup]);
                            nextCallTime = nextCallTime.AddMinutes(perGroupInterval - 1);
                        }
                        else
                        {
                            Logger.Log("Chair Announcer: timeout after 30 seconds on group " + indexGroup.ToString(CultureInfo.CurrentCulture), Helpers.LogLevel.Warning);
                        }
                    }

                    client.Self.GroupChatJoined -= Self_OnGroupChatJoin;
                }
                else
                {
                    Logger.Log("Chair Announcer: skipped", Helpers.LogLevel.Info);
                }

                Logger.Log(String.Format(CultureInfo.CurrentCulture, "AddIn: {0} called {1}", cau.ToString(), smsg), Helpers.LogLevel.Debug);
                return;
            }

            if (classiclayout)
            {
                TextPrinter.SetSelectionForeColor(Color.Gray);
                TextPrinter.PrintClassicTextDate(prefix);
                //textPrinter.PrintDate(prefix);
            }
            else
            {
                //textPrinter.SetSelectionForeColor(Color.Gray);
                //textPrinter.PrintText(dte.ToString("[HH:mm] "));
                //textPrinter.SetOffset(6);
                //textPrinter.SetFontSize(6.5f);
                TextPrinter.PrintDate(prefix);
                //textPrinter.SetFontSize(8.5f);
                //textPrinter.SetOffset(0);
            }

            bool islhdr = false;

            switch (item.Style)
            {
                case ChatBufferTextStyle.Normal:
                    TextPrinter.SetSelectionForeColor(Color.Black);
                    break;

                case ChatBufferTextStyle.StatusBlue:
                    TextPrinter.SetSelectionForeColor(Color.Blue);
                    break;

                case ChatBufferTextStyle.StatusBold:
                    TextPrinter.SetFontStyle(FontStyle.Bold);
                    break;

                case ChatBufferTextStyle.StatusBrown:
                    TextPrinter.SetSelectionForeColor(Color.Brown);
                    break;

                case ChatBufferTextStyle.StatusDarkBlue:
                    TextPrinter.SetSelectionForeColor(Color.Gray);
                    //textPrinter.BackColor = Color.LightSeaGreen;
                    break;

                case ChatBufferTextStyle.LindenChat:
                    TextPrinter.SetSelectionForeColor(Color.DarkGreen);
                    TextPrinter.SetSelectionBackColor(Color.LightYellow);
                    break;

                case ChatBufferTextStyle.ObjectChat:
                    TextPrinter.SetSelectionForeColor(Color.DarkCyan);
                    //item.Text = "\n" + item.Text;
                    break;

                case ChatBufferTextStyle.OwnerSay:
                    TextPrinter.SetSelectionForeColor(Color.FromArgb(255, 211, 75, 0));
                    //item.Text = "\n" + item.Text;
                    break;

                case ChatBufferTextStyle.RegionSay:
                    TextPrinter.SetSelectionForeColor(Color.Green);
                    //item.Text = "\n" + item.Text;
                    break;

                case ChatBufferTextStyle.StartupTitle:
                    TextPrinter.SetSelectionForeColor(Color.Black);
                    TextPrinter.SetFontStyle(FontStyle.Bold);
                    break;

                case ChatBufferTextStyle.Alert:
                    TextPrinter.SetSelectionForeColor(Color.White);
                    //textPrinter.SetSelectionBackColor(Color.BlueViolet);
                    TextPrinter.SetSelectionBackColor(Color.SteelBlue);
                    item.Text = item.Text;   // +"\n";
                    break;

                case ChatBufferTextStyle.Error:
                    TextPrinter.SetSelectionForeColor(Color.Yellow);
                    TextPrinter.SetSelectionBackColor(Color.Red);
                    break;

                case ChatBufferTextStyle.LoginReply:
                    TextPrinter.PrintHeader(":: Grid Login Message ::");
                    TextPrinter.SetSelectionForeColor(Color.Black);
                    //textPrinter.SetSelectionBackColor(Color.LightSteelBlue);
                    TextPrinter.SetSelectionBackColor(instance.Config.CurrentConfig.HeaderBackColour);
                    TextPrinter.SetFontStyle(FontStyle.Bold);
                    //textPrinter.SetFontSize(12);
                    TextPrinter.SetOffset(8);
                    islhdr = true;

                    if (item.Text.Contains("http://"))
                    {
                        item.Text = instance.CleanReplace("http://", "\nhttp://", item.Text);
                    }

                    if (item.Text.Contains("https://"))
                    {
                        item.Text = instance.CleanReplace("https://", "\nhttps://", item.Text);
                    }
                    break;
            }

            TextPrinter.PrintTextLine(item.Text);

            if (islhdr) TextPrinter.PrintHeader(" ");

            //// Handle chat tweets
            //if (TEnabled)
            //{
            //    if (instance.Config.CurrentConfig.EnableChatTweets)
            //    {
            //        Yedda.Twitter twit = new Yedda.Twitter();
            //        string resp = string.Empty;

            //        if (tweet)
            //        {
            //            // if enabled print to Twitter
            //            resp = twit.UpdateAsJSON(tName, tPwd, item.Text);
            //        }
            //        else
            //        {
            //            // it's a direct message
            //            resp = twit.Send(tName, tPwd, tweetname, item.Text);
            //        }

            //        if (resp != "OK")
            //        {
            //            Logger.Log("Twitter error: " + resp, Helpers.LogLevel.Warning);
            //        }
            //    }
            //}

            if (reprinting)
            {
                reprinting = false;
                return;
            }

            if (!classiclayout)
            {
                string txt = item.FromName + ": " + item.Text;
                LogMessage(dte, item.FromUUID.ToString(), item.FromName, txt);
            }
            else
            {
                LogMessage(dte, item.FromUUID.ToString(), item.FromName, item.Text);
            }
        }

        void Self_OnGroupChatLeft(UUID groupchatSessionID)
        {
            Logger.Log(String.Format(CultureInfo.CurrentCulture, "Chair Announcer: Left GroupChat {0}", groupchatSessionID.ToString()), Helpers.LogLevel.Debug);
            waitGroupIMLeaveSession.Set();
        }

        void Self_OnGroupChatJoin(object sender, GroupChatJoinedEventArgs e)
        {
            if (e.Success)
            {
                Logger.Log(String.Format(CultureInfo.CurrentCulture, "Chair Announcer: Joined GroupChat {0} with UUID {1} named {2}", targetIndex, e.SessionID.ToString(), e.SessionName), Helpers.LogLevel.Debug);
                chairAnnouncerGroupNames[targetIndex] = e.SessionName;
                waitGroupIMSession.Set();
            }
            else
            {
                Logger.Log(String.Format(CultureInfo.CurrentCulture, "Chair Announcer: Failed GroupChat {0} with UUID {1} named {2}", targetIndex, e.SessionID.ToString(), e.SessionName), Helpers.LogLevel.Debug);
                chairAnnouncerGroupNames[targetIndex] = "N/A";
                TextPrinter.PrintTextLine("Chair Announcer: Failed to join GroupChat");
            }
        }

        // Seperate thread
        private void GroupMembersHandler(object sender, GroupMembersReplyEventArgs e)
        {
            if (e.RequestID == GroupRequestID)
            {
                client.Groups.GroupMembersReply -= GroupMembersHandler;

                if (igroup != e.GroupID)
                {
                    return;
                }

                invitecounter += 1;

                if (e.Members.Count > 0)
                {
                    GroupMember gmember;

                    if (e.Members.TryGetValue(iperson, out gmember))
                    {
                        invitecounter = 0;

                        if (!ismember)
                        {
                            //DateTime dte = DateTime.Now;

                            //dte = this.instance.State.GetTimeStamp(dte);

                            //if (instance.Config.CurrentConfig.UseSLT)
                            //{
                            //    string _timeZoneId = "Pacific Standard Time";
                            //    DateTime startTime = DateTime.UtcNow;
                            //    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                            //    dte = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
                            //}

                            string prefix = instance.SetTime();    //dte.ToString("[HH:mm] ");
                            string gname = instance.State.GroupStore[igroup];

                            //textPrinter.SetSelectionForeColor(Color.Gray);
                            // textPrinter.PrintTextLine(prefix + "\n\n[ GroupMan Pro ] @ " + gmanlocation + "\n   Invite request for group " + gname.ToUpper(CultureInfo.CurrentCulture) + " has been ignored. " + gavname + " (" + iperson.ToString() + ") is already a member.");

                            string Msg = "\n\n[ GroupMan Pro ] @ " + gmanlocation + "\n   Invite request for group " + gname.ToUpper(CultureInfo.CurrentCulture) + " has been ignored. " + gavname + " (" + iperson + ") is already a member.";

                            ChatBufferItem ready = new ChatBufferItem(DateTime.Now, Msg, ChatBufferTextStyle.StatusGray);

                            ProcessBufferItem(ready, true);

                            return;
                        }
                        else
                        {
                            ismember = false;
                            GivePresent();
                            return;
                        }
                    }
                }

                if (invitecounter > 1)
                {
                    invitecounter = 0;
                    ismember = false;
                    aTimer.Stop();
                    aTimer.Enabled = false;
                    return;
                }

                if (invitecounter == 1)
                {
                    WriteToChat();
                }
            }
        }

        private void WriteToChat()
        {
            //DateTime dte = DateTime.Now;

            //dte = this.instance.State.GetTimeStamp(dte);

            //if (instance.Config.CurrentConfig.UseSLT)
            //{
            //    string _timeZoneId = "Pacific Standard Time";
            //    DateTime startTime = DateTime.UtcNow;
            //    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
            //    dte = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
            //}

            //textPrinter.ForeColor = Color.Gray;
            string prefix = instance.SetTime();    //dte.ToString("[HH:mm] ");

            try
            {
                client.Groups.Invite(igroup, roles, iperson);

                // start timer to check if invite has been accepted
                aTimer = new System.Timers.Timer();
                aTimer.Elapsed += OnTimedEvent;
                // Set the Interval to 10 seconds.
                aTimer.Interval = 10000;
                aTimer.Enabled = true;
                aTimer.Start();
            }
            catch (Exception excp)
            {
                //string eex = excp.ToString();
                //PrintIM(DateTime.Now, e.IM.FromAgentName, "GroupMan Pro has encountered an error and a group invite could not be sent to: " + sGrp[2].ToString());
                TextPrinter.PrintTextLine(String.Format(CultureInfo.CurrentCulture, prefix + "(\nGroupMan Pro @ " + gmanlocation + ")\nGroupMan Pro has encountered an error and a group invite could not be sent to: " + gavname));
                Logger.Log(String.Format(CultureInfo.CurrentCulture, prefix + "GroupMan Pro Error: {0}", excp), Helpers.LogLevel.Error);
                return;
            }

            string gname = string.Empty;

            if (instance.State.GroupStore.ContainsKey(igroup))
            {
                gname = "for group " + instance.State.GroupStore[igroup];
            }

            //textPrinter.SetFontStyle(FontStyle.Bold);
            //textPrinter.PrintTextLine(prefix + "\n\n[ GroupMan Pro ] @ " + gmanlocation + "\n   An invite has been sent to: " + gavname + " (" + iperson.ToString() + ")" + gname);

            string Msg = "\n\n[ GroupMan Pro ] @ " + gmanlocation + "\n   An invite has been sent to: " + gavname + " (" + iperson + ")" + gname;

            ChatBufferItem ready = new ChatBufferItem(
               DateTime.Now, Msg, ChatBufferTextStyle.StatusBold);

            ProcessBufferItem(ready, true);
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            client.Groups.GroupMembersReply += GroupMembersHandler;
            GroupRequestID = client.Groups.RequestGroupMembers(igroup);
            ismember = true;

            aTimer.Stop();
            aTimer.Enabled = false;
        }

        private void GivePresent()
        {
            if (!instance.Config.CurrentConfig.GivePresent) return;

            InventoryFolder presies = client.Inventory.Store.RootFolder;
            List<InventoryBase> foundfolders = client.Inventory.Store.GetContents(presies);

            foreach (InventoryBase o in foundfolders)
            {
                // for this to work the user needs to have a folder called "GroupMan Items"
                if (o.Name.ToLower(CultureInfo.CurrentCulture) == "groupman items")
                {
                    if (o is InventoryFolder)
                    {
                        List<InventoryBase> founditems = client.Inventory.FolderContents(o.UUID, client.Self.AgentID, false, true, InventorySortOrder.ByName, 3000);
                        int icount = founditems.Count;
                        Random random = new Random();
                        int num = random.Next(icount);

                        InventoryItem item = (InventoryItem)founditems[num];

                        if ((item.Permissions.OwnerMask & PermissionMask.Transfer) == PermissionMask.Transfer)
                        {
                            //DateTime dte = DateTime.Now;

                            //dte = this.instance.State.GetTimeStamp(dte);

                            //if (instance.Config.CurrentConfig.UseSLT)
                            //{
                            //    string _timeZoneId = "Pacific Standard Time";
                            //    DateTime startTime = DateTime.UtcNow;
                            //    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                            //    dte = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
                            //}

                            string prefix = instance.SetTime();   // dte.ToString("[HH:mm] ");

                            //Give the item
                            client.Self.InstantMessage(iperson, "I am giving you a present for joining our group. Thank you.");
                            client.Inventory.GiveItem(item.UUID, item.Name, item.AssetType, iperson, false);
                            TextPrinter.PrintTextLine(prefix + "\nGroupMan Pro: Gave '" + item.Name + "' as a joining present to " + gavname);
                        }
                    }
                }
            }
        }

        private void ProcessBufferItemResp(ChatBufferItem item, bool addToBuffer)
        {
            //if (addToBuffer)
            //{
            //    textBuffer.Add(item);

            //    //int lines = textBuffer.Count;
            //    //int maxlines = this.instance.Config.CurrentConfig.lineMax;

            //    //if (lines > maxlines && maxlines > 0)
            //    //{
            //    //    CheckBufferSize();
            //    //    return;
            //    //}
            //}

            TextPrinter.SetSelectionForeColor(Color.Gray);
            DateTime dte = item.Timestamp;

            if (classiclayout)
            {
                if (showTimestamps)
                {
                    dte = instance.State.GetTimeStamp(dte);

                    //if (instance.Config.CurrentConfig.UseSLT)
                    //{
                    //    string _timeZoneId = "Pacific Standard Time";
                    //    DateTime startTime = DateTime.UtcNow;
                    //    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                    //    dte = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
                    //}

                    TextPrinter.PrintClassicTextDate(dte.ToString("[HH:mm] ", CultureInfo.CurrentCulture));
                }

                try
                {
                    if (item.Style != ChatBufferTextStyle.ObjectChat && item.Style != ChatBufferTextStyle.OwnerSay && item.Style != ChatBufferTextStyle.RegionSay)
                    {
                        if (!string.IsNullOrEmpty(item.FromName))
                        {
                            TextPrinter.PrintLink(item.FromName, item.Link + "&" + item.FromName);
                        }
                    }
                    else
                    {
                        TextPrinter.PrintClassicTextDate(item.FromName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Chat Manager: " + ex.Message, Helpers.LogLevel.Error);
                }
            }
            else
            {
                try
                {
                    if (lastspeaker != item.FromName)
                    {
                        TextPrinter.SetFontStyle(FontStyle.Bold);

                        if (item.Style != ChatBufferTextStyle.ObjectChat && item.Style != ChatBufferTextStyle.OwnerSay && item.Style != ChatBufferTextStyle.RegionSay)
                        {
                            if (!string.IsNullOrEmpty(item.FromName))
                            {
                                TextPrinter.PrintLinkHeader(item.FromName, item.FromUUID.ToString(), item.Link + "&" + item.FromName);
                            }
                        }
                        else
                        {
                            TextPrinter.PrintHeader(item.FromName);
                        }

                        lastspeaker = item.FromName;
                    }
                }
                catch
                {
                    ;
                }

                TextPrinter.SetFontStyle(FontStyle.Regular);
                TextPrinter.SetSelectionBackColor(Color.White);
                //textPrinter.SetSelectionBackColor(instance.Config.CurrentConfig.BgColour);

                if (showTimestamps)
                {
                    dte = instance.State.GetTimeStamp(dte);

                    //if (instance.Config.CurrentConfig.UseSLT)
                    //{
                    //    string _timeZoneId = "Pacific Standard Time";
                    //    DateTime startTime = DateTime.UtcNow;
                    //    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                    //    dte = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
                    //}

                    string header = string.Empty;

                    header = "[HH:mm] ";
                    //textPrinter.SetSelectionForeColor(Color.Gray);
                    //textPrinter.SetOffset(6);
                    //textPrinter.SetFontSize(6.5f);
                    TextPrinter.PrintDate(dte.ToString(header, CultureInfo.CurrentCulture));
                    //textPrinter.SetFontSize(8.5f);
                    //textPrinter.SetOffset(0);
                }
            }

            switch (item.Style)
            {
                case ChatBufferTextStyle.Normal:
                    TextPrinter.SetSelectionForeColor(Color.Blue);
                    break;

                case ChatBufferTextStyle.StatusBlue:
                    TextPrinter.SetSelectionForeColor(Color.BlueViolet);
                    break;

                case ChatBufferTextStyle.StatusBold:
                    TextPrinter.SetFontStyle(FontStyle.Bold);
                    break;

                case ChatBufferTextStyle.StatusBrown:
                    TextPrinter.SetSelectionForeColor(Color.Brown);
                    break;

                case ChatBufferTextStyle.StatusDarkBlue:
                    TextPrinter.SetSelectionForeColor(Color.White);
                    TextPrinter.SetSelectionBackColor(Color.LightSeaGreen);
                    break;

                case ChatBufferTextStyle.LindenChat:
                    TextPrinter.SetSelectionForeColor(Color.DarkGreen);
                    TextPrinter.SetSelectionBackColor(Color.LightYellow);
                    break;

                case ChatBufferTextStyle.ObjectChat:
                    TextPrinter.SetSelectionForeColor(Color.DarkCyan);
                    //item.Text = "\n" + item.Text;
                    break;

                case ChatBufferTextStyle.OwnerSay:
                    TextPrinter.SetSelectionForeColor(Color.FromArgb(255, 211, 75, 0));
                    //item.Text = "\n" + item.Text;
                    break;

                case ChatBufferTextStyle.RegionSay:
                    TextPrinter.SetSelectionForeColor(Color.Green);
                    //item.Text = "\n" + item.Text;
                    break;

                case ChatBufferTextStyle.Alert:
                    TextPrinter.SetSelectionForeColor(Color.White);
                    //textPrinter.SetSelectionBackColor(Color.BlueViolet);
                    TextPrinter.SetSelectionBackColor(Color.SteelBlue);
                    item.Text = item.Text;   // +"\n";
                    break;

                case ChatBufferTextStyle.Error:
                    TextPrinter.SetSelectionForeColor(Color.Yellow);
                    TextPrinter.SetSelectionBackColor(Color.Red);
                    break;
            }

            TextPrinter.PrintTextLine(item.Text);

            if (reprinting)
            {
                reprinting = false;
                return;
            }

            if (!classiclayout)
            {
                string txt = item.FromName + ": " + item.Text;
                LogMessage(dte, item.FromUUID.ToString(), item.FromName, txt);
            }
            else
            {
                LogMessage(dte, item.FromUUID.ToString(), item.FromName, item.Text);
            }
        }

        //Used only for non-public chat
        private void ProcessOutgoingChat(ChatSentEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("(channel ");
            sb.Append(e.Channel);

            sb.Append(classiclayout ? ") You: " : ") ");

            switch (e.Type)
            {
                case ChatType.Normal:
                    if (classiclayout)
                    {
                        sb.Append(": ");
                    }
                    break;

                case ChatType.Whisper:
                    sb.Append(" [whispers] ");
                    break;

                case ChatType.Shout:
                    sb.Append(" [shouts] ");
                    break;
            }

            sb.Append(e.Message);

            ChatBufferItem item = new ChatBufferItem(
                DateTime.Now, sb.ToString(), ChatBufferTextStyle.StatusDarkBlue, client.Self.Name);

            ProcessBufferItem(item, true);

            sb = null;
        }

        private void ProcessIncomingChat(ChatEventArgs e)
        {

            if (string.IsNullOrEmpty(e.Message)) return;

            if (instance.IsAvatarMuted(e.OwnerID, e.FromName))
                return;

            //if (e.Message.Substring(0, 1) == "@") return;   // Ignore RLV commands
            if (e.Message.Contains(imu)) return; // Ignore the message for plugin use or whatever
            if (e.Message.Contains(commandin)) return; // LSL API command

            StringBuilder sb = new StringBuilder();

            if (e.Message.StartsWith("/me ", StringComparison.CurrentCultureIgnoreCase))
            {
                sb.Append(e.FromName);
                sb.Append(e.Message.Substring(3));
            }
            else if (e.FromName.ToLower(CultureInfo.CurrentCulture) == client.Self.Name.ToLower(CultureInfo.CurrentCulture) && e.SourceType == ChatSourceType.Agent)
            {
                if (classiclayout)
                {
                    sb.Append(e.FromName);
                }

                switch (e.Type)
                {
                    case ChatType.Normal:
                        sb.Append(classiclayout ? ": " : " ");
                        break;

                    case ChatType.Whisper:
                        sb.Append(" [whispers] ");
                        break;

                    case ChatType.Shout:
                        sb.Append(" [shouts] ");
                        break;
                }

                sb.Append(e.Message);
            }
            else
            {
                if (instance.chatbuffer.RingBuffer(instance))
                {
                    ChatBufferItem dalert = new ChatBufferItem(
                    DateTime.Now, "Too many chat messages are coming in too quickly. Flood Protection is in operation for your security...", ChatBufferTextStyle.Alert);

                    ProcessBufferItem(dalert, true);

                    return;
                }

                switch (e.Type)
                {
                    case ChatType.Normal:
                        sb.Append(classiclayout ? ": " : " ");
                        break;

                    case ChatType.Whisper:
                        sb.Append(" [whispers] ");
                        break;

                    case ChatType.Shout:
                        sb.Append(" [shouts] ");
                        break;
                    case ChatType.Debug:
                        //sb.Append(": ");
                        return;
                }

                sb.Append(e.Message);
            }

            ChatBufferItem item = new ChatBufferItem
            {
                Timestamp = DateTime.Now,
                Text = sb.ToString(),
                FromName = e.FromName,
                Link = "http://mbprofile:" + e.OwnerID
            };

            switch (e.SourceType)
            {
                case ChatSourceType.Agent:
                    item.Style =
                        (e.FromName.EndsWith("Linden", StringComparison.CurrentCultureIgnoreCase) ?
                        ChatBufferTextStyle.LindenChat : ChatBufferTextStyle.Normal);
                    break;

                case ChatSourceType.Object:
                    if (instance.IsObjectMuted(e.SourceID, e.FromName))
                        return;

                    // Ignore RLV commands from objects
                    if (item.Text.StartsWith("@", StringComparison.CurrentCultureIgnoreCase)) return;

                    if (e.Type == ChatType.OwnerSay)
                    {
                        item.Style = ChatBufferTextStyle.OwnerSay;
                    }
                    else if (e.Type == ChatType.Debug)
                    {
                        item.Style = ChatBufferTextStyle.Error;
                    }
                    else if (e.Type == ChatType.RegionSay)
                    {
                        item.Style = ChatBufferTextStyle.RegionSay;
                    }
                    else
                    {
                        item.Style = ChatBufferTextStyle.ObjectChat;
                    }

                    //item.Style = ChatBufferTextStyle.ObjectChat;
                    break;
            }

            if (e.FromName.ToLower(CultureInfo.CurrentCulture) == client.Self.Name.ToLower(CultureInfo.CurrentCulture))
            {
                ProcessBufferItem(item, true);
            }
            else
            {
                ProcessBufferItemResp(item, true);
            }

            sb = null;
        }

        private void LogMessage(DateTime timestamp, string uuid, string fromName, string msg)
        {
            if (!instance.Config.CurrentConfig.SaveChat)
                return;

            //if (string.IsNullOrEmpty(fromName))
            //    return;

            string newsess = string.Empty;

            if (ChatLogin)
            {
                newsess = "\r\n\r\n\nNew login session...\r\n";
                ChatLogin = false;
            }

            string folder = instance.Config.CurrentConfig.LogDir;

            if (!folder.EndsWith("\\", StringComparison.CurrentCultureIgnoreCase))
            {
                folder += "\\";
            }

            // Log the message
            string filename = "CHAT-" + timestamp.Date.ToString(CultureInfo.CurrentCulture) + "-" + client.Self.Name + ".txt";

            filename = instance.CleanReplace("/", "-", filename);    //filename.Replace("/", "-");
            //filename = filename.Replace(" ", "_");
            filename = instance.CleanReplace(":", "-", filename);    //filename.Replace(":", "-");

            string path = folder + filename;
            string line = newsess + "[" + timestamp.ToShortTimeString() + "] " + msg;

            bool exists = false;

            // Check if the file exists
            try
            {
                exists = File.Exists(@path);
            }
            catch
            {
                ;
            }

            if (exists)
            {
                //StreamWriter swFromFileStreamUTF8Buffer = new StreamWriter(fs, System.Text.Encoding.UTF8, 512);
                StreamWriter SW = File.AppendText(@path);

                //long theTrueFileSize = SW.BaseStream.Length;

                try
                {
                    SW.WriteLine(@line);
                    SW.Dispose();
                }
                catch
                {
                    SW.Dispose(); ;
                }
            }
            else
            {
                StreamWriter SW = File.CreateText(@path);

                try
                {
                    SW.WriteLine(@line);
                    SW.Dispose();
                }
                catch
                {
                    //string exp = ex.Message;
                    SW.Dispose();
                }
            }
        }

        public void ReprintAllText()
        {
            //textPrinter.ClearText();

            //try
            //{
            //    foreach (ChatBufferItem item in textBuffer)
            //    {
            //        //ProcessBufferItem(item, false);
            //        if (item.FromName == client.Self.Name)
            //        {
            //            ProcessBufferItem(item, false);
            //        }
            //        else
            //        {
            //            ProcessBufferItemResp(item, false);
            //        }
            //    }
            //}
            //catch
            //{
            //    ;
            //}
        }

        public void ClearInternalBuffer()
        {
            //textBuffer.Clear();
        }

        public ITextPrinter TextPrinter { get; set; }
    }
}
