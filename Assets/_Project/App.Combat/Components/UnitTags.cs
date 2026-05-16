using Unity.Entities;

namespace App.Combat
{
    // 标识玩家
    public struct PlayerTag : IComponentData { }
    
    // 标识怪物
    public struct MonsterTag : IComponentData { }
}