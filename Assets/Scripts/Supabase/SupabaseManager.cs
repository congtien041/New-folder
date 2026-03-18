using System;
using System.Threading.Tasks;
using UnityEngine;
using Supabase;
using Client = Supabase.Client;

namespace SimpleFPS
{
    public class SupabaseManager : MonoBehaviour
    {
        public static SupabaseManager Instance;

        [Header("Supabase Config")]
        public string SupabaseUrl = "https://ebdytrnnavfvgjkjysqr.supabase.co";
        public string SupabaseAnonKey = "sb_publishable_0ONS0ziLjfkpkL96Bt-kHg_pBcgw_aB";

        private Client _supabase;
        
        // Biến lưu dữ liệu người chơi hiện tại để game lấy ra dùng
        public PlayerProfile CurrentProfile { get; private set; }
        public bool IsLoggedIn => CurrentProfile != null;

        private async void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            DontDestroyOnLoad(gameObject);

            // Khởi tạo Supabase
            var options = new SupabaseOptions { AutoConnectRealtime = true, AutoRefreshToken = true };
            _supabase = new Client(SupabaseUrl, SupabaseAnonKey, options);
            await _supabase.InitializeAsync();

            // Thử tự động đăng nhập khi mở game
            await AutoLoginAsync();
        }

        public async Task AutoLoginAsync()
        {
            // --- ĐOẠN CODE TÌM VÉ TRONG Ổ CỨNG ---
            string accessToken = PlayerPrefs.GetString("supa_access", "");
            string refreshToken = PlayerPrefs.GetString("supa_refresh", "");

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
            {
                try
                {
                    // Dùng vé để khôi phục phiên đăng nhập
                    var session = await _supabase.Auth.SetSession(accessToken, refreshToken);
                    
                    if (session != null && session.User != null)
                    {
                        bool isValidDevice = await VerifyAndLoadProfile(session.User.Id);
                        if (isValidDevice)
                        {
                            Debug.Log($"Tự động đăng nhập thành công! Chào {CurrentProfile.Username}, Vàng: {CurrentProfile.Gold}");
                            var authUI = FindObjectOfType<AuthUIManager>();
                            if (authUI != null) authUI.OnAutoLoginSuccess();
                        }
                    }
                }
                catch (Exception)
                {
                    Debug.LogWarning("Phiên đăng nhập đã quá hạn, vui lòng đăng nhập lại bằng tay.");
                    // Vé hết hạn thì xé vé cũ đi
                    PlayerPrefs.DeleteKey("supa_access");
                    PlayerPrefs.DeleteKey("supa_refresh");
                }
            }
        }

        public async Task<bool> Register(string email, string password, string username)
        {
            try
            {
                var session = await _supabase.Auth.SignUp(email, password);
                if (session == null || session.User == null) return false;

                var newProfile = new PlayerProfile
                {
                    Id = session.User.Id,
                    Username = username,
                    DeviceId = SystemInfo.deviceUniqueIdentifier,
                    Gold = 0,
                    RankPoints = 1000 // Điểm khởi đầu
                };

                await _supabase.From<PlayerProfile>().Insert(newProfile);

                // --- ĐOẠN CODE LƯU VÉ VÀO Ổ CỨNG SAU KHI ĐĂNG KÝ ---
                PlayerPrefs.SetString("supa_access", session.AccessToken);
                PlayerPrefs.SetString("supa_refresh", session.RefreshToken);
                PlayerPrefs.Save();
                // ----------------------------------------------------

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi đăng ký: {e.Message}");
                return false;
            }
        }

        public async Task<bool> Login(string email, string password)
        {
            try
            {
                var session = await _supabase.Auth.SignIn(email, password);
                if (session == null || session.User == null) return false;

                bool isValid = await VerifyAndLoadProfile(session.User.Id);
                if (!isValid)
                {
                    _supabase.Auth.SignOut(); // Sai máy thì ép đăng xuất
                    return false;
                }

                // --- ĐOẠN CODE LƯU VÉ VÀO Ổ CỨNG SAU KHI ĐĂNG NHẬP ---
                PlayerPrefs.SetString("supa_access", session.AccessToken);
                PlayerPrefs.SetString("supa_refresh", session.RefreshToken);
                PlayerPrefs.Save();
                // ----------------------------------------------------

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi đăng nhập: {e.Message}");
                return false;
            }
        }

        private async Task<bool> VerifyAndLoadProfile(string userId)
        {
            var response = await _supabase.From<PlayerProfile>().Where(x => x.Id == userId).Single();
            if (response == null) return false;

            string currentDevice = SystemInfo.deviceUniqueIdentifier;

            if (string.IsNullOrEmpty(response.DeviceId))
            {
                response.DeviceId = currentDevice;
                await _supabase.From<PlayerProfile>().Where(x => x.Id == userId).Update(response);
            }
            else if (response.DeviceId != currentDevice)
            {
                Debug.LogError("Tài khoản này đã được liên kết với một thiết bị khác!");
                return false;
            }

            CurrentProfile = response;
            return true;
        }

        public async Task<bool> ChangeUsername(string newName)
        {
            if (!IsLoggedIn) return false;
            
            CurrentProfile.Username = newName;
            await _supabase.From<PlayerProfile>().Where(x => x.Id == CurrentProfile.Id).Update(CurrentProfile);
            
            Debug.Log("Đã đồng bộ tên mới lên Supabase: " + newName);
            return true;
        }

        // HÀM ĐĂNG XUẤT
        public async void SignOut()
        {
            try
            {
                // Gọi API lên Supabase để hủy phiên đăng nhập
                await _supabase.Auth.SignOut();
                
                // Xóa dữ liệu người chơi trong biến và trong bộ nhớ tạm của Unity
                CurrentProfile = null;
                PlayerPrefs.DeleteKey("Photon.Menu.Username");

                // --- ĐOẠN CODE XÓA VÉ KHI ĐĂNG XUẤT ---
                PlayerPrefs.DeleteKey("supa_access");
                PlayerPrefs.DeleteKey("supa_refresh");
                PlayerPrefs.Save();
                // --------------------------------------
                
                Debug.Log("Đã đăng xuất thành công!");
            }
            catch (Exception e)
            {
                Debug.LogError("Lỗi khi đăng xuất: " + e.Message);
            }
        }

        
    }
}