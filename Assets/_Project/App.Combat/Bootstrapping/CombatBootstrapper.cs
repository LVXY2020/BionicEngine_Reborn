using System.Threading;
using Cysharp.Threading.Tasks;
using Latios;
using Unity.Entities;
using UnityEngine;
using VContainer.Unity;

namespace App.Combat
{
    public class CombatBootstrapper : IAsyncStartable
    {
        public async UniTask StartAsync(CancellationToken cancellation)
        {
            var world = World.DefaultGameObjectInjectionWorld as LatiosWorld;
            if (world == null)
            {
                Debug.LogWarning("[App.Combat] LatiosWorld 尚未就绪，本次启动跳过。");
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellation);

            // 注意：这里与 CombatCustomBootstrap 存在“兜底挂接”重叠。
            // 设计建议：后续二选一保留单一挂接入口，避免未来重复 AddSystemToUpdateList。
            var initGroup = world.GetExistingSystemManaged<InitializationSystemGroup>();
            var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();

            var inputSystem = world.GetOrCreateSystem<PlayerInputSystem>();
            initGroup.AddSystemToUpdateList(inputSystem);

            var combatRoot = world.GetOrCreateSystemManaged<CombatSimulationSuperSystem>();
            simGroup.AddSystemToUpdateList(combatRoot);

            world.initializationSystemGroup.SortSystems();
            world.simulationSystemGroup.SortSystems();
            world.presentationSystemGroup.SortSystems();

            Debug.Log("[App.Combat] Combat world initialized and core systems attached.");
        }
    }
}
