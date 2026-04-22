using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace SimpleFPS
{
    // Script này chuyên dùng để bắt sự kiện click vào các thẻ <link> của TextMeshPro
    public class ChatLinkHandler : MonoBehaviour, IPointerClickHandler
    {
        private TextMeshProUGUI _textMeshPro;

        private void Awake()
        {
            _textMeshPro = GetComponent<TextMeshProUGUI>();
        }

        public async void OnPointerClick(PointerEventData eventData)
        {
            // Kiểm tra xem mũi tên chuột có đâm trúng đoạn <link> nào trong đoạn chat không
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(_textMeshPro, Input.mousePosition, eventData.pressEventCamera);
            
            if (linkIndex != -1)
            {
                // Lấy cái mã phòng được giấu bên trong Link ra
                TMP_LinkInfo linkInfo = _textMeshPro.textInfo.linkInfo[linkIndex];
                string roomCode = linkInfo.GetLinkID();
                
                Debug.Log($"[CHAT] Đã bấm trúng mã phòng: {roomCode}. Đang tiến hành ghép trận...");
                
                // 1. TÌM MÀN HÌNH MENU CHÍNH (Thay vì Controller)
                var menuMain = FindObjectOfType<Fusion.Menu.FusionMenuUIMain>(true);
                
                if (menuMain != null && menuMain.Connection != null)
                {
                    var connectArgs = new Fusion.Menu.FusionMenuConnectArgs {
                        Session = roomCode,
                        Creating = false, // Mình là khách
                        Region = ""       // Tự tìm Region
                    };
                    
                    // 2. Tìm bảng Chat để ẩn nó đi cho gọn
                    var chatUI = FindObjectOfType<GlobalChatUI>(true);
                    if (chatUI != null) chatUI.ChatPanel.SetActive(false);

                    // 3. Bật màn hình Loading thông qua Controller của menuMain
                    menuMain.Controller.Show<Fusion.Menu.FusionMenuUILoading>();

                    // 4. VÀO TRẬN BẰNG KÊNH CỦA MÀN HÌNH MENU!
                    var result = await menuMain.Connection.ConnectAsync(connectArgs);
                    
                    // 5. Xử lý kết quả (Nếu lỗi, báo popup. Nếu thành công, tự vào game)
                    await Fusion.Menu.FusionMenuUIMain.HandleConnectionResult(result, menuMain.Controller);
                }
                else
                {
                    Debug.LogError("[CHAT] Lỗi: Không tìm thấy Menu Chính hoặc Connection chưa sẵn sàng!");
                }
            }
        }
    }
}