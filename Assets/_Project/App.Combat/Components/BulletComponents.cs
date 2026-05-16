using Unity.Entities;
using Unity.Mathematics;

namespace App.Combat
{
    // 子弹运动与伤害
    public struct BulletMovement : IComponentData
    {
        public float3 Velocity; // 飞行向量
        public float Damage;    // 造成的伤害
    }
}