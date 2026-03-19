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
                    RankPoints = 1000, // Điểm khởi đầu
                    CurrentCharacter = "Char_Adam", // Mặc định trang bị Adam
                    UnlockedCharacters = "Char_Adam" // Tủ đồ lúc đầu chỉ có Adam
                    
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
                // await _supabase.From<PlayerProfile>().Where(x => x.Id == userId).Update(response);
                await _supabase.From<PlayerProfile>().Update(response);
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
            // await _supabase.From<PlayerProfile>().Where(x => x.Id == CurrentProfile.Id).Update(CurrentProfile);
            await _supabase.From<PlayerProfile>().Update(CurrentProfile);
             
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

        // --- 1. HÀM TÍNH TOÁN VÀ LƯU KẾT QUẢ KHI HẾT TRẬN ---
        public async Task UpdateMatchResult(bool isWin, int kills, int deaths)
        {
            if (!IsLoggedIn) return;

            // Tính Vàng
            int goldEarned = kills * 10;
            
            // Tính Rank
            int matchPoint = isWin ? 20 : -30; // Thắng được 20, Thua bị trừ 30
            int rankEarned = matchPoint + (kills * 10) - (deaths * 2);

            // Cộng vào Profile
            CurrentProfile.Gold += goldEarned;
            CurrentProfile.RankPoints += rankEarned;

            // Không để điểm Rank bị âm (Noob nhất cũng là 0 điểm)
            if (CurrentProfile.RankPoints < 0) CurrentProfile.RankPoints = 0;

            // Đẩy lên Database
            // await _supabase.From<PlayerProfile>().Where(x => x.Id == CurrentProfile.Id).Update(CurrentProfile);
            await _supabase.From<PlayerProfile>().Update(CurrentProfile);
            
            Debug.Log($"Trận đấu kết thúc! Vàng +{goldEarned}. Điểm Rank thay đổi: {rankEarned}. Tổng Rank: {CurrentProfile.RankPoints}");
        }

        // --- 2. HÀM LẤY DỮ LIỆU BẢNG XẾP HẠNG (TOP 10) ---
        public async Task<System.Collections.Generic.List<PlayerProfile>> GetLeaderboard()
        {
            try
            {
                // Gọi lên DB, sắp xếp cột rank_points từ cao xuống thấp và lấy 10 người đứng đầu
                var response = await _supabase.From<PlayerProfile>()
                    .Select("*")
                    .Order("rank_points", Postgrest.Constants.Ordering.Descending)
                    .Limit(10)
                    .Get();
                    
                return response.Models;
            }
            catch (Exception e)
            {
                Debug.LogError("Lỗi tải Bảng Xếp Hạng: " + e.Message);
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
    }
}