/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/6/28
 * Time: 15:25
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Mib;
using WeifenLuo.WinFormsUI.Docking;

namespace Lextm.SharpSnmpLib.Browser
{
    /// <summary>
    /// Description of MibTreePanel.
    /// </summary>
    internal partial class MibTreePanel : DockContent
    {
        private bool _showNumber;

        public MibTreePanel()
        {
            InitializeComponent();
        }

        public IObjectRegistry Objects { get; set; }

        public Manager Manager { get; set; }

        public IProfileRegistry Profiles { get; set; }

        private void RefreshPanel(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker) (() => RefreshPanel(sender, e)));
                return;
            }

            ReloadableObjectRegistry repository = (ReloadableObjectRegistry)sender;
            treeView1.Nodes.Clear();
            TreeNode root = Wrap(repository.Tree.Root);
            foreach (TreeNode node in root.Nodes)
            {
                treeView1.Nodes.Add(node);
            }
        }

        private TreeNode Wrap(IDefinition definition)
        {
            string name = _showNumber ? string.Format("{0}({1})", definition.Name, definition.Value) : definition.Name;
            TreeNode node = new TreeNode(name)
                                {
                                    Tag = definition,
                                    ImageIndex = (int) definition.Type,
                                    SelectedImageIndex = (int) definition.Type,
                                    ToolTipText =
                                        new SearchResult(definition).AlternativeText + Environment.NewLine +
                                        definition.Value
                                };

            List<IDefinition> list = new List<IDefinition>(definition.Children);
            list.Sort(new DefinitionComparer());
            foreach (IDefinition def in list)
            {
                node.Nodes.Add(Wrap(def));
            }

            return node;
        }

        private static string TextualFormForGet(IDefinition def)
        {
            if (def.Type == DefinitionType.Scalar)
            {
                return new SearchResult(def).AlternativeText + ".0";
            }

            int index;
            using (FormIndex form = new FormIndex())
            {
                form.ShowDialog();
                index = form.Index;
            }

            return new SearchResult(def).AlternativeText + "." + index;
        }

        private static string TextualFormForGetNext(IDefinition def)
        {
            if (def.Type == DefinitionType.Scalar)
            {
                return new SearchResult(def).AlternativeText + ".0";
            }

            return new SearchResult(def).AlternativeText;
        }

        private void ActGetExecute(object sender, EventArgs e)
        {
            TraceSource source = new TraceSource("Browser");
            try
            {
                source.TraceInformation("==== Begin GET ====");
                Profiles.DefaultProfile.Get(Manager, TextualFormForGet(treeView1.SelectedNode.Tag as IDefinition));
            }
            catch (Exception ex)
            {
                source.TraceInformation(ex.ToString());
            }
            finally
            {
                source.TraceInformation("==== End GET ====");
                source.Flush();
                source.Close();
            }
        }

        private void ActGetUpdate(object sender, EventArgs e)
        {
            actGet.Enabled = ValidForGet(treeView1.SelectedNode);
        }

        private void ActSetExecute(object sender, EventArgs e)
        {
            TraceSource source = new TraceSource("Browser");
            try
            {
                ISnmpData data;
                using (FormSet form = new FormSet())
                {
                    form.OldVal = Profiles.DefaultProfile.GetValue(Manager,
                                                                   TextualFormForGet(
                                                                       treeView1.SelectedNode.Tag as IDefinition));
                    if (form.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    
                    if (form.IsString)
                    {
                        data = new OctetString(form.NewVal);
                    }
                    else
                    {
                        int result;
                        if (!int.TryParse(form.NewVal, out result))
                        {
                            MessageBox.Show("Value entered was not an Integer!", "SNMP Set Error",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        data = new Integer32(result);
                    }
                }

                source.TraceInformation("==== Begin SET ====");
                Profiles.DefaultProfile.Set(Manager,
                                                TextualFormForGet(treeView1.SelectedNode.Tag as IDefinition),
                                                data);
            }
            catch (Exception ex)
            {
                source.TraceInformation(ex.ToString());
            }
            finally
            {
                source.TraceInformation("==== End SET ====");
                source.Flush();
                source.Close();
            }
        }

        private void ActSetUpdate(object sender, EventArgs e)
        {
            actSet.Enabled = ValidForGet(treeView1.SelectedNode);
        }

        private static bool ValidForGet(TreeNode node)
        {
            if (node == null)
            {
                return false;
            }

            //  Scalar or Column. (see DefinitionType.cs)
            return node.ImageIndex == 2 || node.ImageIndex == 5;
        }

        private static bool ValidForGetNext(TreeNode node)
        {
            return node != null && node.Level > 0;
        }

        private void ActGetTableExecute(object sender, EventArgs e)
        {
            try
            {
                Profiles.DefaultProfile.GetTable(Manager, treeView1.SelectedNode.Tag as IDefinition);
            }
            catch (Exception ex)
            {
                TraceSource source = new TraceSource("Browser");
                source.TraceInformation(ex.ToString());
                source.Flush();
                source.Close();
            }
        }

        private void ActGetTableUpdate(object sender, EventArgs e)
        {
            actGetTable.Enabled = treeView1.SelectedNode != null && treeView1.SelectedNode.ImageIndex == 3;
        }

        private void TreeView1AfterSelect(object sender, TreeViewEventArgs e)
        {
            tslblOID.Text = ObjectIdentifier.Convert(((IDefinition) e.Node.Tag).GetNumericalForm());
            if (ValidForGet(e.Node))
            {
                ActGetExecute(sender, e);
            }
            else if (ValidForGetTable(e.Node))
            {
                ActGetTableExecute(sender, e);
            }
        }

        private static bool ValidForGetTable(TreeNode node)
        {
            return node != null && node.ImageIndex == 3;
        }

        private void ActGetNextExecute(object sender, EventArgs e)
        {
            TraceSource source = new TraceSource("Browser");
            try
            {
                source.TraceInformation("==== Begin GET NEXT ====");
                // TODO: don't use textual form in the future here.
                Profiles.DefaultProfile.GetNext(Manager, TextualFormForGetNext(treeView1.SelectedNode.Tag as IDefinition));
            }
            catch (Exception ex)
            {
                source.TraceInformation(ex.ToString());
            }
            finally
            {
                source.TraceInformation("==== End GET NEXT ====");
                source.Flush();
                source.Close();
            }
        }

        private void ActGetNextUpdate(object sender, EventArgs e)
        {
            actGetNext.Enabled = ValidForGetNext(treeView1.SelectedNode);
        }


/*
        private void ManualWalk(TreeNode node, bool first)
        {
            if (node != null)
            {
                try
                {
                    switch (node.ImageIndex)
                    {
                        case 3:
                            Profiles.DefaultProfile.GetTable(Manager, node.Tag as IDefinition);
                            break;
                        default:
                            if (ValidForGet(node))
                            {
                                Profiles.DefaultProfile.Get(Manager, TextualFormForGet(node.Tag as IDefinition));
                            }
                            else
                            {
                                //
                                // TODO: I would like to be able to put headings for the parent of the child nodes
                                //
                                ManualWalk(node.Nodes[0], false);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    TraceSource source = new TraceSource("Browser");
                    source.TraceInformation(ex.ToString());
                    source.Flush();
                    source.Close();
                }

                if (!first)
                {
                    ManualWalk(node.NextNode, false);
                }
            }
        }
*/

        private void MibTreePanel_Load(object sender, EventArgs e)
        {
            RefreshPanel(Objects, EventArgs.Empty);
            Objects.OnChanged += RefreshPanel;
        }

        private void ActNumberExecute(object sender, EventArgs e)
        {
            _showNumber = !_showNumber;
            RefreshPanel(Objects, EventArgs.Empty);
        }

        private void ActWalkExecute(object sender, EventArgs e)
        {
            TraceSource source = new TraceSource("Browser");
            try
            {
                source.TraceInformation("==== Begin WALK ====");
                Profiles.DefaultProfile.Walk(Manager, (treeView1.SelectedNode.Tag as IDefinition));                
            }
            catch (Exception ex)
            {
                source.TraceInformation(ex.ToString());
            }
            finally
            {
                source.TraceInformation("==== End WALK ====");
                source.Flush();
                source.Close();
            }
        }

        private void ActWalkUpdate(object sender, EventArgs e)
        {
            actWalk.Enabled = ValidForGetNext(treeView1.SelectedNode);
        }

        #region Nested type: DefinitionComparer

        private class DefinitionComparer: IComparer<IDefinition>
        {
            #region IComparer<IDefinition> Members

            public int Compare(IDefinition x, IDefinition y)
            {
                return x.Value.CompareTo(y.Value);
            }

            #endregion
        }

        #endregion
    }
}
