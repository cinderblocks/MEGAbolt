//  Copyright (c) 2008 - 2014, www.metabolt.net (METAbolt)
//  Copyright (c) 2006-2008, Paul Clement (a.k.a. Delta)
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  * Redistributions of source code must retain the above copyright notice, 
//    this list of conditions and the following disclaimer. 
//  * Redistributions in binary form must reproduce the above copyright notice, 
//    this list of conditions and the following disclaimer in the documentation 
//    and/or other materials provided with the distribution. 

//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
//  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED 
//  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
//  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
//  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
//  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR 
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
//  WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
//  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
//  POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Windows.Forms;

namespace METAbolt
{
    public partial class frmDetachedTab : Form
    {
        private METAboltInstance instance;
        private METAboltTab tab;

        //For reattachment

        public frmDetachedTab(METAboltInstance instance, METAboltTab tab)
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