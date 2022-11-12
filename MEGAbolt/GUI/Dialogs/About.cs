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
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;


namespace MEGAbolt
{
    public partial class frmAbout : Form
    {
        private System.Timers.Timer scrollTimer;
        private int charCount = 0;
        private int row = 1;
        private bool stopscroll = false;


        public frmAbout()
        {
            InitializeComponent();

            lblVersion.Text = $"{Assembly.GetExecutingAssembly().GetName().Name} " +
                              $"v{Assembly.GetExecutingAssembly().GetName().Version}";   
            txtDir.Text =  Application.StartupPath;
            textBox1.Text = DataFolder.GetDataFolder();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmAbout_Load(object sender, EventArgs e)
        {
            CenterToParent();

            Thread thread = new Thread(ScrollRTB);
            thread.Start();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Utilities.OpenBrowser("https://metabolt.radegast.life/");
        }

        private void lnkWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Utilities.OpenBrowser("https://metabolt.radegast.life/help/");
        }

        private void lblVersion_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", txtDir.Text);
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", textBox1.Text);
        }

        private void ScrollRTB()
        {
            scrollTimer = new System.Timers.Timer(1000);
            scrollTimer.Enabled = true;
            //scrollTimer.SynchronizingObject = this;
            scrollTimer.Elapsed += ScrollLine;
            scrollTimer.Start(); 
        }

        private void ScrollLine(object sender, ElapsedEventArgs e)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                if (stopscroll) return;

                string line = richTextBox1.Lines[row - 1];

                charCount += line.Length + 1;

                row++;

                richTextBox1.SelectionStart = charCount;

                if (row == richTextBox1.Lines.Length + 1)
                {
                    //set the caret here
                    charCount = 0;
                    row = 1;
                    richTextBox1.SelectionStart = 0;
                }
            }));
        }

        private void frmAbout_FormClosing(object sender, FormClosingEventArgs e)
        {
            scrollTimer.Stop();
            scrollTimer.Enabled = false;
            scrollTimer.Dispose(); 
        }

        private void richTextBox1_MouseEnter(object sender, EventArgs e)
        {
            stopscroll = true;
        }

        private void richTextBox1_MouseLeave(object sender, EventArgs e)
        {
            stopscroll = false;
        }
    }
}