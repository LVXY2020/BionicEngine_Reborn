using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;

namespace App.Combat
{
    // 在开火之后，伤害判定之前执行
    // 🌟 核心防线：彻底断绝 Unity  vanilla 默认世界的盲目自动实例化
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WeaponFiringSystem))]
    [UpdateBefore(typeof(DamageCollisionSystem))]
    [BurstCompile]
    public partial struct BulletMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new MoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref LocalTransform transform, in BulletMovement bullet)
        {
            // 纯物理位移：坐标 = 坐标 + 速度向量 * 时间增量
            transform.Position += bullet.Velocity * DeltaTime;
        }
    }
}