using UnityEngine;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using System.Threading;
using Latios;
using Latios.Transforms;
using Latios.Kinemation;
using Latios.Psyshock; 
using Unity.Entities;
using Unity.Collections;

namespace App.Combat
{
    public class CombatBootstrapper : IAsyncStartable
    {
        public async UniTask StartAsync(CancellationToken cancellation)
        {
            // 🌟 1. 获取在 CustomBootstrap 里提前造好的世界
            var world = World.DefaultGameObjectInjectionWorld as LatiosWorld;
            if (world == null) return;

            // 让主线程稍微喘息一下，等待外围传统资源（如 UI、音频容器）加载完成
            await UniTask.Delay(100, cancellationToken: cancellation); 

            // 2. 高性能手工反射扫描器（安全反射提取当前环境中所有的 ECS 系统）
            var systems = new NativeList<SystemTypeIndex>(Allocator.Temp);
            
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface) continue;

                        if (typeof(ISystem).IsAssignableFrom(type) || type.IsSubclassOf(typeof(SystemBase)))
                        {
                            var typeIndex = TypeManager.GetSystemTypeIndex(type);
                            systems.Add(typeIndex);
                        }
                    }
                }
                catch { /* 优雅忽略部分无法在运行时安全读取类型的第3方程序集 */ }
            }

            // 3. 将装甲护体（DisableAutoCreation）的正规军手动注入对应的非托管世界管线
            BootstrapTools.InjectUnitySystems(systems, world, world.initializationSystemGroup, false);
            BootstrapTools.InjectRootSuperSystems(systems, world, world.simulationSystemGroup);

            // 及时释放非托管临时内存
            systems.Dispose();

            // 4. 执行管线自排序 (统一规范：首字母小写)
            world.initializationSystemGroup.SortSystems();
            world.simulationSystemGroup.SortSystems();
            world.presentationSystemGroup.SortSystems();

            Debug.Log("<color=#00FF00>🚀 [App.Combat] 纯自研仿生避障引擎 & 割草战斗流水线已全血点火！</color>");
        }
    }
}