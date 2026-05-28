using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

namespace App.Combat
{
    // 指挥官层：负责给怪物统一下达“去追玩家”的战术意图
    // 这里不直接改位移，只写 Steering，真正执行交给后续整合系统
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(BionicAvoidanceSystem))]
    [BurstCompile]
    public partial struct CommanderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 只有场上存在玩家时，怪物追击指挥才有意义
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 没有玩家就不下发任何追击指令
            if (!SystemAPI.TryGetSingletonEntity<PlayerTag>(out var playerEntity)) return;

            // 读取玩家当前世界位置，作为所有怪物的追击目标
            // 这一步只负责“下命令”，不负责“执行移动”
            float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

            // 先统一下达战术意图，再由 BionicAvoidanceSystem / BionicIntegrationSystem 落地执行
            new CommandJob
            {
                PlayerPos = playerPos
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct CommandJob : IJobEntity
    {
        // 本帧玩家位置：所有怪物围绕这个坐标生成追击意图
        public float3 PlayerPos;

        public void Execute(ref BionicSteering steering, in LocalTransform transform, in MonsterTag tag)
        {
            // 计算怪物朝玩家的方向，并写入期望速度
            // 这里不处理避障，只做最小可玩版本的“向玩家推进”
            float3 toPlayer = PlayerPos - transform.Position;
            steering.TargetPosition = PlayerPos;
            steering.DesiredVelocity = math.normalizesafe(toPlayer) * 3f;
        }
    }
}