using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Latios.Transforms;
using LocalTransform = Unity.Transforms.LocalTransform;

namespace App.Combat
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WeaponTargetingSystem))]
    [UpdateBefore(typeof(BulletMovementSystem))]
    [BurstCompile]
    public partial struct WeaponFiringSystem : ISystem
    {
        private ComponentLookup<WorldTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<WorldTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);

            // 🌟 这里暂时保持单帧结构性变更，后面如果数量继续膨胀，再换成更严格的 ECB 流程
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var fireHandle = new FireJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ECB = ecb,
                TransformLookup = _transformLookup
            }.Schedule(state.Dependency);

            // FireJob 会写 ECB，必须先 Complete 再 Playback，避免并发写入异常。
            fireHandle.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    public partial struct FireJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer ECB;
        [ReadOnly] public ComponentLookup<WorldTransform> TransformLookup;

        public void Execute(Entity entity, ref WeaponStats stats, ref WeaponTarget target, in WorldTransform weaponTransform)
        {
            // 🌟 先扣冷却，没到点就不发射
            stats.CurrentTimer -= DeltaTime;
            if (stats.CurrentTimer > 0f) return;
            if (target.TargetEntity == Entity.Null) return;
            if (!TransformLookup.HasComponent(target.TargetEntity)) return;

            var bulletEntity = ECB.Instantiate(stats.BulletPrefab);

            float3 weaponPos = weaponTransform.position;
            float3 targetPos = TransformLookup[target.TargetEntity].position;
            float3 fireDir = math.normalizesafe(targetPos - weaponPos, math.forward());

            // 🌟 子弹出生点和朝向统一从武器位置与目标方向计算，避免 prefab 自带姿态污染发射逻辑
            ECB.SetComponent(bulletEntity, LocalTransform.FromPositionRotation(weaponPos, quaternion.LookRotationSafe(fireDir, math.up())));

            // 🌟 子弹参数全部从武器数据读取，不再在系统里写死常量
            ECB.SetComponent(bulletEntity, new BulletMovement
            {
                Velocity = fireDir * stats.BulletSpeed,
                Damage = stats.BulletDamage
            });

            // 🌟 冷却时间重置为武器配置值，保证调参只改 Authoring 就能生效
            stats.CurrentTimer = stats.FireRate;
        }
    }
}
