using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SimpleFPS
{
	/// <summary>
	/// Main UI script that stores references to other elements (views).
	/// </summary>
	public class GameUI : MonoBehaviour
	{
		public Gameplay       Gameplay;
		[HideInInspector]
		public NetworkRunner  Runner;

		public UIPlayerView   PlayerView;
		public UIGameplayView GameplayView;
		public UIGameOverView GameOverView;
		public GameObject     ScoreboardView;
		public GameObject     MenuView;
		public UISettingsView SettingsView;
		public GameObject     DisconnectedView;

		// Called from NetworkEvents on NetworkRunner object
		public void OnRunnerShutdown(NetworkRunner runner, ShutdownReason reason)
		{
            // NẾU PHÒNG BỊ SẬP ĐỘT NGỘT MÀ TRẬN ĐẤU VẪN ĐANG DIỄN RA
            if (Gameplay != null && Gameplay.State == EGameplayState.Running)
            {
                // Giả định: Người kia Out (Host tắt game) -> Mình mặc định Thắng
                if (SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
                {
                    if (Gameplay.PlayerData.TryGet(runner.LocalPlayer, out var myData))
                    {
                        float playTime = Gameplay.GameDuration - Gameplay.RemainingTime.RemainingTime(runner).GetValueOrDefault();
                        // Thưởng thắng, cộng điểm bình thường
                        _ = SupabaseManager.Instance.UpdateMatchResult(true, myData.Kills, myData.Deaths, playTime, "RageQuitter", false);
                    }
                }
            }

			DisconnectedView.SetActive(true);
		}

		public void GoToMenu()
		{
			if (Runner != null)
			{
				Runner.Shutdown();
			}

			SceneManager.LoadScene("Startup");
		}

		private void Awake()
		{
			PlayerView.gameObject.SetActive(false);
			MenuView.SetActive(false);
			SettingsView.gameObject.SetActive(false);
			DisconnectedView.SetActive(false);

			SettingsView.LoadSettings();

			// Make sure the cursor starts unlocked
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		private void Update()
		{
			if (Application.isBatchMode == true)
				return;

			if (Gameplay.Object == null || Gameplay.Object.IsValid == false)
				return;

			Runner = Gameplay.Runner;

			var keyboard = Keyboard.current;
			bool gameplayActive = Gameplay.State < EGameplayState.Finished;

			ScoreboardView.SetActive(gameplayActive && keyboard != null && keyboard.tabKey.isPressed);

			if (gameplayActive && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
			{
				MenuView.SetActive(!MenuView.activeSelf);
			}

			GameplayView.gameObject.SetActive(gameplayActive);
			GameOverView.gameObject.SetActive(gameplayActive == false);

			var playerObject = Runner.GetPlayerObject(Runner.LocalPlayer);
			if (playerObject != null)
			{
				var player = playerObject.GetComponent<Player>();
				var playerData = Gameplay.PlayerData.Get(Runner.LocalPlayer);

				PlayerView.UpdatePlayer(player, playerData);
				PlayerView.gameObject.SetActive(gameplayActive);
			}
			else
			{
				PlayerView.gameObject.SetActive(false);
			}
		}
	}
}
