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
using System.Collections.Generic;
using System.Linq;

namespace METAbolt
{
    public class RingBufferProtection
    {
        private METAboltInstance instance;
        public int ringbuffmax = 20;
        public List<DateTime> ringbuffer = new List<DateTime>();

        public bool RingBuffer(METAboltInstance instance)
        {
            if (ringbuffmax == 0) return false;

            this.instance = instance;

            if (ringbuffer.Count > 0)
            {
                DateTime ltry = ringbuffer[0];

                TimeSpan tspn = DateTime.Now - ltry;

                if (tspn.TotalSeconds < 1.1)
                {
                    if (ringbuffer.Count == ringbuffmax)
                    {
                        instance.BlockChatIn = true;
                        return true;
                    }
                }
                else
                {
                    try
                    {
                        if (tspn.TotalSeconds < 2.1)
                        {
                            ringbuffer.RemoveAt(ringbuffer.Count() - 1);
                        }
                        else
                        {
                            ringbuffer.Clear();
                        }
                    }
                    catch { ; }
                }
            }

            ringbuffer.Add(DateTime.Now);

            SortDescending(ringbuffer);

            instance.BlockChatIn = false;
            return false;
        }

        public void SetBuffer(int bfr)
        {
            ringbuffmax = bfr;
        }

        private static List<DateTime> SortDescending(List<DateTime> list)
        {
            list.Sort((a, b) => b.CompareTo(a));
            return list;
        }
    }
}
