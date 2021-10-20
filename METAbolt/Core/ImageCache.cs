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

using System;
using OpenMetaverse;

namespace METAbolt
{
    public class ImageCache
    {
        private SafeDictionary<UUID, System.Drawing.Image> cache = new SafeDictionary<UUID, System.Drawing.Image>();

        public ImageCache()
        {

        }

        public bool ContainsImage(UUID imageID)
        {
            return cache.ContainsKey(imageID);
        }

        public void AddImage(UUID imageID, System.Drawing.Image image)
        {
            try
            {
                if (!ContainsImage(imageID))
                    cache.Add(imageID, image);
            }
            catch (Exception ex)
            {
                Logger.Log("Image cache: " + ex.Message, Helpers.LogLevel.Error);    
            }
        }

        public void RemoveImage(UUID imageID)
        {
            try
            {
                if (ContainsImage(imageID))
                    cache.Remove(imageID);
            }
            catch (Exception ex)
            {
                Logger.Log("Image cache: " + ex.Message, Helpers.LogLevel.Error);    
            }
        }

        public System.Drawing.Image GetImage(UUID imageID)
        {
            return cache[imageID];
        }
    }
}
