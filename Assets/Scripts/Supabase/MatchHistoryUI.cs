using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace SimpleFPS
{
    public class MatchHistoryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject HistoryPanel;    // Bảng chứa Lịch sử (Bật/Tắt)
        public Transform ContentTransform; // Nơi chứa các dòng (ScrollView Content)
        public GameObject RowPrefab;       // Prefab của 1 dòng lịch sử

        // Gắn hàm này vào nút "Lịch Sử Đấu" trên Menu
        public async void OpenHistory()
        {
            HistoryPanel.SetActive(true);
            
            // Xóa sạch các dòng cũ trước khi tải mới
            foreach (Transform child in ContentTransform) {
                Destroy(child.gameObject);
            }

            // Tải dữ liệu từ SupabaseManager
            List<MatchHistoryModel> history = await SupabaseManager.Instance.GetMyMatchHistory();
            
            foreach (var match in history)
            {
                GameObject row = Instantiate(RowPrefab, ContentTransform);
                TextMeshProUGUI textComp = row.GetComponentInChildren<TextMeshProUGUI>();
                
                // Hiển thị: Kết quả | Kills/Deaths | Thời gian chơi
                string resultColor = match.Result == "Win" ? "<color=#00ff88>THẮNG</color>" : 
                                   (match.Result == "Quit" ? "<color=#ff4d4d>THOÁT</color>" : "<color=#aaaaaa>THUA</color>");
                
                int minutes = (int)match.PlayTimeSeconds / 60;
                int seconds = (int)match.PlayTimeSeconds % 60;

                textComp.text = $"{resultColor} | K/D: {match.Kills}/{match.Deaths} | Time: {minutes:00}:{seconds:00}";
            }
        }

        public void CloseHistory()
        {
            HistoryPanel.SetActive(false);
        }
    }
}