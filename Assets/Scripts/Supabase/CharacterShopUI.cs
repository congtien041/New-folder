using UnityEngine;
using System.Collections.Generic;

namespace SimpleFPS
{
    public class CharacterShopUI : MonoBehaviour
    {
        // BẢNG GIÁ NHÂN VẬT: Sau này muốn thêm n nhân vật, bạn CHỈ CẦN thêm 1 dòng vào đây
        private Dictionary<string, int> CharacterPrices = new Dictionary<string, int>()
        {
            { "Char_Adam", 0 },
            { "Char_Kelly", 99 },
            { "Char_FF", 99 },
            { "Char_Goku", 500 },     // Ví dụ thêm Goku giá 500
            { "Char_Batman", 1000 }   // Ví dụ thêm Batman giá 1000
        };

        // ==========================================
        // PHẦN 1: MUA NHÂN VẬT (Dùng chung cho N nhân vật)
        // ==========================================
        // ==========================================
        // PHẦN 1: MUA NHÂN VẬT (Dùng chung cho N nhân vật)
        // ==========================================
        public async void BuyCharacter(string characterId)
        {
            // Kiểm tra xem nhân vật có trong bảng giá không
            if (!CharacterPrices.ContainsKey(characterId))
            {
                Debug.LogError("Nhân vật " + characterId + " chưa được cài đặt giá!");
                return;
            }

            int price = CharacterPrices[characterId];
            
            // Gọi Supabase mua với đúng tên và giá đó
            bool success = await SupabaseManager.Instance.UnlockCharacter(characterId, price); 
            
            if (success)
            {
                Debug.Log($"Mua {characterId} THÀNH CÔNG! Bây giờ bạn có thể trang bị.");

                // --- BỔ SUNG: ÉP UI CẬP NHẬT LẠI SỐ VÀNG NGAY LẬP TỨC ---
                var authUI = FindObjectOfType<AuthUIManager>();
                if (authUI != null) 
                {
                    authUI.RefreshProfileUI();
                }
            }
            else
            {
                Debug.LogWarning($"Mua {characterId} thất bại (Không đủ vàng hoặc đã có sẵn).");
            }
        }

        // ==========================================
        // PHẦN 2: TRANG BỊ NHÂN VẬT (Dùng chung cho N nhân vật)
        // ==========================================
        
        public async void EquipCharacter(string characterId)
        {
            Debug.Log($"[1] Bấm nút Trang Bị. Đang gửi yêu cầu đổi sang: '{characterId}'...");

            bool success = await SupabaseManager.Instance.EquipCharacter(characterId); 
            
            if (success) 
            {
                Debug.Log("[2] Server Supabase báo thành công! Đang lưu vào PlayerPrefs...");
                
                PlayerPrefs.SetString("Photon.Menu.Character", characterId);
                PlayerPrefs.Save();

                Debug.Log("[3] Đã lưu xong! Tiến hành gọi RefreshLobby()...");
                RefreshLobby(); 
            }
            else
            {
                Debug.LogError($"[LỖI] Server từ chối! (Có thể bạn chưa mua {characterId} hoặc lỗi mạng)");
            }
        }
        // ==========================================
        // HÀM HỖ TRỢ
        // ==========================================
        // ==========================================
        // HÀM HỖ TRỢ
        // ==========================================
        private void RefreshLobby()
        {
            // Tìm TẤT CẢ các script LobbyCharacterDisplay đang có mặt trong màn hình (kể cả đang bị ẩn)
            var allDisplays = FindObjectsOfType<LobbyCharacterDisplay>(true);
            
            bool isUpdated = false;
            foreach (var display in allDisplays)
            {
                // Chỉ update những script có chứa tượng bên trong (né mấy cái script rỗng lỗi)
                if (display.LobbyModels != null && display.LobbyModels.Length > 0)
                {
                    display.UpdateDisplay();
                    isUpdated = true;
                }
            }

            if (!isUpdated)
            {
                Debug.LogError("[SHOP] Không tìm thấy Sảnh nào có chứa tượng để update cả!");
            }
        }
    }
}