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

namespace MEGAbolt.Controls
{
  [Flags]
  public enum PopupAnimations
  {
    None = 0,
    LeftToRight = 1,
    RightToLeft = 2,
    TopToBottom = 4,
    BottomToTop = 8,
    Center = 16, // 0x00000010
    Slide = 262144, // 0x00040000
    Blend = 524288, // 0x00080000
    Roll = 1048576, // 0x00100000
    SystemDefault = 2097152, // 0x00200000
  }
}
