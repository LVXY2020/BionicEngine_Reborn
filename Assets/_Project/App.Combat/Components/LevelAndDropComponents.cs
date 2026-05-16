using Unity.Entities;

namespace App.Combat
{
    // 全局关卡配置：存放那些需要跨系统访问的预制体图纸
    public struct GlobalLevelConfig : IComponentData
    {
        public Entity ExpGemPrefab; // 经验宝石的实体蓝图
    }

    // 经验宝石属性
    public struct ExpGem : IComponentData
    {
        public int ExpValue;      // 经验面值
        public bool IsMagnetized; // 是否已被引力场捕获
    }
}