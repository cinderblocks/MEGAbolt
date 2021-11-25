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

namespace MEGAbolt
{
    public partial class PrefParcelMusic : UserControl, IPreferencePane
    {
        private MEGAboltInstance instance;
        private ConfigManager config;

        public PrefParcelMusic(MEGAboltInstance instance)
        {
            InitializeComponent();

            this.instance = instance;
            config = this.instance.Config;

            chkParcelMusic.Checked = config.CurrentConfig.ParcelMusic;
        }

        #region IPreferencePane Members

        string IPreferencePane.Name => "Music";

        Image IPreferencePane.Icon => Properties.Resources.wmp;

        void IPreferencePane.SetPreferences()
        {
            config.CurrentConfig.ParcelMusic = chkParcelMusic.Checked;
        }

        #endregion

        private void PrefParcelMusic_Load(object sender, EventArgs e)
        {

        }
    }
}
