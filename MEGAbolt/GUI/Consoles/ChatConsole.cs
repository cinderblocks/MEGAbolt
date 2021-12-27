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
using System.Drawing;
using System.Windows.Forms;
using MEGAbolt.NetworkComm;
using OpenMetaverse;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;
using System.Drawing.Drawing2D;
using OpenMetaverse.Assets;
using System.Timers;
using System.Threading;
using System.Linq;
using OpenMetaverse.Voice;
using MEGAbolt.Controls;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;
using OpenJpegDotNet.IO;
using WeCantSpell.Hunspell;

namespace MEGAbolt
{
    public partial class ChatConsole : UserControl
    {

        private readonly MEGAboltInstance instance;
        private readonly MEGAboltNetcom netcom;
        private readonly GridClient client;
        private TabsConsole tabConsole;
        private int previousChannel = 0;
        private bool flying = false;
        private bool sayopen = false;
        private bool saveopen = false;
        private UUID _MapImageID;
        private Image _MapLayer;
        private int px = 0;
        private int py =0;
        public Simulator sim;
        private Rectangle rect;
        private bool move = false;
        private string selectedname = string.Empty;
        private bool avrezzed = false;
        private int newsize = 140;
        private System.Timers.Timer sitTimer;
        private bool showing = false;
        private UUID avuuid = UUID.Zero;
        private string avname = string.Empty;
        private SafeDictionary<uint, Avatar> sfavatar = new();
        private List<string> avtyping = new();
        private int start = 0;
        private int indexOfSearchText = 0;
        private string prevsearchtxt = string.Empty;
        private VoiceGateway vgate = null;

        private const int WM_KEYUP = 0x101;
        private const int WM_KEYDOWN = 0x100;

        private Popup tTip;
        private Popup tTip1;
        private CustomToolTip customToolTip;

        private WordList spellChecker = null;

        private ToolTip toolTip = new();
        private string tooltiptext = string.Empty;
        private Simulator CurrentSIM;
        private Vector3 lastPos = new(0, 0, 0);
        private TabPage tp1;
        private TabPage tp2;
        private TabPage tp3;
        private TabPage tp4;
        //private Form tpf;


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

        public ChatConsole(MEGAboltInstance instance)
        {
            try
            {
                InitializeComponent();
            }
            catch { ; }

            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;

            AddNetcomEvents();

            ChatManager = new ChatTextManager(instance, new RichTextBoxPrinter(instance, rtbChat));
            ChatManager.PrintStartupMessage();

            this.instance.MainForm.Load += MainForm_Load;

            ApplyConfig(this.instance.Config.CurrentConfig);
            this.instance.Config.ConfigApplied += Config_ConfigApplied;

            CreateSmileys();
            //AddLanguages();

            Disposed += ChatConsole_Disposed;

            lvwRadar.ListViewItemSorter = new RadarSorter();

            //sim = client.Network.CurrentSim;

            world.Cursor = Cursors.NoMove2D;
            //pictureBox1.Cursor = Cursors.Hand; 

            string msg1 = "Click for help on how to use/setup the Voice feature.";
            tTip = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            tTip.AutoClose = false;
            tTip.FocusOnOpen = false;
            tTip.ShowingAnimation = tTip.HidingAnimation = PopupAnimations.Blend;

            string msg2 = ">Hover mouse on avatar icon for info\n>Click on avatar icon for Profile\n>Left click on map and drag to zoom\n>Double click on map to open large map";
            tTip1 = new Popup(customToolTip = new CustomToolTip(instance, msg2));
            tTip1.AutoClose = false;
            tTip1.FocusOnOpen = false;
            tTip1.ShowingAnimation = tTip1.HidingAnimation = PopupAnimations.Blend;

            toolTip.AutoPopDelay = 7000;
            toolTip.InitialDelay = 450;
            toolTip.ReshowDelay = 450;
            toolTip.IsBalloon = false;
            //toolTip.ToolTipIcon = ToolTipIcon.Info;

            toolTip.OwnerDraw = true;
            toolTip.BackColor = Color.RoyalBlue;
            toolTip.ForeColor = Color.White;
            toolTip.Draw += toolTip_Draw;

            tp1 = tabPage1;
            tp2 = tabPage2;
            tp3 = tabPage3;
            tp4 = tabPage4;
        }

        private void toolTip_Draw(object sender, DrawToolTipEventArgs e)
        {
            e.DrawBackground();
            e.DrawBorder();
            e.DrawText();
        }

        private void Avatars_OnAvatarNames(object sender, UUIDNameReplyEventArgs names)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    Avatars_OnAvatarNames(sender, names);
                }));

                return;
            }

            lock (instance.avnames)
            {
                foreach (var av in names.Names.Where(
                             av => !instance.avnames.ContainsKey(av.Key)))
                {
                    instance.avnames.Add(av.Key, av.Value);
                }
            }
        }

        private void netcom_TeleportStatusChanged(object sender, TeleportEventArgs e)
        {
            try
            {
                //evs.OnTeleportStatusChanged(e); 

                switch (e.Status)
                {
                    case TeleportStatus.Start:
                        pTP.Visible = true;
                        label13.Text = "Teleporting";

                        break;
                    case TeleportStatus.Progress:
                        BeginInvoke(new MethodInvoker(delegate()
                        {
                            pTP.Visible = true;
                            label13.Text = e.Message;
                        }));


                        break;

                    case TeleportStatus.Failed:
                        BeginInvoke(new MethodInvoker(delegate()
                        {
                            label13.Text = "Teleport Failed";
                            pTP.Visible = true;

                            TPtimer.Enabled = true;
                            TPtimer.Start();  
                        }));

                        break;

                    case TeleportStatus.Finished:
                        if (instance.Config.CurrentConfig.AutoSit)
                        {
                            Logger.Log("AUTOSIT: Initializing...", Helpers.LogLevel.Info);

                            sitTimer = new System.Timers.Timer();
                            sitTimer.Interval = 61000;
                            sitTimer.Elapsed += OnTimedEvent;
                            sitTimer.Enabled = true;
                            sitTimer.Start();
                        }

                        BeginInvoke(new MethodInvoker(delegate()
                        {
                            label13.Text = "Teleport Succeeded";
                            pTP.Visible = true;

                            TPtimer.Enabled = true;
                            TPtimer.Start(); 
                        }));

                        break;
                }
            }
            catch (Exception ex)
            {
                instance.CrashReporter.Post(ex);
            }
        }

        void ChatConsole_Disposed(object sender, EventArgs e)
        {
            instance.Config.ConfigApplied -= Config_ConfigApplied;
            client.Objects.ObjectProperties -= Objects_OnObjectProperties;
            client.Appearance.AppearanceSet -= Appearance_OnAppearanceSet;
            client.Parcels.ParcelDwellReply -= Parcels_OnParcelDwell;
            client.Avatars.UUIDNameReply -= Avatars_OnAvatarNames;

            RemoveEvents();
            ChatManager = null;
        }

        private void Appearance_OnAppearanceSet(object sender, AppearanceSetEventArgs e)
        {
            string rmsg = string.Empty;

            if (avrezzed) return;

            rmsg = " Avatar has rezzed. ";
            avrezzed = true;

            try
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    ChatManager.PrintAlertMessage(rmsg);
                }));
            }
            catch { ; }

            if (instance.Config.CurrentConfig.AutoSit)
            {
                if (!instance.State.IsSitting)
                {
                    Logger.Log("AUTOSIT: Initializing...", Helpers.LogLevel.Info);

                    sitTimer = new System.Timers.Timer();
                    sitTimer.Interval = 61000;
                    sitTimer.Elapsed += OnTimedEvent;
                    sitTimer.Enabled = true;
                    sitTimer.Start();
                }
            }

            try
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    //CheckWearables();
                    //CheckLocation();
                }));
            }
            catch { ; }

            checkBox5.Enabled = instance.AllowVoice;

            label18.Text = checkBox5.Enabled 
                ? "Check 'Voice ON' box below. Then on 'Session start' unmute MIC to talk" 
                : "Voice is disabled on this parcel";

            client.Appearance.AppearanceSet -= Appearance_OnAppearanceSet;
        }

        private void CheckLocation()
        {
            // The land does not tie up with the location coords
            // not sure if this is an SL bug as at SIM V 1.40 (20/07/2010) or libopenmv bug
            // below is a work around and I believe it should remain
            // permanently as a safeguard

            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(CheckLocation));

                return;
            }

            try
            {
                Vector3 apos = new Vector3(Vector3.Zero);
                apos = instance.SIMsittingPos();

                float f1 = 64.0f * (apos.Y / 256.0f);
                float f2 = 64.0f * (apos.X / 256.0f);
                int posY = Convert.ToInt32(f1);
                int posX = Convert.ToInt32(f2);

                int parcelid = client.Network.CurrentSim.ParcelMap[posY, posX];

                if (parcelid == 0)
                {
                    client.Self.Teleport(client.Network.CurrentSim.Name, apos);
                    return;
                }

                if ((posX == 0 && posY == 0) || (posX == -1 && posY == -1) || (posX == -1 && posY == 0) || (posX == 0 && posY == -1))
                {
                    client.Self.GoHome();
                    return;
                }
            }
            catch { ; }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            CheckAutoSit();
        }

        private void Parcels_OnParcelDwell(object sender, ParcelDwellReplyEventArgs e)
        {
            if (instance.MainForm.parcel != null)
            {
                if (instance.MainForm.parcel.LocalID != e.LocalID) return;
            }

            BeginInvoke(new MethodInvoker(UpdateMedia));           
        }

        private void UpdateMedia()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(UpdateMedia));
                return;
            }

            try
            {
                Parcel parcel;
                Vector3 apos = new Vector3(Vector3.Zero); 
                apos = instance.SIMsittingPos();

                float f1 = 64.0f * (apos.Y / 256.0f);
                float f2 = 64.0f * (apos.X / 256.0f);
                int posY = Convert.ToInt32(f1);
                int posX = Convert.ToInt32(f2);

                int parcelid = client.Network.CurrentSim.ParcelMap[posY, posX];

                if (parcelid == 0)
                {
                    Logger.Log("Chat Console: land media could not be updated ", Helpers.LogLevel.Info); 
                    return;
                }

                if (!client.Network.CurrentSim.Parcels.TryGetValue(parcelid, out parcel))
                    return;

                ParcelMedia med = parcel.Media;
                instance.Config.CurrentConfig.mURL = @med.MediaURL;
            }
            catch (Exception ex)
            {
                Logger.Log("Chat Console Error updating Land Media: " + ex.Message, Helpers.LogLevel.Error);   
            }

            tsMovie.Enabled = !string.IsNullOrEmpty(instance.Config.CurrentConfig.mURL);
            tsMusic.Enabled = !string.IsNullOrEmpty(instance.Config.CurrentConfig.pURL);
        }

        private void CheckWearables()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(CheckWearables));
                
                //BeginInvoke(new MethodInvoker(() => CheckWearables()));
                return;
            }

            int loopbreaker = 0;

            try
            {
                client.Appearance.GetWearablesByType();
                var shapes = client.Appearance.GetWearableAssets(WearableType.Shape).ToArray();
                var skins = client.Appearance.GetWearableAssets(WearableType.Skin).ToArray();
                var hairs = client.Appearance.GetWearableAssets(WearableType.Hair).ToArray();

                var pants = client.Appearance.GetWearableAssets(WearableType.Pants).ToArray();
                var skirts = client.Appearance.GetWearableAssets(WearableType.Skirt).ToArray();

                var shoes = client.Appearance.GetWearableAssets(WearableType.Shoes).ToArray();

                var shirts = client.Appearance.GetWearableAssets(WearableType.Shirt).ToArray();
                var jackets = client.Appearance.GetWearableAssets(WearableType.Jacket).ToArray();
                var eyes = client.Appearance.GetWearableAssets(WearableType.Eyes).ToArray();
                var underpants = client.Appearance.GetWearableAssets(WearableType.Underpants).ToArray();
                var undershirts = client.Appearance.GetWearableAssets(WearableType.Undershirt).ToArray();
                var universals = client.Appearance.GetWearableAssets(WearableType.Universal).ToArray();

                if ( !shapes.Any() || !skins.Any() || !hairs.Any() || !shirts.Any()
                     || !pants.Any() || !skirts.Any() || !jackets.Any() 
                     || !eyes.Any() )
                {
                    string missing = string.Empty;

                    if (!shapes.Any())
                    {
                        missing = "shape, ";
                    }

                    if (!skins.Any())
                    {
                        missing += "skin, ";
                    }

                    if (!hairs.Any())
                    {
                        missing += "hair, ";
                    }

                    if (!eyes.Any())
                    {
                        missing += "eyes, ";
                    }

                    if (!shirts.Any() && !jackets.Any())
                    {
                        if (!undershirts.Any())
                        {
                            missing += "shirt/jacket & undershirt, ";
                        }
                    }

                    if (!universals.Any() && !skirts.Any() && !pants.Any())
                    {
                        if (!underpants.Any())
                        {
                            // The avatar is likely to be naked so let's try
                            // to get it dressed but we must avoid ending up in a loop
                            if (loopbreaker == 0)
                            {
                                client.Appearance.RequestSetAppearance(true);
                                loopbreaker += 1;
                                return;
                            }
                        }

                        missing += "pants/skirt & underpants, ";
                    }

                    if (!string.IsNullOrEmpty(missing))
                    {
                        if (missing.EndsWith(", ", StringComparison.CurrentCultureIgnoreCase))
                        {
                            missing = missing.Remove(missing.Length - 2);   
                        }

                        ChatManager.PrintAlertMessage($"Wearables missing: {missing}");
                    }
                }
            }
            catch (Exception ex)
            {
                //Logger.Log(ex.Message, Helpers.LogLevel.Error);
                instance.CrashReporter.Post(ex);
            }

            client.Appearance.AppearanceSet -= Appearance_OnAppearanceSet;
        }

        private void CheckAutoSit()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(CheckAutoSit));

                return;
            }

            if (!sitTimer.Enabled) return;

            instance.State.ResetCamera();   

            sitTimer.Stop();
            sitTimer.Enabled = false;
            sitTimer.Dispose();
            sitTimer.Elapsed -= OnTimedEvent;

            Logger.Log("AUTOSIT: Searching for sit object", Helpers.LogLevel.Info);

            Vector3 location = new Vector3(Vector3.Zero); 
            location = instance.SIMsittingPos();
            float radius = 21;

            // *** find all objects in radius ***
            List<Primitive> prims = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                delegate(Primitive prim)
                {
                    Vector3 pos = new Vector3(Vector3.Zero); 
                    pos = prim.Position;
                    return ((prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(location, pos) < radius));
                }
            );

            int i = 0;

            client.Objects.ObjectProperties += Objects_OnObjectProperties;

            foreach (Primitive prim in prims)
            {
                try
                {
                    if (prim.ParentID == 0) //root prims only
                    {
                        client.Objects.SelectObject(client.Network.CurrentSim, prim.LocalID, true);

                        i += 1;
                    }
                }
                catch (Exception ex)
                {
                    instance.CrashReporter.Post(ex);
                }
            }
        }

        private void Objects_OnObjectProperties(object sender, ObjectPropertiesEventArgs e)
        {
            if (e.Properties.Description.Trim() == client.Self.AgentID.ToString().Trim())
            {
                client.Objects.ObjectProperties -= Objects_OnObjectProperties;

                if (instance.Config.CurrentConfig.AutoSit)
                {
                    if (!instance.State.IsSitting)
                    {
                        instance.State.SetSitting(true, e.Properties.ObjectID);

                        Logger.Log("AUTOSIT: Found sit object and sitting", Helpers.LogLevel.Info);
                    }
                }
            }
        }

        private void AddLanguages()
        {
            // TODO: This should be converted into a language combobox component at
            // some stage

            //cboLanguage.Items.Clear();
            ////cboLanguage.Items.Add("Select...");
            //cboLanguage.Items.Add(new ComboEx.ICItem("Select...", -1));

            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Arabic en|ar", 1));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Chineese(simp) en|zh-CN", 2));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Chineese(trad) en|zh-TW", 3));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Croatian en|hr", 4));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Czech en|cs", 5));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Danish en|da", 6));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Dutch en|nl", 7));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Filipino en|tl", 9));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Finnish en|fi", 10));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/French en|fr", 11));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/German en|de", 12));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Greek en|el", 13));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Hebrew en|iw", 14));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Hindi en|hi", 15));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Hungarian en|hu", 16));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Indonesian en|id", 17));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Italian en|it", 18));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Japanese en|ja", 19));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Korean en|ko", 20));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Lithuanian en|lt", 21));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Norwegian en|no", 22));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Polish en|pl", 23));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Portuguese en|p", 24));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Romanian en|ro", 25));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Russian en|ru", 26));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Slovenian en|sl", 27));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Spanish en|es", 28));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Swedish en|sv", 29));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Thai en|th", 30));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Turkish en|tr", 31));
            //cboLanguage.Items.Add(new ComboEx.ICItem("English/Ukrainian en|uk", 32));

            //cboLanguage.Items.Add("Arabic/English ar|en");
            //cboLanguage.Items.Add("Chineese(simp)/English zh-CN|en");
            //cboLanguage.Items.Add("Chineese(trad)/English zh-TW|en");
            //cboLanguage.Items.Add("Croatian/English hr|en");
            //cboLanguage.Items.Add("Czech/English cs|en");
            //cboLanguage.Items.Add("Danish/English da|en");
            //cboLanguage.Items.Add("Dutch/English nl|en");
            //cboLanguage.Items.Add("Finnish/English fi|en");
            //cboLanguage.Items.Add("Filipino/English tl|en");
            //cboLanguage.Items.Add("French/English fr|en");
            //cboLanguage.Items.Add("German/English de|en");
            //cboLanguage.Items.Add("Greek/English el|en");
            //cboLanguage.Items.Add("Hebrew/English iw|en");
            //cboLanguage.Items.Add("Hindi/English hi|en");
            //cboLanguage.Items.Add("Hungarian/English hu|en");
            //cboLanguage.Items.Add("Indonesian/English id|en");
            //cboLanguage.Items.Add("Italian/English it|en");
            //cboLanguage.Items.Add("Japanese/English ja|en");
            //cboLanguage.Items.Add("Korean/English ko|en");
            //cboLanguage.Items.Add("Lithuanian/English lt|en");
            //cboLanguage.Items.Add("Norwegian/English no|en");
            //cboLanguage.Items.Add("Polish/English pl|en");
            //cboLanguage.Items.Add("Portuguese/English pt|en");
            //cboLanguage.Items.Add("Russian/English ru|en");
            //cboLanguage.Items.Add("Romanian/English ro|en");
            //cboLanguage.Items.Add("Slovenian/English sl|en");
            //cboLanguage.Items.Add("Spanish/English es|en");
            //cboLanguage.Items.Add("Swedish/English sv|en");
            //cboLanguage.Items.Add("Thai/English th|en");
            //cboLanguage.Items.Add("Turkish/English tr|en");
            //cboLanguage.Items.Add("Ukrainian/English uk|en");

            //cboLanguage.Items.Add("German/French de|fr");
            //cboLanguage.Items.Add("Spanish/French es|fr");
            //cboLanguage.Items.Add("French/German fr|de");
            //cboLanguage.Items.Add("French/Spanish fr|es");
            //cboLanguage.SelectedIndex = 0;
        }

        private void CreateSmileys()
        {
            // TODO: This should be converted into a smiley menu component at
            // some stage

            var _menuItem = new EmoticonMenuItem(Smileys.AngelSmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[0].Tag = (object)"angelsmile;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.AngrySmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[1].Tag = "angry;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.Beer);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[2].Tag = "beer;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.BrokenHeart);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[3].Tag = "brokenheart;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.bye);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[4].Tag = "bye";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.clap);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[5].Tag = "clap;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.ConfusedSmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[6].Tag = ":S";

            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.cool);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[7].Tag = "cool;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.CrySmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[8].Tag = "cry;";
            //_menuItem.Dispose();

            //_menuItem.BarBreak = true;

            _menuItem = new EmoticonMenuItem(Smileys.DevilSmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[9].Tag = "devil;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.duh);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[10].Tag = "duh;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.EmbarassedSmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[11].Tag = "embarassed;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.happy0037);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[12].Tag = ":)";

            _menuItem = new EmoticonMenuItem(Smileys.heart);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[13].Tag = "heart;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.kiss);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[14].Tag = "muah;";
            //_menuItem.Dispose();

            //_menuItem.BarBreak = true;

            _menuItem = new EmoticonMenuItem(Smileys.help);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[15].Tag = "help ";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.liar);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[16].Tag = "liar;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.lol);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[17].Tag = "lol";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.oops);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[18].Tag = "oops";

            _menuItem = new EmoticonMenuItem(Smileys.sad);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[19].Tag = ":(";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.shhh);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[20].Tag = "shhh";
            //_menuItem.Dispose();

            //_menuItem.BarBreak = true;

            _menuItem = new EmoticonMenuItem(Smileys.sigh);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[21].Tag = "sigh ";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.silenced);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[22].Tag = ":X";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.think);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[23].Tag = "thinking;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.ThumbsUp);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[24].Tag = "thumbsup;";

            _menuItem = new EmoticonMenuItem(Smileys.whistle);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[25].Tag = "whistle;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.wink);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[26].Tag = ";)";
            //_menuItem.Dispose();

            //_menuItem.BarBreak = true;

            _menuItem = new EmoticonMenuItem(Smileys.wow);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[27].Tag = "wow ";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.yawn);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[28].Tag = "yawn;";
            //_menuItem.Dispose();

            _menuItem = new EmoticonMenuItem(Smileys.zzzzz);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[29].Tag = "zzz";

            _menuItem.Dispose(); 
        }

        // When an emoticon is clicked, insert its image into RTF
        private void cmenu_Emoticons_Click(object _sender, EventArgs _args)
        {
            // Write the code here
            EmoticonMenuItem _item = (EmoticonMenuItem)_sender;

            cbxInput.Text += _item.Tag.ToString();
            cbxInput.Select(cbxInput.Text.Length, 0);
            //cbxInput.Focus();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            tabConsole = instance.TabConsole;
        }

        private void Config_ConfigApplied(object sender, ConfigAppliedEventArgs e)
        {
            BeginInvoke(new MethodInvoker(delegate()
                {
                    ApplyConfig(e.AppliedConfig);
                }));
        }

        private void ApplyConfig(Config config)
        {
            if (config.InterfaceStyle == 0) //System
            {
                toolStrip1.RenderMode = ToolStripRenderMode.System;
            }
            else if (config.InterfaceStyle == 1) //Office 2003
            {
                toolStrip1.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            }

            if (instance.Config.CurrentConfig.EnableSpelling)
            {
                var spellLang = instance.Config.CurrentConfig.SpellLanguage;

                var assembly = Assembly.GetExecutingAssembly();
                var names = assembly.GetManifestResourceNames();
                using var dictResourceStream = assembly.GetManifestResourceStream($"MEGAbolt.Spelling.{spellLang}.dic");
                using var affResourceStream = assembly.GetManifestResourceStream($"MEGAbolt.Spelling.{spellLang}.aff");
                if (dictResourceStream == null || affResourceStream == null)
                {
                    spellChecker = null;
                }
                else
                {
                    var csvFile = $"{DataFolder.GetDataFolder()}\\{spellLang}.csv";

                    if (!File.Exists(csvFile))
                    {
                        using StreamWriter sw = File.CreateText(csvFile);
                        sw.Dispose();
                    }

                    spellChecker = WordList.CreateFromStreams(dictResourceStream, affResourceStream);
                }
            }
            else
            {
                spellChecker = null;
            }

            if (instance.Config.CurrentConfig.DisableRadar)
            {
                toolStrip1.Visible = false;
                tabControl1.TabPages.Remove(tabPage1);
                tabControl1.TabPages.Remove(tabPage2);

                picCompass.Visible = false;
                label1.Visible = false;
                label2.Visible = false;
                label19.Visible = false;
                label20.Visible = false;

                client.Grid.CoarseLocationUpdate -= Grid_OnCoarseLocationUpdate;
            }
            else
            {
                if (!tabControl1.TabPages.Contains(tabPage1))
                {
                    toolStrip1.Visible = true;
                    tabControl1.TabPages.Remove(tabPage3);
                    tabControl1.TabPages.Remove(tabPage4);

                    tabPage1 = tp1;
                    tabControl1.TabPages.Add(tabPage1);
                    tabPage2 = tp2;
                    tabControl1.TabPages.Add(tabPage2);

                    picCompass.Visible = true;
                    label1.Visible = true;
                    label2.Visible = true;
                    label19.Visible = true;
                    label20.Visible = true;

                    client.Grid.CoarseLocationUpdate += Grid_OnCoarseLocationUpdate;
                }
            }

            if (instance.Config.CurrentConfig.DisableVoice)
            {
                tabControl1.TabPages.Remove(tabPage3);
            }
            else
            {
                if (!tabControl1.TabPages.Contains(tabPage3))
                {
                    tabControl1.TabPages.Remove(tabPage4);
                    tabPage3 = tp3;
                    tabControl1.TabPages.Add(tabPage3);
                }
            }

            if (instance.Config.CurrentConfig.DisableFavs)
            {
                tabControl1.TabPages.Remove(tabPage4);
            }
            else
            {
                if (!tabControl1.TabPages.Contains(tabPage4))
                {
                    tabPage4 = tp4;
                    tabControl1.TabPages.Add(tabPage4);
                }
            }

            if (instance.Config.CurrentConfig.DisableRadar && instance.Config.CurrentConfig.DisableFavs && instance.Config.CurrentConfig.DisableVoice)
            {
                try
                {
                    splitContainer1.SplitterDistance = splitContainer1.Width;   //513
                    panel5.Visible = false;
                    tabControl1.Visible = false;
                }
                catch (Exception)
                {
                    splitContainer1.SplitterDistance = splitContainer1.Width - splitContainer1.Panel2MinSize;
                }
            }
            else
            {
                splitContainer1.SplitterDistance = 513;
                panel5.Visible = true;
                tabControl1.Visible = true;
            }

            textBox1.Text = "Range: " + instance.Config.CurrentConfig.RadarRange.ToString(CultureInfo.CurrentCulture) + "m"; 
        }

        public void RemoveEvents()
        {
            netcom.ClientLoginStatus -= netcom_ClientLoginStatus;
            netcom.ClientLoggedOut -= netcom_ClientLoggedOut;
            netcom.ChatReceived -= netcom_ChatReceived;
            netcom.TeleportStatusChanged -= netcom_TeleportStatusChanged;

            client.Grid.CoarseLocationUpdate -= Grid_OnCoarseLocationUpdate;
            client.Network.SimChanged -= Network_OnCurrentSimChanged;
            client.Self.MeanCollision -= Self_Collision;
            client.Objects.TerseObjectUpdate -= Objects_OnObjectUpdated;
            client.Avatars.UUIDNameReply -= Avatars_OnAvatarNames;
        }

        private void AddNetcomEvents()
        {
            netcom.ClientLoginStatus += netcom_ClientLoginStatus;
            netcom.ClientLoggedOut += netcom_ClientLoggedOut;
        }

        private void AddClientEvents()
        {
            netcom.ChatReceived += netcom_ChatReceived;
            netcom.TeleportStatusChanged += netcom_TeleportStatusChanged;

            if (!instance.Config.CurrentConfig.DisableRadar)
            {
                client.Grid.CoarseLocationUpdate += Grid_OnCoarseLocationUpdate;
            }

            client.Network.SimChanged += Network_OnCurrentSimChanged;

            client.Self.MeanCollision += Self_Collision;
            client.Objects.TerseObjectUpdate += Objects_OnObjectUpdated;
            
            client.Appearance.AppearanceSet += Appearance_OnAppearanceSet;
            client.Parcels.ParcelDwellReply += Parcels_OnParcelDwell;

            client.Avatars.UUIDNameReply += Avatars_OnAvatarNames;
        }

        // Seperate thread
        private void Self_Collision(object sender, MeanCollisionEventArgs e)
        {
            // The av has collided with an object or avatar

            string cty = string.Empty;

            try
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    if (e.Type == MeanCollisionType.Bump)
                    {
                        cty = "Bumped in by: (" + e.Time.ToString(CultureInfo.CurrentCulture) + " - " + e.Magnitude.ToString(CultureInfo.CurrentCulture) + "): ";
                    }
                    else if (e.Type == MeanCollisionType.LLPushObject)
                    {
                        cty = "Pushed by: (" + e.Time.ToString(CultureInfo.CurrentCulture) + " - " + e.Magnitude.ToString(CultureInfo.CurrentCulture) + "): ";
                    }
                    else if (e.Type == MeanCollisionType.PhysicalObjectCollide)
                    {
                        cty = "Physical object collided (" + e.Time.ToString() + " - " + e.Magnitude.ToString(CultureInfo.CurrentCulture) + "): ";
                    }

                    ChatManager.PrintAlertMessage(cty + e.Aggressor.ToString());
                }));
            }
            catch
            {
               ;
            }
        }

        private void Objects_OnObjectUpdated(object sender, TerseObjectUpdateEventArgs e)
        {
            if (e.Simulator != client.Network.CurrentSim) return;
            if (!e.Update.Avatar) return;

            if (!netcom.IsLoggedIn) return;

            if (e.Prim.ID == client.Self.AgentID)
            {
                instance.State.ResetCamera();

                BeginInvoke(new MethodInvoker(delegate()
                {
                    try
                    {
                        if (!lvwRadar.Items.ContainsKey(client.Self.Name))
                        {
                            ListViewItem item = lvwRadar.Items.Add(client.Self.Name, client.Self.Name, string.Empty);
                            item.Font = new Font(item.Font, FontStyle.Bold);
                            item.Tag = client.Self.AgentID;
                            item.BackColor = Color.WhiteSmoke;
                            item.ForeColor = Color.Black;

                            item.SubItems.Add(string.Empty);
                            //item.SubItems.Add(string.Empty);
                        }
                    }
                    catch { ; }
                }));

                return;
            }
        }

        private delegate void OnAddSIMAvatar(string av, UUID key, Vector3 avpos, Color clr, string state);
        public void AddSIMAvatar(string av, UUID key, Vector3 avpos, Color clr, string state)
        {
            if (InvokeRequired)
            {

                BeginInvoke(new MethodInvoker(delegate()
                {
                    AddSIMAvatar(av, key, avpos, clr, state);
                }));

                return;
            }

            if (!string.IsNullOrEmpty(selectedname)) return;

            if (av == null) return;
            string name = av;

            if (string.IsNullOrEmpty(name)) return;

            lvwRadar.BeginUpdate();
            if (lvwRadar.Items.ContainsKey(name))
            {
                lvwRadar.Items.RemoveByKey(name);  
            }
            lvwRadar.EndUpdate();

            string sDist = string.Empty;

            try
            {
                Vector3 selfpos = new Vector3(Vector3.Zero); 
                selfpos = instance.SIMsittingPos();

                if (selfpos.Z < 0.1f)
                {
                    selfpos.Z = 1020f;
                }

                if (avpos.Z < 0.1f)
                {
                    avpos.Z = 1020f;
                }

                double dist = Math.Round(Vector3d.Distance(ConverToGLobal(selfpos), ConverToGLobal(avpos)), MidpointRounding.ToEven);

                string sym = string.Empty;

                sym = "(Alt.: " + avpos.Z.ToString("#0", CultureInfo.CurrentCulture) + "m)";

                if (clr == Color.RoyalBlue)
                {
                    if (avpos.Z == 1020f)
                    {
                        //sDist = "[???m] ";
                        sDist = ">[" + Convert.ToInt32(dist).ToString(CultureInfo.CurrentCulture) + "m] ";
                    }
                    else
                    {
                        sDist = "[" + Convert.ToInt32(dist).ToString(CultureInfo.CurrentCulture) + "m] ";
                    }
                }
                else
                {
                    sDist = "[" + Convert.ToInt32(dist).ToString(CultureInfo.CurrentCulture) + "m] ";
                }

                string rentry = " " + sym + state;

                lvwRadar.BeginUpdate();

                if (name != client.Self.Name)
                {
                    ListViewItem item = lvwRadar.Items.Add(name, sDist + name, string.Empty);
                    item.ForeColor = clr;
                    item.Tag = key;

                    //rentry = rentry.Replace("*", " (Sitting)");
                    rentry = instance.CleanReplace("*", " (Sitting)", rentry);

                    item.ToolTipText = sDist + name + rentry;

                    if (avtyping.Contains(name))
                    {
                        int index = lvwRadar.Items.IndexOfKey(name);
                        if (index != -1)
                        {
                            lvwRadar.Items[index].ForeColor = Color.Red;
                        }
                    }
                }

                string avsnem = client.Self.Name;

                if (!lvwRadar.Items.ContainsKey(avsnem))
                {
                    ListViewItem item = lvwRadar.Items.Add(avsnem, avsnem, string.Empty);
                    item.Font = new Font(item.Font, FontStyle.Bold);
                    item.Tag = client.Self.AgentID;
                    item.BackColor = Color.WhiteSmoke;
                    item.ForeColor = Color.Black;  
                    
                    item.SubItems.Add(string.Empty);
                }

                recolorListItems(lvwRadar);
                lvwRadar.EndUpdate();
            }
            catch (Exception ex)
            {
                Logger.Log("Radar update: " + ex.Message, Helpers.LogLevel.Warning);
            }
        }

        private static void recolorListItems(ListView lv)
        {
            for (int ix = 0; ix < lv.Items.Count; ++ix)
            {
                var item = lv.Items[ix];
                item.BackColor = (ix % 2 == 0) ? Color.WhiteSmoke : Color.White;
            }
        }

        private Vector3d ConverToGLobal(Vector3 pos)
        {
            uint regionX, regionY;
            Utils.LongToUInts(client.Network.CurrentSim.Handle, out regionX, out regionY);
            Vector3d objpos;

            objpos.X = (double)pos.X + (double)regionX;
            objpos.Y = (double)pos.Y + (double)regionY;
            objpos.Z = pos.Z;   // -2f;

            return objpos; 
        }

        private void GetCompass()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(GetCompass));

                return;
            }
            
            Quaternion avRot = client.Self.RelativeRotation;

            Matrix4 m = Matrix4.CreateFromQuaternion(avRot);

            Vector3 vDir = new Vector3(Vector3.Zero)
            {
                X = m.M11,
                Y = m.M21,
                Z = m.M31
            };

            int x = Convert.ToInt32(vDir.X);
            int y = Convert.ToInt32(vDir.Y);

            if ((Math.Abs(x) > Math.Abs(y)) && (x > 0))
            {
                //heading = "E";
                picCompass.Image = Properties.Resources.c_e;
            }
            else if ((Math.Abs(x) > Math.Abs(y)) && (x < 0))
            {
                //heading = "W";
                picCompass.Image = Properties.Resources.c_w;
            }
            else if ((Math.Abs(y) > Math.Abs(x)) && (y > 0))
            {
                //heading = "S";
                picCompass.Image = Properties.Resources.c_s;
            }
            else if ((Math.Abs(y) > Math.Abs(x)) && (y < 0))
            {
                //heading = "N";
                picCompass.Image = Properties.Resources.c_n;
            }
            else if ((Math.Abs(y) == Math.Abs(x)) && (x > 0 && y > 0))
            {
                //heading = "SE";
                picCompass.Image = Properties.Resources.c_se;
            }
            else if ((Math.Abs(y) == Math.Abs(x)) && (x < 0 && y > 0))
            {
                //heading = "SW";
                picCompass.Image = Properties.Resources.c_sw;
            }
            else if ((Math.Abs(y) == Math.Abs(x)) && (x < 0 && y < 0))
            {
                //heading = "NW";
                picCompass.Image = Properties.Resources.c_nw;
            }
            else if ((Math.Abs(y) == Math.Abs(x)) && (x > 0 && y < 0))
            {
                //heading = "NE";
                picCompass.Image = Properties.Resources.c_ne;
            }
        }

        public static Vector3 QuaternionToEuler(Quaternion q)
        {
            Vector3 v = new Vector3(Vector3.Zero); 
            v = Vector3.Zero;

            v.X = (float)Math.Atan2
            (
                2 * q.Y * q.W - 2 * q.X * q.Z,
                   1 - 2 * Math.Pow(q.Y, 2) - 2 * Math.Pow(q.Z, 2)
            );

            v.Z = (float)Math.Asin
            (
                2 * q.X * q.Y + 2 * q.Z * q.W
            );

            v.Y = (float)Math.Atan2
            (
                2 * q.X * q.W - 2 * q.Y * q.Z,
                1 - 2 * Math.Pow(q.X, 2) - 2 * Math.Pow(q.Z, 2)
            );

            if (q.X * q.Y + q.Z * q.W == 0.5)
            {
                v.X = (float)(2 * Math.Atan2(q.X, q.W));
                v.Y = 0;
            }

            else if (q.X * q.Y + q.Z * q.W == -0.5)
            {
                v.X = (float)(-2 * Math.Atan2(q.X, q.W));
                v.Y = 0;
            }

            return v;
        }

        private void netcom_ClientLoginStatus(object sender, LoginProgressEventArgs e)
        {
            if (e.Status != LoginStatus.Success) return;

            cbxInput.Enabled = true;
            sim = client.Network.CurrentSim;

            AddClientEvents();
            timer2.Enabled = true;
            timer2.Start();
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            cbxInput.Enabled = false;
            tbSay.Enabled = false;

            lvwRadar.Items.Clear();
        }

        public void PrintAvUUID()
        {
            ChatManager = new ChatTextManager(instance, new RichTextBoxPrinter(instance, rtbChat));
            ChatManager.PrintUUID();
        }

        private void netcom_ChatReceived(object sender, ChatEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    netcom_ChatReceived(sender, e);
                }));

                return;
            }

            if (instance.BlockChatIn) return;

            if (e.SourceType != ChatSourceType.Agent)
            {
                return;
            }

            if (e.FromName.ToLower(CultureInfo.CurrentCulture) == netcom.LoginOptions.FullName.ToLower(CultureInfo.CurrentCulture))
            {
                return;
            }

            if (instance.IsAvatarMuted(e.OwnerID, e.FromName))
                return;

            int index = lvwRadar.Items.IndexOfKey(e.FromName);
            if (index == -1) return;

            if (e.Type == ChatType.StartTyping)
            {
                lvwRadar.Items[index].ForeColor = Color.Red;

                if (!avtyping.Contains(e.FromName))
                {
                    avtyping.Add(e.FromName);
                }

                instance.State.LookAt(true, e.OwnerID);
            }
            else
            {
                lvwRadar.Items[index].ForeColor = Color.FromKnownColor(KnownColor.ControlText);

                if (avtyping.Contains(e.FromName))
                {
                    avtyping.Remove(e.FromName);
                }

                instance.State.LookAt(false, e.OwnerID);
            }
        }        

        public void ProcessChatInput(string input, ChatType type)
        {
            input = instance.CleanReplace("http://secondlife:///", "secondlife:///", input);
            input = instance.CleanReplace("http://secondlife://", "secondlife:///", input);
 
                if (instance.Config.CurrentConfig.EnableSpelling && spellChecker != null)
                {
                    // put preference check here
                    //string cword = Regex.Replace(cbxInput.Text, @"[^a-zA-Z0-9]", "");
                    //string[] swords = cword.Split(' ');
                    string[] swords = input.Split(' ');
                    bool hasmistake = false;
                    bool correct = true;

                    foreach (string word in swords)
                    {
                        string cword = Regex.Replace(word, @"[^a-zA-Z0-9]", "");

                        try
                        {
                            correct = spellChecker.Check(cword);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message == "Dictionary is not loaded")
                            {
                                instance.Config.ApplyCurrentConfig();
                                correct = spellChecker.Check(cword);
                            }
                            else
                            {
                                Logger.Log("Spellcheck error chat: " + ex.Message, Helpers.LogLevel.Error);
                            }
                        }

                        if (!correct)
                        {
                            hasmistake = true;
                        }
                    }

                    if (hasmistake)
                    {
                        (new frmSpelling(instance, cbxInput.Text, swords, type)).Show();
                        hasmistake = false;
                        return;
                    }
                }
            //}

            SendChat(input, type);
        }

        public void SendChat(string input, ChatType type)
        {
            if (String.IsNullOrEmpty(input)) return;

            string[] inputArgs = input.Split(' ');

            if (inputArgs[0].StartsWith("//", StringComparison.CurrentCultureIgnoreCase)) //Chat on previously used channel
            {
                string message = string.Join(" ", inputArgs).Substring(2);
                netcom.ChatOut(message, type, previousChannel);
            }
            else if (inputArgs[0].StartsWith("/", StringComparison.CurrentCultureIgnoreCase)) //Chat on specific channel
            {
                string message = string.Empty;
                string number = inputArgs[0].Substring(1);

                int channel = 0;
                bool pres = int.TryParse(number, out channel);

                if (!pres) channel = 0;

                if (channel < 0) channel = 0;

                message = input.StartsWith("/me ", StringComparison.CurrentCultureIgnoreCase)
                    ? input : string.Join(" ", inputArgs, 1, inputArgs.GetUpperBound(0));

                netcom.ChatOut(message, type, channel);

                previousChannel = channel;
            }
            else //Chat on channel 0 (public chat)
            {
                netcom.ChatOut(input, type, 0);
                //client.Self.Chat(input, 0, type);
            }

            ClearChatInput();
        }

        private static string GetTranslation(string sTrStr)
        {
            ////string sPair = GetLangPair(cboLanguage.Text);

            ////GoogleTranslationUtils.Translate trans = new GoogleTranslationUtils.Translate(sTrStr, sPair);

            ////return trans.Translation;

            //string sPair = GetLangPair(cboLanguage.Text);

            ////GoogleTranslationUtils.Translate trans = new GoogleTranslationUtils.Translate(sTrStr, sPair);
            ////return trans.Translation;

            ////string sPair

            //MB_Translation_Utils.Utils trans = new MB_Translation_Utils.Utils();

            //string tres = trans.Translate(sTrStr, sPair);

            //return tres;

            return string.Empty;  
        }

        private static string GetLangPair(string sPair)
        {
            string[] inputArgs = sPair.Split(' ');

            return inputArgs[1].ToString();
        }

        private void ClearChatInput()
        {
            cbxInput.Items.Add(cbxInput.Text);
            cbxInput.Text = string.Empty;
        }

        private void cbxInput_TextChanged(object sender, EventArgs e)
        {
            if (cbxInput.Text.Length > 0)
            {
                tbSay.Enabled = true;

                if (!cbxInput.Text.StartsWith("/", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!instance.State.IsTyping)
                        instance.State.SetTyping(true);
                }
            }
            else
            {
                tbSay.Enabled = false;
                instance.State.SetTyping(false);
            }
        }

        public ChatTextManager ChatManager { get; private set; }

        private void tbtnStartIM_Click(object sender, EventArgs e)
        {
            UUID av = (UUID)lvwRadar.SelectedItems[0].Tag;

            if (av == UUID.Zero) return;

            string name = instance.avnames[av];

            if (tabConsole.TabExists(name))
            {
                tabConsole.SelectTab(name);
                return;
            }

            tabConsole.AddIMTab(av, client.Self.AgentID ^ av, name);
            tabConsole.SelectTab(name);
        }

        private void tbtnFollow_Click(object sender, EventArgs e)
        {
            client.Self.AutoPilotCancel();

            UUID av = (UUID)lvwRadar.SelectedItems[0].Tag;

            if (av == UUID.Zero) return;

            string name = instance.avnames[av];

            Avatar sav = new Avatar();
            sav = CurrentSIM.ObjectsAvatars.Find(delegate(Avatar fa)
            {
                return fa.ID == av;
            }
            );

            if (sav == null)
            {
                MessageBox.Show("Avatar is out of range for this function.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (instance.State.FollowName != name)
            {
                //instance.State.GoTo(string.Empty, UUID.Zero);

                instance.State.Follow(string.Empty, UUID.Zero);
                instance.State.Follow(name, av);
                tbtnFollow.ToolTipText = "Stop Following";
            }
            else
            {
                instance.State.Follow(string.Empty, UUID.Zero);
                tbtnFollow.ToolTipText = "Follow";
            }
        }

        private void tbtnProfile_Click(object sender, EventArgs e)
        {
            //Avatar av = ((ListViewItem)lvwRadar.SelectedItems[0]).Tag as Avatar;
            //if (av == null) return;

            UUID av = (UUID)lvwRadar.SelectedItems[0].Tag;

            if (av == UUID.Zero) return;

            string name = instance.avnames[av];

            (new frmProfile(instance, name, av)).Show(this);
        }

        private void cbxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) e.SuppressKeyPress = true;

            if (e.Control && e.KeyCode == Keys.V)
            {
                //e.Handled = true;

                ClipboardAsync Clipboard2 = new ClipboardAsync();

                //string ptxt = Clipboard2.GetText(TextDataFormat.UnicodeText).Replace(Environment.NewLine, "\r\n");
                //cbxInput.Text += ptxt;
                ////Clipboard.SetText(" ");
                ////Clipboard.Clear();
                ////string fgt = Clipboard.GetText();
                ////Clipboard.SetText(ptxt);

                ////////pasted = true;
                ////string ptxt = Clipboard.GetText().Replace(Environment.NewLine, "\r\n");

                ////Clipboard.SetText(" ");
                ////string fgt = Clipboard.GetText();
                ////Clipboard.Clear();
                ////Clipboard.SetText(ptxt); 
                ////cbxInput.Text += ptxt;

                string insertText = Clipboard2.GetText(TextDataFormat.UnicodeText).Replace(Environment.NewLine, "\r\n");
                int selectionIndex = textBox1.SelectionStart;
                cbxInput.Text = cbxInput.Text.Insert(selectionIndex, insertText);
                cbxInput.SelectionStart = selectionIndex + insertText.Length;
            }
        }

        private void cbxInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;

            if (e.Control && e.Shift)
                ProcessChatInput(cbxInput.Text, ChatType.Whisper);
            else if (e.Control)
                ProcessChatInput(cbxInput.Text, ChatType.Shout);
            else
                ProcessChatInput(cbxInput.Text, ChatType.Normal);
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void tbtnAddFriend_Click(object sender, EventArgs e)
        {
            //Avatar av = ((ListViewItem)lvwRadar.SelectedItems[0]).Tag as Avatar;
            //if (av == null) return;

            UUID av = (UUID)lvwRadar.SelectedItems[0].Tag;

            if (av == UUID.Zero) return;

            string name = instance.avnames[av];

            Boolean fFound = true;

            client.Friends.FriendList.ForEach(delegate(FriendInfo friend)
            {
                if (friend.Name == name)
                {
                    fFound = false;
                }
            });

            if (fFound)
            {
                client.Friends.OfferFriendship(av);
            }
        }

        private void tbtnFreeze_Click(object sender, EventArgs e)
        {
            //Avatar av = ((ListViewItem)lvwRadar.SelectedItems[0]).Tag as Avatar;
            //if (av == null) return;

            UUID av = (UUID)lvwRadar.SelectedItems[0].Tag;

            client.Parcels.FreezeUser(av, true);
        }

        private void tbtnBan_Click(object sender, EventArgs e)
        {
            //Avatar av = ((ListViewItem)lvwRadar.SelectedItems[0]).Tag as Avatar;
            //if (av == null) return;

            UUID av = (UUID)lvwRadar.SelectedItems[0].Tag;

            client.Parcels.EjectUser(av, true);
        }

        private void tbtnEject_Click_1(object sender, EventArgs e)
        {
            //Avatar av = ((ListViewItem)lvwRadar.SelectedItems[0]).Tag as Avatar;
            //if (av == null) return;

            UUID av = (UUID)lvwRadar.SelectedItems[0].Tag;

            client.Parcels.EjectUser(av, false);
        }

        private void rtbChat_TextChanged(object sender, EventArgs e)
        {

        }

        private void rtbChat_TextChanged_1(object sender, EventArgs e)
        {
            ////int i = rtbChat.Lines.Length;

            ////if (i > 10)
            ////{
            ////    int lineno = i-10;
            ////    int chars = rtbChat.GetFirstCharIndexFromLine(lineno);
            ////    rtbChat.SelectionStart = 0;
            ////    rtbChat.SelectionLength = chars; // rtbChat.Text.IndexOf("\n", 0) + 1;
            ////    rtbChat.SelectedText = "*** " + lineno.ToString() + "lines purged\n";
            ////}
            ////else
            ////{
            ////    return;
            ////}

            ////int lncnt = Convert.ToInt32(rtbChat.Lines.LongLength);

            ////if (lncnt > this.instance.Config.CurrentConfig.lineMax)
            ////{
            ////    int numOfLines = 1;
            ////    var lines = rtbChat.Lines;
            ////    var newLines = lines.Skip(numOfLines);

            ////    rtbChat.Lines = newLines.ToArray();

            ////    chatManager.ReprintAllText();  
            ////}

            //int lncnt = Convert.ToInt32(rtbChat.Lines.LongLength);
            //int maxlines = this.instance.Config.CurrentConfig.lineMax;

            //if (lncnt > maxlines)
            //{
            //    int numOfLines = 1;
            //    var lines = rtbChat.Lines;
            //    var newLines = lines.Skip(numOfLines);

            //    rtbChat.Lines = newLines.ToArray();
            //}

            //bool focused = rtbChat.Focused;
            ////backup initial selection
            //int selection = rtbChat.SelectionStart;
            //int length = rtbChat.SelectionLength;
            ////allow autoscroll if selection is at end of text
            //bool autoscroll = (selection == rtbChat.Text.Length);

            //if (!autoscroll)
            //{
            //    //shift focus from RichTextBox to some other control
            //    if (focused) cbxInput.Focus();
            //    //hide selection
            //    SendMessage(rtbChat.Handle, EM_HIDESELECTION, 1, 0);
            //}
            //else
            //{
            //    SendMessage(rtbChat.Handle, EM_HIDESELECTION, 0, 0);
            //    //restore focus to RichTextBox
            //    if (focused) rtbChat.Focus();
            //}
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chkTranslate_CheckedChanged(object sender, EventArgs e)
        {
            //if (chkTranslate.Checked == true)
            //{
            //    MessageBox.Show("~ MEGAtranslate ~ \n \n You must now select a language pair \n from the dropdown box. \n \n Anything you say will be auto translated to that language.", "METAtranslate");
            //    cboLanguage.Enabled = true;
            //}
            //else
            //{
            //    cboLanguage.Enabled = false;
            //    cboLanguage.SelectedIndex = 0;
            //}
        }

        //private void checkBox2_CheckedChanged(object sender, EventArgs e)
        //{
        //    if (checkBox2.Checked == true)
        //    {
        //        (new frmTranslate(instance)).Show();
        //    }
        //    else
        //    {
        //        (new frmTranslate(instance)).Close();
        //    }
        //}

        private void button1_Click(object sender, EventArgs e)
        {
            //(new frmTranslate(instance)).Show();
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        public string _textBox
        {
            set => cbxInput.Text = value;
        }

        //public bool _Search
        //{
        //    set { panel7.Visible = value; }
        //}

        private void tbtnGoto_Click(object sender, EventArgs e)
        {
            client.Self.AutoPilotCancel();

            ////Avatar av = ((ListViewItem)lvwRadar.SelectedItems[0]).Tag as Avatar;
            ////if (av == null) return;

            UUID av = (UUID)lvwRadar.SelectedItems[0].Tag;

            if (av == UUID.Zero) return;

            //string name = instance.avnames[av];

            Avatar sav = new Avatar();
            sav = CurrentSIM.ObjectsAvatars.Find(fa => fa.ID == av);

            if (sav == null)
            {
                MessageBox.Show("Avatar is out of range for this function.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Vector3 pos = new Vector3(Vector3.Zero);
            pos = sav.Position;

            // Is the avatar sitting
            uint oID = sav.ParentID;

            if (oID != 0)
            {
                // the av is sitting
                Primitive prim = new Primitive();

                try
                {
                    client.Network.CurrentSim.ObjectsPrimitives.TryGetValue(oID, out prim);

                    if (prim == null)
                    {
                        // do nothing
                        client.Self.AutoPilotCancel();
                        Logger.Log("GoTo cancelled. Could find the object the target avatar is sitting on.", Helpers.LogLevel.Warning);
                        return;
                    }
                    else
                    {
                        pos += prim.Position;
                    }
                }
                catch
                {
                    ;
                    //reporter.Show(ex);
                }
            }

            ulong regionHandle = client.Network.CurrentSim.Handle;

            ulong followRegionX = regionHandle >> 32;
            ulong followRegionY = regionHandle & (ulong)0xFFFFFFFF;

            ulong x = (ulong)pos.X + followRegionX;
            ulong y = (ulong)pos.Y + followRegionY;
            float z = pos.Z - 1f;

            //if (instance.State.GoName != name)
            //{
            //    instance.State.Follow(string.Empty, UUID.Zero);

            //    instance.State.GoTo(string.Empty, UUID.Zero);
            //    instance.State.GoTo(name, av);
            //    //tbtnGoto.ToolTipText = "Stop Go to";
            //}
            //else
            //{
            //    instance.State.GoTo(string.Empty, UUID.Zero);
            //    //tbtnFollow.ToolTipText = "Go to";
            //}

            client.Self.AutoPilot(x, y, z);
        }

        private void tbtnTurn_Click(object sender, EventArgs e)
        {
            //Avatar av = ((ListViewItem)lvwRadar.SelectedItems[0]).Tag as Avatar;
            //if (av == null) return;

            UUID av = (UUID)lvwRadar.SelectedItems[0].Tag;

            if (av == UUID.Zero) return;

            //string name = instance.avnames[av];

            Avatar sav = new Avatar();
            sav = CurrentSIM.ObjectsAvatars.Find(delegate(Avatar fa)
             {
                 return fa.ID == av;
             }
             );

            if (sav != null)
            {
                client.Self.AnimationStart(Animations.TURNLEFT, false);

                Vector3 pos = new Vector3(Vector3.Zero); 
                pos = sav.Position;

                client.Self.Movement.TurnToward(pos);

                client.Self.Movement.FinishAnim = true;
                Thread.Sleep(200);
                client.Self.AnimationStop(Animations.TURNLEFT, false);
            }
            else
            {
                MessageBox.Show("Avatar is out of range for this function.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void rtbChat_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (e.LinkText.StartsWith("http://slurl.", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    // Open up the TP form here
                    string encoded = HttpUtility.UrlDecode(e.LinkText);
                    string[] split = encoded.Split(new Char[] { '/' });
                    //string[] split = e.LinkText.Split(new Char[] { '/' });
                    string simr = split[4].ToString();
                    double x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                    (new frmTeleport(instance, simr, (float)x, (float)y, (float)z, false)).Show();
                }
                catch { ; }

            }
            else if (e.LinkText.StartsWith("http://maps.secondlife", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    // Open up the TP form here
                    string encoded = HttpUtility.UrlDecode(e.LinkText);
                    string[] split = encoded.Split(new Char[] { '/' });
                    //string[] split = e.LinkText.Split(new Char[] { '/' });
                    string simr = split[4].ToString();
                    double x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                    (new frmTeleport(instance, simr, (float)x, (float)y, (float)z, true)).Show();
                }
                catch { ; }

            }
            else if (e.LinkText.Contains("http://mbprofile:"))
            {
                try
                {
                    string encoded = HttpUtility.UrlDecode(e.LinkText);
                    string[] split = encoded.Split(new Char[] { '/' });
                    //string[] split = e.LinkText.Split(new Char[] { '#' });
                    string aavname = split[0].ToString();
                    string[] avnamesplit = aavname.Split(new Char[] { '#' });
                    aavname = avnamesplit[0].ToString();

                    split = e.LinkText.Split(new Char[] { ':' });
                    string elink = split[2].ToString(CultureInfo.CurrentCulture);
                    split = elink.Split(new Char[] { '&' });

                    UUID avid = (UUID)split[0].ToString();

                    (new frmProfile(instance, aavname, avid)).Show();
                }
                catch { ; }
            }
            else if (e.LinkText.Contains("http://secondlife:///"))
            {
                // Open up the Group Info form here
                string encoded = HttpUtility.UrlDecode(e.LinkText);
                string[] split = encoded.Split(new Char[] { '/' });
                UUID uuid = UUID.Zero;

                try
                {
                    uuid = (UUID)split[7].ToString();
                }
                catch
                {
                    uuid = UUID.Zero;
                }

                if (uuid != UUID.Zero && split[6].ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture) == "group")
                {
                    frmGroupInfo frm = new frmGroupInfo(uuid, instance);
                    frm.Show();
                }
                else if (uuid != UUID.Zero && split[6].ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture) == "agent")
                {
                    (new frmProfile(instance, string.Empty, uuid)).Show();
                }
            }
            else if (e.LinkText.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase) || e.LinkText.StartsWith("ftp://", StringComparison.CurrentCultureIgnoreCase) || e.LinkText.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase))
            {
                Utilities.OpenBrowser(e.LinkText);
            }
            else
            {
                Utilities.OpenBrowser("http://" + e.LinkText);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            lft();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            rgt();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //if ((keyData == (Keys.Control | Keys.C)))
            //{
            //    //your implementation
            //    return true;
            //}

            if ((keyData == (Keys.Control | Keys.V)))
            {
                ClipboardAsync Clipboard2 = new ClipboardAsync();

                string insertText = Clipboard2.GetText(TextDataFormat.UnicodeText).Replace(Environment.NewLine, "\r\n");
                int selectionIndex = cbxInput.SelectionStart;
                cbxInput.Text = cbxInput.Text.Insert(selectionIndex, insertText);
                cbxInput.SelectionStart = selectionIndex + insertText.Length;

                return true;
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        protected override bool ProcessKeyPreview(ref Message m)
        {
            int key = m.WParam.ToInt32();
            const int WM_SYSKEYDOWN = 0x104;

            // Fix submitted by METAforums user Spirit 25/09/2009
            // TO DO: This should be a setting in preferences so that
            // it works both ways...
            if (cbxInput.Focused)
            {
                return false;
            }

            switch (key)
            {
                case 33: // <--- page up.
                    if ((m.Msg == WM_KEYDOWN) || (m.Msg == WM_SYSKEYDOWN))
                    {
                        up(true);
                    }
                    else
                    {
                        up(false);
                    }
                    break;
                case 34: // <--- page down.
                    if ((m.Msg == WM_KEYDOWN) || (m.Msg == WM_SYSKEYDOWN))
                    {
                        dwn(true);
                    }
                    else
                    {
                        dwn(false);
                    }
                    break;
                case 37: // <--- left arrow.
                    lft();
                    break;
                case 38: // <--- up arrow.
                    if ((m.Msg == WM_KEYDOWN) || (m.Msg == WM_SYSKEYDOWN))
                    {
                        fwd(true);
                    }
                    else
                    {
                        fwd(false);
                    }
                    break;
                case 39: // <--- right arrow.
                    rgt();
                    break;
                case 40: // <--- down arrow.
                    if ((m.Msg == WM_KEYDOWN) || (m.Msg == WM_SYSKEYDOWN))
                    {
                        bck(true);
                    }
                    else
                    {
                        bck(false);
                    }
                    break;
                case 114: // <--- F3 Key
                    if (!checkBox5.Checked) return false; 
  
                    if (m.Msg == WM_KEYDOWN)
                    {
                        if (!checkBox3.Checked) break;

                        vgate.MicMute = checkBox3.Checked = false;
                    }
                    else
                    {
                        vgate.MicMute = checkBox3.Checked = true;
                    }

                    break;
            }

            return false;
        }

        private void up(bool goup)
        {
            if (goup)
            {
                client.Self.Movement.AutoResetControls = false;
                client.Self.Movement.UpPos = true;
                client.Self.Movement.SendUpdate();
            }
            else
            {
                client.Self.Movement.UpPos = false;
                client.Self.Movement.SendUpdate();
                client.Self.Movement.AutoResetControls = true;
            }
        }

        private void dwn(bool godown)
        {
            if (godown)
            {
                client.Self.Movement.AutoResetControls = false;
                client.Self.Movement.UpNeg = true;
                client.Self.Movement.SendUpdate();
            }
            else
            {
                client.Self.Movement.UpNeg = false;
                client.Self.Movement.SendUpdate();
                client.Self.Movement.AutoResetControls = true;
            }
        }

        private void fwd(bool goforward)
        {
            if (goforward)
            {
                client.Self.Movement.AutoResetControls = false;
                client.Self.Movement.AtPos = true;
                client.Self.Movement.SendUpdate();
            }
            else
            {
                client.Self.Movement.AtPos = false;
                client.Self.Movement.SendUpdate();
                client.Self.Movement.AutoResetControls = true;
            }
        }

        private void bck(bool goback)
        {
            if (goback)
            {
                client.Self.Movement.AutoResetControls = false;
                client.Self.Movement.AtNeg = true;
                client.Self.Movement.SendUpdate();
            }
            else
            {
                client.Self.Movement.AtNeg = false;
                client.Self.Movement.SendUpdate();
                client.Self.Movement.AutoResetControls = true;
            }
        }

        private void lft()
        {
            client.Self.Movement.TurnRight = false;
            client.Self.Movement.TurnLeft = true;
            client.Self.Movement.BodyRotation = client.Self.Movement.BodyRotation * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 45f);
            client.Self.Movement.SendUpdate(true);
            Thread.Sleep(500);
            client.Self.Movement.TurnLeft = false;
            client.Self.Movement.SendUpdate(true);
        }

        private void rgt()
        {
            client.Self.Movement.TurnLeft = false;
            client.Self.Movement.TurnRight = true;
            client.Self.Movement.BodyRotation = client.Self.Movement.BodyRotation * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -45f);
            client.Self.Movement.SendUpdate(true);
            Thread.Sleep(500);
            client.Self.Movement.TurnRight = false;
            client.Self.Movement.SendUpdate(true);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            flying = !instance.State.IsFlying;

            instance.State.SetFlying(flying);
        }

        private void shoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessChatInput(cbxInput.Text, ChatType.Shout);
        }

        private void whisperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessChatInput(cbxInput.Text, ChatType.Whisper);
        }

        private void clearChatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbChat.Clear();
            ChatManager.ClearInternalBuffer();
        }

        private void sayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessChatInput(cbxInput.Text, ChatType.Normal);
        }

        private void SaveChat()
        {
            // Create a SaveFileDialog to request a path and file name to save to.
            SaveFileDialog saveFile1 = new SaveFileDialog();

            string logdir = DataFolder.GetDataFolder();
            logdir += "\\Logs\\";

            saveFile1.InitialDirectory = logdir;

            // Initialize the SaveFileDialog to specify the RTF extension for the file.
            saveFile1.DefaultExt = "*.rtf";
            saveFile1.Filter = "txt files (*.txt)|*.txt|RTF Files (*.rtf)|*.rtf";  //"RTF Files|*.rtf";
            saveFile1.Title = "Save chat contents to hard disk...";

            // Determine if the user selected a file name from the saveFileDialog.
            if (saveFile1.ShowDialog() == DialogResult.OK &&
               saveFile1.FileName.Length > 0)
            {
                if (saveFile1.FileName.Substring(saveFile1.FileName.Length - 3) == "rtf")
                {
                    // Save the contents of the RichTextBox into the file.
                    rtbChat.SaveFile(saveFile1.FileName, RichTextBoxStreamType.RichText);
                }
                else
                {
                    rtbChat.SaveFile(saveFile1.FileName, RichTextBoxStreamType.PlainText);
                }
            }

            saveFile1.Dispose(); 
        }

        private void saveChatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveChat();
        }

        private void tsMovie_Click(object sender, EventArgs e)
        {
            Utilities.OpenBrowser(@instance.Config.CurrentConfig.mURL);
        }

        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {
            frmPlayer prForm = new frmPlayer(instance);

            prForm.FormClosed += PrForm_FormClosed;
            tsMusic.Enabled = false;
            prForm.Show();
        }

        private void PrForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            tsMusic.Enabled = true;
        }

        #region Minimap
        // Seperate thread
        private void Grid_OnCoarseLocationUpdate(object sender, CoarseLocationUpdateEventArgs e)
        {
            if (e.Simulator != client.Network.CurrentSim) return;

            CurrentSIM = e.Simulator;

            if (InvokeRequired)
            {

                BeginInvoke(new MethodInvoker(delegate()
                {
                    try
                    {
                        Grid_OnCoarseLocationUpdate(sender, e);
                    }
                    catch { ; }
                }));
                
                return;
            }

            List<UUID> tremove = new List<UUID>();
            tremove = e.RemovedEntries;

            foreach (UUID id in tremove)
            {
                foreach (ListViewItem litem in lvwRadar.Items)
                {
                    if (litem.Tag.ToString() == id.ToString())
                    {
                        lvwRadar.BeginUpdate();
                        lvwRadar.Items.RemoveAt(lvwRadar.Items.IndexOf(litem));
                        lvwRadar.EndUpdate();
                    }
                }

                lock (instance.avnames)
                {
                    instance.avnames.Remove(id);
                }

                if (instance.State.IsFollowing)
                {
                    if (id == instance.State.FollowID)
                    {
                        instance.State.Follow(string.Empty, UUID.Zero);
                        tbtnFollow.ToolTipText = "Follow";
                    }
                }
            }

            e.Simulator.AvatarPositions.ForEach(delegate(KeyValuePair<UUID, Vector3> favpos)
                    {
                        if (!instance.avnames.ContainsKey(favpos.Key))
                        {
                            client.Avatars.RequestAvatarName(favpos.Key);
                        }
                    });

            try
            {
                BeginInvoke((MethodInvoker)delegate { UpdateMiniMap(e.Simulator); });
                BeginInvoke((MethodInvoker)GetCompass);
            }
            catch { ; } 
        }

        // Seperate thread
        void Assets_OnImageReceived(TextureRequestState image, AssetTexture texture)
        {
            if (texture.AssetID != _MapImageID) return;

            using (Reader reader = new(texture.AssetData))
            {
                reader.ReadHeader();
                _MapLayer = reader.Decode().ToBitmap();
            }

            BeginInvoke((MethodInvoker)delegate { UpdateMiniMap(sim); });
        }

        // Seperate thread
        void Network_OnCurrentSimChanged(object sender, SimChangedEventArgs e)
        {
            lock (sfavatar)
            {
                sfavatar.Clear();
            }

            BeginInvoke(new MethodInvoker(delegate()
            {
                lvwRadar.Items.Clear();
            }));

            _MapLayer = null;

            //GetMap();
            BeginInvoke((MethodInvoker)GetMap);
        }

        private void GetMap()
        {
            if (instance.Config.CurrentConfig.DisableRadar) return;

            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(GetMap));
                return;
            }

            GridRegion region;

            label11.Text = "Map downloading...";

            if (_MapLayer == null || sim != client.Network.CurrentSim)
            {
                world.Image = null; 
                sim = client.Network.CurrentSim;
                label8.Text = client.Network.CurrentSim.Name;

                if (client.Grid.GetGridRegion(client.Network.CurrentSim.Name, GridLayerType.Objects, out region))
                {
                    if (region.MapImageID != UUID.Zero)
                    {
                        _MapImageID = region.MapImageID;
                        client.Assets.RequestImage(_MapImageID, ImageType.Baked, Assets_OnImageReceived);
                    }
                    else
                    {
                        if (client.Grid.GetGridRegion(client.Network.CurrentSim.Name, GridLayerType.Terrain, out region))
                        {
                            if (region.MapImageID != UUID.Zero)
                            {
                                _MapImageID = region.MapImageID;
                                client.Assets.RequestImage(_MapImageID, ImageType.Baked, Assets_OnImageReceived);
                            }
                            else
                            {
                                label11.Text = "Map unavailable"; 
                            }
                        }
                    }
                }
            }
            else
            {
                //UpdateMiniMap(sim);
                BeginInvoke(new OnUpdateMiniMap(UpdateMiniMap), new object[] { sim });
            }
        }

        private delegate void OnUpdateMiniMap(Simulator ssim);
        private void UpdateMiniMap(Simulator ssim)
        {
            if (InvokeRequired) BeginInvoke((MethodInvoker)delegate { UpdateMiniMap(ssim); });
            else
            {
                try
                {
                    if (instance.Config.CurrentConfig.DisableRadar) return;

                    if (ssim != client.Network.CurrentSim) return;
                    
                    Bitmap bmp = _MapLayer == null ? new Bitmap(256, 256) : (Bitmap)_MapLayer.Clone();
                    Graphics g = Graphics.FromImage(bmp);

                    if (_MapLayer == null)
                    {
                        g.Clear(BackColor);
                        g.FillRectangle(Brushes.White, 0f, 0f, 256f, 256f);
                        label11.Visible = true;
                    }
                    else
                    {
                        label11.Visible = false;
                    }

                    try
                    {
                        label4.Text = "Ttl objects: " + ssim.Stats.Objects.ToString(CultureInfo.CurrentCulture);
                        label5.Text = "Scripted objects: " + ssim.Stats.ScriptedObjects.ToString(CultureInfo.CurrentCulture);
                        label8.Text = client.Network.CurrentSim.Name;

                        Simulator csim = client.Network.CurrentSim;

                        label9.Text = "FPS: " + csim.Stats.FPS.ToString(CultureInfo.CurrentCulture);

                        // Maximum value changes for OSDGrid compatibility V 0.9.32.0

                        if (csim.Stats.FPS > progressBar7.Maximum)
                        {
                            progressBar7.Maximum = csim.Stats.FPS;
                        }

                        progressBar7.Value = csim.Stats.FPS;

                        label15.Text = "Dilation: " + csim.Stats.Dilation.ToString("0.00", CultureInfo.CurrentCulture);

                        if ((int)(csim.Stats.Dilation * 10) > progressBar1.Maximum)
                        {
                            progressBar1.Maximum = (int)(csim.Stats.Dilation * 10);
                        }

                        progressBar1.Value = (int)(csim.Stats.Dilation * 10);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Chatconsole MiniMap - Stats: " + ex.Message, Helpers.LogLevel.Error);
                    }

                    // V0.9.8.0 changes for OpenSIM compatibility
                    Vector3 myPos = new Vector3(0,0,0);
                    string strInfo = string.Empty;

                    myPos = instance.SIMsittingPos();

                    try
                    {
                        string[] svers = ssim.SimVersion.Split(' ');
                        var e = from s in svers
                                select s;

                        int cnt = e.Count() - 1;

                        try
                        {
                            label3.Text = svers[0] + " " + svers[1] + " " + svers[2] + " " + svers[3];
                            label12.Text = svers[cnt];
                        }
                        catch
                        {
                            ;
                        }
                    }
                    catch
                    {
                        label12.Text = "na";
                    }

                    strInfo = string.Format(CultureInfo.CurrentCulture, "Ttl Avatars: {0}", ssim.AvatarPositions.Count);
                    label6.Text = strInfo;

                    int i = 0;

                    instance.avlocations.Clear();

                    if (myPos.Z < 0.1f)
                    {
                        myPos.Z = 1020f;
                    }

                    // Draw self position
                    Rectangle myrect;

                    if (!instance.Config.CurrentConfig.DisableRadarImageMiniMap)
                    {
                        int rg = instance.Config.CurrentConfig.RadarRange;

                        if (rg < 150)
                        {
                            rg *= 2;

                            myrect = new Rectangle(((int)Math.Round(myPos.X, 0)) - (rg / 2), (255 - ((int)Math.Round(myPos.Y, 0))) - (rg / 2 - 4), rg + 2, rg + 2);
                            SolidBrush semiTransBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 255));
                            g.CompositingQuality = CompositingQuality.GammaCorrected;
                            g.FillEllipse(semiTransBrush, myrect);

                            myrect = new Rectangle(((int)Math.Round(myPos.X, 0)) - (rg / 4), (255 - ((int)Math.Round(myPos.Y, 0))) - (rg / 4 - 4), rg / 2 + 2, rg / 2 + 2);
                            //semiTransBrush = new SolidBrush(Color.FromArgb(128, 0, 245, 225));
                            g.DrawEllipse(new Pen(Color.Blue, 1), myrect);

                            semiTransBrush.Dispose();
                        }
                    }

                    ssim.AvatarPositions.ForEach(
                    delegate(KeyValuePair<UUID, Vector3> pos)
                    {
                        if (pos.Key != client.Self.AgentID)
                        {
                            bool restrict = false;

                            if (!instance.avnames.ContainsKey(pos.Key))
                            {
                                client.Avatars.RequestAvatarName(pos.Key);
                            }

                            Vector3 oavPos = new Vector3(0, 0, 0)
                            {
                                X = pos.Value.X,
                                Y = pos.Value.Y,
                                Z = pos.Value.Z
                            };

                            if (oavPos.Z < 0.1f)
                            {
                                oavPos.Z = 1020f;
                            }

                            Avatar fav = new Avatar();
                            fav = ssim.ObjectsAvatars.Find((Avatar av) => av.ID == pos.Key);

                            string st = string.Empty;

                            if (fav != null)
                            {
                                oavPos = fav.Position;
                                uint sobj = fav.ParentID;

                                if (sobj != 0)
                                {
                                    st = "*";

                                    Primitive prim;
                                    client.Network.CurrentSim.ObjectsPrimitives.TryGetValue(sobj, out prim);

                                    if (prim != null)
                                    {
                                        oavPos = prim.Position + oavPos;
                                    }
                                }
                            }

                            if (instance.Config.CurrentConfig.RestrictRadar)
                            {
                                double dist = Math.Round(Vector3d.Distance(ConverToGLobal(myPos), ConverToGLobal(oavPos)), MidpointRounding.ToEven);

                                if (instance.Config.CurrentConfig.RadarRange < Convert.ToInt32(dist))
                                {
                                    restrict = true;

                                    if (instance.avnames.ContainsKey(pos.Key))
                                    {
                                        string name = instance.avnames[pos.Key];

                                        lvwRadar.BeginUpdate();
                                        if (lvwRadar.Items.ContainsKey(name))
                                        {
                                            lvwRadar.Items.RemoveByKey(name);
                                        }
                                        lvwRadar.EndUpdate();
                                    }
                                }
                            }

                            if (!restrict)
                            {
                                int x = (int)oavPos.X - 2;
                                int y = 255 - (int)oavPos.Y - 2;

                                rect = new Rectangle(x, y, 6, 6);

                                if (myPos.Z - oavPos.Z > 20)
                                {
                                    g.FillRectangle(Brushes.DarkRed, rect);
                                    g.DrawRectangle(new Pen(Brushes.Red, 1), rect);
                                }
                                else if (myPos.Z - oavPos.Z > -11 && myPos.Z - oavPos.Z < 11)
                                {
                                    g.FillEllipse(Brushes.LightGreen, rect);
                                    g.DrawEllipse(new Pen(Brushes.Green, 1), rect);
                                }
                                else
                                {
                                    g.FillRectangle(Brushes.MediumBlue, rect);
                                    g.DrawRectangle(new Pen(Brushes.Red, 1), rect);
                                }

                                Point mouse = new Point(x, y);

                                instance.avlocations.Add(new MEGAboltInstance.AvLocation(mouse, rect.Size, pos.Key.ToString(), string.Empty, oavPos));

                                try
                                {
                                    Color aclr = Color.Black;                                    

                                    if (fav == null)
                                    {
                                        aclr = Color.RoyalBlue;
                                    }
                                    else
                                    {
                                        if (!instance.avtags.ContainsKey(fav.ID))
                                        {
                                            instance.avtags.Add(fav.ID, fav.GroupName);
                                        }
                                    }

                                    if (instance.avnames.ContainsKey(pos.Key))
                                    {
                                        string name = instance.avnames[pos.Key];
                                        BeginInvoke(new OnAddSIMAvatar(AddSIMAvatar), new object[] { name, pos.Key, oavPos, aclr, st });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log("UpdateMiniMap: " + ex.Message, Helpers.LogLevel.Warning);
                                }
                            }
                        }

                        i++;
                    }
                    );

                    g.DrawImage(bmp, 0, 0);

                    myrect = new Rectangle((int)Math.Round(myPos.X, 0) - 2, 255 - ((int)Math.Round(myPos.Y, 0) - 2), 6, 6);
                    g.FillEllipse(new SolidBrush(Color.Yellow), myrect);
                    g.DrawEllipse(new Pen(Brushes.Red, 2), myrect);

                    // Draw compass points
                    StringFormat strFormat = new StringFormat();
                    strFormat.Alignment = StringAlignment.Center;

                    g.DrawString("N", new Font("Arial", 13), Brushes.Black, new RectangleF(0, 2, bmp.Width, bmp.Height), strFormat);
                    g.DrawString("N", new Font("Arial", 10, FontStyle.Bold), Brushes.White, new RectangleF(0, 2, bmp.Width, bmp.Height), strFormat);

                    strFormat.LineAlignment = StringAlignment.Center;
                    strFormat.Alignment = StringAlignment.Near;

                    g.DrawString("W", new Font("Arial", 13), Brushes.Black, new RectangleF(0, 0, bmp.Width, bmp.Height), strFormat);
                    g.DrawString("W", new Font("Arial", 10, FontStyle.Bold), Brushes.White, new RectangleF(2, 0, bmp.Width, bmp.Height), strFormat);

                    strFormat.LineAlignment = StringAlignment.Center;
                    strFormat.Alignment = StringAlignment.Far;

                    g.DrawString("E", new Font("Arial", 13), Brushes.Black, new RectangleF(0, 0, bmp.Width, bmp.Height), strFormat);
                    g.DrawString("E", new Font("Arial", 10, FontStyle.Bold), Brushes.White, new RectangleF(-2, 0, bmp.Width, bmp.Height), strFormat);

                    strFormat.LineAlignment = StringAlignment.Far;
                    strFormat.Alignment = StringAlignment.Center;

                    g.DrawString("S", new Font("Arial", 13), Brushes.Black, new RectangleF(0, 0, bmp.Width, bmp.Height), strFormat);
                    g.DrawString("S", new Font("Arial", 10, FontStyle.Bold), Brushes.White, new RectangleF(0, 0, bmp.Width, bmp.Height), strFormat);

                    world.Image = bmp;
                    //world.Cursor = Cursors.NoMove2D;

                    strFormat.Dispose(); 
                    g.Dispose();

                    if (lastPos != myPos)
                    {
                        lastPos = myPos; 
                        GetCompass();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Chatconsole MiniMap: " + ex.Message, Helpers.LogLevel.Error);
                    //return;
                }
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage1)
            {
                if (!instance.LoggedIn) return;

                toolStrip1.Visible = true;

                label6.Text = string.Empty;
                label4.Text = string.Empty;
                label5.Text = string.Empty;
                label8.Text = string.Empty;
                label9.Text = string.Empty;
                label15.Text = string.Empty;
            }
            else if (tabControl1.SelectedTab == tabPage2)
            {
                client.Grid.RequestMapRegion(client.Network.CurrentSim.Name, GridLayerType.Objects);

                //GetMap();
                BeginInvoke((MethodInvoker)GetMap);

                toolStrip1.Visible = false;
            }
            else if (tabControl1.SelectedTab == tabPage3)
            {
                toolStrip1.Visible = false;
            }
            else
            {
                toolStrip1.Visible = false;
                List<InventoryBase> invroot = client.Inventory.Store.GetContents(client.Inventory.Store.RootFolder.UUID);

                foreach (InventoryBase o in invroot)
                {
                    if (o.Name.ToLower(CultureInfo.CurrentCulture) == "favorites" || o.Name.ToLower(CultureInfo.CurrentCulture) == "my favorites")
                    {
                        if (o is InventoryFolder)
                        {
                            client.Inventory.RequestFolderContents(o.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByDate);

                            break;
                        }
                    }
                }
            }
        }
        #endregion

        private void world_MouseDown(object sender, MouseEventArgs e)
        {
            if (instance.MainForm.WindowState == FormWindowState.Maximized) return;
           
            if (e.Button != MouseButtons.Left)
                return;
            px = world.Top;
            py = world.Left;

            world.Width = newsize * 2;
            world.Height = newsize * 2; 

            move = true;
            rect.X = e.X;
            rect.Y = e.Y;
        }

        private int NormaliseSize(int number)
        {
            decimal ssize = (decimal)256 / (decimal)panel6.Width;

            int pos = Convert.ToInt32(Math.Round(number * ssize));

            return pos;
        }

        private void world_MouseUp(object sender, MouseEventArgs e)
        {
            //decimal ssize = (decimal)256 / (decimal)panel6.Width;

            int posX = NormaliseSize(e.X);   // Convert.ToInt32(Math.Round(e.X * ssize));
            int posY = NormaliseSize(e.Y);   // Convert.ToInt32(Math.Round(e.Y * ssize));

            Point mouse = new Point(posX, posY);

            MEGAboltInstance.AvLocation CurrentLoc = null;

            try
            {
                CurrentLoc = instance.avlocations.Find(delegate(MEGAboltInstance.AvLocation g) { return g.Rectangle.Contains(mouse) == true; });
            }
            catch { ; }

            if (CurrentLoc != null)
            {
                (new frmProfile(instance, avname, avuuid)).Show();
            }

            if (instance.MainForm.WindowState == FormWindowState.Maximized) return;

            move = false;

            world.Width = newsize;
            world.Height = newsize;

            world.Top = px;
            world.Left = py;
        }

        private void world_MouseMove(object sender, MouseEventArgs e)
        {
            int posX = NormaliseSize(e.X);
            int posY = NormaliseSize(e.Y);

            Point mouse = new Point(posX, posY);

            MEGAboltInstance.AvLocation CurrentLoc = null;

            try
            {
                CurrentLoc = instance.avlocations.Find(g => g.Rectangle.Contains(mouse) == true);
            }
            catch { ; }

            if (CurrentLoc != null)
            {
                if (!showing)
                {
                    UUID akey = (UUID)CurrentLoc.LocationName;

                    string apstn = "\nCoords.: " + Math.Round(CurrentLoc.Position.X).ToString(CultureInfo.CurrentCulture) 
                                                 + "/" + Math.Round(CurrentLoc.Position.Y).ToString(CultureInfo.CurrentCulture) 
                                                 + "/" + Math.Round(CurrentLoc.Position.Z).ToString(CultureInfo.CurrentCulture);

                    world.Cursor = Cursors.Hand;

                    string anme = string.Empty;  

                    lock (instance.avnames)
                    {
                        if (instance.avnames.ContainsKey(akey))
                        {
                            avname = instance.avnames[akey];

                            if (instance.avtags.ContainsKey(akey))
                            {
                                anme = "\nTag: " + instance.avtags[akey];
                            }

                            toolTip1.SetToolTip(world, avname + anme + apstn);
                            avuuid = akey;                            
                        }
                        else
                        {
                            toolTip1.SetToolTip(world, CurrentLoc.LocationName + apstn);
                        }
                    }

                    showing = true;
                }
            }
            else
            {
                world.Cursor = Cursors.NoMove2D;
                toolTip1.RemoveAll();
                showing = false;
            }


            if (instance.MainForm.WindowState == FormWindowState.Maximized) return;

            if (e.Button != MouseButtons.Left)
                return;

            if (move)
            {
                world.Left += e.X - rect.X;
                world.Top += e.Y - rect.Y;
            }
        }

        private Avatar GetAvID()
        {
            Avatar nav = new Avatar();
            UUID avid = (UUID)lvwRadar.SelectedItems[0].Tag;
            nav = CurrentSIM.ObjectsAvatars.Find((Avatar av) => av.ID == avid);

            return nav;
        }

        private void tbtnAttachments_Click(object sender, EventArgs e)
        {
            UUID av = (UUID)lvwRadar.SelectedItems[0].Tag;

            if (av == UUID.Zero) return;

            //string name = instance.avnames[av];

            Avatar sav = new Avatar();
            sav = GetAvID();

            if (sav != null)
            {
                (new WornAttachments(instance, sav)).Show(this);
            }
            else
            {
                MessageBox.Show("Avatar is out of range for this function.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);  
            }
        }

        private void lvwRadar_DoubleClick(object sender, EventArgs e)
        {
            if (lvwRadar.SelectedItems.Count == 1)
            {
                tbtnStartIM.PerformClick();
            }
        }

        private void lvwRadar_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvwRadar.SelectedItems.Count == 0)
            {
                tbtnTurn.Enabled =
                tbtnFollow.Enabled =
                tbtnStartIM.Enabled =
                tbtnGoto.Enabled =
                tbtnAddFriend.Enabled =
                tbtnFreeze.Enabled =
                tbtnBan.Enabled =
                tbtnEject.Enabled =
                tbtnAttachments.Enabled =
                tbtnProfile.Enabled = false;

                selectedname = string.Empty;
            }
            else
            {
                tbtnAttachments.Enabled = tbtnProfile.Enabled = true;

                tbtnTurn.Enabled =
                tbtnFollow.Enabled =
                tbtnStartIM.Enabled =
                tbtnGoto.Enabled =
                tbtnAddFriend.Enabled =
                tbtnFreeze.Enabled =
                tbtnBan.Enabled =
                tbtnEject.Enabled =

                (lvwRadar.SelectedItems[0].Name != client.Self.Name);

                selectedname = lvwRadar.SelectedItems[0].Name;
            }
        }

        private void lvwRadar_Leave(object sender, EventArgs e)
        {
            lvwRadar.SelectedItems.Clear();

            tbtnTurn.Enabled =
                tbtnFollow.Enabled =
                tbtnStartIM.Enabled =
                tbtnGoto.Enabled =
                tbtnAddFriend.Enabled =
                tbtnFreeze.Enabled =
                tbtnBan.Enabled =
                tbtnEject.Enabled =
                tbtnAttachments.Enabled =
                tbtnProfile.Enabled = false;

            //if (!instance.Config.CurrentConfig.iRadar)
            //{
            //    UpdateRadar();
            //}
        }

        private void ChatConsole_SizeChanged(object sender, EventArgs e)
        {
            //newsize = tabPage2.Width - 40;

            //px = world.Top;
            //py = world.Left;

            //System.Drawing.Size sz = new Size();
            //sz.Height = newsize;
            //sz.Width = newsize;

            //panel6.Size = sz;

            //lvwRadar.Columns[0].Width = lvwRadar.Width - 3;

            //if (instance.MainForm.WindowState == FormWindowState.Maximized)
            //{
            //    px = world.Top;
            //    py = world.Left;

            //    System.Drawing.Size sz = new Size();
            //    sz.Height = 256;
            //    sz.Width = 256;

            //    panel6.Size = sz;
            //}
            //else
            //{
            //    System.Drawing.Size sz = new Size();
            //    sz.Height = 140;
            //    sz.Width = 140;

            //    panel6.Size = sz;

            //    world.Top = px;
            //    world.Left = py;
            //}
        }

        private void lvwRadar_KeyUp(object sender, KeyEventArgs e)
        {
            if (lvwRadar.SelectedItems.Count < 1) return;
 
            if (e.Control && e.Alt && e.KeyCode == Keys.I)
            {
                tbtnStartIM.PerformClick();
            }

            if (e.Control && e.Alt && e.KeyCode == Keys.P)
            {
                tbtnProfile.PerformClick();
            }

            if (e.Control && e.Alt && e.KeyCode == Keys.A)
            {
                tbtnAttachments.PerformClick();
            }

            if (e.Control && e.Alt && e.KeyCode == Keys.F)
            {
                tbtnAddFriend.PerformClick();
            }

            if (e.Control && e.Alt && e.KeyCode == Keys.T)
            {
                tbtnTurn.PerformClick();
            }

            if (e.Control && e.Alt && e.KeyCode == Keys.W)
            {
                tbtnFollow.PerformClick();
            }

            if (e.Control && e.Alt && e.KeyCode == Keys.G)
            {
                tbtnGoto.PerformClick();
            }

            if (e.Control && e.Alt && e.KeyCode == Keys.E)
            {
                tbtnFreeze.PerformClick();
            }

            if (e.Control && e.Alt && e.KeyCode == Keys.J)
            {
                tbtnEject.PerformClick();
            }

            if (e.Control && e.Alt && e.KeyCode == Keys.B)
            {
                tbtnBan.PerformClick();
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            // All this could go into the extended rtb component in the future

            int startindex = 0;

            if (!string.IsNullOrEmpty(prevsearchtxt))
            {
                if (prevsearchtxt != tsFindText.Text.Trim())
                {
                    startindex = 0;
                    start = 0;
                    indexOfSearchText = 0;
                }
            }

            prevsearchtxt = tsFindText.Text.Trim();

            //int linenumber = rtbScript.GetLineFromCharIndex(rtbScript.SelectionStart) + 1;
            //Point pnt = rtbScript.GetPositionFromCharIndex(rtbScript.SelectionStart);

            if (tsFindText.Text.Length > 0)
                startindex = FindNext(tsFindText.Text.Trim(), start, rtbChat.Text.Length);

            // If string was found in the RichTextBox, highlight it
            if (startindex > 0)
            {
                // Set the highlight color as red
                rtbChat.SelectionColor = Color.LightBlue;
                // Find the end index. End Index = number of characters in textbox
                int endindex = tsFindText.Text.Length;
                // Highlight the search string
                rtbChat.Select(startindex, endindex);
                // mark the start position after the position of 
                // last search string
                start = startindex + endindex;

                if (start == rtbChat.TextLength || start > rtbChat.TextLength)
                {
                    startindex = 0;
                    start = 0;
                    indexOfSearchText = 0;
                }
            }
            else if (startindex == -1)
            {
                startindex = 0;
                start = 0;
                indexOfSearchText = 0;
            }
        }

        public int FindNext(string txtToSearch, int searchStart, int searchEnd)
        {
            // Unselect the previously searched string
            if (searchStart > 0 && searchEnd > 0 && indexOfSearchText >= 0)
            {
                rtbChat.Undo();
            }

            // Set the return value to -1 by default.
            int retVal = -1;

            // A valid starting index should be specified.
            // if indexOfSearchText = -1, the end of search
            if (searchStart >= 0 && indexOfSearchText >= 0)
            {
                // A valid ending index 
                if (searchEnd > searchStart || searchEnd == -1)
                {
                    // Determine if it's a match case or what
                    RichTextBoxFinds mcase = RichTextBoxFinds.None;

                    if (checkBox1.Checked)
                    {
                        mcase = RichTextBoxFinds.MatchCase;
                    }


                    if (checkBox2.Checked)
                    {
                        mcase |= RichTextBoxFinds.WholeWord;
                    }

                    // Find the position of search string in RichTextBox
                    indexOfSearchText = rtbChat.Find(txtToSearch, searchStart, searchEnd, mcase);
                    // Determine whether the text was found in richTextBox1.
                    if (indexOfSearchText != -1)
                    {
                        // Return the index to the specified search text.
                        retVal = indexOfSearchText;
                    }
                }
            }

            return retVal;
        }

        private void toolStripButton1_Click_2(object sender, EventArgs e)
        {
            if (panel7.Visible)
            {
                panel7.Visible = false;
                tsbSearch.ToolTipText = "Show chat search";
                rtbChat.Height += 28; 
            }
            else
            {
                panel7.Visible = true;
                tsbSearch.ToolTipText = "Hide chat search";
                rtbChat.Height -= 28;
            }
        }

        private void tsFindText_Click(object sender, EventArgs e)
        {
            tsFindText.SelectionStart = 0;
            tsFindText.SelectionLength = tsFindText.Text.Length;
        }

        private void vgate_OnVoiceConnectionChange(VoiceGateway.ConnectionState state)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    vgate_OnVoiceConnectionChange(state);
                }));

                return;
            }

            try
            {
                string s = string.Empty;

                if (state == VoiceGateway.ConnectionState.AccountLogin)
                {
                    s = "Logging In...";
                }
                else if (state == VoiceGateway.ConnectionState.ConnectorConnected)
                {
                    s = "Connected...";
                }
                else if (state == VoiceGateway.ConnectionState.DaemonConnected)
                {
                    s = "Daemon Connected. Starting...";
                }
                else if (state == VoiceGateway.ConnectionState.DaemonStarted)
                {
                    s = "Daemon Started. Please wait...";
                }
                else if (state == VoiceGateway.ConnectionState.SessionRunning)
                {
                    s = "Session Started & Ready";
                }

                label18.Text = s;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "MEGAbolt");
            }
        }

        private void vgate_OnAuxGetCaptureDevicesResponse(object sender, VoiceGateway.VoiceDevicesEventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => LoadMics(e.Devices)));
        }

        private void vgate_OnAuxGetRenderDevicesResponse(object sender, VoiceGateway.VoiceDevicesEventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => LoadSpeakers(e.Devices)));
        }

        private void vgate_OnSessionCreate(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    vgate_OnSessionCreate(sender, e);
                }));

                return;
            }

            try
            {
                vgate.AuxGetCaptureDevices();
                vgate.AuxGetRenderDevices();

                vgate.MicMute = true;
                vgate.SpkrMute = false;
                vgate.SpkrLevel = 70;
                vgate.MicLevel = 70;
                checkBox5.ForeColor = Color.Red;
                label18.Text = "Session Started & Ready";
                EnableVoice(true);

                if (!checkBox3.Checked)
                {
                    checkBox3.Checked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "MEGAbolt");
            }
        }

        private void LoadMics(List<string> list)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    LoadMics(list);
                }));

                return;
            }

            try 
            {
                cboCapture.Items.Clear();

                foreach (string dev in list)
                {
                    cboCapture.Items.Add(dev);  
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "MEGAbolt"); }

            try
            {
                string cmic = vgate.CurrentCaptureDevice;

                if (string.IsNullOrEmpty(cmic))
                {
                    cboCapture.SelectedItem = cmic;    //cmic = mics[0];
                }
                else
                {
                    cboCapture.Text = cmic;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "MEGAbolt"); }

            vgate.MicMute = true;
            vgate.MicLevel = 70;
        }

        private void LoadSpeakers(List<string> list)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    LoadSpeakers(list);
                }));

                return;
            }

            try
            {
                cboRender.Items.Clear();
   
                foreach (string dev in list)
                {
                    cboRender.Items.Add(dev);   
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "MEGAbolt"); }

            try
            {
                string cspk = vgate.PlaybackDevice;

                if (string.IsNullOrEmpty(cspk))
                {
                    cboRender.SelectedItem = cspk; //speakers[0];
                }
                else
                {
                    cboRender.Text = cspk;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "MEGAbolt"); }

            vgate.SpkrMute = false;
            vgate.SpkrLevel = 70;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            vgate.MicMute = checkBox3.Checked;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            vgate.SpkrMute = checkBox4.Checked;
        }

        private bool CheckVoiceSetupFile(string filename)
        {
            if (!File.Exists(Application.StartupPath + "\\" + filename))
            {
                MessageBox.Show($"The required '{filename}' file was not found.");
                return false;
            }
            return true;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (!CheckVoiceSetupFile("SLVoice.exe")) { return; }
            if (!CheckVoiceSetupFile("DbgHelp.dll")) { return; }
            if (!CheckVoiceSetupFile("ortp.dll")) { return; }
            if (!CheckVoiceSetupFile("vivoxsdk.dll")) { return; }
            if (!CheckVoiceSetupFile("zlib1.dll")) { return; }

            if (checkBox5.Checked)
            {
                if (!instance.AllowVoice)
                {
                    label18.Text = "Voice is disabled on this parcel";
                    return;
                }

                try
                {
                    vgate = new VoiceGateway(client);
                    vgate.OnVoiceConnectionChange += vgate_OnVoiceConnectionChange;
                    vgate.OnAuxGetCaptureDevicesResponse += vgate_OnAuxGetCaptureDevicesResponse;
                    vgate.OnAuxGetRenderDevicesResponse += vgate_OnAuxGetRenderDevicesResponse;
                    vgate.OnSessionCreate += vgate_OnSessionCreate;

                    vgate.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "MEGAbolt");
                }
            }
            else
            {
                if (!instance.AllowVoice)
                {
                    label18.Text = "Voice is disabled on this parcel";
                    return;
                }

                try
                {
                    vgate.MicMute = true;
                    vgate.Stop();
                    vgate.Dispose();

                    EnableVoice(false);
                    cboRender.Items.Clear();
                    cboCapture.Items.Clear();   

                    vgate.OnVoiceConnectionChange -= vgate_OnVoiceConnectionChange;
                    vgate.OnAuxGetCaptureDevicesResponse -= vgate_OnAuxGetCaptureDevicesResponse;
                    vgate.OnAuxGetRenderDevicesResponse -= vgate_OnAuxGetRenderDevicesResponse;
                    vgate.OnSessionCreate -= vgate_OnSessionCreate;

                    if (!checkBox3.Checked)
                    {
                        checkBox3.Checked = true;
                    }

                    checkBox5.ForeColor = Color.Black;
                    label18.Text = "Check 'Voice ON' box below. Then on 'Session start' unmute MIC to talk";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "MEGAbolt");
                }
            }
        }

        private void EnableVoice(bool ebl)
        {
            cboCapture.Enabled = ebl;
            cboRender.Enabled = ebl;
            trackBar1.Enabled = ebl;
            trackBar2.Enabled = ebl;
            checkBox3.Enabled = ebl;
            checkBox4.Enabled = ebl; 
        }

        private void cboCapture_SelectedIndexChanged(object sender, EventArgs e)
        {
            vgate.CurrentCaptureDevice = cboCapture.SelectedItem.ToString();  
        }

        private void cboRender_SelectedIndexChanged(object sender, EventArgs e)
        {
            vgate.PlaybackDevice = cboRender.SelectedItem.ToString(); 
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            vgate.MicLevel = trackBar1.Value;
            toolTip1.SetToolTip(trackBar1, "Volume: " + trackBar1.Value.ToString(CultureInfo.CurrentCulture)); 
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            vgate.SpkrLevel = trackBar2.Value;
            toolTip1.SetToolTip(trackBar2, "Volume: " + trackBar2.Value.ToString(CultureInfo.CurrentCulture));
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!avrezzed && netcom.IsLoggedIn)
            {
                client.Appearance.RequestSetAppearance(true);
                timer2.Enabled = false;
                timer2.Stop();
            }  
        }

        private void world_Click(object sender, EventArgs e)
        {

        }

        private void tbtnHelp_Click(object sender, EventArgs e)
        {
            Utilities.OpenBrowser(@"https://metabolt.radegast.life/help/");
        }

        private void button5_KeyDown(object sender, KeyEventArgs e)
        {
            fwd(true);
        }

        private void button5_KeyUp(object sender, KeyEventArgs e)
        {
            fwd(false);
        }

        private void button4_KeyDown(object sender, KeyEventArgs e)
        {
            bck(true);
        }

        private void button4_KeyUp(object sender, KeyEventArgs e)
        {
            bck(false);
        }

        private void button5_MouseDown(object sender, MouseEventArgs e)
        {
            fwd(true);
        }

        private void button5_MouseUp(object sender, MouseEventArgs e)
        {
            fwd(false);
        }

        private void button4_MouseDown(object sender, MouseEventArgs e)
        {
            bck(true);
        }

        private void button4_MouseUp(object sender, MouseEventArgs e)
        {
            bck(false);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //WalkRight();
        }

        private void WalkRight()
        {
            client.Self.Movement.SendManualUpdate(AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG, client.Self.Movement.Camera.Position,
                    client.Self.Movement.Camera.AtAxis, client.Self.Movement.Camera.LeftAxis, client.Self.Movement.Camera.UpAxis,
                    client.Self.Movement.BodyRotation, client.Self.Movement.HeadRotation, client.Self.Movement.Camera.Far, AgentFlags.None,
                    AgentState.None, true);
        }

        private void button6_MouseDown(object sender, MouseEventArgs e)
        {
            WalkRight();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            //WalkLeft();
        }

        private void WalkLeft()
        {
            client.Self.Movement.SendManualUpdate(AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS, client.Self.Movement.Camera.Position,
                    client.Self.Movement.Camera.AtAxis, client.Self.Movement.Camera.LeftAxis, client.Self.Movement.Camera.UpAxis,
                    client.Self.Movement.BodyRotation, client.Self.Movement.HeadRotation, client.Self.Movement.Camera.Far, AgentFlags.None,
                    AgentState.None, true);
        }

        private void button8_MouseDown(object sender, MouseEventArgs e)
        {
            WalkLeft();
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void picVoice_MouseHover(object sender, EventArgs e)
        {
            tTip.Show(picVoice);
        }

        private void picVoice_MouseLeave(object sender, EventArgs e)
        {
            tTip.Close(); 
        }

        private void picMap_MouseHover(object sender, EventArgs e)
        {
            tTip1.Show(picMap);
        }

        private void picMap_MouseLeave(object sender, EventArgs e)
        {
            tTip1.Close(); 
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void toolStripDropDownButton1_Click_1(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Utilities.OpenBrowser(@"http://www.duckduckgo.com/");
        }

        public void UpdateFavourites(List<InventoryBase> foundfolders)
        {
            if (foundfolders == null) return;

            if (foundfolders.Count > 0)
            {
                tsFavs.Visible = true;
                tsFavs.Items.Clear();

                foreach (InventoryBase oitem in foundfolders)
                {
                    InventoryItem item = (InventoryItem)oitem;

                    if (item.InventoryType == InventoryType.Landmark)
                    {
                        string iname = item.Name;
                        string desc = item.Description;

                        //int twh = tabPage4.Width; 

                        if (iname.Length > 48)
                        {
                            iname = iname.Substring(0, 48) + "...";
                        }

                        ToolStripButton btn = new ToolStripButton(iname, null,
                            FavsToolStripMenuItem_Click, item.AssetUUID.ToString())
                        {

                            //if (!tsFavs.Items.Contains(btn))
                            //{
                            TextAlign = ContentAlignment.MiddleLeft,
                            ToolTipText = desc
                        };
                        tsFavs.Items.Add(btn);

                        ToolStripSeparator sep = new ToolStripSeparator();
                        tsFavs.Items.Add(sep);
                        //}

                        //sep.Dispose(); 
                        //btn.Dispose(); 
                    }
                }
            }
        }

        private void FavsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //string cbtn = sender.ToString();

            ToolStripButton btn = (ToolStripButton)sender;
            UUID landmark = new UUID();

            if (!UUID.TryParse(btn.Name, out landmark))
            {
                MessageBox.Show("Invalid Landmark", "Teleport");
                return;
            }

            client.Self.Teleport(landmark);
        }

        //[DllImport("user32.dll")]
        //static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, Int32 wParam, Int32 lParam);

        //const int WM_USER = 0x400;
        //const int EM_HIDESELECTION = WM_USER + 63;
        //int cpos = 0;

        private void rtbChat_Click(object sender, EventArgs e)
        {
            //rtbChat.HideSelection = true;
            //SendMessage(rtbChat.Handle, EM_HIDESELECTION, 1, 0);
            //cpos = rtbChat.SelectionStart;

            ////LockWindow(this.Handle);
            ////rtbChat.SuspendLayout();  
        }

        private void rtbChat_Leave(object sender, EventArgs e)
        {
            ////rtbChat.HideSelection = false;

            //SendMessage(rtbChat.Handle, EM_HIDESELECTION, 0, 0);

            //rtbChat.SelectionStart = cpos;
            //cpos = 0;
            ////LockWindow(IntPtr.Zero);
        }

        private void rtbChat_Enter(object sender, EventArgs e)
        {
            ////rtbChat.HideSelection = true;
            //SendMessage(rtbChat.Handle, EM_HIDESELECTION, 1, 0);
            //cpos = rtbChat.SelectionStart;
        }

        private void world_DoubleClick(object sender, EventArgs e)
        {
            (new frmMapClient(instance)).Show();
        }

        private void lvwRadar_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                ListViewItem item = lvwRadar.GetItemAt(e.X, e.Y);
                ListViewHitTestInfo info = lvwRadar.HitTest(e.X, e.Y);

                if (item != null)
                {
                    if (tooltiptext != info.Item.ToolTipText)
                    {
                        tooltiptext = info.Item.ToolTipText;
                        toolTip.SetToolTip(lvwRadar, info.Item.ToolTipText);
                    }
                }
                else
                {
                    tooltiptext = string.Empty;
                    toolTip.SetToolTip(lvwRadar, null);
                }
            }
            catch
            {
                try
                {
                    tooltiptext = string.Empty;
                    toolTip.SetToolTip(lvwRadar, null);
                }
                catch { ; }
            }
        }

        private void lvwRadar_SizeChanged(object sender, EventArgs e)
        {
            lvwRadar.Columns[0].Width = lvwRadar.Width - 3;
        }

        private void tabPage2_SizeChanged(object sender, EventArgs e)
        {
            newsize = tabPage2.Width - 40;

            px = world.Top;
            py = world.Left;

            Size sz = new Size
            {
                Height = newsize,
                Width = newsize
            };

            panel6.Size = sz;
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            label13.Text = "Teleporting...";

            TPtimer.Stop();
            TPtimer.Enabled = false;

            pTP.Visible = false; 
        }

        private void TPtimer_Tick(object sender, EventArgs e)
        {
            pTP.Visible = false;
            label13.Text = "Teleporting...";

            TPtimer.Stop();
            TPtimer.Enabled = false;
        }

        private void rtbChat_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                pTP.Location = new Point(
                rtbChat.Width / 2 - pTP.Size.Width / 2,
                rtbChat.Height / 2 - pTP.Size.Height / 2);
                pTP.Anchor = AnchorStyles.None;
            }
            catch (Exception ex)
            {
                Logger.Log("MB ERROR", Helpers.LogLevel.Error, ex);  
            }
        }

        private void cbxInput_Click(object sender, EventArgs e)
        {
            //rtbChat.HideSelection = false;
        }

        private void button3_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(button3, "Turn right");
        }

        private void button2_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(button2, "Turn left");
        }

        private void button7_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(button7, "Hover");
        }

        private void button5_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(button5, "Walk forward");
        }

        private void button8_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(button8, "Walk left");
        }

        private void button4_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(button4, "Walk back");
        }

        private void button6_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(button6, "Walk right");
        }
    }
}
