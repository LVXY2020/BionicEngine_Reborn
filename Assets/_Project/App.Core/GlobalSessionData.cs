using R3;
using System;

namespace App.Core
{
    // 不继承 MonoBehaviour！
    public class GlobalSessionData : IDisposable
    {
        public ReactiveProperty<int> Coins { get; } = new(0);

        public void AddCoins(int amount)
        {
            Coins.Value += amount;
        }

        public void Dispose()
        {
            Coins.Dispose();
        }
    }
}