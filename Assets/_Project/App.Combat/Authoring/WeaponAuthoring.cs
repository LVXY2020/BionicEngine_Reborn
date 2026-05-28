using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    // 武器蓝图：负责把场景里的武器对象转换成 ECS 武器实体
    // 武器本体不直接开火，真正的发射逻辑由 WeaponFiringSystem 统一接管
    public class WeaponAuthoring : MonoBehaviour
    {
        [Header("武器参数")]
        public float Range = 10f;
        public float FireRate = 0.5f;
        public float BulletSpeed = 20f;
        public float BulletDamage = 10f;
        
        [Header("弹药配置")]
        public GameObject BulletPrefab; // 拖入子弹预制体

        public class WeaponBaker : Baker<WeaponAuthoring>
        {
            public override void Bake(WeaponAuthoring authoring)
            {
                // 武器需要动态变换能力，以便在世界中拥有位置和朝向
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // 将子弹预制体降维为 ECS 实体蓝图
                var bulletEntity = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic);

                // 注入武器基础参数：射程、射速、弹速、伤害与子弹蓝图
                AddComponent(entity, new WeaponStats
                {
                    AttackRange = authoring.Range,
                    FireRate = authoring.FireRate,
                    CurrentTimer = 0f,
                    BulletSpeed = authoring.BulletSpeed,
                    BulletDamage = authoring.BulletDamage,
                    BulletPrefab = bulletEntity
                });

                // 初始化索敌记忆，开局默认没有锁定任何目标
                AddComponent(entity, new WeaponTarget { TargetEntity = Entity.Null });
            }
        }
    }
}