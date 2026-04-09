using UnityEngine;

namespace SimpleFPS
{
    public class LobbyCharacterDisplay : MonoBehaviour
    {
        [Header("Tủ đồ trưng bày ngoài sảnh")]
        public GameObject[] LobbyModels; // Kéo thả các TƯỢNG ở Menu vào đây

        private void OnEnable()
        {
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            string equippedChar = PlayerPrefs.GetString("Photon.Menu.Character", "Char_Adam");
            Debug.Log($"[LOBBY] Bắt đầu quét Sảnh... Tên nhân vật đang tìm: '{equippedChar}'");

            bool foundTarget = false;

            foreach (var model in LobbyModels)
            {
                if (model != null)
                {
                    // Kiểm tra xem tên vật thể có khớp 100% với tên đã lưu không
                    bool isMatch = (model.name == equippedChar);
                    model.SetActive(isMatch);
                    
                    if (isMatch)
                    {
                        foundTarget = true;
                        Debug.Log($"[LOBBY] ---> TÌM THẤY VÀ ĐÃ BẬT TƯỢNG: {model.name}");
                    }
                    else
                    {
                        Debug.Log($"[LOBBY] Đã tắt tượng: {model.name}");
                    }
                }
            }

            // Nếu quét hết cả mảng mà không có ai trùng tên
            if (!foundTarget)
            {
                Debug.LogWarning($"[LOBBY CẢNH BÁO] Ôi hỏng! Không có bức tượng nào mang tên chính xác là '{equippedChar}' cả. Bạn ra Inspector kiểm tra lại xem có bị dư dấu cách (Space) không nhé!");
            }
        }
    }
}