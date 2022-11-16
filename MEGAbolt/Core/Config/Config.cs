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
using OpenMetaverse;
using Nini.Config;
using System.Windows.Forms;
using System.Drawing;
using MEGAcrypto;
using System.IO;
using System.Globalization;


namespace MEGAbolt
{
    public class Config
    {
        //private bool iradar = false;

        //private string tweetername = string.Empty;
        //private string tweeterpwd = string.Empty;
        //private bool enabletweeter = false;
        //private bool enablechattweets = false;
        //private bool tweet = true;
        //private string tweetuser = string.Empty;

        //added by GM on 2-JUL-2009
        private UUID chairAnnouncerUuid = UUID.Zero;
        private int chairAnnouncerInterval = 5;
        private UUID chairAnnouncerGroup1 = UUID.Zero;
        private UUID chairAnnouncerGroup2 = UUID.Zero;
        private UUID chairAnnouncerGroup3 = UUID.Zero;
        private UUID chairAnnouncerGroup4 = UUID.Zero;
        private UUID chairAnnouncerGroup5 = UUID.Zero;
        private UUID chairAnnouncerGroup6 = UUID.Zero;

        //added by GM on 1-APR-2010

        // Incoming command identifier 04 Aug 2009

        //private string logdir = Application.StartupPath.ToString() + "\\Logs\\";

        private int headerbackcolour = Color.Lavender.ToArgb();
        //private int bgcolour = Color.White.ToArgb();   

        //private bool broadcastid = true;
        private bool disablehttpinv = true;

        public Config()
        {

        }

        public static Config LoadFrom(string filename)
        {
            Config config = new Config();

            try
            {
                IConfigSource conf = new IniConfigSource(filename);

                config.Version = conf.Configs["General"].GetInt("Version", 0);

                config.MainWindowState = conf.Configs["Interface"].GetInt("MainWindowState", 0);
                config.InterfaceStyle = conf.Configs["Interface"].GetInt("Style", 1);

                // Login
                config.FirstName = conf.Configs["Login"].GetString("FirstName", string.Empty);
                config.LastName = conf.Configs["Login"].GetString("LastName", string.Empty);

                string epwd = conf.Configs["Login"].GetString("Password", string.Empty);

                config.LoginGrid = conf.Configs["Login"].GetInt("Grid", 0);
                config.LoginUri = conf.Configs["Login"].GetString("Uri", string.Empty);
                config.LoginLocationType = conf.Configs["Login"].GetInt("LocationType", 0);
                config.LoginLocation = conf.Configs["Login"].GetString("Location", string.Empty);
                config.iRemPWD = conf.Configs["Login"].GetBoolean("iRemPWD", false);
                config.UserNameList = conf.Configs["Login"].GetString("UserNameList", string.Empty);

                // General
                config.Connect4 = conf.Configs["General"].GetBoolean("Connect4", false);
                config.DisableNotifications = conf.Configs["General"].GetBoolean("DisableNotifications", false);
                config.DisableInboundGroupInvites = conf.Configs["General"].GetBoolean("DisableInboundGroupInvites", false);
                config.AutoSit = conf.Configs["General"].GetBoolean("AutoSit", false);
                config.RadarRange = conf.Configs["General"].GetInt("RadarRange", 64);
                config.ObjectRange = conf.Configs["General"].GetInt("ObjectRange", 20);
                config.GroupManPro = conf.Configs["General"].GetString("GroupManPro");
                config.HeaderFont = conf.Configs["General"].GetString("HeaderFont", "Tahoma");
                config.HeaderFontStyle = conf.Configs["General"].GetString("HeaderFontStyle", "Regular");
                config.HeaderFontSize = conf.Configs["General"].GetFloat("HeaderFontSize", 8.5f);
                config.StartMinimised = conf.Configs["General"].GetBoolean("StartMinimised", false);
                config.HideDisconnectPrompt = conf.Configs["General"].GetBoolean("HideDisconnectPrompt", false);

                try
                {
                    int clr = conf.Configs["General"].GetInt("HeaderBackColour", Color.Lavender.ToArgb());
                    config.HeaderBackColour = Color.FromArgb(clr);
                }
                catch
                {
                    config.HeaderBackColour = Color.Lavender;
                }

                config.TextFont = conf.Configs["General"].GetString("TextFont", "Tahoma");
                config.TextFontStyle = conf.Configs["General"].GetString("TextFontStyle", "Regular");
                config.TextFontSize = conf.Configs["General"].GetFloat("TextFontSize", 8.5f);
                config.GivePresent = conf.Configs["General"].GetBoolean("GivePresent", false);
                config.HideMeta = conf.Configs["General"].GetBoolean("HideMeta", true);
                config.DeclineInv = conf.Configs["General"].GetBoolean("DeclineInv", false);
                config.DisableLookAt = conf.Configs["General"].GetBoolean("DisableLookAt", true);
                config.ClassicChatLayout = conf.Configs["General"].GetBoolean("ClassicChatLayout", false);
                config.AutoRestart = conf.Configs["General"].GetBoolean("AutoRestart", false);
                config.LogOffTime = conf.Configs["General"].GetInt("LogOffTime", 0);
                config.ReStartTime = conf.Configs["General"].GetInt("ReStartTime", 10);
                config.BandwidthThrottle = conf.Configs["General"].GetFloat("BandwidthThrottle", 500f);

                config.PlayFriendOnline = conf.Configs["General"].GetBoolean("PlayFriendOnline", false);
                config.PlayFriendOffline = conf.Configs["General"].GetBoolean("PlayFriendOffline", false);
                config.PlayIMreceived = conf.Configs["General"].GetBoolean("PlayIMreceived", false);
                config.PlayGroupIMreceived = conf.Configs["General"].GetBoolean("PlayGroupIMreceived", false);
                config.PlayGroupNoticeReceived = conf.Configs["General"].GetBoolean("PlayGroupNoticeReceived", false);
                config.PlayInventoryItemReceived = conf.Configs["General"].GetBoolean("PlayInventoryItemReceived", false);
                config.PlayPaymentReceived = conf.Configs["General"].GetBoolean("PlayPaymentReceived", false);
                config.AutoAcceptItems = conf.Configs["General"].GetBoolean("AutoAcceptItems", false);
                config.AdRemove = conf.Configs["General"].GetString("AdRemove", string.Empty);
                config.MasterAvatar = conf.Configs["General"].GetString("MasterAvatar", UUID.Zero.ToString());
                config.EnforceLSLsecurity = conf.Configs["General"].GetBoolean("EnforceLSLsecurity", true);
                config.DisplayLSLcommands = conf.Configs["General"].GetBoolean("DisplayLSLcommands", true);

                // backward compatibility pre V 0.9.47.0

                if (string.IsNullOrEmpty(config.MasterAvatar))
                {
                    config.MasterAvatar = UUID.Zero.ToString();   
                }

                config.MasterObject = conf.Configs["General"].GetString("MasterObject", UUID.Zero.ToString());
                config.AutoTransfer = conf.Configs["General"].GetBoolean("AutoTransfer", false);
                config.DisableTrayIcon = conf.Configs["General"].GetBoolean("DisableTrayIcon", false);
                config.DisableFriendsNotifications = conf.Configs["General"].GetBoolean("DisableFriendsNotifications", false);
                config.DisableTyping = conf.Configs["General"].GetBoolean("DisableTyping", false);
                config.AutoAcceptFriends = conf.Configs["General"].GetBoolean("AutoAcceptFriends", false);
                //config.BroadcastID = conf.Configs["General"].GetBoolean("BroadcastID", true);
                config.DisableRadar = conf.Configs["General"].GetBoolean("DisableRadar", false);
                config.RestrictRadar = conf.Configs["General"].GetBoolean("RestrictRadar", false);
                config.DisableVoice = conf.Configs["General"].GetBoolean("DisableVoice", false);
                config.DisableFavs = conf.Configs["General"].GetBoolean("DisableFavs", false);
                config.DisableRadarImageMiniMap = conf.Configs["General"].GetBoolean("DisableRadarImageMiniMap", false);
                config.AppMenuPos = conf.Configs["General"].GetString("AppMenuPos", "Top");
                config.LandMenuPos = conf.Configs["General"].GetString("LandMenuPos", "Top");
                config.FnMenuPos = conf.Configs["General"].GetString("FnMenuPos", "Top");
                config.UseLLSD = conf.Configs["General"].GetBoolean("UseLLSD", false);
                config.ChatBufferLimit = conf.Configs["General"].GetInt("ChatBufferLimit", 20);
                config.ScriptUrlBufferLimit = conf.Configs["General"].GetInt("ScriptUrlBufferLimit", 5);
                
                // AI    
                config.AIon = conf.Configs["AI"].GetBoolean("AIon", false);
                config.ReplyAI = conf.Configs["AI"].GetBoolean("ReplyAI", false);
                config.ReplyText = conf.Configs["AI"].GetString("ReplyText", "I am sorry but I didn't understand what you said or I haven't been taught a response for it. Can you try again, making sure your sentences are short and clear.");
                config.MultiLingualAI = conf.Configs["AI"].GetBoolean("MultiLingualAI", false);

                config.ChatTimestamps = conf.Configs["Text"].GetBoolean("ChatTimestamps", true);
                config.IMTimestamps = conf.Configs["Text"].GetBoolean("IMTimestamps", true);
                config.ChatSmileys = conf.Configs["Text"].GetBoolean("ChatSmileys", false);
                config.lineMax = conf.Configs["Text"].GetInt("lineMax", 5000);
                config.ParcelMusic = conf.Configs["Text"].GetBoolean("ParcelMusic", true);
                config.ParcelMedia = conf.Configs["Text"].GetBoolean("ParcelMedia", true);
                config.UseSLT = conf.Configs["Text"].GetBoolean("UseSLT", false);
                config.PlaySound = conf.Configs["Text"].GetBoolean("PlaySound", false);
                config.SaveIMs = conf.Configs["Text"].GetBoolean("SaveIMs", true);
                config.SaveChat = conf.Configs["Text"].GetBoolean("SaveChat", false);
                config.LogDir = conf.Configs["Text"].GetString("LogDir", DataFolder.GetDataFolder() + "\\Logs\\");
                config.DisableGroupNotices = conf.Configs["Text"].GetBoolean("DisableGroupNotices", true);
                config.DisableGroupIMs = conf.Configs["Text"].GetBoolean("DisableGroupIMs", false);
                config.BusyReply = conf.Configs["Text"].GetString("BusyReply", "The Resident you messaged is in 'busy mode' which means they have requested not to be disturbed.  Your message will still be shown in their IM panel for later viewing.");
                config.InitialIMReply = conf.Configs["Text"].GetString("InitialIMReply", "");

                // Proxy
                config.UseProxy =  conf.Configs["Proxy"].GetBoolean("UseProxy", false);
                config.ProxyURL = conf.Configs["Proxy"].GetString("ProxyURL", string.Empty);
                config.ProxyPort = conf.Configs["Proxy"].GetString("ProxyPort", string.Empty);
                config.ProxyUser = conf.Configs["Proxy"].GetString("ProxyUser", string.Empty);
                config.ProxyPWD = conf.Configs["Proxy"].GetString("ProxyPWD", string.Empty);

                // MEGA3D    
                try
                {
                    config.DisableMipmaps = conf.Configs["MEGA3D"].GetBoolean("DisableMipmaps", false);
                }
                catch { ; }

                config.PluginsToLoad = conf.Configs["LoadedPlugIns"].GetString("PluginsToLoad", string.Empty);

                try
                {
                    if (!string.IsNullOrEmpty(epwd))
                    {
                        Crypto cryp = new Crypto(Crypto.SymmProvEnum.AES);
                        string cpwd = cryp.Decrypt(epwd);

                        config.PasswordMD5 = cpwd;
                    }
                    else
                    {
                        config.PasswordMD5 = epwd;
                    }
                }
                catch
                {
                    epwd = config.PasswordMD5 = string.Empty;
                    MessageBox.Show("An error occured while decrypting your stored password.\n" +
                                    "This could mean you have the old format INI file. \n" +
                                    "You will have to re-enter your password so it can be encrypted with the new method.",
                        "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\MEGAbolt\\");
                }

                //added by GM on 2-JUL-2009
                config.GroupManagerUID = conf.Configs["PlugIn"].GetString("GroupManager", "ned49b54-325d-486a-af3m");
                config.chairAnnouncerUuid = UUID.Parse(conf.Configs["PlugIn"].GetString("ChairAnnouncer", UUID.Zero.ToString()));
                config.chairAnnouncerInterval = conf.Configs["PlugIn"].GetInt("ChairAnnouncerInterval", 5);
                config.chairAnnouncerGroup1 = UUID.Parse(conf.Configs["PlugIn"].GetString("ChairAnnouncerGroup1", UUID.Zero.ToString()));
                config.chairAnnouncerGroup2 = UUID.Parse(conf.Configs["PlugIn"].GetString("ChairAnnouncerGroup2", UUID.Zero.ToString()));
                config.chairAnnouncerGroup3 = UUID.Parse(conf.Configs["PlugIn"].GetString("ChairAnnouncerGroup3", UUID.Zero.ToString()));
                config.chairAnnouncerGroup4 = UUID.Parse(conf.Configs["PlugIn"].GetString("ChairAnnouncerGroup4", UUID.Zero.ToString()));
                config.chairAnnouncerGroup5 = UUID.Parse(conf.Configs["PlugIn"].GetString("ChairAnnouncerGroup5", UUID.Zero.ToString()));
                config.chairAnnouncerGroup6 = UUID.Parse(conf.Configs["PlugIn"].GetString("ChairAnnouncerGroup6", UUID.Zero.ToString()));
                config.ChairAnnouncerEnabled = conf.Configs["PlugIn"].GetBoolean("ChairAnnouncerEnabled", false);
                config.ChairAnnouncerChat = conf.Configs["PlugIn"].GetBoolean("ChairAnnouncerChat", true);
                //added by GM on 1-APR-2010
                config.ChairAnnouncerAdvert = conf.Configs["PlugIn"].GetString("ChairAnnouncerAdvert", "Brought to you by MEGAbolt"); // removed reference to Machin's Machines, no longer exist
                //throw new Exception("Test");

                try
                {
                    // Spelling
                    config.EnableSpelling = conf.Configs["Spelling"].GetBoolean("EnableSpelling", false);
                    config.SpellLanguage = conf.Configs["Spelling"].GetString("SpellLanguage", "en-US");
                }
                catch { ; }
            }
            catch (Exception exp)
            {
                try
                {
                    exp.HelpLink = "http://megabolt.radegast.life/help/"; // updated link addy
                    //Logger.Log("ERROR while loading config file'" + filename + "'. Your settings may not have fully loaded. Message: " + exp.Message, Helpers.LogLevel.Error);
                    MessageBox.Show("The was an error when loading your Config (MEGAbolt.ini) file.\nNot all of your settings may have been loaded.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch { ; }
            }

            return config;
        }

        public void Save(string filename)
        {
            IniConfigSource source = new IniConfigSource();

            // General
            IConfig config = source.AddConfig("General");
            config.Set("Version", Version.ToString(CultureInfo.CurrentCulture));
            //config.Set("iRadar", iradar.ToString());
            config.Set("Connect4", Connect4.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableNotifications", DisableNotifications.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableInboundGroupInvites", DisableInboundGroupInvites.ToString(CultureInfo.CurrentCulture));
            config.Set("AutoSit", AutoSit.ToString(CultureInfo.CurrentCulture));
            config.Set("RadarRange", RadarRange.ToString(CultureInfo.CurrentCulture));
            config.Set("ObjectRange", ObjectRange.ToString(CultureInfo.CurrentCulture));
            config.Set("GroupManPro", GroupManPro);
            config.Set("GivePresent", GivePresent.ToString(CultureInfo.CurrentCulture));
            config.Set("HideMeta", HideMeta.ToString(CultureInfo.CurrentCulture));
            config.Set("DeclineInv", DeclineInv.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableLookAt", DisableLookAt);
            config.Set("AutoRestart", AutoRestart.ToString(CultureInfo.CurrentCulture));
            config.Set("LogOffTime", LogOffTime.ToString(CultureInfo.CurrentCulture));
            config.Set("ReStartTime", ReStartTime.ToString(CultureInfo.CurrentCulture));
            config.Set("BandwidthThrottle", BandwidthThrottle.ToString(CultureInfo.CurrentCulture));
            config.Set("ClassicChatLayout", ClassicChatLayout.ToString(CultureInfo.CurrentCulture));
            config.Set("HideDisconnectPrompt", HideDisconnectPrompt.ToString(CultureInfo.CurrentCulture));

            if (HeaderFont == null)
            {
                HeaderFont = "Tahoma";
                HeaderFontStyle = "Regular";
                HeaderFontSize = 8.5f;
                headerbackcolour = Color.Lavender.ToArgb();
            }

            config.Set("HeaderFont", HeaderFont);
            config.Set("HeaderFontStyle", HeaderFontStyle);
            config.Set("HeaderFontSize", HeaderFontSize.ToString(CultureInfo.CurrentCulture));
            config.Set("HeaderBackColour", headerbackcolour.ToString(CultureInfo.CurrentCulture));
            //config.Set("BgColour", bgcolour.ToString());

            if (TextFont == null)
            {
                TextFont = "Tahoma";
                TextFontStyle = "Regular";
                TextFontSize = 8.5f;
            }

            config.Set("TextFont", TextFont);
            config.Set("TextFontStyle", TextFontStyle);
            config.Set("TextFontSize", TextFontSize.ToString(CultureInfo.CurrentCulture));
            config.Set("PlayFriendOnline", PlayFriendOnline.ToString(CultureInfo.CurrentCulture));
            config.Set("PlayFriendOffline", PlayFriendOffline.ToString(CultureInfo.CurrentCulture));
            config.Set("PlayIMreceived", PlayIMreceived.ToString(CultureInfo.CurrentCulture));
            config.Set("PlayGroupIMreceived", PlayGroupIMreceived.ToString(CultureInfo.CurrentCulture));
            config.Set("PlayGroupNoticeReceived", PlayGroupNoticeReceived.ToString(CultureInfo.CurrentCulture));
            config.Set("PlayInventoryItemReceived", PlayInventoryItemReceived.ToString(CultureInfo.CurrentCulture));
            config.Set("PlayPaymentReceived", PlayPaymentReceived.ToString(CultureInfo.CurrentCulture));
            config.Set("AutoAcceptItems", AutoAcceptItems.ToString(CultureInfo.CurrentCulture));
            config.Set("StartMinimised", StartMinimised.ToString(CultureInfo.CurrentCulture));
            config.Set("AdRemove", AdRemove);
            config.Set("MasterAvatar", MasterAvatar);
            config.Set("MasterObject", MasterObject);
            config.Set("EnforceLSLsecurity", EnforceLSLsecurity.ToString(CultureInfo.CurrentCulture));
            config.Set("DisplayLSLcommands", DisplayLSLcommands.ToString(CultureInfo.CurrentCulture));  
            config.Set("AutoTransfer", AutoTransfer.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableTrayIcon", DisableTrayIcon.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableFriendsNotifications", DisableFriendsNotifications.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableTyping", DisableTyping.ToString(CultureInfo.CurrentCulture));
            config.Set("AutoAcceptFriends", AutoAcceptFriends.ToString(CultureInfo.CurrentCulture));
            //config.Set("BroadcastID", broadcastid.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableRadar", DisableRadar.ToString(CultureInfo.CurrentCulture));
            config.Set("RestrictRadar", RestrictRadar.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableVoice", DisableVoice.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableFavs", DisableFavs.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableHTTPinv", disablehttpinv.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableRadarImageMiniMap", DisableRadarImageMiniMap.ToString(CultureInfo.CurrentCulture));
            config.Set("AppMenuPos", AppMenuPos.ToString(CultureInfo.CurrentCulture));
            config.Set("LandMenuPos", LandMenuPos.ToString(CultureInfo.CurrentCulture));
            config.Set("FnMenuPos", FnMenuPos.ToString(CultureInfo.CurrentCulture));
            config.Set("UseLLSD", UseLLSD.ToString(CultureInfo.CurrentCulture));

            config.Set("ChatBufferLimit", ChatBufferLimit.ToString(CultureInfo.CurrentCulture));
            config.Set("ScriptUrlBufferLimit", ScriptUrlBufferLimit.ToString(CultureInfo.CurrentCulture));
            
            // Interface
            config = source.AddConfig("Interface");
            config.Set("MainWindowState", MainWindowState.ToString(CultureInfo.CurrentCulture));
            config.Set("Style", InterfaceStyle.ToString(CultureInfo.CurrentCulture));

            // Login
            config = source.AddConfig("Login");
            config.Set("FirstName", FirstName);
            config.Set("LastName", LastName);

            if (iRemPWD)
            {
                string epwd = PasswordMD5;

                if (!string.IsNullOrEmpty(epwd))
                {
                    Crypto cryp = new Crypto(Crypto.SymmProvEnum.AES);
                    string cpwd = cryp.Encrypt(epwd);

                    config.Set("Password", cpwd);
                }
            }
            else
            {
                config.Set("Password", string.Empty);
            }

            config.Set("UserNameList", UserNameList);
            config.Set("Grid", LoginGrid.ToString(CultureInfo.CurrentCulture));
            config.Set("Uri", LoginUri);
            config.Set("LocationType", LoginLocationType.ToString(CultureInfo.CurrentCulture));
            config.Set("Location", LoginLocation);
            config.Set("iRemPWD", iRemPWD.ToString(CultureInfo.CurrentCulture));

            // AI
            config = source.AddConfig("AI");
            config.Set("AIon", AIon.ToString(CultureInfo.CurrentCulture));
            config.Set("ReplyAI", ReplyAI.ToString(CultureInfo.CurrentCulture));
            config.Set("ReplyText", ReplyText);
            config.Set("MultiLingualAI", MultiLingualAI.ToString(CultureInfo.CurrentCulture));

            // Text
            config = source.AddConfig("Text");
            config.Set("ChatTimestamps", ChatTimestamps.ToString(CultureInfo.CurrentCulture));
            config.Set("IMTimestamps", IMTimestamps.ToString(CultureInfo.CurrentCulture));
            config.Set("ChatSmileys", ChatSmileys.ToString(CultureInfo.CurrentCulture));
            config.Set("ParcelMusic", ParcelMusic.ToString(CultureInfo.CurrentCulture));
            config.Set("ParcelMedia", ParcelMedia.ToString(CultureInfo.CurrentCulture));
            config.Set("lineMax", lineMax.ToString(CultureInfo.CurrentCulture));
            config.Set("UseSLT", UseSLT.ToString(CultureInfo.CurrentCulture));
            config.Set("PlaySound", PlaySound.ToString(CultureInfo.CurrentCulture));
            config.Set("BusyReply", BusyReply);
            config.Set("InitialIMReply", InitialIMReply);
            config.Set("SaveIMs", SaveIMs.ToString(CultureInfo.CurrentCulture));
            config.Set("SaveChat", SaveChat.ToString(CultureInfo.CurrentCulture));
            config.Set("LogDir", LogDir);
            config.Set("DisableGroupNotices", DisableGroupNotices.ToString(CultureInfo.CurrentCulture));
            config.Set("DisableGroupIMs", DisableGroupIMs.ToString(CultureInfo.CurrentCulture));

            //// Twitter
            //config = source.AddConfig("Twitter");
            //config.Set("TweeterName", tweetername);
            //config.Set("TweeterPwd", tweeterpwd);
            //config.Set("EnableTweeter", enabletweeter.ToString());
            //config.Set("EnableChatTweets", enablechattweets.ToString());   
            //config.Set("Tweet", tweet.ToString());
            //config.Set("TweeterUser", tweetuser);

            // Proxy
            config = source.AddConfig("Proxy");
            config.Set("UseProxy", UseProxy.ToString(CultureInfo.CurrentCulture));
            config.Set("ProxyURL", ProxyURL);
            config.Set("ProxyPort", ProxyPort);
            config.Set("ProxyUser", ProxyUser);
            config.Set("ProxyPWD", ProxyPWD);

            // MEGA3D
            config = source.AddConfig("MEGA3D");
            config.Set("DisableMipmaps", DisableMipmaps.ToString(CultureInfo.CurrentCulture));

            // Plugins Loaded
            config = source.AddConfig("LoadedPlugIns");
            config.Set("PluginsToLoad", PluginsToLoad);   

            // Plugins
            //added by GM on 2-JUL-2009
            config = source.AddConfig("PlugIn");
            //don't save if default
            if (GroupManagerUID != "ned49b54-325d-486a-af3m")
            {
                config.Set("GroupManager", GroupManagerUID);
            }
            config.Set("ChairAnnouncer", chairAnnouncerUuid.ToString());
            config.Set("ChairAnnouncerInterval", chairAnnouncerInterval);
            config.Set("ChairAnnouncerGroup1", chairAnnouncerGroup1.ToString());
            config.Set("ChairAnnouncerGroup2", chairAnnouncerGroup2.ToString());
            config.Set("ChairAnnouncerGroup3", chairAnnouncerGroup3.ToString());
            config.Set("ChairAnnouncerGroup4", chairAnnouncerGroup4.ToString());
            config.Set("ChairAnnouncerGroup5", chairAnnouncerGroup5.ToString());
            config.Set("ChairAnnouncerGroup6", chairAnnouncerGroup6.ToString());
            config.Set("ChairAnnouncerEnabled", ChairAnnouncerEnabled);
            config.Set("ChairAnnouncerChat", ChairAnnouncerChat);
            //added by GM on 1-APR-2009
            config.Set("ChairAnnouncerAdvert", ChairAnnouncerAdvert);

            config = source.AddConfig("Spelling");
            config.Set("EnableSpelling", EnableSpelling.ToString(CultureInfo.CurrentCulture));
            config.Set("SpellLanguage", SpellLanguage);

            FileInfo newFileInfo = new FileInfo(Path.Combine(DataFolder.GetDataFolder(), filename));

            if (newFileInfo.Exists)
            {
                if (newFileInfo.IsReadOnly)
                {
                    newFileInfo.IsReadOnly = false;
                }
            }

            source.Save(filename);
        }

        public int Version { get; set; } = 1;

        public int MainWindowState { get; set; } = 0;

        public int InterfaceStyle { get; set; } = 1;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string PasswordMD5 { get; set; } = string.Empty;

        //public bool Md5
        //{
        //    get { return md5; }
        //    set { md5 = value; }
        //}

        public int LoginLocationType { get; set; } = 0;

        public string LoginLocation { get; set; } = string.Empty;

        public int LoginGrid { get; set; } = 0;

        public string LoginUri { get; set; } = string.Empty;

        public bool ChatTimestamps { get; set; } = true;

        public bool ChatSmileys { get; set; } = false;

        public bool IMTimestamps { get; set; } = true;

        public bool ParcelMusic { get; set; } = false;

        public bool ParcelMedia { get; set; } = false;

        public bool iRemPWD { get; set; } = false;

        //public bool iRadar
        //{
        //    get { return iradar; }
        //    set { iradar = value; }
        //}

        public string pURL { get; set; } = string.Empty;

        public string mURL { get; set; } = string.Empty;

        public UUID ObjectsFolder { get; set; } = UUID.Zero;

        public bool DisableNotifications { get; set; } = false;

        public int lineMax { get; set; } = 5000;

        //public bool EnableTweeter
        //{
        //    get { return enabletweeter; }
        //    set { enabletweeter = value; }
        //}

        //public bool EnableChatTweets
        //{
        //    get { return enablechattweets; }
        //    set { enablechattweets = value; }
        //}

        //public string TweeterName
        //{
        //    get { return tweetername; }
        //    set { tweetername = value; }
        //}

        //public string TweeterPwd
        //{
        //    get { return tweeterpwd; }
        //    set { tweeterpwd = value; }
        //}

        //public bool Tweet
        //{
        //    get { return tweet; }
        //    set { tweet = value; }
        //}

        //public string TweeterUser
        //{
        //    get { return tweetuser; }
        //    set { tweetuser = value; }
        //}

        public bool Connect4 { get; set; } = false;

        public bool AIon { get; set; } = false;

        public bool AutoSit { get; set; } = false;

        public int RadarRange { get; set; } = 64;

        public bool UseSLT { get; set; } = false;

        public int ObjectRange { get; set; } = 20;

        public string GroupManPro { get; set; } = string.Empty;

        public bool PlaySound { get; set; } = false;

        public bool HideMeta { get; set; } = false;

        public string BusyReply { get; set; } = "The Resident you messaged is in 'busy mode' which means they have requested not to be disturbed.  Your message will still be shown in their IM panel for later viewing.";

        public string InitialIMReply { get; set; } = string.Empty;

        public bool DeclineInv { get; set; } = false;

        public bool ReplyAI { get; set; } = false;

        public string ReplyText { get; set; } = "I am sorry but I didn't understand what you said or I haven't been taught a response for it. Can you try again, making sure your sentences are short and clear.";

        //added by GM on 2-JUL-2009
        public string GroupManagerUID { get; set; } = "ned49b54-325d-486a-af3m";

        public UUID ChairAnnouncerUUID
        {
            get => chairAnnouncerUuid;
            set => chairAnnouncerUuid = value;
        }

        public int ChairAnnouncerInterval
        {
            get => chairAnnouncerInterval < 5 ? 5 : chairAnnouncerInterval; //spam protection
            set => chairAnnouncerInterval = value;
        }

        public UUID ChairAnnouncerGroup1
        {
            get => chairAnnouncerGroup1;
            set => chairAnnouncerGroup1 = value;
        }

        public UUID ChairAnnouncerGroup2
        {
            get => chairAnnouncerGroup2;
            set => chairAnnouncerGroup2 = value;
        }

        public UUID ChairAnnouncerGroup3
        {
            get => chairAnnouncerGroup3;
            set => chairAnnouncerGroup3 = value;
        }

        public UUID ChairAnnouncerGroup4
        {
            get => chairAnnouncerGroup4;
            set => chairAnnouncerGroup4 = value;
        }

        public UUID ChairAnnouncerGroup5
        {
            get => chairAnnouncerGroup5;
            set => chairAnnouncerGroup5 = value;
        }

        public UUID ChairAnnouncerGroup6
        {
            get => chairAnnouncerGroup6;
            set => chairAnnouncerGroup6 = value;
        }

        public bool ChairAnnouncerEnabled { get; set; } = false;

        public bool ChairAnnouncerChat { get; set; } = true;

        public string ChairAnnouncerAdvert { get; set; } = "Brought to you by MEGAbolt";


        public string CommandInID { get; set; } = "ned34b54-3765-439j-fds5";

        public bool SaveIMs { get; set; } = true;

        public bool SaveChat { get; set; } = false;

        public string LogDir { get; set; } = DataFolder.GetDataFolder() + "\\Logs\\";

        public bool DisableGroupNotices { get; set; } = false;

        public bool DisableInboundGroupInvites { get; set; } = false;

        public bool DisableGroupIMs { get; set; } = false;

        public bool BufferApplied { get; set; } = false;

        public bool DisableLookAt { get; set; } = true;

        public bool GivePresent { get; set; } = false;

        public bool UseProxy { get; set; } = false;

        public string ProxyURL { get; set; } = string.Empty;

        public string ProxyPort { get; set; } = string.Empty;

        public string ProxyUser { get; set; } = string.Empty;

        public string ProxyPWD { get; set; } = string.Empty;

        public bool AutoRestart { get; set; } = true;

        public int LogOffTime { get; set; } = 0;

        public float BandwidthThrottle { get; set; } = 500;

        public bool LogOffTimerChanged { get; set; } = true;

        public bool ClassicChatLayout { get; set; } = false;

        public string HeaderFont { get; set; } = "Tahoma";

        public string HeaderFontStyle { get; set; } = "Regular";

        public float HeaderFontSize { get; set; } = 8.5f;

        public Color HeaderBackColour
        {
            get => Color.FromArgb(headerbackcolour);
            set => headerbackcolour = value.ToArgb();
        }

        //public Color BgColour
        //{
        //    get { return Color.FromArgb(bgcolour); }
        //    set { bgcolour = value.ToArgb(); }
        //}

        public string TextFont { get; set; } = "Tahoma";

        public string TextFontStyle { get; set; } = "Regular";

        public float TextFontSize { get; set; } = 8.5f;

        public string PluginsToLoad { get; set; } = string.Empty;

        public bool PlayFriendOnline { get; set; } = false;

        public bool PlayFriendOffline { get; set; } = false;

        public bool PlayIMreceived { get; set; } = false;

        public bool PlayGroupIMreceived { get; set; } = false;

        public bool PlayGroupNoticeReceived { get; set; } = false;

        public bool PlayInventoryItemReceived { get; set; } = false;

        public bool PlayPaymentReceived { get; set; } = false;

        public bool AutoAcceptItems { get; set; } = false;

        public bool StartMinimised { get; set; } = false;

        public string AdRemove { get; set; } = string.Empty;

        public string MasterAvatar { get; set; } = UUID.Zero.ToString();

        public string MasterObject { get; set; } = UUID.Zero.ToString();

        public bool AutoTransfer { get; set; } = false;

        public bool SortByDistance { get; set; } = true;

        public bool DisableTrayIcon { get; set; } = false;

        public string IgnoreUID { get; set; } = "ned49b54-325d-123a-x33m";

        public string UserNameList { get; set; } = string.Empty;

        public bool DisableFriendsNotifications { get; set; } = false;

        public bool DisableTyping { get; set; } = false;

        public bool AutoAcceptFriends { get; set; } = false;

        public int ReStartTime { get; set; } = 10;

        public bool DisableMipmaps { get; set; } = false;

        public bool EnforceLSLsecurity { get; set; } = true;

        public bool DisplayLSLcommands { get; set; } = true;

        public bool MultiLingualAI { get; set; } = false;

        public bool EnableSpelling { get; set; } = false;

        public string SpellLanguage { get; set; } = "en-US";

        public bool HideDisconnectPrompt { get; set; } = false;

        public bool DisableRadar { get; set; } = false;

        public bool RestrictRadar { get; set; } = false;

        public bool DisableVoice { get; set; } = false;

        public bool DisableFavs { get; set; } = false;

        public bool DisableRadarImageMiniMap { get; set; } = false;

        public string AppMenuPos { get; set; } = "Top";

        public string LandMenuPos { get; set; } = "Top";

        public string FnMenuPos { get; set; } = "Top";

        public bool UseLLSD { get; set; } = false;

        public int ChatBufferLimit { get; set; } = 20;

        public int ScriptUrlBufferLimit { get; set; } = 5;
    }
}
