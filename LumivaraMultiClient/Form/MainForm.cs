using LumivaraMultiClient.Controls;
using LumivaraMultiClient.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LumivaraMultiClient.Forms
{
    public partial class MainForm : Form
    {
        // =========================================================
        // Settings
        // =========================================================

    

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

        private Button btnAddClient;
        private Button btnHideToTray;
        private Button btnWiki;
        private Button btnUpdates;
        private Button btnGithub;
        private ComboBox nummonitor;

        private int maxClients = 10;
        private bool isLoadingClientCount = false;


        // =========================================================
        // Context Menu
        // =========================================================

        private ContextMenuStrip clientContextMenu;

        // =========================================================
        // Tray
        // =========================================================

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        // =========================================================
        // Config
        // =========================================================

        private readonly string basePath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "LumivaraMultiClient"
        );

        private string ConfigFilePath
        {
            get
            {
                return Path.Combine(
                    basePath,
                    "config.txt"
                );
            }
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

            CreateClientList();
            LoadSavedInstances();
        }

        // =========================================================
        // Main UI
        // =========================================================

        private void InitializeMainUI()
        {
            Text =
                "Lumivara Online - Multi Client Manager";

            Size =
                new Size(
                    1280,
                    800
                );

            MinimumSize =
                new Size(
                    900,
                    600
                );

            StartPosition =
                FormStartPosition.CenterScreen;

            // =====================================================
            // HEADER
            // =====================================================

            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(
                    240,
                    240,
                    240
                )
            };

            lblSelectedClient = new Label
            {
                Text =
                    "ยังไม่ได้เลือก Client",

                AutoSize = true,

                Location =
                    new Point(
                        15,
                        13
                    ),

                Font =
                    new Font(
                        "Segoe UI",
                        10f,
                        FontStyle.Bold
                    )
            };
            

            lblRunningCount = new Label
            {
                Text =
                    "Running: 0",

                AutoSize = true,

                Location =
                    new Point(
                        200,
                        13
                    ),

                Font =
                    new Font(
                        "Segoe UI",
                        9f
                    ),

                ForeColor =
                    Color.DimGray
            };
            lblClientStatus = new Label
            {
                Text =
                    "Monitor",
                AutoSize = true,
                Location =
                    new Point(
                        350,
                        13
                    ),
                Font =
                    new Font(
                        "Segoe UI",
                        9f
                    ),
                ForeColor =
                    Color.DimGray
            };
            nummonitor = new ComboBox
            {
                Location = new Point(400, 10),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            headerPanel.Controls.Add(lblSelectedClient);
            headerPanel.Controls.Add(lblRunningCount);
            headerPanel.Controls.Add(lblClientStatus);
            headerPanel.Controls.Add(nummonitor);

            // =====================================================
            // SIDEBAR
            // =====================================================

            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,

                Width = 220,

                BackColor =
                    Color.FromArgb(
                        35,
                        35,
                        35
                    )
            };

            // =====================================================
            // CLIENT HEADER
            // =====================================================

            Label lblClients = new Label
            {
                Text = "CLIENTS",

                Dock = DockStyle.Top,

                Height = 40,

                Padding =
                    new Padding(
                        15,
                        12,
                        0,
                        0
                    ),

                Font =
                    new Font(
                        "Segoe UI",
                        9f,
                        FontStyle.Bold
                    ),

                ForeColor =
                    Color.White,

                BackColor =
                    Color.FromArgb(
                        35,
                        35,
                        35
                    )
            };

            // =====================================================
            // CLIENT LIST
            // =====================================================

            clientListPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,

                FlowDirection =
                    FlowDirection.TopDown,

                WrapContents = false,

                AutoScroll = true,

                Padding =
                    new Padding(
                        5
                    ),

                BackColor =
                    Color.FromArgb(
                        35,
                        35,
                        35
                    )
            };

            // =====================================================
            // BOTTOM BUTTON PANEL
            // =====================================================

            Panel bottomButtonPanel = new Panel
            {
                Dock = DockStyle.Bottom,

                Height = 210,

                BackColor =
                    Color.FromArgb(
                        35,
                        35,
                        35
                    )
            };

            // =====================================================
            // ADD CLIENT
            // =====================================================

            btnAddClient = CreateSidebarButton(
                "+ เพิ่ม Client"
            );

            btnAddClient.Location =
                new Point(
                    5,
                    5
                );

            btnAddClient.Click += delegate
            {
                AddNewClient();
            };

            // =====================================================
            // HIDE TRAY
            // =====================================================

            btnHideToTray = CreateSidebarButton(
                "ซ่อนไปยัง Tray"
            );

            btnHideToTray.Location =
                new Point(
                    5,
                    5
                );

            btnHideToTray.Click += delegate
            {
                HideToSystemTray();
            };

            // =====================================================
            // WIKI
            // =====================================================

            btnWiki = CreateSidebarButton(
                "Wiki"
            );

            btnWiki.Location =
                new Point(
                    5,
                    45
                );

            btnWiki.Click += delegate
            {
                LinkStart(
                    "https://lumivaraonline.com/wiki/#overview"
                );
            };

            // =====================================================
            // UPDATE
            // =====================================================

            btnUpdates = CreateSidebarButton(
                "Update"
            );

            btnUpdates.Location =
                new Point(
                    5,
                    85
                );

            btnUpdates.Click += delegate
            {
                LinkStart(
                    "https://lumivaraonline.com/changelog/"
                );
            };

            // =====================================================
            // GITHUB
            // =====================================================

            btnGithub = CreateSidebarButton(
                "GitHub"
            );

            btnGithub.Location =
                new Point(
                    5,
                    125
                );

            btnGithub.Click += delegate
            {
                LinkStart(
                    "https://github.com/serizava01"
                );
            };

            // =====================================================
            // ADD BUTTONS TO BOTTOM PANEL
            // =====================================================

            //bottomButtonPanel.Controls.Add(
            //    btnAddClient
            //);

            bottomButtonPanel.Controls.Add(
                btnHideToTray
            );

            bottomButtonPanel.Controls.Add(
                btnWiki
            );

            bottomButtonPanel.Controls.Add(
                btnUpdates
            );

            bottomButtonPanel.Controls.Add(
                btnGithub
            );

            sidebarPanel.Controls.Add(
                clientListPanel
            );

            sidebarPanel.Controls.Add(
                bottomButtonPanel
            );

            sidebarPanel.Controls.Add(
                lblClients
            );

            // =====================================================
            // CONTENT
            // =====================================================

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,

                BackColor =
                    Color.FromArgb(
                        20,
                        20,
                        20
                    )
            };

            ShowEmptyContent();

            // =====================================================
            // FOOTER
            // =====================================================

            footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,

                Height = 28,

                BackColor =
                    Color.FromArgb(
                        240,
                        240,
                        240
                    )
            };

            // =====================================================
            // ADD TO FORM
            // =====================================================

            Controls.Add(
                contentPanel
            );

            Controls.Add(
                sidebarPanel
            );

            Controls.Add(
                footerPanel
            );

            Controls.Add(
                headerPanel
            );
        }

        // =========================================================
        // Create Sidebar Button
        // =========================================================

        private Button CreateSidebarButton(
            string text)
        {
            Button button = new Button
            {
                Text = text,

                Width = 210,

                Height = 35,

                FlatStyle =
                    FlatStyle.Flat,

                ForeColor =
                    Color.White,

                BackColor =
                    Color.FromArgb(
                        55,
                        55,
                        55
                    ),

                Cursor =
                    Cursors.Hand,

                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;

            return button;
        }

        // =========================================================
        // Empty Content
        // =========================================================
        // =========================================================
        // Client Count Selector
        // =========================================================

        private void InitializeClientCountSelector()
        {
            if (nummonitor == null)
                return;

            isLoadingClientCount = true;

            nummonitor.Items.Clear();

            for (int i = 1; i <= 10; i++)
            {
                nummonitor.Items.Add(i.ToString());
            }

            nummonitor.SelectedIndex = maxClients - 1;

            nummonitor.SelectedIndexChanged +=
                Nummonitor_SelectedIndexChanged;

            isLoadingClientCount = false;
        }

        // =========================================================
        // Load Client Count
        // =========================================================

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
                string[] lines =
                    File.ReadAllLines(ConfigFilePath);

                foreach (string line in lines)
                {
                    string value = line.Trim();

                    if (!value.StartsWith(
                        "MaxClients=",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string numberText =
                        value.Substring(
                            "MaxClients=".Length
                        ).Trim();

                    int savedCount;

                    if (int.TryParse(
                        numberText,
                        out savedCount))
                    {
                        if (savedCount >= 1 &&
                            savedCount <= 100)
                        {
                            maxClients = savedCount;
                        }
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

        // =========================================================
        // Set ComboBox Value
        // =========================================================

        private void SetClientCountComboValue()
        {
            if (nummonitor == null)
                return;

            if (maxClients < 1)
                maxClients = 1;

            if (maxClients > 100)
                maxClients = 100;

            isLoadingClientCount = true;

            nummonitor.SelectedIndex =
                maxClients - 1;

            isLoadingClientCount = false;
        }

        // =========================================================
        // ComboBox Changed
        // =========================================================

        private void Nummonitor_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            if (isLoadingClientCount)
                return;

            if (nummonitor == null)
                return;

            if (nummonitor.SelectedIndex < 0)
                return;

            int newCount =
                nummonitor.SelectedIndex + 1;

            // ไม่ให้ลดจำนวนต่ำกว่าจำนวน Client
            // ที่กำลังเปิดอยู่
            if (runningClients.Count > newCount)
            {
                MessageBox.Show(
                    "ตอนนี้มี Client กำลังเปิดอยู่ " +
                    runningClients.Count +
                    " จอ\n\n" +
                    "กรุณาปิด Client ที่เกินจำนวนก่อน\n" +
                    "จึงจะลดจำนวน Client ได้",
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

        // =========================================================
        // Rebuild Client List
        // =========================================================

        private void RebuildClientList()
        {
            if (clientListPanel == null)
                return;

            string oldSelectedProfileId =
                selectedProfileId;

            clientListPanel.SuspendLayout();

            try
            {
                clientListPanel.Controls.Clear();

                clientItems.Clear();
                clientStatusLabels.Clear();

                for (int i = 1; i <= maxClients; i++)
                {
                    string profileId =
                        "Lumivara_Acc_" + i;

                    if (!profiles.ContainsKey(profileId))
                    {
                        AccountProfile profile =
                            new AccountProfile
                            {
                                ProfileId = profileId,
                                Title = "Client " + i,
                                TargetUrl =
                                    "https://lumivaraonline.com/"
                            };

                        profiles[profileId] =
                            profile;
                    }

                    CreateClientItem(
                        profiles[profileId]
                    );
                }
            }
            finally
            {
                clientListPanel.ResumeLayout();
            }
            foreach (
                KeyValuePair<
                    string,
                    GameTabControl> pair
                in runningClients)
            {
                string profileId =
                    pair.Key;

                if (!clientStatusLabels.ContainsKey(
                    profileId))
                {
                    continue;
                }

                UpdateClientStatus(
                    profileId,
                    true
                );
            }
            if (!string.IsNullOrEmpty(
                oldSelectedProfileId))
            {
                if (clientItems.ContainsKey(
                    oldSelectedProfileId))
                {
                    SelectClient(
                        oldSelectedProfileId
                    );
                }
                else
                {
                    selectedProfileId = null;

                    lblSelectedClient.Text =
                        "ยังไม่ได้เลือก Client";

                    ShowEmptyContent();
                }
            }
        }
        private void ShowEmptyContent()
        {
            contentPanel.Controls.Clear();

            Label label = new Label
            {
                Text =
                    "เลือก Client จากด้านซ้าย",

                Dock =
                    DockStyle.Fill,

                TextAlign =
                    ContentAlignment.MiddleCenter,

                Font =
                    new Font(
                        "Segoe UI",
                        14f
                    ),

                ForeColor =
                    Color.Gray,

                BackColor =
                    Color.FromArgb(
                        20,
                        20,
                        20
                    )
            };

            contentPanel.Controls.Add(
                label
            );
        }

        // =========================================================
        // Create Client List
        // =========================================================

  

        private void CreateClientList()
        {
            // เตรียม Profile ไว้สูงสุด 100 Client
            // แต่จะแสดงตามจำนวนที่เลือกใน ComboBox
            for (int i = 1; i <= 100; i++)
            {
                string profileId =
                    "Lumivara_Acc_" + i;

                if (profiles.ContainsKey(profileId))
                    continue;

                string title =
                    "Client " + i;

                AccountProfile profile =
                    new AccountProfile
                    {
                        ProfileId = profileId,

                        Title = title,

                        TargetUrl =
                            "https://lumivaraonline.com/"
                    };

                profiles[profileId] =
                    profile;
            }

            RebuildClientList();
        }

        // =========================================================
        // Create Client Item
        // =========================================================

        private void CreateClientItem(
            AccountProfile profile)
        {
            Panel item = new Panel
            {
                Width = 195,

                Height = 55,

                Margin =
                    new Padding(
                        0,
                        0,
                        0,
                        4
                    ),

                BackColor =
                    Color.FromArgb(
                        45,
                        45,
                        45
                    ),

                Cursor =
                    Cursors.Hand,

                Tag =
                    profile.ProfileId
            };

            Label status = new Label
            {
                Text = "●",

                AutoSize = true,

                Location =
                    new Point(
                        10,
                        17
                    ),

                Font =
                    new Font(
                        "Segoe UI Symbol",
                        11f
                    ),

                ForeColor =
                    Color.Red,

                Cursor =
                    Cursors.Hand,

                Tag = item
            };

            Label title = new Label
            {
                Text =
                    profile.Title,

                AutoSize = true,

                Location =
                    new Point(
                        35,
                        10
                    ),

                Font =
                    new Font(
                        "Segoe UI",
                        9.5f,
                        FontStyle.Bold
                    ),

                ForeColor =
                    Color.White,

                Cursor =
                    Cursors.Hand,

                Tag = item
            };

            Label state = new Label
            {
                Text =
                    "Stopped",

                AutoSize = true,

                Location =
                    new Point(
                        35,
                        30
                    ),

                Font =
                    new Font(
                        "Segoe UI",
                        8f
                    ),

                ForeColor =
                    Color.Silver,

                Cursor =
                    Cursors.Hand,

                Tag = item
            };

            item.Controls.Add(
                status
            );

            item.Controls.Add(
                title
            );

            item.Controls.Add(
                state
            );

            clientItems[
                profile.ProfileId
            ] = item;

            clientStatusLabels[
                profile.ProfileId
            ] = status;

            // =====================================================
            // Parent Panel Events
            // =====================================================

            item.Click +=
                ClientItem_Click;

            item.DoubleClick +=
                ClientItem_DoubleClick;

            item.MouseUp +=
                ClientItem_MouseUp;
            status.Click +=
                ClientChild_Click;

            title.Click +=
                ClientChild_Click;

            state.Click +=
                ClientChild_Click;

            status.DoubleClick +=
                ClientChild_DoubleClick;

            title.DoubleClick +=
                ClientChild_DoubleClick;

            state.DoubleClick +=
                ClientChild_DoubleClick;

            status.MouseUp +=
                ClientChild_MouseUp;

            title.MouseUp +=
                ClientChild_MouseUp;

            state.MouseUp +=
                ClientChild_MouseUp;

            clientListPanel.Controls.Add(
                item
            );
        }

        // =========================================================
        // Get Parent Client Item
        // =========================================================

        private Panel GetClientItemFromControl(
            Control control)
        {
            if (control == null)
                return null;

            Panel item =
                control.Tag as Panel;

            if (item != null)
                return item;

            return control.Parent as Panel;
        }

        // =========================================================
        // Child Click
        // =========================================================

        private void ClientChild_Click(
            object sender,
            EventArgs e)
        {
            Control control =
                sender as Control;

            Panel item =
                GetClientItemFromControl(
                    control
                );

            if (item == null)
                return;

            SelectClientFromPanel(
                item
            );
        }

        // =========================================================
        // Child Double Click
        // =========================================================

        private void ClientChild_DoubleClick(
            object sender,
            EventArgs e)
        {
            Control control =
                sender as Control;

            Panel item =
                GetClientItemFromControl(
                    control
                );

            if (item == null)
                return;

            StartClientFromPanel(
                item
            );
        }

        // =========================================================
        // Child Right Click
        // =========================================================

        private void ClientChild_MouseUp(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            Control control =
                sender as Control;

            Panel item =
                GetClientItemFromControl(
                    control
                );

            if (item == null)
                return;

            SelectClientFromPanel(
                item
            );

            clientContextMenu.Show(
                control,
                e.Location
            );
        }

        // =========================================================
        // Single Click
        // =========================================================

        private void ClientItem_Click(
            object sender,
            EventArgs e)
        {
            Panel item =
                sender as Panel;

            if (item == null)
                return;

            SelectClientFromPanel(
                item
            );
        }

        // =========================================================
        // Double Click
        // =========================================================

        private void ClientItem_DoubleClick(
            object sender,
            EventArgs e)
        {
            Panel item =
                sender as Panel;

            if (item == null)
                return;

            StartClientFromPanel(
                item
            );
        }

        // =========================================================
        // Select Client From Panel
        // =========================================================

        private void SelectClientFromPanel(
            Panel item)
        {
            if (item == null)
                return;

            string profileId =
                item.Tag as string;

            if (string.IsNullOrEmpty(profileId))
                return;

            SelectClient(
                profileId
            );
        }

        // =========================================================
        // Start Client From Panel
        // =========================================================

        private void StartClientFromPanel(
            Panel item)
        {
            if (item == null)
                return;

            string profileId =
                item.Tag as string;

            if (string.IsNullOrEmpty(profileId))
                return;

            if (runningClients.ContainsKey(
                profileId))
            {
                SelectClient(
                    profileId
                );

                return;
            }

            StartClientInstance(
                profileId
            );
        }

        // =========================================================
        // Right Click
        // =========================================================

        private void ClientItem_MouseUp(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            Panel item =
                sender as Panel;

            if (item == null)
                return;

            SelectClientFromPanel(
                item
            );

            clientContextMenu.Show(
                item,
                e.Location
            );
        }

        // =========================================================
        // Select Client
        // =========================================================

        private void SelectClient(
            string profileId)
        {
            if (!profiles.ContainsKey(
                profileId))
                return;

            selectedProfileId =
                profileId;

            foreach (
                KeyValuePair<
                    string,
                    Panel> pair
                in clientItems)
            {
                if (pair.Key == profileId)
                {
                    pair.Value.BackColor =
                        Color.FromArgb(
                            70,
                            70,
                            70
                        );
                }
                else
                {
                    pair.Value.BackColor =
                        Color.FromArgb(
                            45,
                            45,
                            45
                        );
                }
            }

            AccountProfile profile =
                profiles[profileId];

            lblSelectedClient.Text =
                profile.Title;

            ShowClientGame(
                profileId
            );
        }

        // =========================================================
        // Show Client Game
        // =========================================================

        private void ShowClientGame(
            string profileId)
        {
            contentPanel.SuspendLayout();

            try
            {
                contentPanel.Controls.Clear();

                if (!runningClients.ContainsKey(
                    profileId))
                {
                    Label label = new Label
                    {
                        Text =
                            "Client ยังไม่ได้เปิด\n\n" +
                            "ดับเบิลคลิก Client เพื่อเปิด",

                        Dock =
                            DockStyle.Fill,

                        TextAlign =
                            ContentAlignment.MiddleCenter,

                        ForeColor =
                            Color.Gray,

                        BackColor =
                            Color.FromArgb(
                                20,
                                20,
                                20
                            ),

                        Font =
                            new Font(
                                "Segoe UI",
                                12f
                            )
                    };

                    contentPanel.Controls.Add(
                        label
                    );

                    return;
                }

                GameTabControl gameControl =
                    runningClients[profileId];

                gameControl.Dock =
                    DockStyle.Fill;

                if (gameControl.Parent !=
                    contentPanel)
                {
                    contentPanel.Controls.Add(
                        gameControl
                    );
                }

                gameControl.BringToFront();
            }
            finally
            {
                contentPanel.ResumeLayout();
            }
        }

        // =========================================================
        // Start Client
        // =========================================================

        private void StartClientInstance(
            string profileId)
        {
            if (!profiles.ContainsKey(
                profileId))
                return;

            if (runningClients.ContainsKey(
                profileId))
            {
                SelectClient(
                    profileId
                );

                return;
            }

            AccountProfile profile =
                profiles[profileId];

            try
            {
                GameTabControl gameControl =
                    new GameTabControl(
                        profile
                    );

                gameControl.Dock =
                    DockStyle.Fill;

                runningClients.Add(
                    profileId,
                    gameControl
                );

                UpdateClientStatus(
                    profileId,
                    true
                );

                SelectClient(
                    profileId
                );

                UpdateAppTitle();

                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "ไม่สามารถเปิด " +
                    profile.Title +
                    " ได้\n\n" +
                    ex.Message,

                    "Start Client Error",

                    MessageBoxButtons.OK,

                    MessageBoxIcon.Error
                );
            }
        }

        // =========================================================
        // Stop Client
        // =========================================================

        private void StopClientInstance(
            string profileId)
        {
            if (!runningClients.ContainsKey(
                profileId))
                return;

            GameTabControl gameControl =
                runningClients[profileId];

            if (gameControl.Parent ==
                contentPanel)
            {
                contentPanel.Controls.Remove(
                    gameControl
                );
            }
            try
            {
                gameControl.Dispose();
            }
            catch
            {
            }

            runningClients.Remove(
                profileId
            );

            UpdateClientStatus(
                profileId,
                false
            );

            if (selectedProfileId ==
                profileId)
            {
                selectedProfileId =
                    null;

                lblSelectedClient.Text =
                    "ยังไม่ได้เลือก Client";

                ShowEmptyContent();
            }

            UpdateAppTitle();

            SaveConfig();
        }

        // =========================================================
        // Update Client Status
        // =========================================================

        private void UpdateClientStatus(
            string profileId,
            bool active)
        {
            if (!clientStatusLabels.ContainsKey(
                profileId))
                return;

            Label status =
                clientStatusLabels[
                    profileId
                ];

            status.ForeColor =
                active
                    ? Color.LimeGreen
                    : Color.Red;

            if (!clientItems.ContainsKey(
                profileId))
                return;

            Panel item =
                clientItems[
                    profileId
                ];

            if (item.Controls.Count < 3)
                return;

            Label state =
                item.Controls[2]
                as Label;

            if (state != null)
            {
                state.Text =
                    active
                        ? "Running"
                        : "Stopped";
            }
        }

        // =========================================================
        // Context Menu
        // =========================================================

        private void InitializeContextMenu()
        {
            clientContextMenu =
                new ContextMenuStrip();

            ToolStripMenuItem startItem =
                new ToolStripMenuItem(
                    "เปิด Client"
                );

            startItem.Click += delegate
            {
                if (!string.IsNullOrEmpty(
                    selectedProfileId))
                {
                    StartClientInstance(
                        selectedProfileId
                    );
                }
            };

            ToolStripMenuItem stopItem =
                new ToolStripMenuItem(
                    "ปิด Client"
                );

            stopItem.Click += delegate
            {
                if (!string.IsNullOrEmpty(
                    selectedProfileId))
                {
                    StopClientInstance(
                        selectedProfileId
                    );
                }
            };

            ToolStripSeparator separator =
                new ToolStripSeparator();

            ToolStripMenuItem deleteItem =
                new ToolStripMenuItem(
                    "ลบข้อมูลล็อกอินของจอนี้"
                );

            deleteItem.Click +=
                DeleteSelectedClient_Click;

            clientContextMenu.Items.Add(
                startItem
            );

            clientContextMenu.Items.Add(
                stopItem
            );

            clientContextMenu.Items.Add(
                separator
            );

            clientContextMenu.Items.Add(
                deleteItem
            );
        }

        // =========================================================
        // Delete Client Data
        // =========================================================

        private void DeleteSelectedClient_Click(
            object sender,
            EventArgs e)
        {
            if (string.IsNullOrEmpty(
                selectedProfileId))
                return;

            if (!profiles.ContainsKey(
                selectedProfileId))
                return;

            AccountProfile profile =
                profiles[
                    selectedProfileId
                ];

            DialogResult result =
                MessageBox.Show(
                    "แน่ใจว่าต้องการลบข้อมูลล็อกอินของ " +
                    profile.Title +
                    " หรือไม่?",

                    "ยืนยันการลบข้อมูล",

                    MessageBoxButtons.YesNo,

                    MessageBoxIcon.Warning
                );

            if (result !=
                DialogResult.Yes)
                return;

            StopClientInstance(
                profile.ProfileId
            );

            DeleteProfileDataFolder(
                profile.ProfileId
            );

            SaveConfig();
        }

        // =========================================================
        // Delete Profile Folder
        // =========================================================

        private void DeleteProfileDataFolder(
            string profileId)
        {
            string profileFolder =
                Path.Combine(
                    basePath,
                    "Profiles",
                    profileId
                );

            if (!Directory.Exists(
                profileFolder))
                return;

            try
            {
                Directory.Delete(
                    profileFolder,
                    true
                );
            }
            catch
            {
                Task.Run(async delegate
                {
                    await Task.Delay(
                        2000
                    );

                    try
                    {
                        if (Directory.Exists(
                            profileFolder))
                        {
                            Directory.Delete(
                                profileFolder,
                                true
                            );
                        }
                    }
                    catch
                    {
                    }
                });
            }
        }

        // =========================================================
        // Add Client
        // =========================================================

        private void AddNewClient()
        {
            MessageBox.Show(
                "ตอนนี้ระบบเตรียม Client ไว้ " +
                nummonitor+
                " จอแล้ว\n\n" +
                "สามารถดับเบิลคลิก Client ที่ต้องการเพื่อเปิดได้เลย",

                "Client Manager",

                MessageBoxButtons.OK,

                MessageBoxIcon.Information
            );
        }

        // =========================================================
        // Load Config
        // =========================================================

        private void LoadSavedInstances()
        {
            List<string> activeProfiles =
                new List<string>();

            if (File.Exists(
                ConfigFilePath))
            {
                try
                {
                    string[] lines =
                        File.ReadAllLines(
                            ConfigFilePath
                        );

                    foreach (
                        string line
                        in lines)
                    {
                        string profileId =
                            line.Trim();

                        if (string.IsNullOrEmpty(
                            profileId))
                            continue;

                        if (!profiles.ContainsKey(
                            profileId))
                            continue;

                        if (!activeProfiles.Contains(
                            profileId))
                        {
                            activeProfiles.Add(
                                profileId
                            );
                        }
                    }
                }
                catch
                {
                }
            }

            // =====================================================
            // ไม่มี Config
            // เปิด Client 1 เป็นค่าเริ่มต้น
            // =====================================================

            if (activeProfiles.Count == 0)
            {
                activeProfiles.Add(
                    "Lumivara_Acc_1"
                );
            }

            foreach (
                string profileId
                in activeProfiles)
            {
                StartClientInstance(
                    profileId
                );
            }

            SaveConfig();
        }

        // =========================================================
        // Save Config
        // =========================================================

        private void SaveConfig()
        {
            try
            {
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }

                List<string> configLines = new List<string>();

                // บันทึกจำนวน Client
                configLines.Add("MaxClients=" + maxClients);

                // บันทึก Client ที่กำลังเปิดอยู่
                foreach (KeyValuePair<string, GameTabControl> pair in runningClients)
                {
                    configLines.Add(pair.Key);
                }

                File.WriteAllLines(ConfigFilePath, configLines.ToArray());
            }
            catch
            {


            }
        }

        // =========================================================
        // Update Title
        // =========================================================

        private void UpdateAppTitle()
        {
            int count =
                runningClients.Count;

            Text =
                "Lumivara Online - Multi Client Manager (" +
                count +
                " Client(s) Running)";

            if (lblRunningCount != null)
            {
                lblRunningCount.Text =
                    "Running: " +
                    count;
            }
        }

        // =========================================================
        // Tray
        // =========================================================

        private void InitializeTrayIcon()
        {
            trayMenu =
                new ContextMenuStrip();

            ToolStripMenuItem showMenuItem =
                new ToolStripMenuItem(
                    "แสดงหน้าต่างเกม"
                );

            showMenuItem.Click += delegate
            {
                ShowFromSystemTray();
            };

            ToolStripMenuItem exitMenuItem =
                new ToolStripMenuItem(
                    "ปิดโปรแกรมทั้งหมด"
                );

            exitMenuItem.Click += delegate
            {
                ExitApplication();
            };

            trayMenu.Items.Add(
                showMenuItem
            );

            trayMenu.Items.Add(
                exitMenuItem
            );

            Icon appIcon = null;

            try
            {
                if (
                    Properties.Resources
                        .LumivaraMultiClient != null)
                {
                    using (
                        MemoryStream ms =
                        new MemoryStream(
                            Properties.Resources
                                .LumivaraMultiClient))
                    {
                        appIcon =
                            new Icon(ms);
                    }
                }
            }
            catch
            {
                appIcon =
                    SystemIcons.Application;
            }

            if (appIcon == null)
            {
                appIcon =
                    SystemIcons.Application;
            }

            Icon =
                appIcon;

            trayIcon =
                new NotifyIcon
                {
                    Text =
                        "Lumivara Multi Client",

                    Icon =
                        appIcon,

                    ContextMenuStrip =
                        trayMenu,

                    Visible =
                        false
                };

            trayIcon.DoubleClick += delegate
            {
                ShowFromSystemTray();
            };
        }

        // =========================================================
        // Hide To Tray
        // =========================================================

        private void HideToSystemTray()
        {
            if (trayIcon == null)
                return;

            trayIcon.Visible =
                true;

            Hide();

            trayIcon.ShowBalloonTip(
                2000,

                "Lumivara Multi Client",

                "โปรแกรมกำลังทำงานอยู่เบื้องหลัง",

                ToolTipIcon.Info
            );
        }

        // =========================================================
        // Show From Tray
        // =========================================================

        private void ShowFromSystemTray()
        {
            if (trayIcon == null)
                return;

            Show();

            WindowState =
                FormWindowState.Normal;

            BringToFront();

            Activate();

            trayIcon.Visible =
                false;
        }

        // =========================================================
        // Close All Clients
        // =========================================================

        private void CloseAllClients()
        {
            List<GameTabControl> clients =
                new List<GameTabControl>(
                    runningClients.Values
                );

            runningClients.Clear();

            foreach (
                GameTabControl gameControl
                in clients)
            {
                try
                {
                    if (gameControl.Parent != null)
                    {
                        gameControl.Parent.Controls.Remove(
                            gameControl
                        );
                    }
                }
                catch
                {
                }

                try
                {
                    gameControl.Dispose();
                }
                catch
                {


                }
            }
        }

        // =========================================================
        // Exit
        // =========================================================

        private void ExitApplication()
        {
            SaveConfig();

            CloseAllClients();

            if (trayIcon != null)
            {
                trayIcon.Visible =
                    false;

                trayIcon.Dispose();

                trayIcon = null;
            }

            Application.Exit();
        }

        // =========================================================
        // Form Closing
        // =========================================================

        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            SaveConfig();

            CloseAllClients();

            if (trayIcon != null)
            {
                trayIcon.Visible =
                    false;

                trayIcon.Dispose();

                trayIcon = null;
            }

            base.OnFormClosing(
                e
            );
        }

        // =========================================================
        // Open URL
        // =========================================================

        public void LinkStart(
            string url)
        {
            if (string.IsNullOrEmpty(
                url))
                return;

            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = url,

                        UseShellExecute = true
                    }
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "ไม่สามารถเปิดเบราว์เซอร์ได้: " +
                    ex.Message,

                    "Error",

                    MessageBoxButtons.OK,

                    MessageBoxIcon.Error
                );
            }
        }
    }
}