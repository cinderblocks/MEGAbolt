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

using System.Drawing;

namespace MEGAbolt.Controls
{
  internal readonly struct GripBounds
  {
    private const int GripSize = 6;
    private const int CornerGripSize = 12;

    public GripBounds(Rectangle clientRectangle) => ClientRectangle = clientRectangle;

    public Rectangle ClientRectangle { get; }

    public Rectangle Bottom
    {
      get
      {
        Rectangle clientRectangle = ClientRectangle;
        clientRectangle.Y = clientRectangle.Bottom - 6 + 1;
        clientRectangle.Height = 6;
        return clientRectangle;
      }
    }

    public Rectangle BottomRight
    {
      get
      {
        Rectangle clientRectangle = ClientRectangle;
        clientRectangle.Y = clientRectangle.Bottom - 12 + 1;
        clientRectangle.Height = 12;
        clientRectangle.X = clientRectangle.Width - 12 + 1;
        clientRectangle.Width = 12;
        return clientRectangle;
      }
    }

    public Rectangle Top
    {
      get
      {
        Rectangle clientRectangle = ClientRectangle;
        clientRectangle.Height = 6;
        return clientRectangle;
      }
    }

    public Rectangle TopRight
    {
      get
      {
        Rectangle clientRectangle = ClientRectangle;
        clientRectangle.Height = 12;
        clientRectangle.X = clientRectangle.Width - 12 + 1;
        clientRectangle.Width = 12;
        return clientRectangle;
      }
    }

    public Rectangle Left
    {
      get
      {
        Rectangle clientRectangle = ClientRectangle;
        clientRectangle.Width = 6;
        return clientRectangle;
      }
    }

    public Rectangle BottomLeft
    {
      get
      {
        Rectangle clientRectangle = ClientRectangle;
        clientRectangle.Width = 12;
        clientRectangle.Y = clientRectangle.Height - 12 + 1;
        clientRectangle.Height = 12;
        return clientRectangle;
      }
    }

    public Rectangle Right
    {
      get
      {
        Rectangle clientRectangle = ClientRectangle;
        clientRectangle.X = clientRectangle.Right - 6 + 1;
        clientRectangle.Width = 6;
        return clientRectangle;
      }
    }

    public Rectangle TopLeft
    {
      get
      {
        Rectangle clientRectangle = ClientRectangle;
        clientRectangle.Width = 12;
        clientRectangle.Height = 12;
        return clientRectangle;
      }
    }
  }
}
