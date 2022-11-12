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
using System.Data;
using System.Windows.Forms;
using MEGAbolt.Controls;
using OpenMetaverse;

namespace MEGAbolt
{
    public partial class PrefMEGAgiver : UserControl, IPreferencePane
    {
        private MEGAboltInstance instance;
        //private ConfigManager config;
        private Popup toolTip;
        private CustomToolTip customToolTip;
        //private GridClient client;
        //private bool isloading = true;

        public PrefMEGAgiver(MEGAboltInstance instance)
        {
            InitializeComponent();

            string msg = "To delete an entry, select the whole row by clicking the arrow on the left of the row and hit the DEL button on your keyboard";
            toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg));
            toolTip.AutoClose = false;
            toolTip.FocusOnOpen = false;
            toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;

            this.instance = instance;
            //client = this.instance.Client;
            //config = this.instance.Config;

            GW.DataSource = instance.GiverItems;

            //isloading = false;
        }

        private void picAI_Click(object sender, EventArgs e)
        {

        }

        private void PrefAI_Load(object sender, EventArgs e)
        {

        }

        #region IPreferencePane Members

        string IPreferencePane.Name => "  MEGAcourier";

        Image IPreferencePane.Icon => Properties.Resources.top_hat;

        void IPreferencePane.SetPreferences()
        {
            //instance.Config.CurrentConfig.DisableMipmaps = chkAI.Checked;
        }

        #endregion

        private void picHelp_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show(picHelp);
        }

        private void picHelp_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Close();
        }

        private void textBox2_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode node = e.Data.GetData(typeof(TreeNode)) as TreeNode;

            if (node == null) return;

            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                InventoryBase io = (InventoryBase)node.Tag;

                if (node.Tag is InventoryFolder)
                {
                    // Folder are not supported
                    MessageBox.Show("Folders are not supported. You can only enter inventory items.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    InventoryItem item = (InventoryItem)io;

                    if ((item.Permissions.OwnerMask & PermissionMask.Copy) != PermissionMask.Copy)
                    {
                        DialogResult res = MessageBox.Show("This is a 'no copy' item and you will lose ownership if you continue.", "Warning", MessageBoxButtons.OKCancel);

                        if (res == DialogResult.Cancel) return;
                    }

                    if (string.IsNullOrEmpty(textBox1.Text))
                    {
                        MessageBox.Show("Command cannot be empty. Enter a UNIQUE command first.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }

                    if (instance.GiverItems.Rows.Contains(textBox1.Text))
                    {
                        MessageBox.Show(textBox1.Text + " command is already in your list of items.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }

                    InventoryItem iitem = (InventoryItem)io;

                    DataRow dr = instance.GiverItems.NewRow();
                    dr["Command"] = textBox1.Text.Trim();
                    dr["UUID"] = iitem.UUID;
                    dr["Name"] = iitem.Name;
                    dr["AssetType"] = iitem.AssetType;

                    instance.GiverItems.Rows.Add(dr);

                    textBox1.Text = string.Empty;   

                    GW.Refresh();
                }
            }
        }

        private void textBox2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(TreeNode)) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void textBox2_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(TreeNode)) ? DragDropEffects.Copy : DragDropEffects.None;
        }
    }
}
