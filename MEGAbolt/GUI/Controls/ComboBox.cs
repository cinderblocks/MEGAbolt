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
          _modalMenuFilter ??= Type.GetType("System.Windows.Forms.ToolStripManager+ModalMenuFilter");
          return _modalMenuFilter ??= new List<Type>(typeof(ToolStripManager).Assembly.GetTypes()).Find((Predicate<Type>)(type =>
              type.FullName == "System.Windows.Forms.ToolStripManager+ModalMenuFilter"));
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

    private static void SuspendMenuMode() => suspendMenuMode?.Invoke(null, null);

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

    private static void ResumeMenuMode() => resumeMenuMode?.Invoke(null, null);

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
