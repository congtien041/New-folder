using UnityEngine;
using Fusion.Menu;

namespace SimpleFPS
{
    public class ShareRoomButton : MonoBehaviour
    {
        // Hàm này sẽ gắn thẳng vào nút "Mời Thế Giới" ở trong màn hình Phòng
        public async void ShareRoom()
        {
            // Tự động tìm màn hình Phòng của Fusion đang bật để lấy Mã Code
            var menuGameplay = FindObjectOfType<FusionMenuUIGameplay>(true);
            
            if (menuGameplay != null && menuGameplay.Connection != null)
            {
                string code = menuGameplay.Connection.SessionName; 
                
                if (!string.IsNullOrEmpty(code))
                {
                    // Tạo một tin nhắn chứa thẻ <link> ẩn mã phòng bên trong
                    string msg = $"<color=#00ff88>Đã tạo phòng Giao lưu! <link=\"{code}\"><u><b>[BẤM VÀO ĐÂY ĐỂ THAM GIA: {code}]</b></u></link></color>";
                    
                    // Gọi THẲNG SupabaseManager (Vì nó sống ở mọi Scene)
                    if (SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
                    {
                        await SupabaseManager.Instance.SendGlobalChat(msg);
                        Debug.Log("Đã gửi lời mời lên Kênh Thế Giới!");
                    }
                }
                else
                {
                    Debug.LogWarning("Chưa có mã phòng để chia sẻ!");
                }
            }
            else 
            {
                Debug.LogError("Không tìm thấy bảng FusionMenuUIGameplay để lấy code!");
            }
        }
    }
}