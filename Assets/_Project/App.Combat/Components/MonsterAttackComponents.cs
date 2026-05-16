using Unity.Entities;

namespace App.Combat
{
    // 1. 怪物攻击属性
    public struct MonsterAttackStats : IComponentData
    {
        public float Damage;
        public float AttackRangeSq; // 架构师Tip：用距离的平方做比较，省去极其消耗性能的开平方(Sqrt)运算！
        public float AttackCooldown;
    }

    // 2. 怪物攻击计时器
    public struct AttackTimer : IComponentData
    {
        public float Value;
    }

    // 3. 玩家身份标签 (如果之前没有的话)
    //public struct PlayerTag : IComponentData { }

    // 4. 伤害事件 (用于解耦，防止多线程同时扣血的竞态问题)
    public struct DamageEvent : IComponentData
    {
        public Entity Target;
        public float Amount;
    }
}