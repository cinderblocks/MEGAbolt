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
using MEGAbolt.Controls;
using System.Globalization;


namespace METAbolt
{
    public partial class frmPreferences : Form
    {
        private METAboltInstance instance;
        private Dictionary<string, IPreferencePane> panes;
        private IPreferencePane selectedPane;
        private Popup toolTip;
        private CustomToolTip customToolTip;

        public frmPreferences(METAboltInstance instance)
        {
            InitializeComponent();

            string msg1 = "Click for help on how to use Application/Preferences";
            toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            toolTip.AutoClose = false;
            toolTip.FocusOnOpen = false;
            toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;

            this.instance = instance;
            panes = new Dictionary<string, IPreferencePane>();
            //tcons = new PrefTextConsole(instance);  
            
            AddPreferencePane(new PrefGeneralConsole(instance));
            AddPreferencePane(new PrefTextConsole(instance));
            AddPreferencePane(new PrefAI(instance));
            //AddPreferencePane(new PrefTwitter(instance));
            AddPreferencePane(new PrefChairAnn(instance));
            AddPreferencePane(new PrefProxy(instance));
            AddPreferencePane(new PrefPlugin(instance));
            AddPreferencePane(new PrefMETAgiver(instance));
            AddPreferencePane(new Pref3D(instance));
            AddPreferencePane(new PrefSpelling(instance));
            lbxPanes.SelectedIndex = 0;
        }

        private void AddPreferencePane(IPreferencePane pane)
        {
            lbxPanes.Items.Add(new PreferencePaneListItem(pane.Name, pane.Icon));

            Control paneControl = (Control)pane;
            paneControl.Dock = DockStyle.Fill;
            paneControl.Visible = false;
            pnlPanes.Controls.Add(paneControl);

            panes.Add(pane.Name, pane);
        }

        private void SelectPreferencePane(string name)
        {
            IPreferencePane pane = panes[name];
            if (pane == selectedPane) return;
            
            Control paneControl = (Control)pane;
            Control selectedPaneControl = selectedPane as Control;

            paneControl.Visible = true;
            if (selectedPaneControl != null) selectedPaneControl.Visible = false;

            selectedPane = pane;
        }

        private void Apply()
        {
            foreach (KeyValuePair<string, IPreferencePane> kvp in panes)
                kvp.Value.SetPreferences();

            instance.Config.ApplyCurrentConfig();
        }

        private void lbxPanes_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0) return;
            
            PreferencePaneListItem itemToDraw = (PreferencePaneListItem)lbxPanes.Items[e.Index];
            Brush textBrush = null;
            Font textFont = null;

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                textBrush = new SolidBrush(Color.FromKnownColor(KnownColor.HighlightText));
                textFont = new Font(e.Font, FontStyle.Bold);
            }
            else
            {
                textBrush = new SolidBrush(Color.FromKnownColor(KnownColor.ControlText));
                textFont = new Font(e.Font, FontStyle.Regular);
            }
            
            SizeF stringSize = e.Graphics.MeasureString(itemToDraw.Name, textFont);
            float stringX = e.Bounds.Left + 4 + itemToDraw.Icon.Width;
            float stringY = e.Bounds.Top + 2 + ((itemToDraw.Icon.Height / 2) - (stringSize.Height / 2));

            e.Graphics.DrawImage(itemToDraw.Icon, e.Bounds.Left + 2, e.Bounds.Top + 2);
            e.Graphics.DrawString(itemToDraw.Name, textFont, textBrush, stringX, stringY);

            e.DrawFocusRectangle();

            textFont.Dispose();
            textBrush.Dispose();
            textFont = null;
            textBrush = null;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Apply();
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            Apply();
        }

        private void lbxPanes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbxPanes.SelectedItem == null) return;
            PreferencePaneListItem item = (PreferencePaneListItem)lbxPanes.SelectedItem;

            // Screen size change to accomodate initial IM feature thx to Elmo Clarity 20/12/2010

            if (item.Name.ToLower(CultureInfo.CurrentCulture) == "text")
            {
                Height = 460;
            }
            else
            {
                Height = 390;
            }

            SelectPreferencePane(item.Name);
        }

        private void frmPreferences_Load(object sender, EventArgs e)
        {
            CenterToParent();
        }

        private void pnlPanes_Paint(object sender, PaintEventArgs e)
        {

        }

        private void picAutoSit_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"http://www.metabolt.net/metawiki/Using-Preferences.ashx");
        }

        private void picAutoSit_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show(picAutoSit);
        }

        private void picAutoSit_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Close();
        }
    }
}