using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.EventSystems;


namespace SimpleFPS
{
    public class GlobalChatUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Elements")]
        public GameObject ChatPanel;           // Cục Panel chứa toàn bộ giao diện Chat
        public TMP_InputField ChatInput;       // Ô nhập tin nhắn
        public TextMeshProUGUI ChatHistoryText;// Chữ hiển thị lịch sử chat
        public ScrollRect ScrollRect;          // Khu vực cuộn tin nhắn

        private bool _isChatOpen = false;
        private int _lastMessageId = 0;

        private void Start()
        {
            ChatPanel.SetActive(false);
            
            // Tự động lắng nghe sự kiện bấm phím Enter
            ChatInput.onSubmit.AddListener(OnChatSubmit);
        }

        // Gắn hàm này vào Nút biểu tượng Chat ngoài sảnh
        public void ToggleChat()
        {
            _isChatOpen = !_isChatOpen;
            ChatPanel.SetActive(_isChatOpen);

            if (_isChatOpen)
            {
                if (_lastMessageId == 0) ChatHistoryText.text = "<i>Đang kết nối Kênh Thế Giới...</i>\n";
                StartCoroutine(PollChatMessages());
                ChatInput.ActivateInputField(); // Tự động trỏ chuột vào ô nhập
            }
            else
            {
                StopAllCoroutines(); // Tắt chat thì ngừng tải dữ liệu cho nhẹ máy
            }
        }

        // Hàm xử lý khi người chơi bấm Enter (Đã được bọc lỗi kẹt Focus)
        private void OnChatSubmit(string text)
        {
            // Kiểm tra xem có chữ không (chống spam khoảng trắng)
            if (!string.IsNullOrWhiteSpace(text) && SupabaseManager.Instance != null)
            {
                // Gửi tin nhắn lên mạng
                _ = SupabaseManager.Instance.SendGlobalChat(text);
                
                // Dùng Coroutine để làm sạch và giữ trỏ chuột mượt mà
                StartCoroutine(ResetChatInput());
            }
            else
            {
                // Nếu người dùng bấm Enter mà không gõ gì -> Tắt trỏ chuột
                ChatInput.DeactivateInputField();
            }
        }

        // Ép Unity đợi 1 khung hình (Frame) rồi mới trỏ chuột lại để không bị lỗi
        private IEnumerator ResetChatInput()
        {
            yield return new WaitForEndOfFrame();
            
            ChatInput.text = ""; // Xóa chữ cũ
            ChatInput.ActivateInputField(); // Nháy chuột lại vào ô để chat tiếp
        }

        // Vòng lặp tự động quét tin nhắn mới mỗi 1.5 giây
        private IEnumerator PollChatMessages()
        {
            while (_isChatOpen)
            {
                Task<List<GlobalChatModel>> task = SupabaseManager.Instance.GetNewGlobalChats(_lastMessageId);
                yield return new WaitUntil(() => task.IsCompleted);

                if (task.Result != null && task.Result.Count > 0)
                {
                    if (_lastMessageId == 0) ChatHistoryText.text = ""; // Xóa chữ "Đang kết nối..."

                    foreach (var msg in task.Result)
                    {
                        // Lấy màu ngẫu nhiên nhưng cố định theo tên
                        string colorHex = GetColorFromName(msg.SenderName);
                        
                        // Ghép chuỗi: [Tên màu]: Nội dung
                        ChatHistoryText.text += $"<color={colorHex}><b>[{msg.SenderName}]</b></color>: {msg.Message}\n";
                        _lastMessageId = msg.Id; // Cập nhật ID tin nhắn mới nhất
                    }

                    // Ép ScrollView cuộn xuống dòng cuối cùng
                    Canvas.ForceUpdateCanvases();
                    ScrollRect.verticalNormalizedPosition = 0f;
                }

                yield return new WaitForSeconds(1.5f);
            }
        }

        // Thuật toán gán 1 màu đẹp mắt cố định cho từng người chơi
        private string GetColorFromName(string name)
        {
            int hash = 0;
            foreach (char c in name) hash = c + (hash << 5) - hash;
            
            // Ép hệ thống random chạy theo mã Hash của tên
            Random.InitState(hash);
            float hue = Random.Range(0f, 1f);
            
            // Saturation 70%, Brightness 100% để ra màu sáng đẹp, dễ đọc trên nền đen
            Color color = Color.HSVToRGB(hue, 0.7f, 1f); 
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        // ==========================================
        // PHÉP THUẬT CLICK VÀO LINK PHÒNG ĐỂ VÀO GAME
        // ==========================================
        public void OnPointerClick(PointerEventData eventData)
        {
            // Kiểm tra xem chuột có đang click trúng một đoạn <link> nào trong khung chat không
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(ChatHistoryText, Input.mousePosition, null);
            
            if (linkIndex != -1) // Nếu có click trúng Link
            {
                TMP_LinkInfo linkInfo = ChatHistoryText.textInfo.linkInfo[linkIndex];
                string roomCode = linkInfo.GetLinkID(); // Lấy mã phòng ẩn trong link
                
                Debug.Log("Đang tham gia phòng: " + roomCode);
                JoinRoomFromChat(roomCode);
            }
        }

        private void JoinRoomFromChat(string roomCode)
        {
            // Tìm hệ thống Menu của Fusion và ép nó vào phòng
            var connection = FindFirstObjectByType<MenuConnectionBehaviour>();
            if (connection != null)
            {
                var connectArgs = new Fusion.Menu.FusionMenuConnectArgs {
                    Session = roomCode,
                    Creating = false, // Là người tham gia, không phải chủ phòng
                    Region = ""       // Tự động tìm Region tốt nhất
                };
                
                ChatPanel.SetActive(false); // Tắt chat đi
                connection.ConnectAsync(connectArgs); // VÀO PHÒNG!
            }
        }

        // ==========================================
        // GỬI LỜI MỜI VÀO PHÒNG LÊN KÊNH THẾ GIỚI
        // ==========================================
        public async void ShareRoomToGlobalChat() 
        {
            // Tự động tìm màn hình Phòng của Fusion đang bật để lấy Mã Code
            var menuGameplay = FindObjectOfType<Fusion.Menu.FusionMenuUIGameplay>(true);
            
            if (menuGameplay != null && menuGameplay.Connection != null)
            {
                string code = menuGameplay.Connection.SessionName; 
                
                if (!string.IsNullOrEmpty(code))
                {
                    // Tạo một tin nhắn chứa thẻ <link> ẩn mã phòng bên trong
                    string msg = $"<color=#00ff88>Đã tạo phòng Giao lưu! <link=\"{code}\"><u><b>[BẤM VÀO ĐÂY ĐỂ THAM GIA: {code}]</b></u></link></color>";
                    
                    // Gửi lên Kênh Chat
                    if (SupabaseManager.Instance != null)
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
        }
    }
}