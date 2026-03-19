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

        private void Start()
        {
            // --- ĐOẠN CODE SỬA LỖI ẨN MENU KHI LEAVE GAME ---
            // Kiểm tra xem SupabaseManager đã có sẵn và đang đăng nhập chưa
            if (SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
            {
                // Nếu đã đăng nhập rồi (từ trong trận thoát ra) -> Bật thẳng Menu Fusion
                AuthPanel.SetActive(false);
                FusionMenuPanel.SetActive(true);
                MessageText.text = "";
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
        public void OnAutoLoginSuccess()
        {
            MessageText.text = "Đăng nhập thành công!";
            AuthPanel.SetActive(false);

            // --- ĐOẠN CODE BÍ MẬT ---
            // Lưu tên từ DB vào bộ nhớ của Unity để Lát nữa Fusion bật lên nó tự lấy ra dùng
            PlayerPrefs.SetString("Photon.Menu.Username", SupabaseManager.Instance.CurrentProfile.Username);
            PlayerPrefs.Save();
            // ------------------------

            FusionMenuPanel.SetActive(true);
        }

        // HÀM GẮN VÀO NÚT ĐĂNG XUẤT TRÊN MENU
        public void OnLogoutClick()
        {
            // 1. Gọi lệnh xóa tài khoản
            if (SupabaseManager.Instance != null)
            {
                SupabaseManager.Instance.SignOut();
            }

            // 2. Giấu Menu Fusion đi, bật lại bảng AuthPanel
            FusionMenuPanel.SetActive(false);
            AuthPanel.SetActive(true);
            
            // 3. Xóa trắng các ô chữ và hiện thông báo
            EmailInput.text = "";
            PasswordInput.text = "";
            MessageText.text = "Bạn đã đăng xuất an toàn.";
        }

        
    }
}