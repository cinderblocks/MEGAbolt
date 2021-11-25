﻿/*
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

namespace MEGAbolt.NetworkComm
{
    public class LoadURLEventArgs : EventArgs
    {
        public LoadURLEventArgs(string objectName, UUID objectID, UUID ownerID, bool ownerIsGroup, string message, string URL)
        {
            ObjectName = objectName;
            ObjectID = objectID;
            OwnerID = ownerID;
            OwnerIsGroup = ownerIsGroup;
            Message = message;
            url = URL;
        }

        public string ObjectName { get; }

        public UUID ObjectID { get; }

        public UUID OwnerID { get; }

        public bool OwnerIsGroup { get; }

        public string Message { get; }

        public string url { get; }
    }
}