using LumivaraMultiClient.Controls;
using LumivaraMultiClient.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LumivaraMultiClient.Forms
{
    public partial class MainForm : Form
    {
        private TabControl tabControl;
        private Panel topMenuPanel;
        private ContextMenuStrip tabContextMenu;

        private CheckBox chkClient1;
        private CheckBox chkClient2;
        private CheckBox chkClient3;

        private Label lblStatus1;
        private Label lblStatus2;
        private Label lblStatus3;

        private bool isInitializing = true;

        // ระบบ Tray Icon
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        private readonly string basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LumivaraMultiClient"
        );

        private string ConfigFilePath => Path.Combine(basePath, "config.txt");

        public MainForm()
        {
            InitializeComponent();
            InitializeMainUI();
            InitializeContextMenu();
            InitializeTrayIcon();
            LoadSavedInstances();
        }

        private void InitializeMainUI()
        {
            this.Text = "Lumivara Online - Multi Client Manager";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            topMenuPanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(240, 240, 240) };

            // Client 1 Group
            lblStatus1 = new Label
            {
                Text = "●",
                AutoSize = true,
                Location = new Point(8, 10),
                Font = new Font("Segoe UI Symbol", 10f),
                ForeColor = Color.Red
            };
            chkClient1 = new CheckBox { Text = "Client 1", AutoSize = true, Location = new Point(30, 8), Tag = "Lumivara_Acc_1" };

            // Client 2 Group
            lblStatus2 = new Label
            {
                Text = "●",
                AutoSize = true,
                Location = new Point(118, 10),
                Font = new Font("Segoe UI Symbol", 10f),
                ForeColor = Color.Red
            };
            chkClient2 = new CheckBox { Text = "Client 2", AutoSize = true, Location = new Point(140, 8), Tag = "Lumivara_Acc_2" };

            // Client 3 Group
            lblStatus3 = new Label
            {
                Text = "●",
                AutoSize = true,
                Location = new Point(228, 10),
                Font = new Font("Segoe UI Symbol", 10f),
                ForeColor = Color.Red
            };
            chkClient3 = new CheckBox { Text = "Client 3", AutoSize = true, Location = new Point(250, 8), Tag = "Lumivara_Acc_3" };

            // ผูก Event การติ๊ก
            chkClient1.CheckedChanged += ClientCheckBox_CheckedChanged;
            chkClient2.CheckedChanged += ClientCheckBox_CheckedChanged;
            chkClient3.CheckedChanged += ClientCheckBox_CheckedChanged;

            Button btnHideToTray = new Button
            {
                Text = "ซ่อนไปยัง Tray",
                Width = 100,
                Height = 28,
                Location = new Point(350, 6)
            };
            btnHideToTray.Click += (s, e) => HideToSystemTray();

            Button Wiki = new Button
            {
                Text = "ดู Wiki",
                Width = 75,
                Height = 28,
                Location = new Point(460, 6)
            };
            Wiki.Click += (s, e) => LinkStart("https://lumivaraonline.com/wiki/#overview");

            Button Updates = new Button
            {
                Text = "เช็คอัพเดต",
                Width = 85,
                Height = 28,
                Location = new Point(545, 6)
            };
            Updates.Click += (s, e) => LinkStart("https://lumivaraonline.com/changelog/");

            Button Github = new Button
            {
                Text = "GitHubผู้พัฒนา",
                Width = 100,
                Height = 28,
                Location = new Point(640, 6)
            };
            Github.Click += (s, e) => LinkStart("https://github.com/serizava01");

            topMenuPanel.Controls.Add(lblStatus1);
            topMenuPanel.Controls.Add(chkClient1);
            topMenuPanel.Controls.Add(lblStatus2);
            topMenuPanel.Controls.Add(chkClient2);
            topMenuPanel.Controls.Add(lblStatus3);
            topMenuPanel.Controls.Add(chkClient3);
            topMenuPanel.Controls.Add(btnHideToTray);
            topMenuPanel.Controls.Add(Wiki);
            topMenuPanel.Controls.Add(Updates);
            topMenuPanel.Controls.Add(Github);

            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.MouseClick += TabControl_MouseClick;

            EnsureAllTabsExist();

            this.Controls.Add(tabControl);
            this.Controls.Add(topMenuPanel);
        }

        private void EnsureAllTabsExist()
        {
            CreateTabSkeleton("Lumivara_Acc_1", "Client 1");
            CreateTabSkeleton("Lumivara_Acc_2", "Client 2");
            CreateTabSkeleton("Lumivara_Acc_3", "Client 3");
        }

        private void CreateTabSkeleton(string profileId, string title)
        {
            var profile = new AccountProfile
            {
                ProfileId = profileId,
                Title = title,
                TargetUrl = "https://lumivaraonline.com/"
            };

            var tabPage = new TabPage(profile.Title)
            {
                Tag = profile
            };

            tabControl.TabPages.Add(tabPage);
        }

        private void ClientCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            if (sender is CheckBox chk && chk.Tag is string profileId)
            {
                if (chk.Checked)
                {
                    StartClientInstance(profileId);
                    UpdateStatusIcon(profileId, true);
                }
                else
                {
                    StopClientInstance(profileId);
                    UpdateStatusIcon(profileId, false);
                }

                SaveConfig();
                UpdateAppTitle();
            }
        }

        private void StartClientInstance(string profileId)
        {
            TabPage page = GetTabPageByProfileId(profileId);
            if (page != null && page.Controls.Count == 0)
            {
                if (page.Tag is AccountProfile profile)
                {
                    var gameControl = new GameTabControl(profile);
                    page.Controls.Add(gameControl);
                    tabControl.SelectedTab = page;
                }
            }
        }

        private void StopClientInstance(string profileId)
        {
            TabPage page = GetTabPageByProfileId(profileId);
            if (page != null && page.Controls.Count > 0)
            {
                if (page.Controls[0] is GameTabControl gameControl)
                {
                    gameControl.WebView?.Dispose();
                }
                page.Controls.Clear();
            }
        }

        private TabPage GetTabPageByProfileId(string profileId)
        {
            foreach (TabPage page in tabControl.TabPages)
            {
                if (page.Tag is AccountProfile profile && profile.ProfileId == profileId)
                {
                    return page;
                }
            }
            return null;
        }

        private void UpdateStatusIcon(string profileId, bool isActive)
        {
            Color statusColor = isActive ? Color.LimeGreen : Color.Red;

            if (profileId == "Lumivara_Acc_1") lblStatus1.ForeColor = statusColor;
            else if (profileId == "Lumivara_Acc_2") lblStatus2.ForeColor = statusColor;
            else if (profileId == "Lumivara_Acc_3") lblStatus3.ForeColor = statusColor;
        }

        private void LoadSavedInstances()
        {
            isInitializing = true;

            var activeProfiles = new List<string>();
            if (File.Exists(ConfigFilePath))
            {
                string[] lines = File.ReadAllLines(ConfigFilePath);
                if (lines.Length == 1 && int.TryParse(lines[0].Trim(), out int count))
                {
                    for (int i = 1; i <= Math.Min(count, 3); i++)
                    {
                        activeProfiles.Add("Lumivara_Acc_" + i);
                    }
                }
                else
                {
                    foreach (string line in lines)
                    {
                        string pId = line.Trim();
                        if (!string.IsNullOrEmpty(pId)) activeProfiles.Add(pId);
                    }
                }
            }

            if (activeProfiles.Count == 0)
            {
                activeProfiles.Add("Lumivara_Acc_1");
            }

            chkClient1.Checked = activeProfiles.Contains("Lumivara_Acc_1");
            chkClient2.Checked = activeProfiles.Contains("Lumivara_Acc_2");
            chkClient3.Checked = activeProfiles.Contains("Lumivara_Acc_3");

            if (chkClient1.Checked) { StartClientInstance("Lumivara_Acc_1"); UpdateStatusIcon("Lumivara_Acc_1", true); }
            else UpdateStatusIcon("Lumivara_Acc_1", false);

            if (chkClient2.Checked) { StartClientInstance("Lumivara_Acc_2"); UpdateStatusIcon("Lumivara_Acc_2", true); }
            else UpdateStatusIcon("Lumivara_Acc_2", false);

            if (chkClient3.Checked) { StartClientInstance("Lumivara_Acc_3"); UpdateStatusIcon("Lumivara_Acc_3", true); }
            else UpdateStatusIcon("Lumivara_Acc_3", false);

            isInitializing = false;
            SaveConfig();
            UpdateAppTitle();
        }

        private void SaveConfig()
        {
            try
            {
                if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

                var activeProfiles = new List<string>();
                if (chkClient1.Checked) activeProfiles.Add("Lumivara_Acc_1");
                if (chkClient2.Checked) activeProfiles.Add("Lumivara_Acc_2");
                if (chkClient3.Checked) activeProfiles.Add("Lumivara_Acc_3");

                File.WriteAllLines(ConfigFilePath, activeProfiles.ToArray());
            }
            catch { }
        }

        private void InitializeContextMenu()
        {
            tabContextMenu = new ContextMenuStrip();
            var deleteMenuItem = new ToolStripMenuItem("ลบข้อมูลล็อกอินของจอนี้ (Clear Data)");
            deleteMenuItem.Click += DeleteSelectedTab_Click;
            tabContextMenu.Items.Add(deleteMenuItem);
        }

        private void DeleteSelectedTab_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == null) return;

            TabPage selectedTab = tabControl.SelectedTab;
            AccountProfile profile = selectedTab.Tag as AccountProfile;

            var result = MessageBox.Show(
                "แน่ใจว่าต้องการลบข้อมูลล็อกอินของ " + selectedTab.Text + " หรือไม่?",
                "ยืนยันการลบข้อมูล",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                if (profile != null)
                {
                    if (profile.ProfileId == "Lumivara_Acc_1") chkClient1.Checked = false;
                    else if (profile.ProfileId == "Lumivara_Acc_2") chkClient2.Checked = false;
                    else if (profile.ProfileId == "Lumivara_Acc_3") chkClient3.Checked = false;

                    DeleteProfileDataFolder(profile.ProfileId);
                }

                SaveConfig();
                UpdateAppTitle();
            }
        }

        private void DeleteProfileDataFolder(string profileId)
        {
            string profileFolder = Path.Combine(basePath, "Profiles", profileId);
            if (!Directory.Exists(profileFolder)) return;

            try
            {
                Directory.Delete(profileFolder, true);
            }
            catch
            {
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    try
                    {
                        if (Directory.Exists(profileFolder)) Directory.Delete(profileFolder, true);
                    }
                    catch { }
                });
            }
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            var showMenuItem = new ToolStripMenuItem("แสดงหน้าต่างเกม");
            showMenuItem.Click += (s, e) => ShowFromSystemTray();

            var exitMenuItem = new ToolStripMenuItem("ปิดโปรแกรมทั้งหมด");
            exitMenuItem.Click += (s, e) => ExitApplication();

            trayMenu.Items.Add(showMenuItem);
            trayMenu.Items.Add(exitMenuItem);

            Icon appIcon = null;
            try
            {
                if (Properties.Resources.LumivaraMultiClient != null)
                {
                    using (MemoryStream ms = new MemoryStream(Properties.Resources.LumivaraMultiClient))
                    {
                        appIcon = new Icon(ms);
                    }
                }
            }
            catch
            {
                appIcon = SystemIcons.Application;
            }

            if (appIcon == null) appIcon = SystemIcons.Application;

            this.Icon = appIcon;
            trayIcon = new NotifyIcon
            {
                Text = "Lumivara Multi Client",
                Icon = appIcon,
                ContextMenuStrip = trayMenu,
                Visible = false
            };

            trayIcon.DoubleClick += (s, e) => ShowFromSystemTray();
        }

        private void HideToSystemTray()
        {
            this.Hide();
            trayIcon.Visible = true;
            trayIcon.ShowBalloonTip(2000, "Lumivara Multi Client", "โปรแกรมกำลังทำงานอยู่เบื้องหลัง ดับเบิ้ลคลิกที่ไอคอนเพื่อเปิดกลับมา", ToolTipIcon.Info);
        }

        private void ShowFromSystemTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            trayIcon.Visible = false;
        }

        private void ExitApplication()
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }

        private void TabControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                for (int i = 0; i < tabControl.TabPages.Count; i++)
                {
                    Rectangle tabRect = tabControl.GetTabRect(i);
                    if (tabRect.Contains(e.Location))
                    {
                        tabControl.SelectedIndex = i;
                        tabContextMenu.Show(tabControl, e.Location);
                        break;
                    }
                }
            }
        }

        public void LinkStart(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ไม่สามารถเปิดเบราว์เซอร์ได้: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateAppTitle()
        {
            int runningCount = 0;
            if (chkClient1.Checked) runningCount++;
            if (chkClient2.Checked) runningCount++;
            if (chkClient3.Checked) runningCount++;

            this.Text = $"Lumivara Online - Multi Client Manager ({runningCount} Client(s) Running)";
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}