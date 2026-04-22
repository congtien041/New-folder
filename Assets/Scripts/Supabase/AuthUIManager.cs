using UnityEngine;
using TMPro; 

namespace SimpleFPS 
{
    public class AuthUIManager : MonoBehaviour
    {
        [Header("Giao diện")]
        public GameObject AuthPanel;         
        public GameObject FusionMenuPanel;   

        [Header("Ô nhập liệu")]
        public TMP_InputField EmailInput;
        public TMP_InputField PasswordInput;
        public TMP_InputField UsernameInput;
        public TextMeshProUGUI MessageText;

        [Header("Giao diện Đổi Pass (OTP)")]
        public GameObject ResetPassPanel; 
        public TMP_InputField ResetEmailInput;
        public TMP_InputField OtpInput;
        public TMP_InputField NewPasswordInput;
        public TextMeshProUGUI ResetMessageText;

        [Header("Thông tin Vàng và Rank")]
        public TextMeshProUGUI GoldText; // Kéo thả chữ Vàng vào đây
        public RankDisplay RankDisplayComp; // Kéo thả GameObject chứa script RankDisplay vào đây
        public TextMeshProUGUI UsernameText; // <--- THÊM DÒNG NÀY (Nhớ kéo thả Object tên vào đây ở Inspector)


        private void Start()
        {
            // Kiểm tra xem SupabaseManager đã có sẵn và đang đăng nhập chưa
            if (SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
            {
                // Nếu đã đăng nhập rồi (từ trong trận thoát ra) -> Bật thẳng Menu Fusion
                AuthPanel.SetActive(false);
                FusionMenuPanel.SetActive(true);
                MessageText.text = "";
                RefreshProfileUI();

                // --- BỔ SUNG: ÉP GIAO DIỆN CẬP NHẬT LẠI SỐ VÀNG VÀ RANK ---
                var profile = SupabaseManager.Instance.CurrentProfile;
                if (profile != null)
                {
                    if (GoldText != null) GoldText.text = profile.Gold.ToString();
                    if (RankDisplayComp != null) RankDisplayComp.UpdateRank(profile.RankPoints);
                    Debug.Log("[MENU] Đã load lại Vàng và Rank từ RAM!");
                }
            }
            else
            {
                // Nếu chưa đăng nhập (mới mở game lên) -> Hiện bảng Đăng nhập
                AuthPanel.SetActive(true);
                FusionMenuPanel.SetActive(false);
                MessageText.text = "";
            }
        }

        public async void OnLoginClick()
        {
            MessageText.text = "Đang kết nối server...";
            
            bool success = await SupabaseManager.Instance.Login(EmailInput.text, PasswordInput.text);
            
            if (success)
            {
                OnAutoLoginSuccess();
            }
            else
            {
                MessageText.text = "Sai tài khoản, hoặc máy này không khớp với tài khoản!";
            }
        }

        public async void OnRegisterClick()
        {
            if (string.IsNullOrEmpty(UsernameInput.text))
            {
                MessageText.text = "Vui lòng nhập tên nhân vật!";
                return;
            }

            MessageText.text = "Đang tạo tài khoản...";
            bool success = await SupabaseManager.Instance.Register(EmailInput.text, PasswordInput.text, UsernameInput.text);
            
            if (success)
            {
                MessageText.text = "Đăng ký thành công! Hãy bấm Đăng Nhập.";
            }
            else
            {
                MessageText.text = "Lỗi đăng ký (Email đã tồn tại hoặc mật khẩu quá ngắn).";
            }
        }

        // Hàm này được gọi khi login bằng tay thành công hoặc auto login thành công
        // Hàm này được gọi khi login bằng tay thành công hoặc auto login thành công
        public void OnAutoLoginSuccess()
        {
            MessageText.text = "Đăng nhập thành công!";
            AuthPanel.SetActive(false);

            PlayerPrefs.SetString("Photon.Menu.Username", SupabaseManager.Instance.CurrentProfile.Username);
            PlayerPrefs.SetString("Photon.Menu.Character", SupabaseManager.Instance.CurrentProfile.CurrentCharacter);
            PlayerPrefs.Save();

            FusionMenuPanel.SetActive(true);

            // --- CẬP NHẬT GIAO DIỆN VÀNG VÀ RANK ---
            var profile = SupabaseManager.Instance.CurrentProfile;
            if (profile != null)
            {
                if (GoldText != null) GoldText.text = profile.Gold.ToString();
                if (RankDisplayComp != null) RankDisplayComp.UpdateRank(profile.RankPoints);
            }
        }

        // HÀM GẮN VÀO NÚT ĐĂNG XUẤT TRÊN MENU
        public void OnLogoutClick()
        {
            if (SupabaseManager.Instance != null)
            {
                SupabaseManager.Instance.SignOut();
            }


            if (UsernameText != null) UsernameText.text = "---";
            PlayerPrefs.DeleteKey("Photon.Menu.Username");
            // --- THÊM DÒNG NÀY ĐỂ XÓA BÓNG MA NHÂN VẬT ---
            PlayerPrefs.DeleteKey("Photon.Menu.Character"); 
            PlayerPrefs.Save();

            FusionMenuPanel.SetActive(false);
            AuthPanel.SetActive(true);
            
            // Ép tượng nhân vật quay về mặc định ngay lập tức khi đăng xuất
            var display = FindObjectOfType<LobbyCharacterDisplay>();
            if (display != null) display.UpdateDisplay();

            EmailInput.text = "";
            PasswordInput.text = "";
            MessageText.text = "Bạn đã đăng xuất an toàn.";
        }

        // SỬA LẠI HÀM NÀY
        public async void OnForgotPasswordClick()
        {
            if (string.IsNullOrEmpty(EmailInput.text))
            {
                MessageText.text = "Vui lòng nhập Email vào ô trên để lấy mã!";
                return;
            }

            MessageText.text = "Đang gửi mã OTP về email...";
            bool success = await SupabaseManager.Instance.ResetPassword(EmailInput.text);
            
            if (success)
            {
                // Tắt bảng Đăng nhập, Bật bảng nhập OTP lên
                AuthPanel.SetActive(false);
                ResetPassPanel.SetActive(true);
                
                ResetEmailInput.text = EmailInput.text; // Copy sẵn email qua cho rảnh tay
                ResetMessageText.text = "Hãy kiểm tra Email và nhập mã OTP (8 số) vào đây.";
            }
            else
            {
                MessageText.text = "Lỗi: Không thể gửi email.";
            }
        }

        // THÊM HÀM MỚI NÀY DÀNH CHO NÚT "XÁC NHẬN ĐỔI PASS"
        public async void OnConfirmOtpClick()
        {
            if (string.IsNullOrEmpty(OtpInput.text) || string.IsNullOrEmpty(NewPasswordInput.text))
            {
                ResetMessageText.text = "Vui lòng nhập đủ Mã OTP và Mật khẩu mới!";
                return;
            }

            ResetMessageText.text = "Đang xác thực và đổi mật khẩu...";

            // Gọi hàm bên SupabaseManager
            bool success = await SupabaseManager.Instance.VerifyOtpAndChangePassword(ResetEmailInput.text, OtpInput.text, NewPasswordInput.text);

            if (success)
            {
                // Đổi thành công -> Trả về màn hình đăng nhập
                ResetPassPanel.SetActive(false);
                AuthPanel.SetActive(true);
                PasswordInput.text = ""; // Xóa trắng ô pass cũ
                MessageText.text = "✅ Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
            }
            else
            {
                ResetMessageText.text = "❌ Lỗi: Mã OTP sai hoặc đã hết hạn!";
            }
        }


        // --- HÀM NÀY ĐỂ CÁC NÚT MUA SẮM GỌI ĐẾN ĐỂ ÉP UI NHẢY SỐ ---
        public void RefreshProfileUI()
        {
            if (SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
            {
                var profile = SupabaseManager.Instance.CurrentProfile;
                if (profile != null)
                {
                    if (GoldText != null) GoldText.text = profile.Gold.ToString();
                    if (RankDisplayComp != null) RankDisplayComp.UpdateRank(profile.RankPoints);
                    Debug.Log("[UI] Đã cập nhật lại số Vàng trên màn hình: " + profile.Gold);

                    // --- THÊM DÒNG NÀY ĐỂ ĐUỔI "BÓNG MA" TÊN CŨ ---
                    if (UsernameText != null) UsernameText.text = profile.Username; 
                    
                    Debug.Log("[UI] Đã cập nhật Tên: " + profile.Username);
                }
            }
        }
        
    }
}