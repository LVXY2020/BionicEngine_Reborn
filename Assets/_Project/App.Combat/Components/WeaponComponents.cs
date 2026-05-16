using Unity.Entities;

namespace App.Combat
{
    // 武器基础属性
    public struct WeaponStats : IComponentData
    {
        public float AttackRange;   // 攻击范围
        public float FireRate;      // 射速（秒/发）
        public float CurrentTimer;  // 冷却计时器
        public Entity BulletPrefab; // 挂载的子弹蓝图
    }

    // 武器锁定记忆（解决火力过剩漏洞）
    public struct WeaponTarget : IComponentData
    {
        public Entity TargetEntity; // 当前正在持续锁定的猎物
    }
}