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
using System.IO;
using System.Windows.Forms;
using MEGAbolt.NetworkComm;
using OpenMetaverse;
using OpenMetaverse.Assets;
using System.Threading;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BugSplatDotNetStandard;
using Microsoft.Web.WebView2.WinForms;
using CSJ2K;
using SkiaSharp;
using SkiaSharp.Views.Desktop;


namespace MEGAbolt
{
    public partial class frmProfile : Form
    {
        private MEGAboltInstance instance;
        private TabsConsole tabConsole;
        private MEGAboltNetcom netcom;
        private GridClient client;
        private string fullName;
        private UUID agentID;
        Avatar.AvatarProperties props;
        private bool aboutchanged = false;
        private bool lifeaboutchanged = false;
        private bool urlchanged = false;

        private UUID FLImageID = UUID.Zero;
        private UUID SLImageID = UUID.Zero;
        private UUID PickImageID = UUID.Zero;
        private UUID partner = UUID.Zero;
        private UUID pickUUID = UUID.Zero;
        private string parcelname = string.Empty;
        private string simname = string.Empty;
        private int posX = 0;
        private int posY = 0;
        private int posZ = 0;
        //private bool displaynamechanged = false;
        //private string olddisplayname = string.Empty;
        private List<UUID> displaynames = new List<UUID>();
        string newname = string.Empty;
        const int WM_NCHITTEST = 0x0084;
        const int HTTRANSPARENT = -1;
        const int HTCLIENT = 1;
        private NumericStringComparer lvwColumnSorter;

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

        protected override void WndProc(ref Message m) 
        { 
            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST) 
            { 
                if (m.Result.ToInt32() == HTTRANSPARENT)            
                    m.Result = new IntPtr(HTCLIENT); 
            } 
        }

        public frmProfile(MEGAboltInstance instance, string fullName, UUID agentID)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.fullName = fullName;
            this.agentID = agentID;
            tabConsole = instance.TabConsole;

            while (!IsHandleCreated)
            {
                // Force handle creation
                IntPtr temp = Handle;
            }

            txtOnline.Text = "";
            Text = $"{fullName} (profile) - MEGAbolt";

            AddClientEvents();
            AddNetcomEvents();

            InitializeProfile();

            if (agentID == client.Self.AgentID)
            {
                rtbAbout.ReadOnly = false;
                txtWebURL.ReadOnly = false;
                rtbAboutFL.ReadOnly = false;
                picSLImage.AllowDrop = true;
                picFLImage.AllowDrop = true;
                //txtDisplayName.ReadOnly = false;
                button7.Enabled = true;
                button8.Enabled = true;
                txtTitle.ReadOnly = false;
                txtDescription.ReadOnly = false;
            }

            lvwColumnSorter = new NumericStringComparer();
            lvGroups.ListViewItemSorter = lvwColumnSorter;
            lvwPicks.ListViewItemSorter = lvwColumnSorter;
        }

        ~frmProfile()
        {
            Dispose();
            //GC.Collect(); 
        }
        private void CleanUp()
        {
            client.Avatars.UUIDNameReply -= Avatars_OnAvatarNames;
            client.Avatars.AvatarPropertiesReply -= Avatars_OnAvatarProperties;
            netcom.ClientLoggedOut -= netcom_ClientLoggedOut;
            client.Avatars.AvatarGroupsReply -= Avatars_OnGroupsReply;
            client.Avatars.PickInfoReply -= Avatars_OnPicksInfoReply;
            client.Avatars.AvatarPicksReply -= Avatars_OnPicksReply;
            client.Parcels.ParcelInfoReply -= Parcels_OnParcelInfoReply;
            client.Avatars.DisplayNameUpdate -= Avatar_DisplayNameUpdated;    
        }

        private void Avatar_DisplayNameUpdated(object sender, DisplayNameUpdateEventArgs e)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                //string old = e.OldDisplayName;
                string newname = e.DisplayName.DisplayName;

                if (!newname.ToLower(CultureInfo.CurrentCulture).Contains("resident")
                    && !newname.ToLower(CultureInfo.CurrentCulture).Contains(" "))
                {
                    txtDisplayName.Text = newname;
                    button7.Enabled = false;
                }
                else
                {
                    txtDisplayName.Text = string.Empty;
                    button7.Enabled = true;
                }
            }));
        }

        private void AddClientEvents()
        {
            client.Avatars.UUIDNameReply += Avatars_OnAvatarNames;
            client.Avatars.AvatarPropertiesReply += Avatars_OnAvatarProperties;
            client.Avatars.AvatarGroupsReply += Avatars_OnGroupsReply;
            client.Avatars.PickInfoReply += Avatars_OnPicksInfoReply;
            client.Avatars.AvatarPicksReply += Avatars_OnPicksReply;
            client.Parcels.ParcelInfoReply += Parcels_OnParcelInfoReply;
            client.Avatars.DisplayNameUpdate += Avatar_DisplayNameUpdated;
        }

        private void AddNetcomEvents()
        {
            netcom.ClientLoggedOut += netcom_ClientLoggedOut;
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            Close();
        }

        private void Avatars_OnPicksReply(object sender, AvatarPicksReplyEventArgs e)
        {
            if (e.AvatarID != agentID) return;

            BeginInvoke(new MethodInvoker(() =>
            {
                PopulatePicksList(e.Picks);
                loadwait1.Visible = false;
            }));
        }

        private void PopulatePicksList(Dictionary<UUID, string> picks)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => PopulatePicksList(picks)));
                return;
            }

            lvwPicks.Items.Clear(); 

            foreach (KeyValuePair<UUID, string> pick in picks)
            {
                ListViewItem item = lvwPicks.Items.Add(pick.Value);
                item.Tag = pick.Key;
            }

            button8.Enabled = picks.Count < 10;
        }

        private void Avatars_OnPicksInfoReply(object sender, PickInfoReplyEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Avatars_OnPicksInfoReply(sender, e)));
                return;
            }

            BeginInvoke(new MethodInvoker(() =>
            {
                txtTitle.Text = e.Pick.Name;
                txtDescription.Text = e.Pick.Desc;
                txtSlurl.Text = "None";
            }));

            pickUUID = e.Pick.ParcelID;
            client.Parcels.RequestParcelInfo(e.Pick.ParcelID);

            PickImageID = e.Pick.SnapshotID;

            if (!instance.ImageCache.ContainsImage(PickImageID))
            {
                client.Assets.RequestImage(PickImageID, ImageType.Normal, Assets_OnImageReceived);
            }
            else
            {
                BeginInvoke(
                    new OnSetPickImage(SetPickImage), PickImageID, instance.ImageCache.GetImage(PickImageID));
            }
        }

        private void Parcels_OnParcelInfoReply(object sender, ParcelInfoReplyEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Parcels_OnParcelInfoReply(sender, e)));
                return;
            }

            if (pickUUID != e.Parcel.ID) return;

            BeginInvoke(new MethodInvoker(() =>
            {
                parcelname = e.Parcel.Name;
                simname = e.Parcel.SimName;

                posX = (int)e.Parcel.GlobalX % 256;
                posY = (int)e.Parcel.GlobalY % 256;
                posZ = (int)e.Parcel.GlobalZ % 256;

                txtSlurl.Text = $"{parcelname}, {simname} ({posX.ToString(CultureInfo.CurrentCulture)},{posY.ToString(CultureInfo.CurrentCulture)},{posZ.ToString(CultureInfo.CurrentCulture)})";
            }));
        }

        private void Avatars_OnAvatarNames(object sender, UUIDNameReplyEventArgs e)
        {
            foreach (KeyValuePair<UUID, string> av in e.Names)
            {
                try
                {
                    BeginInvoke(new OnSetPartnerText(SetPartnerText), av);
                    break;
                }
                catch
                {
                    ; 
                }
            }
        }

        private void Avatars_OnGroupsReply(object sender, AvatarGroupsReplyEventArgs e)
        {
            if (e.AvatarID != agentID) return;

            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Avatars_OnGroupsReply(sender, e)));
                return;
            }

            //lvGroups.Items.Clear();  

            foreach (var lvi in e.Groups.Select(@group => new ListViewItem
                     {
                         Text = @group.GroupName,
                         Tag = @group
                     }).Where(lvi => !lvGroups.Items.Contains(lvi)))
            {
                lvGroups.Items.Add(lvi);
            }
        }

        private delegate void OnSetPartnerText(KeyValuePair<UUID, string> kvp);
        private void SetPartnerText(KeyValuePair<UUID, string> kvp)
        {
            if (partner == kvp.Key)
            {
                client.Avatars.AvatarPropertiesReply -= Avatars_OnAvatarProperties;
                txtPartner.Text = kvp.Value;
            }
        }

        //comes in on a separate thread
        private void Assets_OnImageReceived(TextureRequestState image, AssetTexture texture)
        {
            if (texture.AssetID != SLImageID && texture.AssetID != FLImageID)
            {
                if (texture.AssetID != PickImageID) return;

                using (var bitmap = J2kImage.FromBytes(texture.AssetData).As<SKBitmap>())
                {
                    var decodedImage = bitmap.ToBitmap();
                    if (decodedImage != null)
                    {
                        BeginInvoke(new MethodInvoker(() =>
                        {
                            pictureBox1.Image = decodedImage;
                            loadwait2.Visible = false;
                        }));

                        instance.ImageCache.AddImage(texture.AssetID, decodedImage);
                    }
                }                
            }
            else
            {
                Image decodedImage = null;
                using (var bitmap = J2kImage.FromBytes(texture.AssetData).As<SKBitmap>())
                {
                   decodedImage = bitmap.ToBitmap();
                }

                if (decodedImage == null)
                {
                    if (texture.AssetID == SLImageID) BeginInvoke(new MethodInvoker(SetBlankSLImage));
                    else if (texture.AssetID == FLImageID) BeginInvoke(new MethodInvoker(SetBlankFLImage));

                    return;
                }

                instance.ImageCache.AddImage(texture.AssetID, decodedImage);

                try
                {
                    BeginInvoke(new OnSetProfileImage(SetProfileImage), texture.AssetID, decodedImage);
                }
                catch { ; }

                //if (image.Success)
                //    picInsignia.Image = OpenJPEGNet.OpenJPEG.DecodeToImage(image.AssetData);
            }
        }

        private delegate void OnSetPickImage(UUID id, Image image);
        private void SetPickImage(UUID id, Image image)
        {
            if (id == PickImageID)
            {
                loadwait2.Visible = false;
                pictureBox1.Image = image;
            }
        }

        //called on GUI thread
        private delegate void OnSetProfileImage(UUID id, Image image);
        private void SetProfileImage(UUID id, Image image)
        {
            if (id == SLImageID)
            {
                picSLImage.Image = image;
                proSLImage.Visible = false;
            }
            else if (id == FLImageID)
            {
                picFLImage.Image = image;
                proFLImage.Visible = false;
            }
        }

        private void SetBlankSLImage()
        {
            picSLImage.BackColor = Color.FromKnownColor(KnownColor.Control);
            proSLImage.Visible = false;
        }

        private void SetBlankFLImage()
        {
            picFLImage.BackColor = Color.FromKnownColor(KnownColor.Control);
            proFLImage.Visible = false;
        }

        //comes in on separate thread
        private void Avatars_OnAvatarProperties(object sender, AvatarPropertiesReplyEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => Avatars_OnAvatarProperties(sender, e)));
                return;
            }

            if (e.AvatarID != agentID) return;

            props = e.Properties;

            FLImageID = props.FirstLifeImage;
            SLImageID = props.ProfileImage;

            if (instance.avtags.ContainsKey(e.AvatarID))
            {
                try
                {
                    string atag = instance.avtags[e.AvatarID];
                    txtTag.Text = atag;
                }
                catch { ; }
            }
            else
            {
                txtTag.Text = "avatar out of range";
            }

            if (SLImageID != UUID.Zero)
            {
                if (!instance.ImageCache.ContainsImage(SLImageID))
                    client.Assets.RequestImage(SLImageID, ImageType.Normal, Assets_OnImageReceived);
                else
                    BeginInvoke(
                        new OnSetProfileImage(SetProfileImage), SLImageID, instance.ImageCache.GetImage(SLImageID));
            }
            else
            {
                BeginInvoke(new MethodInvoker(SetBlankSLImage));
            }

            if (FLImageID != UUID.Zero)
            {
                if (!instance.ImageCache.ContainsImage(FLImageID))
                    client.Assets.RequestImage(FLImageID, ImageType.Normal, Assets_OnImageReceived);
                else
                    BeginInvoke(
                        new OnSetProfileImage(SetProfileImage), FLImageID, instance.ImageCache.GetImage(FLImageID));
            }
            else
            {
                BeginInvoke(new MethodInvoker(SetBlankFLImage));
            }

            BeginInvoke(
                new OnSetProfileProperties(SetProfileProperties), props);
        }

        //called on GUI thread
        private delegate void OnSetProfileProperties(Avatar.AvatarProperties properties);
        private void SetProfileProperties(Avatar.AvatarProperties properties)
        {
            try
            {
                txtBornOn.Text = properties.BornOn;
                partner = properties.Partner;
                if (properties.Partner != UUID.Zero)
                {
                    if (!instance.avnames.ContainsKey(properties.Partner))
                    {
                        client.Avatars.RequestAvatarName(properties.Partner);
                    }
                    else
                    {
                        txtPartner.Text = instance.avnames[properties.Partner];
                    }
                }

                try
                {
                    rtbAccountInfo.AppendText(fullName.EndsWith("Linden", StringComparison.CurrentCulture)
                        ? "Linden Lab Employee\n"
                        : "Resident\n");
                }
                catch { ; }

                if (properties.Identified && !properties.Transacted) rtbAccountInfo.AppendText("Payment Info On File\n");
                else if (properties.Transacted) rtbAccountInfo.AppendText("Payment Info Used\n");
                else rtbAccountInfo.AppendText("No Payment Info On File\n");

                txtOnline.Text = properties.Online ? "Currently Online" : "unknown";

                rtbAbout.AppendText(properties.AboutText);

                txtWebURL.Text = properties.ProfileURL;
                btnWebView.Enabled = btnWebOpen.Enabled = (txtWebURL.TextLength > 0);

                rtbAboutFL.AppendText(properties.FirstLifeText);

                txtUUID.Text = agentID.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log("Exception setting Profile properties", Helpers.LogLevel.Error, ex);
            }
        }

        private void InitializeProfile()
        {
            txtFullName.Text = fullName;
            btnOfferTeleport.Enabled = button1.Enabled = button2.Enabled = button3.Enabled = btnPay.Enabled = (agentID != client.Self.AgentID);

            client.Avatars.RequestAvatarProperties(agentID);
            client.Avatars.RequestAvatarPicks(agentID);

            bool dnavailable = client.Avatars.DisplayNamesAvailable();

            if (dnavailable)
            {
                List<UUID> avIDs = new List<UUID> { agentID };
                client.Avatars.GetDisplayNames(avIDs, DisplayNameReceived);
            }

            //this.textBox1.DragDrop += new System.Windows.Forms.DragEventHandler(this.textBox1_DragDrop);
            //this.textBox1.DragEnter += new System.Windows.Forms.DragEventHandler(this.textBox1_DragEnter);
            //this.textBox1.DragOver += new System.Windows.Forms.DragEventHandler(this.textBox1_DragOver); 
        }

        private void DisplayNameReceived(bool success, AgentDisplayName[] names, UUID[] badIDs)
        {
            if (success)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    if (!names[0].DisplayName.ToLower(CultureInfo.CurrentCulture).Contains("resident") && !names[0].DisplayName.ToLower(CultureInfo.CurrentCulture).Contains(" "))
                    {
                        txtDisplayName.Text = names[0].DisplayName;
                    }
                    else
                    {
                        txtDisplayName.Text = string.Empty;
                    }
                }));
            }
        }

        private void frmProfile_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanUp();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnWebView_Click(object sender, EventArgs e)
        {
            WebView2 web = new WebView2();
            web.Dock = DockStyle.Fill;

            string url = txtWebURL.Text;
            if (!url.StartsWith("http", StringComparison.CurrentCulture) 
                && !url.StartsWith("https", StringComparison.CurrentCulture))
            {

                url = "http://" + url;
            }

            try {
                web.Source = new Uri(url);
            } catch (UriFormatException)
            { return; } 

            pnlWeb.Controls.Add(web);
        }

        private static void ProcessWebURL(string url)
        {
            if (url.StartsWith("http://", StringComparison.CurrentCulture) || url.StartsWith("ftp://", StringComparison.CurrentCulture))
                Utilities.OpenBrowser(url);
            else
                Utilities.OpenBrowser("http://" + url);
        }

        private void btnWebOpen_Click(object sender, EventArgs e)
        {
            ProcessWebURL(txtWebURL.Text);
        }

        private void rtbAbout_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            ProcessWebURL(e.LinkText);
        }

        private void rtbAboutFL_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            ProcessWebURL(e.LinkText);
        }

        private void btnOfferTeleport_Click(object sender, EventArgs e)
        {
            client.Self.SendTeleportLure(agentID, $"Join me in {client.Network.CurrentSim.Name}!");
        }

        private void btnPay_Click(object sender, EventArgs e)
        {
            (new frmPay(instance, agentID, fullName)).Show(this);
        }

        private void frmProfile_Load(object sender, EventArgs e)
        {
            if (agentID == client.Self.AgentID)
            {
                button5.Visible = true;
            }

            // Load notes
            string logdir = instance.appdir;
            logdir += "\\Notes\\";
            LoadNotes(logdir);

            CenterToParent();
        }

        private void LoadNotes(string LogPath)
        {
            DirectoryInfo di = new DirectoryInfo(LogPath);
            FileSystemInfo[] files = di.GetFileSystemInfos();

            foreach (FileSystemInfo fi in files)
            {
                string inFile = fi.FullName;
                string finname = fi.Name;

                if (fullName != null)
                {
                    if (finname.Contains(fullName))
                    {
                        rtbNotes.LoadFile(inFile);
                    }
                }
            }
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(TreeNode)) 
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode node = e.Data.GetData(typeof(TreeNode)) as TreeNode;

            if (node == null) return;

            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                InventoryBase io = (InventoryBase)node.Tag;

                if (node.Tag is InventoryFolder folder)
                {
                    //InventoryFolder folder = (InventoryFolder)io;

                    client.Inventory.GiveFolder(folder.UUID, folder.Name, agentID, true);
                    instance.TabConsole.DisplayChatScreen("Offered inventory folder " + folder.Name + " to " + fullName + ".");
                }
                else
                {
                    InventoryItem item = (InventoryItem)io;

                    if ((item.Permissions.OwnerMask & PermissionMask.Copy) != PermissionMask.Copy)
                    {
                        DialogResult res = MessageBox.Show("This is a 'no copy' item and you will lose ownership if you continue.", "Warning", MessageBoxButtons.OKCancel);

                        if (res == DialogResult.Cancel) return;
                    }

                    client.Inventory.GiveItem(item.UUID, item.Name, item.AssetType, agentID, true);
                    instance.TabConsole.DisplayChatScreen($"Offered inventory item {item.Name} to {fullName}.");
                }
            }
        }

        private void tpgProfile_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void rtbAccountInfo_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtOnline_TextChanged(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void txtBornOn_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void rtbAbout_TextChanged(object sender, EventArgs e)
        {
            aboutchanged = true;
        }

        private void picSLImage_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(TreeNode)) 
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void rtbAbout_Leave(object sender, EventArgs e)
        {
            if (!aboutchanged) return;

            if (agentID != client.Self.AgentID) return;
        
            props.AboutText = rtbAbout.Text;

            client.Self.UpdateProfile(props); 
        }

        private void txtWebURL_Leave(object sender, EventArgs e)
        {
           if (!urlchanged) return;

           if (agentID != client.Self.AgentID) return;

            props.ProfileURL = txtWebURL.Text;

            client.Self.UpdateProfile(props);
        }

        private void rtbAboutFL_Leave(object sender, EventArgs e)
        {
            if (!lifeaboutchanged) return;

            if (agentID != client.Self.AgentID) return;
      
            props.FirstLifeText = rtbAboutFL.Text;

            client.Self.UpdateProfile(props);
        }

        private void txtWebURL_TextChanged(object sender, EventArgs e)
        {
            urlchanged = true;
        }

        private void rtbAboutFL_TextChanged(object sender, EventArgs e)
        {
            lifeaboutchanged = true;
        }

        private void picSLImage_DragEnter(object sender, DragEventArgs e)
        {
            //if (e.Data.GetDataPresent(DataFormats.FileDrop))
            //{
            //    e.Effect = DragDropEffects.Move;
            //}
            //else
            //{
            //    e.Effect = DragDropEffects.None;
            //}

            e.Effect = e.Data.GetDataPresent(typeof(TreeNode))
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void picSLImage_DragDrop(object sender, DragEventArgs e)
        {
            //if (e.Data.GetDataPresent(DataFormats.FileDrop))
            //{
            //    string s = (string)e.Data.GetData(DataFormats.FileDrop, false);

            //    char[] deli = ",".ToCharArray();
            //    string[] iDets = s.Split(deli);

            //    bool isimage = false;

            //    if (iDets[2].ToString() == "ImageJPEG")
            //    {
            //        isimage = true; 
            //    }
            //    else if (iDets[2].ToString() == "ImageTGA")
            //    {
            //        isimage = true;
            //    }
            //    else if (iDets[2].ToString() == "Texture")
            //    {
            //        isimage = true;
            //    }
            //    else if (iDets[2].ToString() == "TextureTGA")
            //    {
            //        isimage = true;
            //    }
            //    else
            //    {
            //        isimage = false;
            //    }

            //    if (!isimage) return;

            //    SLImageID = (UUID)iDets[3];

            //    props.ProfileImage = SLImageID;

            //    client.Self.UpdateProfile(props);

            //    proSLImage.Visible = true;

            //    if (!instance.ImageCache.ContainsImage(SLImageID))
            //    {
            //        client.Assets.RequestImage(SLImageID, ImageType.Normal, Assets_OnImageReceived);
            //    }
            //    else
            //    {
            //        picSLImage.Image = instance.ImageCache.GetImage(SLImageID);
            //        proSLImage.Visible = false;
            //    }
            //}

            TreeNode node = e.Data.GetData(typeof(TreeNode)) as TreeNode;

            if (node == null) return;

            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                InventoryBase io = (InventoryBase)node.Tag;

                if (node.Tag is InventoryFolder folder)
                {
                    client.Inventory.GiveFolder(folder.UUID, folder.Name, agentID, true);
                    instance.TabConsole.DisplayChatScreen($"Offered inventory folder {folder.Name} to {fullName}.");
                }
                else
                {
                    InventoryItem item = (InventoryItem)io;

                    if (agentID != client.Self.AgentID)
                    {
                        if ((item.Permissions.OwnerMask & PermissionMask.Copy) != PermissionMask.Copy)
                        {
                            DialogResult res = MessageBox.Show("This is a 'no copy' item and you will lose ownership if you continue.", "Warning", MessageBoxButtons.OKCancel);

                            if (res == DialogResult.Cancel) return;
                        }

                        client.Inventory.GiveItem(item.UUID, item.Name, item.AssetType, agentID, true);
                        instance.TabConsole.DisplayChatScreen($"Offered inventory item {item.Name} to {fullName}.");
                    }
                    else
                    {
                        // Change the picture
                        if (item.AssetType is AssetType.ImageJPEG or AssetType.ImageTGA or AssetType.Texture or AssetType.TextureTGA)
                        {
                            SLImageID = item.AssetUUID;

                            props.ProfileImage = SLImageID;

                            client.Self.UpdateProfile(props);

                            proSLImage.Visible = true;

                            if (!instance.ImageCache.ContainsImage(SLImageID))
                            {
                                client.Assets.RequestImage(SLImageID, ImageType.Normal, Assets_OnImageReceived);
                            }
                            else
                            {
                                picSLImage.Image = instance.ImageCache.GetImage(SLImageID);
                                proSLImage.Visible = false;
                            }
                        }
                        else
                        {
                            MessageBox.Show("To change your picture you must drag and drop an image or a texture", "MEGAbolt");
                            return;
                        }
                    }
                }
            }
        }

        private void picSLImage_DragOver(object sender, DragEventArgs e)
        {
            //if (e.Data.GetDataPresent(DataFormats.FileDrop))
            //{
            //    e.Effect = DragDropEffects.Move;
            //}
            //else
            //{
            //    e.Effect = DragDropEffects.None;
            //}

            e.Effect = e.Data.GetDataPresent(typeof(TreeNode)) 
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void picFLImage_DragOver(object sender, DragEventArgs e)
        {
            if (agentID != client.Self.AgentID) return;

            e.Effect = e.Data.GetDataPresent(typeof(TreeNode))
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void picFLImage_DragEnter(object sender, DragEventArgs e)
        {
            if (agentID != client.Self.AgentID) return;

            e.Effect = e.Data.GetDataPresent(typeof(TreeNode)) 
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void picFLImage_DragDrop(object sender, DragEventArgs e)
        {
            if (agentID != client.Self.AgentID) { return; }

            if (e.Data.GetData(typeof(TreeNode)) is not TreeNode node) { return; }

            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                InventoryBase io = (InventoryBase)node.Tag;

                if (node.Tag is InventoryFolder) { return; }

                InventoryItem item = (InventoryItem)io;

                // Change the picture
                if (item.AssetType is AssetType.ImageJPEG or AssetType.ImageTGA or AssetType.Texture or AssetType.TextureTGA)
                {
                    SLImageID = item.AssetUUID;

                    props.ProfileImage = SLImageID;

                    client.Self.UpdateProfile(props);

                    proFLImage.Visible = true;

                    if (!instance.ImageCache.ContainsImage(SLImageID))
                    {
                        client.Assets.RequestImage(SLImageID, ImageType.Normal, Assets_OnImageReceived);
                    }
                    else
                    {
                        picFLImage.Image = instance.ImageCache.GetImage(SLImageID);
                        proFLImage.Visible = false;
                    }
                }
                else
                {
                    MessageBox.Show("To change your picture you must drag and drop an image or a texture", "MEGAbolt");
                    return;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (instance.IsAvatarMuted(agentID, fullName))
            {
                MessageBox.Show($"{fullName} is already in your mute list.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            //DataRow dr = instance.MuteList.NewRow();
            //dr["uuid"] = agentID;
            //dr["mute_name"] = fullName;
            //instance.MuteList.Rows.Add(dr);

            instance.Client.Self.UpdateMuteListEntry(MuteType.Resident, agentID, fullName);

            MessageBox.Show($"{fullName} is now muted.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);      
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Boolean fFound = true;

            client.Friends.FriendList.ForEach(friend =>
            {
                if (friend.Name == fullName)
                {
                    fFound = false;
                }
            });

            if (fFound)
            {
                client.Friends.OfferFriendship(agentID);
            }
        }

        private void lvGroups_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                AvatarGroup group = (AvatarGroup)lvGroups.SelectedItems[0].Tag;

                frmGroupInfo frm = new frmGroupInfo(group, instance);
                frm.Show();
            }
            catch
            {
                ; 
            }
        }

        private void lvGroups_MouseEnter(object sender, EventArgs e)
        {
            lvGroups.Cursor = Cursors.Hand;
        }

        private void lvGroups_MouseLeave(object sender, EventArgs e)
        {
            lvGroups.Cursor = Cursors.Default;  
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (tabConsole.TabExists(fullName))
            {
                tabConsole.SelectTab(fullName);
                return;
            }

            tabConsole.AddIMTab(agentID, client.Self.AgentID ^ agentID, fullName);
            tabConsole.SelectTab(fullName);

            tabConsole.Focus();
        }

        private void lvwPicks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvwPicks.SelectedItems.Count == 0) return;

            UUID pick = (UUID)lvwPicks.SelectedItems[0].Tag;

            loadwait2.Visible = true;

            if (agentID == client.Self.AgentID)
            {
                button11.Enabled = true;
            }

            client.Avatars.RequestPickInfo(agentID, pick);
            txtTitle.Text = string.Empty;
            txtDescription.Text = string.Empty;
            txtSlurl.Text = string.Empty;

            pictureBox1.Image = null;

            parcelname = string.Empty;
            simname = string.Empty;
            //pickUUID = UUID.Zero;  

            posX = 0;
            posY = 0;
            posZ = 0;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(simname))
            {
                Vector3 pos = new Vector3
                {
                    X = posX,
                    Y = posY,
                    Z = posZ
                };

                client.Self.Teleport(simname, pos);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //if (pickUUID == UUID.Zero) return;

            if (lvwPicks.SelectedItems.Count == 0)
            {
                MessageBox.Show("To DELETE a pick you need to select one from the list first", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            UUID pick = (UUID)lvwPicks.SelectedItems[0].Tag;

            client.Self.PickDelete(pick);

            txtTitle.Text = string.Empty;
            txtDescription.Text = string.Empty;
            pictureBox1.Image = null;

            client.Avatars.RequestAvatarPicks(agentID);
        }

        private void txtPartner_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtDisplayName_Leave(object sender, EventArgs e)
        {
            //if (displaynamechanged)
            //{
            //    DialogResult response = MessageBox.Show("You have changed your Display Name to '" + txtDisplayName.Text + "'.\nAre you sure you want to change to this name?", "MEGAbolt", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            //    if (response == DialogResult.OK)
            //    {
            //        client.Self.SetDisplayNameReply += new EventHandler<SetDisplayNameReplyEventArgs>(Self_SetDisplayNameReply);

            //        displaynames.Add(client.Self.AgentID);
            //        client.Avatars.GetDisplayNames(displaynames, DisplayNamesCallBack);
            //        displaynames.Clear();
            //    }
            //}

            //displaynamechanged = false;
        }

        private void Self_SetDisplayNameReply(object sender, SetDisplayNameReplyEventArgs e)
        {
            if (e.Status == 200)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    txtDisplayName.Text = e.DisplayName.DisplayName;
                    button9.Enabled = false;
                }));
            }
            else
            {
                string reason = e.Reason;

                if (reason.Trim().ToLower(CultureInfo.CurrentCulture) == "bad request")
                {
                    MessageBox.Show("Display name could not be set.\nYou can only change your display name once per week!", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Display name could not be set.\nReason: {reason}", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            BeginInvoke(new MethodInvoker(() =>
            {
                pBar3.Visible = false;
            }));

            client.Self.SetDisplayNameReply -= Self_SetDisplayNameReply;
        }

        private void txtDisplayName_TextChanged(object sender, EventArgs e)
        {
            //if (agentID == client.Self.AgentID)
            //{
            //    displaynamechanged = true;
            //}
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string logdir = instance.appdir;   // Application.StartupPath.ToString();
            logdir += "\\Notes\\";

            string filename = fullName;

            rtbNotes.SaveFile(logdir + filename + ".rtf");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            gbDisplayName.Visible = true;
            txtDisplayName.Enabled = false;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (textBox2.Text.Trim() == textBox3.Text.Trim())
            {
                newname = textBox2.Text.Trim();

                displaynames.Add(client.Self.AgentID);
                client.Avatars.GetDisplayNames(displaynames, DisplayNamesCallBack);
                displaynames.Clear();
                pBar3.Visible = true;
            }
            else
            {
                MessageBox.Show("The names you entered do not match. Check your entries and re-try.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                textBox2.Focus();
            }
        }

        private void DisplayNamesCallBack(bool status, AgentDisplayName[] nme, UUID[] badIDs)
        {
            if (status)
            {
                client.Self.SetDisplayNameReply += Self_SetDisplayNameReply;
                client.Self.SetDisplayName(nme[0].DisplayName, newname);
            }
            else
            {
                MessageBox.Show("Could not retrieve old name.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            gbDisplayName.Visible = false;
            txtDisplayName.Enabled = true;
        }

        private void tpgFirstLife_Click(object sender, EventArgs e)
        {

        }

        private void loadwait2_Click(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            UUID pick = UUID.Random();
            UUID pid = client.Parcels.RequestRemoteParcelID(instance.SIMsittingPos(), client.Network.CurrentSim.Handle, client.Network.CurrentSim.ID);

            client.Self.PickInfoUpdate(pick, false, pid, instance.MainForm.parcel.Name, client.Self.GlobalPosition, instance.MainForm.parcel.SnapshotID, instance.MainForm.parcel.Desc);
            client.Avatars.RequestAvatarPicks(agentID);

            button11.Enabled = true;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            UUID pick = (UUID)lvwPicks.SelectedItems[0].Tag;
            UUID pid = client.Parcels.RequestRemoteParcelID(instance.SIMsittingPos(), client.Network.CurrentSim.Handle, client.Network.CurrentSim.ID);

            client.Self.PickInfoUpdate(pick, false, pid, txtTitle.Text.Trim(), client.Self.GlobalPosition, instance.MainForm.parcel.SnapshotID, txtDescription.Text.Trim());
            client.Avatars.RequestAvatarPicks(agentID);

            button11.Enabled = false;
        }
    }
}