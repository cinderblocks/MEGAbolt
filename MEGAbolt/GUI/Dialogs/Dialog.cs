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


namespace MEGAbolt
{
    public partial class frmDialogLoadURL : Form
    {
        private MEGAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;
        private ScriptDialogEventArgs ed;


        public frmDialogLoadURL(MEGAboltInstance instance, ScriptDialogEventArgs e)
        {
            InitializeComponent();

            this.instance = instance;
            client = this.instance.Client;
            ed = e;

            timer1.Interval = instance.DialogTimeOut;
            timer1.Enabled = true;
            timer1.Start();

            Text += "   " + "[ " + client.Self.Name + " ]";
        }

        private void Dialog_Load(object sender, EventArgs e)
        {
            CenterToParent();

            lblTitle.Text = ed.FirstName + "'s " + ed.ObjectName;
            string smsg = ed.Message;
            //txtMessage.Text = smsg;

            char[] deli = "\n".ToCharArray();
            string[] sGrp = smsg.Split(deli);
            txtMessage.Lines = sGrp;
            //label2.Text = smsg;  

            List<string> btns = ed.ButtonLabels;

            int count = btns.Count;

            if (btns.Count == 1 && btns[0] == "!!llTextBox!!")
            {
                txtMessage.ReadOnly = false;
                button1.Visible = true; 
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    //cboReply.Items.Add(i.ToString(CultureInfo.CurrentCulture) + "-" + btns[i]);
                    cboReply.Items.Add(btns[i]);

                    ToolStripSeparator sep = new ToolStripSeparator();

                    tsButtons.Items.Add(sep);

                    ToolStripButton btn = new ToolStripButton();
                    btn.Click += AnyMenuItem_Click;
                    btn.Text = btns[i];

                    tsButtons.Items.Add(btn);

                    //btn.Dispose();
                }
            }

            if (instance.DialogCount == 7)
            {
                button3.Visible = true;
                BackColor = Color.Red;  
            }
            else
            {
                button3.Visible = false;
                BackColor = ColorTranslator.FromHtml(ColorTranslator.ToHtml(Color.FromArgb(64,64,64)));   //  Color.White;
            }
        }

        private void AnyMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripItem mitem = (ToolStripItem)sender;

            //cboReply.Text = mitem.Text;

            int butindex = cboReply.SelectedIndex = cboReply.FindStringExact(mitem.Text);   //(int)sGrp[2];
            string butlabel = mitem.Text;   // sGrp[1];

            client.Self.ReplyToScriptDialog(ed.Channel, butindex, butlabel, ed.ObjectID);

            CleanUp();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CleanUp();
        }

        private void CleanUp()
        {
            instance.DialogCount -= 1;
            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            instance.DialogCount = 0;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (instance.IsObjectMuted(ed.ObjectID, ed.ObjectName))
            {
                MessageBox.Show(ed.ObjectName + " is already in your mute list.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            //DataRow dr = instance.MuteList.NewRow();
            //dr["uuid"] = ed.ObjectID.ToString();
            //dr["mute_name"] = ed.ObjectName;
            //instance.MuteList.Rows.Add(dr);

            instance.Client.Self.UpdateMuteListEntry(MuteType.Object, ed.ObjectID, ed.ObjectName);

            MessageBox.Show(ed.ObjectName + " is now muted.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            button2.PerformClick();  
        }

        private void txtMessage_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            instance.Client.Self.ReplyToScriptDialog(ed.Channel, 0, txtMessage.Text, ed.ObjectID);
            CleanUp();
        }

        private void frmDialog_MouseEnter(object sender, EventArgs e)
        {
            Opacity = 100;
        }

        private void frmDialog_MouseLeave(object sender, EventArgs e)
        {
            Opacity = 75;
        }
    }
}
