/*
 * Radegast Metaverse Client
 * Copyright(c) 2009-2014, Radegast Development Team
 * Copyright(c) 2016-2021, Sjofn, LLC
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

using CoreJ2K;
using OpenMetaverse;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace MEGAbolt.Rendering
{
    public static class TerrainSplat
    {
        #region Constants

        private static readonly UUID DIRT_DETAIL = new UUID("0bc58228-74a0-7e83-89bc-5c23464bcec5");
        private static readonly UUID GRASS_DETAIL = new UUID("63338ede-0037-c4fd-855b-015d77112fc8");
        private static readonly UUID MOUNTAIN_DETAIL = new UUID("303cd381-8560-7579-23f1-f0a880799740");
        private static readonly UUID ROCK_DETAIL = new UUID("53a2f406-4895-1d13-d541-d2e3b86bc19c");
        private static readonly int RegionSize = 256;

        private static readonly UUID[] DEFAULT_TERRAIN_DETAIL = new UUID[]
        {
            DIRT_DETAIL,
            GRASS_DETAIL,
            MOUNTAIN_DETAIL,
            ROCK_DETAIL
        };

        private static readonly Color[] DEFAULT_TERRAIN_COLOR = new Color[]
        {
            Color.FromArgb(255, 164, 136, 117),
            Color.FromArgb(255, 65, 87, 47),
            Color.FromArgb(255, 157, 145, 131),
            Color.FromArgb(255, 125, 128, 130)
        };

        private static readonly UUID TERRAIN_CACHE_MAGIC = new UUID("2c0c7ef2-56be-4eb8-aacb-76712c535b4b");

        #endregion Constants

        /// <summary>
        /// Builds a composited terrain texture given the region texture
        /// and heightmap settings
        /// </summary>
        /// <param name="instance">Radegast Instance</param>
        /// <param name="heightmap">Terrain heightmap</param>
        /// <param name="textureIDs"></param>
        /// <param name="startHeights"></param>
        /// <param name="heightRanges"></param>
        /// <returns>A composited 256x256 RGB texture ready for rendering</returns>
        /// <remarks>Based on the algorithm described at http://opensimulator.org/wiki/Terrain_Splatting
        /// </remarks>
        public static Bitmap Splat(MEGAboltInstance instance, float[,] heightmap,
            UUID[] textureIDs, float[] startHeights, float[] heightRanges)
        {
            Debug.Assert(textureIDs.Length == 4);
            Debug.Assert(startHeights.Length == 4);
            Debug.Assert(heightRanges.Length == 4);
            var outputSize = 2048;

            var detailTexture = new Bitmap[4];

            // Swap empty terrain textureIDs with default IDs
            for (var i = 0; i < textureIDs.Length; i++)
            {
                if (textureIDs[i] == UUID.Zero)
                    textureIDs[i] = DEFAULT_TERRAIN_DETAIL[i];
            }

            #region Texture Fetching
            for (var i = 0; i < 4; i++)
            {
                var textureDone = new AutoResetEvent(false);
                var textureID = textureIDs[i];

                instance.Client.Assets.RequestImage(textureID, TextureDownloadCallback(detailTexture, i, textureDone));

                textureDone.WaitOne(60 * 1000, false);
            }

            #endregion Texture Fetching

            // Fill in any missing textures with a solid color
            for (var i = 0; i < 4; i++)
            {
                if (detailTexture[i] == null)
                {
                    // Create a solid color texture for this layer
                    detailTexture[i] = new Bitmap(outputSize, outputSize, PixelFormat.Format24bppRgb);
                    using var gfx = Graphics.FromImage(detailTexture[i]);
                    using var brush = new SolidBrush(DEFAULT_TERRAIN_COLOR[i]);
                    gfx.FillRectangle(brush, 0, 0, outputSize, outputSize);
                }
                else if (detailTexture[i].Width != outputSize || detailTexture[i].Height != outputSize)
                {
                    detailTexture[i] = ResizeBitmap(detailTexture[i], 256, 256);
                }
            }

            #region Layer Map

            var diff = heightmap.GetLength(0) / RegionSize;
            var layermap = new float[RegionSize * RegionSize];

            for (var y = 0; y < heightmap.GetLength(0); y += diff)
            {
                for (var x = 0; x < heightmap.GetLength(1); x += diff)
                {
                    var newX = x / diff;
                    var newY = y / diff;
                    var height = heightmap[newX, newY];

                    var pctX = newX / 255f;
                    var pctY = newY / 255f;

                    // Use bilinear interpolation between the four corners of start height and
                    // height range to select the current values at this position
                    var startHeight = ImageUtils.Bilinear(
                        startHeights[0],
                        startHeights[2],
                        startHeights[1],
                        startHeights[3],
                        pctX, pctY);
                    startHeight = Utils.Clamp(startHeight, 0f, 255f);

                    var heightRange = ImageUtils.Bilinear(
                        heightRanges[0],
                        heightRanges[2],
                        heightRanges[1],
                        heightRanges[3],
                        pctX, pctY);
                    heightRange = Utils.Clamp(heightRange, 0f, 255f);

                    // Generate two frequencies of perlin noise based on our global position
                    // The magic values were taken from http://opensimulator.org/wiki/Terrain_Splatting
                    var vec = new Vector3
                    (
                        newX * 0.20319f,
                        newY * 0.20319f,
                        height * 0.25f
                    );

                    var lowFreq = Perlin.noise2(vec.X * 0.222222f, vec.Y * 0.222222f) * 6.5f;
                    var highFreq = Perlin.turbulence2(vec.X, vec.Y, 2f) * 2.25f;
                    var noise = (lowFreq + highFreq) * 2f;

                    // Combine the current height, generated noise, start height, and height range parameters, then scale all of it
                    var layer = ((height + noise - startHeight) / heightRange) * 4f;
                    if (Single.IsNaN(layer))
                        layer = 0f;
                    layermap[newY * RegionSize + newX] = Utils.Clamp(layer, 0f, 3f);
                }
            }

            #endregion Layer Map

            #region Texture Compositing
            var output = new Bitmap(outputSize, outputSize, PixelFormat.Format24bppRgb);
            var outputData = output.LockBits(new Rectangle(0, 0, outputSize, outputSize), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                // Get handles to all of the texture data arrays
                var datas = new BitmapData[]
                {
                    detailTexture[0].LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.ReadOnly, detailTexture[0].PixelFormat),
                    detailTexture[1].LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.ReadOnly, detailTexture[1].PixelFormat),
                    detailTexture[2].LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.ReadOnly, detailTexture[2].PixelFormat),
                    detailTexture[3].LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.ReadOnly, detailTexture[3].PixelFormat)
                };

                var comps = new int[]
                {
                    (datas[0].PixelFormat == PixelFormat.Format32bppArgb) ? 4 : 3,
                    (datas[1].PixelFormat == PixelFormat.Format32bppArgb) ? 4 : 3,
                    (datas[2].PixelFormat == PixelFormat.Format32bppArgb) ? 4 : 3,
                    (datas[3].PixelFormat == PixelFormat.Format32bppArgb) ? 4 : 3
                };

                var strides = new int[]
                {
                    datas[0].Stride,
                    datas[1].Stride,
                    datas[2].Stride,
                    datas[3].Stride
                };

                var scans = new IntPtr[]
                {
                    datas[0].Scan0,
                    datas[1].Scan0,
                    datas[2].Scan0,
                    datas[3].Scan0
                };

                var ratio = outputSize / RegionSize;

                for (var y = 0; y < outputSize; y++)
                {
                    for (var x = 0; x < outputSize; x++)
                    {
                        var layer = layermap[(y / ratio) * RegionSize + x / ratio];
                        var layerx = layermap[(y / ratio) * RegionSize + Math.Min(outputSize - 1, (x + 1)) / ratio];
                        var layerxx = layermap[(y / ratio) * RegionSize + Math.Max(0, (x - 1)) / ratio];
                        var layery = layermap[Math.Min(outputSize - 1, (y + 1)) / ratio * RegionSize + x / ratio];
                        var layeryy = layermap[(Math.Max(0, (y - 1)) / ratio) * RegionSize + x / ratio];

                        // Select two textures
                        var l0 = (int)Math.Floor(layer);
                        var l1 = Math.Min(l0 + 1, 3);

                        var ptrA = (byte*)scans[l0] + (y % 256) * strides[l0] + (x % 256) * comps[l0];
                        var ptrB = (byte*)scans[l1] + (y % 256) * strides[l1] + (x % 256) * comps[l1];
                        var ptrO = (byte*)outputData.Scan0 + y * outputData.Stride + x * 3;

                        float aB = *(ptrA + 0);
                        float aG = *(ptrA + 1);
                        float aR = *(ptrA + 2);

                        var lX = (int)Math.Floor(layerx);
                        var ptrX = (byte*)scans[lX] + (y % 256) * strides[lX] + (x % 256) * comps[lX];
                        var lXX = (int)Math.Floor(layerxx);
                        var ptrXX = (byte*)scans[lXX] + (y % 256) * strides[lXX] + (x % 256) * comps[lXX];
                        var lY = (int)Math.Floor(layery);
                        var ptrY = (byte*)scans[lY] + (y % 256) * strides[lY] + (x % 256) * comps[lY];
                        var lYY = (int)Math.Floor(layeryy);
                        var ptrYY = (byte*)scans[lYY] + (y % 256) * strides[lYY] + (x % 256) * comps[lYY];

                        float bB = *(ptrB + 0);
                        float bG = *(ptrB + 1);
                        float bR = *(ptrB + 2);

                        var layerDiff = layer - l0;
                        var xlayerDiff = layerx - layer;
                        var xxlayerDiff = layerxx - layer;
                        var ylayerDiff = layery - layer;
                        var yylayerDiff = layeryy - layer;
                        // Interpolate between the two selected textures
                        *(ptrO + 0) = (byte)Math.Floor(aB + layerDiff * (bB - aB) +
                            xlayerDiff * (*ptrX - aB) +
                            xxlayerDiff * (*(ptrXX) - aB) +
                            ylayerDiff * (*ptrY - aB) +
                            yylayerDiff * (*(ptrYY) - aB));
                        *(ptrO + 1) = (byte)Math.Floor(aG + layerDiff * (bG - aG) +
                            xlayerDiff * (*(ptrX + 1) - aG) +
                            xxlayerDiff * (*(ptrXX + 1) - aG) +
                            ylayerDiff * (*(ptrY + 1) - aG) +
                            yylayerDiff * (*(ptrYY + 1) - aG));
                        *(ptrO + 2) = (byte)Math.Floor(aR + layerDiff * (bR - aR) +
                            xlayerDiff * (*(ptrX + 2) - aR) +
                            xxlayerDiff * (*(ptrXX + 2) - aR) +
                            ylayerDiff * (*(ptrY + 2) - aR) +
                            yylayerDiff * (*(ptrYY + 2) - aR));
                    }
                }

                for (var i = 0; i < 4; i++)
                {
                    detailTexture[i].UnlockBits(datas[i]);
                    detailTexture[i].Dispose();
                }
            }

            layermap = null;
            output.UnlockBits(outputData);

            output.RotateFlip(RotateFlipType.Rotate270FlipNone);

            #endregion Texture Compositing

            return output;
        }

        private static TextureDownloadCallback TextureDownloadCallback(Bitmap[] detailTexture, int i, AutoResetEvent textureDone)
        {
            return (state, assetTexture) =>
            {
                if (state == TextureRequestState.Finished && assetTexture?.AssetData != null)
                {
                    using var image = J2kImage.FromBytes(assetTexture.AssetData).As<SKBitmap>();
                    detailTexture[i] = image.ToBitmap();
                }
                textureDone.Set();
            };
        }

        public static Bitmap ResizeBitmap(Bitmap b, int nWidth, int nHeight)
        {
            var result = new Bitmap(nWidth, nHeight);
            using (var g = Graphics.FromImage(result))
            {
                g.DrawImage(b, 0, 0, nWidth, nHeight);
            }
            b.Dispose();
            return result;
        }

        public static Bitmap TileBitmap(Bitmap b, int tiles)
        {
            var result = new Bitmap(b.Width * tiles, b.Width * tiles);
            using (var g = Graphics.FromImage(result))
            {
                for (var x = 0; x < tiles; x++)
                {
                    for (var y = 0; y < tiles; y++)
                    {
                        g.DrawImage(b, x * 256, y * 256, x * 256 + 256, y * 256 + 256);
                    }
                }
            }
            b.Dispose();
            return result;
        }

        public static Bitmap SplatSimple(float[,] heightmap)
        {
            const float BASE_HSV_H = 93f / 360f;
            const float BASE_HSV_S = 44f / 100f;
            const float BASE_HSV_V = 34f / 100f;

            var img = new Bitmap(256, 256);
            var bitmapData = img.LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                for (var y = 255; y >= 0; y--)
                {
                    for (var x = 0; x < 256; x++)
                    {
                        var normHeight = heightmap[x, y] / 255f;
                        normHeight = Utils.Clamp(normHeight, BASE_HSV_V, 1.0f);

                        var color = Color4.FromHSV(BASE_HSV_H, BASE_HSV_S, normHeight);

                        var ptr = (byte*)bitmapData.Scan0 + y * bitmapData.Stride + x * 3;
                        *(ptr + 0) = (byte)(color.B * 255f);
                        *(ptr + 1) = (byte)(color.G * 255f);
                        *(ptr + 2) = (byte)(color.R * 255f);
                    }
                }
            }

            img.UnlockBits(bitmapData);
            return img;
        }
    }
}
