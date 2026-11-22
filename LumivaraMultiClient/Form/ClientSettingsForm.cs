using System;
using System.Drawing;
using System.Windows.Forms;
using LumivaraMultiClient.Models;

namespace LumivaraMultiClient.Forms
{
    /// <summary>
    /// ฟอร์มตั้งค่า Client (สร้างControls ในโค้ดทั้งหมด ไม่ใช้ Designer)
    /// </summary>
    public class ClientSettingsForm : Form
    {
        private TextBox txtTitle;
        private TextBox txtUrl;
        private Button btnOk;
        private Button btnCancel;
        private Button btnReset;

        public string ClientTitle { get; private set; }
        public string ClientUrl { get; private set; }

        private readonly string defaultUrl;

        public ClientSettingsForm(AccountProfile profile, string defaultUrl)
        {
            this.defaultUrl = defaultUrl ?? "";

            // ---------- ตั้งค่า Form ----------
            this.Text = "ตั้งค่า Client - " + (profile != null ? profile.Title : "");
            this.ClientSize = new Size(540, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Font = new Font("Segoe UI", 9f);

            // ---------- Label: ชื่อ ----------
            Label lblTitle = new Label
            {
                Text = "ชื่อที่แสดง:",
                Location = new Point(15, 20),
                AutoSize = true
            };

            txtTitle = new TextBox
            {
                Location = new Point(120, 17),
                Width = 390,
                Text = profile != null ? (profile.Title ?? "") : ""
            };

            // ---------- Label: URL ----------
            Label lblUrl = new Label
            {
                Text = "URL ของจอนี้:",
                Location = new Point(15, 60),
                AutoSize = true
            };

            txtUrl = new TextBox
            {
                Location = new Point(120, 57),
                Width = 390,
                Text = (profile != null && !string.IsNullOrEmpty(profile.TargetUrl))
                    ? profile.TargetUrl
                    : this.defaultUrl
            };

            // ---------- ปุ่ม Reset URL ----------
            btnReset = new Button
            {
                Text = "ใช้ URL เริ่มต้น",
                Location = new Point(120, 92),
                Width = 130,
                Height = 26
            };
            btnReset.Click += (s, e) => txtUrl.Text = this.defaultUrl;

            // ---------- ปุ่ม บันทึก ----------
            btnOk = new Button
            {
                Text = "บันทึก",
                Location = new Point(325, 132),
                Width = 85,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            btnOk.Click += BtnOk_Click;

            // ---------- ปุ่ม ยกเลิก ----------
            btnCancel = new Button
            {
                Text = "ยกเลิก",
                Location = new Point(425, 132),
                Width = 85,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };

            // ---------- เพิ่มControls เข้า Form ----------
            this.Controls.Add(lblTitle);
            this.Controls.Add(txtTitle);
            this.Controls.Add(lblUrl);
            this.Controls.Add(txtUrl);
            this.Controls.Add(btnReset);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            string title = (txtTitle.Text ?? "").Trim();
            string url = (txtUrl.Text ?? "").Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("กรุณากรอกชื่อ Client", "ข้อมูลไม่ครบ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (!IsValidUrl(url))
            {
                MessageBox.Show("URL ไม่ถูกต้อง (ต้องขึ้นต้นด้วย http:// หรือ https://)",
                    "URL ไม่ถูกต้อง", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            ClientTitle = title;
            ClientUrl = url;
        }

        private static bool IsValidUrl(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}