using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankDisplay : MonoBehaviour {
    public Image RankIcon;
    public TextMeshProUGUI RankPointText;
    public Sprite[] RankSprites; // 0: Đồng, 1: Bạc, 2: Vàng, 3: Bạch kim, 4: Kim cương, 5: Huyền thoại

    public void UpdateRank(int points) {
        RankPointText.text = points.ToString();
        int rankIndex = GetRankIndex(points);

        if (RankSprites == null || RankSprites.Length <= rankIndex) {
            Debug.LogWarning($"RankSprites is not configured correctly. Expected at least {rankIndex + 1} sprites.");
            return;
        }

        RankIcon.sprite = RankSprites[rankIndex];
    }

    private int GetRankIndex(int points) {
        if (points < 1200) return 0;
        if (points < 1500) return 1;
        if (points < 1700) return 2;
        if (points < 1900) return 3;
        if (points < 2000) return 4;
        return 5;
    }
}