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
using System.Globalization;

namespace MEGAbolt
{
    public partial class frmDisconnected : Form
    {
        private MEGAboltInstance instance;
        private string rea = string.Empty;  

        public frmDisconnected(MEGAboltInstance instance, string reason)
        {
            InitializeComponent();

            this.instance = instance;

            rea = reason;

            Text += "   " + "[ " + instance.Client.Self.Name + " ]";
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            instance.MainForm.Close();
        }

        private void frmDisconnected_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void frmDisconnected_Load(object sender, EventArgs e)
        {
            lblMessage.Text = rea;
            button1.Visible = true;
            if (instance.State.UnReadIMs > 0)
            {
                label2.Visible = true;
                label2.Text = "You have " + instance.State.UnReadIMs.ToString(CultureInfo.CurrentCulture) + " unread IMs";
            }
            else
            {
                label2.Visible = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            instance.ReadIMs = true; 
            Close(); 
        }

        private void frmDisconnected_MouseEnter(object sender, EventArgs e)
        {
            Opacity = 100;
        }

        private void frmDisconnected_MouseLeave(object sender, EventArgs e)
        {
            Opacity = 75;
        }
    }
}