using Unity.Entities;
using Latios;
using UnityEngine;

namespace App.Combat
{
    // 自定义启动：创建 Latios 世界，并按“游戏世界”路径补齐默认系统与 PlayerLoop。
    public class CombatCustomBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            // 1) 创建战斗世界，并作为默认注入世界。
            var world = new LatiosWorld("CombatWorld");
            World.DefaultGameObjectInjectionWorld = world;

            // 2) 关键：补齐 Default 路径系统（含 Scene/流送相关系统）。
            // 没有这一步时，常见现象是 World 存在但 SubScene 实体计数长期为 0。
            // 注意：该策略会引入更多默认系统，后续发布前建议按实际需求做系统精简与白名单化。
            var allDefaultSystems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, allDefaultSystems);

            // 3) 将世界挂入 PlayerLoop，确保真正开始更新。
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);

            // 4) 兜底挂接：战斗主链与输入系统显式入组（避免入口漏触发导致全链静默）。
            // 注意：若 CombatBootstrapper 也做同样挂接，会形成“双入口冗余”。
            // 当前阶段为了稳定可保留；后续建议统一到单一入口。
            var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
            var initGroup = world.GetExistingSystemManaged<InitializationSystemGroup>();

            var combatRoot = world.GetOrCreateSystemManaged<CombatSimulationSuperSystem>();
            simGroup.AddSystemToUpdateList(combatRoot);

            var inputSystem = world.GetOrCreateSystem<PlayerInputSystem>();
            initGroup.AddSystemToUpdateList(inputSystem);

            world.initializationSystemGroup.SortSystems();
            world.simulationSystemGroup.SortSystems();
            world.presentationSystemGroup.SortSystems();

            Debug.Log("[App.Combat] CustomBootstrap created game-path world and attached combat core systems.");
            return true;
        }
    }
}
