using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    // 经验宝石蓝图：负责把场景里的 ExpGem Prefab 转换成 ECS 掉落实体
    // 宝石本体不负责吸附与拾取，移动、吸附和销毁都由后续系统统一接管
    public class ExpGemAuthoring : MonoBehaviour
    {
        [Header("经验宝石参数")]
        public int ExpValue = 1;

        public class ExpGemBaker : Baker<ExpGemAuthoring>
        {
            public override void Bake(ExpGemAuthoring authoring)
            {
                // 经验宝石需要动态变换能力，才能在世界中拥有位置和朝向
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // 注入掉落物基因：经验值、初始磁化状态
                AddComponent(entity, new ExpGem
                {
                    ExpValue = authoring.ExpValue,
                    IsMagnetized = false
                });
            }
        }
    }
}