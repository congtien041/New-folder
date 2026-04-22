using UnityEngine;

namespace SimpleFPS
{
    public class AutoHideMenu : MonoBehaviour
    {
        private Canvas _menuCanvas;

        private void Start()
        {
            // 1. TÌM CHÍNH XÁC MÀN HÌNH MENU
            var menuMain = FindObjectOfType<Fusion.Menu.FusionMenuUIMain>(true);
            
            if (menuMain != null)
            {
                // Dò ngược lên tìm cái khung Canvas to nhất bọc ngoài cùng
                Canvas canvas = menuMain.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    _menuCanvas = canvas.rootCanvas; // Lấy thằng cha to nhất
                    
                    // TUYỆT CHIÊU: Tắt hiển thị (Tàng hình) thay vì tắt GameObject
                    _menuCanvas.enabled = false;
                    Debug.Log("[GAME] Đã làm tàng hình Menu an toàn!");
                }
            }

            // 2. Tắt Khung Chat Thế Giới
            var chatUI = FindObjectOfType<GlobalChatUI>(true);
            if (chatUI != null && chatUI.ChatPanel != null)
            {
                chatUI.ChatPanel.SetActive(false);
                Debug.Log("[GAME] Đã ẩn Khung Chat!");
            }
        }

        private void OnDestroy()
        {
            // 3. Hiện hình lại Menu khi thoát trận về sảnh
            if (_menuCanvas != null)
            {
                _menuCanvas.enabled = true;
            }
        }
    }
}