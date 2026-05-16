using Unity.Entities;

namespace App.Combat
{
    // 生命值基因
    public struct Health : IComponentData
    {
        public float Current;
        public float Max;
    }
}