using Unity.Entities;
using Latios;
using Latios.Systems;

namespace App.Combat
{
    // 强制接管核心模拟组
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CombatSimulationSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            // ==========================================
            // 阶段一：世界降临
            // ==========================================
            GetOrCreateAndAddUnmanagedSystem<SpawnerSystem>();

            // ==========================================
            // 阶段二：仿生导航与绝对位移 (Bionic Engine)
            // ==========================================
            // 1. 扫描全场，建立物理空间哈希雷达网
            GetOrCreateAndAddUnmanagedSystem<BionicCollisionLayerSystem>();
            // 2. 指挥官动态索敌，下达追击目标
            GetOrCreateAndAddUnmanagedSystem<CommanderSystem>();
            // 3. 仿生大脑利用雷达网，计算排斥与同向免责的理想意图
            GetOrCreateAndAddUnmanagedSystem<BionicAvoidanceSystem>();
            // 4. 主角无视推挤，进行绝对空间位移
            GetOrCreateAndAddUnmanagedSystem<PlayerMoveSystem>();
            // 5. 怪物肌肉执行，将意图转化为平滑位移与转向
            GetOrCreateAndAddUnmanagedSystem<BionicIntegrationSystem>();

            // ==========================================
            // 阶段三：自动化重火力网
            // ==========================================
            // 1. 武器向物理雷达网发起极速索敌查询
            GetOrCreateAndAddUnmanagedSystem<WeaponTargetingSystem>();
            // 2. 枪械开火，在内存中瞬间爆破出子弹实体
            GetOrCreateAndAddUnmanagedSystem<WeaponFiringSystem>();
            // 3. 赋予子弹动能，在空间中狂飙
            GetOrCreateAndAddUnmanagedSystem<BulletMovementSystem>();

            // ==========================================
            // 阶段四：死神法庭与战利品收割
            // ==========================================
            // 1. 射线防穿透检测，单线程安全结算血量并爆出宝石
            GetOrCreateAndAddUnmanagedSystem<DamageCollisionSystem>();
            // 2. 玩家引力场激活，宝石追踪并安全累加经验
            GetOrCreateAndAddUnmanagedSystem<GemMagnetSystem>();
        }
    }
}