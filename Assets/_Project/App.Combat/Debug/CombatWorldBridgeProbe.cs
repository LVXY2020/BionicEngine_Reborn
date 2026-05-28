using System.Text;
using UnityEngine;
using Unity.Entities;

namespace App.Combat
{
    // 世界桥接探针：用于确认“场景 -> World -> Entity”这条链是否真的连通
    // 它会同时告诉你：默认世界是否存在、世界里有多少关键实体、以及是否出现了多个世界
    public class CombatWorldBridgeProbe : MonoBehaviour
    {
        [Header("日志节奏")]
        public bool LogOnceOnStart = true;
        public float RepeatInterval = 2f;

        [Header("成功后是否停止")]
        public bool StopAfterSuccess = true;

        private bool _worldReady;
        private bool _loggedSuccess;
        private float _nextLogTime;
        private EntityManager _entityManager;

        private void Update()
        {
            if (!_worldReady)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;

                _entityManager = world.EntityManager;
                _worldReady = true;
                _nextLogTime = Time.realtimeSinceStartup;
            }

            if (StopAfterSuccess && _loggedSuccess) return;
            if (!LogOnceOnStart && Time.realtimeSinceStartup < _nextLogTime) return;

            ProbeOnce();
            _nextLogTime = Time.realtimeSinceStartup + Mathf.Max(0.1f, RepeatInterval);
        }

        private void ProbeOnce()
        {
            var sb = new StringBuilder(512);

            // 1. 先确认默认世界是否存在
            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            sb.Append("[CombatWorldBridgeProbe] ");
            sb.Append("DefaultWorld=");
            sb.Append(defaultWorld != null && defaultWorld.IsCreated ? defaultWorld.Name : "<null>");
            sb.Append(" | ");

            // 2. 输出当前项目里所有已创建世界的数量，帮助你确认是否存在多世界干扰
            int worldCount = 0;
            foreach (var world in World.All)
            {
                if (world != null && world.IsCreated) worldCount++;
            }
            sb.Append("WorldCount=");
            sb.Append(worldCount);
            sb.Append(" | ");

            // 3. 统计关键组件数量，直接看链路是否进 ECS
            int playerTagCount = CountEntities<PlayerTag>();
            int playerExpCount = CountEntities<PlayerExp>();
            int monsterTagCount = CountEntities<MonsterTag>();
            int weaponCount = CountEntities<WeaponStats, WeaponTarget>();
            int bulletCount = CountEntities<BulletMovement>();
            int expGemCount = CountEntities<ExpGem>();

            sb.Append("PlayerTag=").Append(playerTagCount).Append(" | ");
            sb.Append("PlayerExp=").Append(playerExpCount).Append(" | ");
            sb.Append("MonsterTag=").Append(monsterTagCount).Append(" | ");
            sb.Append("Weapon=").Append(weaponCount).Append(" | ");
            sb.Append("Bullet=").Append(bulletCount).Append(" | ");
            sb.Append("ExpGem=").Append(expGemCount);

            Debug.Log(sb.ToString());

            // 4. 如果核心链条已经出现实体，就标记成功
            if (playerTagCount > 0 && playerExpCount > 0 && monsterTagCount > 0 && weaponCount > 0)
            {
                _loggedSuccess = true;
                Debug.Log("[CombatWorldBridgeProbe] Core ECS chain detected. Scene -> World -> Entity bridge is alive.");
            }
        }

        private int CountEntities<T>() where T : unmanaged, IComponentData
        {
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.CalculateEntityCount();
        }

        private int CountEntities<T1, T2>()
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
        {
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<T1>(), ComponentType.ReadOnly<T2>());
            return query.CalculateEntityCount();
        }
    }
}