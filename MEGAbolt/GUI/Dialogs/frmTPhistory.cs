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
using MEGAbolt.NetworkComm;
using OpenMetaverse;
using MEGAbolt.Controls;
using System.Globalization;

namespace MEGAbolt
{
    public partial class frmTPhistory : Form
    {
        //string XmlFile = "TP_History.xml";
        private MEGAboltInstance instance;
        private MEGAboltNetcom netcom;
        private GridClient client;

        private Popup toolTip;
        private CustomToolTip customToolTip;

        public frmTPhistory(MEGAboltInstance instance)
        {
            InitializeComponent();
            this.instance = instance;
            client = this.instance.Client;
            netcom = this.instance.Netcom;

            dataGridView1.DataSource = instance.TP;
            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader);

            string msg1 = "To delete a history record, select the whole row by clicking the arrow on the left of the row and hit the DEL button on your keyboard";
            toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            toolTip.AutoClose = false;
            toolTip.FocusOnOpen = false;
            toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                int cnt = dataGridView1.Rows.GetRowCount(DataGridViewElementStates.Selected);

                if (cnt > 0)
                {
                    int ind = dataGridView1.SelectedRows[0].Index; 

                    button2.Enabled = true;
                    button4.Enabled = true;
                    textBox1.Text = dataGridView1.Rows[ind].Cells[1].Value.ToString();
                    textBox2.Text = dataGridView1.Rows[ind].Cells[2].Value.ToString();
                }
                else
                {
                    button2.Enabled = false;
                    button4.Enabled = false;
                    textBox1.Text = string.Empty;
                    textBox2.Text = string.Empty;
                }
            }
            catch
            {
                ; 
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();   
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox2.Text))
            {
                // Open up the TP form here
                string[] split = textBox2.Text.Split(new Char[] { '/' });
                string sim = split[4];
                double x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                double y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                double z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                //(new frmTeleport(instance, sim, (float)x, (float)y, (float)z)).ShowDialog();

                netcom.Teleport(sim.Trim(), new Vector3((float)x, (float)y, (float)z));
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string file = textBox1.Text;

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
                        else
                        {
                            MessageBox.Show("The location has been successfully saved as a \nLandmark in your 'Landmarks' folder.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                );  
        }

        private void frmTPhistory_Load(object sender, EventArgs e)
        {
            CenterToParent();
        }

        private void picHelp_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show(picHelp);
        }

        private void picHelp_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Close();
        }
    }
}
