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
using System.Windows.Forms.Design;

namespace MEGAbolt
{
    public class WToolStripControlHost : ToolStripControlHost
    {
        public WToolStripControlHost()
            : base(new Control())
        {

        }

        public WToolStripControlHost(Control c)
            : base(c)
        {
        }
    }

    [ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip)]
    public partial class ToolStripCheckBox : WToolStripControlHost
    {
        public ToolStripCheckBox()
            : base(new CheckBox())
        {
            ToolStripCheckBoxControl.MouseHover += chk_MouseHover;
        }

        public CheckBox ToolStripCheckBoxControl => Control as CheckBox;

        void chk_MouseHover(object sender, EventArgs e)
        {
            OnMouseHover(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            ToolStripCheckBoxControl.Text = Text;
        }

        //expose checkbox.enabled as property
        public bool ToolStripCheckBoxEnabled
        {
            get => ToolStripCheckBoxControl.Enabled;
            set => ToolStripCheckBoxControl.Enabled = value;
        }

        public bool Checked
        {
            get => ToolStripCheckBoxControl.Checked;
            set => ToolStripCheckBoxControl.Checked = value;
        }

        protected override void OnSubscribeControlEvents(Control c)
        {
            // Call the base method so the basic events are unsubscribed.
            base.OnSubscribeControlEvents(c);

            CheckBox ToolStripCheckBoxControl = (CheckBox)c;
            // Add the event.
            ToolStripCheckBoxControl.CheckedChanged += OnCheckChanged;
        }

        protected override void OnUnsubscribeControlEvents(Control c)
        {
            // Call the base method so the basic events are unsubscribed.
            base.OnUnsubscribeControlEvents(c);


            CheckBox ToolStripCheckBoxControl = (CheckBox)c;
            // Remove the event.
            ToolStripCheckBoxControl.CheckedChanged -= OnCheckChanged;
        }

        public event EventHandler CheckedChanged;

        private void OnCheckChanged(object sender, EventArgs e)
        {
            CheckedChanged?.Invoke(this, e);
        }
    }
}