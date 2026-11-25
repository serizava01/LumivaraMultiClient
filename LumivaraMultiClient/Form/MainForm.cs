using LumivaraMultiClient.Controls;
using LumivaraMultiClient.Models;
using LumivaraMultiClient.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LumivaraMultiClient.Forms
{
    public partial class MainForm : Form
    {
        // =========================================================
        // STATE & VARIABLES
        // =========================================================
        private readonly Dictionary<string, GameTabControl> runningClients = new Dictionary<string, GameTabControl>();
        private readonly Dictionary<string, Panel> clientItems = new Dictionary<string, Panel>();
        private readonly Dictionary<string, Label> clientStatusLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, AccountProfile> profiles = new Dictionary<string, AccountProfile>();

        private string selectedProfileId = null;
        private readonly string Url = "https://play.nightcamp.app/";
        private readonly string urlWiki = "https://play.nightcamp.app/wiki/index.html";
        private readonly string urlChangelog = "https://lumivaraonline.com/changelog/";
        private string maximizedProfileId = null;
        private const int MaxClientsInCombo = 100;
        private const int ItemsPerPage = 12;
        private bool isGlobalMute = false;
     
        private int currentPage = 1;
        private int maxClients = 3;
        private bool isLoadingClientCount = false;
        private bool isLoadingDiagnosticLog = false;
        private bool isHiddenToTray = false;
        private bool isClosingConfirmed = false;
        private bool isSidebarCollapsed = false;
        private int lastSidebarWidth = 220;

        // =========================================================
        // UI CONTROLS (ประกาศตัวแปร)
        // =========================================================
        private Panel headerPanel;
        private Panel footerPanel;
        private SplitContainer splitContainer;
        private Panel sidebarPanel;
        private Panel contentPanel;
        private Panel dashboardScrollPanel;
        private FlowLayoutPanel clientListPanel;
        private TableLayoutPanel gridLayout;
        private Button btnMuteAll;
        private Label lblSelectedClient;
        private Label lblRunningCount;
        private Label lblClientStatus;
        private Label lbURlnow;
        private Label lblPageInfo;

        private Button btnHideToTray;
        private Button btnWiki;
        private Button btnUpdates;
        private Button btnGithub;
        private Button btnPrevPage;
        private Button btnNextPage;

        private ComboBox nummonitor;
        private CheckBox chkDiagnosticLog;

        private ContextMenuStrip clientContextMenu;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        private readonly string basePath = WebViewManager.BaseDataPath;
        private string ConfigFilePath { get { return Path.Combine(basePath, "config.txt"); } }

        // =========================================================
        // CONSTRUCTOR
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

            Resize += (s, e) => { UpdateAllClientsActiveState(); RelayoutClientGrid(); };
        }

        // =========================================================
        // UI INITIALIZATION
        // =========================================================
        #region UI Initialization
        //private void InitializeMainUI()
        //{
        //    Text = "Online Web Browser - Dashboard";
        //    Size = new Size(1400, 900);
        //    MinimumSize = new Size(900, 600);
        //    StartPosition = FormStartPosition.CenterScreen;

        //    // Header
        //    headerPanel = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(240, 240, 240) };

        //    lblSelectedClient = new Label { Text = "ยังไม่ได้เลือก Client", AutoSize = true, Location = new Point(15, 13), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
        //    lblRunningCount = new Label { Text = "Running: 0", AutoSize = true, Location = new Point(200, 13), Font = new Font("Segoe UI", 9f), ForeColor = Color.DimGray };
        //    lblClientStatus = new Label { Text = "Monitor", AutoSize = true, Location = new Point(350, 13), Font = new Font("Segoe UI", 9f), ForeColor = Color.Black };
        //    nummonitor = new ComboBox { Location = new Point(400, 10), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        //    lbURlnow = new Label { Text = "URL: " + Url, AutoSize = true, Location = new Point(520, 13), Font = new Font("Segoe UI", 9f), ForeColor = Color.DimGray };

        //    btnPrevPage = new Button { Text = "<", Width = 30, Height = 25, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(headerPanel.ClientSize.Width - 220, 10), FlatStyle = FlatStyle.Flat };
        //    btnPrevPage.Click += (s, e) => { if (currentPage > 1) { currentPage--; RelayoutClientGrid(); } };

        //    lblPageInfo = new Label { Text = "Page 1", Width = 60, Height = 25, TextAlign = ContentAlignment.MiddleCenter, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(headerPanel.ClientSize.Width - 190, 15) };

        //    btnNextPage = new Button { Text = ">", Width = 30, Height = 25, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(headerPanel.ClientSize.Width - 130, 10), FlatStyle = FlatStyle.Flat };
        //    btnNextPage.Click += (s, e) => { if (currentPage * ItemsPerPage < runningClients.Count) { currentPage++; RelayoutClientGrid(); } };

        //    chkDiagnosticLog = new CheckBox
        //    {
        //        Text = "เก็บLog",
        //        AutoSize = false,
        //        Width = 85,
        //        Height = 25,
        //        TextAlign = ContentAlignment.MiddleRight,
        //        Checked = true,
        //        Anchor = AnchorStyles.Top | AnchorStyles.Right,
        //        Location = new Point(headerPanel.ClientSize.Width - 95, 10),
        //        Font = new Font("Segoe UI", 9f),
        //        ForeColor = Color.Black,
        //        BackColor = headerPanel.BackColor,
        //        Cursor = Cursors.Hand
        //    };
        //    chkDiagnosticLog.CheckedChanged += ChkDiagnosticLog_CheckedChanged;

        //    headerPanel.Controls.AddRange(new Control[] {
        //        lblSelectedClient, lblRunningCount, lblClientStatus,
        //        nummonitor, lbURlnow, btnPrevPage, lblPageInfo, btnNextPage, chkDiagnosticLog
        //    });

        //    // Footer
        //    footerPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(35, 35, 35) };

        //    Button btnStartAll = new Button { Text = "▶ เปิดทั้งหมด", Width = 110, Height = 30, Location = new Point(10, 5), BackColor = Color.Green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        //    btnStartAll.Click += (s, e) => StartAllClients();

        //    Button btnStopAll = new Button { Text = "⏹ ปิดทั้งหมด", Width = 110, Height = 30, Location = new Point(125, 5), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        //    btnStopAll.Click += (s, e) => StopAllClients();

        //    footerPanel.Controls.AddRange(new Control[] { btnStartAll, btnStopAll });

        //    // SplitContainer
        //    splitContainer = new SplitContainer
        //    {
        //        Dock = DockStyle.Fill,
        //        Orientation = Orientation.Vertical,
        //        FixedPanel = FixedPanel.Panel1,
        //        SplitterDistance = 220,
        //        SplitterWidth = 4,
        //        BackColor = Color.FromArgb(20, 20, 20)
        //    };

        //    //  Sidebar
        //    sidebarPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(35, 35, 35) };
        //    Label lblClients = new Label { Text = "CLIENTS", Dock = DockStyle.Top, Height = 40, Padding = new Padding(15, 12, 0, 0), Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(35, 35, 35) };
        //    clientListPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(5), BackColor = Color.FromArgb(35, 35, 35) };


        //    Panel bottomButtonPanel = new Panel { Dock = DockStyle.Bottom, Height = 180, BackColor = Color.FromArgb(35, 35, 35) };

        //    btnHideToTray = CreateSidebarButton("ซ่อนไปยัง Tray"); btnHideToTray.Location = new Point(5, 5); btnHideToTray.Click += delegate { HideToSystemTray(); };
        //    btnWiki = CreateSidebarButton("Wiki"); btnWiki.Location = new Point(5, 45); btnWiki.Click += delegate { LinkStart(urlWiki); };
        //    btnUpdates = CreateSidebarButton("Update"); btnUpdates.Location = new Point(5, 85); btnUpdates.Click += delegate { LinkStart(urlChangelog); };
        //    btnGithub = CreateSidebarButton("GitHub"); btnGithub.Location = new Point(5, 125); btnGithub.Click += delegate { LinkStart("https://github.com/serizava01"); };

        //    bottomButtonPanel.Controls.AddRange(new Control[] { btnHideToTray, btnWiki, btnUpdates, btnGithub });
        //    sidebarPanel.Controls.AddRange(new Control[] { clientListPanel, bottomButtonPanel, lblClients });
        //    splitContainer.Panel1.Controls.Add(sidebarPanel);

        //    // Dashboard
        //    contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 20), Padding = new Padding(10) };
        //    dashboardScrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };
        //    contentPanel.Controls.Add(dashboardScrollPanel);

        //    gridLayout = new TableLayoutPanel
        //    {
        //        Dock = DockStyle.Fill,
        //        AutoSize = false,
        //        ColumnCount = 0,
        //        RowCount = 0,
        //        BackColor = Color.Transparent,
        //        Padding = new Padding(0),
        //        CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        //    };
        //    dashboardScrollPanel.Controls.Add(gridLayout);
        //    dashboardScrollPanel.Resize += (s, e) => RelayoutClientGrid();

        //    splitContainer.Panel2.Controls.Add(contentPanel);


        //    Controls.Add(splitContainer);
        //    Controls.Add(footerPanel);
        //    Controls.Add(headerPanel);

        //    // Events
        //    splitContainer.SplitterMoved += SplitContainer_SplitterMoved;
        //    splitContainer.MouseDoubleClick += (s, e) => ToggleSidebar();
        //}
        #endregion
        private void InitializeMainUI()
        {
            Text = "Online Web Browser - Dashboard";
            Size = new Size(1400, 900);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            // =========================================================
            // HEADER
            // =========================================================
            headerPanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(40, 40, 40) };

            lblSelectedClient = new Label { Text = "ยังไม่ได้เลือก Client", AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.White };
            lblRunningCount = new Label { Text = "Running: 0", AutoSize = true, Location = new Point(200, 12), Font = new Font("Segoe UI", 9f), ForeColor = Color.LightGray };
            nummonitor = new ComboBox { Location = new Point(300, 10), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            lbURlnow = new Label { Text = "URL: " + Url, AutoSize = true, Location = new Point(400, 12), Font = new Font("Segoe UI", 9f), ForeColor = Color.LightGray };

            // Pagination
            btnPrevPage = new Button { Text = "<", Width = 30, Height = 25, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(headerPanel.ClientSize.Width - 220, 8), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnPrevPage.Click += (s, e) => { if (currentPage > 1) { currentPage--; RelayoutClientGrid(); } };

            lblPageInfo = new Label { Text = "Page 1", Width = 60, Height = 25, TextAlign = ContentAlignment.MiddleCenter, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(headerPanel.ClientSize.Width - 190, 12), ForeColor = Color.White };

            btnNextPage = new Button { Text = ">", Width = 30, Height = 25, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(headerPanel.ClientSize.Width - 130, 8), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnNextPage.Click += (s, e) => { if (currentPage * ItemsPerPage < runningClients.Count) { currentPage++; RelayoutClientGrid(); } };

            chkDiagnosticLog = new CheckBox
            {
                Text = "เก็บLog",
                AutoSize = false,
                Width = 85,
                Height = 25,
                TextAlign = ContentAlignment.MiddleRight,
                Checked = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(headerPanel.ClientSize.Width - 95, 8),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            chkDiagnosticLog.CheckedChanged += ChkDiagnosticLog_CheckedChanged;

            headerPanel.Controls.AddRange(new Control[] {
        lblSelectedClient, lblRunningCount, nummonitor, lbURlnow,
        btnPrevPage, lblPageInfo, btnNextPage, chkDiagnosticLog
    });

            // =========================================================
            // FOOTER
            // =========================================================
            footerPanel = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(30, 30, 30) };

            Button btnStartAll = new Button { Text = "▶ เปิดทั้งหมด", Width = 110, Height = 30, Location = new Point(10, 7), BackColor = Color.Green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnStartAll.Click += (s, e) => StartAllClients();

            Button btnStopAll = new Button { Text = "⏹ ปิดทั้งหมด", Width = 110, Height = 30, Location = new Point(125, 7), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnStopAll.Click += (s, e) => StopAllClients();

            btnMuteAll = new Button { Text = "🔊 เสียงทั้งหมด", Width = 120, Height = 30, Location = new Point(240, 7), BackColor = Color.FromArgb(70, 70, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnMuteAll.Click += (s, e) => ToggleMuteAll();

            footerPanel.Controls.AddRange(new Control[] { btnStartAll, btnStopAll, btnMuteAll });

            // =========================================================
            // SIDEBAR
            // =========================================================
            sidebarPanel = new Panel { Dock = DockStyle.Left, Width = 200, BackColor = Color.FromArgb(35, 35, 35) };

            Label lblClients = new Label { Text = "CLIENTS", Dock = DockStyle.Top, Height = 35, Padding = new Padding(10, 10, 0, 0), Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(35, 35, 35) };
            clientListPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(2), BackColor = Color.FromArgb(35, 35, 35) };

            Panel bottomButtonPanel = new Panel { Dock = DockStyle.Bottom, Height = 150, BackColor = Color.FromArgb(35, 35, 35) };
            btnHideToTray = CreateSidebarButton("ซ่อนไป Tray"); btnHideToTray.Location = new Point(5, 5); btnHideToTray.Click += delegate { HideToSystemTray(); };
            btnWiki = CreateSidebarButton("Wiki"); btnWiki.Location = new Point(5, 45); btnWiki.Click += delegate { LinkStart(urlWiki); };
            btnUpdates = CreateSidebarButton("Update"); btnUpdates.Location = new Point(5, 85); btnUpdates.Click += delegate { LinkStart(urlChangelog); };
            btnGithub = CreateSidebarButton("GitHub"); btnGithub.Location = new Point(5, 125); btnGithub.Click += delegate { LinkStart("https://github.com/serizava01"); };

            bottomButtonPanel.Controls.AddRange(new Control[] { btnHideToTray, btnWiki, btnUpdates, btnGithub });
            sidebarPanel.Controls.AddRange(new Control[] { clientListPanel, bottomButtonPanel, lblClients });

            // =========================================================
            // (Dashboard)
            // =========================================================
            contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 20), Padding = new Padding(0) };
            dashboardScrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = false, BackColor = Color.Transparent };
            contentPanel.Controls.Add(dashboardScrollPanel);

            gridLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                ColumnCount = 0,
                RowCount = 0,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            dashboardScrollPanel.Controls.Add(gridLayout);
            dashboardScrollPanel.Resize += (s, e) => RelayoutClientGrid();

      
            Controls.Add(contentPanel);
            Controls.Add(sidebarPanel);
            Controls.Add(footerPanel);
            Controls.Add(headerPanel);

            Resize += (s, e) => RelayoutClientGrid();
        }
        private void ToggleMuteAll()
        {
            isGlobalMute = !isGlobalMute;

            foreach (var gc in runningClients.Values)
            {
                if (gc != null && !gc.IsDisposed)
                {
                    gc.IsMuted = isGlobalMute;
                }
            }

            if (isGlobalMute)
            {
                btnMuteAll.Text = "🔇 เปิดเสียงคืน";
                btnMuteAll.BackColor = Color.Orange;
            }
            else
            {
                btnMuteAll.Text = "🔊 ปิดเสียงทั้งหมด";
                btnMuteAll.BackColor = Color.FromArgb(70, 70, 70);
            }
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

        private void ToggleSidebar()
        {
            if (splitContainer.IsSplitterFixed) return;
            if (isSidebarCollapsed) { splitContainer.SplitterDistance = lastSidebarWidth; isSidebarCollapsed = false; }
            else { lastSidebarWidth = splitContainer.SplitterDistance; splitContainer.SplitterDistance = 0; isSidebarCollapsed = true; }
        }

        private void SplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            if (splitContainer.SplitterDistance == 0) isSidebarCollapsed = true;
            else { isSidebarCollapsed = false; lastSidebarWidth = splitContainer.SplitterDistance; }
        }

        // =========================================================
        // 5. DASHBOARD GRID LAYOUT
        // =========================================================
        private void RelayoutClientGrid()
        {
            if (gridLayout == null || dashboardScrollPanel == null) return;

            gridLayout.SuspendLayout();

            //ล้างของเก่า
            foreach (Control c in gridLayout.Controls) c.Parent = null;
            gridLayout.Controls.Clear();
            gridLayout.ColumnStyles.Clear();
            gridLayout.RowStyles.Clear();

            var activeClients = runningClients.ToList();
            int count = activeClients.Count;

            if (count == 0)
            {
                Label lblEmpty = new Label { Text = "ไม่มี Client ที่เปิดอยู่\n\nดับเบิลคลิกที่รายชื่อด้านซ้ายเพื่อเปิด", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 14f), ForeColor = Color.Gray };
                gridLayout.ColumnCount = 1;
                gridLayout.RowCount = 1;
                gridLayout.Controls.Add(lblEmpty, 0, 0);
                gridLayout.ResumeLayout();
                return;
            }

            //เช็คว่ามีจอที่ถูกสั่งขยายอยู่หรือไม่
            if (!string.IsNullOrEmpty(maximizedProfileId) && runningClients.ContainsKey(maximizedProfileId))
            {
                // โหมดขยาย: แสดงแค่จอเดียวเต็มพื้นที่
                gridLayout.ColumnCount = 1;
                gridLayout.RowCount = 1;
                gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                var gc = runningClients[maximizedProfileId];
                gridLayout.Controls.Add(gc, 0, 0);
                gc.Dock = DockStyle.Fill;
                gc.Margin = new Padding(0);
                gc.BackColor = Color.Orange;
                gc.Padding = new Padding(1);

                gridLayout.ResumeLayout();
                return;
            }

            //คำนวณคอลัมน์และแถวแบบปกติ (Grid)
            int cols = 1, rows = 1;
            if (count == 1) { cols = 1; rows = 1; }
            else if (count == 2) { cols = 2; rows = 1; }
            else if (count <= 4) { cols = 2; rows = 2; }
            else if (count <= 6) { cols = 3; rows = 2; }
            else if (count <= 9) { cols = 3; rows = 3; }
            else if (count <= 12) { cols = 4; rows = 3; }
            else { cols = (int)Math.Ceiling(Math.Sqrt(count)); rows = (int)Math.Ceiling((double)count / cols); }

            gridLayout.ColumnCount = cols;
            gridLayout.RowCount = rows;

            for (int i = 0; i < cols; i++)
                gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
            for (int i = 0; i < rows; i++)
                gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));

            int index = 0;
            foreach (var kv in activeClients)
            {
                GameTabControl gc = kv.Value;
                if (gc == null || gc.IsDisposed) continue;

                int r = index / cols;
                int c = index % cols;
                gridLayout.Controls.Add(gc, c, r);

                gc.Dock = DockStyle.Fill;
                gc.Margin = new Padding(0);
                gc.BackColor = (kv.Key == selectedProfileId) ? Color.Orange : Color.Black;
                gc.Padding = (kv.Key == selectedProfileId) ? new Padding(1) : new Padding(0);

                gc.MouseDown -= Gc_MouseDown;
                gc.MouseDown += Gc_MouseDown;
                index++;
            }
            gridLayout.ResumeLayout();
        }

        private void Gc_MouseDown(object sender, MouseEventArgs e)
        {
            GameTabControl gc = sender as GameTabControl;
            if (gc == null) return;
            var pair = runningClients.FirstOrDefault(p => p.Value == gc);
            if (!string.IsNullOrEmpty(pair.Key)) SelectClient(pair.Key);
        }

        // =========================================================
        // CLIENT MANAGEMENT (Start/Stop/Select)
        // =========================================================
        private void SelectClient(string profileId)
        {
            if (!profiles.ContainsKey(profileId)) return;
            selectedProfileId = profileId;

            foreach (KeyValuePair<string, Panel> pair in clientItems)
                pair.Value.BackColor = (pair.Key == profileId) ? Color.FromArgb(70, 70, 70) : Color.FromArgb(45, 45, 45);

            lblSelectedClient.Text = profiles[profileId].Title;
            if (lbURlnow != null) lbURlnow.Text = "URL: " + (profiles[profileId].TargetUrl ?? Url);

            RelayoutClientGrid();
            UpdateAllClientsActiveState();
        }

        private void StartClientInstance(string profileId)
        {
            if (!profiles.ContainsKey(profileId)) return;
            if (runningClients.ContainsKey(profileId)) { SelectClient(profileId); return; }

            AccountProfile profile = profiles[profileId];
            try
            {
                GameTabControl gameControl = new GameTabControl(profile);
                gameControl.Dock = DockStyle.Fill;

                gameControl.CloseRequested += (id) =>
                {
                    if (InvokeRequired) Invoke(new Action(() => StopClientInstance(id)));
                    else StopClientInstance(id);
                };

                gameControl.UrlChanged += (id, newUrl) =>
                {
                    if (id == selectedProfileId && lbURlnow != null)
                    {
                        if (this.InvokeRequired) this.Invoke(new Action(() => lbURlnow.Text = "URL: " + newUrl));
                        else lbURlnow.Text = "URL: " + newUrl;
                    }
                };
                 

                runningClients.Add(profileId, gameControl);
                if (isGlobalMute && gameControl.WebView != null && gameControl.WebView.CoreWebView2 != null)
                {
                    gameControl.IsMuted = true;
                }
                gameControl.ToggleMaximizeRequested += (id) =>
                {
                    if (InvokeRequired) Invoke(new Action(() => ToggleClientMaximize(id)));
                    else ToggleClientMaximize(id);
                };
                UpdateClientStatus(profileId, true);
                SelectClient(profileId);
                UpdateAppTitle();
                SaveConfig();
            }
            catch (Exception ex) { MessageBox.Show("ไม่สามารถเปิด " + profile.Title + " ได้\n\n" + ex.Message, "Start Client Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private void ToggleClientMaximize(string profileId)
        {
            if (maximizedProfileId == profileId)
            {
                maximizedProfileId = null;
                RelayoutClientGrid();
                return;
            }

            maximizedProfileId = profileId;
            RelayoutClientGrid();
        }
        private void StopClientInstance(string profileId)
        {
            if (!runningClients.ContainsKey(profileId)) return;
            GameTabControl gameControl = runningClients[profileId];
            if (maximizedProfileId == profileId)
            {
                maximizedProfileId = null;
            }
            if (gameControl.Parent != null) gameControl.Parent.Controls.Remove(gameControl);
            try { gameControl.Dispose(); } catch { }

            runningClients.Remove(profileId);
            UpdateClientStatus(profileId, false);

            if (selectedProfileId == profileId) { selectedProfileId = null; lblSelectedClient.Text = "ยังไม่ได้เลือก Client"; }

            RelayoutClientGrid();
            UpdateAllClientsActiveState();
            UpdateAppTitle();
            SaveConfig();
        }

        private void StartAllClients()
        {
            int startCount = 0;
            for (int i = 1; i <= maxClients; i++)
            {
                string profileId = "Online_Web_Browser" + i;
                if (!runningClients.ContainsKey(profileId)) { StartClientInstance(profileId); startCount++; }
            }
            if (startCount == 0) MessageBox.Show("Client ทั้งหมดถูกเปิดอยู่แล้ว", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StopAllClients()
        {
            if (runningClients.Count == 0) return;
            DialogResult r = MessageBox.Show("ต้องการปิด Client ทั้งหมด " + runningClients.Count + " จอใช่หรือไม่?", "ยืนยันการปิดทั้งหมด", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;

            var keys = new List<string>(runningClients.Keys);
            foreach (string key in keys) StopClientInstance(key);
        }

        private void UpdateAllClientsActiveState()
        {
            bool isAppActive = !isHiddenToTray && WindowState != FormWindowState.Minimized;
            foreach (KeyValuePair<string, GameTabControl> pair in runningClients)
                pair.Value.SetActive(isAppActive);
        }

        // =========================================================
        // 7. SIDEBAR & CLIENT LIST
        // =========================================================
        private void CreateClientList()
        {
            for (int i = 1; i <= 100; i++)
            {
                string profileId = "Online_Web_Browser" + i;
                if (!profiles.ContainsKey(profileId))
                    profiles[profileId] = new AccountProfile { ProfileId = profileId, Title = "Client " + i, TargetUrl = Url };
            }
            RebuildClientList();
        }

        private void RebuildClientList()
        {
            if (clientListPanel == null) return;
            string oldSelectedProfileId = selectedProfileId;
            clientListPanel.SuspendLayout();
            try
            {
                clientListPanel.Controls.Clear(); clientItems.Clear(); clientStatusLabels.Clear();
                for (int i = 1; i <= maxClients; i++)
                {
                    string profileId = "Online_Web_Browser" + i;
                    if (!profiles.ContainsKey(profileId))
                        profiles[profileId] = new AccountProfile { ProfileId = profileId, Title = "Client " + i, TargetUrl = Url };
                    CreateClientItem(profiles[profileId]);
                }
            }
            finally { clientListPanel.ResumeLayout(); }

            foreach (KeyValuePair<string, GameTabControl> pair in runningClients)
                if (clientStatusLabels.ContainsKey(pair.Key)) UpdateClientStatus(pair.Key, true);

            if (!string.IsNullOrEmpty(oldSelectedProfileId))
            {
                if (clientItems.ContainsKey(oldSelectedProfileId)) SelectClient(oldSelectedProfileId);
                else { selectedProfileId = null; lblSelectedClient.Text = "ยังไม่ได้เลือก Client"; }
            }
        }

        private void CreateClientItem(AccountProfile profile)
        {
            Panel item = new Panel
            {
                Width = 190,
                Height = 36,
                Margin = new Padding(0, 0, 0, 2),
                BackColor = Color.FromArgb(45, 45, 45),
                Cursor = Cursors.Hand,
                Tag = profile.ProfileId
            };

            Label status = new Label
            {
                Text = "●",
                AutoSize = true,
                Location = new Point(8, 8),
                Font = new Font("Segoe UI Symbol", 10f),
                ForeColor = Color.Red,
                Cursor = Cursors.Hand,
                Tag = item
            };

 
            Label title = new Label
            {
                Text = profile.Title,
                AutoSize = false,
                Width = 150,
                Height = 16,
                Location = new Point(25, 3),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Tag = item,
                AutoEllipsis = true
            };

            Label state = new Label
            {
                Text = "Stopped",
                AutoSize = true,
                Location = new Point(25, 19),
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.Silver,
                Cursor = Cursors.Hand,
                Tag = item
            };

            item.Controls.Add(status);
            item.Controls.Add(title);
            item.Controls.Add(state);

            clientItems[profile.ProfileId] = item;
            clientStatusLabels[profile.ProfileId] = status;

            item.Click += ClientItem_Click; item.DoubleClick += ClientItem_DoubleClick; item.MouseUp += ClientItem_MouseUp;
            status.Click += ClientChild_Click; title.Click += ClientChild_Click; state.Click += ClientChild_Click;
            status.DoubleClick += ClientChild_DoubleClick; title.DoubleClick += ClientChild_DoubleClick; state.DoubleClick += ClientChild_DoubleClick;
            status.MouseUp += ClientChild_MouseUp; title.MouseUp += ClientChild_MouseUp; state.MouseUp += ClientChild_MouseUp;

            clientListPanel.Controls.Add(item);
        }

        private void ClientItem_Click(object sender, EventArgs e) { Panel item = sender as Panel; if (item != null) SelectClientFromPanel(item); }
        private void ClientItem_DoubleClick(object sender, EventArgs e) { Panel item = sender as Panel; if (item != null) StartClientFromPanel(item); }
        private void ClientItem_MouseUp(object sender, MouseEventArgs e) { if (e.Button != MouseButtons.Right) return; Panel item = sender as Panel; if (item == null) return; SelectClientFromPanel(item); clientContextMenu.Show(item, e.Location); }
        private void ClientChild_Click(object sender, EventArgs e) { Panel item = GetClientItemFromControl(sender as Control); if (item != null) SelectClientFromPanel(item); }
        private void ClientChild_DoubleClick(object sender, EventArgs e) { Panel item = GetClientItemFromControl(sender as Control); if (item != null) StartClientFromPanel(item); }
        private void ClientChild_MouseUp(object sender, MouseEventArgs e) { if (e.Button != MouseButtons.Right) return; Control control = sender as Control; Panel item = GetClientItemFromControl(control); if (item == null) return; SelectClientFromPanel(item); clientContextMenu.Show(control, e.Location); }

        private Panel GetClientItemFromControl(Control control)
        {
            if (control == null) return null;
            Panel item = control.Tag as Panel;
            return item ?? (control.Parent as Panel);
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
            if (runningClients.ContainsKey(profileId)) { SelectClient(profileId); return; }
            StartClientInstance(profileId);
        }

        private void UpdateClientStatus(string profileId, bool active)
        {
            if (!clientStatusLabels.ContainsKey(profileId)) return;
            clientStatusLabels[profileId].ForeColor = active ? Color.LimeGreen : Color.Red;
            if (!clientItems.ContainsKey(profileId)) return;
            Panel item = clientItems[profileId];
            if (item.Controls.Count >= 3 && item.Controls[2] is Label state)
                state.Text = active ? "Running" : "Stopped";
        }

        // =========================================================
        // 8. CONFIG & SETTINGS
        // =========================================================
        private void InitializeClientCountSelector()
        {
            if (nummonitor == null) return;
            isLoadingClientCount = true;
            nummonitor.Items.Clear();
            for (int i = 1; i <= MaxClientsInCombo; i++) nummonitor.Items.Add(i.ToString());
            nummonitor.SelectedIndex = maxClients - 1;
            nummonitor.SelectedIndexChanged += Nummonitor_SelectedIndexChanged;
            isLoadingClientCount = false;
        }

        private void LoadClientCount()
        {
            maxClients = 3;
            if (!File.Exists(ConfigFilePath)) { SetClientCountComboValue(); return; }
            try
            {
                foreach (string line in File.ReadAllLines(ConfigFilePath))
                {
                    string value = line.Trim();
                    if (!value.StartsWith("MaxClients=", StringComparison.OrdinalIgnoreCase)) continue;
                    int savedCount;
                    if (int.TryParse(value.Substring("MaxClients=".Length).Trim(), out savedCount))
                        if (savedCount >= 1 && savedCount <= MaxClientsInCombo) maxClients = savedCount;
                    break;
                }
            }
            catch { maxClients = 3; }
            SetClientCountComboValue();
        }

        private void SetClientCountComboValue()
        {
            if (nummonitor == null) return;
            if (maxClients < 1) maxClients = 1;
            if (maxClients > MaxClientsInCombo) maxClients = MaxClientsInCombo;
            isLoadingClientCount = true; nummonitor.SelectedIndex = maxClients - 1; isLoadingClientCount = false;
        }

        private void Nummonitor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoadingClientCount || nummonitor == null || nummonitor.SelectedIndex < 0) return;
            int newCount = nummonitor.SelectedIndex + 1;
            if (runningClients.Count > newCount)
            {
                MessageBox.Show("ตอนนี้มี Client กำลังเปิดอยู่ " + runningClients.Count + " จอ\n\nกรุณาปิด Client ที่เกินจำนวนก่อน", "ไม่สามารถลดจำนวน Client", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetClientCountComboValue(); return;
            }
            maxClients = newCount; RebuildClientList(); SaveConfig();
        }

        private void LoadSavedInstances()
        {
            List<string> activeProfiles = new List<string>();
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    foreach (string rawLine in File.ReadAllLines(ConfigFilePath))
                    {
                        string line = rawLine.Trim(); if (string.IsNullOrEmpty(line)) continue;
                        if (line.StartsWith("Profile=", StringComparison.OrdinalIgnoreCase))
                        {
                            string payload = line.Substring("Profile=".Length); string[] parts = payload.Split('|');
                            if (parts.Length >= 1)
                            {
                                string profileId = parts[0].Trim();
                                if (!profiles.ContainsKey(profileId)) continue;
                                AccountProfile p = profiles[profileId];
                                if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1])) p.Title = parts[1];
                                if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2])) p.TargetUrl = parts[2];
                            }
                            continue;
                        }
                        if (line.StartsWith("Running=", StringComparison.OrdinalIgnoreCase))
                        {
                            string profileId = line.Substring("Running=".Length).Trim();
                            if (!string.IsNullOrEmpty(profileId) && profiles.ContainsKey(profileId) && !activeProfiles.Contains(profileId)) activeProfiles.Add(profileId);
                        }
                    }
                }
                catch { }
            }
            foreach (KeyValuePair<string, AccountProfile> pair in profiles) RefreshClientItemUI(pair.Value);
            if (activeProfiles.Count == 0) activeProfiles.Add("Online_Web_Browser1");
            foreach (string profileId in activeProfiles) StartClientInstance(profileId);
            SaveConfig();
        }

        private void SaveConfig()
        {
            try
            {
                if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
                List<string> configLines = new List<string>();
                configLines.Add("MaxClients=" + maxClients);
                configLines.Add("DiagnosticLogging=" + (chkDiagnosticLog != null && chkDiagnosticLog.Checked));
                foreach (KeyValuePair<string, AccountProfile> pair in profiles)
                {
                    AccountProfile p = pair.Value; if (p == null) continue;
                    string defaultTitle = "Client " + GetClientNumber(p.ProfileId);
                    bool titleChanged = !string.Equals(p.Title, defaultTitle, StringComparison.Ordinal);
                    bool urlChanged = !string.Equals(p.TargetUrl, Url, StringComparison.OrdinalIgnoreCase);
                    if (!titleChanged && !urlChanged) continue;
                    configLines.Add("Profile=" + p.ProfileId + "|" + (p.Title ?? "") + "|" + (p.TargetUrl ?? ""));
                }
                foreach (KeyValuePair<string, GameTabControl> pair in runningClients) configLines.Add("Running=" + pair.Key);
                string tempPath = ConfigFilePath + ".tmp";
                File.WriteAllLines(tempPath, configLines.ToArray(), Encoding.UTF8);
                if (File.Exists(ConfigFilePath)) File.Delete(ConfigFilePath);
                File.Move(tempPath, ConfigFilePath);
            }
            catch { }
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
                            if (bool.TryParse(value.Substring("DiagnosticLogging=".Length).Trim(), out savedValue)) enabled = savedValue;
                            break;
                        }
                    }
                }
            }
            catch { }
            isLoadingDiagnosticLog = true; chkDiagnosticLog.Checked = enabled; isLoadingDiagnosticLog = false;
            WebViewManager.DiagnosticLoggingEnabled = enabled;
        }

        private void ChkDiagnosticLog_CheckedChanged(object sender, EventArgs e)
        {
            if (isLoadingDiagnosticLog || chkDiagnosticLog == null) return;
            WebViewManager.DiagnosticLoggingEnabled = chkDiagnosticLog.Checked; SaveConfig();
            if (chkDiagnosticLog.Checked) WebViewManager.WriteDiagnosticLog("System", "Diagnostic logging ENABLED by user.");
        }

        private static int GetClientNumber(string profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return 0;
            const string prefix = "Online_Web_Browser";
            if (!profileId.StartsWith(prefix, StringComparison.Ordinal)) return 0;
            string num = profileId.Substring(prefix.Length); int n;
            return int.TryParse(num, out n) ? n : 0;
        }

        private void UpdateAppTitle()
        {
            int count = runningClients.Count;
            Text = "Online Web Browser (" + count + " Client(s) Running)";
            if (lblRunningCount != null) lblRunningCount.Text = "Running: " + count;
        }

        private void RefreshClientItemUI(AccountProfile profile)
        {
            if (profile == null || !clientItems.ContainsKey(profile.ProfileId)) return;
            Panel item = clientItems[profile.ProfileId];
            if (item.Controls.Count >= 2 && item.Controls[1] is Label titleLabel) titleLabel.Text = profile.Title;
        }

        // =========================================================
        // 9. CONTEXT MENU & PROFILE EDITING
        // =========================================================
        private void InitializeContextMenu()
        {
            clientContextMenu = new ContextMenuStrip();
            ToolStripMenuItem startItem = new ToolStripMenuItem("เปิด Client");
            startItem.Click += delegate { if (!string.IsNullOrEmpty(selectedProfileId)) StartClientInstance(selectedProfileId); };
            ToolStripMenuItem stopItem = new ToolStripMenuItem("ปิด Client");
            stopItem.Click += delegate { if (!string.IsNullOrEmpty(selectedProfileId)) StopClientInstance(selectedProfileId); };
            ToolStripMenuItem settingsItem = new ToolStripMenuItem("ตั้งค่า Client...");
            settingsItem.Click += EditSelectedClient_Click;
            ToolStripSeparator sep1 = new ToolStripSeparator();
            ToolStripMenuItem deleteItem = new ToolStripMenuItem("ลบข้อมูลล็อกอินของจอนี้");
            deleteItem.Click += DeleteSelectedClient_Click;
            clientContextMenu.Items.AddRange(new ToolStripItem[] { startItem, stopItem, sep1, settingsItem, new ToolStripSeparator(), deleteItem });
        }

        private void EditSelectedClient_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedProfileId) || !profiles.ContainsKey(selectedProfileId)) return;
            AccountProfile profile = profiles[selectedProfileId];
            using (ClientSettingsForm dialog = new ClientSettingsForm(profile, Url))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                bool urlChanged = !string.Equals(profile.TargetUrl, dialog.ClientUrl, StringComparison.OrdinalIgnoreCase);
                bool titleChanged = !string.Equals(profile.Title, dialog.ClientTitle, StringComparison.Ordinal);
                if (!urlChanged && !titleChanged) return;
                profile.Title = dialog.ClientTitle; profile.TargetUrl = dialog.ClientUrl;
                RefreshClientItemUI(profile);
                if (selectedProfileId == profile.ProfileId) lblSelectedClient.Text = profile.Title;
                if (urlChanged && runningClients.ContainsKey(profile.ProfileId))
                    runningClients[profile.ProfileId].NavigateTo(profile.TargetUrl);
                SaveConfig();
            }
        }

        private void DeleteSelectedClient_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedProfileId) || !profiles.ContainsKey(selectedProfileId)) return;

            AccountProfile profile = profiles[selectedProfileId];
            DialogResult result = MessageBox.Show(
                "แน่ใจว่าต้องการลบข้อมูลล็อกอิน ชื่อ และ URL ของ " + profile.Title + " หรือไม่?",
                "ยืนยันการลบข้อมูล",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            StopClientInstance(profile.ProfileId);
            DeleteProfileDataFolder(profile.ProfileId);
            profile.Title = "Client " + GetClientNumber(profile.ProfileId);
            profile.TargetUrl = Url;
            RefreshClientItemUI(profile);

            if (selectedProfileId == profile.ProfileId)
            {
                lblSelectedClient.Text = profile.Title;
                if (lbURlnow != null) lbURlnow.Text = "URL: " + profile.TargetUrl;
            }
            SaveConfig();
        }

        private void DeleteProfileDataFolder(string profileId)
        {
            string profileFolder = WebViewManager.GetProfilePath(profileId);
            if (string.IsNullOrEmpty(profileFolder) || !Directory.Exists(profileFolder)) return;
            try { Directory.Delete(profileFolder, true); }
            catch { Task.Run(async delegate { await Task.Delay(2000); try { if (Directory.Exists(profileFolder)) Directory.Delete(profileFolder, true); } catch { } }); }
        }

        // =========================================================
        // 10. TRAY & FORM LIFECYCLE
        // =========================================================
        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            ToolStripMenuItem showMenuItem = new ToolStripMenuItem("แสดงหน้าต่างเกม");
            showMenuItem.Click += delegate { ShowFromSystemTray(); };
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("ปิดโปรแกรมทั้งหมด");
            exitMenuItem.Click += delegate { ExitApplication(); };
            trayMenu.Items.AddRange(new ToolStripItem[] { showMenuItem, exitMenuItem });

            Icon appIcon = null;
            try
            {
                if (Properties.Resources.sync_24dp_E3E3E3_FILL0_wght400_GRAD0_opsz24 != null)
                {
                    using (MemoryStream ms = new MemoryStream(Properties.Resources.sync_24dp_E3E3E3_FILL0_wght400_GRAD0_opsz24))
                        appIcon = new Icon(ms);
                }
            }
            catch { appIcon = SystemIcons.Application; }
            if (appIcon == null) appIcon = SystemIcons.Application;
            Icon = appIcon;

            trayIcon = new NotifyIcon { Text = "Online Web Browser", Icon = appIcon, ContextMenuStrip = trayMenu, Visible = false };
            trayIcon.DoubleClick += delegate { ShowFromSystemTray(); };
        }

        private void HideToSystemTray()
        {
            if (trayIcon == null) return;
            isHiddenToTray = true; trayIcon.Visible = true; Hide(); UpdateAllClientsActiveState();
            trayIcon.ShowBalloonTip(2000, "Online Web Browser", "โปรแกรมกำลังทำงานอยู่เบื้องหลัง", ToolTipIcon.Info);
        }

        private void ShowFromSystemTray()
        {
            if (trayIcon == null) return;
            isHiddenToTray = false; Show(); WindowState = FormWindowState.Normal;
            BringToFront(); Activate(); trayIcon.Visible = false;
            UpdateAllClientsActiveState(); RelayoutClientGrid();
        }

        private void CloseAllClients()
        {
            List<GameTabControl> clients = new List<GameTabControl>(runningClients.Values);
            runningClients.Clear();
            foreach (GameTabControl gameControl in clients)
            {
                try { if (gameControl.Parent != null) gameControl.Parent.Controls.Remove(gameControl); } catch { }
                try { gameControl.Dispose(); } catch { }
            }
        }

        private void ExitApplication()
        {
            isClosingConfirmed = true; SaveConfig(); CloseAllClients();
            if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); trayIcon = null; }
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!isClosingConfirmed && e.CloseReason == CloseReason.UserClosing && runningClients.Count > 0)
            {
                DialogResult r = MessageBox.Show(
                    "มี Client กำลังรันอยู่ " + runningClients.Count + " จอ\n\nต้องการปิดโปรแกรมทั้งหมดหรือไม่?",
                    "ยืนยันปิดโปรแกรม", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) { e.Cancel = true; return; }
            }
            SaveConfig(); CloseAllClients();
            if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); trayIcon = null; }
            base.OnFormClosing(e);
        }

        public void LinkStart(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("ไม่สามารถเปิดเบราว์เซอร์ได้: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void MainForm_Load(object sender, EventArgs e)
        { 


        }
    }
}