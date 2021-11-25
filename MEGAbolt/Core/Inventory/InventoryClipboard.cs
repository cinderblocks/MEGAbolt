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

using System.Collections.Generic;
using System.Windows.Forms;
using OpenMetaverse;

namespace MEGAbolt
{
    public class InventoryClipboard
    {
        private GridClient client;
        private bool cut = false;

        public InventoryClipboard(GridClient client)
        {
            this.client = client;
        }

        public void SetClipboardNode(TreeNode itemNode, bool ccut)
        {
            CurrentClipNode = itemNode;
            CurrentClipItem = (InventoryBase)itemNode.Tag;

            cut = ccut;

            if (ccut)
            {
                if (CurrentClipNode.Parent != null)
                {
                    if (CurrentClipNode.Parent.Nodes.Count == 1)
                        CurrentClipNode.Parent.Collapse();
                }

                CurrentClipNode.Remove();
            }
        }

        public void PasteTo(TreeNode pasteNode)
        {
            if (CurrentClipNode == null) return;

            InventoryBase pasteio = (InventoryBase)pasteNode.Tag;

            if (CurrentClipItem is InventoryFolder folder)
            {
                if (cut)
                {
                    if (pasteio is InventoryFolder)
                    {
                        client.Inventory.MoveFolder(folder.UUID, pasteio.UUID);
                        pasteNode.Nodes.Add(CurrentClipNode);
                    }
                    else if (pasteio is InventoryItem)
                    {
                        client.Inventory.MoveFolder(folder.UUID, pasteio.ParentUUID);
                        pasteNode.Parent.Nodes.Add(CurrentClipNode);
                    }
                }
                else
                {
                    if (pasteio is InventoryFolder)
                    {
                        UUID destfolder = client.Inventory.CreateFolder(pasteio.UUID, folder.Name, FolderType.None);

                        List<InventoryBase> contents = client.Inventory.Store.GetContents(folder.UUID);

                        foreach (InventoryItem item in contents)
                        {
                            client.Inventory.RequestCopyItem(item.UUID, destfolder, item.Name, item.OwnerID, (InventoryBase _) => { });
                        }
                    }
                    else if (pasteio is InventoryItem)
                    {
                        UUID destfolder = client.Inventory.CreateFolder(pasteio.ParentUUID, folder.Name, FolderType.None);

                        List<InventoryBase> contents = client.Inventory.Store.GetContents(folder.UUID);

                        foreach (InventoryItem pitem in contents)
                        {
                            client.Inventory.RequestCopyItem(pitem.UUID, destfolder, pitem.Name, pitem.OwnerID, (InventoryBase _) => { });
                        }
                    }
                }

                CurrentClipNode.EnsureVisible();
                CurrentClipNode = null;
                CurrentClipItem = null;
            }
            else if (CurrentClipItem is InventoryItem item)
            {
                if (cut)
                {
                    if (pasteio is InventoryFolder)
                    {
                        client.Inventory.MoveItem(item.UUID, pasteio.UUID, item.Name);
                        pasteNode.Nodes.Add(CurrentClipNode);
                    }
                    else if (pasteio is InventoryItem)
                    {
                        client.Inventory.MoveItem(item.UUID, pasteio.ParentUUID, item.Name);
                        pasteNode.Parent.Nodes.Add(CurrentClipNode);
                    }
                }
                else
                {
                    if (pasteio is InventoryFolder)
                    {
                        client.Inventory.RequestCopyItem(item.UUID, pasteio.UUID, item.Name, item.OwnerID, (InventoryBase _) => { });
                    }
                    else if (pasteio is InventoryItem)
                    {
                        client.Inventory.RequestCopyItem(item.UUID, pasteio.ParentUUID, item.Name, item.OwnerID, (InventoryBase _) => { });
                    }
                }

                CurrentClipNode.EnsureVisible();
                CurrentClipNode = null;
                CurrentClipItem = null;
            }
        }

        public InventoryBase CurrentClipItem { get; private set; }

        public TreeNode CurrentClipNode { get; private set; }
    }
}