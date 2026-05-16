using Unity.Entities;
using Unity.Mathematics;

namespace App.Combat
{
    // 存放每一帧从手柄/键盘采集到的输入向量
    public struct PlayerInput : IComponentData
    {
        public float2 Movement;
    }

    // 玩家的经验槽与等级
    public struct PlayerExp : IComponentData
    {
        public int CurrentExp;
        public int Level;
    }
}