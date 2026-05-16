using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

namespace App.Combat
{
    [BurstCompile]
    public partial struct PlayerMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 提取全局时间增量
            float deltaTime = SystemAPI.Time.DeltaTime;

            new PlayerMoveJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct PlayerMoveJob : IJobEntity
    {
        public float DeltaTime;

        // 严格读取组件：必须包含 PlayerTag，同时操作 Transform 和 Bionic 基因
        public void Execute(in PlayerInput input, ref BionicBody body, ref LocalTransform transform, in BionicLocomotion loco, in PlayerTag tag)
        {
            // 1. 将 2D 输入指令升维至 3D 物理世界
            float3 moveDirection = new float3(input.Movement.x, 0, input.Movement.y);

            // 2. 动能结算：计算当前帧的真实物理速度
            if (math.lengthsq(moveDirection) > 0.01f)
            {
                body.Velocity = moveDirection * loco.MaxSpeed;
            }
            else
            {
                // 刹车停滞
                body.Velocity = float3.zero;
            }

            // 3. 绝对空间位移：玩家不受尸潮推挤，直接修改坐标矩阵
            transform.Position += body.Velocity * DeltaTime;

            // 4. 骨骼平滑转向：使用球面线性插值 (Slerp) 确保模型转身丝滑，不瞬拍
            if (math.lengthsq(body.Velocity) > 0.01f)
            {
                quaternion targetRotation = quaternion.LookRotationSafe(body.Velocity, math.up());
                transform.Rotation = math.slerp(transform.Rotation, targetRotation, DeltaTime * 15f);
            }
        }
    }
}