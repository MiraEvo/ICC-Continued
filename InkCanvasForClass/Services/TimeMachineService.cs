using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// TimeMachine 服务实现 - 包装现有的 TimeMachine 类
    /// </summary>
    public class TimeMachineService : ITimeMachineService
    {
        private readonly TimeMachine _timeMachine;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TimeMachineService()
        {
            _timeMachine = new TimeMachine();
            
            // 将内部事件转发到服务事件
            _timeMachine.OnUndoStateChanged += status => UndoStateChanged?.Invoke(status);
            _timeMachine.OnRedoStateChanged += status => RedoStateChanged?.Invoke(status);
        }

        /// <summary>
        /// 构造函数 - 使用现有的 TimeMachine 实例
        /// </summary>
        /// <param name="existingTimeMachine">现有的 TimeMachine 实例</param>
        public TimeMachineService(TimeMachine existingTimeMachine)
        {
            _timeMachine = existingTimeMachine ?? throw new ArgumentNullException(nameof(existingTimeMachine));
            
            // 将内部事件转发到服务事件
            _timeMachine.OnUndoStateChanged += status => UndoStateChanged?.Invoke(status);
            _timeMachine.OnRedoStateChanged += status => RedoStateChanged?.Invoke(status);
        }

        #region 事件

        /// <inheritdoc />
        public event Action<bool> UndoStateChanged;

        /// <inheritdoc />
        public event Action<bool> RedoStateChanged;

        #endregion

        #region 属性

        /// <inheritdoc />
        public bool CanUndo => CurrentIndex > -1;

        /// <inheritdoc />
        public bool CanRedo => HistoryCount - CurrentIndex - 1 > 0;

        /// <inheritdoc />
        public int HistoryCount => ExportHistory()?.Length ?? 0;

        /// <inheritdoc />
        public int CurrentIndex
        {
            get
            {
                // 通过导出历史记录来获取当前索引
                var history = ExportHistory();
                return history?.Length - 1 ?? -1;
            }
        }

        /// <summary>
        /// 获取内部 TimeMachine 实例（用于兼容现有代码）
        /// </summary>
        public TimeMachine InternalTimeMachine => _timeMachine;

        #endregion

        #region 提交历史方法

        /// <inheritdoc />
        public void CommitStrokeUserInputHistory(StrokeCollection strokes)
        {
            if (strokes == null) return;
            _timeMachine.CommitStrokeUserInputHistory(strokes);
        }

        /// <inheritdoc />
        public void CommitStrokeShapeHistory(StrokeCollection strokeToBeReplaced, StrokeCollection generatedStroke)
        {
            if (generatedStroke == null) return;
            _timeMachine.CommitStrokeShapeHistory(strokeToBeReplaced, generatedStroke);
        }

        /// <inheritdoc />
        public void CommitStrokeManipulationHistory(Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary)
        {
            if (stylusPointDictionary == null || stylusPointDictionary.Count == 0) return;
            _timeMachine.CommitStrokeManipulationHistory(stylusPointDictionary);
        }

        /// <inheritdoc />
        public void CommitStrokeDrawingAttributesHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes)
        {
            if (drawingAttributes == null || drawingAttributes.Count == 0) return;
            _timeMachine.CommitStrokeDrawingAttributesHistory(drawingAttributes);
        }

        /// <inheritdoc />
        public void CommitStrokeEraseHistory(StrokeCollection strokes, StrokeCollection sourceStrokes = null)
        {
            if (strokes == null) return;
            _timeMachine.CommitStrokeEraseHistory(strokes, sourceStrokes);
        }

        #endregion

        #region 撤销/重做操作

        /// <inheritdoc />
        public TimeMachineHistory Undo()
        {
            if (!CanUndo) return null;
            return _timeMachine.Undo();
        }

        /// <inheritdoc />
        public TimeMachineHistory Redo()
        {
            if (!CanRedo) return null;
            return _timeMachine.Redo();
        }

        #endregion

        #region 历史记录管理

        /// <inheritdoc />
        public void ClearHistory()
        {
            _timeMachine.ClearStrokeHistory();
        }

        /// <inheritdoc />
        public TimeMachineHistory[] ExportHistory()
        {
            return _timeMachine.ExportTimeMachineHistory();
        }

        /// <inheritdoc />
        public bool ImportHistory(TimeMachineHistory[] history)
        {
            return _timeMachine.ImportTimeMachineHistory(history);
        }

        #endregion
    }
}