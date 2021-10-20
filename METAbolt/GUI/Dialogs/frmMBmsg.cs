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

namespace METAbolt
{
    public partial class frmMBmsg : Form
    {
        private METAboltInstance instance;
        //private string meg = string.Empty;

        public frmMBmsg(METAboltInstance instance, string msg)
        {
            InitializeComponent();

            this.instance = instance;

            txtMsg.Text = @msg;

            Text += "   " + "[ " + instance.Client.Self.Name + " ]";

            timer1.Enabled = true;
            timer1.Start();  
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmMBmsg_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void frmMBmsg_Load(object sender, EventArgs e)
        {
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            timer1.Stop();
            Close(); 
        }

        private void frmMBmsg_MouseEnter(object sender, EventArgs e)
        {
            Opacity = 100;
        }

        private void frmMBmsg_MouseLeave(object sender, EventArgs e)
        {
            Opacity = 75;
        }
    }
}