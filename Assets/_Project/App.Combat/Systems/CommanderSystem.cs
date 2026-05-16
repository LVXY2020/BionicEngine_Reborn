using Unity.Entities;
using Unity.Burst;

namespace App.Combat
{
    [BurstCompile]
    public partial struct CommanderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) { }
    }
}