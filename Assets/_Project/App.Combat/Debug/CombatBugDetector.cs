using System.Text;
using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    // 战斗框架自检器：用于在编辑器和运行时快速定位“世界已创建但链路没接通”的问题。
    // 它只负责观测与报警，不修复任何状态，避免干扰正式模拟。
    public class CombatBugDetector : MonoBehaviour
    {
        [Header("检测节奏")]
        [Tooltip("是否在启动时立刻输出一次完整检测结果。")]
        public bool LogOnceOnStart = true;

        [Tooltip("重复检测间隔（秒）。")]
        public float RepeatInterval = 2f;

        [Tooltip("成功后是否停止继续检测。")]
        public bool StopAfterSuccess = true;

        [Header("检测开关")]
        [Tooltip("检测场景启动入口是否存在。")]
        public bool CheckDefaultWorld = true;

        [Tooltip("检测 ECS 关键 singleton / 组件是否存在。")]
        public bool CheckEcsChain = true;

        [Tooltip("检测战斗核心实体数量是否合理。")]
        public bool CheckEntityCounts = true;

        [Header("细致诊断")]
        [Tooltip("检测 Authoring/Baker 链是否真的烘焙出实体。")]
        public bool CheckBakerOutputs = true;

        [Tooltip("检测场景入口配置是否齐全（例如 LevelAuthoring / RootLifetimeScope）。")]
        public bool CheckSceneEntry = true;

        [Tooltip("检测战斗系统组是否至少进入了当前 World。")]
        public bool CheckSystemPresence = true;

        private World _world;
        private EntityManager _entityManager;
        private bool _worldReady;
        private bool _passed;
        private float _nextLogTime;

        private void Update()
        {
            if (!_worldReady)
            {
                _world = World.DefaultGameObjectInjectionWorld;
                if (_world == null || !_world.IsCreated)
                {
                    return;
                }

                _entityManager = _world.EntityManager;
                _worldReady = true;
                _nextLogTime = Time.realtimeSinceStartup;
            }

            if (_passed && StopAfterSuccess)
                return;

            if (!LogOnceOnStart && Time.realtimeSinceStartup < _nextLogTime)
                return;

            ProbeOnce();
            _nextLogTime = Time.realtimeSinceStartup + Mathf.Max(0.1f, RepeatInterval);
        }

        private void ProbeOnce()
        {
            var sb = new StringBuilder(1024);
            bool ok = true;

            sb.AppendLine("[CombatBugDetector] ===== Combat Framework Health Check =====");

            if (CheckDefaultWorld)
            {
                var defaultWorld = World.DefaultGameObjectInjectionWorld;
                bool worldOk = defaultWorld != null && defaultWorld.IsCreated;
                sb.Append("World").Append(worldOk ? " OK" : " FAIL");
                if (defaultWorld != null)
                    sb.Append(" | Name=").Append(defaultWorld.Name);
                sb.AppendLine();
                ok &= worldOk;
            }

            if (CheckSceneEntry)
            {
                AppendCheck(sb, "Scene: RootLifetimeScope", HasSceneObjectByName("RootScope"), ref ok);
                AppendCheck(sb, "Scene: LevelAuthoring", HasSceneObjectWithComponent<LevelAuthoring>(), ref ok);
                AppendCheck(sb, "Scene: CombatLifetimeScope", HasSceneObjectByName("CombatLifetimeScope"), ref ok);
            }

            if (CheckEcsChain)
            {
                bool hasLevelConfig = HasSingleton<GlobalLevelConfig>();
                bool hasPlayer = CountEntities<PlayerTag>() > 0 && CountEntities<PlayerExp>() > 0;
                bool hasMonster = CountEntities<MonsterTag>() > 0;
                bool hasWeapon = CountEntities<WeaponStats, WeaponTarget>() > 0;
                bool hasBulletMotion = CountEntities<BulletMovement>() > 0;
                bool hasExpGem = CountEntities<ExpGem>() > 0;
                bool hasSpawner = HasSingleton<SpawnerConfig>();
                bool hasCollisionLayer = HasSingleton<BionicCollisionLayerSystem.Singleton>();

                AppendCheck(sb, "GlobalLevelConfig", hasLevelConfig, ref ok);
                AppendCheck(sb, "Player entities", hasPlayer, ref ok);
                AppendCheck(sb, "Monster entities", hasMonster, ref ok);
                AppendCheck(sb, "Weapon entities", hasWeapon, ref ok);
                AppendCheck(sb, "BulletMovement", hasBulletMotion, ref ok);
                AppendCheck(sb, "ExpGem", hasExpGem, ref ok);
                AppendCheck(sb, "SpawnerConfig", hasSpawner, ref ok);
                AppendCheck(sb, "CollisionLayer singleton", hasCollisionLayer, ref ok);
            }

            if (CheckBakerOutputs)
            {
                bool playerBaked = HasAnyEntityWithComponents<PlayerTag, PlayerExp, BionicLocomotion>();
                bool monsterBaked = HasAnyEntityWithComponents<MonsterTag, Health, BionicLocomotion>();
                bool weaponBaked = HasAnyEntityWithComponents<WeaponStats, WeaponTarget>();
                bool bulletBaked = HasAnyEntityWithComponents<BulletMovement>();
                bool gemBaked = HasAnyEntityWithComponents<ExpGem>();
                bool levelBaked = HasSingleton<GlobalLevelConfig>();

                AppendCheck(sb, "Baker: PlayerPrefab", playerBaked, ref ok);
                AppendCheck(sb, "Baker: MonsterPrefab", monsterBaked, ref ok);
                AppendCheck(sb, "Baker: WeaponPrefab", weaponBaked, ref ok);
                AppendCheck(sb, "Baker: BulletPrefab", bulletBaked, ref ok);
                AppendCheck(sb, "Baker: ExpGemPrefab", gemBaked, ref ok);
                AppendCheck(sb, "Baker: LevelAuthoring", levelBaked, ref ok);
            }

            if (CheckSystemPresence)
            {
                bool systemGroupPresent = _world != null && _world.IsCreated && _world.GetExistingSystemManaged<CombatSimulationSuperSystem>() != null;
                AppendCheck(sb, "System presence (heuristic)", systemGroupPresent, ref ok);
            }

            if (CheckEntityCounts)
            {
                int playerCount = CountEntities<PlayerTag>();
                int monsterCount = CountEntities<MonsterTag>();
                int weaponCount = CountEntities<WeaponStats, WeaponTarget>();
                int bulletCount = CountEntities<BulletMovement>();
                int gemCount = CountEntities<ExpGem>();

                sb.AppendLine($"Counts | PlayerTag={playerCount} | MonsterTag={monsterCount} | Weapon={weaponCount} | Bullet={bulletCount} | ExpGem={gemCount}");
            }

            if (ok)
            {
                _passed = true;
                sb.AppendLine("[CombatBugDetector] PASS: Core combat chain looks alive.");
            }
            else
            {
                sb.AppendLine("[CombatBugDetector] FAIL: One or more critical links are missing.");
            }

            Debug.Log(sb.ToString());
        }

        private void AppendCheck(StringBuilder sb, string label, bool passed, ref bool ok)
        {
            sb.Append(label).Append(passed ? " OK" : " FAIL").AppendLine();
            ok &= passed;
        }

        private bool HasSingleton<T>() where T : unmanaged, IComponentData
        {
            if (!_worldReady || _world == null || !_world.IsCreated) return false;
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return !query.IsEmptyIgnoreFilter;
        }

        private bool HasSceneObjectByName(string objectName) => GameObject.Find(objectName) != null;

        private bool HasSceneObjectWithComponent<T>() where T : Component => Object.FindObjectOfType<T>(true) != null;

        private bool HasAnyEntityWithComponents<T1, T2, T3>()
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
            where T3 : unmanaged, IComponentData
        {
            if (!_worldReady || _world == null || !_world.IsCreated) return false;
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<T1>(), ComponentType.ReadOnly<T2>(), ComponentType.ReadOnly<T3>());
            return !query.IsEmptyIgnoreFilter;
        }

        private bool HasAnyEntityWithComponents<T1, T2>()
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
        {
            if (!_worldReady || _world == null || !_world.IsCreated) return false;
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<T1>(), ComponentType.ReadOnly<T2>());
            return !query.IsEmptyIgnoreFilter;
        }

        private bool HasAnyEntityWithComponents<T1>() where T1 : unmanaged, IComponentData
        {
            if (!_worldReady || _world == null || !_world.IsCreated) return false;
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<T1>());
            return !query.IsEmptyIgnoreFilter;
        }

        private int CountEntities<T>() where T : unmanaged, IComponentData
        {
            if (!_worldReady || _world == null || !_world.IsCreated) return 0;
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.CalculateEntityCount();
        }

        private int CountEntities<T1, T2>()
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
        {
            if (!_worldReady || _world == null || !_world.IsCreated) return 0;
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<T1>(), ComponentType.ReadOnly<T2>());
            return query.CalculateEntityCount();
        }
    }
}
