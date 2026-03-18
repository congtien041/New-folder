using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace SimpleFPS
{
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject LeaderboardPanel; // Bảng chứa Leaderboard (Bật/Tắt)
        public Transform ContentTransform;  // Nơi chứa danh sách người chơi (Thường là Content của ScrollView)
        public GameObject RowPrefab;        // Prefab của 1 dòng hiển thị (1 người chơi)
        
        // Gắn hàm này vào một nút "Bảng Xếp Hạng" trên Menu
        public async void OpenLeaderboard()
        {
            LeaderboardPanel.SetActive(true);
            
            // Xóa sạch các dòng cũ
            foreach (Transform child in ContentTransform) {
                Destroy(child.gameObject);
            }

            // Tải dữ liệu Top 10 từ Supabase
            List<PlayerProfile> topPlayers = await SupabaseManager.Instance.GetLeaderboard();
            
            int rank = 1;
            foreach (var player in topPlayers)
            {
                // Tạo ra một dòng mới
                GameObject row = Instantiate(RowPrefab, ContentTransform);
                TextMeshProUGUI textComp = row.GetComponentInChildren<TextMeshProUGUI>();
                
                // Hiển thị nội dung
                textComp.text = $"#{rank} | {player.Username} | Rank: {player.RankPoints} | Vàng: {player.Gold}";
                rank++;
            }
        }

        // Gắn hàm này vào nút X (Đóng) trên bảng xếp hạng
        public void CloseLeaderboard()
        {
            LeaderboardPanel.SetActive(false);
        }
    }
}