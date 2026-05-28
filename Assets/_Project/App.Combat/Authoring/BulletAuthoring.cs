using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    // 子弹蓝图：负责把场景里的 Bullet Prefab 转换成 ECS 子弹实体
    // 子弹本体不直接处理逻辑，飞行、命中和销毁都交给后续系统统一接管
    public class BulletAuthoring : MonoBehaviour
    {
        [Header("子弹参数")]
        public float Speed = 20f;
        public float Damage = 10f;

        public class BulletBaker : Baker<BulletAuthoring>
        {
            public override void Bake(BulletAuthoring authoring)
            {
                // 子弹需要动态变换能力，才能在世界中拥有位置和朝向
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // 注入子弹运动与伤害基因
                // 速度先给一个默认值，真正发射时由 WeaponFiringSystem 根据目标方向重写
                AddComponent(entity, new BulletMovement
                {
                    Velocity = Unity.Mathematics.float3.zero,
                    Damage = authoring.Damage
                });
            }
        }
    }
}