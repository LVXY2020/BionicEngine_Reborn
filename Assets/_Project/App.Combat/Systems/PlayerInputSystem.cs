using Unity.Entities;
using Unity.Mathematics;
using UnityEngine; 

namespace App.Combat
{
    // 强制声明在生命周期的最早期执行，确保逻辑帧拿到的是最新按键
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PlayerInputSystem : ISystem
    {
        // 绝对铁律：包含 UnityEngine.Input 托管代码，绝不可加 [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 1. 采集原生轴向输入
            float2 rawInput = new float2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            // 2. 终极防线：向量归一化。防止斜向移动速度超标 (sqrt(2) bug)
            float2 normalizedInput = float2.zero;
            if (math.lengthsq(rawInput) > 0.01f)
            {
                normalizedInput = math.normalize(rawInput);
            }

            // 3. 将净化后的指令抄写入 ECS 基因
            foreach (var input in SystemAPI.Query<RefRW<PlayerInput>>())
            {
                input.ValueRW.Movement = normalizedInput;
            }
        }
    }
}