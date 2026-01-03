using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Ink_Canvas.Core
{
    /// <summary>
    /// 所有 ViewModel 的基类
    /// 提供通用的属性和方法
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject, IDisposable
    {
        /// <summary>
        /// 指示 ViewModel 是否正在忙碌（执行异步操作）
        /// </summary>
        [ObservableProperty]
        private bool _isBusy;

        /// <summary>
        /// ViewModel 的标题或名称
        /// </summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>
        /// 指示 ViewModel 是否已初始化
        /// </summary>
        [ObservableProperty]
        private bool _isInitialized;

        /// <summary>
        /// 指示资源是否已释放
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// 初始化 ViewModel
        /// 子类可以重写此方法执行异步初始化逻辑
        /// </summary>
        public virtual void Initialize()
        {
            IsInitialized = true;
        }

        /// <summary>
        /// 异步初始化 ViewModel
        /// 子类可以重写此方法执行异步初始化逻辑
        /// </summary>
        public virtual System.Threading.Tasks.Task InitializeAsync()
        {
            Initialize();
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// 清理 ViewModel 资源
        /// 子类可以重写此方法执行清理逻辑
        /// </summary>
        public virtual void Cleanup()
        {
            // 默认实现为空，子类可以重写
        }

        /// <summary>
        /// 当 IsBusy 属性改变时调用
        /// </summary>
        partial void OnIsBusyChanged(bool value)
        {
            // 子类可以通过重写 OnIsBusyChanged 方法来响应忙碌状态变化
            OnBusyStateChanged(value);
        }

        /// <summary>
        /// 忙碌状态变化时的回调方法
        /// 子类可以重写此方法响应忙碌状态变化
        /// </summary>
        /// <param name="isBusy">是否忙碌</param>
        protected virtual void OnBusyStateChanged(bool isBusy)
        {
            // 默认实现为空
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实际实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Cleanup();
            }

            _disposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~ViewModelBase()
        {
            Dispose(false);
        }
    }
}