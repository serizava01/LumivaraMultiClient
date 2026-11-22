using LumivaraMultiClient.Controls;
using LumivaraMultiClient.Models;
using LumivaraMultiClient.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LumivaraMultiClient.Forms
{
    public partial class MainForm : Form
    {
        // =========================================================
        // Client Data
        // =========================================================

        private readonly Dictionary<string, GameTabControl> runningClients =
            new Dictionary<string, GameTabControl>();

        private readonly Dictionary<string, Panel> clientItems =
            new Dictionary<string, Panel>();

        private readonly Dictionary<string, Label> clientStatusLabels =
            new Dictionary<string, Label>();

        private readonly Dictionary<string, AccountProfile> profiles =
            new Dictionary<string, AccountProfile>();

        private string selectedProfileId = null;

        private readonly string Url = "https://play.nightcamp.app/";

        private const int MaxClientsInCombo = 10;

        // =========================================================
        // Main UI
        // =========================================================

        private Panel headerPanel;
        private Panel sidebarPanel;
        private Panel contentPanel;
        private Panel footerPanel;

        private FlowLayoutPanel clientListPanel;

        private Label lblSelectedClient;
        private Label lblRunningCount;
        private Label lblClientStatus;
        private Label lbURlnow;

        private Button btnHideToTray;
        private Button btnWiki;
        private Button btnUpdates;
        private Button btnGithub;

        private ComboBox nummonitor;
        private CheckBox chkDiagnosticLog;

        private bool isLoadingDiagnosticLog;
        private int maxClients = 3;
        private bool isLoadingClientCount = false;
        private bool isHiddenToTray = false;
        private bool isClosingConfirmed = false;

        // =========================================================
        // Context Menu & Tray
        // =========================================================

        private ContextMenuStrip clientContextMenu;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        // =========================================================
        // Config (ใช้โฟลเดอร์ Data ร่วมกับ WebViewManager)
        // =========================================================

        private readonly string basePath = WebViewManager.BaseDataPath;

        private string ConfigFilePath
        {
            get { return Path.Combine(basePath, "config.txt"); }
        }

        // =========================================================
        // Constructor
        // =========================================================

        public MainForm()
        {
            InitializeComponent();

            InitializeMainUI();
            InitializeContextMenu();
            InitializeTrayIcon();

            InitializeClientCountSelector();
            LoadClientCount();
            LoadDiagnosticLogSetting();

            CreateClientList();
            LoadSavedInstances();

            // ตรวจจับการย่อ/ขยายหน้าต่างเพื่อสลับโหมดประหยัดทรัพยากร
            this.Resize += (s, e) => UpdateAllClientsActiveState();
        }

        // =========================================================
        // Main UI
        // =========================================================

        private void InitializeMainUI()
        {
            Text = "Online Web Browser - Multi Client Manager";

            Size = new Size(1280, 800);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;

            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            lblSelectedClient = new Label
            {
                Text = "ยังไม่ได้เลือก Client",
                AutoSize = true,
                Location = new Point(15, 13),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            lblRunningCount = new Label
            {
                Text = "Running: 0",
                AutoSize = true,
                Location = new Point(200, 13),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.DimGray
            };

            lblClientStatus = new Label
            {
                Text = "Monitor",
                AutoSize = true,
                Location = new Point(350, 13),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Black
            };

            nummonitor = new ComboBox
            {
                Location = new Point(400, 10),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            lbURlnow = new Label
            {
                Text = "URL Game:  " + Url,
                AutoSize = true,
                Location = new Point(600, 13),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.DimGray
            };

            chkDiagnosticLog = new CheckBox
            {
                Text = "เก็บLog",
                AutoSize = false,
                Width = 85,
                Height = 25,
                TextAlign = ContentAlignment.MiddleRight,
                Checked = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(headerPanel.ClientSize.Width - 95, 10),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Black,
                BackColor = headerPanel.BackColor,
                Cursor = Cursors.Hand
            };
            chkDiagnosticLog.CheckedChanged += ChkDiagnosticLog_CheckedChanged;

            headerPanel.Controls.Add(lblSelectedClient);
            headerPanel.Controls.Add(lblRunningCount);
            headerPanel.Controls.Add(lblClientStatus);
            headerPanel.Controls.Add(nummonitor);
            headerPanel.Controls.Add(lbURlnow);
            headerPanel.Controls.Add(chkDiagnosticLog);
            chkDiagnosticLog.BringToFront();

            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(35, 35, 35)
            };

            Label lblClients = new Label
            {
                Text = "CLIENTS",
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(15, 12, 0, 0),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(35, 35, 35)
            };

            clientListPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(35, 35, 35)
            };

            Panel bottomButtonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 210,
                BackColor = Color.FromArgb(35, 35, 35)
            };

            btnHideToTray = CreateSidebarButton("ซ่อนไปยัง Tray");
            btnHideToTray.Location = new Point(5, 5);
            btnHideToTray.Click += delegate { HideToSystemTray(); };

            btnWiki = CreateSidebarButton("Wiki");
            btnWiki.Location = new Point(5, 45);
            btnWiki.Click += delegate { LinkStart("https://play.nightcamp.app/wiki/index.html"); };

            btnUpdates = CreateSidebarButton("Update");
            btnUpdates.Location = new Point(5, 85);
            btnUpdates.Click += delegate { MessageBox.Show("ไม่ได้ตั้งไว้"); };

            btnGithub = CreateSidebarButton("GitHub");
            btnGithub.Location = new Point(5, 125);
            btnGithub.Click += delegate { LinkStart("https://github.com/serizava01"); };

            bottomButtonPanel.Controls.Add(btnHideToTray);
            bottomButtonPanel.Controls.Add(btnWiki);
            bottomButtonPanel.Controls.Add(btnUpdates);
            bottomButtonPanel.Controls.Add(btnGithub);

            sidebarPanel.Controls.Add(clientListPanel);
            sidebarPanel.Controls.Add(bottomButtonPanel);
            sidebarPanel.Controls.Add(lblClients);

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20)
            };

            ShowEmptyContent();

            footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            Controls.Add(contentPanel);
            Controls.Add(sidebarPanel);
            Controls.Add(footerPanel);
            Controls.Add(headerPanel);
        }

        private Button CreateSidebarButton(string text)
        {
            Button button = new Button
            {
                Text = text,
                Width = 210,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(55, 55, 55),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private void ShowEmptyContent()
        {
            contentPanel.Controls.Clear();

            Label label = new Label
            {
                Text = "เลือก Client จากด้านซ้าย",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14f),
                ForeColor = Color.Gray,
                BackColor = Color.FromArgb(20, 20, 20)
            };

            contentPanel.Controls.Add(label);
        }

        // =========================================================
        // Active State Management
        // =========================================================

        private void UpdateAllClientsActiveState()
        {
            bool isAppActive = !isHiddenToTray && this.WindowState != FormWindowState.Minimized;

            foreach (KeyValuePair<string, GameTabControl> pair in runningClients)
            {
                bool shouldBeActive = isAppActive && (pair.Key == selectedProfileId);
                pair.Value.SetActive(shouldBeActive);
            }
        }

        // =========================================================
        // Client Count Selector
        // =========================================================

        private void InitializeClientCountSelector()
        {
            if (nummonitor == null)
                return;

            isLoadingClientCount = true;
            nummonitor.Items.Clear();

            for (int i = 1; i <= MaxClientsInCombo; i++)
            {
                nummonitor.Items.Add(i.ToString());
            }

            nummonitor.SelectedIndex = maxClients - 1;
            nummonitor.SelectedIndexChanged += Nummonitor_SelectedIndexChanged;
            isLoadingClientCount = false;
        }

        private void LoadClientCount()
        {
            maxClients = 3;

            if (!File.Exists(ConfigFilePath))
            {
                SetClientCountComboValue();
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(ConfigFilePath);

                foreach (string line in lines)
                {
                    string value = line.Trim();

                    if (!value.StartsWith("MaxClients=", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string numberText = value.Substring("MaxClients=".Length).Trim();
                    int savedCount;

                    if (int.TryParse(numberText, out savedCount))
                    {
                        if (savedCount >= 1 && savedCount <= MaxClientsInCombo)
                            maxClients = savedCount;
                    }
                    break;
                }
            }
            catch
            {
                maxClients = 3;
            }

            SetClientCountComboValue();
        }

        private void SetClientCountComboValue()
        {
            if (nummonitor == null)
                return;

            if (maxClients < 1) maxClients = 1;
            if (maxClients > MaxClientsInCombo) maxClients = MaxClientsInCombo;

            isLoadingClientCount = true;
            nummonitor.SelectedIndex = maxClients - 1;
            isLoadingClientCount = false;
        }

        private void Nummonitor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoadingClientCount || nummonitor == null || nummonitor.SelectedIndex < 0)
                return;

            int newCount = nummonitor.SelectedIndex + 1;

            if (runningClients.Count > newCount)
            {
                MessageBox.Show(
                    "ตอนนี้มี Client กำลังเปิดอยู่ " + runningClients.Count + " จอ\n\n" +
                    "กรุณาปิด Client ที่เกินจำนวนก่อน\nจึงจะลดจำนวน Client ได้",
                    "ไม่สามารถลดจำนวน Client",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                SetClientCountComboValue();
                return;
            }

            maxClients = newCount;
            RebuildClientList();
            SaveConfig();
        }

        private void RebuildClientList()
        {
            if (clientListPanel == null)
                return;

            string oldSelectedProfileId = selectedProfileId;
            clientListPanel.SuspendLayout();

            try
            {
                clientListPanel.Controls.Clear();
                clientItems.Clear();
                clientStatusLabels.Clear();

                for (int i = 1; i <= maxClients; i++)
                {
                    string profileId = "Online_Web_Browser" + i;

                    if (!profiles.ContainsKey(profileId))
                    {
                        profiles[profileId] = new AccountProfile
                        {
                            ProfileId = profileId,
                            Title = "Client " + i,
                            TargetUrl = Url
                        };
                    }

                    CreateClientItem(profiles[profileId]);
                }
            }
            finally
            {
                clientListPanel.ResumeLayout();
            }

            foreach (KeyValuePair<string, GameTabControl> pair in runningClients)
            {
                if (clientStatusLabels.ContainsKey(pair.Key))
                    UpdateClientStatus(pair.Key, true);
            }

            if (!string.IsNullOrEmpty(oldSelectedProfileId))
            {
                if (clientItems.ContainsKey(oldSelectedProfileId))
                {
                    SelectClient(oldSelectedProfileId);
                }
                else
                {
                    selectedProfileId = null;
                    lblSelectedClient.Text = "ยังไม่ได้เลือก Client";
                    ShowEmptyContent();
                    UpdateAllClientsActiveState();
                }
            }
        }

        private void CreateClientList()
        {
            for (int i = 1; i <= 100; i++)
            {
                string profileId = "Online_Web_Browser" + i;

                if (profiles.ContainsKey(profileId))
                    continue;

                profiles[profileId] = new AccountProfile
                {
                    ProfileId = profileId,
                    Title = "Client " + i,
                    TargetUrl = Url
                };
            }

            RebuildClientList();
        }

        private void CreateClientItem(AccountProfile profile)
        {
            Panel item = new Panel
            {
                Width = 195,
                Height = 55,
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Color.FromArgb(45, 45, 45),
                Cursor = Cursors.Hand,
                Tag = profile.ProfileId
            };

            Label status = new Label
            {
                Text = "●",
                AutoSize = true,
                Location = new Point(10, 17),
                Font = new Font("Segoe UI Symbol", 11f),
                ForeColor = Color.Red,
                Cursor = Cursors.Hand,
                Tag = item
            };

            Label title = new Label
            {
                Text = profile.Title,
                AutoSize = false,
                Width = 150,
                Height = 20,
                Location = new Point(35, 8),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Tag = item,
                AutoEllipsis = true
            };

            Label state = new Label
            {
                Text = "Stopped",
                AutoSize = true,
                Location = new Point(35, 30),
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.Silver,
                Cursor = Cursors.Hand,
                Tag = item
            };

            item.Controls.Add(status);
            item.Controls.Add(title);
            item.Controls.Add(state);

            clientItems[profile.ProfileId] = item;
            clientStatusLabels[profile.ProfileId] = status;

            item.Click += ClientItem_Click;
            item.DoubleClick += ClientItem_DoubleClick;
            item.MouseUp += ClientItem_MouseUp;

            status.Click += ClientChild_Click;
            title.Click += ClientChild_Click;
            state.Click += ClientChild_Click;

            status.DoubleClick += ClientChild_DoubleClick;
            title.DoubleClick += ClientChild_DoubleClick;
            state.DoubleClick += ClientChild_DoubleClick;

            status.MouseUp += ClientChild_MouseUp;
            title.MouseUp += ClientChild_MouseUp;
            state.MouseUp += ClientChild_MouseUp;

            clientListPanel.Controls.Add(item);
        }

        private Panel GetClientItemFromControl(Control control)
        {
            if (control == null)
                return null;

            Panel item = control.Tag as Panel;
            return item ?? (control.Parent as Panel);
        }

        private void ClientChild_Click(object sender, EventArgs e)
        {
            Panel item = GetClientItemFromControl(sender as Control);
            if (item != null) SelectClientFromPanel(item);
        }

        private void ClientChild_DoubleClick(object sender, EventArgs e)
        {
            Panel item = GetClientItemFromControl(sender as Control);
            if (item != null) StartClientFromPanel(item);
        }

        private void ClientChild_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            Control control = sender as Control;
            Panel item = GetClientItemFromControl(control);
            if (item == null) return;

            SelectClientFromPanel(item);
            clientContextMenu.Show(control, e.Location);
        }

        private void ClientItem_Click(object sender, EventArgs e)
        {
            Panel item = sender as Panel;
            if (item != null) SelectClientFromPanel(item);
        }

        private void ClientItem_DoubleClick(object sender, EventArgs e)
        {
            Panel item = sender as Panel;
            if (item != null) StartClientFromPanel(item);
        }

        private void SelectClientFromPanel(Panel item)
        {
            string profileId = item != null ? item.Tag as string : null;
            if (!string.IsNullOrEmpty(profileId)) SelectClient(profileId);
        }

        private void StartClientFromPanel(Panel item)
        {
            string profileId = item != null ? item.Tag as string : null;
            if (string.IsNullOrEmpty(profileId)) return;

            if (runningClients.ContainsKey(profileId))
            {
                SelectClient(profileId);
                return;
            }

            StartClientInstance(profileId);
        }

        private void ClientItem_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            Panel item = sender as Panel;
            if (item == null) return;

            SelectClientFromPanel(item);
            clientContextMenu.Show(item, e.Location);
        }

        private void SelectClient(string profileId)
        {
            if (!profiles.ContainsKey(profileId))
                return;

            selectedProfileId = profileId;

            foreach (KeyValuePair<string, Panel> pair in clientItems)
            {
                pair.Value.BackColor = (pair.Key == profileId)
                    ? Color.FromArgb(70, 70, 70)
                    : Color.FromArgb(45, 45, 45);
            }

            AccountProfile profile = profiles[profileId];
            lblSelectedClient.Text = profile.Title;

            ShowClientGame(profileId);
            UpdateAllClientsActiveState();
        }

        private void ShowClientGame(string profileId)
        {
            contentPanel.SuspendLayout();

            try
            {
                while (contentPanel.Controls.Count > 0)
                {
                    Control c = contentPanel.Controls[0];
                    contentPanel.Controls.RemoveAt(0);
                    if (c is GameTabControl) continue;
                    try { c.Dispose(); } catch { }
                }

                if (!runningClients.ContainsKey(profileId))
                {
                    Label label = new Label
                    {
                        Text = "Client ยังไม่ได้เปิด\n\nดับเบิลคลิก Client เพื่อเปิด",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        ForeColor = Color.Gray,
                        BackColor = Color.FromArgb(20, 20, 20),
                        Font = new Font("Segoe UI", 12f)
                    };

                    contentPanel.Controls.Add(label);
                    return;
                }

                GameTabControl gameControl = runningClients[profileId];
                gameControl.Dock = DockStyle.Fill;

                if (gameControl.Parent != contentPanel)
                    contentPanel.Controls.Add(gameControl);

                gameControl.BringToFront();
            }
            finally
            {
                contentPanel.ResumeLayout();
            }
        }

        private void StartClientInstance(string profileId)
        {
            if (!profiles.ContainsKey(profileId))
                return;

            if (runningClients.ContainsKey(profileId))
            {
                SelectClient(profileId);
                return;
            }

            AccountProfile profile = profiles[profileId];

            try
            {
                GameTabControl gameControl = new GameTabControl(profile);
                gameControl.Dock = DockStyle.Fill;

                runningClients.Add(profileId, gameControl);
                UpdateClientStatus(profileId, true);
                SelectClient(profileId);
                UpdateAppTitle();
                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "ไม่สามารถเปิด " + profile.Title + " ได้\n\n" + ex.Message,
                    "Start Client Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void StopClientInstance(string profileId)
        {
            if (!runningClients.ContainsKey(profileId))
                return;

            GameTabControl gameControl = runningClients[profileId];

            if (gameControl.Parent == contentPanel)
                contentPanel.Controls.Remove(gameControl);

            try { gameControl.Dispose(); } catch { }

            runningClients.Remove(profileId);
            UpdateClientStatus(profileId, false);

            if (selectedProfileId == profileId)
            {
                selectedProfileId = null;
                lblSelectedClient.Text = "ยังไม่ได้เลือก Client";
                ShowEmptyContent();
            }

            UpdateAllClientsActiveState();
            UpdateAppTitle();
            SaveConfig();
        }

        private void UpdateClientStatus(string profileId, bool active)
        {
            if (!clientStatusLabels.ContainsKey(profileId))
                return;

            clientStatusLabels[profileId].ForeColor = active ? Color.LimeGreen : Color.Red;

            if (!clientItems.ContainsKey(profileId))
                return;

            Panel item = clientItems[profileId];
            if (item.Controls.Count >= 3 && item.Controls[2] is Label state)
            {
                state.Text = active ? "Running" : "Stopped";
            }
        }

        // =========================================================
        // Context Menu & Delete / Edit Profile
        // =========================================================

        private void InitializeContextMenu()
        {
            clientContextMenu = new ContextMenuStrip();

            ToolStripMenuItem startItem = new ToolStripMenuItem("เปิด Client");
            startItem.Click += delegate
            {
                if (!string.IsNullOrEmpty(selectedProfileId))
                    StartClientInstance(selectedProfileId);
            };

            ToolStripMenuItem stopItem = new ToolStripMenuItem("ปิด Client");
            stopItem.Click += delegate
            {
                if (!string.IsNullOrEmpty(selectedProfileId))
                    StopClientInstance(selectedProfileId);
            };

            ToolStripMenuItem settingsItem = new ToolStripMenuItem("ตั้งค่า Client...");
            settingsItem.Click += EditSelectedClient_Click;

            ToolStripSeparator separator = new ToolStripSeparator();

            ToolStripMenuItem deleteItem = new ToolStripMenuItem("ลบข้อมูลล็อกอินของจอนี้");
            deleteItem.Click += DeleteSelectedClient_Click;

            clientContextMenu.Items.Add(startItem);
            clientContextMenu.Items.Add(stopItem);
            clientContextMenu.Items.Add(new ToolStripSeparator());
            clientContextMenu.Items.Add(settingsItem);
            clientContextMenu.Items.Add(separator);
            clientContextMenu.Items.Add(deleteItem);
        }

        private void EditSelectedClient_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedProfileId) ||
                !profiles.ContainsKey(selectedProfileId))
                return;

            AccountProfile profile = profiles[selectedProfileId];

            using (ClientSettingsForm dialog = new ClientSettingsForm(profile, Url))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                bool urlChanged = !string.Equals(
                    profile.TargetUrl, dialog.ClientUrl, StringComparison.OrdinalIgnoreCase);
                bool titleChanged = !string.Equals(
                    profile.Title, dialog.ClientTitle, StringComparison.Ordinal);

                if (!urlChanged && !titleChanged)
                    return;

                profile.Title = dialog.ClientTitle;
                profile.TargetUrl = dialog.ClientUrl;

                RefreshClientItemUI(profile);

                if (selectedProfileId == profile.ProfileId)
                    lblSelectedClient.Text = profile.Title;

                if (urlChanged && runningClients.ContainsKey(profile.ProfileId))
                {
                    GameTabControl gameControl = runningClients[profile.ProfileId];
                    if (gameControl != null)
                    {
                        gameControl.NavigateTo(profile.TargetUrl);
                    }
                }

                SaveConfig();
            }
        }

        private void RefreshClientItemUI(AccountProfile profile)
        {
            if (profile == null || !clientItems.ContainsKey(profile.ProfileId))
                return;

            Panel item = clientItems[profile.ProfileId];
            if (item.Controls.Count >= 2 && item.Controls[1] is Label titleLabel)
                titleLabel.Text = profile.Title;
        }

        private void DeleteSelectedClient_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedProfileId) || !profiles.ContainsKey(selectedProfileId))
                return;

            AccountProfile profile = profiles[selectedProfileId];

            DialogResult result = MessageBox.Show(
                "แน่ใจว่าต้องการลบข้อมูลล็อกอินของ " + profile.Title + " หรือไม่?",
                "ยืนยันการลบข้อมูล",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
                return;

            StopClientInstance(profile.ProfileId);
            DeleteProfileDataFolder(profile.ProfileId);
            SaveConfig();
        }

        private void DeleteProfileDataFolder(string profileId)
        {
            string profileFolder = WebViewManager.GetProfilePath(profileId);

            if (string.IsNullOrEmpty(profileFolder) || !Directory.Exists(profileFolder))
                return;

            try
            {
                Directory.Delete(profileFolder, true);
            }
            catch
            {
                Task.Run(async delegate
                {
                    await Task.Delay(2000);
                    try
                    {
                        if (Directory.Exists(profileFolder))
                            Directory.Delete(profileFolder, true);
                    }
                    catch { }
                });
            }
        }

        // =========================================================
        // Load & Save Config
        // =========================================================

        private void LoadSavedInstances()
        {
            List<string> activeProfiles = new List<string>();

            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(ConfigFilePath);

                    foreach (string rawLine in lines)
                    {
                        string line = rawLine.Trim();
                        if (string.IsNullOrEmpty(line)) continue;

                        if (line.StartsWith("Profile=", StringComparison.OrdinalIgnoreCase))
                        {
                            string payload = line.Substring("Profile=".Length);
                            string[] parts = payload.Split('|');
                            if (parts.Length >= 1)
                            {
                                string profileId = parts[0].Trim();
                                if (!profiles.ContainsKey(profileId)) continue;

                                AccountProfile p = profiles[profileId];
                                if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
                                    p.Title = parts[1];
                                if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                                    p.TargetUrl = parts[2];
                            }
                            continue;
                        }

                        if (line.StartsWith("Running=", StringComparison.OrdinalIgnoreCase))
                        {
                            string profileId = line.Substring("Running=".Length).Trim();
                            if (!string.IsNullOrEmpty(profileId) &&
                                profiles.ContainsKey(profileId) &&
                                !activeProfiles.Contains(profileId))
                            {
                                activeProfiles.Add(profileId);
                            }
                        }
                    }
                }
                catch { }
            }

            foreach (KeyValuePair<string, AccountProfile> pair in profiles)
                RefreshClientItemUI(pair.Value);

            if (activeProfiles.Count == 0)
                activeProfiles.Add("Online_Web_Browser1");

            foreach (string profileId in activeProfiles)
                StartClientInstance(profileId);

            SaveConfig();
        }

        private void LoadDiagnosticLogSetting()
        {
            bool enabled = true;

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    foreach (string line in File.ReadAllLines(ConfigFilePath))
                    {
                        string value = line.Trim();
                        if (value.StartsWith("DiagnosticLogging=", StringComparison.OrdinalIgnoreCase))
                        {
                            bool savedValue;
                            if (bool.TryParse(value.Substring("DiagnosticLogging=".Length).Trim(), out savedValue))
                                enabled = savedValue;
                            break;
                        }
                    }
                }
            }
            catch { }

            isLoadingDiagnosticLog = true;
            chkDiagnosticLog.Checked = enabled;
            isLoadingDiagnosticLog = false;
            WebViewManager.DiagnosticLoggingEnabled = enabled;
        }

        private void ChkDiagnosticLog_CheckedChanged(object sender, EventArgs e)
        {
            if (isLoadingDiagnosticLog || chkDiagnosticLog == null)
                return;

            WebViewManager.DiagnosticLoggingEnabled = chkDiagnosticLog.Checked;
            SaveConfig();

            if (chkDiagnosticLog.Checked)
            {
                WebViewManager.WriteDiagnosticLog("System", "Diagnostic logging ENABLED by user.");
            }
        }

        private void SaveConfig()
        {
            try
            {
                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                List<string> configLines = new List<string>();
                configLines.Add("MaxClients=" + maxClients);
                configLines.Add("DiagnosticLogging=" +
                    (chkDiagnosticLog != null && chkDiagnosticLog.Checked));

                foreach (KeyValuePair<string, AccountProfile> pair in profiles)
                {
                    AccountProfile p = pair.Value;
                    if (p == null) continue;

                    string defaultTitle = "Client " + GetClientNumber(p.ProfileId);
                    bool titleChanged = !string.Equals(p.Title, defaultTitle, StringComparison.Ordinal);
                    bool urlChanged = !string.Equals(p.TargetUrl, Url, StringComparison.OrdinalIgnoreCase);

                    if (!titleChanged && !urlChanged)
                        continue;

                    configLines.Add("Profile=" + p.ProfileId
                        + "|" + (p.Title ?? "")
                        + "|" + (p.TargetUrl ?? ""));
                }

                foreach (KeyValuePair<string, GameTabControl> pair in runningClients)
                    configLines.Add("Running=" + pair.Key);

                string tempPath = ConfigFilePath + ".tmp";
                File.WriteAllLines(tempPath, configLines.ToArray(), Encoding.UTF8);

                if (File.Exists(ConfigFilePath))
                    File.Delete(ConfigFilePath);
                File.Move(tempPath, ConfigFilePath);
            }
            catch { }
        }

        private static int GetClientNumber(string profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return 0;
            const string prefix = "Online_Web_Browser";
            if (!profileId.StartsWith(prefix, StringComparison.Ordinal)) return 0;
            string num = profileId.Substring(prefix.Length);
            int n;
            return int.TryParse(num, out n) ? n : 0;
        }

        private void UpdateAppTitle()
        {
            int count = runningClients.Count;
            Text = "Online Web Browser (" + count + " Client(s) Running)";

            if (lblRunningCount != null)
                lblRunningCount.Text = "Running: " + count;
        }

        // =========================================================
        // Tray
        // =========================================================

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();

            ToolStripMenuItem showMenuItem = new ToolStripMenuItem("แสดงหน้าต่างเกม");
            showMenuItem.Click += delegate { ShowFromSystemTray(); };

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("ปิดโปรแกรมทั้งหมด");
            exitMenuItem.Click += delegate { ExitApplication(); };

            trayMenu.Items.Add(showMenuItem);
            trayMenu.Items.Add(exitMenuItem);

            Icon appIcon = null;

            try
            {
                if (Properties.Resources.sync_24dp_E3E3E3_FILL0_wght400_GRAD0_opsz24 != null)
                {
                    using (MemoryStream ms =
                        new MemoryStream(Properties.Resources.sync_24dp_E3E3E3_FILL0_wght400_GRAD0_opsz24))
                    {
                        appIcon = new Icon(ms);
                    }
                }
            }
            catch
            {
                appIcon = SystemIcons.Application;
            }

            if (appIcon == null)
                appIcon = SystemIcons.Application;

            Icon = appIcon;

            trayIcon = new NotifyIcon
            {
                Text = "Online Web Browser",
                Icon = appIcon,
                ContextMenuStrip = trayMenu,
                Visible = false
            };

            trayIcon.DoubleClick += delegate { ShowFromSystemTray(); };
        }

        private void HideToSystemTray()
        {
            if (trayIcon == null)
                return;

            isHiddenToTray = true;
            trayIcon.Visible = true;
            Hide();
            UpdateAllClientsActiveState();

            trayIcon.ShowBalloonTip(
                2000,
                "Online Web Browser",
                "โปรแกรมกำลังทำงานอยู่เบื้องหลัง",
                ToolTipIcon.Info
            );
        }

        private void ShowFromSystemTray()
        {
            if (trayIcon == null)
                return;

            isHiddenToTray = false;
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
            trayIcon.Visible = false;
            UpdateAllClientsActiveState();
        }

        // =========================================================
        // Close & Exit
        // =========================================================

        private void CloseAllClients()
        {
            List<GameTabControl> clients = new List<GameTabControl>(runningClients.Values);
            runningClients.Clear();

            foreach (GameTabControl gameControl in clients)
            {
                try
                {
                    if (gameControl.Parent != null)
                        gameControl.Parent.Controls.Remove(gameControl);
                }
                catch { }

                try { gameControl.Dispose(); } catch { }
            }
        }

        private void ExitApplication()
        {
            isClosingConfirmed = true;
            SaveConfig();
            CloseAllClients();

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }

            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!isClosingConfirmed &&
                e.CloseReason == CloseReason.UserClosing &&
                runningClients.Count > 0)
            {
                DialogResult r = MessageBox.Show(
                    "มี Client กำลังรันอยู่ " + runningClients.Count + " จอ\n\n" +
                    "ต้องการปิดโปรแกรมทั้งหมดหรือไม่?\n" +
                    "(ถ้าต้องการซ่อนไป Tray ให้กดปุ่ม 'ซ่อนไปยัง Tray' ก่อน)",
                    "ยืนยันปิดโปรแกรม",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (r != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            SaveConfig();
            CloseAllClients();

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }

            base.OnFormClosing(e);
        }

        public void LinkStart(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "ไม่สามารถเปิดเบราว์เซอร์ได้: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {


        }
    }
}