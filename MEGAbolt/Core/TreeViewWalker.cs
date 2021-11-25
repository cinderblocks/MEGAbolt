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
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using OpenMetaverse;

//Thanks to Josh Smith
//http://www.codeproject.com/Articles/12952/TreeViewWalker-Simplifying-Recursion

namespace MEGAbolt
{
    /// <summary>
    /// Provides a generic mechanism for navigating the nodes in a TreeView control.  Call the ProcessTree method to 
    /// start the navigation process for an entire TreeView.  Call ProcessBranch to navigate only a subset of a TreeView's nodes.
    /// The ProcessNode event will fire for every node in the tree or branch, unless the processing is aborted before reaching the last node.
    /// </summary>
    public class TreeViewWalker
    {
        #region Data

        private GridClient client;
        private MEGAboltInstance instance;
        private bool stopProcessing = false;

        #endregion // Data

        #region Constructors

        /// <summary>
        /// Creates an instance which references the specified TreeView.
        /// </summary>
        /// <param name="treeView">The TreeView to navigate.</param>
        public TreeViewWalker(TreeView treeView)
        {
            TreeView = treeView;
        }

        #endregion // Constructors

        #region Public Interface

        #region ProcessNode [event]

        /// <summary>
        /// This event is raised when the TreeViewWalker navigates to a TreeNode in a TreeView.
        /// </summary>
        public event ProcessNodeEventHandler ProcessNode;

        #endregion // ProcessNode [event]

        #region ProcessBranch

        /// <summary>
        /// Navigates the node branch which starts with the specified node and fires the ProcessNode event for every TreeNode it encounters.
        /// The TreeNode passed to this method does not have to belong to the TreeView assigned to the TreeView property.
        /// </summary>
        /// <param name="rootNode"></param>
        public void ProcessBranch(TreeNode rootNode)
        {
            if (rootNode == null)
                throw new ArgumentNullException("rootNode");

            // Reset the abort flag in case it was previously set.
            stopProcessing = false;

            WalkNodes(rootNode);
        }

        #endregion // ProcessBranch

        #region ProcessTree

        /// <summary>
        /// Navigates the TreeView and fires the ProcessNode event for every TreeNode it encounters.
        /// </summary>
        public void ProcessTree()
        {
            if (TreeView == null)
                throw new InvalidOperationException("The TreeViewWalker must reference a TreeView when ProcessTree is called.");

            foreach (TreeNode node in TreeView.Nodes)
            {
                ProcessBranch(node);
                if (stopProcessing)
                    break;
            }
        }

        #endregion // ProcessTree

        #region TreeView

        /// <summary>
        /// Gets/sets the TreeView control to navigate.
        /// </summary>
        public TreeView TreeView { get; set; }

        #endregion // TreeView

        #endregion // Public Interface

        #region Protected Interface

        #region OnProcessNode

        /// <summary>
        /// Raises the ProcessNode event.
        /// </summary>
        /// <param name="e">The event argument.</param>
        protected virtual void OnProcessNode(ProcessNodeEventArgs e)
        {
            var handler = ProcessNode;
            handler?.Invoke(this, e);
        }

        #endregion // OnProcessNode

        #endregion // Protected Interface

        #region Private Helpers

        #region WalkNodes

        private bool WalkNodes(TreeNode node)
        {
            // Fire the ProcessNode event.
            var args = ProcessNodeEventArgs.CreateInstance(node);
            OnProcessNode(args);

            // Cache the value of ProcessSiblings since ProcessNodeEventArgs is a singleton.
            var processSiblings = args.ProcessSiblings;

            if (args.StopProcessing)
            {
                stopProcessing = true;
            }
            else if (args.ProcessDescendants)
            {
                for (var i = 0; i < node.Nodes.Count; ++i)
                    if (!WalkNodes(node.Nodes[i]) || stopProcessing)
                        break;
            }

            return processSiblings;
        }

        #endregion // WalkNodes

        #endregion // Private Helpers

        public void LoadInventory(MEGAboltInstance instance, UUID folderID)
        {
            this.instance = instance;
            client = this.instance.Client;
            var rootFolder = client.Inventory.Store.RootFolder;
            var contents = client.Inventory.Store.GetContents(folderID);
            if (folderID != client.Inventory.Store.RootFolder.UUID)
            {
                var array = TreeView.Nodes.Find(folderID.ToString(), true);
                if (array.Length <= 0) { return; }

                var nodes = array[0].Nodes;
                nodes.Clear();
                if (contents.Count == 0)
                {
                    nodes.Add(UUID.Zero.ToString(), "(empty)");
                    nodes[UUID.Zero.ToString()].Tag = "empty";
                    nodes[UUID.Zero.ToString()].ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                }
                else
                {
                    var list = client.Network.CurrentSim.ObjectsPrimitives.FindAll(delegate(Primitive prim)
                    {
                        bool result;
                        try
                        {
                            result = (prim.ParentID == instance.Client.Self.LocalID);
                        }
                        catch
                        {
                            result = false;
                        }
                        return result;
                    });
                    foreach (var current in contents)
                    {
                        var key = current.UUID.ToString();
                        var flag = current is InventoryFolder;
                        try
                        {
                            var text = string.Empty;
                            if (!flag)
                            {
                                var inventoryItem = (InventoryItem)current;
                                var wearableType = client.Appearance.IsItemWorn(inventoryItem);
                                if (wearableType != WearableType.Invalid)
                                {
                                    text = " (WORN)";
                                }
                                var lhs = UUID.Zero;
                                foreach (var current2 in list.Where(current2 => current2.NameValues != null))
                                {
                                    for (var i = 0; i < current2.NameValues.Length; i++)
                                    {
                                        if (current2.NameValues[i].Name == "AttachItemID")
                                        {
                                            lhs = (UUID)current2.NameValues[i].Value.ToString();
                                            if (lhs == inventoryItem.UUID)
                                            {
                                                text = " (WORN)";
                                                break;
                                            }
                                        }
                                    }
                                }
                                nodes.Add(key, current.Name + text);
                                nodes[key].Tag = current;
                                if (text == " (WORN)")
                                {
                                    nodes[key].ForeColor = Color.RoyalBlue;
                                }
                                var empty = string.Empty;
                                var inventoryType = inventoryItem.InventoryType;
                                switch (inventoryType)
                                {
                                    case InventoryType.Texture:
                                        nodes[key].ImageKey = "Texture";
                                        continue;
                                    case InventoryType.Sound:
                                    case (InventoryType)4:
                                    case (InventoryType)5:
                                    case InventoryType.Category:
                                    case InventoryType.RootCategory:
                                        break;
                                    case InventoryType.CallingCard:
                                        nodes[key].ImageKey = "CallingCard";
                                        continue;
                                    case InventoryType.Landmark:
                                        nodes[key].ImageKey = "LM";
                                        continue;
                                    case InventoryType.Object:
                                        nodes[key].ImageKey = "Objects";
                                        continue;
                                    case InventoryType.Notecard:
                                        nodes[key].ImageKey = "Notecard";
                                        continue;
                                    case InventoryType.LSL:
                                        nodes[key].ImageKey = "Script";
                                        continue;
                                    default:
                                        if (inventoryType == InventoryType.Snapshot)
                                        {
                                            nodes[key].ImageKey = "Snapshots";
                                            continue;
                                        }
                                        if (inventoryType == InventoryType.Wearable)
                                        {
                                            nodes[key].ImageKey = "Wearable";
                                            continue;
                                        }
                                        break;
                                }
                                nodes[key].ImageKey = "Gear";
                            }
                            else
                            {
                                nodes.Add(key, current.Name);
                                nodes[key].Tag = current;
                                nodes[key].ImageKey = "ClosedFolder";
                                nodes[key].Nodes.Add(null, "(loading...)").ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            else
            {
                TreeView.Nodes.Clear();
                var treeNode = TreeView.Nodes.Add(rootFolder.UUID.ToString(), "My Inventory");
                treeNode.Tag = rootFolder;
                treeNode.ImageKey = "OpenFolder";
                if (contents.Count == 0)
                {
                    treeNode.Nodes.Add(UUID.Zero.ToString(), "(empty)");
                    treeNode.Nodes[UUID.Zero.ToString()].Tag = "empty";
                    treeNode.Nodes[UUID.Zero.ToString()].ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                }
                else
                {
                    var list = client.Network.CurrentSim.ObjectsPrimitives.FindAll(delegate(Primitive prim)
                    {
                        bool result;
                        try
                        {
                            result = (prim.ParentID == instance.Client.Self.LocalID);
                        }
                        catch
                        {
                            result = false;
                        }
                        return result;
                    });
                    foreach (var current in contents)
                    {
                        var key = current.UUID.ToString();
                        var flag = current is InventoryFolder;
                        try
                        {
                            var text = string.Empty;
                            if (!flag)
                            {
                                var inventoryItem = (InventoryItem)current;
                                var wearableType = client.Appearance.IsItemWorn(inventoryItem);
                                if (wearableType != WearableType.Invalid)
                                {
                                    text = " (WORN)";
                                }
                                var lhs = UUID.Zero;
                                foreach (var current2 in list.Where(current2 => current2.NameValues != null))
                                {
                                    for (var i = 0; i < current2.NameValues.Length; i++)
                                    {
                                        if (current2.NameValues[i].Name == "AttachItemID")
                                        {
                                            lhs = (UUID)current2.NameValues[i].Value.ToString();
                                            if (lhs == inventoryItem.UUID)
                                            {
                                                text = " (WORN)";
                                                break;
                                            }
                                        }
                                    }
                                }
                                treeNode.Nodes.Add(key, current.Name + text);
                                treeNode.Nodes[key].Tag = current;
                                if (text == " (WORN)")
                                {
                                    treeNode.Nodes[key].ForeColor = Color.RoyalBlue;
                                }
                                var empty = string.Empty;
                                var inventoryType = inventoryItem.InventoryType;
                                switch (inventoryType)
                                {
                                    case InventoryType.Texture:
                                        treeNode.Nodes[key].ImageKey = "Texture";
                                        continue;
                                    case InventoryType.Sound:
                                    case (InventoryType)4:
                                    case (InventoryType)5:
                                    case InventoryType.Category:
                                    case InventoryType.RootCategory:
                                        break;
                                    case InventoryType.CallingCard:
                                        treeNode.Nodes[key].ImageKey = "CallingCard";
                                        continue;
                                    case InventoryType.Landmark:
                                        treeNode.Nodes[key].ImageKey = "LM";
                                        continue;
                                    case InventoryType.Object:
                                        treeNode.Nodes[key].ImageKey = "Objects";
                                        continue;
                                    case InventoryType.Notecard:
                                        treeNode.Nodes[key].ImageKey = "Notecard";
                                        continue;
                                    case InventoryType.LSL:
                                        treeNode.Nodes[key].ImageKey = "Script";
                                        continue;
                                    case InventoryType.Snapshot:
                                        treeNode.Nodes[key].ImageKey = "Snapshots";
                                        continue;
                                    case InventoryType.Wearable:
                                        treeNode.Nodes[key].ImageKey = "Wearable";
                                        continue;
                                    case InventoryType.Unknown:
                                        break;
                                    case InventoryType.Attachment:
                                        break;
                                    case InventoryType.Animation:
                                        break;
                                    case InventoryType.Gesture:
                                        break;
                                    case InventoryType.Mesh:
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                treeNode.Nodes[key].ImageKey = "Gear";
                            }
                            else
                            {
                                treeNode.Nodes.Add(key, current.Name);
                                treeNode.Nodes[key].Tag = current;
                                treeNode.Nodes[key].ImageKey = "ClosedFolder";
                                treeNode.Nodes[key].Nodes.Add(null, "(loading...)").ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    treeNode.Expand();
                }
            }
        }
    }
}