using UnityEngine;

namespace SimpleFPS
{
    public class SupabaseNameSync : MonoBehaviour
    {
        private string lastUsername;

        private void Start()
        {
            // Ghi nhớ cái tên lúc mới vào game
            lastUsername = PlayerPrefs.GetString("Photon.Menu.Username", "");
        }

        private void Update()
        {
            // Liên tục kiểm tra xem cái tên trong Menu Fusion có bị đổi không
            string currentName = PlayerPrefs.GetString("Photon.Menu.Username", "");

            // Nếu người chơi vừa gõ tên mới khác với tên cũ
            if (currentName != lastUsername && !string.IsNullOrEmpty(currentName))
            {
                lastUsername = currentName; // Cập nhật tên cũ

                // Gọi Supabase đẩy tên mới lên Database
                if (SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
                {
                    _ = SupabaseManager.Instance.ChangeUsername(currentName);
                }
            }
        }
    }
}