using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Collections;

namespace App.Combat
{
    // 强制在避障之后、开火之前执行
    // 🌟 核心防线：彻底断绝 Unity  vanilla 默认世界的盲目自动实例化
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BionicIntegrationSystem))]
    [UpdateBefore(typeof(WeaponFiringSystem))]
    [BurstCompile]
    public partial struct WeaponTargetingSystem : ISystem
    {
        private ComponentLookup<WorldTransform> _transformLookup;
        private ComponentLookup<MonsterTag> _monsterLookup; // 🌟 身份验证雷达

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<WorldTransform>(true);
            _monsterLookup = state.GetComponentLookup<MonsterTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _monsterLookup.Update(ref state);

            // 获取全局物理雷达网
            if (!SystemAPI.TryGetSingletonEntity<BionicCollisionLayerSystem.Singleton>(out var layerEntity)) return;
            var layer = SystemAPI.GetComponent<BionicCollisionLayerSystem.Singleton>(layerEntity).Layer;

            new TargetingJob
            {
                Layer = layer,
                TransformLookup = _transformLookup,
                MonsterLookup = _monsterLookup
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct TargetingJob : IJobEntity
    {
        [ReadOnly] public CollisionLayer Layer;
        [ReadOnly] public ComponentLookup<WorldTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<MonsterTag> MonsterLookup;

        public void Execute(ref WeaponTarget target, in WeaponStats stats, in WorldTransform weaponTransform)
        {
            float3 weaponPos = weaponTransform.position;
            float maxRangeSq = stats.AttackRange * stats.AttackRange;

            // 🌟 修复暗雷：加上 MonsterLookup 的双重身份验证，彻底杜绝“借尸还魂”锁定地上的经验宝石！
            if (target.TargetEntity != Entity.Null && 
                TransformLookup.HasComponent(target.TargetEntity) && 
                MonsterLookup.HasComponent(target.TargetEntity))
            {
                float3 targetPos = TransformLookup[target.TargetEntity].position;
                if (math.distancesq(weaponPos, targetPos) <= maxRangeSq)
                {
                    // 旧目标活着、是真正的怪物、且在射程内，保持锁定，直接收工！
                    return; 
                }
            }

            // 2. 搜寻新目标：利用 Psyshock 雷达网进行极速 AABB 查询
            Aabb searchAabb = new Aabb(weaponPos - stats.AttackRange, weaponPos + stats.AttackRange);
            
            var processor = new TargetProcessor
            {
                WeaponPos = weaponPos,
                MaxDistSq = maxRangeSq,
                ClosestEntity = Entity.Null,
                TransformLookup = TransformLookup,
                MonsterLookup = MonsterLookup
            };

            Physics.FindObjects(searchAabb, Layer, processor);

            // 3. 记忆新目标
            target.TargetEntity = processor.ClosestEntity;
        }
    }

    public struct TargetProcessor : IFindObjectsProcessor
    {
        public float3 WeaponPos;
        public float MaxDistSq;
        public Entity ClosestEntity;
        
        public ComponentLookup<WorldTransform> TransformLookup;
        public ComponentLookup<MonsterTag> MonsterLookup;

        public void Execute(in FindObjectsResult result)
        {
            Entity foundEntity = result.entity;

            // 🌟 绝对防线：过滤掉玩家！只锁定带 MonsterTag 的实体
            if (!MonsterLookup.HasComponent(foundEntity)) return;

            float3 pos = TransformLookup[foundEntity].position;
            float distSq = math.distancesq(WeaponPos, pos);

            // 寻找距离最近的合法怪物
            if (distSq < MaxDistSq)
            {
                MaxDistSq = distSq;
                ClosestEntity = foundEntity;
            }
        }
    }
}