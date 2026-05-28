using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    // 🌟 这个脚本建议挂在 Bootstrap 场景里的全局配置对象上
    // 它的职责只有一个：把场景配置烘焙成全局 singleton，供掉落、关卡和初始化系统使用。
    public class LevelAuthoring : MonoBehaviour
    {
        [Header("战利品配置")]
        public GameObject ExpGemPrefab;

        public class LevelBaker : Baker<LevelAuthoring>
        {
            public override void Bake(LevelAuthoring authoring)
            {
                // 🌟 关卡配置本体不需要变换，只负责把全局数据烘焙成 singleton
                var entity = GetEntity(TransformUsageFlags.None);

                // 🌟 把经验宝石 prefab 转成 ECS 可直接实例化的实体蓝图
                var gemEntity = GetEntity(authoring.ExpGemPrefab, TransformUsageFlags.Dynamic);

                AddComponent(entity, new GlobalLevelConfig
                {
                    ExpGemPrefab = gemEntity
                });
            }
        }
    }
}
