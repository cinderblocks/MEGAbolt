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
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace MEGAbolt.Controls
{
  [CLSCompliant(true)]
  [ToolboxItem(false)]
  public class Popup : ToolStripDropDown
  {
    private IContainer components;
    private PopupAnimations showingAnimation;
    private PopupAnimations hidingAnimation;
    private int animationDuration;
    private Control opener;
    private Popup ownerPopup;
    private Popup childPopup;
    private bool resizableTop;
    private bool resizableLeft;
    private bool isChildPopupOpened;
    private bool resizable;
    private ToolStripControlHost host;
    private VisualStyleRenderer sizeGripRenderer;

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (this.components != null)
          this.components.Dispose();
        if (this.Content != null)
        {
          Control content = this.Content;
          this.Content = (Control) null;
          content.Dispose();
        }
      }
      base.Dispose(disposing);
    }

    private void InitializeComponent() => this.components = (IContainer) new Container();

    public Control Content { get; private set; }

    public PopupAnimations ShowingAnimation
    {
      get => this.showingAnimation;
      set
      {
        if (this.showingAnimation == value)
          return;
        this.showingAnimation = value;
      }
    }

    public PopupAnimations HidingAnimation
    {
      get => this.hidingAnimation;
      set
      {
        if (this.hidingAnimation == value)
          return;
        this.hidingAnimation = value;
      }
    }

    public int AnimationDuration
    {
      get => this.animationDuration;
      set
      {
        if (this.animationDuration == value)
          return;
        this.animationDuration = value;
      }
    }

    public bool FocusOnOpen { get; set; } = true;

    public bool AcceptAlt { get; set; } = true;

    public bool Resizable
    {
      get => this.resizable && !this.isChildPopupOpened;
      set => this.resizable = value;
    }

    public new Size MinimumSize { get; set; }

    public new Size MaximumSize { get; set; }

    protected override CreateParams CreateParams
    {
      [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)] get
      {
        CreateParams createParams = base.CreateParams;
        createParams.ExStyle |= 134217728;
        return createParams;
      }
    }

    public Popup(Control content)
    {
      Popup popup = this;
      this.Content = content != null ? content : throw new ArgumentNullException(nameof (content));
      this.showingAnimation = PopupAnimations.SystemDefault;
      this.hidingAnimation = PopupAnimations.None;
      this.animationDuration = 100;
      this.isChildPopupOpened = false;
      this.InitializeComponent();
      this.AutoSize = false;
      this.DoubleBuffered = true;
      this.ResizeRedraw = true;
      this.host = new ToolStripControlHost(content);
      this.Padding = this.Margin = this.host.Padding = this.host.Margin = Padding.Empty;
      this.MinimumSize = content.MinimumSize;
      content.MinimumSize = content.Size;
      this.MaximumSize = content.MaximumSize;
      content.MaximumSize = content.Size;
      this.Size = content.Size;
      this.TabStop = content.TabStop = true;
      content.Location = Point.Empty;
      this.Items.Add((ToolStripItem) this.host);
      content.Disposed += (EventHandler) ((sender, e) =>
      {
        content = (Control) null;
        popup.Dispose(true);
      });
      content.RegionChanged += (EventHandler) ((sender, e) => this.UpdateRegion());
      content.Paint += (PaintEventHandler) ((sender, e) => this.PaintSizeGrip(e));
      this.UpdateRegion();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
      base.OnVisibleChanged(e);
      if (this.Visible && this.ShowingAnimation == PopupAnimations.None || !this.Visible && this.HidingAnimation == PopupAnimations.None)
        return;
      NativeMethods.AnimationFlags animationFlags = this.Visible ? NativeMethods.AnimationFlags.Roll : NativeMethods.AnimationFlags.Hide;
      PopupAnimations popupAnimations = this.Visible ? this.ShowingAnimation : this.HidingAnimation;
      if (popupAnimations == PopupAnimations.SystemDefault)
        popupAnimations = !SystemInformation.IsMenuAnimationEnabled ? PopupAnimations.None : (!SystemInformation.IsMenuFadeEnabled ? (PopupAnimations) (262144 | (this.Visible ? 4 : 8)) : PopupAnimations.Blend);
      if ((popupAnimations & (PopupAnimations.Center | PopupAnimations.Slide | PopupAnimations.Blend | PopupAnimations.Roll)) == PopupAnimations.None)
        return;
      if (this.resizableTop)
      {
        if ((popupAnimations & PopupAnimations.BottomToTop) != PopupAnimations.None)
          popupAnimations = popupAnimations & ~PopupAnimations.BottomToTop | PopupAnimations.TopToBottom;
        else if ((popupAnimations & PopupAnimations.TopToBottom) != PopupAnimations.None)
          popupAnimations = popupAnimations & ~PopupAnimations.TopToBottom | PopupAnimations.BottomToTop;
      }
      if (this.resizableLeft)
      {
        if ((popupAnimations & PopupAnimations.RightToLeft) != PopupAnimations.None)
          popupAnimations = popupAnimations & ~PopupAnimations.RightToLeft | PopupAnimations.LeftToRight;
        else if ((popupAnimations & PopupAnimations.LeftToRight) != PopupAnimations.None)
          popupAnimations = popupAnimations & ~PopupAnimations.LeftToRight | PopupAnimations.RightToLeft;
      }
      NativeMethods.AnimateWindow((Control) this, this.AnimationDuration, animationFlags | (NativeMethods.AnimationFlags) ((PopupAnimations) 1048575 & popupAnimations));
    }

    [UIPermission(SecurityAction.LinkDemand, Window = UIPermissionWindow.AllWindows)]
    protected override bool ProcessDialogKey(Keys keyData)
    {
      if (this.AcceptAlt && (keyData & Keys.Alt) == Keys.Alt)
      {
        if ((keyData & Keys.F4) != Keys.F4)
          return false;
        this.Close();
      }
      bool flag = base.ProcessDialogKey(keyData);
      if (!flag && (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift)))
        this.Content.SelectNextControl((Control) null, (keyData & Keys.Shift) != Keys.Shift, true, true, true);
      return flag;
    }

    protected void UpdateRegion()
    {
      if (this.Region != null)
      {
        this.Region.Dispose();
        this.Region = (Region) null;
      }
      if (this.Content.Region == null)
        return;
      this.Region = this.Content.Region.Clone();
    }

    public void Show(Control control)
    {
      if (control == null)
        throw new ArgumentNullException(nameof (control));
      this.Show(control, control.ClientRectangle);
    }

    public void Show(Control control, Rectangle area)
    {
      if (control == null)
        throw new ArgumentNullException(nameof (control));
      this.SetOwnerItem(control);
      this.resizableTop = this.resizableLeft = false;
      Point point = control.PointToScreen(new Point(area.Left, area.Top + area.Height));
      Rectangle workingArea = Screen.FromControl(control).WorkingArea;
      if (point.X + this.Size.Width > workingArea.Left + workingArea.Width)
      {
        this.resizableLeft = true;
        point.X = workingArea.Left + workingArea.Width - this.Size.Width;
      }
      if (point.Y + this.Size.Height > workingArea.Top + workingArea.Height)
      {
        this.resizableTop = true;
        point.Y -= this.Size.Height + area.Height;
      }
      point = control.PointToClient(point);
      this.Show(control, point, ToolStripDropDownDirection.BelowRight);
    }

    private void SetOwnerItem(Control control)
    {
      if (control == null)
        return;
      if (control is Popup popup)
      {
          this.ownerPopup = popup;
        this.ownerPopup.childPopup = this;
        this.OwnerItem = popup.Items[0];
      }
      else
      {
        if (this.opener == null)
          this.opener = control;
        if (control.Parent == null)
          return;
        this.SetOwnerItem(control.Parent);
      }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
      this.Content.MinimumSize = this.Size;
      this.Content.MaximumSize = this.Size;
      this.Content.Size = this.Size;
      this.Content.Location = Point.Empty;
      base.OnSizeChanged(e);
    }

    protected override void OnOpening(CancelEventArgs e)
    {
      if (this.Content.IsDisposed || this.Content.Disposing)
      {
        e.Cancel = true;
      }
      else
      {
        this.UpdateRegion();
        base.OnOpening(e);
      }
    }

    protected override void OnOpened(EventArgs e)
    {
      if (this.ownerPopup != null)
        this.ownerPopup.isChildPopupOpened = true;
      if (this.FocusOnOpen)
        this.Content.Focus();
      base.OnOpened(e);
    }

    protected override void OnClosed(ToolStripDropDownClosedEventArgs e)
    {
      this.opener = (Control) null;
      if (this.ownerPopup != null)
        this.ownerPopup.isChildPopupOpened = false;
      base.OnClosed(e);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    protected override void WndProc(ref Message m)
    {
      if (this.InternalProcessResizing(ref m, false))
        return;
      base.WndProc(ref m);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    public bool ProcessResizing(ref Message m) => this.InternalProcessResizing(ref m, true);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    private bool InternalProcessResizing(ref Message m, bool contentControl)
    {
      if (m.Msg == 134 && m.WParam != IntPtr.Zero && this.childPopup != null && this.childPopup.Visible)
        this.childPopup.Hide();
      if (!this.Resizable)
        return false;
      if (m.Msg == 132)
        return this.OnNcHitTest(ref m, contentControl);
      return m.Msg == 36 && this.OnGetMinMaxInfo(ref m);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    private bool OnGetMinMaxInfo(ref Message m)
    {
      NativeMethods.MINMAXINFO structure = (NativeMethods.MINMAXINFO) Marshal.PtrToStructure(m.LParam, typeof (NativeMethods.MINMAXINFO));
      if (!this.MaximumSize.IsEmpty)
        structure.maxTrackSize = this.MaximumSize;
      structure.minTrackSize = this.MinimumSize;
      Marshal.StructureToPtr((object) structure, m.LParam, false);
      return true;
    }

    private bool OnNcHitTest(ref Message m, bool contentControl)
    {
      Point client = this.PointToClient(new Point(NativeMethods.LOWORD(m.LParam), NativeMethods.HIWORD(m.LParam)));
      GripBounds gripBounds = new GripBounds(contentControl ? this.Content.ClientRectangle : this.ClientRectangle);
      IntPtr num = new IntPtr(-1);
      if (this.resizableTop)
      {
        if (this.resizableLeft && gripBounds.TopLeft.Contains(client))
        {
          m.Result = contentControl ? num : (IntPtr) 13;
          return true;
        }
        if (!this.resizableLeft && gripBounds.TopRight.Contains(client))
        {
          m.Result = contentControl ? num : (IntPtr) 14;
          return true;
        }
        if (gripBounds.Top.Contains(client))
        {
          m.Result = contentControl ? num : (IntPtr) 12;
          return true;
        }
      }
      else
      {
        if (this.resizableLeft && gripBounds.BottomLeft.Contains(client))
        {
          m.Result = contentControl ? num : (IntPtr) 16;
          return true;
        }
        if (!this.resizableLeft && gripBounds.BottomRight.Contains(client))
        {
          m.Result = contentControl ? num : (IntPtr) 17;
          return true;
        }
        if (gripBounds.Bottom.Contains(client))
        {
          m.Result = contentControl ? num : (IntPtr) 15;
          return true;
        }
      }
      if (this.resizableLeft && gripBounds.Left.Contains(client))
      {
        m.Result = contentControl ? num : (IntPtr) 10;
        return true;
      }
      if (this.resizableLeft || !gripBounds.Right.Contains(client))
        return false;
      m.Result = contentControl ? num : (IntPtr) 11;
      return true;
    }

    public void PaintSizeGrip(PaintEventArgs e)
    {
      if (e == null || e.Graphics == null || !this.resizable)
        return;
      Size clientSize = this.Content.ClientSize;
      using (Bitmap bitmap = new Bitmap(16, 16))
      {
        using (Graphics graphics = Graphics.FromImage((Image) bitmap))
        {
          if (Application.RenderWithVisualStyles)
          {
            if (this.sizeGripRenderer == null)
              this.sizeGripRenderer = new VisualStyleRenderer(VisualStyleElement.Status.Gripper.Normal);
            this.sizeGripRenderer.DrawBackground((IDeviceContext) graphics, new Rectangle(0, 0, 16, 16));
          }
          else
            ControlPaint.DrawSizeGrip(graphics, this.Content.BackColor, 0, 0, 16, 16);
        }
        GraphicsState gstate = e.Graphics.Save();
        e.Graphics.ResetTransform();
        if (this.resizableTop)
        {
          if (this.resizableLeft)
          {
            e.Graphics.RotateTransform(180f);
            e.Graphics.TranslateTransform((float) -clientSize.Width, (float) -clientSize.Height);
          }
          else
          {
            e.Graphics.ScaleTransform(1f, -1f);
            e.Graphics.TranslateTransform(0.0f, (float) -clientSize.Height);
          }
        }
        else if (this.resizableLeft)
        {
          e.Graphics.ScaleTransform(-1f, 1f);
          e.Graphics.TranslateTransform((float) -clientSize.Width, 0.0f);
        }
        e.Graphics.DrawImage((Image) bitmap, clientSize.Width - 16, clientSize.Height - 16 + 1, 16, 16);
        e.Graphics.Restore(gstate);
      }
    }
  }
}
