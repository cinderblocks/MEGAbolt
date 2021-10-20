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

using System.Drawing;
using System.Windows.Forms;

namespace METAbolt
{

    /// <summary>
    /// Summary description for EmoticonMenuItem.
    /// </summary>
    public class EmoticonMenuItem : ToolStripMenuItem
    {

        private const int ICON_WIDTH = 19;
        private const int ICON_HEIGHT = 19;
        private const int ICON_MARGIN = 4;
        private Color backgroundColor, selectionColor, selectionBorderColor;

        public override Image Image { get; set; }

        public EmoticonMenuItem()
        {
            backgroundColor = SystemColors.ControlLightLight;
            selectionColor = Color.FromArgb(50, 0, 0, 150);
            selectionBorderColor = SystemColors.Highlight;
        }

        public EmoticonMenuItem(Image _image)
            : this()
        {
            Image = _image;
        }

        protected void OnMeasureItem(MeasureItemEventArgs e)
        {
            e.ItemWidth = ICON_WIDTH + ICON_MARGIN;
            e.ItemHeight = ICON_HEIGHT + 2 * ICON_MARGIN;
        }

        protected void OnDrawItem(DrawItemEventArgs e)
        {
            Graphics _graphics = e.Graphics;
            Rectangle _bounds = e.Bounds;

            DrawBackground(_graphics, _bounds, ((e.State & DrawItemState.Selected) != 0));
            _graphics.DrawImage(Image, _bounds.X + ((_bounds.Width - ICON_WIDTH) / 2), _bounds.Y + ((_bounds.Height - ICON_HEIGHT) / 2));
        }

        private void DrawBackground(Graphics _graphics, Rectangle _bounds, bool _selected)
        {
            if (_selected)
            {
                _graphics.FillRectangle(new SolidBrush(selectionColor), _bounds);
                _graphics.DrawRectangle(new Pen(selectionBorderColor), _bounds.X, _bounds.Y,
                    _bounds.Width - 1, _bounds.Height - 1);
            }
            else
            {
                _graphics.FillRectangle(new SolidBrush(backgroundColor), _bounds);
            }
        }
    }
}
