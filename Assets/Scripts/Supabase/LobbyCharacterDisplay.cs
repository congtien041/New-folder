using UnityEngine;

namespace SimpleFPS
{
    public class LobbyCharacterDisplay : MonoBehaviour
    {
        [Header("Tủ đồ trưng bày ngoài sảnh")]
        public GameObject[] LobbyModels; // Kéo thả các TƯỢNG ở Menu vào đây

        private void OnEnable()
        {
            // Mỗi khi bật Menu lên, tự động cập nhật tượng
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            // Đọc tên nhân vật đang trang bị (Mặc định là Char_Adam nếu chưa có)
            string equippedChar = PlayerPrefs.GetString("Photon.Menu.Character", "Char_Adam");

            // Bật/tắt tượng
            foreach (var model in LobbyModels)
            {
                if (model != null)
                {
                    // Tên tượng phải đặt khớp 100% với tên nhân vật (vd: Char_Adam)
                    model.SetActive(model.name == equippedChar);
                }
            }
        }
    }
}