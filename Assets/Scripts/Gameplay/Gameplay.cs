using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Fusion;

#if !UNITY_EDITOR && (UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS)
#error This sample doesn't support currently selected platform, please switch to Windows, Mac, Linux in Build Settings.
#endif

namespace SimpleFPS
{
	/// <summary>
	/// Runtime data structure to hold player information which must survive events like player death/disconnect.
	/// </summary>
	public struct PlayerData : INetworkStruct
	{
		[Networked, Capacity(24)]
		public string    Nickname { get => default; set {} }
		// --- THÊM DÒNG NÀY ---
		[Networked, Capacity(16)] 
        public string    CharacterID { get => default; set {} } 
        // ---------------------
		public PlayerRef PlayerRef;
		public int       Kills;
		public int       Deaths;
		public int       LastKillTick;
		public int       StatisticPosition;
		public bool      IsAlive;
		public bool      IsConnected;
		public byte      Team;
	}

	public enum EGameplayState
	{
		Skirmish = 0,
		Running  = 1,
		Finished = 2,
	}

	/// <summary>
	/// Drives gameplay logic - state, timing, handles player connect/disconnect/spawn/despawn/death, calculates statistics.
	/// </summary>
	public class Gameplay : NetworkBehaviour
	{
		public GameUI GameUI;
		public Player PlayerPrefab;
		public float  GameDuration = 180f;
		public float  PlayerRespawnTime = 5f;
		public float  DoubleDamageDuration = 30f;

		// --- CÔNG TẮC CHẾ ĐỘ CHƠI ---
		[Header("Game Mode Settings")]
		public bool IsTeamMode = false;
		public bool IsZombieMode = false; // THÊM DÒNG NÀY ĐỂ BẬT TẮT CHẾ ĐỘ ZOMBIE
		[Networked] public int Team1Score { get; set; }
		[Networked] public int Team2Score { get; set; }
		public int TargetScoreToWin = 20; // Đội nào giết 20 mạng trước sẽ thắng

		[Networked][Capacity(32)][HideInInspector]
		public NetworkDictionary<PlayerRef, PlayerData> PlayerData { get; }
		[Networked][HideInInspector]
		public TickTimer RemainingTime { get; set; }
		[Networked][HideInInspector]
		public EGameplayState State { get; set; }

		public bool DoubleDamageActive => State == EGameplayState.Running && RemainingTime.RemainingTime(Runner).GetValueOrDefault() < DoubleDamageDuration;

		private bool _isNicknameSent;
		private float _runningStateTime;
		private List<Player> _spawnedPlayers = new(16);
		private List<PlayerRef> _pendingPlayers = new(16);
		private List<PlayerData> _tempPlayerData = new(16);
		private List<Transform> _recentSpawnPoints = new(4);


		// --- BIẾN ĐÁNH DẤU ĐÃ LƯU ĐIỂM ---
		private bool _hasSavedStats = false;



		

		public void PlayerKilled(PlayerRef killerPlayerRef, PlayerRef victimPlayerRef, EWeaponType weaponType, bool isCriticalKill)
        {
            if (HasStateAuthority == false) return;

            // KIỂM TRA PHÂN LOẠI: Ai là Người, Ai là Quái?
            bool isKillerPlayer = PlayerData.TryGet(killerPlayerRef, out PlayerData killerData);
            bool isVictimPlayer = PlayerData.TryGet(victimPlayerRef, out PlayerData victimData);

            // =========================================
            // 1. XỬ LÝ NGƯỜI GIẾT (CỘNG ĐIỂM KILLS)
            // =========================================
            if (isKillerPlayer)
            {
                if (IsZombieMode)
                {
                    // [CHẾ ĐỘ ZOMBIE]: Giết quái được cộng mạng (1 mạng = 1 Vàng lúc hết trận)
                    if (!isVictimPlayer) 
                    {
                        killerData.Kills++;
                        killerData.LastKillTick = Runner.Tick;
                        PlayerData.Set(killerPlayerRef, killerData);
                    }
                }
                else if (isVictimPlayer) 
                {
                    // [CHẾ ĐỘ PVP BÌNH THƯỜNG]: Chỉ cộng điểm khi Người giết Người
                    if (IsTeamMode)
                    {
                        // ĐẤU ĐỘI (2v2)
                        if (killerData.Team != victimData.Team)
                        {
                            killerData.Kills++;
                            killerData.LastKillTick = Runner.Tick;
                            PlayerData.Set(killerPlayerRef, killerData);

                            if (killerData.Team == 1) Team1Score++;
                            else if (killerData.Team == 2) Team2Score++;

                            if (Team1Score >= TargetScoreToWin || Team2Score >= TargetScoreToWin)
                            {
                                StopGameplay(); 
                                return; 
                            }
                        }
                    }
                    else
                    {
                        // BẮN TỰ DO (FFA)
                        killerData.Kills++;
                        killerData.LastKillTick = Runner.Tick;
                        PlayerData.Set(killerPlayerRef, killerData);

                        if (killerData.Kills >= TargetScoreToWin)
                        {
                            StopGameplay(); 
                            return;
                        }
                    }
                }
            }

            // =========================================
            // 2. XỬ LÝ NẠN NHÂN (BỊ TRỪ ĐIỂM & HỒI SINH)
            // =========================================
            if (isVictimPlayer)
            {
                // NẾU NẠN NHÂN LÀ CON NGƯỜI -> Tính lượt chết, hiện thông báo và cho hồi sinh
                victimData.Deaths++;
                victimData.IsAlive = false;
                PlayerData.Set(victimPlayerRef, victimData);

                // Gửi thông báo Kill Feed (Góc trên bên phải)
                RPC_PlayerKilled(killerPlayerRef, victimPlayerRef, weaponType, isCriticalKill);

                // Bắt đầu đếm ngược hồi sinh nạn nhân
                StartCoroutine(RespawnPlayer(victimPlayerRef, PlayerRespawnTime));
            }
            else if (IsZombieMode)
            {
                // NẾU NẠN NHÂN LÀ ZOMBIE -> Không làm gì cả (Chỉ in ra Console cho vui)
                Debug.Log("💀 [ZOMBIE MODE] Một con Zombie vừa bị tiễn về chầu trời!");
            }

            // =========================================
            // 3. CẬP NHẬT LẠI BẢNG XẾP HẠNG (TAB)
            // =========================================
            RecalculateStatisticPositions();
        }
		public override void Spawned()
		{
			if (Runner.Mode == SimulationModes.Server)
			{
				Application.targetFrameRate = TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;
			}

			if (Runner.GameMode == GameMode.Shared)
			{
				throw new System.NotSupportedException("This sample doesn't support Shared Mode, please start the game as Server, Host or Client.");
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (HasStateAuthority == false)
				return;

			// PlayerManager is a special helper class which iterates over list of active players (NetworkRunner.ActivePlayers) and call spawn/despawn callbacks on demand.
			PlayerManager.UpdatePlayerConnections(Runner, SpawnPlayer, DespawnPlayer);

			// Start gameplay when there are enough players connected.
			if (State == EGameplayState.Skirmish && PlayerData.Count > 1)
			{
				StartGameplay();
			}

			if (State == EGameplayState.Running)
			{
				_runningStateTime += Runner.DeltaTime;

				var sessionInfo = Runner.SessionInfo;

				// Hide the match after 60 seconds. Players won't be able to randomly connect to existing game and start new one instead.
				// Joining via party code should work.
				if (sessionInfo.IsVisible && (_runningStateTime > 60f || sessionInfo.PlayerCount >= sessionInfo.MaxPlayers))
				{
					sessionInfo.IsVisible = false;
				}

				if (RemainingTime.Expired(Runner))
				{
					StopGameplay();
				}
			}
		}

		public override void Render()
		{
			if (Runner.Mode == SimulationModes.Server)
				return;

			if (_isNicknameSent == false)
			{
                // Lấy tên và nhân vật từ Ổ CỨNG (PlayerPrefs)
                string myName = PlayerPrefs.GetString("Photon.Menu.Username");
                string myChar = PlayerPrefs.GetString("Photon.Menu.Character", "Char_Adam"); 
                
				RPC_SetPlayerInfo(Runner.LocalPlayer, myName, myChar);
				_isNicknameSent = true;
			}
		}

		private void SpawnPlayer(PlayerRef playerRef)
		{
			if (PlayerData.TryGet(playerRef, out var playerData) == false)
			{
				playerData = new PlayerData();
				playerData.PlayerRef = playerRef;
				playerData.Nickname = playerRef.ToString();
				playerData.StatisticPosition = int.MaxValue;
				playerData.IsAlive = false;
				playerData.IsConnected = false;
				/// NẾU LÀ CHẾ ĐỘ TEAM -> CHIA PHE
				if (IsTeamMode)
				{
					int team1Count = 0, team2Count = 0;
					foreach (var p in PlayerData)
					{
						if (p.Value.Team == 1) team1Count++;
						else if (p.Value.Team == 2) team2Count++;
					}
					playerData.Team = (byte)(team1Count <= team2Count ? 1 : 2);
				}
				else
				{
					// NẾU LÀ BẮN TỰ DO -> KHÔNG CÓ TEAM
					playerData.Team = 0; 
				}
			}

			if (playerData.IsConnected == true)
				return;

			Debug.LogWarning($"{playerRef} connected.");

			playerData.IsConnected = true;
			playerData.IsAlive = true;

			PlayerData.Set(playerRef, playerData);

			var spawnPoint = GetSpawnPoint();
			var player = Runner.Spawn(PlayerPrefab, spawnPoint.position, spawnPoint.rotation, playerRef);

			// Set player instance as PlayerObject so we can easily get it from other locations.
			Runner.SetPlayerObject(playerRef, player.Object);

			RecalculateStatisticPositions();
		}

		private void DespawnPlayer(PlayerRef playerRef, Player player)
		{
			if (PlayerData.TryGet(playerRef, out var playerData) == true)
			{
				if (playerData.IsConnected == true)
				{
					Debug.LogWarning($"{playerRef} disconnected.");
				}

				playerData.IsConnected = false;
				playerData.IsAlive = false;
				PlayerData.Set(playerRef, playerData);
			}

			Runner.Despawn(player.Object);

			// --- LUẬT MỚI: XỬ LÝ KHI CÓ NGƯỜI OUT TRẬN ---
			if (State == EGameplayState.Running)
			{
				// Đếm số người còn lại trong phòng (đang connect)
				int activePlayers = 0;
				foreach (var p in PlayerData)
				{
					if (p.Value.IsConnected) activePlayers++;
				}

				// Nếu chỉ còn 1 người (1vs1) out 1 còn 1 -> Người còn lại tự động thắng
				if (activePlayers <= 1)
				{
					StopGameplay();
				}
				
				// Nếu chính mình là người bấm Out / Tắt game -> Lưu kết quả bị phạt
				if (playerRef == Runner.LocalPlayer)
				{
					// KIỂM TRA TƯƠNG TỰ
					bool isRanked = Runner.SessionInfo.IsVisible;
					
					if (isRanked)
					{
						// TÌM TÊN ĐỐI THỦ
						string enemyName = "Unknown";
						foreach (var p in PlayerData)
						{
							if (p.Key != Runner.LocalPlayer)
							{
								enemyName = p.Value.Nickname;
								break;
							}
						}

						// Tính thời gian đã chơi
						float playTime = GameDuration - RemainingTime.RemainingTime(Runner).GetValueOrDefault();
						
						// Gửi thêm enemyName vào hàm (isQuit = true)
						_ = SupabaseManager.Instance.UpdateMatchResult(false, playerData.Kills, playerData.Deaths, playTime, enemyName, true);
					}
					else
					{
						Debug.Log("Thoát Phòng Giao Lưu: An toàn, không bị phạt!");
					}
				}
			}

			RecalculateStatisticPositions();
		}

		private IEnumerator RespawnPlayer(PlayerRef playerRef, float delay)
		{
			if (delay > 0f)
				yield return new WaitForSecondsRealtime(delay);

			if (Runner == null)
				yield break;

			// Despawn old player object if it exists.
			var playerObject = Runner.GetPlayerObject(playerRef);
			if (playerObject != null)
			{
				Runner.Despawn(playerObject);
			}

			// Don't spawn the player for disconnected clients.
			if (PlayerData.TryGet(playerRef, out PlayerData playerData) == false || playerData.IsConnected == false)
				yield break;

			// Update player data.
			playerData.IsAlive = true;
			PlayerData.Set(playerRef, playerData);

			var spawnPoint = GetSpawnPoint();
			var player = Runner.Spawn(PlayerPrefab, spawnPoint.position, spawnPoint.rotation, playerRef);

			// Set player instance as PlayerObject so we can easily get it from other locations.
			Runner.SetPlayerObject(playerRef, player.Object);
		}

		private Transform GetSpawnPoint()
		{
			Transform spawnPoint = default;

			// Iterate over all spawn points in the scene.
			var spawnPoints = Runner.SimulationUnityScene.GetComponents<SpawnPoint>(false);
			for (int i = 0, offset = Random.Range(0, spawnPoints.Length); i < spawnPoints.Length; i++)
			{
				spawnPoint = spawnPoints[(offset + i) % spawnPoints.Length].transform;

				if (_recentSpawnPoints.Contains(spawnPoint) == false)
					break;
			}

			// Add spawn point to list of recently used spawn points.
			_recentSpawnPoints.Add(spawnPoint);

			// Ignore only last 3 spawn points.
			if (_recentSpawnPoints.Count > 3)
			{
				_recentSpawnPoints.RemoveAt(0);
			}

			return spawnPoint;
		}

		private void StartGameplay()
		{
			// Stop all respawn coroutines.
			StopAllCoroutines();

			State = EGameplayState.Running;
			RemainingTime = TickTimer.CreateFromSeconds(Runner, GameDuration);

			// --- THÊM ĐÚNG 1 DÒNG NÀY ĐỂ MỞ KHÓA KHI BẮT ĐẦU VÁN MỚI ---
			_hasSavedStats = false;

			// Reset player data after skirmish and respawn players.
			foreach (var playerPair in PlayerData)
			{
				var data = playerPair.Value;

				data.Kills = 0;
				data.Deaths = 0;
				data.StatisticPosition = int.MaxValue;
				data.IsAlive = false;

				PlayerData.Set(data.PlayerRef, data);

				StartCoroutine(RespawnPlayer(data.PlayerRef, 0f));
			}
		}

		private void StopGameplay()
		{
			RecalculateStatisticPositions();
			State = EGameplayState.Finished; 

			Debug.Log("<color=cyan>[GAME] Host đã gọi StopGameplay - Đang phát loa RPC cho tất cả người chơi...</color>");
			RPC_NotifyMatchEnd();
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
		private void RPC_NotifyMatchEnd()
		{
			// NẾU ĐÃ LƯU RỒI THÌ BỎ QUA KHÔNG LƯU NỮA
			if (_hasSavedStats) return; 
			_hasSavedStats = true; // Đánh dấu là đã lưu

			if (SupabaseManager.Instance == null || !SupabaseManager.Instance.IsLoggedIn) return;

			if (PlayerData.TryGet(Runner.LocalPlayer, out var myData))
			{
				if (IsZombieMode)
				{
					bool isTop1 = (myData.StatisticPosition == 1);
					_ = SupabaseManager.Instance.UpdateZombieMatchResult(myData.Kills, isTop1);
				}
				else
				{
					bool isWin = false;
					if (IsTeamMode) {
						if (myData.Team == 1 && Team1Score > Team2Score) isWin = true;
						else if (myData.Team == 2 && Team2Score > Team1Score) isWin = true;
					} else {
						if (myData.StatisticPosition == 1) isWin = true;
					}

					// Tạm thời bật true để chắc chắn test được Vàng và Rank
					bool isRanked = true; 

					if (isRanked)
					{
						string enemyName = "Đối thủ";
						foreach (var p in PlayerData) {
							if (p.Key != Runner.LocalPlayer && p.Value.Team != myData.Team) {
								enemyName = p.Value.Nickname;
								break;
							}
						}
						float playTime = GameDuration - RemainingTime.RemainingTime(Runner).GetValueOrDefault();
						_ = SupabaseManager.Instance.UpdateMatchResult(isWin, myData.Kills, myData.Deaths, playTime, enemyName, false);
						Debug.Log($"<color=green>[SUCCESS] Đã nhận lệnh RPC và lưu điểm: {(isWin ? "THẮNG" : "THUA")}</color>");
					}
				}
			}
		}


		private void RecalculateStatisticPositions()
		{
			if (State == EGameplayState.Finished)
				return;

			_tempPlayerData.Clear();
			foreach (var pair in PlayerData) {
				_tempPlayerData.Add(pair.Value);
			}

			_tempPlayerData.Sort((a, b) => {
				if (a.Kills != b.Kills) return b.Kills.CompareTo(a.Kills);
				return a.LastKillTick.CompareTo(b.LastKillTick);
			});

			for (int i = 0; i < _tempPlayerData.Count; i++)
			{
				var playerData = _tempPlayerData[i];
				// SỬA Ở ĐÂY: Luôn trao Hạng 1, 2, 3... tương ứng với vị trí, kể cả khi 0 Kills
				playerData.StatisticPosition = i + 1; 

				PlayerData.Set(playerData.PlayerRef, playerData);
			}
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
		private void RPC_PlayerKilled(PlayerRef killerPlayerRef, PlayerRef victimPlayerRef, EWeaponType weaponType, bool isCriticalKill)
		{
			string killerNickname = "";
			string victimNickname = "";

			if (PlayerData.TryGet(killerPlayerRef, out PlayerData killerData))
			{
				killerNickname = killerData.Nickname;
			}

			if (PlayerData.TryGet(victimPlayerRef, out PlayerData victimData))
			{
				victimNickname = victimData.Nickname;
			}

			GameUI.GameplayView.KillFeed.ShowKill(killerNickname, victimNickname, weaponType, isCriticalKill);
		}

		// [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
		// private void RPC_SetPlayerNickname(PlayerRef playerRef, string nickname)
		// {
		// 	var playerData = PlayerData.Get(playerRef);
		// 	playerData.Nickname = nickname;
		// 	playerData.CharacterID = characterID; // Lưu nhân vật lên mạng
		// 	PlayerData.Set(playerRef, playerData);
		// }

		[Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
		private void RPC_SetPlayerInfo(PlayerRef playerRef, string nickname, string characterID)
		{
			var playerData = PlayerData.Get(playerRef);
			
			// --- TỐI ƯU HIỂN THỊ TEAM 2vs2 ---
			if (IsTeamMode)
			{
				// Gắn thẻ màu Xanh/Đỏ vào trước tên nhân vật
				string teamTag = playerData.Team == 1 ? "<color=#00aaff>[TEAM XANH]</color>" : "<color=#ff4444>[TEAM ĐỎ]</color>";
				playerData.Nickname = $"{teamTag} {nickname}";
			}
			else
			{
				playerData.Nickname = nickname;
			}

            playerData.CharacterID = characterID; 
			PlayerData.Set(playerRef, playerData);
		}


		// ==========================================
		// HỘP ĐEN CỨU HỘ: TỰ LƯU ĐIỂM KHI BỊ HOST ĐÁ VĂNG (LỖI 104)
		// ==========================================
		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			// Nếu bị văng mạng, sập phòng mà CHƯA KỊP LƯU ĐIỂM
			if (!_hasSavedStats && SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
			{
				_hasSavedStats = true; // Khóa lại ngay lập tức
				
				if (PlayerData.TryGet(Runner.LocalPlayer, out var myData))
				{
					Debug.Log("<color=orange>[CỨU HỘ] Server bị tắt đột ngột! Máy Client đang tự động lưu bù điểm...</color>");
					
					if (IsZombieMode)
					{
						bool isTop1 = (myData.StatisticPosition == 1);
						_ = SupabaseManager.Instance.UpdateZombieMatchResult(myData.Kills, isTop1);
					}
					else
					{
						bool isWin = false;
						if (IsTeamMode) {
							if (myData.Team == 1 && Team1Score > Team2Score) isWin = true;
							else if (myData.Team == 2 && Team2Score > Team1Score) isWin = true;
						} else {
							if (myData.StatisticPosition == 1) isWin = true;
						}

						// Gửi bù lên Server (Ghi chú tên đối thủ là "Host out mạng" để dễ phân biệt trong lịch sử)
						_ = SupabaseManager.Instance.UpdateMatchResult(isWin, myData.Kills, myData.Deaths, 0, "Host out mạng", false);
					}
				}
			}
		}
	}
}
