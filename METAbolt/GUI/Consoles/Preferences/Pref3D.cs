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
using MEGAbolt.Controls;

namespace METAbolt
{
    public partial class Pref3D : System.Windows.Forms.UserControl, IPreferencePane
    {
        private METAboltInstance instance;
        private ConfigManager config;
        private Popup toolTip3;
        private CustomToolTip customToolTip;
        //private GridClient client;
        private bool isloading = true;

        public Pref3D(METAboltInstance instance)
        {
            InitializeComponent();

            string msg4 = "If MEGA3D is not rendering textures and displaying them as WHITE surfaces, then disable mipmaps";
            toolTip3 = new Popup(customToolTip = new CustomToolTip(instance, msg4));
            toolTip3.AutoClose = false;
            toolTip3.FocusOnOpen = false;
            toolTip3.ShowingAnimation = toolTip3.HidingAnimation = PopupAnimations.Blend;

            this.instance = instance;
            //client = this.instance.Client;
            config = this.instance.Config;

            chkAI.Checked = config.CurrentConfig.DisableMipmaps;

            isloading = false;
        }

        private void picAI_Click(object sender, EventArgs e)
        {

        }

        private void PrefAI_Load(object sender, EventArgs e)
        {

        }

        #region IPreferencePane Members

        string IPreferencePane.Name => "  MEGA3D";

        Image IPreferencePane.Icon => Properties.Resources._3d;

        void IPreferencePane.SetPreferences()
        {
            instance.Config.CurrentConfig.DisableMipmaps = chkAI.Checked;
        }

        #endregion

        private void picAI_MouseHover(object sender, EventArgs e)
        {
            toolTip3.Show(picAI);
        }

        private void picAI_MouseLeave(object sender, EventArgs e)
        {
            toolTip3.Close();
        }

        private void chkAI_CheckedChanged(object sender, EventArgs e)
        {
            if (isloading)
                return; 
        }
    }
}
