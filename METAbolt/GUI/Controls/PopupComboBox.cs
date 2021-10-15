/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2021, Sjofn, LLC
 * All rights reserved.
 *  
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;

namespace MEGAbolt.Controls
{
  [ToolboxBitmap(typeof (System.Windows.Forms.ComboBox))]
  [ToolboxItemFilter("System.Windows.Forms")]
  [ToolboxItem(true)]
  [Description("Displays an editable text box with a drop-down list of permitted values.")]
  public class PopupComboBox : ComboBox
  {
    private IContainer components;
    private Popup dropDown;
    private Control dropDownControl;
    private DateTime dropDownHideTime;

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (this.components != null)
          this.components.Dispose();
        if (this.dropDown != null)
          this.dropDown.Dispose();
      }
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      this.SuspendLayout();
      this.ResumeLayout(false);
    }

    public PopupComboBox()
    {
      this.dropDownHideTime = DateTime.UtcNow;
      this.InitializeComponent();
      base.DropDownHeight = base.DropDownWidth = 1;
      base.IntegralHeight = false;
    }

    public Control DropDownControl
    {
      get => this.dropDownControl;
      set
      {
        if (this.dropDownControl == value)
          return;
        this.dropDownControl = value;
        if (this.dropDown != null)
        {
          this.dropDown.Closed -= this.dropDown_Closed;
          this.dropDown.Dispose();
        }
        this.dropDown = new Popup(value);
        this.dropDown.Closed += this.dropDown_Closed;
      }
    }

    private void dropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e) => this.dropDownHideTime = DateTime.UtcNow;

    public new bool DroppedDown
    {
      get => this.dropDown.Visible;
      set
      {
        if (this.DroppedDown)
          this.HideDropDown();
        else
          this.ShowDropDown();
      }
    }

    public new event EventHandler DropDown;

    public void ShowDropDown()
    {
      if (this.dropDown == null)
        return;
      if ((DateTime.UtcNow - this.dropDownHideTime).TotalSeconds > 0.5)
      {
        if (this.DropDown != null)
          this.DropDown((object) this, EventArgs.Empty);
        this.dropDown.Show((Control) this);
      }
      else
      {
        this.dropDownHideTime = DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 1));
        this.Focus();
      }
    }

    public new event EventHandler DropDownClosed;

    public void HideDropDown()
    {
      if (this.dropDown == null)
        return;
      this.dropDown.Hide();
      if (this.DropDownClosed == null)
        return;
      this.DropDownClosed((object) this, EventArgs.Empty);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    protected override void WndProc(ref Message m)
    {
      if (m.Msg == 8465 && NativeMethods.HIWORD(m.WParam) == 7)
        this.ShowDropDown();
      else
        base.WndProc(ref m);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public new int DropDownWidth
    {
      get => base.DropDownWidth;
      set => base.DropDownWidth = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    public new int DropDownHeight
    {
      get => base.DropDownHeight;
      set => base.DropDownHeight = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    public new bool IntegralHeight
    {
      get => base.IntegralHeight;
      set => base.IntegralHeight = value;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public new System.Windows.Forms.ComboBox.ObjectCollection Items => base.Items;

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new int ItemHeight
    {
      get => base.ItemHeight;
      set => base.ItemHeight = value;
    }
  }
}
