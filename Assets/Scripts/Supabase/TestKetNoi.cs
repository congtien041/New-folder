using UnityEngine;
using Supabase;
namespace SimpleFPS
{
    public class TestKetNoi : MonoBehaviour
    {
        private async void Start()
        {
            // Khởi tạo Supabase client
            var client = new Supabase.Client("https://ebdytrnnavfvgjkjysqr.supabase.co", "sb_publishable_0ONS0ziLjfkpkL96Bt-kHg_pBcgw_aB");
            Debug.Log("Đã kết nối đến Supabase!");
        }
    }
}