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
using System.Drawing;
using System.Windows.Forms;
using OpenMetaverse;

namespace MEGAbolt
{
    public partial class frmMutes : Form
    {
        //private Popup toolTip;
        //private CustomToolTip customToolTip;

        private MEGAboltInstance instance;

        public frmMutes(MEGAboltInstance instance)
        {
            InitializeComponent();

            this.instance = instance;

            //GW.DataSource = instance.MuteList;

            //string msg1 = "To un-mute, select the whole row by clicking the arrow on the left of the row and hit the DEL button on your keyboard";
            //toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            //toolTip.AutoClose = false;
            //toolTip.FocusOnOpen = false;
            //toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;

            instance.Client.Self.MuteListUpdated += MuteListUpdated;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //instance.MuteList = GW.DataSource;
            Close();
        }

        private void frmMutes_Load(object sender, EventArgs e)
        {
            CenterToParent();

            lvMutes.Columns[0].Width = lvMutes.Width - 25 - 60;
            LoadMutes();
        }

        private void LoadMutes()
        {
            lvMutes.BeginUpdate();
            lvMutes.Items.Clear();

            int cnt = 0;

            instance.Client.Self.MuteList.ForEach((MuteEntry entry) =>
            {
                //string mutetype = string.Empty;

                ListViewItem item = new ListViewItem(entry.Name)
                {
                    Tag = entry
                };

                if (cnt == 1)
                {
                    item.BackColor = Color.GhostWhite; 
                    cnt = 0;
                }

                item.SubItems.Add(entry.Type.ToString());  

                lvMutes.Items.Add(item);

                cnt = 1;
            }
            );

            lvMutes.EndUpdate();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MuteEntry entry = (MuteEntry)lvMutes.SelectedItems[0].Tag;

            instance.Client.Self.RemoveMuteListEntry(entry.ID, entry.Name);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //LoadMutes();
            instance.Client.Self.RequestMuteList();
        }

        private void lvMutes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvMutes.SelectedItems.Count == 0)
            {
                button2.Enabled = false;
            }
            else
            {
                button2.Enabled = true;
            }
        }

        private void frmMutes_FormClosing(object sender, FormClosingEventArgs e)
        {
            instance.Client.Self.MuteListUpdated -= MuteListUpdated;
        }

        private void MuteListUpdated(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(new MethodInvoker(() => MuteListUpdated(sender, e)));
                }
                return;
            }

            LoadMutes();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            (new frmObjects(instance)).Show();
        }
    }
}
