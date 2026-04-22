using Fusion;
using UnityEngine;
using UnityEngine.AI;

namespace SimpleFPS
{
    public class ZombieAI : NetworkBehaviour
    {
        public NavMeshAgent Agent;
        public float AttackDamage = 25f;
        public float AttackCooldown = 1.5f;
        public float AttackRange = 1.5f;

        private Health _health;
        [Networked] private TickTimer _attackTimer { get; set; }
        [Networked] private PlayerRef _targetPlayer { get; set; }
        
        // --- FIX LỖI DỌN XÁC: Đổi sang biến thường để đồng hồ đếm giờ không bị kẹt ---
        private bool _isDeadLocal;
        private TickTimer _despawnTimer;

        private float _verticalVelocity = 0f;

        public override void Spawned()
        {
            _health = GetComponent<Health>();
            _isDeadLocal = false; // Đặt lại trạng thái sống khi lấy từ Pool ra
            _verticalVelocity = 0f; 

            if (_health == null) Debug.LogError("👉 [LỖI ZOMBIE]: Chưa gắn 'Health'!");
            if (Agent == null) Debug.LogError("👉 [LỖI ZOMBIE]: Chưa kéo 'NavMesh Agent'!");
            else Agent.speed = 5f;

            if (HasStateAuthority) AssignRandomTarget();
        }

        public override void FixedUpdateNetwork()
        {
            if (_health == null) return;
            
            // ==========================================
            // 1. KHI ZOMBIE BỊ BẮN HẾT MÁU
            // ==========================================
            if (!_health.IsAlive) 
            {
                if (Agent != null && Agent.isOnNavMesh) Agent.isStopped = true;

                // Chỉ Chủ phòng mới chạy đếm ngược để dọn dẹp
                if (HasStateAuthority)
                {
                    if (!_isDeadLocal)
                    {
                        _isDeadLocal = true;
                        _despawnTimer = TickTimer.CreateFromSeconds(Runner, 2f); // 2 giây sau sẽ dọn
                        Debug.Log("💀 [ZOMBIE] Vừa gục ngã! Đang đếm ngược 2s để dọn xác...");
                    }

                    // Đồng hồ chạy hết 2 giây -> Vứt vào Pool
                    if (_isDeadLocal && _despawnTimer.Expired(Runner))
                    {
                        Debug.Log("🧹 [ZOMBIE] Đã dọn dẹp xác thành công!");
                        Runner.Despawn(Object); 
                    }
                }
                
                ApplyGravity(); // Xác chết vẫn chịu trọng lực rớt xuống đất
                return; 
            }

            // ==========================================
            // 2. KHI ZOMBIE CÒN SỐNG & ĐI CẮN NGƯỜI
            // ==========================================
            if (Agent == null) return;

            if (!Agent.isOnNavMesh)
            {
                ApplyGravity();
                return; 
            }
            else
            {
                _verticalVelocity = 0f; 
                Agent.isStopped = false;
            }

            var sceneObjects = Runner.GetSingleton<SceneObjects>();
            if (sceneObjects == null || sceneObjects.Gameplay == null) return;
            var gameplay = sceneObjects.Gameplay;

            if (_targetPlayer.IsNone || !gameplay.PlayerData.ContainsKey(_targetPlayer))
            {
                AssignRandomTarget(); 
                return;
            }

            Player target = GetTargetPlayer(_targetPlayer);
            
            if (target != null && target.Health.IsAlive)
            {
                Agent.SetDestination(target.transform.position);

                if (Vector3.Distance(transform.position, target.transform.position) <= AttackRange)
                {
                    if (_attackTimer.ExpiredOrNotRunning(Runner))
                    {
                        target.Health.ApplyDamage(Object.InputAuthority, AttackDamage, transform.position, transform.forward, EWeaponType.None, false);
                        _attackTimer = TickTimer.CreateFromSeconds(Runner, AttackCooldown);
                    }
                }
            }
            else
            {
                AssignRandomTarget();
            }
        }

        // ==========================================
        // VÙNG AN TOÀN TRUYỆT ĐỐI CỦA TRỌNG LỰC
        // ==========================================
        private void ApplyGravity()
        {
            if (Runner == null) return; 

            _verticalVelocity += Physics.gravity.y * Runner.DeltaTime;
            transform.position += new Vector3(0, _verticalVelocity * Runner.DeltaTime, 0);

            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 0.2f, UnityEngine.AI.NavMesh.AllAreas))
            {
                if (Agent != null && Agent.isActiveAndEnabled && !Agent.isOnNavMesh) 
                {
                    Agent.Warp(hit.position); 
                }
                _verticalVelocity = 0f;
            }
        }

        private void AssignRandomTarget()
        {
            var sceneObjects = Runner.GetSingleton<SceneObjects>();
            if (sceneObjects == null || sceneObjects.Gameplay == null) return;
            var gameplay = sceneObjects.Gameplay;

            int playerCount = gameplay.PlayerData.Count;
            if (playerCount == 0) return;

            int randomIndex = Random.Range(0, playerCount);
            int currentIndex = 0;

            foreach (var p in gameplay.PlayerData)
            {
                if (currentIndex == randomIndex)
                {
                    _targetPlayer = p.Key;
                    break;
                }
                currentIndex++;
            }
        }

        private Player GetTargetPlayer(PlayerRef playerRef)
        {
            var netObj = Runner.GetPlayerObject(playerRef);
            if (netObj != null) return netObj.GetComponent<Player>();
            
            foreach (var p in FindObjectsOfType<Player>())
            {
                if (p.Object != null && p.Object.InputAuthority == playerRef) return p;
            }
            return null;
        }
    }
}