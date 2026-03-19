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
            if (success) Debug.Log("Đã đổi sang Adam!");
        }

        public async void OnEquipKellyClick()
        {
            // Phải bắt lấy kết quả trả về (success) để xem Backend có cho phép không
            bool success = await SupabaseManager.Instance.EquipCharacter("Char_Kelly"); 
            
            if (success) 
            {
                Debug.Log("Trang bị Kelly THÀNH CÔNG! Lần sau vào trận sẽ là Kelly.");
            }
            else
            {
                // Nếu Backend chặn, in ra thông báo để UI biết đường xử lý
                Debug.LogError("Trang bị thất bại! Bạn phải Mua nhân vật này trước.");
            }
        }
    }
}