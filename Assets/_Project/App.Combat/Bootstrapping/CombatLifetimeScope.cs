using VContainer;
using VContainer.Unity;
using App.Combat; // 引入点火器所在的命名空间

public class CombatLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 🌟 核心桥接：将点火器注册为 VContainer 的异步启动入口
        // 当这个场景加载、LifetimeScope 初始化时，系统会自动触发 CombatBootstrapper 的 StartAsync()
        builder.RegisterEntryPoint<CombatBootstrapper>();
    }
}