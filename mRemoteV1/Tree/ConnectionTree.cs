﻿using Microsoft.VisualBasic;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Tools.Sorting;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using mRemoteNG.My;

namespace mRemoteNG.Tree
{
    public class ConnectionTree
    {
        private static TreeNode SetNodeToolTip_old_node = null;
        private static TreeNode treeNodeToBeSelected;
        private static TreeView _TreeView;

        public static TreeView TreeView
        {
            get { return _TreeView; }
            set { _TreeView = value; }
        }

        public static TreeNode SelectedNode
        {
            get
            { 
                return _TreeView.SelectedNode;
            }
            set
            {
                treeNodeToBeSelected = value;
                SelectNode();
            }
        }

        public static void DeleteSelectedNode()
        {
            try
            {
                if (SelectedNode == null)
                {
                    return;
                }

                if (Tree.ConnectionTreeNode.GetNodeType(SelectedNode) == TreeNodeType.Root)
                {
                    Runtime.MessageCollector.AddMessage(Messages.MessageClass.WarningMsg, "The root item cannot be deleted!");
                }
                else if (Tree.ConnectionTreeNode.GetNodeType(SelectedNode) == TreeNodeType.Container)
                {
                    if (Tree.ConnectionTreeNode.IsEmpty(SelectedNode) == false)
                    {
                        if (Interaction.MsgBox(string.Format(Language.strConfirmDeleteNodeFolder, SelectedNode.Text), (Microsoft.VisualBasic.MsgBoxStyle)(MsgBoxStyle.YesNo | MsgBoxStyle.Question), null) == MsgBoxResult.Yes)
                        {
                            SelectedNode.Remove();
                        }
                    }
                    else
                    {
                        if (Interaction.MsgBox(string.Format(Language.strConfirmDeleteNodeFolderNotEmpty, SelectedNode.Text), (Microsoft.VisualBasic.MsgBoxStyle)(MsgBoxStyle.YesNo | MsgBoxStyle.Question), null) == MsgBoxResult.Yes)
                        {
                            foreach (TreeNode tNode in SelectedNode.Nodes)
                            {
                                tNode.Remove();
                            }
                            SelectedNode.Remove();
                        }
                    }
                }
                else if (Tree.ConnectionTreeNode.GetNodeType(SelectedNode) == TreeNodeType.Connection)
                {
                    if (Interaction.MsgBox(string.Format(Language.strConfirmDeleteNodeConnection, SelectedNode.Text), (Microsoft.VisualBasic.MsgBoxStyle)(MsgBoxStyle.YesNo | MsgBoxStyle.Question), null) == MsgBoxResult.Yes)
                    {
                        SelectedNode.Remove();
                    }
                }
                else
                {
                    Runtime.MessageCollector.AddMessage(Messages.MessageClass.WarningMsg, "Tree item type is unknown so it cannot be deleted!");
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, "Deleting selected node failed" + Environment.NewLine + ex.Message, true);
            }
        }

        public static void StartRenameSelectedNode()
        {
            if (SelectedNode != null)
            {
                SelectedNode.BeginEdit();
            }
        }

        public static void FinishRenameSelectedNode(string newName)
        {
            ConnectionInfo connectionInfo = SelectedNode.Tag as ConnectionInfo;
            if (connectionInfo != null)
                Tree.ConnectionTreeNode.RenameNode(connectionInfo, newName);
        }

        public static void SetNodeToolTip(MouseEventArgs e, ToolTip tTip)
        {
            try
            {
                if (mRemoteNG.Settings.Default.ShowDescriptionTooltipsInTree)
                {
                    //Find the node under the mouse.
                    TreeNode new_node = _TreeView.GetNodeAt(e.X, e.Y);
                    if (new_node.Equals(SetNodeToolTip_old_node))
                    {
                        return;
                    }
                    SetNodeToolTip_old_node = new_node;

                    //See if we have a node.
                    if (SetNodeToolTip_old_node == null)
                    {
                        tTip.SetToolTip(_TreeView, "");
                    }
                    else
                    {
                        //Get this node's object data.
                        if (Tree.ConnectionTreeNode.GetNodeType(SetNodeToolTip_old_node) == TreeNodeType.Connection)
                        {
                            tTip.SetToolTip(_TreeView, (SetNodeToolTip_old_node.Tag as ConnectionInfo).Description);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, "SetNodeToolTip failed" + Environment.NewLine + ex.Message, true);
            }
        }

        public static void ExpandAllNodes()
        {
            TreeView.BeginUpdate();
            TreeView.ExpandAll();
            TreeView.EndUpdate();
        }

        public static void CollapseAllNodes()
        {
            TreeView.BeginUpdate();
            foreach (TreeNode treeNode in TreeView.Nodes[0].Nodes)
            {
                treeNode.Collapse(false);
            }
            TreeView.EndUpdate();
        }

        public static void MoveNodeDown()
        {
            try
            {
                if (SelectedNode != null)
                {
                    if (!(SelectedNode.NextNode == null))
                    {
                        TreeView.BeginUpdate();
                        TreeView.Sorted = false;

                        TreeNode newNode = (TreeNode)SelectedNode.Clone();
                        SelectedNode.Parent.Nodes.Insert(SelectedNode.Index + 2, newNode);
                        SelectedNode.Remove();
                        SelectedNode = newNode;

                        TreeView.EndUpdate();
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, "MoveNodeDown failed" + Environment.NewLine + ex.Message, true);
            }
        }

        public static void MoveNodeUp()
        {
            try
            {
                if (SelectedNode != null)
                {
                    if (!(SelectedNode.PrevNode == null))
                    {
                        TreeView.BeginUpdate();
                        TreeView.Sorted = false;

                        TreeNode newNode = (TreeNode)SelectedNode.Clone();
                        SelectedNode.Parent.Nodes.Insert(SelectedNode.Index - 1, newNode);
                        SelectedNode.Remove();
                        SelectedNode = newNode;

                        TreeView.EndUpdate();
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, "MoveNodeUp failed" + Environment.NewLine + ex.Message, true);
            }
        }

        public static void Sort(TreeNode treeNode, SortOrder sorting)
        {
            if (TreeView == null)
                return;

            TreeView.BeginUpdate();

            if (treeNode == null)
            {
                if (TreeView.Nodes.Count > 0)
                    treeNode = TreeView.Nodes[0];
                else
                    return;
            }
            else if (Tree.ConnectionTreeNode.GetNodeType(treeNode) == TreeNodeType.Connection)
            {
                treeNode = treeNode.Parent;
                if (treeNode == null)
                    return;
            }

            Sort(treeNode, new TreeNodeSorter(sorting));
            TreeView.EndUpdate();
        }

        private static void Sort(TreeNode treeNode, TreeNodeSorter nodeSorter)
        {
            // Adapted from http://www.codeproject.com/Tips/252234/ASP-NET-TreeView-Sort
            foreach (TreeNode childNode in treeNode.Nodes)
            {
                Sort(childNode, nodeSorter);
            }

            try
            {
                List<TreeNode> sortedNodes = new List<TreeNode>();
                TreeNode currentNode = null;
                while (treeNode.Nodes.Count > 0)
                {
                    foreach (TreeNode childNode in treeNode.Nodes)
                    {
                        if (currentNode == null || nodeSorter.Compare(childNode, currentNode) < 0)
                        {
                            currentNode = childNode;
                        }
                    }
                    treeNode.Nodes.Remove(currentNode);
                    sortedNodes.Add(currentNode);
                    currentNode = null;
                }

                foreach (TreeNode childNode in sortedNodes)
                {
                    treeNode.Nodes.Add(childNode);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, "Sort nodes failed" + Environment.NewLine + ex.Message, true);
            }
        }

        public static TreeNode Find(TreeNode treeNode, string searchFor)
        {
            TreeNode tmpNode = default(TreeNode);

            try
            {
                if (IsThisTheNodeWeAreSearchingFor(treeNode, searchFor))
                    return treeNode;

                foreach (TreeNode childNode in treeNode.Nodes)
                {
                    tmpNode = Find(childNode, searchFor);
                    if (!(tmpNode == null))
                    {
                        return tmpNode;
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, "Find node failed" + Environment.NewLine + ex.Message, true);
            }

            return null;
        }

        private static bool IsThisTheNodeWeAreSearchingFor(TreeNode treeNode, string searchFor)
        {
            return ((treeNode.Text.ToLower()).IndexOf(searchFor.ToLower()) + 1 > 0);
        }

        public static TreeNode Find(TreeNode treeNode, ConnectionInfo conInfo)
        {
            TreeNode tmpNode = default(TreeNode);
            try
            {
                if (treeNode.Tag == conInfo)
                    return treeNode;

                foreach (TreeNode childNode in treeNode.Nodes)
                {
                    tmpNode = Find(childNode, conInfo);
                    if (!(tmpNode == null))
                        return tmpNode;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, "Find node failed" + Environment.NewLine + ex.Message, true);
            }

            return null;
        }

        private delegate void ResetTreeDelegate();
        public static void ResetTree()
        {
            if (TreeView.InvokeRequired)
            {
                ResetTreeDelegate resetTreeDelegate = new ResetTreeDelegate(ResetTree);
                Windows.treeForm.Invoke(resetTreeDelegate);
            }
            else
            {
                TreeView.BeginUpdate();
                TreeView.Nodes.Clear();
                TreeView.Nodes.Add(Language.strConnections);
                TreeView.EndUpdate();
            }
        }

        private delegate void SelectNodeCB();
        private static void SelectNode()
        {
            if (_TreeView.InvokeRequired == true)
            {
                SelectNodeCB d = new SelectNodeCB(SelectNode);
                _TreeView.Invoke(d);
            }
            else
            {
                _TreeView.SelectedNode = treeNodeToBeSelected;
            }
        }
    }
}