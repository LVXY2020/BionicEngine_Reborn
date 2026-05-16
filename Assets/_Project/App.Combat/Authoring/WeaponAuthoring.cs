using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    public class WeaponAuthoring : MonoBehaviour
    {
        [Header("武器参数")]
        public float Range = 10f;
        public float FireRate = 0.5f;
        
        [Header("弹药配置")]
        public GameObject BulletPrefab; // 拖入子弹预制体

        public class WeaponBaker : Baker<WeaponAuthoring>
        {
            public override void Bake(WeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // 转换子弹图纸
                var bulletEntity = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic);

                // 注入武器基因
                AddComponent(entity, new WeaponStats
                {
                    AttackRange = authoring.Range,
                    FireRate = authoring.FireRate,
                    CurrentTimer = 0f,
                    BulletPrefab = bulletEntity
                });

                // 初始化索敌记忆
                AddComponent(entity, new WeaponTarget { TargetEntity = Entity.Null });
            }
        }
    }
}