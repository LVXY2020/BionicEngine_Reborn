using Unity.Entities;

namespace App.Combat
{
    // 冰冻/硬控状态（关闭时完全不占 CPU 算力）
    public struct FrozenState : IComponentData, IEnableableComponent { }
}