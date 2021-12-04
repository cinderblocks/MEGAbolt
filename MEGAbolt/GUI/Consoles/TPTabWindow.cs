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
using System.Windows.Forms;
using OpenMetaverse;
using MEGAbolt.NetworkComm;
using System.Threading;
using System.Globalization;

namespace MEGAbolt
{
    public partial class TPTabWindow : UserControl
    {
        private MEGAboltInstance instance;
        private MEGAboltNetcom netcom;
        private GridClient client;
        private UUID targetUUID = UUID.Zero;
        private ManualResetEvent TPEvent = new ManualResetEvent(false);
        private UUID targetSession = UUID.Zero;  

        public TPTabWindow(MEGAboltInstance instance, InstantMessageEventArgs e)
        {
            InitializeComponent();

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;

            Disposed += TPTabWindow_Disposed;

            ProcessEventArgs(e);

            netcom.TeleportStatusChanged += netcom_TeleportStatusChanged;
        }

        private void TPTabWindow_Disposed(object sender, EventArgs e)
        {
            netcom.TeleportStatusChanged -= netcom_TeleportStatusChanged;
        }

        private void netcom_TeleportStatusChanged(object sender, TeleportEventArgs e)
        { 
            switch (e.Status)
            {
                case TeleportStatus.Failed:
                    MessageBox.Show(e.Message, "Teleport", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case TeleportStatus.Finished:
                    //try
                    //{
                    //    client.Appearance.SetPreviousAppearance(false);
                    //}
                    //catch (Exception exp)
                    //{
                    //    Logger.Log("TPTabWindow: " + exp.InnerException.ToString(), Helpers.LogLevel.Error);
                    //}
                    break;
            }
        }

        private void ProcessEventArgs(InstantMessageEventArgs e)
        {
            TargetName = e.IM.FromAgentName;
            targetUUID = e.IM.FromAgentID;
            targetSession = e.IM.IMSessionID; 

            lblSubheading.Text =
                "Received teleport offer from " + TargetName + " with message:";

            rtbOfferMessage.AppendText(e.IM.Message);
        }

        public void CloseTab()
        {
            instance.TabConsole.GetTab("chat").Select();
            instance.TabConsole.GetTab(targetUUID.ToString()).Close();
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            if (instance.State.IsSitting)
            {
                client.Self.Stand();
                instance.State.SetStanding();
                TPEvent.WaitOne(2000, false);
            }

            client.Self.TeleportLureRespond(targetUUID, targetSession, true);
            CloseTab();
        }

        private void btnDecline_Click(object sender, EventArgs e)
        {
            client.Self.TeleportLureRespond(targetUUID, UUID.Random(), false);
            CloseTab();
        }

        public string TargetName { get; private set; }

        public UUID TargetUUID => targetUUID;

        private void TPTabWindow_Load(object sender, EventArgs e)
        {

        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {

        }

        private void rtbOfferMessage_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (e.LinkText.StartsWith("http://slurl.", StringComparison.CurrentCultureIgnoreCase))
            {
                // Open up the TP form here
                string encoded = System.Web.HttpUtility.UrlDecode(e.LinkText);
                string[] split = encoded.Split(new Char[] { '/' });
                //string[] split = e.LinkText.Split(new Char[] { '/' });
                string sim = split[4].ToString();
                double x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                double y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                double z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                (new frmTeleport(instance, sim, (float)x, (float)y, (float)z, false)).Show();

            }
            else if (e.LinkText.StartsWith("http://maps.secondlife", StringComparison.CurrentCultureIgnoreCase))
            {
                // Open up the TP form here
                string encoded = System.Web.HttpUtility.UrlDecode(e.LinkText);
                string[] split = encoded.Split(new Char[] { '/' });
                //string[] split = e.LinkText.Split(new Char[] { '/' });
                string sim = split[4].ToString();
                double x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                double y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                double z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                (new frmTeleport(instance, sim, (float)x, (float)y, (float)z, true)).Show();

            }
            else if (e.LinkText.Contains("http://mbprofile:"))
            {
                string encoded = System.Web.HttpUtility.UrlDecode(e.LinkText);
                string[] split = encoded.Split(new Char[] { '/' });
                //string[] split = e.LinkText.Split(new Char[] { '#' });
                string aavname = split[0].ToString();
                string[] avnamesplit = aavname.Split(new Char[] { '#' });
                aavname = avnamesplit[0].ToString();

                split = e.LinkText.Split(new Char[] { ':' });
                string elink = split[2].ToString();
                split = elink.Split(new Char[] { '&' });

                UUID avid = (UUID)split[0].ToString();

                (new frmProfile(instance, aavname, avid)).Show();
            }
            //else if (e.LinkText.Contains("secondlife:///"))
            //{
            //    // Open up the Group Info form here
            //    //string[] split = e.LinkText.Split(new Char[] { '/' });
            //    //UUID uuid = (UUID)split[4].ToString();

            //    //frmGroupInfo frm = new frmGroupInfo(uuid, instance);
            //    //frm.Show();
            //}
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
