namespace LumivaraMultiClient.Models
{
    public class AccountProfile
    {
        /// <summary>
        /// ตัวระบุโปรไฟล์ของบัญชีผู้ใช้ (Profile ID) ซึ่งเป็นค่าที่ไม่ซ้ำกันสำหรับแต่ละโปรไฟล์
        /// </summary>
        public string ProfileId { get; set; }
        public string Title { get; set; }
        public string TargetUrl { get; set; }

    }
}