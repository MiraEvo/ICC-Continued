using System;
using System.ComponentModel;
using System.Timers;

namespace Ink_Canvas.Helpers
{
    public class DelayAction : IDisposable
    {
        private System.Timers.Timer _timerDebounce;
        private Action? _pendingAction;
        private ISynchronizeInvoke? _syncInvoke;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        /// <summary>
        /// 防抖函式 - 优化版本，重用 Timer 对象
        /// </summary>
        /// <param name="inv">同步的對象，一般傳入控件，不需要可null</param>
        public void DebounceAction(int timeMs, ISynchronizeInvoke inv, Action action)
        {
            if (_disposed) return;
            
            lock (_lockObject) {
                _pendingAction = action;
                _syncInvoke = inv;
                
                if (_timerDebounce == null) {
                    _timerDebounce = new System.Timers.Timer(timeMs) { AutoReset = false };
                    _timerDebounce.Elapsed += OnTimerElapsed;
                } else {
                    _timerDebounce.Interval = timeMs;
                }
                
                _timerDebounce.Stop();
                _timerDebounce.Start();
            }
        }
        
        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            Action actionToExecute;
            ISynchronizeInvoke syncContext;
            
            lock (_lockObject) {
                actionToExecute = _pendingAction;
                syncContext = _syncInvoke;
                _pendingAction = null;
                _syncInvoke = null;
            }
            
            if (actionToExecute != null) {
                InvokeAction(actionToExecute, syncContext);
            }
        }

        private static void InvokeAction(Action action, ISynchronizeInvoke inv)
        {
            if (inv == null)
            {
                action();
            }
            else
            {
                if (inv.InvokeRequired)
                {
                    inv.Invoke(action, null);
                }
                else
                {
                    action();
                }
            }
        }
        
        /// <summary>
        /// 取消待执行的防抖动作
        /// </summary>
        public void Cancel()
        {
            lock (_lockObject) {
                _timerDebounce?.Stop();
                _pendingAction = null;
                _syncInvoke = null;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            if (disposing) {
                lock (_lockObject) {
                    if (_timerDebounce != null) {
                        _timerDebounce.Stop();
                        _timerDebounce.Elapsed -= OnTimerElapsed;
                        _timerDebounce.Dispose();
                        _timerDebounce = null;
                    }
                    _pendingAction = null;
                    _syncInvoke = null;
                }
            }
            
            _disposed = true;
        }
        
        ~DelayAction()
        {
            Dispose(false);
        }
    }
}
