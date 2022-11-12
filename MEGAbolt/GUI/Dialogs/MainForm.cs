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
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Timers;
using System.Threading;
using System.Windows.Forms;
using MEGAbolt.NetworkComm;
using OpenMetaverse;
using Microsoft.Win32;
using System.IO;
using MEGAx;
using MEGAxCommon;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;
using NetSparkleUpdater;
using NetSparkleUpdater.SignatureVerifiers;

namespace MEGAbolt
{
    public partial class frmMain : Form, IHost
    {
        private MEGAboltInstance instance;
        private MEGAboltNetcom netcom;
        private GridClient client;
        
        private frmDebugLog debugLogForm;
        private System.Timers.Timer statusTimer;
        public Parcel parcel;
        public string dwell;
        private const int WM_KEYUP = 0x101;
        private System.Timers.Timer logOffTimer;
        private DateTime offtime;
        private bool logoff = false;
        private bool disconnectHasExecuted = false;

        private SparkleUpdater sparkleUpdater;

        // start Alexs
        // About Land Definitions (need to do that here)
        // since ParcelPropertiesCallBack is first called here and can't recall in about Land dialog
        //public Parcel Aboutlandparcelinfo;
        public string AboutlandOwneridname, AboutlandGroupidname;
        public bool Aboutlandforsale, AboutlandAllowFly, AboutlandCreateObj, AboutMature,  AboutShow, AllowOtherScripts, AboutlandRestrictPush, AboutlandAllowDamage = false;
        public bool AboutAllowGroupObjectEntry, AboutAllowAllObjectEntry, AboutlandGroupCreateObj, AllowGroupScripts, Allowcreatelm, AllowTerraform = false;
        // end Alexs

        // Extension manager stuff
        private ExtensionManager<IExtension, IHost> manager = new ExtensionManager<IExtension, IHost>();
        //private List<IExtension> elist;
        private int plugintimer = 0;
        private bool pluginsloaded = false;
        private string strInfolast = string.Empty;
        private string disconnectreason = "unknown";

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

        public frmMain(MEGAboltInstance instance)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            client = this.instance.Client;
            netcom = this.instance.Netcom;
            netcom.NetcomSync = this;

            Disposed += RemoveNetcomEvents;

            client.Parcels.ParcelProperties += Parcels_OnParcelProperties;
            this.instance.Config.ConfigApplied += Config_ConfigApplied;
            client.Objects.TerseObjectUpdate += Objects_OnObjectUpdated;
            netcom.MoneyBalanceUpdated +=netcom_MoneyBalanceUpdated;
            //client.Self.MoneyBalanceReply += new EventHandler<MoneyBalanceReplyEventArgs>(Self_MoneyBalanceReply);

            AddNetcomEvents();
            InitializeStatusTimer();
            RefreshWindowTitle();

            logOffTimer = new System.Timers.Timer();
            logOffTimer.Elapsed += OnTimedEvent;
            logOffTimer.Interval = 60000;   // 300000;   // 3600000;
            logOffTimer.Enabled = false;
            logOffTimer.Stop();

            ApplyConfig(this.instance.Config.CurrentConfig, true);

            StartUpdater();

            WindowState = FormWindowState.Normal;
        }

        //void Self_MoneyBalanceReply(object sender, MoneyBalanceReplyEventArgs e)
        //{
        //    //if (e.Success)
        //    //{
        //    //    TransactionInfo ti = e.TransactionInfo;
        //    //    string desc = ti.ItemDescription;

        //    //    int typtype = ti.TransactionType;

        //    //    if (typtype > 0)
        //    //    {
        //    //        if (!String.IsNullOrEmpty(desc))
        //    //        {
        //    //            if (ti.DestID != client.Self.AgentID)
        //    //            {
        //    //                tabsConsole.DisplayChatScreen(" => You paid L$" + ti.Amount.ToString() + " for " + desc + " ");
        //    //            }
        //    //        }
        //    //    }
        //    //}
        //}

        // Seperate thread
        private void Objects_OnObjectUpdated(object sender, TerseObjectUpdateEventArgs e)
        {
            if (!e.Update.Avatar) return;

            try
            {
                BeginInvoke(new MethodInvoker(RefreshStatusBar));
            }
            catch { ; }
        }

        private void Parcels_OnParcelProperties(object sender, ParcelPropertiesEventArgs e)
        {
            if (e.Result != ParcelResult.Single) return;

            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    Parcels_OnParcelProperties(sender, e);
                }));

                return;
            }

            UpdateLand(e.Parcel);
        }

        private void UpdateLand(Parcel parceln)
        {
            if (!netcom.IsLoggedIn)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    UpdateLand(parceln);
                }));

                return;
            }

            try
            {
                //if (parceln == null) return; 

                //if (currentparcelid != 0)
                //{
                //    if (currentparcelid != parceln.LocalID)
                //    {
                //        currentparcelid = parceln.LocalID;
                //        this.parcel = parceln;
                //    }
                //}
                //else
                //{
                //    currentparcelid = parceln.LocalID;
                //    this.parcel = parceln;
                //}

                //currentparcelid = parceln.LocalID;
                parcel = parceln;

                //List<Simulator> connectedsims = client.Network.Simulators; 

                instance.Config.CurrentConfig.pURL = @parcel.MusicURL;
                tlblParcel.Text = parcel.Name;

               // client.Parcels.RequestDwell(client.Network.CurrentSim, parcel.LocalID);

                List<UUID> avIDs = new List<UUID>();
                avIDs.Add(parcel.OwnerID);
                avIDs.Add(parcel.GroupID);

                client.Avatars.UUIDNameReply += Avatars_OnAvatarNames;
                client.Avatars.RequestAvatarNames(avIDs);

                tb1.Visible = false;
                tb2.Visible = false;
                tb3.Visible = false;
                tb4.Visible = false;
                tb5.Visible = false;
                tb6.Visible = false;
                tb7.Visible = false;

                //set them to false incase of parcel change
                Aboutlandforsale = false;
                AboutlandAllowFly = false;
                AboutlandCreateObj = false;
                AllowOtherScripts = false;
                AboutlandRestrictPush = false;
                AboutlandAllowDamage = false;
                AboutAllowGroupObjectEntry = false;
                AboutAllowAllObjectEntry = false;
                AboutlandGroupCreateObj = false;
                AllowGroupScripts = false;
                Allowcreatelm = false;
                AllowTerraform = false;
                AboutShow = false;
                AboutMature = false;

                // update some things so we have public access in About Land dialog
                // TODO: This is good work but it should be at app level i.e. MEGAboltIntance.cs and not here. Luke 

                if ((parcel.Flags & ParcelFlags.AllowFly) != ParcelFlags.AllowFly)
                {
                    tb1.Visible = true;
                    AboutlandAllowFly = true;
                }
                if ((parcel.Flags & ParcelFlags.CreateObjects) != ParcelFlags.CreateObjects)
                {
                    tb2.Visible = true;
                    AboutlandCreateObj = true;
                }
                if ((parcel.Flags & ParcelFlags.CreateGroupObjects) != ParcelFlags.CreateGroupObjects)
                {
                    AboutlandGroupCreateObj = true;
                }
                if ((parcel.Flags & ParcelFlags.AllowOtherScripts) != ParcelFlags.AllowOtherScripts)
                {
                    tb3.Visible = true;
                    AllowOtherScripts = true;
                }
                if ((parcel.Flags & ParcelFlags.AllowGroupScripts) != ParcelFlags.AllowGroupScripts)
                {
                    AllowGroupScripts = true;
                }
                if ((parcel.Flags & ParcelFlags.RestrictPushObject) == ParcelFlags.RestrictPushObject)
                {
                    tb4.Visible = true;
                    AboutlandRestrictPush = true;
                }
                if ((parcel.Flags & ParcelFlags.ShowDirectory) == ParcelFlags.ShowDirectory)
                {
                    AboutShow = true;
                }
                if ((parcel.Flags & ParcelFlags.MaturePublish) == ParcelFlags.MaturePublish)
                {
                    AboutMature = true;
                }
                if ((parcel.Flags & ParcelFlags.AllowDamage) != ParcelFlags.AllowDamage)
                {
                    tb5.Visible = true;
                    AboutlandAllowDamage = true;
                }
                if ((parcel.Flags & ParcelFlags.ForSale) == ParcelFlags.ForSale)
                {
                    tb6.Visible = true;
                    Aboutlandforsale = true;
                }
                if ((parcel.Flags & ParcelFlags.AllowAPrimitiveEntry) == ParcelFlags.AllowAPrimitiveEntry)
                {
                    AboutAllowAllObjectEntry = true;
                }
                if ((parcel.Flags & ParcelFlags.AllowGroupObjectEntry) == ParcelFlags.AllowGroupObjectEntry)
                {
                    AboutAllowGroupObjectEntry = true;
                }
                if ((parcel.Flags & ParcelFlags.AllowLandmark) == ParcelFlags.AllowLandmark)
                {
                    Allowcreatelm = true;
                }
                if ((parcel.Flags & ParcelFlags.AllowTerraform) == ParcelFlags.AllowTerraform)
                {
                    AllowTerraform = true;
                }
                if ((parcel.Flags & ParcelFlags.AllowVoiceChat) != ParcelFlags.AllowVoiceChat)
                {
                    tb7.Visible = true;
                    instance.AllowVoice = false;
                }
                else
                {
                    instance.AllowVoice = true;
                }

                // Log tp/lm location into history
                DateTime timestamp = DateTime.Now;

                timestamp = instance.State.GetTimeStamp(timestamp);

                string strInfo = string.Format(CultureInfo.CurrentCulture, "{0}/{1}/{2}/{3}", client.Network.CurrentSim.Name,
                                                                            Math.Round(instance.SIMsittingPos().X, 0),
                                                                            Math.Round(instance.SIMsittingPos().Y, 0),
                                                                            Math.Round(instance.SIMsittingPos().Z, 0));
                strInfo = "http://slurl.com/secondlife/" + strInfo;

                if (strInfolast == strInfo) return;

                strInfolast = strInfo;

                try
                {
                    DataRow dr = instance.TP.NewRow();
                    dr["time"] = timestamp.ToString();
                    dr["name"] = parcel.Name;
                    dr["slurl"] = strInfo;
                    instance.TP.Rows.Add(dr);
                }
                catch
                {
                    ;
                }
            }
            catch (Exception ex)
            {
                string serr = ex.Message;
                Logger.Log(String.Format(CultureInfo.CurrentCulture, "Land properties (main form) {0}", serr), Helpers.LogLevel.Error);
                //reporter.Show(ex);
            }
        }

        private void netcom_MoneyBalanceUpdated(object sender, BalanceEventArgs e)
        {
            if (instance.Config.CurrentConfig.AutoTransfer)
            {
                UUID mavatar = (UUID)instance.Config.CurrentConfig.MasterAvatar;

                if (mavatar == client.Self.AgentID)
                {
                    instance.Config.CurrentConfig.AutoTransfer = false;
                    return;
                }

                int mamount = client.Self.Balance;

                if (mamount > 0 && mavatar != UUID.Zero)
                {
                    client.Self.GiveAvatarMoney(mavatar, mamount, "MEGAbolt auto money transfer to master avatar");
                }
            }

            tlblMoneyBalance.Text = "L$" + client.Self.Balance.ToString(CultureInfo.CurrentCulture);
        }


        private void Avatars_OnAvatarNames(object sender, UUIDNameReplyEventArgs e)
        {
            if (InvokeRequired)
            {

                BeginInvoke(new MethodInvoker(() =>
                {
                    Avatars_OnAvatarNames(sender, e);
                }));

                return;
            }

            BeginInvoke(new MethodInvoker(() =>
            {
                NameReceived(e.Names);
            }));
        }

        //runs on the GUI thread
        private void NameReceived(Dictionary<UUID, string> names)
        {
            foreach (KeyValuePair<UUID, string> kvp in names)
            {
                if (kvp.Key == parcel.OwnerID)
                    AboutlandOwneridname = kvp.Value;
                else if (kvp.Key == parcel.GroupID)
                    AboutlandGroupidname = kvp.Value;
            }

            client.Avatars.UUIDNameReply -= Avatars_OnAvatarNames;
        }

        private void Config_ConfigApplied(object sender, ConfigAppliedEventArgs e)
        {
            ApplyConfig(e.AppliedConfig, false);
        }

        private void ApplyConfig(Config config, bool doingInit)
        {
            if (doingInit)
                WindowState = (FormWindowState)config.MainWindowState;

            if (config.InterfaceStyle == 0) //System
                toolStrip1.RenderMode = ToolStripRenderMode.System;
            else if (config.InterfaceStyle == 1) //Office 2003
                toolStrip1.RenderMode = ToolStripRenderMode.ManagerRenderMode;

            if (instance.Config.CurrentConfig.UseProxy)
            {
                // Apply proxy settings if any
                MEGAproxy proxy = new MEGAproxy();
                proxy.SetProxy(config.UseProxy, config.ProxyURL, config.ProxyPort, config.ProxyUser, config.ProxyPWD);
            }

            // Check for auto logoff and start time if set
            if (config.LogOffTime > 0)
            {
                if (instance.Config.CurrentConfig.LogOffTimerChanged)
                {
                    logOffTimer.Stop();
                    logOffTimer.Enabled = true;
                    logOffTimer.Start();
                    offtime = DateTime.Now.AddMinutes(Convert.ToDouble(instance.Config.CurrentConfig.LogOffTime));

                    instance.Config.CurrentConfig.LogOffTimerChanged = false;
                }

                tsTimeOut.Visible = true;

                TimeSpan ts = offtime - DateTime.Now;
                tsTimeOut.Text = ts.Hours.ToString("00", CultureInfo.CurrentCulture) + ":" + ts.Minutes.ToString("00", CultureInfo.CurrentCulture);
            }
            else
            {
                tsTimeOut.Visible = false;

                if (logOffTimer != null)
                {
                    logoff = false;
                    logOffTimer.Stop();

                    if (logOffTimer.Enabled)
                    {
                        logOffTimer.Enabled = false;
                    }
                }
            }

            // Menu positions
            Control control;
            bool topofscreen = false;

            switch (instance.Config.CurrentConfig.AppMenuPos)
            {
                case "Top":
                    control = toolStripContainer1.TopToolStripPanel;
                    topofscreen = true;
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

            toolStrip1.Parent = control;

            if (topofscreen) toolStrip1.Location = new Point(0, 0);
            topofscreen = false;
            

            switch (instance.Config.CurrentConfig.LandMenuPos)
            {
                case "Top":
                    control = toolStripContainer1.TopToolStripPanel;
                    topofscreen = true;
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

            statusStrip1.Parent = control;

            if (topofscreen) statusStrip1.Location = new Point(0, 25);
            topofscreen = false;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now >= offtime)
            {
                // log off
                logoff = true;
                //BeginInvoke(new MethodInvoker(tmnuExit.PerformClick));
                int itrvl = instance.Config.CurrentConfig.ReStartTime;
                DisconnectClient(true, "AutoRestart", itrvl);
            }

            TimeSpan ts = offtime - DateTime.Now;

            BeginInvoke((MethodInvoker)delegate
                {
                    tsTimeOut.Text = ts.Hours.ToString("00", CultureInfo.CurrentCulture) + ":" + ts.Minutes.ToString("00", CultureInfo.CurrentCulture);
                });
        }

        public void InitializeControls()
        {
            InitializeTabsConsole();
            InitializeDebugLogForm();
        }

        private void statusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                RefreshWindowTitle();
                RefreshStatusBar();

                if (!netcom.IsLoggedIn)
                {
                    Disconnect(true);
                }
                else
                {
                    if (instance.EList == null) return;

                    if (!pluginsloaded)
                    {
                        plugintimer += Convert.ToInt32(statusTimer.Interval);

                        if (plugintimer >= 60000 && !pluginsloaded)
                        {
                            // Load plugins if any
                            if (instance.EList.Count > 0)
                            {
                                string plugins = instance.Config.CurrentConfig.PluginsToLoad;

                                foreach (IExtension extOn in instance.EList)
                                {
                                    if (plugins.Contains(extOn.Title))
                                    {
                                        extOn.Process(instance);
                                    }
                                }

                                //if (plugins != null && plugins != string.Empty)
                                //{
                                //    string[] lplugs = plugins.Split('|');

                                //    foreach (string plug in lplugs)
                                //    {
                                //        foreach (IExtension extOn in this.instance.EList)
                                //        {
                                //            if (plug == extOn.Title)
                                //            {
                                //                //If we have an instance, call process!
                                //                if (extOn != null)
                                //                    extOn.Process(this.instance);
                                //            }
                                //        }
                                //    }
                                //}
                            }

                            pluginsloaded = true;
                        }
                    }
                }
            }
            catch
            {
                ;
            }
        }

        private void AddNetcomEvents()
        {
            netcom.ClientLoginStatus += netcom_ClientLoginStatus;
            netcom.ClientLoggedOut += netcom_ClientLoggedOut;
            netcom.ClientDisconnected += netcom_ClientDisconnected;
            netcom.InstantMessageReceived += netcom_InstantMessageReceived;
            //netcom.Teleporting += new EventHandler<TeleportingEventArgs>(netcom_Teleporting);  
        }

        private void netcom_InstantMessageReceived(object sender, InstantMessageEventArgs e)
        {
            if (e.IM.Dialog is InstantMessageDialog.StartTyping or InstantMessageDialog.StopTyping)
                return;

            // Check to see if group IMs or Notices are disabled
            if (instance.Config.CurrentConfig.DisableGroupIMs || instance.Config.CurrentConfig.DisableGroupNotices)
                return;

            if (!Focused) FormFlash.Flash(this);
        }

        private void netcom_ClientLoginStatus(object sender, LoginProgressEventArgs e)
        {
            if (e.Status != LoginStatus.Success) return;

            client.Settings.ASSET_CACHE_DIR = DataFolder.GetDataFolder() + Path.DirectorySeparatorChar + client.Self.Name + Path.DirectorySeparatorChar + "cache";

            tlTools.Enabled = tlLogs.Enabled = tsUtilities.Enabled = btnMap.Enabled = btnAvatar.Enabled = tbtnTeleport.Enabled = tbtnObjects.Enabled = true;
            statusTimer.Enabled = true;
            statusTimer.Start();
            RefreshWindowTitle();

            if (instance.Config.CurrentConfig.StartMinimised)
            {
                WindowState = FormWindowState.Minimized;
            }

            client.Self.RequestMuteList();
            client.Self.Movement.Camera.Far = instance.Config.CurrentConfig.RadarRange;
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            disconnectreason = "Client logged out from SL";
            Disconnect(true);
        }

        private void RemoveNetcomEvents(object sender, EventArgs e)
        {
            netcom.ClientLoginStatus -= netcom_ClientLoginStatus;
            netcom.ClientLoggedOut -= netcom_ClientLoggedOut;
            netcom.ClientDisconnected -= netcom_ClientDisconnected;
            netcom.InstantMessageReceived -= netcom_InstantMessageReceived;

            client.Parcels.ParcelProperties -= Parcels_OnParcelProperties;
            instance.Config.ConfigApplied -= Config_ConfigApplied;
            client.Objects.TerseObjectUpdate -= Objects_OnObjectUpdated;
            netcom.MoneyBalanceUpdated -= netcom_MoneyBalanceUpdated;

            manager.AssemblyFailedLoading -= manager_AssemblyFailedLoading;
            manager.AssemblyLoaded -= manager_AssemblyLoaded;
            manager.AssemblyLoading -= manager_AssemblyLoading;

            //statusTimer.Elapsed -= new ElapsedEventHandler(statusTimer_Elapsed);

            instance.EndCrashRep();
        }

        private void netcom_ClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            // Commented code left here for illustration purposes of enhancement #35
            //if (e.Reason != NetworkManager.DisconnectType.ClientInitiated)
            //{
            //    frmDisconnected disconnectedDialog = new frmDisconnected(instance, e);
            //    disconnectedDialog.ShowDialog();
            //}

            disconnectreason = e.Reason.ToString(); 
            Disconnect(true);
        }

        //private string HttpPost(string uri, string parameters)
        //{
        //    //// parameters: name1=value1&name2=value2	
        //    //WebRequest webRequest = WebRequest.Create(uri);
        //    ////string ProxyString = 
        //    ////   System.Configuration.ConfigurationManager.AppSettings
        //    ////   [GetConfigKey("proxy")];
        //    ////webRequest.Proxy = new WebProxy (ProxyString, true);
        //    ////Commenting out above required change to App.Config
        //    //webRequest.ContentType = "application/x-www-form-urlencoded";
        //    //webRequest.Method = "POST";
        //    //byte[] bytes = Encoding.ASCII.GetBytes(parameters);
        //    //Stream os = null;
        //    //try
        //    //{ // send the Post
        //    //    webRequest.ContentLength = bytes.Length;   //Count bytes to send
        //    //    os = webRequest.GetRequestStream();
        //    //    os.Write(bytes, 0, bytes.Length);         //Send it
        //    //}
        //    //catch (WebException ex)
        //    //{
        //    //    // do nothing
        //    //}
        //    //finally
        //    //{
        //    //    if (os != null)
        //    //    {
        //    //        os.Close();
        //    //    }
        //    //}

        //    //try
        //    //{ // get the response
        //    //    WebResponse webResponse = webRequest.GetResponse();
        //    //    if (webResponse == null)
        //    //    { return null; }
        //    //    StreamReader sr = new StreamReader(webResponse.GetResponseStream());
        //    //    return sr.ReadToEnd().Trim();
        //    //}
        //    //catch (WebException ex)
        //    //{
        //    //    // do nothing
        //    //}

        //    return null;
        //}

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (netcom.IsLoggedIn)
                {
                    if (!logoff)
                    {
                        MEGAbolt.Controls.MsgBoxCheck.MessageBox dlg = new();
                        DialogResult dr = dlg.Show(@"Software\MEGAbolt\CloseMBCheck", 
                            "DontShowAgain", DialogResult.Yes, 
                            "Don't ask me this again", 
                            "You are about to close MEGAbolt. Are you sure you want to continue?", "MEGAbolt",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                        if (dr == DialogResult.No)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }

                // To avoid LSL errors in SL
                instance.State.SetStanding();

                // Save the window state. As the main application is exiting it will save the state to the config file.
                Visible = false;
                instance.Config.CurrentConfig.MainWindowState = (int)WindowState;

                // I don't like setting this here, but this event is the only place to know if the user possibly clicked the X to close the window.
                // I had a check for e.CloseReason here, but if anything calls this.Close(), it uses the same reason as if the user clicked the X.
                // However, the code has been restructured to call Disconnect() first for non-user-initiated logout. Disconnect can only be executed
                // once, so in those cases setting LogOffClicked to true here has no effect.
                // -Apotheus
                instance.LogOffClicked = true;

                Disconnect(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                ex.Message,
                "MEGAbolt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }
        }

        private void RefreshStatusBar()
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)RefreshStatusBar);

                return;
            }

            if (netcom.IsLoggedIn)
            {
                //tlblLoginName.Text = netcom.LoginOptions.FullName;
                tlblMoneyBalance.Text = "L$" + client.Self.Balance.ToString(CultureInfo.CurrentCulture);

                tlblRegionInfo.Text =
                        client.Network.CurrentSim.Name +
                        " (" + Math.Floor(instance.SIMsittingPos().X).ToString(CultureInfo.CurrentCulture) + ", " +
                        Math.Floor(instance.SIMsittingPos().Y).ToString(CultureInfo.CurrentCulture) + ", " +
                        Math.Floor(instance.SIMsittingPos().Z).ToString(CultureInfo.CurrentCulture) + ")";
            }
            else
            {
                //tlblLoginName.Text = "Offline";
                tlblMoneyBalance.Text = "L$0";
                tlblRegionInfo.Text = "No Region";
                tlblParcel.Text = "No Parcel";

                tb1.Visible = false;
                tb2.Visible = false;
                tb3.Visible = false;
                tb4.Visible = false;
                tb5.Visible = false;
                tb6.Visible = false;
                tb7.Visible = false;
            }
        }

        private void RefreshWindowTitle()
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)RefreshWindowTitle);

                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("MEGAbolt - ");

            if (netcom.IsLoggedIn)
            {
                sb.Append("[" + netcom.LoginOptions.FullName + "]");

                if (instance.State.IsSitting)
                {
                    sb.Append(" [Sitting]");
                }

                if (instance.State.IsSittingOnGround)
                {
                    sb.Append(" [Sitting on Ground]");
                }

                if (instance.State.IsFlying)
                {
                    sb.Append(" - [Flying] ");
                }

                if (instance.State.IsAway)
                {
                    sb.Append(" - Away");
                    if (instance.State.IsBusy) sb.Append(", Busy");
                }
                else if (instance.State.IsBusy)
                {
                    sb.Append(" - Busy");
                }

                if (instance.State.IsFollowing)
                {
                    sb.Append(" - [Following] ");
                    sb.Append(instance.State.FollowName);
                }
            }
            else
            {
                sb.Append("Logged Out");
                statusTimer.Stop();
            }

            Text = sb.ToString();
            sb = null;
        }

        private void InitializeStatusTimer()
        {
            statusTimer = new System.Timers.Timer(1000);
            statusTimer.Enabled = false;
            statusTimer.SynchronizingObject = this;
            statusTimer.Elapsed += statusTimer_Elapsed;
        }

        private void InitializeTabsConsole()
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)InitializeTabsConsole);

                return;
            }

            TabConsole = new TabsConsole(instance);
            TabConsole.Dock = DockStyle.Fill;
            toolStripContainer1.ContentPanel.Controls.Add(TabConsole);
        }

        private void InitializeDebugLogForm()
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)InitializeDebugLogForm);

                return;
            }

            debugLogForm = new frmDebugLog(instance);
        }

        private void tmnuAbout_Click(object sender, EventArgs e)
        {
            (new frmAbout()).Show(this);
        }

        private void tmnuExit_Click(object sender, EventArgs e)
        {
            instance.LogOffClicked = true;
            Close();
        }

        private void tbtnTeleport_Click(object sender, EventArgs e)
        {
            (new frmTeleport(instance,"",0,0,0,false)).Show();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            
            TabConsole.SelectTab("Main");

            // The extension stuff followeth :)

            //Listen to the events
            manager.AssemblyFailedLoading += manager_AssemblyFailedLoading;
            manager.AssemblyLoaded += manager_AssemblyLoaded;
            manager.AssemblyLoading += manager_AssemblyLoading;

            //Loads .cs, .vb, .js, and .dll extensions
            manager.LoadDefaultFileExtensions();

            //We need to add our library with the interfaces to the list
            manager.SourceFileReferencedAssemblies.Add("MEGAxCommon.dll");
            manager.SourceFileReferencedAssemblies.Add("MEGAbolt.exe");
            manager.SourceFileReferencedAssemblies.Add("LibreMetaverse.dll");
            manager.SourceFileReferencedAssemblies.Add("LibreMetaverse.Types.dll");
            manager.SourceFileReferencedAssemblies.Add("LibreMetaverse.StructuredData.dll");
            manager.SourceFileReferencedAssemblies.Add("LibreMetaverse.Utilities.dll");

            try
            {
                //Lookin in the AppPath\Extensions\ folder
                //manager.LoadExtensions(Application.StartupPath.TrimEnd("\\".ToCharArray()) + "\\Extensions\\");
                manager.LoadExtensions(DataFolder.GetDataFolder() + "\\Extensions\\");
            }
            catch (Exception ex)
            {
               MessageBox.Show("Invalid files in 'Extensions' folder caused Windows errors: " + ex.Message);   
            }

            //List<Extension<IExtension>> tounload = new List<Extension<IExtension>>();

            if (manager.Extensions.Count > 0)
            {
                //Loop through all the extensions
                foreach (Extension<IExtension> extOn in manager.Extensions)
                {
                    //It needs to know we're the host!
                    extOn.Instance.Host = this;

                    ToolStripDropDownItem mitem;   // = tsPlugins.OwnerItem as ToolStripDropDownItem;
                    mitem = tsPlugins;

                    ToolStripButton item = new ToolStripButton();
                    item.Tag = extOn.Instance;
                    item.Text = extOn.Instance.Title;
                    item.Width = extOn.Instance.Title.Length * 6;   // 200;
                    item.Click += AnyMenuItem_Click;
                    mitem.DropDownItems.Add(item);

                    //elist.Add(extOn.Instance);
                    instance.EList.Add(extOn.Instance);

                    //tounload.Add(extOn);
                }

                //foreach (Extension<IExtension> extOn in tounload)
                //{
                //    manager.UnloadExtension(extOn);
                //}

                ToolStripDropDownItem mmgr;
                mmgr = tsPlugins;

                ToolStripSeparator sep = new ToolStripSeparator();
                mmgr.DropDownItems.Add(sep);

                ToolStripButton itm = new ToolStripButton();
                //itm.Tag = extOn.Instance;
                itm.Text = "Plugin Manager";
                itm.Width = 14 * 6;
                itm.Click += AnyMenuItem_Click;
                mmgr.DropDownItems.Add(itm);

                //this.instance.EList = elist;

                //elist.Clear(); 

                tsPlugins.Visible = true;
            }
        }

        private void AnyMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripItem mitem = (ToolStripItem)sender;

            if (mitem.Text.ToLower(CultureInfo.CurrentCulture) == "plugin manager")
            {
                (new frmPluginManager(instance)).ShowDialog(this);
                return;
            }

            IExtension extInstance = (IExtension)mitem.Tag;

            //Extension<IExtension> extobj = (Extension<IExtension>)mitem.Tag;
            //IExtension extInstance = extobj.Instance; 

            //If we have an instance, call process!
            //if (!manager.Extensions.Contains(extobj))
            //{
            //    manager.LoadExtension(extobj.Filename);
            //}

            extInstance?.Process(instance);
        }

        #region IHost Members
        public void HostStatus(string statusMessage)
        {
            //Change the status label
            // this is here for future proofing in case
            // we want to receive a response from the plugin
            //string status = statusMessage;
        }
        #endregion

        #region IHost events
        void manager_AssemblyLoading(object sender, AssemblyLoadingEventArgs e)
        {
            //status
            //string ev = "Loading: " + e.Filename;
        }

        void manager_AssemblyLoaded(object sender, AssemblyLoadedEventArgs e)
        {
            string file = (new FileInfo(e.Filename)).Name.Trim();
            string ev = "Loaded plugin: " + file;

            Logger.Log(ev, Helpers.LogLevel.Info);
        }

        void manager_AssemblyFailedLoading(object sender, AssemblyFailedLoadingEventArgs e)
        {
            //Build an error message with specific compiler errors
            StringBuilder msg = new StringBuilder();
            msg.AppendLine("Failed Loading Extension: " + e.Filename + " of Type: " + e.ExtensionType);
            msg.AppendLine("Error Message: ");
            msg.AppendLine(e.ErrorMessage);
            msg.AppendLine(" ");
            msg.AppendLine("Compiler Errors: ");

            foreach (System.CodeDom.Compiler.CompilerError errorOn in e.SourceFileCompilerErrors)
                msg.AppendLine($"  #{errorOn.ErrorNumber.ToString(CultureInfo.CurrentCulture)} " +
                    $"on Line: {errorOn.Line.ToString(CultureInfo.CurrentCulture)} " +
                    $"at Column: {errorOn.Column.ToString(CultureInfo.CurrentCulture)} - {errorOn.ErrorText}");

            //Show the user
            //MessageBox.Show(this, msg.ToString(), "Extension Compilation Error", MessageBoxButtons.OK);
            Logger.Log("Plugin Error: " + msg, Helpers.LogLevel.Error); 
        }
        #endregion
        

        public TabsConsole TabConsole { get; private set; }

        private void tmnuNewWindow_Click(object sender, EventArgs e)
        {
            //instance.Config.SaveCurrentConfig();
            //if (instance.IsFirstInstance) instance.OtherInstancesOpen = true;

            //(new METAboltInstance(false)).MainForm.Show();
        }

        private void tmnuPrefs_Click(object sender, EventArgs e)
        {
            (new frmPreferences(instance)).Show(this);
        }

        private void tbtnObjects_Click(object sender, EventArgs e)
        {
            (new frmObjects(instance)).Show();
        }

        private void toolStripContainer1_ContentPanel_Load(object sender, EventArgs e)
        {
            
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // FIXME: Help menu link
        }

        private void avatarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //client.Appearance.RequestCachedBakes();
                client.Appearance.RequestSetAppearance(true);  
                //client.Appearance.SetPreviousAppearance(false);
            }
            catch (Exception exp)
            {
                Logger.Log("Rebake (menu): " + exp.InnerException, Helpers.LogLevel.Error);
            }
        }

        private void setHomeToolStripMenuItem_Click(object sender, EventArgs e)
        {
               
        }

        private void setHomeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("Are you sure you want to SET HOME to here?", "MEGAbolt", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                client.Self.SetHome();
            }            
        }

        private void tPHomeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            client.Self.GoHome();
        }

        private void standToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void standToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            instance.State.SetStanding();
            RefreshWindowTitle();
        }

        private void btnMap_Click(object sender, EventArgs e)
        {
            (new frmMapClient(instance)).Show();
        }

        private void tb1_Click(object sender, EventArgs e)
        {

        }

        private void tb1_MouseEnter(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(statusStrip1, "No Fly");
            tb1.BackColor = Color.LightSteelBlue;  
        }

        private void tb2_MouseEnter(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(statusStrip1, "No Build");
            tb2.BackColor = Color.LightSteelBlue;
        }

        private void tb3_MouseEnter(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(statusStrip1, "No Script");
            tb3.BackColor = Color.LightSteelBlue;
        }

        private void tb4_MouseEnter(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(statusStrip1, "No Push");
            tb4.BackColor = Color.LightSteelBlue;
        }

        private void tb5_MouseEnter(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(statusStrip1, "Safe");
            tb5.BackColor = Color.LightSteelBlue;
        }

        private void tb6_MouseEnter(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(statusStrip1, "For Sale");
            tb6.BackColor = Color.LightSteelBlue;
        }

        private void tlblParcel_MouseEnter(object sender, EventArgs e)
        {
            tlblParcel.BackColor = Color.LightSteelBlue;
            toolTip1.SetToolTip(statusStrip1, "Parcel Name");
        }

        private void tlblRegionInfo_MouseEnter(object sender, EventArgs e)
        {
            tlblRegionInfo.BackColor = Color.LightSteelBlue;
            toolTip1.SetToolTip(statusStrip1, "SIM Name");
        }

        private void tlblMoneyBalance_Click(object sender, EventArgs e)
        {
           Utilities.OpenBrowser(@"https://secondlife.com/my/account/history.php?lang=en");
        }

        private void tlblMoneyBalance_MouseEnter(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(statusStrip1, "L$ Balance");
        }

        private void tlblLoginName_Click(object sender, EventArgs e)
        {
            if (netcom.IsLoggedIn)
            {
                client.Self.GoHome();
            }
        }

        private void tlblLoginName_MouseEnter(object sender, EventArgs e)
        {
            tlblLoginName.BackColor = Color.LightSteelBlue; 
            toolTip1.SetToolTip(statusStrip1, "Go Home");
        }

        private void landMonitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //(new frmLand(instance)).Show();
        }

        private void awayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.State.SetAway(awayToolStripMenuItem.Checked);
        }

        private void busyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.State.SetBusy(busyToolStripMenuItem.Checked);
        }

        private void flyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.State.SetFlying(flyToolStripMenuItem.Checked);
        }

        private void alwaysRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.State.SetAlwaysRun(alwaysRunToolStripMenuItem.Checked);
        }
       
        private void tlblParcel_Click(object sender, EventArgs e)
        {
            //if (netcom.IsLoggedIn && tbtnDebug.Visible == true) // only allow to open About land when logged in and for now only as a Debug Future
            if (netcom.IsLoggedIn)
            {
                (new frmAboutLand(instance)).Show(this);
            }
        }

        private void accountHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utilities.OpenBrowser(@"https://secondlife.com/my/account/history.php?lang=en");
        }

        private void sLHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utilities.OpenBrowser(@"https://secondlife.com/support/");
        }

        private void sLKnowledgebaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utilities.OpenBrowser(@"https://community.secondlife.com/t5/tkb/communitypage");
        }

        private void scriptingPortalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utilities.OpenBrowser(@"https://wiki.secondlife.com/wiki/LSL_Portal");
        }

        private void reportABugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // FIXME: Report a bug
        }

        private void aboutLandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new frmAboutLand(instance)).Show(this);
        }

        private void bellyDanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.State.BellyDance(bellyDanceToolStripMenuItem.Checked);
        }

        private void clubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.State.ClubDance(clubToolStripMenuItem.Checked);
        }

        private void salsaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.State.SalsaDance(salsaToolStripMenuItem.Checked);
        }

        private void fallOnFaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.State.FallOnFace(fallOnFaceToolStripMenuItem.Checked);
        }

        private void crouchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.State.Crouch(crouchToolStripMenuItem.Checked);
        }

        private void launchSLViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // The following line was borrowed from Kitten Lulu's SLRun1CPU
                // http://kittenlulu.wordpress.com/2006/11/08/secondlife-on-multi-core-systems/
                string SecondLifeExe = GetSecondLifeExe();

                if (string.IsNullOrEmpty(SecondLifeExe))
                    return;

                Process thisProcess = new Process();
                thisProcess.StartInfo.FileName = SecondLifeExe;
                thisProcess.StartInfo.UseShellExecute = false;
                thisProcess.StartInfo.RedirectStandardInput = true;
                thisProcess.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oops something went wrong!\n\n" + ex.Message, "MEGAbolt");
            }
        }

        private static string GetSecondLifeExe()
        {
            try
            {
                // This routine was borrowed from Kitten Lulu's SLRun1CPU
                // http://kittenlulu.wordpress.com/2006/11/08/secondlife-on-multi-core-systems/

                RegistryKey SecondLifeReg = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Linden Research, Inc.\\SecondLifeViewer2");

                if (SecondLifeReg == null)
                {
                    SecondLifeReg = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Linden Research, Inc.\\SecondLifeViewer2");

                    if (SecondLifeReg == null)
                    {
                        SecondLifeReg = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Linden Research, Inc.\\SecondLife");

                        if (SecondLifeReg == null)
                        {
                            SecondLifeReg = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Linden Research, Inc.\\SecondLifeViewer");

                            if (SecondLifeReg == null)
                            {
                                MessageBox.Show("The Secondlife viewer is not installed", "MEGAbolt");
                                return string.Empty;
                            }
                        }
                    }
                }

                Object path = SecondLifeReg.GetValue("");
                Object exeName = SecondLifeReg.GetValue("Exe");

                if ((path != null) && (exeName != null))
                {
                    return (String)path + "\\" + (String)exeName;
                }
                else
                {
                    MessageBox.Show("The Secondlife viewer is not installed", "MEGAbolt");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oops something went wrong!\n\n" + ex.Message,"MEGAbolt");
                return string.Empty;
            }
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void statusToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void teleportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new frmTeleport(instance, "", 0, 0, 0, false)).Show(this);
        }

        private void mapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new frmMapClient(instance)).Show(this);
        }

        private void objectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new frmObjects(instance)).Show();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                string _timeZoneId = "Pacific Standard Time";
                DateTime startTime = DateTime.UtcNow;
                TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                DateTime _now = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);

                //DateTime SLdate = DateTime.UtcNow.AddHours(-8);
                //tsTime.Text = SLdate.ToString();
                tsTime.Text = _now.ToLongTimeString();   // ToString();
            }
            catch
            {
                // do nothing
                tsTime.Text = "?00:00:00";
            }
        }

        private void tmnuBackup_Click(object sender, EventArgs e)
        {
            (new frmBackup()).Show(this); 
        }

        private void createLandmarkHereToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string file = parcel.Name;

            if (file.Length > 32)
            {
                file = file.Substring(0, 32); 
            }

            string pos = instance.SIMsittingPos().X.ToString(CultureInfo.CurrentCulture) + ", " + instance.SIMsittingPos().Y.ToString(CultureInfo.CurrentCulture) + ", " + instance.SIMsittingPos().Z.ToString(CultureInfo.CurrentCulture);
               
            string desc = file + ", " + client.Network.CurrentSim.Name + " (" + pos + ")";

            client.Inventory.RequestCreateItem(client.Inventory.FindFolderForType(AssetType.Landmark),
                    file, desc, AssetType.Landmark, UUID.Random(), InventoryType.Landmark, PermissionMask.All,
                    (success, item) =>
                    {
                        if (!success)
                        {
                            MessageBox.Show("Landmark could not be created", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    }
                );  
        }

        private void addToPicksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //client.Parcels.InfoRequest()
  
            // I couldn't find a way to get a land's UUID using the
            // parcel object. I also couldn't find a function that will
            // return a parcel's UUID by passing the LocalID. There is probably
            // another way of doing this!!!
            // To be honest I haven't spent much time on it
            // Complete this is you fancy pulling your hair out.
            //
            // client.Self.PickInfoUpdate(UUID.Random(), true, parcel.LocalID, parcel.Name, client.Self.GlobalPosition, parcel.SnapshotID, parcel.Desc);  
        }

        private void hoverToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (instance.Config.CurrentConfig.HideMeta)
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    Hide();
                }
            }
        }

        private void tsUtilities_Click(object sender, EventArgs e)
        {

        }

        public void SetFlag(Image img, string lang)
        {
            tsFlag.Image = img;
            tsFlag.ToolTipText = "Detectected language: " + lang; 
        }

        private void mETAplayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new frmPlayer(instance)).Show(this);
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            (new frmStats(instance)).Show(this);
        }

        private void teleportHistoryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            (new frmTPhistory(instance)).Show(this);
        }

        private void muteListToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (!instance.LoggedIn) return;

            client.Self.RequestMuteList();
            (new frmMutes(instance)).Show(this);
        }

        private void debugLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                debugLogForm.Show(this);
            }
            catch
            {
                //Logger.Log(String.Format("Debug Form Display Error: {0}", exp), Helpers.LogLevel.Error);
                debugLogForm.Dispose(); 
                debugLogForm = new frmDebugLog(instance);
            }
        }

        private void chatLogsToolStripMenuItem_Click(object sender, EventArgs e)                                       
        {
            Process.Start("explorer.exe", instance.Config.CurrentConfig.LogDir);
        }

        private void tsPlugins_Click(object sender, EventArgs e)
        {

        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sparkleUpdater.CheckForUpdatesAtUserRequest();
        }

        private void setPreviousAppearanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //client.Appearance.SetPreviousAppearance(true);
            }
            catch (Exception exp)
            {
                Logger.Log("Previous Appearance (menu): " + exp.InnerException, Helpers.LogLevel.Error);
            }
        }

        private void tbtnHelp_Click(object sender, EventArgs e)
        {

        }

        private void groundSitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            client.Self.SitOnGround();
            instance.State.SetGroundSit(true);  
        }

        private void searchLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new frmLogSearch(instance)).Show(this);
        }

        private void frmMain_LocationChanged(object sender, EventArgs e)
        {
              
        }

        private void tlblRegionInfo_Click(object sender, EventArgs e)
        {
            if (netcom.IsLoggedIn)
            {
                (new frmMapClient(instance)).Show();
            }
        }

        private void reloadAIMLLibrariesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.InitAI(); 
        }

        /// <summary>
        /// Disconnects from the server.
        /// Optionally forks METArestart.exe based on this.LogOffClicked
        /// Optionally closes the window.
        /// 
        /// This method ensures its logic executes only once to eliminate errors and unintentional conflicts.
        /// </summary>
        /// <param name="closeWindow">Determines whether this method should call this.Close()</param>
        private void Disconnect(bool closeWindow)
        {
            // Only run this once
            if (disconnectHasExecuted)
            {
                return;
            }

            disconnectHasExecuted = true;

            // Functional shutdown
            statusTimer.Elapsed -= statusTimer_Elapsed;
            statusTimer.Stop();

            if (!string.IsNullOrEmpty(netcom.LoginOptions.FirstName) && !string.IsNullOrEmpty(netcom.LoginOptions.LastName))
            {
                if (netcom.IsLoggedIn)
                {
                    string full_name = netcom.LoginOptions.FirstName + "_" + netcom.LoginOptions.LastName;
                    instance.Config.SetAvConfig(full_name);
                }
            }

            if (netcom.IsLoggedIn)
            {
                netcom.Logout();
            }

            // Special case for server-side disconnect rather than user-initiated
            if (!instance.LogOffClicked)
            {
                if (!instance.Config.CurrentConfig.AutoRestart)
                {
                    // Auto-restart
                    try
                    {
                        int restartinterval = instance.Config.CurrentConfig.ReStartTime * 60; // convert to seconds

                        Process p = new Process();
                        p.StartInfo.FileName = "MEGArestart.exe";
                        p.StartInfo.WorkingDirectory = Application.StartupPath;
                        p.StartInfo.Arguments = netcom.LoginOptions.FirstName + " " + netcom.LoginOptions.LastName + " " + netcom.LoginOptions.Password + " " + disconnectreason.Replace(" ", "|") + " " + restartinterval.ToString(CultureInfo.CurrentCulture);
                        p.Start();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Exception while trying to execute MEGArestart.exe: " + ex.Message, Helpers.LogLevel.Error);
                        return;
                    }
                }
                else
                {
                    tlTools.Enabled = tlLogs.Enabled = tsUtilities.Enabled = btnMap.Enabled = btnAvatar.Enabled = tbtnTeleport.Enabled = tbtnObjects.Enabled = false;
                    statusTimer.Enabled = false;
                    statusTimer.Stop();

                    RefreshStatusBar();
                    RefreshWindowTitle();

                    //instance.ReadIMs = false;

                    //(new frmDisconnected(instance, disconnectreason)).ShowDialog(this);

                    if (!instance.Config.CurrentConfig.HideDisconnectPrompt)
                    {
                        (new frmDisconnected(instance, disconnectreason)).ShowDialog(this);

                        if (instance.ReadIMs) return;
                    }
                }
            }

            try
            {
                // UI shutdown
                tlTools.Enabled = btnMap.Enabled = btnAvatar.Enabled = tbtnTeleport.Enabled = tbtnObjects.Enabled = false;
                RefreshStatusBar();
                RefreshWindowTitle();

                if (debugLogForm is { Disposing: false })
                {
                    debugLogForm.Dispose();
                    debugLogForm.Close();
                    debugLogForm = null;
                }
            }
            catch
            {
                ;
            }

            if (closeWindow)
            {
                Close();
            }
        }

        /// <summary>
        /// Disconnects from the server on request.
        /// Always forks MEGArestart.exe overriding disconnectreason and restartinterval
        /// Optionally closes the window.
        /// 
        /// This method ensures its logic executes only once to eliminate errors and unintentional conflicts.
        /// </summary>
        /// <param name="closeWindow">Determines whether this method should call this.Close()</param>
        /// <returns>false if already run or failed to execute METArestart, otherwise true</returns>
        public bool DisconnectClient(bool CloseWindow, string Reason, int ReconnectWaitMinutes)
        {
            // Only run this once
            if (disconnectHasExecuted)
            {
                return false;
            }

            disconnectHasExecuted = true;

            // Functional shutdown
            statusTimer.Elapsed -= statusTimer_Elapsed;
            statusTimer.Stop();

            if (!string.IsNullOrEmpty(netcom.LoginOptions.FirstName) && !string.IsNullOrEmpty(netcom.LoginOptions.LastName))
            {
                if (netcom.IsLoggedIn)
                {
                    string full_name = netcom.LoginOptions.FirstName + "_" + netcom.LoginOptions.LastName;
                    instance.Config.SetAvConfig(full_name);
                }
            }

            if (netcom.IsLoggedIn)
            {
                netcom.Logout();
            }

            if (!instance.Config.CurrentConfig.AutoRestart)
            {
                try
                {
                    int restartinterval = 10;

                    checked
                    {
                        restartinterval = ReconnectWaitMinutes * 60; // convert to seconds
                    }

                    
                    disconnectreason = Reason;

                    Process p = new Process();
                    p.StartInfo.FileName = "MEGArestart.exe";
                    p.StartInfo.WorkingDirectory = Application.StartupPath;
                    p.StartInfo.Arguments = netcom.LoginOptions.FirstName + " " + netcom.LoginOptions.LastName + " " + netcom.LoginOptions.Password + " " + disconnectreason.Replace(" ", "|") + " " + restartinterval.ToString(CultureInfo.CurrentCulture);
                    p.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("Exception while trying to execute MEGArestart.exe: " + ex.Message, Helpers.LogLevel.Error);
                    return false;
                }
            }

            try
            {
                // UI shutdown
                tlTools.Enabled = btnMap.Enabled = btnAvatar.Enabled = tbtnTeleport.Enabled = tbtnObjects.Enabled = false;

                RefreshStatusBar();
                RefreshWindowTitle();

                if (debugLogForm is { Disposing: false })
                {
                    debugLogForm.Dispose();
                    debugLogForm.Close();
                    debugLogForm = null;
                }
            }
            catch
            {
                ;
            }

            if (CloseWindow)
            {
                Close();
            }

            return true;
        }

        private void stopAnimationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            instance.State.StopAnimations();
        }

        private void uploadImageL10PerUploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog open = new OpenFileDialog
                {
                    Filter = "Image Files(*.jpg; *.jpeg; *.tga; *.bmp; *.png)|*.jpg; *.jpeg; *.tga; *.bmp; *.png"
                };

                string ext = string.Empty;

                if (open.ShowDialog() == DialogResult.OK)
                {
                    Bitmap bitmap = new Bitmap(open.FileName);
                    ext = Path.GetExtension(open.FileName).ToLower(CultureInfo.CurrentCulture);

                    (new UploadImage(instance, bitmap, open.FileName, ext)).Show(this);
                }

                open.Dispose(); 
            }
            catch (Exception)
            {
                throw new ApplicationException("Failed loading image");
            }
        }

        private void scriptManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new frmScriptEditor(instance)).Show(this);
        }

        private void tlTools_Click(object sender, EventArgs e)
        {

        }

        private void sLGridStatusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utilities.OpenBrowser(@"https://status.secondlifegrid.net/");
        }

        public void UpdateFavourites(List<InventoryBase> foundfolders)
        {
            TabConsole.chatConsole.UpdateFavourites(foundfolders);  

            //foreach (InventoryBase oitem in foundfolders)
            //{
            //    InventoryItem item = (InventoryItem)oitem;

            //    if (item.InventoryType == InventoryType.Landmark)
            //    {
            //        string iname = item.Name;

            //        int ncnt = iname.Length; 

            //        if (parcel.Name.ToString().StartsWith(iname, StringComparison.CurrentCultureIgnoreCase))
            //        {
            //            toolStripStatusLabel2.Enabled = false;
            //        }                       
            //    }
            //}
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            tlblParcel.PerformClick(); 
        }

        private void toolStripStatusLabel1_MouseEnter(object sender, EventArgs e)
        {
            toolStripStatusLabel1.BackColor = Color.LightSteelBlue;
            toolTip1.SetToolTip(statusStrip1, "About Land");
        }

        private void tlblLoginName_MouseHover(object sender, EventArgs e)
        {
            
        }

        private void toolStripStatusLabel1_MouseHover(object sender, EventArgs e)
        {
            
        }

        private void tlblLoginName_MouseLeave(object sender, EventArgs e)
        {
            tlblLoginName.BackColor = Color.White;
        }

        private void toolStripStatusLabel1_MouseLeave(object sender, EventArgs e)
        {
            toolStripStatusLabel1.BackColor = Color.White;
        }

        private void tlblRegionInfo_MouseLeave(object sender, EventArgs e)
        {
            tlblRegionInfo.BackColor = Color.White;
        }

        private void tlblParcel_MouseLeave(object sender, EventArgs e)
        {
            tlblParcel.BackColor = Color.White;
        }

        private void tb7_MouseEnter(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(statusStrip1, "Voice disabled");
            tb7.BackColor = Color.LightSteelBlue;
        }

        private void tb2_MouseLeave(object sender, EventArgs e)
        {
            tb2.BackColor = Color.White;
        }

        private void tb1_MouseLeave(object sender, EventArgs e)
        {
            tb1.BackColor = Color.White;
        }

        private void tb3_MouseLeave(object sender, EventArgs e)
        {
            tb3.BackColor = Color.White;
        }

        private void tb4_MouseLeave(object sender, EventArgs e)
        {
            tb4.BackColor = Color.White;
        }

        private void tb5_Click(object sender, EventArgs e)
        {
            //tb5.BackColor = Color.White;
        }

        private void tb5_MouseLeave(object sender, EventArgs e)
        {
            tb5.BackColor = Color.White;
        }

        private void tb6_MouseLeave(object sender, EventArgs e)
        {
            tb6.BackColor = Color.White;
        }

        private void tb7_MouseLeave(object sender, EventArgs e)
        {
            tb7.BackColor = Color.White;
        }

        private void toolStripStatusLabel2_MouseEnter(object sender, EventArgs e)
        {
            toolStripStatusLabel2.BackColor = Color.LightSteelBlue;
            toolTip1.SetToolTip(statusStrip1, "Favourite this parcel");
        }

        private void toolStripStatusLabel2_MouseLeave(object sender, EventArgs e)
        {
            toolStripStatusLabel2.BackColor = Color.White;
        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            if (!netcom.IsLoggedIn) return;
            
            string file = parcel.Name;

            if (file.Length > 32)
            {
                file = file.Substring(0, 32);
            }

            string pos = instance.SIMsittingPos().X.ToString(CultureInfo.CurrentCulture) + ", " + instance.SIMsittingPos().Y.ToString(CultureInfo.CurrentCulture) + ", " + instance.SIMsittingPos().Z.ToString(CultureInfo.CurrentCulture);

            string desc = file + ", " + client.Network.CurrentSim.Name + " (" + pos + ")";

            client.Inventory.RequestCreateItem(instance.FavsFolder,
                    file, desc, AssetType.Landmark, UUID.Random(), InventoryType.Landmark, PermissionMask.All,
                    (success, item) =>
                    {
                        if (!success)
                        {
                            MessageBox.Show("Favourite could not be created", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                        else
                        {
                            List<InventoryBase> invroot = client.Inventory.Store.GetContents(client.Inventory.Store.RootFolder.UUID);

                            foreach (InventoryBase o in invroot)
                            {
                                if (o.Name.ToLower(CultureInfo.CurrentCulture) == "favorites" || o.Name.ToLower(CultureInfo.CurrentCulture) == "my favorites")
                                {
                                    if (o is InventoryFolder)
                                    {
                                        client.Inventory.RequestFolderContents(o.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByDate);
                                    }
                                }
                            }
                        }
                    }
                );
        }

        private void StartUpdater()
        {
            var appcastUrl = "https://megabolt.radegast.life/appcast.xml";
            var manifestModuleName = System.Reflection.Assembly.GetEntryAssembly()?.ManifestModule.FullyQualifiedName;
            var icon = Icon.ExtractAssociatedIcon(manifestModuleName);
            sparkleUpdater = new SparkleUpdater(appcastUrl,
                new Ed25519Checker(NetSparkleUpdater.Enums.SecurityMode.Strict, "YR4STztpPyLnlPwhOVwaL2F7ToCmyXZ53cTt/encBu8="))
            {
                UIFactory = new NetSparkleUpdater.UI.WinForms.UIFactory(icon),
                RelaunchAfterUpdate = true,
                UseNotificationToast = true,
                SecurityProtocolType = System.Net.SecurityProtocolType.Tls12
            };
            sparkleUpdater.StartLoop(true);
        }
    }
}