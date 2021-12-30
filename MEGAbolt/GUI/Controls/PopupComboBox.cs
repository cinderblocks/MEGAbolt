/*
 * MEGAbolt Metaverse Client
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
          components?.Dispose();
          dropDown?.Dispose();
      }
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      SuspendLayout();
      ResumeLayout(false);
    }

    public PopupComboBox()
    {
      dropDownHideTime = DateTime.UtcNow;
      InitializeComponent();
      base.DropDownHeight = base.DropDownWidth = 1;
      base.IntegralHeight = false;
    }

    public Control DropDownControl
    {
      get => dropDownControl;
      set
      {
        if (dropDownControl == value)
          return;
        dropDownControl = value;
        if (dropDown != null)
        {
          dropDown.Closed -= dropDown_Closed;
          dropDown.Dispose();
        }
        dropDown = new Popup(value);
        dropDown.Closed += dropDown_Closed;
      }
    }

    private void dropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e) => dropDownHideTime = DateTime.UtcNow;

    public new bool DroppedDown
    {
      get => dropDown.Visible;
      set
      {
        if (DroppedDown)
          HideDropDown();
        else
          ShowDropDown();
      }
    }

    public new event EventHandler DropDown;

    public void ShowDropDown()
    {
      if (dropDown == null)
        return;
      if ((DateTime.UtcNow - dropDownHideTime).TotalSeconds > 0.5)
      {
          DropDown?.Invoke((object) this, EventArgs.Empty);
        dropDown.Show((Control) this);
      }
      else
      {
        dropDownHideTime = DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 1));
        Focus();
      }
    }

    public new event EventHandler DropDownClosed;

    public void HideDropDown()
    {
      if (dropDown == null)
        return;
      dropDown.Hide();
      DropDownClosed?.Invoke((object) this, EventArgs.Empty);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    protected override void WndProc(ref Message m)
    {
      if (m.Msg == 8465 && NativeMethods.HIWORD(m.WParam) == 7)
        ShowDropDown();
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
    public new ObjectCollection Items => base.Items;

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
