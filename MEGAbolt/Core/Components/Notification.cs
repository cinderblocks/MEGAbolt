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

namespace MEGAbolt
{
    public partial class Notification : DifuseForm
    {
        //private string msg = string.Empty;

        public Notification()
            : base(true)
        {
            InitializeComponent();
        }

        private void Notification_Load(object sender, EventArgs e)
        {
            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            Left = screenWidth - Width - 5;
            Top = screenHeight - Height - 5;

            timer1.Enabled = true;
            timer1.Start();

            label1.Text = Message;
            Text = Title;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Close();
        }

        //public void ShowMessage(string msg)
        //{
        //    //this.msg = msg;
        //}

        public string Message { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        //public void NotifTitle(string title)
        //{
        //    this.Text = title;
        //}

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
