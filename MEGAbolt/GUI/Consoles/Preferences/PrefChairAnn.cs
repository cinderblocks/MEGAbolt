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
using MEGAbolt.Controls;
using OpenMetaverse;
using System.Globalization;

namespace MEGAbolt
{
    public partial class PrefChairAnn : UserControl, IPreferencePane
    {
        private MEGAboltInstance instance;
        private ConfigManager config;
        private Popup toolTip1;
        private CustomToolTip customToolTip;

        public PrefChairAnn(MEGAboltInstance instance)
        {
            InitializeComponent();

            this.instance = instance;
            config = this.instance.Config;

            string msg2 = "Send messages to the Group UUIDs entered below, blank for no group. You can copy the UUID for a group you belong to from the Group window.";
            toolTip1 = new Popup(customToolTip = new CustomToolTip(instance, msg2));
            toolTip1.AutoClose = false;
            toolTip1.FocusOnOpen = false;
            toolTip1.ShowingAnimation = toolTip1.HidingAnimation = PopupAnimations.Blend;

            textBox1.Text = config.CurrentConfig.ChairAnnouncerUUID.ToString();
            textBox2.Text = config.CurrentConfig.ChairAnnouncerInterval.ToString(CultureInfo.CurrentCulture);

            checkBox1.Enabled = true;
            checkBox1.Checked = config.CurrentConfig.ChairAnnouncerEnabled;
            checkBox2.Checked = config.CurrentConfig.ChairAnnouncerChat;

            textBox3.Text = config.CurrentConfig.ChairAnnouncerGroup1.ToString();
            textBox4.Text = config.CurrentConfig.ChairAnnouncerGroup2.ToString();
            textBox5.Text = config.CurrentConfig.ChairAnnouncerGroup3.ToString();
            textBox6.Text = config.CurrentConfig.ChairAnnouncerGroup4.ToString();
            textBox7.Text = config.CurrentConfig.ChairAnnouncerGroup5.ToString();
            textBox8.Text = config.CurrentConfig.ChairAnnouncerGroup6.ToString();
            //added by GM on 1-APR-2010
            textBox9.Text = config.CurrentConfig.ChairAnnouncerAdvert;
        }

        #region IPreferencePane Members

        string IPreferencePane.Name => " Chair Announcer";

        Image IPreferencePane.Icon => Properties.Resources.ChairAnn;

        void IPreferencePane.SetPreferences()
        {
            
            config.CurrentConfig.ChairAnnouncerUUID = UUID.Parse(textBox1.Text);
            config.CurrentConfig.ChairAnnouncerInterval = Convert.ToInt32(textBox2.Text, CultureInfo.CurrentCulture);
            config.CurrentConfig.ChairAnnouncerEnabled = checkBox1.Checked;
            config.CurrentConfig.ChairAnnouncerChat = checkBox2.Checked;
            config.CurrentConfig.ChairAnnouncerGroup1 = UUID.Parse(textBox3.Text);
            config.CurrentConfig.ChairAnnouncerGroup2 = UUID.Parse(textBox4.Text);
            config.CurrentConfig.ChairAnnouncerGroup3 = UUID.Parse(textBox5.Text);
            config.CurrentConfig.ChairAnnouncerGroup4 = UUID.Parse(textBox6.Text);
            config.CurrentConfig.ChairAnnouncerGroup5 = UUID.Parse(textBox7.Text);
            config.CurrentConfig.ChairAnnouncerGroup6 = UUID.Parse(textBox8.Text);
            config.CurrentConfig.ChairAnnouncerAdvert = textBox9.Text;
            
        }

        #endregion

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = textBox2.Enabled = 
            textBox3.Enabled = textBox4.Enabled =
            textBox5.Enabled = textBox6.Enabled =
            textBox7.Enabled = textBox8.Enabled =
            //added by GM on 1-APR-2009
            textBox9.Enabled =
            checkBox2.Enabled = checkBox1.Checked;
        }

        private void pictureBox2_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show(pictureBox2);
        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            toolTip1.Close();
        }
    }
}
