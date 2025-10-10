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

using MEGAbolt.NetworkComm;
using OpenMetaverse;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace MEGAbolt
{
    public partial class TPTabWindow : UserControl
    {
        private readonly MEGAboltInstance instance;
        private readonly MEGAboltNetcom netcom;
        private readonly GridClient client;
        private UUID targetUUID = UUID.Zero;
        private readonly ManualResetEvent TPEvent = new ManualResetEvent(false);
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
                $"Received teleport offer from {TargetName} with message:";

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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
                var encoded = System.Web.HttpUtility.UrlDecode(e.LinkText);
                var split = encoded.Split(new Char[] { '/' });
                //string[] split = e.LinkText.Split(new Char[] { '/' });
                var sim = split[4];
                var x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                var y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                var z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                (new frmTeleport(instance, sim, (float)x, (float)y, (float)z, false)).Show();

            }
            else if (e.LinkText.StartsWith("http://maps.secondlife", StringComparison.CurrentCultureIgnoreCase))
            {
                // Open up the TP form here
                var encoded = System.Web.HttpUtility.UrlDecode(e.LinkText);
                var split = encoded.Split(new Char[] { '/' });
                //string[] split = e.LinkText.Split(new Char[] { '/' });
                var sim = split[4];
                var x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                var y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                var z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                (new frmTeleport(instance, sim, (float)x, (float)y, (float)z, true)).Show();

            }
            else if (e.LinkText.Contains("http://mbprofile:"))
            {
                var encoded = System.Web.HttpUtility.UrlDecode(e.LinkText);
                var split = encoded.Split(new Char[] { '/' });
                //string[] split = e.LinkText.Split(new Char[] { '#' });
                var aavname = split[0];
                var avnamesplit = aavname.Split(new Char[] { '#' });
                aavname = avnamesplit[0];

                split = e.LinkText.Split(new Char[] { ':' });
                var elink = split[2];
                split = elink.Split(new Char[] { '&' });

                var avid = (UUID)split[0];

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
