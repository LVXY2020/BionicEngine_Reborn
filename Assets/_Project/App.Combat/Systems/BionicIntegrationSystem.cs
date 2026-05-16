using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

namespace App.Combat
{
    [BurstCompile]
    public partial struct BionicIntegrationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new IntegrationJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct IntegrationJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref LocalTransform transform, ref BionicBody body, in BionicSteering steering, in BionicLocomotion loco)
        {
            // 1. 动能转化：模拟真实的物理加速度，让怪物启动和推挤时更加平滑
            body.Velocity = math.lerp(body.Velocity, steering.DesiredVelocity, loco.Acceleration * DeltaTime);

            // 🌟 修复暗雷 2：强制抹除 Y 轴速度，防止受力叠加产生浮点起飞现象
            body.Velocity.y = 0f;

            // 2. 绝对限速：无论推挤力多大，绝对不能超过肉体设定的极限速度
            float currentSpeedSq = math.lengthsq(body.Velocity);
            float maxSpeedSq = loco.MaxSpeed * loco.MaxSpeed;
            if (currentSpeedSq > maxSpeedSq)
            {
                body.Velocity = math.normalize(body.Velocity) * loco.MaxSpeed;
            }

            // 3. 最终落位：修改实体的本地坐标
            transform.Position += body.Velocity * DeltaTime;
            
            // 🌟 修复暗雷 2 (双重保险)：物理层面绝对锁死高度为 0，防止地形或其他因素造成的漂移
            transform.Position.y = 0f;
            
            // 4. 顺滑转向
            if (currentSpeedSq > 0.01f)
            {
                quaternion targetRotation = quaternion.LookRotationSafe(body.Velocity, math.up());
                transform.Rotation = math.slerp(transform.Rotation, targetRotation, DeltaTime * 10f);
            }
        }
    }
}