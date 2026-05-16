using Unity.Entities;

namespace App.Combat
{
    // 刷怪器的数据基因 (建议放在 Components 文件夹中)
    public struct SpawnerConfig : IComponentData
    {
        public Entity MonsterPrefab;
        public int SpawnCount;
    }
}