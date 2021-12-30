/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2009-2014, Radegast Development Team
 * Copyright(c) 2016-2021, Sjofn, LLC
 * All rights reserved.
 *  
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.ComponentModel;
using OpenMetaverse;
using System.Net.NetworkInformation;
using MEGAbolt;
using System.Management;

namespace MEGAbolt.NetworkComm
{
    /// <summary>
    /// SLNetCom is a class built on top of OpenMetaverse that provides a way to
    /// raise events on the proper thread (for GUI apps especially).
    /// </summary>
    public partial class MEGAboltNetcom
    {
        private GridClient client;
        private MEGAboltInstance instance;

        private const string MainGridLogin = @"https://login.agni.lindenlab.com/cgi-bin/login.cgi";
        private const string BetaGridLogin = @"https://login.aditi.lindenlab.com/cgi-bin/login.cgi";

        

        public MEGAboltNetcom(MEGAboltInstance instance)
        {
            client = instance.Client;
            this.instance = instance;
            LoginOptions = new LoginOptions();

            AddClientEvents();
        }

        private void AddClientEvents()
        {
            client.Self.ChatFromSimulator += Self_OnChat;
            client.Self.IM += Self_OnInstantMessage;
            client.Self.ScriptDialog += Self_OnDialog;
            client.Self.MoneyBalance += Avatar_OnBalanceUpdated;
            client.Self.TeleportProgress += Self_OnTeleport;
            //client.Network.Connected += new NetworkManager.ConnectedCallback(Network_OnConnected);
            client.Network.Disconnected += Network_OnDisconnected;
            client.Network.LoginProgress += Network_OnLogin;
            client.Network.LoggedOut += Network_OnLogoutReply;
            client.Self.ScriptQuestion += Self_OnDialogQuestion;
            client.Self.LoadURL += Self_OnURLLoad;
            client.Self.AlertMessage += Self_AlertMessage;
        }

        private void Self_OnInstantMessage(object sender, InstantMessageEventArgs ea)
        {
            try
            {
                if (NetcomSync != null)
                    NetcomSync.BeginInvoke(new OnInstantMessageRaise(OnInstantMessageReceived), new object[] { ea });
                else
                    OnInstantMessageReceived(ea);

            }
            catch (Exception exp)
            {
                Logger.Log(exp.Message.ToString(), Helpers.LogLevel.Error);
            }
        }

        private void Network_OnLogin(object sender, LoginProgressEventArgs ea)
        {
            try
            {
                if (ea.Status == LoginStatus.Success)
                {
                    IsLoggedIn = true;
                    client.Self.RequestBalance();
                }

                if (ea.Status == LoginStatus.Failed)
                {
                    instance.EndCrashRep();
                }

                if (NetcomSync != null)
                {
                    NetcomSync.BeginInvoke(new OnClientLoginRaise(OnClientLoginStatus), new object[] { ea });
                }
                else
                {
                    OnClientLoginStatus(ea);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("SLnetcomm (onlogin) " + ex.Message, Helpers.LogLevel.Error);
            }
        }

        private void Network_OnLogoutReply(object sender, LoggedOutEventArgs ea)
        {
            try
            {
                IsLoggedIn = false;

                if (NetcomSync != null)
                    NetcomSync.BeginInvoke(new OnClientLogoutRaise(OnClientLoggedOut), new object[] { EventArgs.Empty });
                else
                    OnClientLoggedOut(EventArgs.Empty);
            }
            catch
            {
                ;
            }
        }

        private void Self_OnTeleport(object sender, TeleportEventArgs ea)
        {
            try
            {
                if (ea.Status is TeleportStatus.Finished or TeleportStatus.Failed)
                    IsTeleporting = false;

                if (NetcomSync != null)
                    NetcomSync.BeginInvoke(new OnTeleportStatusRaise(OnTeleportStatusChanged), new object[] { ea });
                else
                    OnTeleportStatusChanged(ea);
            }
            catch
            {
                ;
            }
        }

        private void Self_OnChat(object sender, ChatEventArgs ea)
        {
            try
            {
                if (NetcomSync != null)
                    NetcomSync.BeginInvoke(new OnChatRaise(OnChatReceived), new object[] { ea });
                else
                    OnChatReceived(ea);
            }
            catch
            {
                ;
            }
        }

        private void Self_OnDialog(object sender, ScriptDialogEventArgs ea)
        {
            try
            {
                if (NetcomSync != null)
                    NetcomSync.BeginInvoke(new OnScriptDialogRaise(OnScriptDialogReceived), new object[] { ea });
                else
                    OnScriptDialogReceived(ea);
            }
            catch
            {
                ;
            }
        }

        private void Self_OnDialogQuestion(object sender, ScriptQuestionEventArgs ea)
        {
            try
            {
                if (NetcomSync != null)
                    NetcomSync.BeginInvoke(new OnScriptQuestionRaise(OnScriptQuestionReceived), new object[] { ea });
                else
                    OnScriptQuestionReceived(ea);
            }
            catch
            {
                ;
            }
        }

        private void Self_OnURLLoad(object sender, LoadUrlEventArgs ea)
        {
            if (NetcomSync != null)
                NetcomSync.BeginInvoke(new OnLoadURLRaise(OnLoadURL), new object[] { ea });
            else
                OnLoadURL(ea);
        }

        private void Network_OnDisconnected(object sender, DisconnectedEventArgs ea)
        {
            if (!IsLoggedIn) return;

            IsLoggedIn = false;

            instance.EndCrashRep();

            try
            {
                if (NetcomSync != null)
                    NetcomSync.BeginInvoke(new OnClientDisconnectRaise(OnClientDisconnected), new object[] { ea });
                else
                    OnClientDisconnected(ea);
            }
            catch
            {
                ;
            }
        }

        private void Avatar_OnBalanceUpdated(object sender, BalanceEventArgs ea)
        {
            try
            {
                if (NetcomSync != null)
                    NetcomSync.BeginInvoke(new OnMoneyBalanceRaise(OnMoneyBalanceUpdated), new object[] { ea });
                else
                    OnMoneyBalanceUpdated(ea);
            }
            catch
            {
                ;
            }
        }

        void Self_AlertMessage(object sender, AlertMessageEventArgs ea)
        {
            if (NetcomSync != null)
                NetcomSync.BeginInvoke(new OnAlertMessageRaise(OnAlertMessageReceived), new object[] { ea });
            else
                OnAlertMessageReceived(ea);
        }

        public void Login()
        {
            try
            {
                IsLoggingIn = true;

                LastExecStatus crashrep = instance.HadCrashed();

                instance.StartCrashRep();

                OverrideEventArgs ea = new OverrideEventArgs();
                OnClientLoggingIn(ea);

                if (ea.Cancel)
                {
                    IsLoggingIn = false;
                    return;
                }

                if (string.IsNullOrEmpty(LoginOptions.FirstName) ||
                    string.IsNullOrEmpty(LoginOptions.LastName) ||
                    string.IsNullOrEmpty(LoginOptions.Password))
                {
                    OnClientLoginStatus(
                        new LoginProgressEventArgs(LoginStatus.Failed, "One or more fields are blank.", string.Empty));
                }

                string startLocation = string.Empty;

                switch (LoginOptions.StartLocation)
                {
                    case StartLocationType.Home: startLocation = "home"; break;
                    case StartLocationType.Last: startLocation = "last"; break;

                    case StartLocationType.Custom:
                        startLocation = LoginOptions.StartLocationCustom.Trim();

                        StartLocationParser parser = new StartLocationParser(startLocation);
                        startLocation = NetworkManager.StartLocation(parser.Sim, parser.X, parser.Y, parser.Z);

                        break;
                }

                string password = LoginOptions.SecondLifePassHashIfNecessary(LoginOptions.Password);

                LoginParams loginParams = client.Network.DefaultLoginParams(
                    LoginOptions.FirstName, LoginOptions.LastName, password,
                    LoginOptions.Channel, instance.MEGAbolt_Version);

                loginParams.Start = startLocation;
                loginParams.AgreeToTos = true;

                loginParams.UserAgent = $"{LoginOptions.Channel} {LoginOptions.Version}";
                loginParams.MAC = GetMacAddress();
                loginParams.ID0 = GetId0();
                loginParams.Platform = Environment.OSVersion.VersionString;   // "Windows";

                switch (LoginOptions.Grid)
                {
                    case LoginGrid.MainGrid: client.Settings.LOGIN_SERVER = MainGridLogin; loginParams.URI = MainGridLogin; break;
                    case LoginGrid.BetaGrid: client.Settings.LOGIN_SERVER = BetaGridLogin; loginParams.URI = BetaGridLogin; break;
                    case LoginGrid.Custom: client.Settings.LOGIN_SERVER = LoginOptions.GridCustomLoginUri; loginParams.URI = LoginOptions.GridCustomLoginUri; break;
                }

                client.Network.BeginLogin(loginParams);
            }
            catch (Exception ex)
            {
                Logger.Log("Connection to SL failed", Helpers.LogLevel.Warning, ex);
            }
        }

        public static string GetMacAddress()
        {
            var mac = string.Empty;

            try
            {
                System.Net.NetworkInformation.NetworkInterface[] nics =
                    System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

                if (nics.Length > 0)
                {
                    foreach (NetworkInterface t in nics)
                    {
                        string adapterMac = t.GetPhysicalAddress().ToString().ToUpper();
                        if (adapterMac.Length == 12 && adapterMac != "000000000000")
                        {
                            mac = adapterMac;
                            break;
                        }
                    }
                }
            }
            catch
            {
                Logger.Log("Could not detect MAC address of NIC", Helpers.LogLevel.Info);
                return "00:00:00:00:00:00";
            }

            if (mac.Length < 12)
            {
                mac = UUID.Random().ToString().Substring(24, 12);
            }

            return string.Format("{0}:{1}:{2}:{3}:{4}:{5}",
                mac.Substring(0, 2),
                mac.Substring(2, 2),
                mac.Substring(4, 2),
                mac.Substring(6, 2),
                mac.Substring(8, 2),
                mac.Substring(10, 2));
        }

        public static string GetId0()
        {
            ManagementObjectSearcher searcher = new("SELECT * FROM Win32_PhysicalMedia");
            foreach (ManagementObject wmi_HD in searcher.Get())
            {
                // get the hardware serial no.
                if (wmi_HD["SerialNumber"] != null)
                {
                    return wmi_HD["SerialNumber"].ToString();
                }
            }
            return "Undetected";
        }

        public void Logout()
        {
            if (!IsLoggedIn)
            {
                OnClientLoggedOut(EventArgs.Empty);
                return;
            }

            OverrideEventArgs ea = new OverrideEventArgs();
            OnClientLoggingOut(ea);
            if (ea.Cancel) return;

            client.Network.Logout();
        }

        public void ChatOut(string chat, ChatType type, int channel)
        {
            if (!IsLoggedIn) return;

            client.Self.Chat(chat, channel, type);
            OnChatSent(new ChatSentEventArgs(chat, type, channel));
        }

        public void SendInstantMessage(string message, UUID target, UUID session)
        {
            if (!IsLoggedIn) return;

            //client.Self.InstantMessage(target, message, session);

            client.Self.InstantMessage(
                LoginOptions.FullName, target, message, session, InstantMessageDialog.MessageFromAgent,
                InstantMessageOnline.Online, client.Self.SimPosition, client.Network.CurrentSim.ID, null);

            OnInstantMessageSent(new InstantMessageSentEventArgs(message, target, session, DateTime.Now));
        }

        public void SendInstantMessageGroup(string message, UUID target, UUID session)
        {
            if (!IsLoggedIn) return;

            //client.Self.InstantMessageGroup(target, message);
            client.Self.InstantMessageGroup(session, message);

            OnInstantMessageSent(new InstantMessageSentEventArgs(message, target, session, DateTime.Now));
        }

        public void SendIMStartTyping(UUID target, UUID session)
        {
            if (!IsLoggedIn) return;

            client.Self.InstantMessage(
                LoginOptions.FullName, target, "typing", session, InstantMessageDialog.StartTyping,
                InstantMessageOnline.Online, client.Self.SimPosition, client.Network.CurrentSim.ID, null);
        }

        public void SendIMStopTyping(UUID target, UUID session)
        {
            if (!IsLoggedIn) return;

            client.Self.InstantMessage(
                LoginOptions.FullName, target, "typing", session, InstantMessageDialog.StopTyping,
                InstantMessageOnline.Online, client.Self.SimPosition, client.Network.CurrentSim.ID, null);
        }

        public void Teleport(string sim, Vector3 coordinates)
        {
            if (!IsLoggedIn) return;
            if (IsTeleporting) return;

            TeleportingEventArgs ea = new TeleportingEventArgs(sim, coordinates);
            OnTeleporting(ea);
            if (ea.Cancel) return;

            IsTeleporting = true;
            client.Self.Teleport(sim, coordinates);
        }

        public bool IsLoggingIn { get; private set; } = false;

        public bool IsLoggedIn { get; private set; } = false;

        public bool IsTeleporting { get; private set; } = false;

        public LoginOptions LoginOptions { get; set; }

        // NetcomSync is used for raising certain events on the
        // GUI/main thread. Useful if you're modifying GUI controls
        // in the client app when responding to those events.
        public ISynchronizeInvoke NetcomSync { get; set; }
    }
}
