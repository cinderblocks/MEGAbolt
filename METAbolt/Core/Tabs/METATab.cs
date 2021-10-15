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
using System.Drawing;
using System.Windows.Forms;

namespace METAbolt
{
    public partial class METAboltTab
    {
        private string label;
        private string originalLabel;

        public METAboltTab(ToolStripButton button, Control control, string name, string label)
        {
            this.Button = button;
            this.Control = control;
            this.Name = name;
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
            SelectedTab = this.Name;

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

        public void Detach(METAboltInstance instance)
        {
            if (!AllowDetach) return;
            if (Detached) return;

            Owner = new frmDetachedTab(instance, this);
            Detached = true;
            OnTabDetached(EventArgs.Empty);            
        }

        public void MergeWith(METAboltTab tab)
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
            this.Label = label + "+" + tab.Label;
            
            Merged = tab.Merged = true;

            OnTabMerged(EventArgs.Empty);
        }

        public METAboltTab Split()
        {
            if (!AllowMerge) return null;
            if (!Merged) return null;

            METAboltTab returnTab = MergedTab;
            MergedTab = null;
            returnTab.MergedTab = null;

            SplitContainer container = (SplitContainer)Control;
            Control = container.Panel1.Controls[0];
            returnTab.Control = container.Panel2.Controls[0];
            Merged = returnTab.Merged = false;

            this.Label = originalLabel;
            OnTabSplit(EventArgs.Empty);

            return returnTab;
        }

        public ToolStripButton Button { get; set; }

        public Control Control { get; set; }

        public Button DefaultControlButton { get; set; }

        public string Name { get; }

        public string Label
        {
            get { return label; }
            set { label = Button.Text = value; }
        }

        public METAboltTab MergedTab { get; private set; }

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
