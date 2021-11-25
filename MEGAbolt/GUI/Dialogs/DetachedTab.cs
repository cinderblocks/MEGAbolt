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
    public partial class frmDetachedTab : Form
    {
        private MEGAboltInstance instance;
        private MEGAboltTab tab;

        //For reattachment

        public frmDetachedTab(MEGAboltInstance instance, MEGAboltTab tab)
        {
            InitializeComponent();

            this.instance = instance;
            this.tab = tab;
            Controls.Add(tab.Control);
            tab.Control.BringToFront();

            AddTabEvents();
            Text = tab.Label + " (tab) - MEGAbolt";

            ApplyConfig(this.instance.Config.CurrentConfig);
            this.instance.Config.ConfigApplied += Config_ConfigApplied;
        }

        private void Config_ConfigApplied(object sender, ConfigAppliedEventArgs e)
        {
            ApplyConfig(e.AppliedConfig);
        }

        private void ApplyConfig(Config config)
        {
            if (config.InterfaceStyle == 0) //System
                tstMain.RenderMode = ToolStripRenderMode.System;
            else if (config.InterfaceStyle == 1) //Office 2003
                tstMain.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        }

        private void AddTabEvents()
        {
            tab.TabPartiallyHighlighted += tab_TabPartiallyHighlighted;
            tab.TabUnhighlighted += tab_TabUnhighlighted;
        }

        private void tab_TabUnhighlighted(object sender, EventArgs e)
        {
            tlblTyping.Visible = false;
        }

        private void tab_TabPartiallyHighlighted(object sender, EventArgs e)
        {
            tlblTyping.Visible = true;
        }

        private void frmDetachedTab_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (tab.Detached)
            {
                if (tab.AllowClose)
                    tab.Close();
                else
                    tab.AttachTo(ReattachStrip, ReattachContainer);
            }
        }

        private void tbtnReattach_Click(object sender, EventArgs e)
        {
            tab.AttachTo(ReattachStrip, ReattachContainer);
            Close();
        }

        public ToolStrip ReattachStrip { get; set; }

        public Panel ReattachContainer { get; set; }

        private void frmDetachedTab_Load(object sender, EventArgs e)
        {

        }
    }
}