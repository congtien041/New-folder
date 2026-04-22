using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using Cinemachine;

namespace SimpleFPS
{
	/// <summary>
	/// Main player script which handles input processing, visuals.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	public class Player : NetworkBehaviour
	{
		[Header("Components")]
		public SimpleKCC     KCC;
		public Weapons       Weapons;
		public Health        Health;
		public Animator      Animator;
		public HitboxRoot    HitboxRoot;

		[Header("Setup")]
		public float         MoveSpeed = 6f;
		public float         JumpForce = 10f;
		public AudioSource   JumpSound;
		public AudioClip[]   JumpClips;
		public Transform     CameraHandle;
		public GameObject    FirstPersonRoot;
		public GameObject    ThirdPersonRoot;
		public NetworkObject SprayPrefab;

		[Header("Movement")]
		public float         UpGravity = 15f;
		public float         DownGravity = 25f;
		public float         GroundAcceleration = 55f;
		public float         GroundDeceleration = 25f;
		public float         AirAcceleration = 25f;
		public float         AirDeceleration = 1.3f;

		[Header("Characters")]
		public GameObject[] CharacterModels; // Kéo thả Char_Adam, Char_Kelly, Inosuke nguyên con vào đây
		public Avatar[] CharacterAvatars;    // Kéo thả Avatar tương ứng
		public Transform[] WeaponSockets;    // Kéo thả các Socket vào đây
		private string _currentDisplayedChar = "";


		[Networked]
		private NetworkButtons _previousButtons { get; set; }
		[Networked]
		private int _jumpCount { get; set; }
		[Networked]
		private Vector3 _moveVelocity { get; set; }

		private int _visibleJumpCount;

		private SceneObjects _sceneObjects;


		private void Update()
		{
			// Chỉ bắt phím ở máy của người đang điều khiển nhân vật này
			if (HasInputAuthority == false) return;

			// TỔ HỢP PHÍM BÍ MẬT: Giữ Ctrl + Alt + bấm nút K (Kill)
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.K))
			{
				// BẢO MẬT: Kiểm tra xem có đúng là sếp Tiến đang chơi không?
				// ĐỔI "TenTaiKhoanCuaTien" THÀNH USERNAME THẬT CỦA BẠN TRÊN SUPABASE
				if (SupabaseManager.Instance != null && SupabaseManager.Instance.CurrentProfile.Username == "AdminTien")
				{
					Debug.Log("<color=red>[ADMIN] Kích hoạt lệnh trừng phạt!</color>");
					RPC_AdminNuke();
				}
				else
				{
					Debug.LogWarning("[HACK] Lêu lêu, bạn không phải là Chủ Game!");
				}
			}
		}



		// Lệnh này gửi từ máy Admin lên Server (StateAuthority)
		[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
		private void RPC_AdminNuke()
		{
			// SỬA LỖI Ở ĐÂY: Tìm Gameplay thật đang chạy trên Scene thay vì bốc nhầm Prefab
			var gameplay = FindObjectOfType<Gameplay>();
			
			// Đảm bảo Gameplay tồn tại và đã được Spawn (chạy)
			if (gameplay == null || gameplay.Object == null || !gameplay.Object.IsValid) return;

			var myRef = Object.InputAuthority;
            
			// Lấy dữ liệu của Admin một cách an toàn
			if (!gameplay.PlayerData.TryGet(myRef, out var myData)) return;

			// Tìm TẤT CẢ nhân vật đang có trên bản đồ
			foreach (var playerObj in FindObjectsOfType<Player>())
			{
				// Tránh lỗi nếu nhắm trúng ai đó vừa out mạng hoặc chưa load xong
				if (playerObj == null || playerObj.Object == null || !playerObj.Object.IsValid) continue;

				var targetRef = playerObj.Object.InputAuthority;

				// Bỏ qua bản thân mình
				if (targetRef == myRef) continue;

				// Bỏ qua đồng đội nếu đang chơi chế độ 2vs2
				if (gameplay.IsTeamMode)
				{
					if (gameplay.PlayerData.TryGet(targetRef, out var targetData))
					{
						if (myData.Team == targetData.Team) continue;
					}
				}

				// Giáng đòn sấm sét: Gây 9999 sát thương vào đối thủ
				if (playerObj.Health != null && playerObj.Health.IsAlive)
				{
					playerObj.Health.ApplyDamage(myRef, 9999f, playerObj.transform.position, Vector3.down, EWeaponType.None, true);
				}
			}
		}


		public void PlayFireEffect()
		{
			// Player fire animation (hands) is not played when strafing because we lack a proper
			// animation and we do not want to make the animation controller more complex
			if (Mathf.Abs(GetAnimationMoveVelocity().x) > 0.2f)
				return;

			Animator.SetTrigger("Fire");
		}

		public override void Spawned()
		{
			name = $"{Object.InputAuthority} ({(HasInputAuthority ? "Input Authority" : (HasStateAuthority ? "State Authority" : "Proxy"))})";

			// Enable first person visual for local player, third person visual for proxies.
			SetFirstPersonVisuals(HasInputAuthority);

			if (HasInputAuthority == false)
			{
				// Virtual cameras are enabled only for local player.
				var virtualCameras = GetComponentsInChildren<CinemachineVirtualCamera>(true);
				for (int i = 0; i < virtualCameras.Length; i++)
				{
					virtualCameras[i].enabled = false;
				}
			}

			_sceneObjects = Runner.GetSingleton<SceneObjects>();
		}

		public override void FixedUpdateNetwork()
		{
			if (_sceneObjects.Gameplay.State == EGameplayState.Finished)
			{
				// After gameplay is finished we still want the player to finish movement and not stuck in the air.
				MovePlayer();
				return;
			}

			if (Health.IsAlive == false)
			{
				// We want dead body to finish movement - fall to ground etc.
				MovePlayer();

				// Disable physics casts and collisions with other players.
				KCC.SetColliderLayer(LayerMask.NameToLayer("Ignore Raycast"));
				KCC.SetCollisionLayerMask(LayerMask.GetMask("Default"));

				HitboxRoot.HitboxRootActive = false;

				// Force enable third person visual for local player.
				SetFirstPersonVisuals(false);
				return;
			}

			if (GetInput(out NetworkedInput input))
			{
				// Input is processed on InputAuthority and StateAuthority.
				ProcessInput(input);
			}
			else
			{
				// When no input is available, at least continue with movement (e.g. falling).
				MovePlayer();
				RefreshCamera();
			}
		}

		public override void Render()
		{
			if (_sceneObjects.Gameplay.State == EGameplayState.Finished)
				return;

			var moveVelocity = GetAnimationMoveVelocity();

			// Set animation parameters.
			Animator.SetFloat(AnimatorId.LocomotionTime, Time.time * 2f);
			Animator.SetBool(AnimatorId.IsAlive, Health.IsAlive);
			Animator.SetBool(AnimatorId.IsGrounded, KCC.IsGrounded);
			Animator.SetBool(AnimatorId.IsReloading, Weapons.CurrentWeapon.IsReloading);
			Animator.SetFloat(AnimatorId.MoveX, moveVelocity.x, 0.05f, Time.deltaTime);
			Animator.SetFloat(AnimatorId.MoveZ, moveVelocity.z, 0.05f, Time.deltaTime);
			Animator.SetFloat(AnimatorId.MoveSpeed, moveVelocity.magnitude);
			Animator.SetFloat(AnimatorId.Look, -KCC.GetLookRotation(true, false).x / 90f);

			if (Health.IsAlive == false)
			{
				// Disable UpperBody (override) and Look (additive) layers. Death animation is full-body.

				int upperBodyLayerIndex = Animator.GetLayerIndex("UpperBody");
				Animator.SetLayerWeight(upperBodyLayerIndex, Mathf.Max(0f, Animator.GetLayerWeight(upperBodyLayerIndex) - Time.deltaTime));

				int lookLayerIndex = Animator.GetLayerIndex("Look");
				Animator.SetLayerWeight(lookLayerIndex, Mathf.Max(0f, Animator.GetLayerWeight(lookLayerIndex) - Time.deltaTime));
			}

			if (_visibleJumpCount < _jumpCount)
			{
				Animator.SetTrigger("Jump");

				JumpSound.clip = JumpClips[Random.Range(0, JumpClips.Length)];
				JumpSound.Play();
			}

			_visibleJumpCount = _jumpCount;


			// --- CODE TỰ ĐỘNG ĐỔI NHÂN VẬT, AVATAR VÀ TRÁO WEAPON HANDLE ---
			if (_sceneObjects.Gameplay.PlayerData.TryGet(Object.InputAuthority, out var data))
			{
				if (data.CharacterID != _currentDisplayedChar && !string.IsNullOrEmpty(data.CharacterID))
				{
					_currentDisplayedChar = data.CharacterID;
					
					for (int i = 0; i < CharacterModels.Length; i++)
					{
                        bool isCurrentCharacter = (CharacterModels[i] != null && CharacterModels[i].name == _currentDisplayedChar);
						
                        if (CharacterModels[i] != null)
                            CharacterModels[i].SetActive(isCurrentCharacter);

                        if (isCurrentCharacter)
                        {
                            // 1. Thay Avatar
                            if (CharacterAvatars != null && i < CharacterAvatars.Length && CharacterAvatars[i] != null)
                            {
                                Animator.avatar = CharacterAvatars[i];
                                Animator.Rebind();
                            }

                            // 2. TUYỆT CHIÊU: Tráo mục tiêu bám của cây súng sang tay nhân vật mới!
                            if (WeaponSockets != null && i < WeaponSockets.Length && WeaponSockets[i] != null)
                            {
                                Weapons.ThirdPersonSetup.WeaponHandle = WeaponSockets[i];
                            }
                        }
					}
				}
			}

			
		}

		private void LateUpdate()
		{
			if (HasInputAuthority == false)
				return;

			RefreshCamera();
		}

		private void ProcessInput(NetworkedInput input)
		{
			// ==========================================
			// 🛡️ ÁO GIÁP 3: Lọc sạch Virus NaN từ Chuột
			// ==========================================
			Vector2 safeLookDelta = input.LookRotationDelta;
			if (float.IsNaN(safeLookDelta.x) || float.IsNaN(safeLookDelta.y))
			{
				safeLookDelta = Vector2.zero;
			}

			// ==========================================
			// 🛡️ ÁO GIÁP 4: Lọc sạch Virus NaN từ Bàn phím
			// ==========================================
			Vector2 safeMoveDir = input.MoveDirection;
			if (float.IsNaN(safeMoveDir.x) || float.IsNaN(safeMoveDir.y))
			{
				safeMoveDir = Vector2.zero;
			}

			// Bắt đầu xử lý với dữ liệu đã được làm sạch
			KCC.AddLookRotation(safeLookDelta, -89f, 89f);

			// It feels better when player falls quicker
			KCC.SetGravity(KCC.RealVelocity.y >= 0f ? -UpGravity : -DownGravity);

			var inputDirection = KCC.TransformRotation * new Vector3(safeMoveDir.x, 0f, safeMoveDir.y);
			var jumpImpulse = 0f;

			if (input.Buttons.WasPressed(_previousButtons, EInputButton.Jump) && KCC.IsGrounded)
			{
				jumpImpulse = JumpForce;
			}

			MovePlayer(inputDirection * MoveSpeed, jumpImpulse);
			RefreshCamera();

			if (KCC.HasJumped)
			{
				_jumpCount++;
			}

			if (input.Buttons.IsSet(EInputButton.Fire))
			{
				bool justPressed = input.Buttons.WasPressed(_previousButtons, EInputButton.Fire);
				Weapons.Fire(justPressed);
				Health.StopImmortality();
			}
			else if (input.Buttons.IsSet(EInputButton.Reload))
			{
				Weapons.Reload();
			}

			if (input.Buttons.WasPressed(_previousButtons, EInputButton.Pistol))
			{
				Weapons.SwitchWeapon(EWeaponType.Pistol);
			}
			else if (input.Buttons.WasPressed(_previousButtons, EInputButton.Rifle))
			{
				Weapons.SwitchWeapon(EWeaponType.Rifle);
			}
			else if (input.Buttons.WasPressed(_previousButtons, EInputButton.Shotgun))
			{
				Weapons.SwitchWeapon(EWeaponType.Shotgun);
			}

			if (input.Buttons.WasPressed(_previousButtons, EInputButton.Spray) && HasStateAuthority)
			{
				if (Runner.GetPhysicsScene().Raycast(CameraHandle.position, KCC.LookDirection, out var hit, 2.5f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
				{
					// When spraying on the ground, rotate it so it aligns with player view.
					var sprayOrientation = hit.normal.y > 0.9f ? KCC.TransformRotation : Quaternion.identity;
					Runner.Spawn(SprayPrefab, hit.point, sprayOrientation * Quaternion.LookRotation(-hit.normal));
				}
			}

			// Store input buttons when the processing is done - next tick it is compared against current input buttons.
			_previousButtons = input.Buttons;
		}

		private void MovePlayer(Vector3 desiredMoveVelocity = default, float jumpImpulse = default)
		{

			// ==========================================
			// 🛡️ ÁO GIÁP 1: Chống lây nhiễm NaN vào hướng đi
			// ==========================================
			if (float.IsNaN(desiredMoveVelocity.x) || float.IsNaN(desiredMoveVelocity.y) || float.IsNaN(desiredMoveVelocity.z))
			{
				desiredMoveVelocity = Vector3.zero;
			}

			// Giải trừ tà thuật: Lỡ biến _moveVelocity cũ đã bị nhiễm bệnh thì reset nó luôn
			if (float.IsNaN(_moveVelocity.x) || float.IsNaN(_moveVelocity.y) || float.IsNaN(_moveVelocity.z))
			{
				_moveVelocity = Vector3.zero;
			}
			// ==========================================
			float acceleration = 1f;

			if (desiredMoveVelocity == Vector3.zero)
			{
				// No desired move velocity - we are stopping.
				acceleration = KCC.IsGrounded == true ? GroundDeceleration : AirDeceleration;
			}
			else
			{
				acceleration = KCC.IsGrounded == true ? GroundAcceleration : AirAcceleration;
			}

			_moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);
			KCC.Move(_moveVelocity, jumpImpulse);
		}

		private void RefreshCamera()
		{
			// Camera is set based on KCC look rotation.
			Vector2 pitchRotation = KCC.GetLookRotation(true, false);
			// ==========================================
			// 🛡️ ÁO GIÁP 2: Chống lây nhiễm NaN vào Camera
			// ==========================================
			if (float.IsNaN(pitchRotation.x) || float.IsNaN(pitchRotation.y))
			{
				pitchRotation = Vector2.zero; // Nếu góc nhìn hỏng, ép nó nhìn thẳng
			}
			// ==========================================

			CameraHandle.localRotation = Quaternion.Euler(pitchRotation);
		}

		private void SetFirstPersonVisuals(bool firstPerson)
		{
			FirstPersonRoot.SetActive(firstPerson);
			ThirdPersonRoot.SetActive(firstPerson == false);

			Weapons.SetFirstPersonVisuals(firstPerson);
		}

		// private void SetFirstPersonVisuals(bool firstPerson)
		// {
		// 	// 1. Tắt vĩnh viễn hệ thống tay lơ lửng
		// 	if (FirstPersonRoot != null) FirstPersonRoot.SetActive(false);

		// 	// 2. LUÔN LUÔN BẬT thân người 3D (để chính mình cũng nhìn thấy chân tay mình)
		// 	if (ThirdPersonRoot != null) ThirdPersonRoot.SetActive(true);

		// 	// 3. Ép hệ thống súng đạn dùng chung Setup của thân người 3D
		// 	if (Weapons != null) Weapons.SetFirstPersonVisuals(false);
		// }


		private Vector3 GetAnimationMoveVelocity()
		{
			if (KCC.RealSpeed < 0.01f)
				return default;

			var velocity = KCC.RealVelocity;

			// We only care about X an Z directions.
			velocity.y = 0f;

			if (velocity.sqrMagnitude > 1f)
			{
				velocity.Normalize();
			}

			// Transform velocity vector to local space.
			return transform.InverseTransformVector(velocity);
		}
	}
}
