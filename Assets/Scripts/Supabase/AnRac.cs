using UnityEngine;

namespace SimpleFPS
{
    public class AnRac : MonoBehaviour
    {
        [Header("Menu Trash Cleanup")]
        public string TrashTag = "Rac";

        private void Start()
        {
            CleanupMenuTrash();
        }

        public void CleanupMenuTrash()
        {
            var extraUIs = GameObject.FindGameObjectsWithTag(TrashTag);
            foreach (var ui in extraUIs)
            {
                ui.SetActive(false);
                Debug.Log($"[GAME] Đã dọn dẹp rác Menu: {ui.name}");
            }
        }
    }
}
