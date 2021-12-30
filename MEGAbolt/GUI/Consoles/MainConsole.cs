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
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using OpenMetaverse;
using MEGAbolt.NetworkComm;
using MD5library;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MEGAbolt
{
    public partial class MainConsole : UserControl, IMEGAboltTabControl
    {
        private MEGAboltInstance instance;
        private MEGAboltNetcom netcom;
        private GridClient client;
        private string murl;
        private string clickedurl = string.Empty;
        private Dictionary<string, string> MGrids = new();
        private List<string> usernlist = new List<string>();

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

        public MainConsole(MEGAboltInstance instance)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            AddNetcomEvents();

            //while (!IsHandleCreated)
            //{
            //    // Force handle creation
            //    IntPtr temp = Handle;
            //}

            ////btnInfo_Click();
            //if (webBrowser == null)
            //    this.InitializeWebBrowser();

            WebView_Initialize();
            
            //btnInfo.Text = "Hide Grid Status";
            label7.Text = $"v{Assembly.GetExecutingAssembly().GetName().Version}"; 

            Disposed += MainConsole_Disposed;

            LoadGrids();
            InitGridCombo();
            cbxLocation.SelectedIndex = 0;
            InitializeConfig();
        }

        private void LoadGrids()
        {
            try
            {
                MGrids.Clear();
 
                bool fext = File.Exists(DataFolder.GetDataFolder() + "\\Grids.txt");

                if (fext)
                {
                    string[] file = File.ReadAllLines(DataFolder.GetDataFolder() + "\\Grids.txt");

                    MGrids = (from p in file
                              let x = p.Split(',')
                              select x).ToDictionary(a => a[0], a => a[1]);
                }
                else
                {
                    CreateGridFile();

                    string[] file = File.ReadAllLines(DataFolder.GetDataFolder() + "\\Grids.txt");

                    MGrids = (from p in file
                              let x = p.Split(',')
                              select x).ToDictionary(a => a[0], a => a[1]);
                }
            }
            catch { ; }
        }

        static void CreateGridFile()
        {
            StreamWriter SW;

            SW = File.CreateText(DataFolder.GetDataFolder() + "\\Grids.txt");
            SW.WriteLine("OSGrid,http://login.osgrid.org");

            SW.Dispose();
        }

        private void InitGridCombo()
        {
            cbxGrid.Items.Clear();

            cbxGrid.Items.Add("SecondLife Main (Agni)");
            cbxGrid.Items.Add("SecondLife Beta (Aditi)");

            foreach (KeyValuePair<string, string> entry in MGrids)
            {
                cbxGrid.Items.Add(entry.Key);  
            } 

            cbxGrid.Items.Add("Other...");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void SaveUserSettings()
        {
            instance.Config.CurrentConfig.FirstName = txtFirstName.Text;
            instance.Config.CurrentConfig.LastName = txtLastName.Text;

            // Save user list
            string ulist = usernlist.Aggregate(string.Empty, (current, s) => current + (s + "|"));

            if (ulist.EndsWith("|", StringComparison.CurrentCultureIgnoreCase))
            {
                ulist = ulist.Substring(0, ulist.Length - 1);   
            }

            instance.Config.CurrentConfig.UserNameList = ulist; 

            instance.Config.CurrentConfig.iRemPWD = chkPWD.Checked;

            instance.Config.CurrentConfig.PasswordMD5 = 
                LoginOptions.SecondLifePassHashIfNecessary(txtPassword.Text);

            instance.Config.CurrentConfig.LoginLocationType = cbxLocation.SelectedIndex;
            instance.Config.CurrentConfig.LoginLocation = cbxLocation.Text;

            instance.Config.CurrentConfig.LoginGrid = cbxGrid.SelectedIndex;
            instance.Config.CurrentConfig.LoginUri = txtCustomLoginUri.Text;

            instance.Config.SaveCurrentConfig();  
        }

        private void AddNetcomEvents()
        {
            netcom.ClientLoggingIn += netcom_ClientLoggingIn;
            netcom.ClientLoginStatus += netcom_ClientLoginStatus;
            netcom.ClientLoggingOut += netcom_ClientLoggingOut;
            netcom.ClientLoggedOut += netcom_ClientLoggedOut;
        }

        void MainConsole_Disposed(object sender, EventArgs e)
        {
            netcom.ClientLoggingIn -= netcom_ClientLoggingIn;
            netcom.ClientLoginStatus -= netcom_ClientLoginStatus;
            netcom.ClientLoggingOut -= netcom_ClientLoggingOut;
            netcom.ClientLoggedOut -= netcom_ClientLoggedOut;
        }

        private class Item
        {
            public string Name;
            public string Value;

            public Item(string name, string value)
            {
                Name = name; Value = value;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private void InitializeConfig()
        {
            // Populate usernames
            //usernlist
            string ulist = instance.Config.CurrentConfig.UserNameList;

            if (!string.IsNullOrEmpty(ulist))
            {
                string[] llist = ulist.Split('|');

                foreach (string s in llist)
                {
                    string[] llist1 = s.Split('\\');
                    usernlist.Add(s);
                    //cboUserList.Items.Add(s);

                    string epwd = string.Empty;  

                    if (llist1.Length == 2)
                    {
                        epwd = llist1[1];

                        if (!string.IsNullOrEmpty(epwd))
                        {
                            try
                            {
                                Crypto cryp = new Crypto(Crypto.SymmProvEnum.Rijndael);
                                string cpwd = cryp.Decrypt(epwd);
                                epwd = cpwd;
                            }
                            catch
                            {
                                epwd = string.Empty;  
                            }
                        }

                        cboUserList.Items.Add(new Item(llist1[0], epwd));
                    }
                    else
                    {
                        cboUserList.Items.Add(new Item(llist1[0], string.Empty));
                    }
                }
            }

            chkPWD.Checked = instance.Config.CurrentConfig.iRemPWD;
            txtFirstName.Text = instance.Config.CurrentConfig.FirstName;
            txtLastName.Text = instance.Config.CurrentConfig.LastName;

            if (instance.Config.CurrentConfig.iRemPWD)
            {
                string epwd = instance.Config.CurrentConfig.PasswordMD5;

                txtPassword.Text = epwd;
            }

            cbxLocation.SelectedIndex = instance.Config.CurrentConfig.LoginLocationType;
            cbxLocation.Text = instance.Config.CurrentConfig.LoginLocation;

            cbxGrid.SelectedIndex = instance.Config.CurrentConfig.LoginGrid;
            txtCustomLoginUri.Text = instance.Config.CurrentConfig.LoginUri;

            if (instance.ReBooted)
            {
                BeginLogin();
                //btnLogin.PerformClick();
                timer2.Enabled = true;
                timer2.Start(); 
            }
        }

        private void netcom_ClientLoginStatus(object sender, LoginProgressEventArgs e)
        {
            try
            {
                switch (e.Status)
                {
                    case LoginStatus.ConnectingToLogin:
                        lblLoginStatus.Text = "Connecting to login server...";
                        lblLoginStatus.ForeColor = Color.Black;
                        break;

                    case LoginStatus.ConnectingToSim:
                        lblLoginStatus.Text = "Connecting to region...";
                        lblLoginStatus.ForeColor = Color.Black;
                        break;

                    case LoginStatus.Redirecting:
                        lblLoginStatus.Text = "Redirecting...";
                        lblLoginStatus.ForeColor = Color.Black;
                        break;

                    case LoginStatus.ReadingResponse:
                        lblLoginStatus.Text = "Reading response...";
                        lblLoginStatus.ForeColor = Color.Black;
                        break;

                    case LoginStatus.Success:
                        //SetLang();

                        lblLoginStatus.Text = "Logged in as " + netcom.LoginOptions.FullName;
                        lblLoginStatus.ForeColor = Color.Blue;
     
                        string uname = client.Self.Name + "\\";

                        Wildcard wildcard = new Wildcard(client.Self.Name + "*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        List<string> torem = usernlist.Where(s => wildcard.IsMatch(s)).ToList();

                        foreach (var s in torem.Where(s => wildcard.IsMatch(s)))
                        {
                            usernlist.Remove(s);
                        }

                        //string epwd1 = txtPassword.Text;

                        if (chkPWD.Checked)
                        {
                            string epwd = txtPassword.Text;
                            Crypto cryp = new Crypto(Crypto.SymmProvEnum.Rijndael);
                            string cpwd = cryp.Encrypt(epwd);

                            uname += cpwd;
                        }

                        usernlist.Add(uname);

                        btnLogin.Text = "Exit";
                        btnLogin.Enabled = true;

                        instance.ReBooted = false;
                        timer2.Enabled = false;
                        timer2.Stop();

                        try
                        {
                            SaveUserSettings();

                            string fname = client.Self.FirstName + "_" + client.Self.LastName;

                            //instance.Config.ChangeConfigFile(fname);
                            instance.ReapplyConfig(fname);

                            if (instance.Config.CurrentConfig.AIon)
                            {
                                instance.InitAI();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Error trying to save user settings to MEGAbolt.ini ", Helpers.LogLevel.Warning, ex);
                        }

                        //LoadWebPage();

                        client.Self.Movement.Camera.Far = (float)instance.Config.CurrentConfig.RadarRange;

                        break;

                    case LoginStatus.Failed:
                        lblLoginStatus.Text = e.Message;
                        Logger.Log("Login Failed: " + e.FailReason, Helpers.LogLevel.Info);
                        lblLoginStatus.ForeColor = Color.Red;

                        //proLogin.Visible = false;

                        btnLogin.Text = "Retry";
                        btnLogin.Enabled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Login (status): " + ex.Message, Helpers.LogLevel.Error);
            }
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            pnlLoginPrompt.Visible = true;
            pnlLoggingIn.Visible = false;

            btnLogin.Text = "Login";
            btnLogin.Enabled = true;
        }

        private void netcom_ClientLoggingOut(object sender, OverrideEventArgs e)
        {
            btnLogin.Enabled = false;

            lblLoginStatus.Text = "Logging out...";
            lblLoginStatus.ForeColor = Color.FromKnownColor(KnownColor.ControlText);

            //proLogin.Visible = true;
        }

        private void netcom_ClientLoggingIn(object sender, OverrideEventArgs e)
        {
            lblLoginStatus.Text = "Logging in...";
            lblLoginStatus.ForeColor = Color.FromKnownColor(KnownColor.ControlText);

            //proLogin.Visible = true;
            pnlLoggingIn.Visible = true;
            pnlLoginPrompt.Visible = false;

            btnLogin.Enabled = false;
        }

        private void WebView_Initialize()
        {
            WebView.Source = new Uri("https://megabolt.radegast.life/splash.html");
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                ((WebView2)sender).ExecuteScriptAsync(
                    "document.querySelector('body').style.overflow='scroll';" +
                    "var style=document.createElement('style');" +
                    "style.type='text/css';style.innerHTML='::-webkit-scrollbar{display:none}';" +
                    "document.getElementsByTagName('body')[0].appendChild(style)");
            }
        }

        private void BeginLogin()
        {
            try
            {
                if (string.IsNullOrEmpty(txtLastName.Text))
                {
                    txtLastName.Text = "Resident";
                }

                instance.LoggedIn = true;
                netcom.LoginOptions.FirstName = txtFirstName.Text;
                netcom.LoginOptions.LastName = txtLastName.Text;
                netcom.LoginOptions.Password = txtPassword.Text;

                switch (cbxLocation.SelectedIndex)
                {
                    case -1: //Custom
                        netcom.LoginOptions.StartLocation = StartLocationType.Custom;
                        netcom.LoginOptions.StartLocationCustom = cbxLocation.Text;
                        break;

                    case 0: //Home
                        netcom.LoginOptions.StartLocation = StartLocationType.Home;
                        break;

                    case 1: //Last
                        netcom.LoginOptions.StartLocation = StartLocationType.Last;
                        break;
                }

                switch (cbxGrid.SelectedIndex)
                {
                    case 0: //Main grid
                        netcom.LoginOptions.Grid = LoginGrid.MainGrid;
                        break;

                    case 1: //Beta grid
                        netcom.LoginOptions.Grid = LoginGrid.BetaGrid;
                        break;

                    default: //Custom or other
                        netcom.LoginOptions.Grid = LoginGrid.Custom;

                        string selectedgrid = cbxGrid.SelectedItem.ToString();

                        if (selectedgrid == "Other...")
                        {

                            if (txtCustomLoginUri.TextLength == 0 ||
                                txtCustomLoginUri.Text.Trim().Length == 0)
                            {
                                MessageBox.Show("You must specify the Login Uri to connect to a custom grid.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            // Check for http beginning
                            string hhder = string.Empty;

                            if (!txtCustomLoginUri.Text.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase)
                                || !txtCustomLoginUri.Text.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase))
                            {
                                hhder = "http://";
                            }

                            netcom.LoginOptions.GridCustomLoginUri = hhder + txtCustomLoginUri.Text;
                        }
                        else
                        {
                            if (MGrids.ContainsKey(selectedgrid))
                            {
                                netcom.LoginOptions.GridCustomLoginUri = MGrids[selectedgrid];
                            }
                        }

                        break;
                }

                lblLoginStatus.Text = "Logging in...";
                lblLoginStatus.ForeColor = Color.FromKnownColor(KnownColor.ControlText);

                pnlLoggingIn.Visible = true;
                pnlLoginPrompt.Visible = false;

                btnLogin.Enabled = false;
                
                client.Settings.USE_LLSD_LOGIN = instance.Config.CurrentConfig.UseLLSD;
                //instance.SetSettings();  

                netcom.Login();
                //DoBrowser();
            }
            catch (Exception ex)
            {
                Logger.Log("Login (main): " + ex.Message, Helpers.LogLevel.Error);
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            switch (btnLogin.Text)
            {
                case "Login": instance.LogOffClicked = false; BeginLogin(); break;

                case "Retry":
                    pnlLoginPrompt.Visible = true;
                    pnlLoggingIn.Visible = false;
                    btnLogin.Text = "Login";
                    break;

                //case "Logout": this.instance.MainForm.Close(); break;
                case "Exit":
                    instance.LogOffClicked = true;  
                    instance.LoggedIn = false; 
                    pnlLoggingIn.Visible = false;
                    pnlLoginPrompt.Visible = true;
                    btnLogin.Enabled = true;
                    
                    //netcom.Logout();
                    try
                    {
                        if (netcom.IsLoggedIn) netcom.Logout();
                    }
                    catch (Exception ex)
                    {
                        string exp = ex.Message.ToString(CultureInfo.CurrentCulture);
                        MessageBox.Show(exp);  
                    }

                    break;
            }
        }

        private void CreateCmdFile()
        {
            try
            {
                string cuser = $"{txtFirstName.Text} {txtLastName.Text}";
                string textfile = $"{cuser}.bat";
                string path = Path.Combine(DataFolder.GetDataFolder(), textfile);

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                using StreamWriter sr = File.CreateText(path);

                string line = "@ECHO OFF";
                sr.WriteLine(line);
                sr.WriteLine("");
                sr.WriteLine("");
                
                line = $"START \"\" /D \"{Application.StartupPath}\\\" \"{Application.ExecutablePath}\" {cuser.Replace("_", " ")} {txtPassword.Text}";
                sr.WriteLine(line);
                
                sr.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log("Login (create cmd file)", Helpers.LogLevel.Error, ex);
            }
        }

        #region IMEGAboltTabControl Members

        public void RegisterTab(MEGAboltTab tab)
        {
            tab.DefaultControlButton = btnLogin;
        }

        #endregion IMEGAboltTabControl Members

        private void MainConsole_Load(object sender, EventArgs e)
        {
            
        }

        private void chkPWD_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void cbxGrid_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbxGrid.SelectedItem.ToString() == "Other...") //Custom option is selected
            {
                txtCustomLoginUri.Enabled = true;
                txtCustomLoginUri.Text = "http://";
                txtCustomLoginUri.Select();

                cbxGrid.Width = 157;
                button1.Visible = true;
                button2.Visible = true; 
            }
            else
            {
                txtCustomLoginUri.Enabled = false;
                txtCustomLoginUri.Text = string.Empty;

                cbxGrid.Width = 210;
                button1.Visible = false;
                button2.Visible = false; 
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            BeginLogin();
        }

        private void cbxLocation_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void txtFirstName_Enter(object sender, EventArgs e)
        {
            txtFirstName.SelectionStart = 0;
            txtFirstName.SelectionLength = txtFirstName.Text.Length;
            txtFirstName.Text = txtFirstName.SelectedText;
        }

        private void txtLastName_Enter(object sender, EventArgs e)
        {
            txtLastName.SelectAll();
        }

        private void txtFirstName_Click(object sender, EventArgs e)
        {
            txtFirstName.SelectAll();
        }

        private void txtLastName_Click(object sender, EventArgs e)
        {
            txtLastName.SelectAll();
        }

        private void txtPassword_Click(object sender, EventArgs e)
        {
            txtPassword.SelectAll();
        }

        private void txtPassword_Enter(object sender, EventArgs e)
        {
            txtPassword.SelectAll();
        }

        private void cboUserList_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cboUserList.SelectedIndex == -1) return;

                Item itm = (Item)cboUserList.SelectedItem;

                string[] name = itm.Name.ToString(CultureInfo.CurrentCulture).Split(' ');  // cboUserList.SelectedItem.ToString().Split(' ');

                txtFirstName.Text = name[0];
                txtLastName.Text = name[1];
                txtPassword.Text = itm.Value;   // cboUserList.SelectedValue.ToString(); 

                txtPassword.Focus();
            }
            catch { ; }
        }

        private void txtLastName_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string fullfile = DataFolder.GetDataFolder() + "\\Grids.txt"; ;

            try
            {
                Utilities.OpenBrowser(fullfile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "MEGAbolt");  
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            LoadGrids();
            InitGridCombo();
            cbxGrid.SelectedIndex = 0; 
        }
    }
}
