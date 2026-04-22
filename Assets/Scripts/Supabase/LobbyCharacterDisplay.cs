using UnityEngine;

namespace SimpleFPS
{
    public class LobbyCharacterDisplay : MonoBehaviour
    {
        [Header("Tủ đồ trưng bày ngoài sảnh")]
        public GameObject[] LobbyModels; 

        private void OnEnable()
        {
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            string equippedChar = "Char_Adam"; // Mặc định

            // CHIẾN THUẬT MỚI:
            // 1. Nếu đã đăng nhập -> Lấy nhân vật CHUẨN từ Profile của Server
            if (SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
            {
                equippedChar = SupabaseManager.Instance.CurrentProfile.CurrentCharacter;
            }
            // 2. Nếu chưa đăng nhập (đang ở màn hình Login) -> Mới dùng đến PlayerPrefs (cache)
            else
            {
                equippedChar = PlayerPrefs.GetString("Photon.Menu.Character", "Char_Adam");
            }

            Debug.Log($"[LOBBY] Đang hiển thị nhân vật cho Profile hiện tại: '{equippedChar}'");

            foreach (var model in LobbyModels)
            {
                if (model != null)
                {
                    model.SetActive(model.name == equippedChar);
                }
            }
        }
    }
}