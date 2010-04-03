﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Lextm.SharpSnmpLib.Browser
{
    internal partial class AgentProfilePanel : DockContent
    {
        public AgentProfilePanel()
        {
            InitializeComponent();
        }

        public IProfileRegistry Profiles { get; set; }

        private void AgentProfilePanel_Load(object sender, EventArgs e)
        {
            Profiles.LoadProfiles();
            UpdateView(this, EventArgs.Empty);
            Profiles.OnChanged += UpdateView;
        }

        private void UpdateView(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            foreach (AgentProfile profile in Profiles.Profiles)
            {
                string display = profile.Name.Length != 0 ? profile.Name : profile.Agent.ToString();

                ListViewItem item = new ListViewItem(new[] { display, profile.Agent.ToString() });
                listView1.Items.Add(item);
                item.Tag = profile;

                switch (profile.VersionCode)
                {
                    case VersionCode.V1:
                        {
                            item.Group = listView1.Groups["lvgV1"];
                            break;
                        }
                    case VersionCode.V2:
                        {
                            item.Group = listView1.Groups["lvgV2"];
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                //
                // Lets make the default Agent bold
                //
                if (profile == Profiles.DefaultProfile)
                {
                    item.Font = new Font(listView1.Font, FontStyle.Bold);
                }

                item.ToolTipText = profile.Agent.ToString();
            }
        }

        private void actDelete_Update(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1 && Profiles.DefaultProfile == listView1.SelectedItems[0].Tag as AgentProfile)
            {
                actDelete.Enabled = false;
            }
            else
            {
                actDelete.Enabled = listView1.SelectedItems.Count == 1;
            }
        }

        private void actEdit_Update(object sender, EventArgs e)
        {
            actEdit.Enabled = listView1.SelectedItems.Count == 1;
        }

        private void actDefault_Update(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1 && Profiles.DefaultProfile == listView1.SelectedItems[0].Tag as AgentProfile)
            {
                actDefault.Enabled = false;
            }
            else
            {
                actDefault.Enabled = listView1.SelectedItems.Count == 1;
            }
        }

        private void actDefault_Execute(object sender, EventArgs e)
        {
            Profiles.DefaultProfile = listView1.SelectedItems[0].Tag as AgentProfile;
            Profiles.SaveProfiles();

            //
            // Update view for new default agent
            //
            UpdateView(null, null);
        }

        private void actionList1_Update(object sender, EventArgs e)
        {
            tslblDefault.Text = "Default agent is " + Profiles.DefaultProfile.Name;
        }

        private void actDelete_Execute(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to remove this item", "Confirmation", MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Profiles.DeleteProfile(((AgentProfile) listView1.SelectedItems[0].Tag));
                Profiles.SaveProfiles();
            }
            catch (BrowserException ex)
            {
                TraceSource source = new TraceSource("Browser");
                source.TraceInformation(ex.Message);
                source.Flush();
                source.Close();
            }
        }

        private void actAdd_Execute(object sender, EventArgs e)
        {
            using (FormProfile editor = new FormProfile(null))
            {
                if (editor.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    Profiles.AddProfile(new AgentProfile(Guid.NewGuid(), editor.VersionCode,
                                                         new IPEndPoint(editor.IP, editor.Port), editor.GetCommunity,
                                                         editor.SetCommunity, editor.AgentName,
                                                         editor.AuthenticationPassphrase, editor.PrivacyPassphrase,
                                                         editor.AuthenticationMethod, editor.PrivacyMethod,
                                                         editor.UserName));
                    Profiles.SaveProfiles();
                }
                catch (BrowserException ex)
                {
                    TraceSource source = new TraceSource("Browser");
                    source.TraceInformation(ex.Message);
                    source.Flush();
                    source.Close();
                }
            }
        }

        private void actEdit_Execute(object sender, EventArgs e)
        {
            AgentProfile profile = listView1.SelectedItems[0].Tag as AgentProfile;
            using (FormProfile editor = new FormProfile(profile))
            {
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    //Profiles.ReplaceProfile(new AgentProfile(profile.Id, editor.VersionCode, new IPEndPoint(editor.IP, editor.Port), editor.GetCommunity, editor.SetCommunity, editor.AgentName));
                    //Profiles.SaveProfiles();
                }
            }
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextAgentMenu.Show(listView1, e.Location);
            }
        }
    }
}