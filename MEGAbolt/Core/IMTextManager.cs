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
using System.Drawing;
using System.Text;
using System.Threading;
using MEGAbolt.NetworkComm;
using OpenMetaverse;
using System.Windows.Forms;
using MEGAbrain;
using System.IO;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;


namespace MEGAbolt
{
    public class IMTextManager
    {
        private MEGAboltInstance instance;
        private MEGAboltNetcom netcom;

        private string sessionAVname = string.Empty;
        //private string sessionGroupName = string.Empty;  
        private GridClient client;
        //private string tName = string.Empty;
        //private string tPwd = string.Empty;
        //private bool TEnabled = false;
        //private bool tweet = true;
        //private string tweetname = string.Empty;
        public global::MEGAbrain.MEGAbrain answer;

        //private ArrayList textBuffer;
        private AIMLbot.Bot myBot;
        private string lastspeaker = string.Empty;
        private bool classiclayout = false;
        private MEGAbrain brain;

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                BugSplat crashReporter = new BugSplat("radegast", "MEGAbolt",
                    Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                {
                    User = "cinder@cinderblocks.biz",
                    ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                };
                crashReporter.Post(e.Exception);
            }
        }

        public IMTextManager(MEGAboltInstance instance, ITextPrinter textPrinter, UUID sessionID, string groupname, Group grp)
        {
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            SessionID = sessionID;
            //this.sessionGroupName = groupname; 

            TextPrinter = textPrinter;
            //this.textBuffer = new ArrayList();

            this.instance = instance;
            client = this.instance.Client;
            netcom = this.instance.Netcom;
            AddNetcomEvents();

            ShowTimestamps = this.instance.Config.CurrentConfig.IMTimestamps;
            //tName = this.instance.Config.CurrentConfig.TweeterName;
            //tPwd = this.instance.Config.CurrentConfig.TweeterPwd;
            //TEnabled = this.instance.Config.CurrentConfig.EnableTweeter;
            //tweet = this.instance.Config.CurrentConfig.Tweet;
            //tweetname = this.instance.Config.CurrentConfig.TweeterUser;
            classiclayout = this.instance.Config.CurrentConfig.ClassicChatLayout;

            this.instance.Config.ConfigApplied += Config_ConfigApplied;

            myBot = this.instance.ABot;
        }

        public IMTextManager(MEGAboltInstance instance, ITextPrinter textPrinter, UUID sessionID, string avname)
        {
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            SessionID = sessionID;
            sessionAVname = avname;

            TextPrinter = textPrinter;
            //this.textBuffer = new ArrayList();

            this.instance = instance;
            client = this.instance.Client;
            netcom = this.instance.Netcom;
            AddNetcomEvents();

            ShowTimestamps = this.instance.Config.CurrentConfig.IMTimestamps;
            //tName = this.instance.Config.CurrentConfig.TweeterName;
            //tPwd = this.instance.Config.CurrentConfig.TweeterPwd;
            //TEnabled = this.instance.Config.CurrentConfig.EnableTweeter;
            //tweet = this.instance.Config.CurrentConfig.Tweet;
            //tweetname = this.instance.Config.CurrentConfig.TweeterUser;
            classiclayout = this.instance.Config.CurrentConfig.ClassicChatLayout;

            this.instance.Config.ConfigApplied += Config_ConfigApplied;

            myBot = this.instance.ABot;
        }

        private void Config_ConfigApplied(object sender, ConfigAppliedEventArgs e)
        {
            ShowTimestamps = e.AppliedConfig.IMTimestamps;
            //ReprintAllText();
            classiclayout = instance.Config.CurrentConfig.ClassicChatLayout;

            //tName = e.AppliedConfig.TweeterName;
            //tPwd = e.AppliedConfig.TweeterPwd;
            //TEnabled = e.AppliedConfig.EnableTweeter;
            //tweet = this.instance.Config.CurrentConfig.Tweet;
            //tweetname = this.instance.Config.CurrentConfig.TweeterUser;
        }

        private void AddNetcomEvents()
        {
            netcom.InstantMessageReceived += netcom_InstantMessageReceived;
            netcom.InstantMessageSent += netcom_InstantMessageSent;
        }

        private void RemoveNetcomEvents()
        {
            netcom.InstantMessageReceived -= netcom_InstantMessageReceived;
            netcom.InstantMessageSent -= netcom_InstantMessageSent;
        }

        private void netcom_InstantMessageSent(object sender, InstantMessageSentEventArgs e)
        {
            if (e.SessionID != SessionID) return;

            //textBuffer.Add(e);

            //int lines = textBuffer.Count;
            //int maxlines = this.instance.Config.CurrentConfig.lineMax;

            //if (lines > maxlines && maxlines > 0)
            //{
            //    CheckBufferSize();
            //    return;
            //}

            ProcessIM(e);
        }

        private void netcom_InstantMessageReceived(object sender, InstantMessageEventArgs e)
        {
            if (e.IM.IMSessionID != SessionID)
            {
                return;
            }

            //if (e.IM.Message.Contains(this.instance.Config.CurrentConfig.CommandInID)) return;
            //if (e.IM.Message.Contains(this.instance.Config.CurrentConfig.IgnoreUID)) return;

            if (e.IM.Dialog == InstantMessageDialog.StartTyping ||
                e.IM.Dialog == InstantMessageDialog.StopTyping ||
                e.IM.Dialog == InstantMessageDialog.MessageFromObject)
                return;

            string cp = Properties.Resources.ChairPrefix;

            if (e.IM.FromAgentID != client.Self.AgentID)
            {
                //textBuffer.Add(e);

                //int lines = textBuffer.Count;
                //int maxlines = this.instance.Config.CurrentConfig.lineMax;

                //if (lines > maxlines && maxlines > 0)
                //{
                //    CheckBufferSize();
                //    return;
                //}

                ProcessIM(e);
            }
            //GM new bit to show our Chair Announcing
            //not pretty but how else can we catch just the calling stuff?
            else if (e.IM.FromAgentID == client.Self.AgentID && e.IM.Message.StartsWith(cp, StringComparison.CurrentCultureIgnoreCase))
            {
                //textBuffer.Add(e);

                //int lines = textBuffer.Count;
                //int maxlines = this.instance.Config.CurrentConfig.lineMax;

                //if (lines > maxlines && maxlines > 0)
                //{
                //    CheckBufferSize();
                //    return;
                //}

                ProcessIM(e);
            }
        }

        public void ProcessIM(object e)
        {
            if (e is InstantMessageEventArgs args)
                ProcessIncomingIM(args);
            else if (e is InstantMessageSentEventArgs eventArgs)
                ProcessOutgoingIM(eventArgs);
        }

        private void ProcessOutgoingIM(InstantMessageSentEventArgs e)
        {
            PrintIM(DateTime.Now , e.TargetID.ToString(), netcom.LoginOptions.FullName, e.Message, e.SessionID);
        }

        private void ProcessIncomingIM(InstantMessageEventArgs e)
        {
            // Check to see if avatar is muted
            if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName))
                return;

            //string iuid = this.instance.Config.CurrentConfig.IgnoreUID;

            //if (e.IM.Message.Contains(iuid)) return; // Ignore Im for plugins use etc.
            if (e.IM.Message.Contains(instance.Config.CurrentConfig.CommandInID)) return;
            if (e.IM.Message.Contains(instance.Config.CurrentConfig.IgnoreUID)) return;

            bool isgroup = instance.State.GroupStore.ContainsKey(e.IM.IMSessionID);

            if (isgroup)
            {
                //if (null != client.Self.MuteList.Find(me => me.Type == MuteType.Group && (me.ID == e.IM.IMSessionID || me.ID == e.IM.FromAgentID))) return;

                // Check to see if group IMs are disabled
                if (instance.Config.CurrentConfig.DisableGroupIMs)
                    return;
            }
            
            PrintIM(DateTime.Now, e.IM.FromAgentID.ToString(), e.IM.FromAgentName, e.IM.Message, e.IM.IMSessionID);

            if (!isgroup)
            {
                if (instance.State.IsBusy)
                {
                    string responsemsg = instance.Config.CurrentConfig.BusyReply;
                    client.Self.InstantMessage(client.Self.Name, e.IM.FromAgentID, responsemsg, e.IM.IMSessionID, InstantMessageDialog.BusyAutoResponse, InstantMessageOnline.Offline, instance.SIMsittingPos(), UUID.Zero, new byte[0]); 
                }
                else
                {
                    // Handles MEGAbrain
                    if (instance.Config.CurrentConfig.AIon)
                    {
                        if (e.IM.FromAgentID == client.Self.AgentID) return;
                        if (client.Self.GroupChatSessions.ContainsKey(e.IM.IMSessionID)) return;
                        if (e.IM.FromAgentName == "Second Life") return;
                        if (e.IM.FromAgentName.Contains("Linden")) return;
                        if (e.IM.Dialog == InstantMessageDialog.SessionSend) return;
                        
                        brain = new MEGAbrain(instance, myBot);
                        brain.StartProcess(e);
                    }
                }
            }
        }

        private void LogMessage(DateTime timestamp, string uuid, string fromName, string msg, bool group, string groupname)
        {
            if (!instance.Config.CurrentConfig.SaveIMs)
                return;

            string folder = instance.Config.CurrentConfig.LogDir;

            if (!folder.EndsWith("\\", StringComparison.CurrentCultureIgnoreCase))
            {
                folder += "\\";
            }
    
            // Log the message
            string filename = string.Empty;

            if (group)
            {
                string cleangrpname = instance.RemoveReservedCharacters(groupname);

                filename = "IM-" + timestamp.Date.ToString() + "-" + client.Self.Name + "-GROUP-" + cleangrpname + ".txt";
            }
            else
            {
                filename = "IM-" + timestamp.Date.ToString() + "-" + client.Self.Name + "-" + sessionAVname + ".txt";
            }

            //filename = filename.Replace("/", "-");
            ////filename = filename.Replace(" ", "_");
            //filename = filename.Replace(":", "-");

            filename = instance.CleanReplace("/", "-", filename);    //filename.Replace("/", "-");
            //filename = filename.Replace(" ", "_");
            filename = instance.CleanReplace(":", "-", filename);    //filename.Replace(":", "-");

            string path = folder + filename;
            string line = "[" + timestamp.ToShortTimeString() + "] " + fromName + ": " + msg; 

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
                StreamWriter SW = File.AppendText(@path);

                try
                {
                    SW.WriteLine(@line);
                    SW.Dispose(); 
                }
                catch
                {
                    SW.Dispose();
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

        private void PrintIM(DateTime timestamp, string uuid, string fromName, string message, UUID ssessionID)
        {
            StringBuilder sb = new StringBuilder();

            if (classiclayout)
            {
                if (ShowTimestamps)
                {
                    timestamp = instance.State.GetTimeStamp(timestamp);

                    //if (instance.Config.CurrentConfig.UseSLT)
                    //{
                    //    string _timeZoneId = "Pacific Standard Time";
                    //    DateTime startTime = DateTime.UtcNow;
                    //    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                    //    timestamp = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
                    //}

                    try
                    {
                        TextPrinter.SetSelectionForeColor(Color.Gray);
                    }
                    catch
                    {
                        ;
                    }

                    TextPrinter.PrintClassicTextDate(timestamp.ToString("[HH:mm] ", CultureInfo.CurrentCulture));
                }

                try
                {
                    TextPrinter.SetSelectionForeColor(Color.Black);
                }
                catch
                {
                    ;
                }

                if (message.StartsWith("/me ", StringComparison.CurrentCultureIgnoreCase))
                {
                    sb.Append(fromName);
                    sb.Append(message.Substring(3));
                }
                else
                {
                    sb.Append(message);


                    string avid = "http://mbprofile:" + uuid.ToString();

                    if (fromName.ToLower(CultureInfo.CurrentCulture) != client.Self.Name.ToLower(CultureInfo.CurrentCulture))
                    {
                        if (!string.IsNullOrEmpty(fromName))
                        {
                            TextPrinter.PrintLink(fromName, avid + "&" + fromName);
                        }

                        TextPrinter.PrintClassicTextDate(": ");
                    }
                    else
                    {
                        TextPrinter.PrintClassicTextDate(fromName + ": ");
                    }
                }
            }
            else
            {
                if (message.StartsWith("/me ", StringComparison.CurrentCultureIgnoreCase))
                {
                    sb.Append(message.Substring(3));
                }
                else
                {
                    sb.Append(message);
                }

                TextPrinter.SetSelectionForeColor(Color.Black);

                try
                {
                    string avid = "http://mbprofile:" + uuid.ToString();

                    if (lastspeaker != fromName)
                    {
                        if (fromName.ToLower(CultureInfo.CurrentCulture) != client.Self.Name.ToLower(CultureInfo.CurrentCulture))
                        {
                            //textPrinter.PrintLinkHeader(fromName, avid + "&" + fromName);
                            TextPrinter.PrintLinkHeader(fromName, uuid.ToString(), avid + "&" + fromName);
                        }
                        else
                        {
                            TextPrinter.SetFontStyle(FontStyle.Bold);
                            TextPrinter.PrintHeader(fromName);
                        }

                        lastspeaker = fromName;
                    }
                }
                catch
                {
                    ;
                }

                TextPrinter.SetFontStyle(FontStyle.Regular);
                TextPrinter.SetSelectionBackColor(Color.White);

                if (ShowTimestamps)
                {
                    timestamp = instance.State.GetTimeStamp(timestamp);

                    //if (instance.Config.CurrentConfig.UseSLT)
                    //{
                    //    string _timeZoneId = "Pacific Standard Time";
                    //    DateTime startTime = DateTime.UtcNow;
                    //    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                    //    timestamp = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
                    //}

                    TextPrinter.SetSelectionForeColor(Color.Gray);
                    TextPrinter.SetOffset(6);
                    TextPrinter.SetFontSize(6.5f);
                    TextPrinter.PrintDate(timestamp.ToString("[HH:mm] ", CultureInfo.CurrentCulture));
                    TextPrinter.SetFontSize(8.5f);
                    TextPrinter.SetOffset(0);
                }
            }

            TextPrinter.SetSelectionForeColor(Color.Black);

            TextPrinter.PrintTextLine(sb.ToString());

            string groupname = string.Empty;
            bool groupfound = instance.State.GroupStore.TryGetValue(ssessionID, out groupname);

            LogMessage(timestamp, uuid, fromName, sb.ToString(), groupfound, groupname);

            sb = null;
        }

        //public void ReprintAllText()
        //{
        //    try
        //    {
        //        textPrinter.ClearText();

        //        foreach (object obj in textBuffer)
        //            ProcessIM(obj);
        //    }
        //    catch
        //    {
        //        ;
        //    }
        //}

        private void CheckBufferSize()
        {
            //int lines = textBuffer.Count;
            //int maxlines = this.instance.Config.CurrentConfig.lineMax;

            //if (maxlines == 0)
            //    return;

            //if (lines > maxlines)
            //{
            //    int lineno = maxlines / 2;

            //    for (int a = 0; a < lineno; a++)
            //    {
            //        textBuffer.RemoveAt(a);
            //    }

            //    ReprintAllText();
            //}
        }

        public void ClearInternalBuffer()
        {
            //textBuffer.Clear();
        }

        /// <summary>
        /// Instruct the TextPrinter to clear the contents of the window
        /// </summary>
        public void ClearAllText()
        {
            TextPrinter.ClearText();
        }

        public void CleanUp()
        {
            RemoveNetcomEvents();

            //textBuffer.Clear();
            //textBuffer = null;


            TextPrinter = null;
        }

        public ITextPrinter TextPrinter { get; set; }

        public bool ShowTimestamps { get; set; }

        public UUID SessionID { get; set; } = UUID.Zero;
    }
}
