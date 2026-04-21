using Fusion;
using UnityEngine;

namespace SimpleFPS
{
    public class ZombieSpawner : NetworkBehaviour
    {
        public NetworkPrefabRef ZombiePrefab;
        public Transform[] SpawnPoints;
        public float SpawnInterval = 2f;
        public int MaxZombiesAlive = 20;

        [Networked] private TickTimer _spawnTimer { get; set; }

       public override void FixedUpdateNetwork()
        {
            // Chỉ Host mới có quyền đẻ quái
            if (!HasStateAuthority) return;

            var gameplay = Runner.GetSingleton<SceneObjects>().Gameplay;
            
            // Nếu không phải Zombie Mode hoặc trận đấu đã Kết thúc (Finished) thì ngừng đẻ quái
            if (!gameplay.IsZombieMode || gameplay.State == EGameplayState.Finished) return;

            // XÓA DÒNG CHECK TRẠNG THÁI RUNNING NGẶT NGHÈO, cho phép đẻ cả lúc Skirmish

            if (_spawnTimer.ExpiredOrNotRunning(Runner))
            {
                if (SpawnPoints == null || SpawnPoints.Length == 0) return;

                Transform sp = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
                Runner.Spawn(ZombiePrefab, sp.position, sp.rotation);
                
                _spawnTimer = TickTimer.CreateFromSeconds(Runner, SpawnInterval);
            }
        }
    }
}
