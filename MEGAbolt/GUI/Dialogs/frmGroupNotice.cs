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
using OpenMetaverse;
using OpenMetaverse.Assets;
using System.Web;
using System.Globalization;
using OpenJpegDotNet.IO;

namespace MEGAbolt
{
    public partial class frmGroupNotice : Form
    {
        private MEGAboltInstance instance;
        private GridClient client;
        private InstantMessage imsg;
        private UUID assetfolder = UUID.Zero;
        private AssetType assettype;
        private string filename = string.Empty;
        Group profile;

        public frmGroupNotice(MEGAboltInstance instance, InstantMessageEventArgs e)
        {
            InitializeComponent();
            this.instance = instance;
            client = this.instance.Client;
            imsg = e.IM;

            Disposed += GroupNotice_Disposed;

            Text += "   " + "[ " + client.Self.Name + " ]";
        }

        private void GroupNotice_Disposed(object sender, EventArgs e)
        {
            
        }

        private void frmGroupNotice_Load(object sender, EventArgs e)
        {
            CenterToParent();

            if (instance.Config.CurrentConfig.PlayGroupNoticeReceived)
            {
                instance.MediaManager.PlayUISound(Properties.Resources.Group_Notice);
            }

            PrepareGroupNotice();

            timer1.Interval = instance.DialogTimeOut;
            timer1.Enabled = true;
            timer1.Start();
        }

        private void PrepareGroupNotice()
        {
            //UUID fromAgentID = imsg.FromAgentID;
            UUID fromAgentID = new UUID(imsg.BinaryBucket, 2);

            if (instance.State.Groups.ContainsKey(fromAgentID))
            {
                profile = instance.State.Groups[fromAgentID];

                label2.Text = $"Sent by: {imsg.FromAgentName}, {profile.Name}";

                if (profile.InsigniaID != UUID.Zero)
                {
                    // Request insignia
                    client.Assets.RequestImage(profile.InsigniaID, ImageType.Normal, Assets_OnImageReceived);
                }
            }
            else
            {
                label2.Text = $"Sent by: {imsg.FromAgentName}";
            }

            int rep = imsg.Message.IndexOf('|');
            string msgtitle = imsg.Message.Substring(0, rep);
            rtbTitle.Text = msgtitle;
            MakeBold(msgtitle, 0, FontStyle.Bold);

            DateTime dte = DateTime.Now;

            dte = instance.State.GetTimeStamp(dte);

            if (instance.Config.CurrentConfig.UseSLT)
            {
                string _timeZoneId = "Pacific Standard Time";
                DateTime startTime = DateTime.UtcNow;
                TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                dte = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
            }

            string atext = "\n" + dte.DayOfWeek + "," + dte;
            rtbTitle.AppendText(atext);
            MakeBold(atext, (msgtitle.Length + 1), FontStyle.Regular);

            string msgbody = imsg.Message.Substring(rep + 1);
            rtbBody.Text = msgbody;

            try
            {
                // Check for attachment
                if (imsg.BinaryBucket[0] != 0)
                {
                    assettype = (AssetType)imsg.BinaryBucket[1];

                    assetfolder = client.Inventory.FindFolderForType(assettype);

                    filename = imsg.BinaryBucket.Length > 18 
                        ? Utils.BytesToString(imsg.BinaryBucket, 18, imsg.BinaryBucket.Length - 19) 
                        : string.Empty;

                    panel1.Visible = true;
                    label4.Visible = true;
                    label3.Text = filename;

                    if (filename.Length > label3.Size.Width)
                    {
                        label3.Text = filename.Substring(0, label3.Size.Width - 3) + "...";
                    }

                    switch (assettype)
                    {
                        case AssetType.Notecard:
                            pictureBox1.Image = Properties.Resources.documents_16;
                            break;
                        case AssetType.LSLText:
                            pictureBox1.Image = Properties.Resources.lsl_scripts_16;
                            break;
                        case AssetType.Landmark:
                            pictureBox1.Image = Properties.Resources.lm;
                            break;
                        case AssetType.Texture:
                            pictureBox1.Image = Properties.Resources.texture;
                            break;
                        case AssetType.Clothing:
                            pictureBox1.Image = Properties.Resources.wear;
                            break;
                        case AssetType.Object:
                            pictureBox1.Image = Properties.Resources.objects;
                            break;
                        default:
                            pictureBox1.Image = Properties.Resources.applications_16;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Group notice attachment error: " + ex.Message, Helpers.LogLevel.Error);     
            }
        }

        private void MakeBold(string otext, int start, FontStyle bold)
        {
            Font nFont = new Font("Microsoft Sans Serif", 10, bold);
            rtbTitle.Select(start, otext.Length);
            rtbTitle.SelectionFont = nFont;

            nFont.Dispose();

            rtbTitle.Select(0, 0);
        }

        void Assets_OnImageReceived(TextureRequestState image, AssetTexture texture)
        {
            if (texture.AssetID != profile.InsigniaID) { return; }

            try
            {
                using var reader = new Reader(texture.AssetData);
                reader.ReadHeader();
                Image bitmap = reader.DecodeToBitmap();

                BeginInvoke(new MethodInvoker(() =>
                {
                    picInsignia.Image = bitmap;
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "MEGAbolt");   
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmGroupNotice_FormClosing(object sender, FormClosingEventArgs e)
        {
            instance.NoticeCount -= 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            client.Self.InstantMessage(client.Self.Name, imsg.FromAgentID, string.Empty, imsg.IMSessionID, InstantMessageDialog.GroupNoticeInventoryAccepted, InstantMessageOnline.Offline, instance.SIMsittingPos(), client.Network.CurrentSim.RegionID, assetfolder.GetBytes());
            button1.Enabled = false;

            MessageBox.Show("Attachment has been saved to your inventory", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //if (assettype != AssetType.Notecard && assettype != AssetType.LSLText)
            //{
            //    MessageBox.Show("Attachment has been saved to your inventory", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //else
            //{
            //    List<InventoryBase> contents = client.Inventory.FolderContents(assetfolder, client.Self.AgentID, false, true, InventorySortOrder.ByName | InventorySortOrder.ByDate, 5000);

            //    if (contents != null)
            //    {
            //        foreach (InventoryBase ibase in contents)
            //        {
            //            if (ibase is InventoryItem)
            //            {
            //                if (ibase.Name.ToLower() == filename.ToLower())
            //                {
            //                    //UUID itemid = item.AssetUUID;
            //                    InventoryItem item = (InventoryItem)ibase;

            //                    switch (assettype)
            //                    {
            //                        case AssetType.Notecard:
            //                            (new frmNotecardEditor(instance, item)).Show();
            //                            break;
            //                        case AssetType.LSLText:
            //                            (new frmScriptEditor(instance, item)).Show();
            //                            break;
            //                    }

            //                    return;
            //                }
            //            }
            //        }
            //    }
            //}
        }

        static IEnumerable<T> ReverseIterator<T>(IList<T> list)
        {
            int count = list.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                yield return list[i];
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Close();
        }

        private void frmGroupNotice_MouseEnter(object sender, EventArgs e)
        {
            Opacity = 100;
        }

        private void frmGroupNotice_MouseLeave(object sender, EventArgs e)
        {
            Opacity = 75;
        }

        private void rtbBody_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (e.LinkText.StartsWith("http://slurl.", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    // Open up the TP form here
                    string encoded = HttpUtility.UrlDecode(e.LinkText);
                    string[] split = encoded.Split(new Char[] { '/' });
                    //string[] split = e.LinkText.Split(new Char[] { '/' });
                    string sim = split[4];
                    double x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                    (new frmTeleport(instance, sim, (float)x, (float)y, (float)z, false)).Show();
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
                    string sim = split[4];
                    double x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                    (new frmTeleport(instance, sim, (float)x, (float)y, (float)z, true)).Show();
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
                    string aavname = split[0];
                    string[] avnamesplit = aavname.Split(new Char[] { '#' });
                    aavname = avnamesplit[0];

                    split = e.LinkText.Split(new Char[] { ':' });
                    string elink = split[2];
                    split = elink.Split(new Char[] { '&' });

                    UUID avid = (UUID)split[0];

                    (new frmProfile(instance, aavname, avid)).Show();
                }
                catch { ; }
            }
            else if (e.LinkText.Contains("http://secondlife:///"))
            {
                // Open up the Group Info form here
                string encoded = HttpUtility.UrlDecode(e.LinkText);
                string[] split = encoded.Split(new Char[] { '/' });
                //string[] split = e.LinkText.Split(new Char[] { '/' });
                UUID uuid = (UUID)split[7];

                if (uuid != UUID.Zero && split[6].ToLower(CultureInfo.CurrentCulture) == "group")
                {
                    frmGroupInfo frm = new frmGroupInfo(uuid, instance);
                    frm.Show();
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
    }
}
