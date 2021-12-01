/**
 * Radegast Metaverse Client
 * Copyright(c) 2009-2014, Radegast Development Team
 * Copyright(c) 2016-2020, Sjofn, LLC
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
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Rendering;

namespace MEGAbolt.Rendering
{
    public class RenderTerrain : SceneObject
    {
        MEGAboltInstance Instance;
        GridClient Client => Instance.Client;

        public bool Modified = true;
        float[,] heightTable = new float[256, 256];
        Face terrainFace;
        uint[] terrainIndices;
        ColorVertex[] terrainVertices;
        int terrainTexture = -1;
        bool fetchingTerrainTexture = false;
        Bitmap terrainImage = null;
        int terrainVBO = -1;
        int terrainIndexVBO = -1;
        bool terrainVBOFailed = false;
        bool terrainInProgress = false;
        bool terrainTextureNeedsUpdate = false;
        float terrainTimeSinceUpdate = RenderSettings.MinimumTimeBetweenTerrainUpdated + 1f; // Update terrain om first run
        MeshmerizerR renderer;
        Simulator sim => Instance.Client.Network.CurrentSim;

        public RenderTerrain(MEGAboltInstance instance)
        {
            Instance = instance;
            renderer = new MeshmerizerR();
        }

        public void ResetTerrain()
        {
            ResetTerrain(true);
        }

        public void ResetTerrain(bool removeImage)
        {
            if (terrainImage != null)
            {
                terrainImage.Dispose();
                terrainImage = null;
            }

            if (terrainVBO != -1)
            {
                Compat.DeleteBuffer(terrainVBO);
                terrainVBO = -1;
            }

            if (terrainIndexVBO != -1)
            {
                Compat.DeleteBuffer(terrainIndexVBO);
                terrainIndexVBO = -1;
            }

            if (removeImage)
            {
                if (terrainTexture != -1)
                {
                    GL.DeleteTexture(terrainTexture);
                    terrainTexture = -1;
                }
            }

            fetchingTerrainTexture = false;
            Modified = true;
        }

        private void UpdateTerrain()
        {
            if (sim == null || sim.Terrain == null) return;

            ThreadPool.QueueUserWorkItem(sync =>
            {
                int step = 1;

                for (int x = 0; x < 256; x += step)
                {
                    for (int y = 0; y < 256; y += step)
                    {
                        float z = 0;
                        int patchNr = ((int)x / 16) * 16 + (int)y / 16;
                        if (sim.Terrain[patchNr] != null
                            && sim.Terrain[patchNr].Data != null)
                        {
                            float[] data = sim.Terrain[patchNr].Data;
                            z = data[(int)x % 16 * 16 + (int)y % 16];
                        }
                        heightTable[x, y] = z;
                    }
                }

                terrainFace = renderer.TerrainMesh(heightTable, 0f, 255f, 0f, 255f);
                terrainVertices = new ColorVertex[terrainFace.Vertices.Count];
                for (int i = 0; i < terrainFace.Vertices.Count; i++)
                {
                    byte[] part = Utils.IntToBytes(i);
                    terrainVertices[i] = new ColorVertex()
                    {
                        Vertex = terrainFace.Vertices[i],
                        Color = new Color4b()
                        {
                            R = part[0],
                            G = part[1],
                            B = part[2],
                            A = 253 // terrain picking
                        }
                    };
                }
                terrainIndices = new uint[terrainFace.Indices.Count];
                for (int i = 0; i < terrainIndices.Length; i++)
                {
                    terrainIndices[i] = terrainFace.Indices[i];
                }
                terrainInProgress = false;
                Modified = false;
                terrainTextureNeedsUpdate = true;
                terrainTimeSinceUpdate = 0f;
            });
        }

        void UpdateTerrainTexture()
        {
            if (!fetchingTerrainTexture)
            {
                fetchingTerrainTexture = true;
                ThreadPool.QueueUserWorkItem(sync =>
                {
                    Simulator currentSim = Client.Network.CurrentSim;
                    terrainImage = TerrainSplat.Splat(Instance, heightTable,
                        new UUID[] { currentSim.TerrainDetail0, currentSim.TerrainDetail1, currentSim.TerrainDetail2, currentSim.TerrainDetail3 },
                        new float[] { currentSim.TerrainStartHeight00, currentSim.TerrainStartHeight01, currentSim.TerrainStartHeight10, currentSim.TerrainStartHeight11 },
                        new float[] { currentSim.TerrainHeightRange00, currentSim.TerrainHeightRange01, currentSim.TerrainHeightRange10, currentSim.TerrainHeightRange11 });

                    fetchingTerrainTexture = false;
                    terrainTextureNeedsUpdate = false;
                });
            }
        }

        public bool TryGetVertex(int indeex, out ColorVertex picked)
        {
            if (indeex < terrainVertices.Length)
            {
                picked = terrainVertices[indeex];
                return true;
            }
            picked = new ColorVertex();
            return false;
        }

    }
}
