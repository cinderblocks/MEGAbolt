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

namespace MEGAbolt
{
    public class ChatBufferItem
    {
        public ChatBufferItem()
        {
            // do nothing
        }

        public ChatBufferItem(DateTime timestamp, string text, ChatBufferTextStyle style)
        {
            Timestamp = timestamp;
            Text = text;
            Style = style;
        }

        public ChatBufferItem(DateTime timestamp, string text, ChatBufferTextStyle style, string fromname)
        {
            Timestamp = timestamp;
            Text = text;
            Style = style;
            FromName = fromname; 
        }

        public ChatBufferItem(DateTime timestamp, string text, string link, ChatBufferTextStyle style, string fromname)
        {
            Timestamp = timestamp;
            Text = text;
            Link = link;
            Style = style;
            FromName = fromname;
        }

        /// <summary>
        /// Constructor for an item in the ChatBuffer
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="text"></param>
        /// <param name="style"></param>
        /// <param name="fromname"></param>
        /// <param name="fromuuid">UUID of the agent sending the message</param>
        public ChatBufferItem(DateTime timestamp, string text, ChatBufferTextStyle style, string name, UUID uuid)
        {
            Timestamp = timestamp;
            Text = text;
            Style = style;
            FromName = name;
            FromUUID = uuid;
        }
        
        public DateTime Timestamp { get; set; }

        public string Text { get; set; }

        public string Link { get; set; }

        public string FromName { get; set; }

        public ChatBufferTextStyle Style { get; set; }

        /// <summary>
        /// UUID of the object sending the message, strangely this is not the FromAgentId but the IMSessionId in a message from object
        /// </summary>
        public UUID FromUUID { get; set; }
    }

    public enum ChatBufferTextStyle
    {
        Normal,
        StatusBlue,
        StatusBold,
        StatusBrown,
        StatusDarkBlue,
        StatusGray,
        LindenChat,
        ObjectChat,
        OwnerSay,
        StartupTitle,
        RegionSay,
        Error,
        Alert,
        LoginReply
    }
}
