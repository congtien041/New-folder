using UnityEngine;

namespace SimpleFPS
{
    public class CharacterShopUI : MonoBehaviour
    {
        // ==========================================
        // PHẦN 1: MUA NHÂN VẬT (UNLOCK)
        // ==========================================
        
        public async void OnBuyKellyClick()
        {
            // Để test nhanh, mình set giá mua Kelly là 0 Vàng. (Vì acc của bạn đang có 0 Vàng)
            // Sau này muốn bán Kelly giá 500 Vàng thì đổi số 0 thành 500 nhé!
            bool success = await SupabaseManager.Instance.UnlockCharacter("Char_Kelly", 99); 
            
            if (success)
            {
                Debug.Log("Mua Kelly THÀNH CÔNG! Bây giờ bạn có thể trang bị.");
            }
            else
            {
                Debug.LogWarning("Mua thất bại (Không đủ vàng hoặc đã có sẵn).");
            }
        }


        // ==========================================
        // PHẦN 2: TRANG BỊ NHÂN VẬT (EQUIP)
        // ==========================================

        public async void OnEquipAdamClick()
        {
            bool success = await SupabaseManager.Instance.EquipCharacter("Char_Adam"); 
            if (success) 
            {
                Debug.Log("Đã đổi sang Adam!");
                RefreshLobby(); // Gọi hàm làm mới tượng
            }
        }

        public async void OnEquipKellyClick()
        {
            bool success = await SupabaseManager.Instance.EquipCharacter("Char_Kelly"); 
            if (success) 
            {
                Debug.Log("Trang bị Kelly THÀNH CÔNG!");
                RefreshLobby(); // Gọi hàm làm mới tượng
            }
        }

        // --- THÊM HÀM NÀY VÀO CUỐI FILE CharacterShopUI.cs ---
        private void RefreshLobby()
        {
            var display = FindObjectOfType<LobbyCharacterDisplay>();
            if (display != null)
            {
                display.UpdateDisplay();
            }
        }
    }
}