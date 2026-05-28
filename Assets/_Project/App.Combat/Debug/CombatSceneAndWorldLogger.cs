using System.Text;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace App.Combat
{
    /// <summary>
    /// 深度日志器：打印场景加载状态 + World 状态 + 关键系统是否存在。
    /// 用于定位“场景对象都在，但运行时 ECS 为空”的问题。
    /// </summary>
    public class CombatSceneAndWorldLogger : MonoBehaviour
    {
        [Header("日志节奏")]
        public bool LogOnStart = true;
        public bool Repeat = true;
        public float RepeatInterval = 3f;

        private float _next;

        private void Start()
        {
            if (LogOnStart)
            {
                Dump();
            }
            _next = Time.realtimeSinceStartup + Mathf.Max(0.2f, RepeatInterval);
        }

        private void Update()
        {
            if (!Repeat) return;
            if (Time.realtimeSinceStartup < _next) return;
            Dump();
            _next = Time.realtimeSinceStartup + Mathf.Max(0.2f, RepeatInterval);
        }

        [ContextMenu("Dump Now")]
        public void Dump()
        {
            var sb = new StringBuilder(2048);
            sb.AppendLine("[CombatSceneAndWorldLogger] ===== DUMP BEGIN =====");

            // 1) 场景状态
            int loaded = SceneManager.sceneCount;
            sb.AppendLine($"Scenes Loaded: {loaded}");
            for (int i = 0; i < loaded; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                int roots = scene.IsValid() && scene.isLoaded ? scene.rootCount : 0;
                sb.AppendLine($" - Scene[{i}] name={scene.name}, loaded={scene.isLoaded}, rootCount={roots}");
            }

            // 2) 默认 World
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                sb.AppendLine("DefaultWorld: <null>");
                Debug.Log(sb.ToString());
                return;
            }

            sb.AppendLine($"DefaultWorld: {world.Name}, IsCreated={world.IsCreated}, IsGameWorld={world.Flags.HasFlag(WorldFlags.Game)}");

            var em = world.EntityManager;

            // 3) 核心系统是否存在
            bool hasInit = world.GetExistingSystemManaged<InitializationSystemGroup>() != null;
            bool hasSim = world.GetExistingSystemManaged<SimulationSystemGroup>() != null;
            bool hasPresentation = world.GetExistingSystemManaged<PresentationSystemGroup>() != null;
            bool hasCombatRoot = world.GetExistingSystemManaged<CombatSimulationSuperSystem>() != null;

            sb.AppendLine($"Systems: Init={hasInit}, Sim={hasSim}, Presentation={hasPresentation}, CombatRoot={hasCombatRoot}");

            // 4) 关键组件数量
            sb.AppendLine($"Counts: PlayerTag={Count<PlayerTag>(em)}, PlayerExp={Count<PlayerExp>(em)}, MonsterTag={Count<MonsterTag>(em)}, WeaponStats={Count<WeaponStats>(em)}, SpawnerConfig={Count<SpawnerConfig>(em)}, GlobalLevelConfig={Count<GlobalLevelConfig>(em)}");

            sb.AppendLine("[CombatSceneAndWorldLogger] ===== DUMP END =====");
            Debug.Log(sb.ToString());
        }

        private int Count<T>(EntityManager em) where T : unmanaged, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.CalculateEntityCount();
        }
    }
}
