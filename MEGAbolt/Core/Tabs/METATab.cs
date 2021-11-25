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
    public partial class MEGAboltTab
    {
        private string label;
        private string originalLabel;

        public MEGAboltTab(ToolStripButton button, Control control, string name, string label)
        {
            Button = button;
            Control = control;
            Name = name;
            this.label = label;
        }

        public void Close()
        {
            if (!AllowClose) return;

            if (Button != null)
            {
                Button.Dispose();
                Button = null;
            }

            if (Control != null)
            {
                Control.Dispose();
                Control = null;
            }

            OnTabClosed(EventArgs.Empty);
        }

        public void Select()
        {
            if (Detached) return;

            Control.Visible = true;
            Control.BringToFront();

            //if (!imboxhighlighted) Unhighlight();

            Unhighlight();

            Button.Checked = true;
            Selected = true;
            SelectedTab = Name;

            OnTabSelected(EventArgs.Empty);
        }

        public void Deselect()
        {
            if (Detached) return;

            if (Control != null) Control.Visible = false;
            if (Button != null) Button.Checked = false;
            Selected = false;

            OnTabDeselected(EventArgs.Empty);
        }

        public void PartialHighlight()
        {
            if (Selected) return;

            if (Detached)
            {
                if (!Owner.Focused)
                    FormFlash.Flash(Owner);
            }
            else
            {
                Button.Image = null;
                Button.ForeColor = Color.Blue;
            }

            PartiallyHighlighted = true;
            OnTabPartiallyHighlighted(EventArgs.Empty);
        }

        public void Highlight()
        {
            if (Selected) return;

            if (Detached)
            {
                if (!Owner.Focused)
                    FormFlash.Flash(Owner);
            }
            else
            {
                Button.Image = Properties.Resources.arrow_forward_16;
                Button.ForeColor = Color.Red;
            }

            Highlighted = true;
            OnTabHighlighted(EventArgs.Empty);
        }

        public void IMboxHighlight()
        {
            if (Selected) return;

            if (Detached)
            {
                if (!Owner.Focused)
                    FormFlash.Flash(Owner);
            }
            else
            {
                //button.Image = Properties.Resources.arrow_forward_16;
                Button.ForeColor = Color.Red;
            }

            IMboxHighlighted = true;
            OnTabHighlighted(EventArgs.Empty);
        }

        public void Unhighlight()
        {
            if (Detached)
            {
                FormFlash.Unflash(Owner);
            }
            else
            {
                Button.Image = null;
                Button.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
            }

            Highlighted = PartiallyHighlighted = IMboxHighlighted = false;
            OnTabUnhighlighted(EventArgs.Empty);
        }

        public void AttachTo(ToolStrip strip, Panel container)
        {
            if (!AllowDetach) return;
            if (!Detached) return;

            strip.Items.Add(Button);
            container.Controls.Add(Control);

            Owner = null;
            Detached = false;
            OnTabAttached(EventArgs.Empty);
        }

        public void Detach(MEGAboltInstance instance)
        {
            if (!AllowDetach) return;
            if (Detached) return;

            Owner = new frmDetachedTab(instance, this);
            Detached = true;
            OnTabDetached(EventArgs.Empty);            
        }

        public void MergeWith(MEGAboltTab tab)
        {
            if (!AllowMerge) return;
            if (Merged) return;

            SplitContainer container = new SplitContainer();
            container.Dock = DockStyle.Fill;
            container.BorderStyle = BorderStyle.Fixed3D;
            container.SplitterDistance = container.Width / 2;
            container.Panel1.Controls.Add(Control);
            container.Panel2.Controls.Add(tab.Control);

            Control.Visible = true;
            tab.Control.Visible = true;

            Control = container;
            tab.Control = container;
            
            MergedTab = tab;
            tab.MergedTab = this;

            originalLabel = label;
            tab.originalLabel = tab.label;
            Label = label + "+" + tab.Label;
            
            Merged = tab.Merged = true;

            OnTabMerged(EventArgs.Empty);
        }

        public MEGAboltTab Split()
        {
            if (!AllowMerge) return null;
            if (!Merged) return null;

            MEGAboltTab returnTab = MergedTab;
            MergedTab = null;
            returnTab.MergedTab = null;

            SplitContainer container = (SplitContainer)Control;
            Control = container.Panel1.Controls[0];
            returnTab.Control = container.Panel2.Controls[0];
            Merged = returnTab.Merged = false;

            Label = originalLabel;
            OnTabSplit(EventArgs.Empty);

            return returnTab;
        }

        public ToolStripButton Button { get; set; }

        public Control Control { get; set; }

        public Button DefaultControlButton { get; set; }

        public string Name { get; }

        public string Label
        {
            get => label;
            set => label = Button.Text = value;
        }

        public MEGAboltTab MergedTab { get; private set; }

        public Form Owner { get; private set; }

        public bool AllowMerge { get; set; } = true;

        public bool AllowDetach { get; set; } = true;

        public bool AllowClose { get; set; } = true;

        public bool PartiallyHighlighted { get; private set; } = false;

        public bool Highlighted { get; private set; } = false;

        public bool IMboxHighlighted { get; private set; } = false;

        public bool Selected { get; private set; } = false;

        public bool Detached { get; private set; } = false;

        public bool Merged { get; private set; } = false;

        public string SelectedTab { get; set; } = string.Empty;
    }
}
