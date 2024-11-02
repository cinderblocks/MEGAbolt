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
using CoreJ2K;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace MEGAbolt
{
    public static class ImageHelper
    {
        public static Image Decode(byte[] j2cdata)
        {
            using (var bitmap = J2kImage.FromBytes(j2cdata).As<SKBitmap>())
            {
                return bitmap.ToBitmap();
            }
        }
    }
}
