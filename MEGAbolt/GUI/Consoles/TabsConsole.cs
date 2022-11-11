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
using System.Text;
using System.Windows.Forms;
using MEGAbolt.NetworkComm;
using OpenMetaverse;
using System.Threading;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BugSplatDotNetStandard;
using MEGAbolt;

namespace MEGAbolt
{
    public partial class TabsConsole : UserControl
    {
        private readonly MEGAboltInstance instance;
        private readonly MEGAboltNetcom netcom;
        private readonly GridClient client;
        public ChatConsole chatConsole;
        public SafeDictionary<string, MEGAboltTab> tabs = new SafeDictionary<string, MEGAboltTab>();
        private string TabAgentName = "";
        //private bool isgroup = false;
        private bool stopnotify = false;
        private string avname = string.Empty;
        private MEGAboltTab selectedTab;
        private Form owner;
        private int tmoneybalance;
        private bool floading = true;

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                BugSplat crashReporter = new BugSplat(Generated.BugsplatDatabase, "MEGAbolt",
                    Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                {
                    User = "cinder@cinderblocks.biz",
                    ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                };
                crashReporter.Post(e.Exception);
            }
        }

        public TabsConsole(MEGAboltInstance instance)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            AddNetcomEvents();

            InitializeMainTab();
            InitializeChatTab();

            ApplyConfig(this.instance.Config.CurrentConfig);
            this.instance.Config.ConfigApplied += Config_ConfigApplied;
        }

        //private ToolStripItem GetSelectedItem()
        //{
        //    ToolStripItem item = null;
        //    for (int i = 0; i < tabs.Count; i++)
        //    {
        //        if (tabs[i].Selected)
        //        {
        //            item = this.DisplayedItems[i];
        //        }
        //    }
        //    return item;
        //}

        private void Config_ConfigApplied(object sender, ConfigAppliedEventArgs e)
        {
            ApplyConfig(e.AppliedConfig);
        }

        private void ApplyConfig(Config config)
        {
            if (config.InterfaceStyle == 0) //System
                tstTabs.RenderMode = ToolStripRenderMode.System;
            else if (config.InterfaceStyle == 1) //Office 2003
                tstTabs.RenderMode = ToolStripRenderMode.ManagerRenderMode;

            stopnotify = config.DisableNotifications;

            if (config.DisableTrayIcon)
            {
                if (stopnotify)
                {
                    notifyIcon1.Visible = false;
                    config.HideMeta = false;
                }
                else
                {
                    if (!config.HideMeta)
                    {
                        notifyIcon1.Visible = false;
                    }
                    else
                    {
                        notifyIcon1.Visible = true;
                    }
                }
            }
            else
            {
                notifyIcon1.Visible = true;
            }

            // Menu positions

            Control control;

            //bool topofscreen = false;

            switch (instance.Config.CurrentConfig.FnMenuPos)
            {
                case "Top":
                    control = toolStripContainer1.TopToolStripPanel;
                    //topofscreen = true;
                    break;

                case "Bottom":
                    control = toolStripContainer1.BottomToolStripPanel;
                    break;

                case "Left":
                    control = toolStripContainer1.LeftToolStripPanel;
                    break;

                case "Right":
                    control = toolStripContainer1.RightToolStripPanel;
                    break;

                default:
                    control = toolStripContainer1.TopToolStripPanel;
                    break;
            }

            tstTabs.Parent = control;
            //topofscreen = false;
        }

        private void AddNetcomEvents()
        {
            netcom.ClientLoginStatus += netcom_ClientLoginStatus;
            netcom.ClientLoggedOut += netcom_ClientLoggedOut;
            netcom.ClientDisconnected += netcom_ClientDisconnected;
            netcom.ChatReceived += netcom_ChatReceived;
            netcom.ChatSent += netcom_ChatSent;
            netcom.AlertMessageReceived += netcom_AlertMessageReceived;
            netcom.InstantMessageReceived += netcom_InstantMessageReceived;
            client.Groups.CurrentGroups += Groups_OnCurrentGroups;
            client.Self.MoneyBalanceReply +=Self_MoneyBalanceReply;
            client.Friends.FriendOffline += Friends_OnFriendOffline;
            client.Friends.FriendOnline += Friends_OnFriendOnline;
        }

        private void RemoveNetcomEvents()
        {
            netcom.ClientLoginStatus -= netcom_ClientLoginStatus;
            netcom.ClientLoggedOut -= netcom_ClientLoggedOut;
            netcom.ClientDisconnected -= netcom_ClientDisconnected;
            netcom.ChatReceived -= netcom_ChatReceived;
            netcom.ChatSent -= netcom_ChatSent;
            netcom.AlertMessageReceived -= netcom_AlertMessageReceived;
            netcom.InstantMessageReceived -= netcom_InstantMessageReceived;
            client.Groups.CurrentGroups -= Groups_OnCurrentGroups;
            instance.Config.ConfigApplied -= Config_ConfigApplied;
            client.Friends.FriendOffline -= Friends_OnFriendOffline;
            client.Friends.FriendOnline -= Friends_OnFriendOnline;
        }

        private void Groups_OnCurrentGroups(object sender, CurrentGroupsEventArgs e)
        {
            try
            {
                //this.instance.State.Groups = e.Groups;

                foreach (KeyValuePair<UUID, Group> g in e.Groups)
                {
                    lock (instance.State.Groups)
                    {
                        if (!instance.State.Groups.ContainsKey(g.Key))
                        {
                            instance.State.Groups.Add(g.Key, g.Value);
                        }
                    }
                }

                BeginInvoke(new MethodInvoker(GetGroupsName));
            }
            catch (Exception ex)
            {
                //string exp = ex.Message;
                instance.CrashReporter.Post(ex);
            }
        }

        private void Self_MoneyBalanceReply(object sender, MoneyBalanceReplyEventArgs e)
        {
            if (floading)
            {
                tmoneybalance = e.Balance;
                floading = false;
                return;
            }

            TransactionInfo ti = e.TransactionInfo;

            if (ti.DestID != UUID.Zero && ti.SourceID != UUID.Zero)
            {
                if (instance.Config.CurrentConfig.PlayPaymentReceived)
                {
                    instance.MediaManager.PlayUISound(Properties.Resources.MoneyBeep);
                }
            }

            //tabs["chat"].Highlight();

            int bal = e.Balance - tmoneybalance;

            string ttl = "MEGAbolt Alert";
            string body = string.Empty;

            if (bal > 0)
            {
                if (e.Success)
                {
                    if (ti.DestID != client.Self.AgentID)
                    {
                        body = e.Description;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(e.Description))
                        {
                            string pfrm = string.Empty;

                            if (ti.TransactionType == 5008)
                            {
                                pfrm = " via " + ti.ItemDescription;
                            }

                            body = e.Description + pfrm;

                            body = body.Replace(".", string.Empty);   
                        }
                        else
                        {
                            body = !string.IsNullOrEmpty(ti.ItemDescription) 
                                ? $"You have received a payment of L${ti.Amount.ToString(CultureInfo.CurrentCulture)} from {ti.ItemDescription}" 
                                : $"You have received a payment of L${ti.Amount.ToString(CultureInfo.CurrentCulture)}";
                        }
                    }
                }                

                TrayNotifiy(ttl, body, false);
            }
            else
            {
                ////body = e.Description;
                //if (ti.DestID != client.Self.AgentID)
                //{

                //    if (!String.IsNullOrEmpty(ti.ItemDescription))
                //    {
                //        body = "You paid L$" + ti.Amount.ToString() + " to/for " + ti.ItemDescription;
                //    }
                //    else
                //    {
                //        body = "You paid L$" + ti.Amount.ToString();
                //    }
                //}
                //else
                //{
                //    body = e.Description;
                //}

                if (!String.IsNullOrEmpty(e.Description))
                {
                    body = e.Description;

                    TrayNotifiy(ttl, body, false);
                }
            }

            tmoneybalance = e.Balance;
        }

        //Separate thread
        private void Friends_OnFriendOffline(object sender, FriendInfoEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Friends_OnFriendOffline(sender, e)));
                return;
            }

            if (e.Friend.Name != null)
            {
                if (!instance.Config.CurrentConfig.DisableFriendsNotifications)
                {
                    if (instance.Config.CurrentConfig.PlayFriendOffline)
                    {
                        instance.MediaManager.PlayUISound(Properties.Resources.Friend_Off);
                    }

                    string ttl = "MEGAbolt Alert";
                    string body = $"{e.Friend.Name} is offline";
                    TrayNotifiy(ttl, body, false);
                }
            }
        }

        //Separate thread
        private void Friends_OnFriendOnline(object sender, FriendInfoEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Friends_OnFriendOnline(sender, e)));
                return;
            }

            if (!string.IsNullOrEmpty(e.Friend.Name) && !string.IsNullOrEmpty(avname))
            {
                if (!instance.Config.CurrentConfig.DisableFriendsNotifications)
                {
                    if (instance.Config.CurrentConfig.PlayFriendOnline)
                    {
                        instance.MediaManager.PlayUISound(Properties.Resources.Friend_On);
                    }

                    string ttl = "MEGAbolt Alert";
                    string body = $"{e.Friend.Name} is online";
                    TrayNotifiy(ttl, body, false);
                }
            }
        }

        private void GetGroupsName()
        {
            //this.instance.State.GroupStore.Clear();
            
            foreach (Group group in instance.State.Groups.Values)
            {
                lock (instance.State.GroupStore)
                {
                    if (!instance.State.GroupStore.ContainsKey(group.ID))
                    {
                        instance.State.GroupStore.Add(group.ID, group.Name);
                    }
                }
            }
        }

        private void netcom_ClientLoginStatus(object sender, LoginProgressEventArgs e)
        {
            if (e.Status != LoginStatus.Success) return;

            try
            {
                if (e.Status == LoginStatus.Success)
                {
                    InitializeFriendsTab();
                    InitializeGroupsTab();
                    InitializeInventoryTab();
                    InitializeSearchTab();
                    //InitializeMapTab();
                    InitializeIMboxTab();

                    avname = netcom.LoginOptions.FullName;
                    notifyIcon1.Text = $"MEGAbolt [{avname}]";

                    if (selectedTab.Name == "main")
                        tabs["chat"].Select();

                    //client.Groups.RequestCurrentGroups();
                    client.Self.RetrieveInstantMessages();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("login (tabs console): " + ex.Message, Helpers.LogLevel.Error);
            }
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            TidyUp();

            TrayNotifiy($"MEGAbolt - {avname}", "Logged out");
        }

        private void netcom_ClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            if (e.Reason == NetworkManager.DisconnectType.ClientInitiated) return;

            TidyUp();

            notifyIcon1.Text = $"MEGAbolt - {avname} [Disconnected]";
            TrayNotifiy($"MEGAbolt - {avname}", "Disconnected");
        }

        private void TidyUp()
        {
            DisposeSearchTab();
            DisposeGroupsTab();
            DisposeInventoryTab();
            DisposeFriendsTab();
            DisposeIMboxTab();

            RemoveNetcomEvents();

            tabs["main"].Select();
        }

        private void netcom_AlertMessageReceived(object sender, AlertMessageEventArgs e)
        {
            tabs["chat"].Highlight();

            string ttl = "MEGAbolt Alert";
            string body = e.Message;
            TrayNotifiy(ttl, body);
        }

        private void netcom_ChatSent(object sender, ChatSentEventArgs e)
        {
            tabs["chat"].Highlight();
        }

        private void netcom_ChatReceived(object sender, ChatEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Message)) return;

            // Avoid form flash if RLV command
            if (e.SourceType == ChatSourceType.Object)
            {
                if (e.Message.StartsWith("@", StringComparison.CurrentCultureIgnoreCase)) return;
            }

            tabs["chat"].Highlight();
        }

        public void DisplayOnChat(InstantMessageEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    DisplayOnChat(e);
                }));

                return;
            }

            if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;
            if (e.IM.Message.Contains(instance.Config.CurrentConfig.CommandInID)) return;
            if (e.IM.Message.Contains(instance.Config.CurrentConfig.IgnoreUID)) return;

            BeginInvoke(new MethodInvoker(delegate()
            {
                ChatBufferItem ready = new ChatBufferItem(DateTime.Now,
                           e.IM.FromAgentName + " (" + e.IM.FromAgentID + "): " + e.IM.Message,
                           ChatBufferTextStyle.ObjectChat,
                           null,
                           e.IM.IMSessionID); //added by GM on 3-JUL-2009 - the FromAgentID

                chatConsole.ChatManager.ProcessBufferItem(ready, false);
            }));
        }

        public void DisplayChatScreen(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    DisplayChatScreen(msg);
                }));

                return;
            }

            BeginInvoke(new MethodInvoker(delegate()
            {
                ChatBufferItem ready = new ChatBufferItem(DateTime.Now,
                           msg,
                           ChatBufferTextStyle.Alert,
                           null,
                           UUID.Random()); //added by GM on 3-JUL-2009 - the FromAgentID

                chatConsole.ChatManager.ProcessBufferItem(ready, false);
            }));
        }

        private void netcom_InstantMessageReceived(object sender, InstantMessageEventArgs e)
        {
            //if (instance.IsAvatarMuted(e.IM.FromAgentID)) return;

            switch (e.IM.Dialog)
            {
                case InstantMessageDialog.MessageFromAgent:
                    //if (e.IM.FromAgentID != client.Self.AgentID)
                    //{
                    if (e.IM.FromAgentName.ToLower(CultureInfo.CurrentCulture) == "second life")
                    {
                        DisplayOnChat(e);
                        return;
                    }
                    else if (e.IM.FromAgentID == UUID.Zero)
                    {
                        // Marketplace Received item notification
                        //MessageBox.Show(e.IM.Message, "MEGAbolt");
                        (new frmMBmsg(instance, e.IM.Message)).ShowDialog(this);
                    }
                    else if (e.IM.IMSessionID == UUID.Zero)
                    {
                        if (e.IM.RegionID != UUID.Zero)
                        {
                            // Region message
                            String msg = "Region message from " + e.IM.FromAgentName + Environment.NewLine + Environment.NewLine;
                            msg += @e.IM.Message;

                            //MessageBox.Show(msg, "MEGAbolt");

                            (new frmMBmsg(instance, msg)).ShowDialog(this);
                        }
                        else
                        {
                            HandleIM(e);
                        }
                    }
                    else
                    {
                        HandleIM(e);
                    }
                    //}
                    
                    break;
                case InstantMessageDialog.SessionSend:
                //case InstantMessageDialog.SessionGroupStart:
                    HandleIM(e);
                    break;
                case InstantMessageDialog.MessageFromObject:
                    if (instance.IsObjectMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;
                    if (instance.State.IsBusy) return;
                    DisplayOnChat(e);
                    
                    break;

                case InstantMessageDialog.StartTyping:
                    if (TabExists(e.IM.FromAgentName))
                    {
                        // this is making the window flash and people don't like it
                        // so I am taking it out. LL
                        //METAboltTab tab = tabs[e.IM.FromAgentName.ToLower()];
                        //if (!tab.Highlighted) tab.PartialHighlight();
                    }

                    break;

                case InstantMessageDialog.StopTyping:
                    if (TabExists(e.IM.FromAgentName))
                    {
                        // this is making the window flash and people don't like it
                        // so I am taking it out. LL
                        //METAboltTab tab = tabs[e.IM.FromAgentName.ToLower()];
                        //if (!tab.Highlighted) tab.Unhighlight();
                    }

                    break;

                case InstantMessageDialog.RequestTeleport:
                    if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;
                    HandleTP(e);
                    break;

                case InstantMessageDialog.FriendshipOffered:
                    if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;
                    HandleFriendship(e);
                    break;

                case InstantMessageDialog.ConsoleAndChatHistory:
                    //HandleHistory(e);
                    break;

                case InstantMessageDialog.TaskInventoryOffered:
                case InstantMessageDialog.InventoryOffered:
                    //if (instance.IsAvatarMuted(e.IM.FromAgentID)) return;
                    HandleInventory(e);
                    break;

                case InstantMessageDialog.InventoryAccepted:
                    HandleInventoryReplyAccepted(e);
                    break;

                case InstantMessageDialog.InventoryDeclined:
                    HandleInventoryReplyDeclined(e);
                    break;

                case InstantMessageDialog.GroupInvitation:
                    if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;
                    HandleGroupInvite(e);
                    break;

                case InstantMessageDialog.FriendshipAccepted:
                    HandleFriendshipAccepted(e);
                    break;

                case InstantMessageDialog.FriendshipDeclined:
                    HandleFriendshipDeclined(e);
                    break;

                case InstantMessageDialog.GroupNotice:
                    if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;
                    HandleGroupNoticeReceived(e);
                    break;

                case InstantMessageDialog.GroupInvitationAccept:
                    HandleGroupInvitationAccept(e);
                    break;

                case InstantMessageDialog.GroupInvitationDecline:
                    HandleGroupInvitationDecline(e);
                    break;

                case InstantMessageDialog.MessageBox:
                    if (instance.IsObjectMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;
                    HandleMessageBox(e);
                    break;
            }
        }

        private void TrayNotifiy(string title, string msg)
        {
            if (instance.State.IsBusy) return;

            if (System.Text.RegularExpressions.Regex.IsMatch(msg.ToLower(CultureInfo.CurrentCulture).Trim(), "autopilot", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return;

            notifyIcon1.Text = UpdateIconTitle();

            try
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    //chatConsole.ChatManager.PrintMsg("\n" + msg + "\n");
                    chatConsole.ChatManager.PrintMsg(Environment.NewLine + getTimeStamp() + msg);
                }));
            }
            catch { ; }

            if (!stopnotify)
            {
                notifyIcon1.BalloonTipText = msg;
                notifyIcon1.BalloonTipTitle = $"{title} [{avname}]";
                notifyIcon1.ShowBalloonTip(2000);

                if (instance.Config.CurrentConfig.PlaySound)
                {
                    instance.MediaManager.PlayUISound(Properties.Resources.notify);
                }
            }
        }

        private void TrayNotifiy(string title, string msg, bool makesound)
        {
            if (instance.State.IsBusy) return;

            if (System.Text.RegularExpressions.Regex.IsMatch(msg.ToLower(CultureInfo.CurrentCulture).Trim(), "autopilot", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return;

            notifyIcon1.Text = UpdateIconTitle();

            BeginInvoke(new MethodInvoker(delegate()
            {
                ////chatConsole.ChatManager.PrintMsg("\n" + msg + "\n");
                ////chatConsole.ChatManager.PrintMsg(Environment.NewLine + getTimeStamp() + msg);
                //chatConsole.ChatManager.PrintMsg(Environment.NewLine + msg);
                chatConsole.ChatManager.PrintMsg(msg);
            }));

            if (!stopnotify)
            {
                notifyIcon1.BalloonTipText = msg;
                notifyIcon1.BalloonTipTitle = $"{title} [{avname}]";
                notifyIcon1.ShowBalloonTip(2000);

                if (instance.Config.CurrentConfig.PlaySound && makesound)
                {
                    //System.Media.SystemSounds..Play();
                    instance.MediaManager.PlayUISound(Properties.Resources.notify);
                }
            }
        }

        private string getTimeStamp()
        {
            if (instance.Config.CurrentConfig.ChatTimestamps)
            {
                DateTime dte = DateTime.Now;
                dte = instance.State.GetTimeStamp(dte);

                return dte.ToString("[HH:mm] ", CultureInfo.CurrentCulture);
            }

            return string.Empty;  
        }

        private string UpdateIconTitle()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("MEGAbolt - ");

            if (netcom.IsLoggedIn)
            {
                sb.Append("[" + avname + "]");
            }
            else
            {
                sb.Append(avname + " [Logged out]");
            }

            string title =  sb.ToString();
            sb = null;

            return title;
        }

        private void HandleFriendshipAccepted(InstantMessageEventArgs e)
        {
            if (instance.State.IsBusy) return;

            string fromAgent = e.IM.FromAgentName;

            string ttl = "Friendship offered";
            string body = fromAgent + " has accepted your friendship offer";
            TrayNotifiy(ttl, body); 
        }

        private void HandleFriendshipDeclined(InstantMessageEventArgs e)
        {
            if (instance.State.IsBusy) return;

            string fromAgent = e.IM.FromAgentName;

            string ttl = "Friendship offered";
            string body = fromAgent + " has declined your friendship offer";
            TrayNotifiy(ttl, body);
        }

        private void HandleGroupNoticeReceived(InstantMessageEventArgs e)
        {
            //if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;

            if (instance.Config.CurrentConfig.DisableGroupNotices)
            {
                return;
            }

            if (instance.State.IsBusy) return;

            // Count the ones already on display
            // to avoid flooding

            if (instance.NoticeCount < 9)
            {
                instance.NoticeCount += 1;
            }

            if (instance.NoticeCount < 9)
            {
                (new frmGroupNotice(instance, e)).Show(this);
            }
        }

        private void HandleGroupInvitationAccept(InstantMessageEventArgs e)
        {
            if (instance.State.IsBusy) return;

            string fromAgent = e.IM.FromAgentName;

            string ttl = "Group invitation";
            string body = fromAgent + " has accepted your group invitation";
            TrayNotifiy(ttl, body);
        }

        private void HandleGroupInvitationDecline(InstantMessageEventArgs e)
        {
            if (instance.State.IsBusy) return;

            string fromAgent = e.IM.FromAgentName;

            string ttl = "Group invitation";
            string body = fromAgent + " has declined your group invitation";
            TrayNotifiy(ttl, body);
        }

        private void HandleMessageBox(InstantMessageEventArgs e)
        {
            if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;

            if (instance.State.IsBusy) return;

            //string ttl = "MEGAbolt";
            string body = @e.IM.Message;
            //TrayNotifiy(ttl, body);

            (new frmMBmsg(instance, body)).ShowDialog(this);
        }

        private void HandleIM(InstantMessageEventArgs e)
        {
            if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;
            if (e.IM.Message.Contains(instance.Config.CurrentConfig.CommandInID)) return;
            if (e.IM.Message.Contains(instance.Config.CurrentConfig.IgnoreUID)) return;

            if (instance.IsGiveItem(e.IM.Message.ToLower(CultureInfo.CurrentCulture), e.IM.FromAgentID))
            {
                return;
            }

            if (e.IM.Dialog == InstantMessageDialog.SessionSend)
            {
                lock (instance.State.GroupStore)
                {
                    if (instance.State.GroupStore.ContainsKey(e.IM.IMSessionID))
                    {
                        //if (null != client.Self.MuteList.Find(me => me.Type == MuteType.Group && (me.ID == e.IM.IMSessionID || me.ID == e.IM.FromAgentID))) return;

                        // Check to see if group IMs are disabled
                        if (instance.Config.CurrentConfig.DisableGroupIMs)
                            return;

                        if (instance.State.IsBusy) return;

                        if (TabExists(instance.State.GroupStore[e.IM.IMSessionID]))
                        {
                            MEGAboltTab tab = tabs[instance.State.GroupStore[e.IM.IMSessionID].ToLower(CultureInfo.CurrentCulture)];
                            if (!tab.Selected)
                            {
                                tab.Highlight();
                                tabs["imbox"].PartialHighlight();
                            }

                            return;
                        }
                        else
                        {
                            IMTabWindowGroup imTab = AddIMTabGroup(e);
                            tabs[imTab.TargetName.ToLower()].Highlight();
                            tabs["imbox"].IMboxHighlight();
                            if (tabs[imTab.TargetName.ToLower(CultureInfo.CurrentCulture)].Selected) tabs[imTab.TargetName.ToLower(CultureInfo.CurrentCulture)].Highlight();

                            return;
                        }
                    }
                }

                return;
            }

            if (TabExists(e.IM.FromAgentName))   //if (tabs.ContainsKey(e.IM.FromAgentName.ToLower()))
            {
                if (!tabs[e.IM.FromAgentName.ToLower(CultureInfo.CurrentCulture)].Selected)
                {
                    tabs["imbox"].PartialHighlight();
                }
            }
            else
            {
                tabs["imbox"].IMboxHighlight();
            }

            if (instance.MainForm.WindowState == FormWindowState.Minimized)
            {
                if (!stopnotify)
                {
                    string ttl = string.Empty;

                    avname = netcom.LoginOptions.FullName;

                    if (instance.State.GroupStore.ContainsKey(e.IM.IMSessionID))
                    {
                        ttl = "Group IM notification [" + avname + "]";
                    }
                    else
                    {
                        ttl = "IM notification [" + avname + "]";
                    }

                    string imsg = e.IM.Message;

                    if (imsg.Length > 125)
                    {
                        imsg = imsg.Substring(0, 125) + "...";
                    }

                    string body = e.IM.FromAgentName + ": " + imsg;

                    Notification notifForm = new Notification();
                    notifForm.Message = body;
                    notifForm.Title = ttl;
                    notifForm.Show();
                }
            }
            lock (instance.State.GroupStore)
            {
                if (instance.State.GroupStore.ContainsKey(e.IM.IMSessionID))
                {
                    //if (null != client.Self.MuteList.Find(me => me.Type == MuteType.Group && (me.ID == e.IM.IMSessionID || me.ID == e.IM.FromAgentID))) return;

                    // Check to see if group IMs are disabled
                    if (instance.Config.CurrentConfig.DisableGroupIMs)
                    {
                        Group grp = instance.State.Groups[e.IM.IMSessionID];
                        client.Self.RequestLeaveGroupChat(grp.ID);
                        return;
                    }

                    if (instance.State.IsBusy)
                    {
                        Group grp = instance.State.Groups[e.IM.IMSessionID];
                        client.Self.RequestLeaveGroupChat(grp.ID);
                        return;
                    }

                    if (TabExists(instance.State.GroupStore[e.IM.IMSessionID]))
                    {
                        MEGAboltTab tab = tabs[instance.State.GroupStore[e.IM.IMSessionID].ToLower(CultureInfo.CurrentCulture)];
                        if (!tab.Selected) tab.PartialHighlight();
                        //Logger.Log("Stored|ExistingGroupTab:: " + e.IM.Message, Helpers.LogLevel.Debug);
                        return;
                    }
                    else
                    {
                        //create a new tab
                        IMTabWindowGroup imTab = AddIMTabGroup(e);
                        tabs[imTab.TargetName.ToLower(CultureInfo.CurrentCulture)].Highlight();

                        if (instance.Config.CurrentConfig.PlayGroupIMreceived)
                        {
                            instance.MediaManager.PlayUISound(Properties.Resources.Group_Im_received);
                        }

                        //Logger.Log("Stored|NewGroupTab:: " + e.IM.Message, Helpers.LogLevel.Debug);
                        return;
                    }
                }
                else
                {
                    if (TabExists(e.IM.FromAgentName))
                    {
                        MEGAboltTab tab = tabs[e.IM.FromAgentName.ToLower(CultureInfo.CurrentCulture)];
                        if (!tab.Selected) tab.PartialHighlight();
                        return;
                    }
                    else
                    {
                        IMTabWindow imTab = AddIMTab(e);
                        tabs[imTab.TargetName.ToLower(CultureInfo.CurrentCulture)].Highlight();

                        if (instance.Config.CurrentConfig.InitialIMReply.Length > 0)
                        {
                            client.Self.InstantMessage(e.IM.FromAgentID, instance.Config.CurrentConfig.InitialIMReply);
                        }

                        if (instance.Config.CurrentConfig.PlayIMreceived)
                        {
                            instance.MediaManager.PlayUISound(Properties.Resources.IM_received);
                        }
                    }
                }
            }
        }

        private void HandleHistory(InstantMessageEventArgs e)
        {
            //string msg =  e.IM.Message;
            //if (TabExists(e.IM.FromAgentName))
            //{
            //    METAboltTab tab = tabs[e.IM.FromAgentName.ToLower()];
            //    if (!tab.Selected) tab.Highlight();
            //    return;
            //}
            //else
            //{
            //    IMTabWindow imTab = AddIMTab(e);
            //    tabs[imTab.TargetName.ToLower()].Highlight();
            //}
        }
        private void HandleTP(InstantMessageEventArgs e)
        {
            if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;

            if (instance.State.IsBusy)
            {
                string responsemsg = instance.Config.CurrentConfig.BusyReply;
                client.Self.InstantMessage(client.Self.Name, e.IM.FromAgentID, responsemsg, 
                    e.IM.IMSessionID, InstantMessageDialog.BusyAutoResponse, InstantMessageOnline.Offline, 
                    instance.SIMsittingPos(), UUID.Zero, Array.Empty<byte>()); 
                return;
            }

            string fromAgentID = e.IM.FromAgentID.ToString();
            string fromAgent = e.IM.FromAgentName;

            if (TabExists(fromAgentID))
                tabs[fromAgentID].Close();

            TPTabWindow tpTab = AddTPTab(e);
            tabs[tpTab.TargetUUID.ToString()].Highlight();

            string ttl = "MEGAbolt";
            string body = $"You have received a Teleport request from {fromAgent}";
            TrayNotifiy(ttl, body);
        }

        private void HandleFriendship(InstantMessageEventArgs e)
        {
            if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;

            if (instance.State.IsBusy)
            {
                string responsemsg = instance.Config.CurrentConfig.BusyReply;
                client.Self.InstantMessage(client.Self.Name, e.IM.FromAgentID, responsemsg, 
                    e.IM.IMSessionID, InstantMessageDialog.BusyAutoResponse, InstantMessageOnline.Offline,
                    instance.SIMsittingPos(), UUID.Zero, Array.Empty<byte>());
                return;
            }

            string fromAgentID = e.IM.FromAgentID.ToString();
            string fromAgent = e.IM.FromAgentName;

            if (TabExists(fromAgentID))
                tabs[fromAgentID].Close();

            if (instance.Config.CurrentConfig.AutoAcceptFriends)
            {
                client.Friends.AcceptFriendship(e.IM.FromAgentID, e.IM.IMSessionID);
                return;
            }

            FRTabWindow frTab = AddFRTab(e);
            tabs[frTab.TargetUUID.ToString()].Highlight();

            string ttl = "MEGAbolt";
            string body = "You have received a Friendship offer " + fromAgent;
            TrayNotifiy(ttl, body);
        }

        private void HandleInventory(InstantMessageEventArgs e)
        {
            //if (e.IM.Dialog == InstantMessageDialog.TaskInventoryOffered)
            //{
            //    if (instance.IsObjectMuted(e.IM.FromAgentID, e.IM.FromAgentName))
            //        return;
            //}
            //else
            //{
            //    if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName))
            //        return;
            //}

            //if (instance.IsAvatarMuted(e.IM.FromAgentID, MuteType.Object))
            //    return;

            //if (instance.State.IsBusy)
            //{
            //    string responsemsg = this.instance.Config.CurrentConfig.BusyReply;
            //    client.Self.InstantMessage(client.Self.Name, e.IM.FromAgentID, responsemsg, e.IM.IMSessionID, InstantMessageDialog.BusyAutoResponse, InstantMessageOnline.Offline, instance.SIMsittingPos(), UUID.Zero, new byte[0]);
            //    return;
            //}

            if (instance.IsObjectMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;

            AssetType type = (AssetType)e.IM.BinaryBucket[0];

            if (type == AssetType.Unknown) return;

            UUID oID = UUID.Zero;

            if (e.IM.BinaryBucket.Length == 17)
            {
                oID = new UUID(e.IM.BinaryBucket, 1);
            }

            UUID invfolder = UUID.Zero;

            if (type == AssetType.Folder)
            {
                invfolder = client.Inventory.Store.RootFolder.UUID;
            }
            else
            {
                invfolder = client.Inventory.FindFolderForType(type);
            }

            if (!instance.Config.CurrentConfig.DeclineInv)
            {
                try
                {
                    if (e.IM.BinaryBucket.Length > 0)
                    {
                        if (instance.Config.CurrentConfig.AutoAcceptItems)
                        {
                            if (e.IM.Dialog == InstantMessageDialog.InventoryOffered)
                            {
                                client.Self.InstantMessage(client.Self.Name, e.IM.FromAgentID, string.Empty, e.IM.IMSessionID, InstantMessageDialog.InventoryAccepted, InstantMessageOnline.Offline, instance.SIMsittingPos(), client.Network.CurrentSim.RegionID, invfolder.GetBytes());   //new byte[0]); // Accept Inventory Offer
                            }
                            else if (e.IM.Dialog == InstantMessageDialog.TaskInventoryOffered)
                            {
                                client.Self.InstantMessage(client.Self.Name, e.IM.FromAgentID, string.Empty, e.IM.IMSessionID, InstantMessageDialog.TaskInventoryAccepted, InstantMessageOnline.Offline, instance.SIMsittingPos(), client.Network.CurrentSim.RegionID, invfolder.GetBytes());   //new byte[0]); // Accept Inventory Offer
                            }

                            client.Inventory.RequestFetchInventory(oID, client.Self.AgentID);

                            string ttl = "MEGAbolt Alert";
                            string body = $"{e.IM.FromAgentName} has given you a {type} named {e.IM.Message}";

                            TrayNotifiy(ttl, body, false);

                            return;
                        }

                        //(new frmInvOffered(instance, e.IM, oID, type)).Show(this);
                        (new frmInvOffered(instance, e.IM, oID, type)).Show(this);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Inventory Received error: " + ex.Message, Helpers.LogLevel.Error);
                    //reporter.Show(ex);
                }
            }
            else
            {
                if (e.IM.BinaryBucket.Length > 0)
                {
                    if (e.IM.Dialog == InstantMessageDialog.InventoryOffered)
                    {
                        client.Self.InstantMessage(client.Self.Name, e.IM.FromAgentID, string.Empty, e.IM.IMSessionID, InstantMessageDialog.InventoryDeclined, InstantMessageOnline.Offline, instance.SIMsittingPos(), client.Network.CurrentSim.RegionID, invfolder.GetBytes()); // Decline Inventory Offer

                        try
                        {
                            //client.Inventory.RemoveItem(objectID);
                            //client.Inventory.RequestFetchInventory(oID, client.Self.AgentID);

                            InventoryBase item = client.Inventory.Store.Items[oID].Data;
                            UUID content = client.Inventory.FindFolderForType(FolderType.Trash);

                            InventoryFolder folder = (InventoryFolder)client.Inventory.Store.Items[content].Data;

                            if (type != AssetType.Folder)
                            {
                                client.Inventory.Move(item, folder);
                            }
                            else
                            {
                                client.Inventory.MoveFolder(oID, content);
                            }
                        }
                        catch { ; }
                    }
                    else if (e.IM.Dialog == InstantMessageDialog.TaskInventoryOffered)
                    {
                        client.Self.InstantMessage(client.Self.Name, e.IM.FromAgentID, string.Empty, e.IM.IMSessionID, InstantMessageDialog.TaskInventoryDeclined, InstantMessageOnline.Offline, instance.SIMsittingPos(), client.Network.CurrentSim.RegionID, invfolder.GetBytes()); // Decline Inventory Offer
                    }
                }
            }
        }

        private void HandleInventoryReplyAccepted(InstantMessageEventArgs e)
        {
            if (instance.State.IsBusy) return;

            string ttl = "MEGAbolt";
            string body = $"{e.IM.FromAgentName} has accepted your inventory offer";
            TrayNotifiy(ttl, body);
        }

        private void HandleInventoryReplyDeclined(InstantMessageEventArgs e)
        {
            if (instance.State.IsBusy) return;

            string ttl = "MEGAbolt";
            string body = $"{e.IM.FromAgentName} has declined your inventory offer";
            TrayNotifiy(ttl, body);
        }

        private void HandleGroupInvite(InstantMessageEventArgs e)
        {
            if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;

            if (instance.Config.CurrentConfig.DisableInboundGroupInvites)
                return;

            if (instance.State.IsBusy) return;

            string fromAgentID = e.IM.FromAgentID.ToString();

            if (TabExists(fromAgentID))
                tabs[fromAgentID].Close();

            GRTabWindow grTab = AddGRTab(e);
            tabs[grTab.TargetUUID.ToString()].Highlight();

            string ttl = "MEGAbolt";
            string body = "You have received a group invite";
            TrayNotifiy(ttl, body);
        }

        private void InitializeMainTab()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    InitializeMainTab();
                    //client.Self.RetrieveInstantMessages();
                }));

                return;
            }

            MainConsole mainConsole = new MainConsole(instance);
            mainConsole.Dock = DockStyle.Fill;
            mainConsole.Visible = false;

            toolStripContainer1.ContentPanel.Controls.Add(mainConsole);

            MEGAboltTab tab = AddTab("main", "Main", mainConsole);
            tab.AllowClose = false;
            tab.AllowDetach = false;
            tab.AllowMerge = false;

            mainConsole.RegisterTab(tab);

            ToolStripItem item = new ToolStripSeparator();

            tstTabs.Items.Add(item);
        }

        private void InitializeChatTab()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(InitializeChatTab));

                return;
            }

            chatConsole = new ChatConsole(instance);
            chatConsole.Dock = DockStyle.Fill;
            chatConsole.Visible = false;

            toolStripContainer1.ContentPanel.Controls.Add(chatConsole);

            MEGAboltTab tab = AddTab("chat", "Chat", chatConsole);
            tab.AllowClose = false;
            tab.AllowDetach = false;

            ToolStripItem item = new ToolStripSeparator();

            tstTabs.Items.Add(item);
        }

        private void InitializeFriendsTab()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(InitializeFriendsTab));

                return;
            }

            FriendsConsole friendsConsole = new FriendsConsole(instance);
            friendsConsole.Dock = DockStyle.Fill;
            friendsConsole.Visible = false;

            toolStripContainer1.ContentPanel.Controls.Add(friendsConsole);

            MEGAboltTab tab = AddTab("friends", "Friends", friendsConsole);
            tab.AllowClose = false;
            tab.AllowDetach = true;

            ToolStripItem item = new ToolStripSeparator();

            tstTabs.Items.Add(item);
        }

        private void InitializeIMboxTab()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(InitializeIMboxTab));

                return;
            }

            IMbox imboxConsole = new IMbox(instance);
            imboxConsole.Dock = DockStyle.Fill;
            imboxConsole.Visible = false;

            toolStripContainer1.ContentPanel.Controls.Add(imboxConsole);

            MEGAboltTab tab = AddTab("imbox", "IMbox", imboxConsole);
            tab.AllowClose = false;
            tab.AllowDetach = true;

            ToolStripItem item = new ToolStripSeparator();

            tstTabs.Items.Add(item);

            ToolStripItem item1 = new ToolStripSeparator();
            tstTabs.Items.Add(item1);
        }

        private void InitializeGroupsTab()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(InitializeGroupsTab));

                return;
            }

            try
            {
                GroupsConsole groupsConsole = new GroupsConsole(instance)
                {
                    Dock = DockStyle.Fill,
                    Visible = false
                };

                toolStripContainer1.ContentPanel.Controls.Add(groupsConsole);

                MEGAboltTab tab = AddTab("groups", "Groups", groupsConsole);
                tab.AllowClose = false;
                tab.AllowDetach = true;
            }
            catch (Exception ex)
            {
                //Logger.Log("Group tab error: " + ex.Message, Helpers.LogLevel.Error);
                instance.CrashReporter.Post(ex);
            }

            ToolStripItem item = new ToolStripSeparator();

            tstTabs.Items.Add(item);
        }

        private void InitializeSearchTab()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(InitializeSearchTab));

                return;
            }

            SearchConsole searchConsole = new SearchConsole(instance);
            searchConsole.Dock = DockStyle.Fill;
            searchConsole.Visible = false;

            toolStripContainer1.ContentPanel.Controls.Add(searchConsole);

            MEGAboltTab tab = AddTab("search", "Search", searchConsole);
            tab.AllowClose = false;
            tab.AllowDetach = false;

            ToolStripItem item = new ToolStripSeparator();

            tstTabs.Items.Add(item);
        }

        private void InitializeInventoryTab()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(InitializeInventoryTab));

                return;
            }

            InventoryConsole invConsole = new InventoryConsole(instance)
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            toolStripContainer1.ContentPanel.Controls.Add(invConsole);

            MEGAboltTab tab = AddTab("inventory", "Inventory", invConsole);
            tab.AllowClose = false;
            tab.AllowDetach = true;

            ToolStripItem item = new ToolStripSeparator();

            tstTabs.Items.Add(item);
        }

        private void DisposeFriendsTab()
        {
            ForceCloseTab("friends");
        }

        private void DisposeGroupsTab()
        {
            ForceCloseTab("groups");
        }

        private void DisposeSearchTab()
        {
            ForceCloseTab("search");
        }

        private void DisposeIMboxTab()
        {
            ForceCloseTab("imbox");
        }

        //private void DisposeMapTab()
        //{
        //    ForceCloseTab("map");
        //}

        private void DisposeInventoryTab()
        {
            ForceCloseTab("inventory");
        }

        private void ForceCloseTab(string name)
        {
            if (!TabExists(name)) return;

            MEGAboltTab tab = tabs[name];
            if (tab.Merged) SplitTab(tab);

            tab.AllowClose = true;
            tab.Close();
            tab = null;
        }

        public void AddTab(MEGAboltTab tab)
        {
            ToolStripButton button = (ToolStripButton)tstTabs.Items.Add(tab.Label);
            button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            button.Image = null;
            button.AutoToolTip = false;
            button.Tag = tab.Name;
            button.Click += TabButtonClick;

            tab.Button = button;

            if (!tabs.ContainsKey(tab.Name))
            {
                tabs.Add(tab.Name, tab);
            }
        }

        public MEGAboltTab AddTab(string name, string label, Control control)
        {
            ToolStripButton button = (ToolStripButton)tstTabs.Items.Add("&"+label);
            button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            button.Image = null;
            button.AutoToolTip = false;
            button.Tag = name.ToLower(CultureInfo.CurrentCulture);
            button.Click += TabButtonClick;

            MEGAboltTab tab = new MEGAboltTab(button, control, name.ToLower(CultureInfo.CurrentCulture), label);
            tab.TabAttached += tab_TabAttached;
            tab.TabDetached += tab_TabDetached;
            tab.TabSelected += tab_TabSelected;
            tab.TabClosed += tab_TabClosed;

            if (!tabs.ContainsKey(tab.Name))
            {
                tabs.Add(name.ToLower(CultureInfo.CurrentCulture), tab);
            }

            //ToolStripItem item = new ToolStripSeparator();

            //tstTabs.Items.Add(item);

            return tab;
        }

        private void tab_TabAttached(object sender, EventArgs e)
        {
            MEGAboltTab tab = (MEGAboltTab)sender;
            tab.Select();
        }

        private void tab_TabDetached(object sender, EventArgs e)
        {
            MEGAboltTab tab = (MEGAboltTab)sender;
            frmDetachedTab form = (frmDetachedTab)tab.Owner;

            form.ReattachStrip = tstTabs;
            form.ReattachContainer = toolStripContainer1.ContentPanel;
        }

        private void tab_TabSelected(object sender, EventArgs e)
        {
            MEGAboltTab tab = (MEGAboltTab)sender;

            if (selectedTab != null &&
                selectedTab != tab)
            { selectedTab.Deselect(); }
            
            selectedTab = tab;

            tbtnCloseTab.Enabled = tab.AllowClose;
            owner.AcceptButton = tab.DefaultControlButton;
        }

        private void tab_TabClosed(object sender, EventArgs e)
        {
            MEGAboltTab tab = (MEGAboltTab)sender;

            if (tabs.ContainsKey(tab.Name))
            {
                tabs.Remove(tab.Name);
            }

            tab = null;
        }

        private void TabButtonClick(object sender, EventArgs e)
        {
            ToolStripButton button = (ToolStripButton)sender;

            MEGAboltTab tab = tabs[button.Tag.ToString()];
            tab.Select();

            //METAboltTab stab = tab.GetTab("IMbox");

            if (button.Tag.ToString() != "main" && button.Tag.ToString() != "chat" && button.Tag.ToString() != "friends" && button.Tag.ToString() != "groups" && button.Tag.ToString() != "inventory" && button.Tag.ToString() != "search" && button.Tag.ToString() != "imbox")
            {
                string tabname = button.Tag.ToString();

                //if (button.Tag.ToString().StartsWith("IM"))
                //{
                //    tabname = button.Tag.ToString().Substring(3).Trim();
                //}
                //if (button.Tag.ToString().StartsWith("GIM"))
                //{
                //    tabname = button.Tag.ToString().Substring(4).Trim();
                //}

                if (!instance.ReadIMs)
                {
                    IMbox imtab = instance.imBox;
                    imtab.IMRead(tabname);
                }
            }
        }

        public void RemoveTabEntry(string tabname)
        {
            if (tabs.ContainsKey(tabname))
            {
                tabs.Remove(tabname);
            }
        }

        public void RemoveTabEntry(MEGAboltTab tab)
        {
            tab.Button.Dispose();

            if (tabs.ContainsKey(tab.Name))
            {
                tabs.Remove(tab.Name);
            }
        }

        //Used for outside classes that have a reference to TabsConsole
        public void SelectTab(string name)
        {
            tabs[name.ToLower(CultureInfo.CurrentCulture)].Select();
        }

        public bool TabExists(string name)
        {
            return tabs.ContainsKey(name.ToLower(CultureInfo.CurrentCulture));
        }

        public MEGAboltTab GetTab(string name)
        {
            return tabs[name.ToLower(CultureInfo.CurrentCulture)];
        }

        public void DisplayOnIM(IMTabWindow imTab, InstantMessageEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    DisplayOnIM(imTab, e);
                }));

                return;
            }

            imTab.TextManager.ProcessIM(e);
        }

        public void DisplayOnIMGroup(IMTabWindowGroup imTab, InstantMessageEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    DisplayOnIMGroup(imTab, e);
                }));

                return;
            }

            imTab.TextManager.ProcessIM(e);
        }

        public List<MEGAboltTab> GetOtherTabs()
        {
            return (from ToolStripItem item in tstTabs.Items 
                where item.Tag != null 
                where (ToolStripButton)item != selectedTab.Button 
                select tabs[item.Tag.ToString()] into tab 
                where tab.AllowMerge where !tab.Merged 
                select tab).ToList();
        }

        public IMTabWindow AddIMTab(InstantMessageEventArgs e)
        {
            TabAgentName = e.IM.FromAgentName;
            IMTabWindow imTab = AddIMTab(e.IM.FromAgentID, e.IM.IMSessionID, TabAgentName);

            DisplayOnIM(imTab, e); 

            return imTab;
        }

        public IMTabWindow AddIMTab(UUID target, UUID session, string targetName)
        {
            IMTabWindow imTab = new IMTabWindow(instance, target, session, targetName);
            imTab.Dock = DockStyle.Fill;
            toolStripContainer1.ContentPanel.Controls.Add(imTab);

            string tname = targetName;

            if (tname.Length > 9)
            {
                tname = tname.Substring(0, 7) + "..."; 
            }
            
            AddTab(targetName, "IM: " + tname, imTab);
            imTab.SelectIMInput();

            return imTab;
        }

        public IMTabWindowGroup AddIMTabGroup(InstantMessageEventArgs e)
        {
            TabAgentName = instance.State.GroupStore[e.IM.IMSessionID];
            Group grp = instance.State.Groups[e.IM.IMSessionID];

            //UUID gsession = new UUID(e.IM.BinaryBucket, 2);

            IMTabWindowGroup imTab = AddIMTabGroup(e.IM.FromAgentID, e.IM.IMSessionID, TabAgentName, grp);

            DisplayOnIMGroup(imTab, e);
            //imTab.TextManager.ProcessIM(e);

            return imTab;
        }

        public IMTabWindowGroup AddIMTabGroup(UUID target, UUID session, string targetName, Group grp)
        {
            IMTabWindowGroup imTab = new IMTabWindowGroup(instance, session, target, targetName, grp);
            imTab.Dock = DockStyle.Fill;
            toolStripContainer1.ContentPanel.Controls.Add(imTab);

            string tname = targetName;

            if (tname.Length > 9)
            {
                tname = tname.Substring(0, 7) + "...";
            }
            
            AddTab(targetName, $"GIM: {targetName}", imTab);
            imTab.SelectIMInput();

            return imTab;
        }

        public TPTabWindow AddTPTab(InstantMessageEventArgs e)
        {
            TPTabWindow tpTab = new TPTabWindow(instance, e);
            tpTab.Dock = DockStyle.Fill;

            toolStripContainer1.ContentPanel.Controls.Add(tpTab);
            AddTab(tpTab.TargetUUID.ToString(), $"TP: {tpTab.TargetName}", tpTab);

            return tpTab;
        }

        public FRTabWindow AddFRTab(InstantMessageEventArgs e)
        {
            FRTabWindow frTab = new FRTabWindow(instance, e);
            frTab.Dock = DockStyle.Fill;

            toolStripContainer1.ContentPanel.Controls.Add(frTab);
            AddTab(frTab.TargetUUID.ToString(), $"FR: {frTab.TargetName}", frTab);

            return frTab;
        }

        //public IITabWindow AddIITab(InstantMessageEventArgs e)
        //{
        //    IITabWindow iiTab = new IITabWindow(instance, e);
        //    iiTab.Dock = DockStyle.Fill;

        //    toolStripContainer1.ContentPanel.Controls.Add(iiTab);
        //    METAboltTab tab = AddTab(iiTab.TargetUUID.ToString(), "II: " + iiTab.TargetName, iiTab);

        //    return iiTab;
        //}

        public GRTabWindow AddGRTab(InstantMessageEventArgs e)
        {
            GRTabWindow grTab = new GRTabWindow(instance, e);
            grTab.Dock = DockStyle.Fill;

            toolStripContainer1.ContentPanel.Controls.Add(grTab);
            AddTab(grTab.TargetUUID.ToString(), $"GR: {grTab.TargetName}", grTab);

            return grTab;
        }

        private void tbtnTabOptions_Click(object sender, EventArgs e)
        {
            tmnuMergeWith.Enabled = selectedTab.AllowMerge;
            tmnuDetachTab.Enabled = selectedTab.AllowDetach;

            tmnuMergeWith.DropDown.Items.Clear();

            if (!selectedTab.AllowMerge) return;
            if (!selectedTab.Merged)
            {
                tmnuMergeWith.Text = "Merge With";

                List<MEGAboltTab> otherTabs = GetOtherTabs();

                tmnuMergeWith.Enabled = (otherTabs.Count > 0);
                if (!tmnuMergeWith.Enabled) return;

                foreach (MEGAboltTab tab in otherTabs)
                {
                    ToolStripItem item = tmnuMergeWith.DropDown.Items.Add(tab.Label);
                    item.Tag = tab.Name;
                    item.Click += MergeItemClick;
                }
            }
            else
            {
                tmnuMergeWith.Text = "Split";
                tmnuMergeWith.Click += SplitClick;
            }
        }

        private void MergeItemClick(object sender, EventArgs e)
        {
            ToolStripItem item = (ToolStripItem)sender;
            MEGAboltTab tab = tabs[item.Tag.ToString()];

            selectedTab.MergeWith(tab);

            SplitContainer container = (SplitContainer)selectedTab.Control;
            toolStripContainer1.ContentPanel.Controls.Add(container);

            selectedTab.Select();
            RemoveTabEntry(tab);

            if (!tabs.ContainsKey(tab.Name))
            {
                tabs.Add(tab.Name, selectedTab);
            }
        }

        private void SplitClick(object sender, EventArgs e)
        {
            SplitTab(selectedTab);
            selectedTab.Select();
        }

        public void SplitTab(MEGAboltTab tab)
        {
            MEGAboltTab otherTab = tab.Split();
            if (otherTab == null) return;

            toolStripContainer1.ContentPanel.Controls.Add(tab.Control);
            toolStripContainer1.ContentPanel.Controls.Add(otherTab.Control);

            if (tabs.ContainsKey(tab.Name))
            {
                tabs.Remove(otherTab.Name);
            }
            AddTab(otherTab);
        }

        private void tmnuDetachTab_Click(object sender, EventArgs e)
        {
            if (!selectedTab.AllowDetach) return;

            tstTabs.Items.Remove(selectedTab.Button);
            selectedTab.Detach(instance);
            selectedTab.Owner.Show();

            tabs["chat"].Select();
        }

        private void tbtnCloseTab_Click(object sender, EventArgs e)
        {
            MEGAboltTab tab = selectedTab;

            tabs["chat"].Select();
            tab.Close();
            
            tab = null;
        }

        private void TabsConsole_Load(object sender, EventArgs e)
        {
            owner = FindForm();
        }

        private void tstTabs_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            instance.State.CurrentTab = e.ClickedItem.Text;
        }

        private void toolStripContainer1_ContentPanel_Load(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (instance.MainForm.WindowState == FormWindowState.Normal)
            {
                
                instance.MainForm.Hide();
                instance.MainForm.WindowState = FormWindowState.Minimized;
            }
            else
            {
                instance.MainForm.Show();
                instance.MainForm.WindowState = FormWindowState.Normal;
            }
        }

        private void restoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.MainForm.Show();
            instance.MainForm.WindowState = FormWindowState.Normal;  
        }

        private void closeMEGAboltToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.MainForm.Close(); 
        }

        private void TabsConsole_SizeChanged(object sender, EventArgs e)
        {

        }

        private void TabsConsole_KeyUp(object sender, KeyEventArgs e)
        {
            
        }
    }
}
