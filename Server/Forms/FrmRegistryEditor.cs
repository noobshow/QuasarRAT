using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using xServer.Core.Networking;
using xServer.Core.Utilities;

namespace xServer.Forms
{
    public partial class FrmRegistryEditor : Form
    {
        private readonly Client _connectClient;

        private readonly object locker = new object();

        public FrmRegistryEditor(Client c)
        {
            _connectClient = c;
            _connectClient.Value.FrmRe = this;

            InitializeComponent();
        }

        public TreeNode AddRootKey(RegSeekerMatch match)
        {
            TreeNode node = null;

            tvRegistryDirectory.Invoke((MethodInvoker)delegate
            {
                node = CreateNode(match.Key, match.Value, null);
                tvRegistryDirectory.Nodes.Add(node);

                node.Nodes.Add(new TreeNode());
            });

            return node;
        }

        public TreeNode AddRootKey(RegistryKey key)
        {
            TreeNode node = null;

            tvRegistryDirectory.Invoke((MethodInvoker)delegate
            {
                node = CreateNode(key.Name, key.Name, key);
                tvRegistryDirectory.Nodes.Add(node);

                node.Nodes.Add(new TreeNode());
            });

            return node;
        }

        private TreeNode CreateNode(string key, string text, object tag)
        {
            return new TreeNode()
            {
                Text = text,
                Name = key,
                Tag = tag
            };
        }

        public void AddKeyToTree(RegSeekerMatch match)
        {
            if (string.IsNullOrEmpty(match.Key))
            {
                // The key can be whitespace, but not null or empty...
                return;
            }

            TreeNode parent = GetParentTreeNode(match);

            // If the parent is null, it should be a root node.
            if (parent == null)
            {
                AddRootKey(match);
            }
            else
            {
                // ToDo: Add more on the object's tag.
                parent.Nodes.Add(CreateNode(match.Key, match.Key, match.Data));
            }
        }

        /// <summary>
        /// Using the RegSeekerMatch's name, obtain the parent TreeNode of the match, creating
        /// the TreeNodes if necessary.
        /// </summary>
        /// <param name="match">The match from which we obtain the corresponding TreeNode from.</param>
        /// <returns>Null if an invalid name is passed; The parent TreeNode for non-root matches; Returns
        /// itself if it is a root match.</returns>
        private TreeNode GetParentTreeNode(RegSeekerMatch match)
        {
            if (match.Key.Contains("/"))
            {
                // It might not be a root node.
                string[] nodePath = match.Key.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                // Only one valid node. Probably malformed or a root node.
                if (nodePath.Length < 2)
                {
                    return null;
                }
                else
                {
                    // Keep track of the last node to reference for traversal.
                    TreeNode lastNode = null;

                    // If the TreeView contains the first element in the node path, we
                    // won't have to create it. Otherwise, create it then set the last node
                    // to serve as our reference point.
                    if (tvRegistryDirectory.Nodes.ContainsKey(nodePath[0]))
                    {
                        lastNode = tvRegistryDirectory.Nodes[nodePath[0]];
                    }
                    else
                    {
                        // This node does not exist in the TreeView. Create it then add it.
                        lastNode = CreateNode(nodePath[0], nodePath[0], null);
                        tvRegistryDirectory.Nodes.Add(lastNode);
                    }

                    // Go through the rest of the node path.
                    for (int i = 1; i < nodePath.Length; i++)
                    {
                        // If the last node does have this entry in the path, just set
                        // the last node to the existing entry.
                        if (lastNode.Nodes.ContainsKey(nodePath[i]))
                        {
                            lastNode = tvRegistryDirectory.Nodes[nodePath[i]];
                        }
                        else
                        {
                            // If the last node does not contain the next item in the path,
                            // create the node and add it to the path.
                            TreeNode newNode = CreateNode(nodePath[i], nodePath[i], null);
                            lastNode.Nodes.Add(newNode);

                            lastNode = newNode;
                        }
                    }

                    return lastNode.Parent;
                }
            }
            else
            {
                // Likely a root node.
                return null;
            }
        }

        public void PopulateLstRegistryKeys(RegistryKey[] keys)
        {
            // Clear the ListView.
            for (int i = 0; i < lstRegistryKeys.Items.Count; i++)
            {
                RegistryKey key = lstRegistryKeys.Items[i].Tag as RegistryKey;
                if (key != null)
                {
                    key.Close();
                }
            }

            lstRegistryKeys.Clear();

            // If the array is not null, we have usable data.
            if (keys != null && keys.Length > 0)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    // To-Do: Use a custom ListViewItem for a better style.
                    lstRegistryKeys.Items.Add(new ListViewItem() { Text = keys[i].Name, Tag = keys[i] });
                }
            }
        }

        private void tvRegistryDirectory_Click(object sender, EventArgs e)
        {
            if ((tvRegistryDirectory.SelectedNode != null) && (tvRegistryDirectory.SelectedNode.Tag != null))
            {
                // Step One
                // Set a class-level variable based on the full RegistryKey path to the
                // RegistryKey we wish to obtain the value of.
                // To-Do: Currently, we do not use the full path to the RegistryKey for the TreeNode.
                //       Such behavior needs to be changed for this to work because we need the full path.
                // ---
                /*
                 Pseudo:
                 
                 class-level variable to get the value of (RegistryKey path) =
                 * tvRegistryDirectory.SelectedNode.Name;
                 */

                // Step Two
                // Send a packet containing the path to the RegistryKey to obtain the value of.
                // ---

                // Step Three
                // Upon a response, if the RegistryKey path does not match, do not bother attempting
                // to use the data. If it does, use this code below to populate the ListView with the
                // data that the client provided to us.
                // ---

                List<RegistryKey> KeysFromNode = new List<RegistryKey>();
                if (KeysFromNode.Count < 1)
                {
                    // If there aren't, send null to clear the ListView.
                    PopulateLstRegistryKeys(null);
                }
                else
                {
                    // If there are valid keys to use from the selected TreeNode, populate the ListView with it.
                    PopulateLstRegistryKeys(KeysFromNode.ToArray());
                }
            }
            else
            {
                // It is likely that the user clicked on either an empty direction or an invalid RegistryKey.
                // Sending null to this method will clear the ListView.
                PopulateLstRegistryKeys(null);
            }
        }

        private void FrmRegistryEditor_Load(object sender, EventArgs e)
        {
            // By passing no search arguments, we tell the client to provide us with a response of its root keys.
            new xServer.Core.Packets.ServerPackets.DoLoadRegistryKey(null).Execute(_connectClient);
        }

        private void FrmRegistryEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_connectClient.Value != null)
                _connectClient.Value.FrmRe = null;
        }

        private void tvRegistryDirectory_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {

        }

        private void tvRegistryDirectory_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void tvRegistryDirectory_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            // Before expansion of the node, prepare the first node with RegistryKeys.
            TreeNode parentNode = e.Node;

            // If nothing is there (yet).
            if (parentNode.FirstNode.Tag == null)
            {
                try
                {
                    tvRegistryDirectory.SuspendLayout();

                    // Send a packet to retrieve the data to use for the nodes.

                    parentNode.Nodes.Clear();
                    RegistryKey key = parentNode.Tag as RegistryKey;

                    if (key != null)
                    {
                        // Get sub-keys, sort them, then add them to the tree.

                    }
                }
                finally
                {
                    tvRegistryDirectory.ResumeLayout();
                }
            }
        }

        private void tvRegistryDirectory_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void tvRegistryDirectory_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void tvRegistryDirectory_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {

        }
    }
}