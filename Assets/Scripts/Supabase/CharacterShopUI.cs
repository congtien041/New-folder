using UnityEngine;
using System.Collections.Generic;

namespace SimpleFPS
{
    public class CharacterShopUI : MonoBehaviour
    {
        // ==========================================
        // CẤU HÌNH NHÂN VẬT
        // ==========================================
        
        private readonly Dictionary<string, CharacterInfo> characters = new()
        {
            { "Naruto", new CharacterInfo { Name = "Char_Naruto", Price = 1 } },
            { "Kelly", new CharacterInfo { Name = "Char_Kelly", Price = 1 } },
            { "FF", new CharacterInfo { Name = "Char_FF", Price = 1 } },
            { "Goku", new CharacterInfo { Name = "Char_Goku", Price = 1 } },
            { "Omen", new CharacterInfo { Name = "New_Omen", Price = 1 } },
            { "Adam", new CharacterInfo { Name = "Char_Adam", Price = 0 } } // Adam miễn phí
        };

        private struct CharacterInfo
        {
            public string Name;
            public int Price;
        }

        // ==========================================
        // PHẦN 1: MUA NHÂN VẬT (UNLOCK)
        // ==========================================
        
        private async System.Threading.Tasks.Task<bool> BuyCharacter(string key)
        {
            if (!characters.ContainsKey(key)) return false;
            var charInfo = characters[key];
            return await SupabaseManager.Instance.UnlockCharacter(charInfo.Name, charInfo.Price);
        }

        public async void OnBuyChar_Naruto()
        {
            bool success = await BuyCharacter("Naruto");
            if (success)
            {
                Debug.Log("Mua Naruto THÀNH CÔNG! Bây giờ bạn có thể trang bị.");
            }
            else
            {
                Debug.LogWarning("Mua thất bại (Không đủ vàng hoặc đã có sẵn).");
            }
        }

        public async void OnBuyChar_Kelly()
        {
            bool success = await BuyCharacter("Kelly");
            if (success)
            {
                Debug.Log("Mua Kelly THÀNH CÔNG! Bây giờ bạn có thể trang bị.");
            }
            else
            {
                Debug.LogWarning("Mua thất bại (Không đủ vàng hoặc đã có sẵn).");
            }
        }

        public async void OnBuyChar_FF()
        {
            bool success = await BuyCharacter("FF");
            if (success)
            {
                Debug.Log("Mua FF THÀNH CÔNG! Bây giờ bạn có thể trang bị.");
            }
            else
            {
                Debug.LogWarning("Mua thất bại (Không đủ vàng hoặc đã có sẵn).");
            }
        }

        public async void OnBuyChar_Goku()
        {
            bool success = await BuyCharacter("Goku");
            if (success)
            {
                Debug.Log("Mua Goku THÀNH CÔNG! Bây giờ bạn có thể trang bị.");
            }
            else
            {
                Debug.LogWarning("Mua thất bại (Không đủ vàng hoặc đã có sẵn).");
            }
        }

        public async void OnBuyChar_Omen()
        {
            bool success = await BuyCharacter("Omen");
            if (success)
            {
                Debug.Log("Mua Omen THÀNH CÔNG! Bây giờ bạn có thể trang bị.");
            }
            else
            {
                Debug.LogWarning("Mua thất bại (Không đủ vàng hoặc đã có sẵn).");
            }
        }

        // ==========================================
        // PHẦN 2: TRANG BỊ NHÂN VẬT (EQUIP)
        // ==========================================

        private async System.Threading.Tasks.Task<bool> EquipCharacter(string key)
        {
            if (!characters.ContainsKey(key)) return false;
            var charInfo = characters[key];
            return await SupabaseManager.Instance.EquipCharacter(charInfo.Name);
        }

        public async void OnEquipAdamClick()
        {
            bool success = await EquipCharacter("Adam");
            if (success)
            {
                Debug.Log("Đã đổi sang Adam!");
                RefreshLobby();
            }
        }

        public async void OnEquipNarutoClick()
        {
            bool success = await EquipCharacter("Naruto");
            if (success)
            {
                Debug.Log("Trang bị Naruto THÀNH CÔNG!");
                RefreshLobby();
            }
        }

        public async void OnEquipKellyClick()
        {
            bool success = await EquipCharacter("Kelly");
            if (success)
            {
                Debug.Log("Đã đổi sang Kelly!");
                RefreshLobby();
            }
        }

        public async void OnEquipFFClick()
        {
            bool success = await EquipCharacter("FF");
            if (success)
            {
                Debug.Log("Đã đổi sang FF!");
                RefreshLobby();
            }
        }

        public async void OnEquipGokuClick()
        {
            bool success = await EquipCharacter("Goku");
            if (success)
            {
                Debug.Log("Đã đổi sang Goku!");
                RefreshLobby();
            }
        }

        public async void OnEquipOmenClick()
        {
            bool success = await EquipCharacter("Omen");
            if (success)
            {
                Debug.Log("Đã đổi sang Omen!");
                RefreshLobby();
            }
        }

        // ==========================================
        // HÀM HỖ TRỢ
        // ==========================================
        
        private void RefreshLobby()
        {
            var display = FindFirstObjectByType<LobbyCharacterDisplay>();
            if (display != null)
            {
                display.UpdateDisplay();
            }
        }
    }
}