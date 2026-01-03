using Microsoft.Extensions.DependencyInjection;
using System;

namespace Ink_Canvas.Core
{
    /// <summary>
    /// 服务定位器，提供全局访问依赖注入容器的能力
    /// 用于在无法使用构造函数注入的场景中获取服务
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider _serviceProvider;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取或设置服务提供者
        /// </summary>
        public static IServiceProvider ServiceProvider
        {
            get => _serviceProvider;
            set
            {
                lock (_lock)
                {
                    _serviceProvider = value ?? throw new ArgumentNullException(nameof(value));
                }
            }
        }

        /// <summary>
        /// 检查服务定位器是否已初始化
        /// </summary>
        public static bool IsInitialized => _serviceProvider != null;

        /// <summary>
        /// 获取指定类型的服务，如果不存在则返回 null
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例或 null</returns>
        public static T GetService<T>() where T : class
        {
            EnsureInitialized();
            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// 获取指定类型的服务，如果不存在则抛出异常
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        /// <exception cref="InvalidOperationException">服务未注册时抛出</exception>
        public static T GetRequiredService<T>() where T : class
        {
            EnsureInitialized();
            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// 获取指定类型的服务，如果不存在则返回 null
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <returns>服务实例或 null</returns>
        public static object GetService(Type serviceType)
        {
            EnsureInitialized();
            return _serviceProvider.GetService(serviceType);
        }

        /// <summary>
        /// 获取指定类型的服务，如果不存在则抛出异常
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <returns>服务实例</returns>
        /// <exception cref="InvalidOperationException">服务未注册时抛出</exception>
        public static object GetRequiredService(Type serviceType)
        {
            EnsureInitialized();
            return _serviceProvider.GetRequiredService(serviceType);
        }

        /// <summary>
        /// 创建一个新的作用域
        /// </summary>
        /// <returns>新的服务作用域</returns>
        public static IServiceScope CreateScope()
        {
            EnsureInitialized();
            return _serviceProvider.CreateScope();
        }

        /// <summary>
        /// 确保服务定位器已初始化
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException(
                    "ServiceLocator has not been initialized. " +
                    "Please set ServiceLocator.ServiceProvider before using it.");
            }
        }

        /// <summary>
        /// 重置服务定位器（主要用于测试）
        /// </summary>
        internal static void Reset()
        {
            lock (_lock)
            {
                _serviceProvider = null;
            }
        }
    }
}