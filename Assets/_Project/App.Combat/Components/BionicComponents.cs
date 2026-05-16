using Unity.Entities;
using Unity.Mathematics;

namespace App.Combat
{
    // 1. 机能基因：肉体的物理上限与质量等级
    public struct BionicLocomotion : IComponentData
    {
        public float MaxSpeed;      // 最高速度上限
        public float Acceleration;  // 加速度 (用于起步和抗鬼畜平滑过渡)
        public float Mass;          // 质量霸权 (主角1000，精英怪10，小怪1)
    }

    // 2. 意图基因：AI 大脑的决策结果
    public struct BionicSteering : IComponentData
    {
        public float3 TargetPosition;  // 战略终点 (如玩家的当前坐标)
        public float3 DesiredVelocity; // 期望速度 (融合了追击与避障排斥力后的最终意图向量)
    }

    // 3. 躯体基因：真实的物理运动状态
    public struct BionicBody : IComponentData
    {
        public float3 Velocity; // 当前真实运动的瞬时速度向量 (用于计算 RVO 同向免责)
    }

    // 4. 碰撞基因：用于接入 Latios Psyshock 雷达网的物理体积
    public struct BionicShape : IComponentData
    {
        public float Radius; // 实体的碰撞检测半径
    }
}