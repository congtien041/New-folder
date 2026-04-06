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

        // Auto-login state tracking for better UX
        public enum LoginState
        {
            Initializing,      // Supabase client is initializing
            CheckingSession,   // Currently checking for saved session
            LoggedIn,         // Successfully auto-logged in
            NotLoggedIn       // No saved session or session expired
        }

        public LoginState CurrentLoginState { get; private set; } = LoginState.Initializing;

        /*
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
        */

        private async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            await InitializeSupabase();
        }

        private async Task InitializeSupabase()
        {
            try
            {
                CurrentLoginState = LoginState.Initializing;

                // Khởi tạo Supabase
                var options = new SupabaseOptions { AutoConnectRealtime = true, AutoRefreshToken = true };
                _supabase = new Client(SupabaseUrl, SupabaseAnonKey, options);
                await _supabase.InitializeAsync();

                Debug.Log("✓ Supabase initialized successfully");

                // Thử tự động đăng nhập khi mở game
                await AutoLoginAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize Supabase: {e.Message}");
                CurrentLoginState = LoginState.NotLoggedIn;
            }
        }

        public async Task AutoLoginAsync()
        {
            CurrentLoginState = LoginState.CheckingSession;

            // --- ĐOẠN CODE TÌM VÉ TRONG Ổ CỨNG ---
            string accessToken = PlayerPrefs.GetString("supa_access", "");
            string refreshToken = PlayerPrefs.GetString("supa_refresh", "");

            // Nếu không có token đã lưu, bỏ qua auto-login
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                CurrentLoginState = LoginState.NotLoggedIn;
                Debug.Log("No saved session found. User needs to log in.");
                return;
            }

            try
            {
                Debug.Log("Attempting auto-login with saved session...");

                // Dùng vé để khôi phục phiên đăng nhập
                var session = await _supabase.Auth.SetSession(accessToken, refreshToken);

                if (session != null && session.User != null)
                {
                    bool isValidDevice = await VerifyAndLoadProfile(session.User.Id);
                    if (isValidDevice)
                    {
                        CurrentLoginState = LoginState.LoggedIn;
                        Debug.Log($"✓ Tự động đăng nhập thành công! Chào {CurrentProfile.Username}, Vàng: {CurrentProfile.Gold}");
                        return;
                    }
                }

                // If we reach here, verification failed
                CurrentLoginState = LoginState.NotLoggedIn;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Auto-login failed: {ex.Message}. User needs to log in manually.");

                // Vé hết hạn thì xé vé cũ đi
                ClearSavedTokens();
                CurrentLoginState = LoginState.NotLoggedIn;
            }
        }

        public async Task<bool> Register(string email, string password, string username)
        {
            if (_supabase == null)
            {
                Debug.LogError("Supabase client not initialized!");
                return false;
            }

            try
            {
                Debug.Log($"Attempting to register user: {username} ({email})");

                var session = await _supabase.Auth.SignUp(email, password);
                if (session == null || session.User == null)
                {
                    Debug.LogError("Registration failed: No session returned");
                    return false;
                }

                var newProfile = new PlayerProfile
                {
                    Id = session.User.Id,
                    Username = username,
                    DeviceId = SystemInfo.deviceUniqueIdentifier,
                    Gold = 0,
                    RankPoints = 1000, // Điểm khởi đầu
                    CurrentCharacter = "Char_Adam", // Mặc định trang bị Adam
                    UnlockedCharacters = "Char_Adam" // Tủ đồ lúc đầu chỉ có Adam
                };

                await _supabase.From<PlayerProfile>().Insert(newProfile);

                // Load profile into memory
                CurrentProfile = newProfile;
                CurrentLoginState = LoginState.LoggedIn;

                // --- ĐOẠN CODE LƯU VÉ VÀO Ổ CỨNG SAU KHI ĐĂNG KÝ ---
                SaveTokens(session.AccessToken, session.RefreshToken);

                Debug.Log($"✓ Registration successful for {username}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Registration error: {e.Message}");
                return false;
            }
        }

        public async Task<bool> Login(string email, string password)
        {
            if (_supabase == null)
            {
                Debug.LogError("Supabase client not initialized!");
                return false;
            }

            try
            {
                Debug.Log($"Attempting to login: {email}");

                var session = await _supabase.Auth.SignIn(email, password);
                if (session == null || session.User == null)
                {
                    Debug.LogWarning("Login failed: Invalid credentials");
                    return false;
                }

                bool isValid = await VerifyAndLoadProfile(session.User.Id);
                if (!isValid)
                {
                    await _supabase.Auth.SignOut(); // Sai máy thì ép đăng xuất
                    CurrentLoginState = LoginState.NotLoggedIn;
                    return false;
                }

                // --- ĐOẠN CODE LƯU VÉ VÀO Ổ CỨNG SAU KHI ĐĂNG NHẬP ---
                SaveTokens(session.AccessToken, session.RefreshToken);
                CurrentLoginState = LoginState.LoggedIn;

                Debug.Log($"✓ Login successful for {CurrentProfile.Username}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Login error: {e.Message}");
                CurrentLoginState = LoginState.NotLoggedIn;
                return false;
            }
        }

        private async Task<bool> VerifyAndLoadProfile(string userId)
        {
            try
            {
                var response = await _supabase.From<PlayerProfile>().Where(x => x.Id == userId).Single();
                if (response == null)
                {
                    Debug.LogError("Profile not found in database");
                    return false;
                }

                string currentDevice = SystemInfo.deviceUniqueIdentifier;

                if (string.IsNullOrEmpty(response.DeviceId))
                {
                    // First time login from this device - bind it
                    response.DeviceId = currentDevice;
                    await _supabase.From<PlayerProfile>().Update(response);
                    Debug.Log("Device bound to account");
                }
                else if (response.DeviceId != currentDevice)
                {
                    Debug.LogError("Tài khoản này đã được liên kết với một thiết bị khác!");
                    return false;
                }

                CurrentProfile = response;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to verify and load profile: {e.Message}");
                return false;
            }
        }

        public async Task<bool> ChangeUsername(string newName)
        {
            if (!IsLoggedIn)
            {
                Debug.LogWarning("Cannot change username: Not logged in");
                return false;
            }

            if (string.IsNullOrEmpty(newName))
            {
                Debug.LogWarning("Cannot change username: Empty name");
                return false;
            }

            try
            {
                CurrentProfile.Username = newName;
                await _supabase.From<PlayerProfile>().Update(CurrentProfile);

                Debug.Log($"✓ Username changed to: {newName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to change username: {e.Message}");
                return false;
            }
        }

        // HÀM ĐĂNG XUẤT
        public async void SignOut()
        {
            try
            {
                if (_supabase != null)
                {
                    await _supabase.Auth.SignOut();
                }

                // Xóa dữ liệu người chơi trong biến
                CurrentProfile = null;
                CurrentLoginState = LoginState.NotLoggedIn;

                // Xóa PlayerPrefs
                PlayerPrefs.DeleteKey("Photon.Menu.Username");
                PlayerPrefs.DeleteKey("Photon.Menu.Character");
                ClearSavedTokens();

                Debug.Log("✓ Logged out successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during logout: {e.Message}");
            }
        }

        // --- 1. HÀM TÍNH TOÁN VÀ LƯU KẾT QUẢ KHI HẾT TRẬN ---
        // public async Task UpdateMatchResult(bool isWin, int kills, int deaths)
        // {
        //     if (!IsLoggedIn)
        //     {
        //         Debug.LogWarning("Cannot update match result: Not logged in");
        //         return;
        //     }

        //     try
        //     {
        //         // Tính Vàng,
        //         int goldEarned = kills * 10;

        //         // Tính Rank
        //         int matchPoint = isWin ? 20 : -30; // Thắng được 20, Thua bị trừ 30
        //         int rankEarned = matchPoint + (kills * 10) - (deaths * 2);

        //         // Cộng vào Profile
        //         CurrentProfile.Gold += goldEarned;
        //         CurrentProfile.RankPoints += rankEarned;

        //         // Không để điểm Rank nhất cũng là 0 điểm)
        //         if (CurrentProfile.RankPoints < 0) CurrentProfile.RankPoints = 0;

        //         // Đẩy lên Database 
        //         await _supabase.From<PlayerProfile>().Update(CurrentProfile);

        //         Debug.Log($"Match result updated! Gold: +{goldEarned}, Rank: {(rankEarned >= 0 ? "+" : "")}{rankEarned}, Total Rank: {CurrentProfile.RankPoints}");
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogError($"Failed to update match result: {e.Message}");
        //     }
        // }
        // --- CẬP NHẬT HÀM LƯU KẾT QUẢ (THÊM TRƯỜNG HỢP QUÍT TRẬN) ---
        public async Task UpdateMatchResult(bool isWin, int kills, int deaths, float playTime, bool isQuit = false)
        {
            if (!IsLoggedIn) return;

            try {
                int goldEarned = isQuit ? 0 : (kills * 10 + (isWin ? 50 : 0));
                int rankChange = isQuit ? -20 : (isWin ? 25 : -15) + (kills * 2);

                CurrentProfile.Gold += goldEarned;
                CurrentProfile.RankPoints = Mathf.Max(0, CurrentProfile.RankPoints + rankChange);

                // 1. Cập nhật Profile
                await _supabase.From<PlayerProfile>().Update(CurrentProfile);

                // 2. CHỮA LỖI Ở ĐÂY: Sử dụng Class Model thay vì object ẩn danh
                var history = new MatchHistoryModel {
                    UserId = CurrentProfile.Id,
                    Kills = kills,
                    Deaths = deaths,
                    PlayTimeSeconds = playTime,
                    Result = isQuit ? "Quit" : (isWin ? "Win" : "Loss")
                };
                
                // Gọi Insert theo kiểu định dạng chuẩn của C#
                await _supabase.From<MatchHistoryModel>().Insert(history);

                Debug.Log($"Kết quả: {(isQuit ? "Bỏ cuộc" : "Xong trận")}. Rank: {rankChange}, Vàng: {goldEarned}");
            } catch (Exception e) {
                Debug.LogError($"Lỗi lưu kết quả: {e.Message}");
            }
        }

        // --- 2. HÀM LẤY DỮ LIỆU BẢNG XẾP HẠNG (TOP 10) ---
        public async Task<System.Collections.Generic.List<PlayerProfile>> GetLeaderboard()
        {
            try
            {
                var response = await _supabase.From<PlayerProfile>()
                    .Select("*")
                    .Order("rank_points", Postgrest.Constants.Ordering.Descending)
                    .Limit(10)
                    .Get();

                Debug.Log($"Loaded {response.Models.Count} leaderboard entries");
                return response.Models;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load leaderboard: {e.Message}");
                return new System.Collections.Generic.List<PlayerProfile>();
            }
        }


        // --- HÀM MUA NHÂN VẬT MỚI BẰNG VÀNG ---
        public async Task<bool> UnlockCharacter(string charId, int cost)
        {
            if (!IsLoggedIn) return false;

            // Kiểm tra xem đã sở hữu chưa (tránh mua trùng)
            if (CurrentProfile.UnlockedCharacters.Contains(charId))
            {
                Debug.Log("Bạn đã sở hữu nhân vật này rồi!");
                return true;
            }

            // Kiểm tra số dư Vàng
            if (CurrentProfile.Gold < cost)
            {
                Debug.LogWarning("Không đủ Vàng để mua nhân vật!");
                return false;
            }

            // Trừ tiền và ném nhân vật mới vào tủ đồ
            CurrentProfile.Gold -= cost;
            CurrentProfile.UnlockedCharacters += "," + charId; // VD: "Char_Adam,Char_Kelly"

            // Đẩy dữ liệu mới lên Server
            // await _supabase.From<PlayerProfile>().Where(x => x.Id == CurrentProfile.Id).Update(CurrentProfile);
            // Xóa dòng cũ:
            // await _supabase.From<PlayerProfile>().Where(x => x.Id == CurrentProfile.Id).Update(CurrentProfile);

            // Sửa thành:
            await _supabase.From<PlayerProfile>().Update(CurrentProfile);
            Debug.Log($"Mở khóa {charId} thành công! Vàng còn lại: {CurrentProfile.Gold}");

            return true;
        }

        // --- HÀM CHỌN NHÂN VẬT ĐỂ MANG VÀO TRẬN ---
        public async Task<bool> EquipCharacter(string charId)
        {
            if (!IsLoggedIn) return false;

            // Chặn việc hack/chọn nhân vật chưa mở khóa
            if (!CurrentProfile.UnlockedCharacters.Contains(charId))
            {
                Debug.LogError("Lỗi: Bạn chưa mở khóa nhân vật này!");
                return false;
            }

            // Đổi nhân vật hiện tại
            CurrentProfile.CurrentCharacter = charId;

            // Xóa dòng cũ:
            // await _supabase.From<PlayerProfile>().Where(x => x.Id == CurrentProfile.Id).Update(CurrentProfile);

            // Sửa thành:
            await _supabase.From<PlayerProfile>().Update(CurrentProfile);

            // Lưu vào ổ cứng để lát nữa Fusion đọc và Spawn đúng Prefab 3D
            PlayerPrefs.SetString("Photon.Menu.Character", charId);
            PlayerPrefs.Save();

            Debug.Log("Đã trang bị nhân vật: " + charId);
            return true;
        }

        // --- HÀM QUÊN MẬT KHẨU (GỬI LINK RESET VỀ EMAIL) ---
        public async Task<bool> ResetPassword(string email)
        {
            try
            {
                // Gọi lệnh gửi email khôi phục của Supabase
                await _supabase.Auth.ResetPasswordForEmail(email);
                Debug.Log("Đã gửi email khôi phục mật khẩu tới: " + email);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi gửi email reset pass: {e.Message}");
                return false;
            }
        }

        // --- HÀM XÁC NHẬN OTP VÀ ĐỔI MẬT KHẨU TRỰC TIẾP TRONG GAME ---
        public async Task<bool> VerifyOtpAndChangePassword(string email, string otp, string newPassword)
        {
            try
            {
                // 1. Gửi OTP lên để xác thực (kèm theo mã ẩn PKCE của Unity)
                var session = await _supabase.Auth.VerifyOTP(email, otp, Supabase.Gotrue.Constants.EmailOtpType.Recovery);

                if (session != null && session.User != null)
                {
                    // 2. OTP chuẩn -> Cập nhật mật khẩu mới luôn
                    var attrs = new Supabase.Gotrue.UserAttributes { Password = newPassword };
                    await _supabase.Auth.Update(attrs);

                    // Xóa vé cũ để người chơi đăng nhập lại bằng pass mới
                    PlayerPrefs.DeleteKey("supa_access");
                    PlayerPrefs.DeleteKey("supa_refresh");
                    PlayerPrefs.Save();

                    Debug.Log("Đổi pass bằng OTP thành công!");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("Lỗi xác nhận OTP: " + e.Message);
                return false;
            }
        }

        #region Helper Methods

        private void SaveTokens(string accessToken, string refreshToken)
        {
            PlayerPrefs.SetString("supa_access", accessToken);
            PlayerPrefs.SetString("supa_refresh", refreshToken);
            PlayerPrefs.Save();
        }

        private void ClearSavedTokens()
        {
            PlayerPrefs.DeleteKey("supa_access");
            PlayerPrefs.DeleteKey("supa_refresh");
            PlayerPrefs.Save();
        }

        #endregion

        #region Debug Tools

        /// <summary>
        /// Context menu method for debugging - Clears all saved authentication data
        /// Right-click the SupabaseManager component in Inspector to use this
        /// </summary>
        [ContextMenu("Debug: Clear Saved Account Data")]
        private void ClearSavedAccountData()
        {
            ClearSavedTokens();
            PlayerPrefs.DeleteKey("Photon.Menu.Username");
            PlayerPrefs.DeleteKey("Photon.Menu.Character");
            PlayerPrefs.Save();

            CurrentProfile = null;
            CurrentLoginState = LoginState.NotLoggedIn;

            Debug.LogWarning("🗑️ DEBUG: All saved account data cleared!");
            Debug.Log("Tokens cleared: supa_access, supa_refresh");
            Debug.Log("User data cleared: Photon.Menu.Username, Photon.Menu.Character");
            Debug.Log("Current state: NotLoggedIn");
        }

        [ContextMenu("Debug: Show Current State")]
        private void ShowCurrentState()
        {
            Debug.Log("=== SupabaseManager State ===");
            Debug.Log($"Login State: {CurrentLoginState}");
            Debug.Log($"Is Logged In: {IsLoggedIn}");
            if (CurrentProfile != null)
            {
                Debug.Log($"Username: {CurrentProfile.Username}");
                Debug.Log($"Gold: {CurrentProfile.Gold}");
                Debug.Log($"Rank Points: {CurrentProfile.RankPoints}");
                Debug.Log($"Current Character: {CurrentProfile.CurrentCharacter}");
                Debug.Log($"Unlocked Characters: {CurrentProfile.UnlockedCharacters}");
            }
            else
            {
                Debug.Log("Profile: NULL");
            }
            Debug.Log("===========================");
        }

        #endregion
    }

    
}