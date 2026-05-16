using Unity.Entities;
using Latios; // 🌟 引入 Latios 命名空间

namespace App.Combat
{
    // 拔掉 Unity 原生点火器的氧气管，彻底接管世界创建权！
    public class CombatCustomBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            // 1. 在 Unity ECS 最底层初始化阶段，同步创建我们的高性能世界
            var world = new LatiosWorld("CombatWorld");

            // 2. 满足 Unity 底层的硬性要求：既然接管了，就必须给系统一个合法的注入世界
            World.DefaultGameObjectInjectionWorld = world;

            // 返回 true，完美骗过原生管线，干掉 Default World！
            return true; 
        }
    }
}