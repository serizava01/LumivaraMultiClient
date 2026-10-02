using LumivaraMultiClient.Controls;
using LumivaraMultiClient.Models;
using System;
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
 
        // ระบบ Tray Icon สำหรับซ่อนแอปเบื้องหลัง
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
        /// <summary>
        /// กำหนดค่า UI หลักของแอปพลิเคชัน
        /// </summary>
        private void InitializeMainUI()
        {
            this.Text = "Lumivara Online - Multi Client Manager";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            topMenuPanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.LightGray };

            Button btnAddTab = new Button
            {
                Text = "+ เพิ่ม Client",
                Width = 130,
                Height = 30,
                Location = new Point(10, 5)
            };
            btnAddTab.Click += (s, e) => AddNewGameInstance();

            Button btnHideToTray = new Button
            {
                Text = "ซ่อนไปยัง Tray",
                Width = 110,
                Height = 30,
                Location = new Point(150, 5)
            };
            btnHideToTray.Click += (s, e) => HideToSystemTray();

   
            Button Wiki = new Button
            {
                Text = "ดู Wiki",
                Width = 90,
                Height = 30,
                Location = new Point(270, 5)
            };
            Wiki.Click += (s, e) => LinkStart("https://lumivaraonline.com/wiki/#overview");


       
            Button Updates = new Button
            {
                Text = "เช็คอัพเดต",
                Width = 100,
                Height = 30,
                Location = new Point(370, 5)
            };
            Updates.Click += (s, e) => LinkStart("https://lumivaraonline.com/changelog/");
            
            Button LinkFB = new Button
            {
                Text = "เฟสบุคพัฒนา",
                Width = 100,
                Height = 30,
                Location = new Point(480, 5)
            };
            LinkFB.Click += (s, e) => LinkStart("https://www.facebook.com/PLAMSsE/");

            Button Github = new Button
            {
                Text = "GitHubผู้พัฒนา",
                Width = 100,
                Height = 30,
                Location = new Point(590, 5)
            };
            Github.Click += (s, e) => LinkStart("https://github.com/serizava01");

            topMenuPanel.Controls.Add(btnAddTab);
            topMenuPanel.Controls.Add(btnHideToTray);
            topMenuPanel.Controls.Add(Wiki);
            topMenuPanel.Controls.Add(Updates);
            topMenuPanel.Controls.Add(Github);
            topMenuPanel.Controls.Add(LinkFB);

            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.MouseClick += TabControl_MouseClick;

            this.Controls.Add(tabControl);
            this.Controls.Add(topMenuPanel);
        }
        /// 
        /// สร้างระบบ Tray Icon ตรงมุมขวาล่างแถบ Taskbar
        /// 

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();

            var showMenuItem = new ToolStripMenuItem("แสดงหน้าต่างเกม");
            showMenuItem.Click += (s, e) => ShowFromSystemTray();

            var exitMenuItem = new ToolStripMenuItem("ปิดโปรแกรมทั้งหมด");
            exitMenuItem.Click += (s, e) => ExitApplication();

            trayMenu.Items.Add(showMenuItem);
            trayMenu.Items.Add(exitMenuItem);
            Icon appIcon;
            using (MemoryStream ms = new MemoryStream(Properties.Resources.LumivaraMultiClient))
            {
                appIcon = new Icon(ms);
            }
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
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Exit();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            base.OnFormClosing(e);
        }
        /// <summary>
        /// สร้างเมนูคลิกขวาสำหรับแท็บ
        /// </summary>
        private void InitializeContextMenu()
        {
            tabContextMenu = new ContextMenuStrip();
            var deleteMenuItem = new ToolStripMenuItem("ลบหน้าต่าง(เคลียร์ข้อมูลทั้งหมด)");
            deleteMenuItem.Click += DeleteSelectedTab_Click;
            tabContextMenu.Items.Add(deleteMenuItem);
        }
        private void LoadSavedInstances()
        {
            int savedCount = 1;

            if (File.Exists(ConfigFilePath))
            {
                string text = File.ReadAllText(ConfigFilePath);
                int.TryParse(text, out savedCount);
            }

            if (savedCount < 1) savedCount = 1;

            for (int i = 1; i <= savedCount; i++)
            {
                CreateTabForProfile("Lumivara_Acc_" + i, "Client " + i);
            }

            UpdateAppTitle();
        }
        public void AddNewGameInstance()
        {
            if (tabControl.TabPages.Count >= 3)
            {
                MessageBox.Show(
                    "เกมจำกัดให้เปิดได้สูงสุด 3 จอเท่านั้นครับ",
                    "แจ้งเตือนจำกัดจำนวนจอ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            int nextIndex = tabControl.TabPages.Count + 1;
            CreateTabForProfile("Lumivara_Acc_" + nextIndex, "Client " + nextIndex);

            SaveConfig();
            UpdateAppTitle();
        }
        /// <summary>
        /// เปิดลิงก์ในเบราว์เซอร์เริ่มต้นของผู้ใช้
        /// </summary>
        /// <param name="url"></param>
        public void LinkStart(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
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
                MessageBox.Show($"ไม่สามารถเปิดเบราว์เซอร์ได้: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// สร้างแท็บใหม่สำหรับโปรไฟล์ที่กำหนด
        /// </summary>
        /// <param name="profileId"></param>
        /// <param name="title"></param>
        private void CreateTabForProfile(string profileId, string title)
        {
            var profile = new AccountProfile
            {
                ProfileId = profileId,
                Title = title,
                TargetUrl = "https://lumivaraonline.com/"
            };

            var gameControl = new GameTabControl(profile);
            var tabPage = new TabPage(profile.Title)
            {
                Tag = profile
            };

            tabPage.Controls.Add(gameControl);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;
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
        private void DeleteSelectedTab_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == null) return;

            TabPage selectedTab = tabControl.SelectedTab;
            AccountProfile profile = selectedTab.Tag as AccountProfile;

            var result = MessageBox.Show(
                "แน่ใจว่าต้องการลบ " + selectedTab.Text + " และเคลียร์ข้อมูลทั้งหมด?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                if (selectedTab.Controls.Count > 0 && selectedTab.Controls[0] is GameTabControl gameControl)
                {
                    gameControl.WebView?.Dispose();
                }

                tabControl.TabPages.Remove(selectedTab);
                selectedTab.Dispose();

                if (profile != null)
                {
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
                        if (Directory.Exists(profileFolder))
                        {
                            Directory.Delete(profileFolder, true);
                        }
                    }
                    catch { }
                });
            }
        }
        private void SaveConfig()
        {
            try
            {
                if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
                File.WriteAllText(ConfigFilePath, tabControl.TabPages.Count.ToString());
            }
            catch { }
        }
        private void UpdateAppTitle()
        {
            this.Text = $"Lumivara Online - Multi Client Manager ({tabControl.TabPages.Count} Client(s) Loaded)";
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}