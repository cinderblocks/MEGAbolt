﻿/*
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace MEGAbolt.Controls
{
  [Description("Displays an editable text box with a drop-down list of permitted values.")]
  [ToolboxItemFilter("System.Windows.Forms")]
  [ToolboxBitmap(typeof (System.Windows.Forms.ComboBox))]
  [ToolboxItem(true)]
  public class ComboBox : System.Windows.Forms.ComboBox
  {
    private static Type _modalMenuFilter;
    private static MethodInfo _suspendMenuMode;
    private static MethodInfo _resumeMenuMode;
    private IContainer components;

    public ComboBox() => InitializeComponent();

    private static Type modalMenuFilter
    {
      get
      {
        if (_modalMenuFilter == null)
          _modalMenuFilter = Type.GetType("System.Windows.Forms.ToolStripManager+ModalMenuFilter");
        if (_modalMenuFilter == null)
          _modalMenuFilter = new List<Type>((IEnumerable<Type>) typeof (ToolStripManager).Assembly.GetTypes()).Find((Predicate<Type>) (type => type.FullName == "System.Windows.Forms.ToolStripManager+ModalMenuFilter"));
        return _modalMenuFilter;
      }
    }

    private static MethodInfo suspendMenuMode
    {
      get
      {
        if (_suspendMenuMode == null)
        {
          Type modalMenuFilter = ComboBox.modalMenuFilter;
          if (modalMenuFilter != null)
            _suspendMenuMode = modalMenuFilter.GetMethod("SuspendMenuMode", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        return _suspendMenuMode;
      }
    }

    private static void SuspendMenuMode() => suspendMenuMode?.Invoke((object) null, (object[]) null);

    private static MethodInfo resumeMenuMode
    {
      get
      {
        if (_resumeMenuMode == null)
        {
          Type modalMenuFilter = ComboBox.modalMenuFilter;
          if (modalMenuFilter != null)
            _resumeMenuMode = modalMenuFilter.GetMethod("ResumeMenuMode", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        return _resumeMenuMode;
      }
    }

    private static void ResumeMenuMode() => resumeMenuMode?.Invoke((object) null, (object[]) null);

    protected override void OnDropDown(EventArgs e)
    {
      base.OnDropDown(e);
      SuspendMenuMode();
    }

    protected override void OnDropDownClosed(EventArgs e)
    {
      ResumeMenuMode();
      base.OnDropDownClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && components != null)
        components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      SuspendLayout();
      ResumeLayout(false);
    }
  }
}
