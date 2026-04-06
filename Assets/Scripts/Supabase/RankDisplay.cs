using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankDisplay : MonoBehaviour {
    public Image RankIcon;
    public TextMeshProUGUI RankPointText;
    public Sprite[] RankSprites; // Bỏ các hình Bronze, Silver, Gold vào đây theo thứ tự

    public void UpdateRank(int points) {
        RankPointText.text = points.ToString();
        // 0-1199: Đồng, 1200-1499: Bạc, 1500+: Vàng
        if (points < 1200) RankIcon.sprite = RankSprites[0];
        else if (points < 1500) RankIcon.sprite = RankSprites[1];
        else RankIcon.sprite = RankSprites[2];
    }
}