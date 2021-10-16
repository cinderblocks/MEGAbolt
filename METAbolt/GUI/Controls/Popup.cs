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
        if (components != null)
          components.Dispose();
        if (Content != null)
        {
          Control content = Content;
          Content = (Control) null;
          content.Dispose();
        }
      }
      base.Dispose(disposing);
    }

    private void InitializeComponent() => components = (IContainer) new Container();

    public Control Content { get; private set; }

    public PopupAnimations ShowingAnimation
    {
      get => showingAnimation;
      set
      {
        if (showingAnimation == value)
          return;
        showingAnimation = value;
      }
    }

    public PopupAnimations HidingAnimation
    {
      get => hidingAnimation;
      set
      {
        if (hidingAnimation == value)
          return;
        hidingAnimation = value;
      }
    }

    public int AnimationDuration
    {
      get => animationDuration;
      set
      {
        if (animationDuration == value)
          return;
        animationDuration = value;
      }
    }

    public bool FocusOnOpen { get; set; } = true;

    public bool AcceptAlt { get; set; } = true;

    public bool Resizable
    {
      get => resizable && !isChildPopupOpened;
      set => resizable = value;
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
      Content = content != null ? content : throw new ArgumentNullException(nameof (content));
      showingAnimation = PopupAnimations.SystemDefault;
      hidingAnimation = PopupAnimations.None;
      animationDuration = 100;
      isChildPopupOpened = false;
      InitializeComponent();
      AutoSize = false;
      DoubleBuffered = true;
      ResizeRedraw = true;
      host = new ToolStripControlHost(content);
      Padding = Margin = host.Padding = host.Margin = Padding.Empty;
      MinimumSize = content.MinimumSize;
      content.MinimumSize = content.Size;
      MaximumSize = content.MaximumSize;
      content.MaximumSize = content.Size;
      Size = content.Size;
      TabStop = content.TabStop = true;
      content.Location = Point.Empty;
      Items.Add((ToolStripItem) host);
      content.Disposed += (EventHandler) ((sender, e) =>
      {
        content = (Control) null;
        popup.Dispose(true);
      });
      content.RegionChanged += (EventHandler) ((sender, e) => UpdateRegion());
      content.Paint += (PaintEventHandler) ((sender, e) => PaintSizeGrip(e));
      UpdateRegion();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
      base.OnVisibleChanged(e);
      if (Visible && ShowingAnimation == PopupAnimations.None || !Visible && HidingAnimation == PopupAnimations.None)
        return;
      NativeMethods.AnimationFlags animationFlags = Visible ? NativeMethods.AnimationFlags.Roll : NativeMethods.AnimationFlags.Hide;
      PopupAnimations popupAnimations = Visible ? ShowingAnimation : HidingAnimation;
      if (popupAnimations == PopupAnimations.SystemDefault)
        popupAnimations = !SystemInformation.IsMenuAnimationEnabled ? PopupAnimations.None : (!SystemInformation.IsMenuFadeEnabled ? (PopupAnimations) (262144 | (Visible ? 4 : 8)) : PopupAnimations.Blend);
      if ((popupAnimations & (PopupAnimations.Center | PopupAnimations.Slide | PopupAnimations.Blend | PopupAnimations.Roll)) == PopupAnimations.None)
        return;
      if (resizableTop)
      {
        if ((popupAnimations & PopupAnimations.BottomToTop) != PopupAnimations.None)
          popupAnimations = popupAnimations & ~PopupAnimations.BottomToTop | PopupAnimations.TopToBottom;
        else if ((popupAnimations & PopupAnimations.TopToBottom) != PopupAnimations.None)
          popupAnimations = popupAnimations & ~PopupAnimations.TopToBottom | PopupAnimations.BottomToTop;
      }
      if (resizableLeft)
      {
        if ((popupAnimations & PopupAnimations.RightToLeft) != PopupAnimations.None)
          popupAnimations = popupAnimations & ~PopupAnimations.RightToLeft | PopupAnimations.LeftToRight;
        else if ((popupAnimations & PopupAnimations.LeftToRight) != PopupAnimations.None)
          popupAnimations = popupAnimations & ~PopupAnimations.LeftToRight | PopupAnimations.RightToLeft;
      }
      NativeMethods.AnimateWindow((Control) this, AnimationDuration, animationFlags | (NativeMethods.AnimationFlags) ((PopupAnimations) 1048575 & popupAnimations));
    }

    [UIPermission(SecurityAction.LinkDemand, Window = UIPermissionWindow.AllWindows)]
    protected override bool ProcessDialogKey(Keys keyData)
    {
      if (AcceptAlt && (keyData & Keys.Alt) == Keys.Alt)
      {
        if ((keyData & Keys.F4) != Keys.F4)
          return false;
        Close();
      }
      bool flag = base.ProcessDialogKey(keyData);
      if (!flag && (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift)))
        Content.SelectNextControl((Control) null, (keyData & Keys.Shift) != Keys.Shift, true, true, true);
      return flag;
    }

    protected void UpdateRegion()
    {
      if (Region != null)
      {
        Region.Dispose();
        Region = (Region) null;
      }
      if (Content.Region == null)
        return;
      Region = Content.Region.Clone();
    }

    public void Show(Control control)
    {
      if (control == null)
        throw new ArgumentNullException(nameof (control));
      Show(control, control.ClientRectangle);
    }

    public void Show(Control control, Rectangle area)
    {
      if (control == null)
        throw new ArgumentNullException(nameof (control));
      SetOwnerItem(control);
      resizableTop = resizableLeft = false;
      Point point = control.PointToScreen(new Point(area.Left, area.Top + area.Height));
      Rectangle workingArea = Screen.FromControl(control).WorkingArea;
      if (point.X + Size.Width > workingArea.Left + workingArea.Width)
      {
        resizableLeft = true;
        point.X = workingArea.Left + workingArea.Width - Size.Width;
      }
      if (point.Y + Size.Height > workingArea.Top + workingArea.Height)
      {
        resizableTop = true;
        point.Y -= Size.Height + area.Height;
      }
      point = control.PointToClient(point);
      Show(control, point, ToolStripDropDownDirection.BelowRight);
    }

    private void SetOwnerItem(Control control)
    {
      if (control == null)
        return;
      if (control is Popup popup)
      {
          ownerPopup = popup;
        ownerPopup.childPopup = this;
        OwnerItem = popup.Items[0];
      }
      else
      {
        if (opener == null)
          opener = control;
        if (control.Parent == null)
          return;
        SetOwnerItem(control.Parent);
      }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
      Content.MinimumSize = Size;
      Content.MaximumSize = Size;
      Content.Size = Size;
      Content.Location = Point.Empty;
      base.OnSizeChanged(e);
    }

    protected override void OnOpening(CancelEventArgs e)
    {
      if (Content.IsDisposed || Content.Disposing)
      {
        e.Cancel = true;
      }
      else
      {
        UpdateRegion();
        base.OnOpening(e);
      }
    }

    protected override void OnOpened(EventArgs e)
    {
      if (ownerPopup != null)
        ownerPopup.isChildPopupOpened = true;
      if (FocusOnOpen)
        Content.Focus();
      base.OnOpened(e);
    }

    protected override void OnClosed(ToolStripDropDownClosedEventArgs e)
    {
      opener = (Control) null;
      if (ownerPopup != null)
        ownerPopup.isChildPopupOpened = false;
      base.OnClosed(e);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    protected override void WndProc(ref Message m)
    {
      if (InternalProcessResizing(ref m, false))
        return;
      base.WndProc(ref m);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    public bool ProcessResizing(ref Message m) => InternalProcessResizing(ref m, true);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    private bool InternalProcessResizing(ref Message m, bool contentControl)
    {
      if (m.Msg == 134 && m.WParam != IntPtr.Zero && childPopup != null && childPopup.Visible)
        childPopup.Hide();
      if (!Resizable)
        return false;
      if (m.Msg == 132)
        return OnNcHitTest(ref m, contentControl);
      return m.Msg == 36 && OnGetMinMaxInfo(ref m);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    private bool OnGetMinMaxInfo(ref Message m)
    {
      NativeMethods.MINMAXINFO structure = (NativeMethods.MINMAXINFO) Marshal.PtrToStructure(m.LParam, typeof (NativeMethods.MINMAXINFO));
      if (!MaximumSize.IsEmpty)
        structure.maxTrackSize = MaximumSize;
      structure.minTrackSize = MinimumSize;
      Marshal.StructureToPtr((object) structure, m.LParam, false);
      return true;
    }

    private bool OnNcHitTest(ref Message m, bool contentControl)
    {
      Point client = PointToClient(new Point(NativeMethods.LOWORD(m.LParam), NativeMethods.HIWORD(m.LParam)));
      GripBounds gripBounds = new GripBounds(contentControl ? Content.ClientRectangle : ClientRectangle);
      IntPtr num = new IntPtr(-1);
      if (resizableTop)
      {
        if (resizableLeft && gripBounds.TopLeft.Contains(client))
        {
          m.Result = contentControl ? num : (IntPtr) 13;
          return true;
        }
        if (!resizableLeft && gripBounds.TopRight.Contains(client))
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
        if (resizableLeft && gripBounds.BottomLeft.Contains(client))
        {
          m.Result = contentControl ? num : (IntPtr) 16;
          return true;
        }
        if (!resizableLeft && gripBounds.BottomRight.Contains(client))
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
      if (resizableLeft && gripBounds.Left.Contains(client))
      {
        m.Result = contentControl ? num : (IntPtr) 10;
        return true;
      }
      if (resizableLeft || !gripBounds.Right.Contains(client))
        return false;
      m.Result = contentControl ? num : (IntPtr) 11;
      return true;
    }

    public void PaintSizeGrip(PaintEventArgs e)
    {
      if (e == null || e.Graphics == null || !resizable)
        return;
      Size clientSize = Content.ClientSize;
      using (Bitmap bitmap = new Bitmap(16, 16))
      {
        using (Graphics graphics = Graphics.FromImage((Image) bitmap))
        {
          if (Application.RenderWithVisualStyles)
          {
            if (sizeGripRenderer == null)
              sizeGripRenderer = new VisualStyleRenderer(VisualStyleElement.Status.Gripper.Normal);
            sizeGripRenderer.DrawBackground((IDeviceContext) graphics, new Rectangle(0, 0, 16, 16));
          }
          else
            ControlPaint.DrawSizeGrip(graphics, Content.BackColor, 0, 0, 16, 16);
        }
        GraphicsState gstate = e.Graphics.Save();
        e.Graphics.ResetTransform();
        if (resizableTop)
        {
          if (resizableLeft)
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
        else if (resizableLeft)
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
