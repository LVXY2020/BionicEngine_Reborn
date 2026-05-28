using UnityEngine;
using Unity.Entities;

namespace App.Combat
{
    // 运行时自检探针：用于快速确认关键 ECS 实体有没有真正进入当前世界
    // 这个脚本只负责“看见问题”，不负责修复问题，方便你在编辑器里定位断点
    public class CombatRuntimeProbe : MonoBehaviour
    {
        [Header("自检开关")]
        public bool LogOnceOnStart = true;
        public float RepeatInterval = 2f;

        [Header("只看一次时是否在成功后停止")]
        public bool StopAfterSuccess = true;

        private EntityManager _entityManager;
        private bool _worldReady;
        private bool _loggedSuccess;
        private float _nextLogTime;

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
            int playerTagCount = CountEntities<PlayerTag>();
            int playerExpCount = CountEntities<PlayerExp>();
            int monsterTagCount = CountEntities<MonsterTag>();
            int weaponCount = CountEntities<WeaponStats, WeaponTarget>();
            int bulletCount = CountEntities<BulletMovement>();

            Debug.Log($"[CombatRuntimeProbe] PlayerTag={playerTagCount}, PlayerExp={playerExpCount}, MonsterTag={monsterTagCount}, Weapon={weaponCount}, Bullet={bulletCount}");

            if (playerTagCount > 0 && playerExpCount > 0 && monsterTagCount > 0 && weaponCount > 0)
            {
                _loggedSuccess = true;
                Debug.Log("[CombatRuntimeProbe] Core ECS entities are present. The minimal combat chain is entering a valid state.");
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