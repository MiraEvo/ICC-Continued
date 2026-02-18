using System;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Helpers
{
    public class TimeMachine
    {
        // 初始容量设为 64，减少动态扩容次数
        private readonly List<TimeMachineHistory> _currentStrokeHistory = new List<TimeMachineHistory>(64);

        private int _currentIndex = -1;
        
        // 缓存上次通知的状态，避免重复触发事件
        private bool _lastUndoState = false;
        private bool _lastRedoState = false;

        public delegate void OnUndoStateChange(bool status);

        public event OnUndoStateChange OnUndoStateChanged;

        public delegate void OnRedoStateChange(bool status);

        public event OnRedoStateChange OnRedoStateChanged;

        /// <summary>
        /// 清除当前索引之后的历史记录（用于在撤销后进行新操作时）
        /// </summary>
        private void TruncateHistoryAfterCurrentIndex()
        {
            int removeCount = _currentStrokeHistory.Count - _currentIndex - 1;
            if (removeCount > 0)
            {
                _currentStrokeHistory.RemoveRange(_currentIndex + 1, removeCount);
            }
        }

        public void CommitStrokeUserInputHistory(StrokeCollection stroke)
        {
            TruncateHistoryAfterCurrentIndex();
            _currentStrokeHistory.Add(new TimeMachineHistory(stroke, TimeMachineHistoryType.UserInput, false));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        public void CommitStrokeShapeHistory(StrokeCollection strokeToBeReplaced, StrokeCollection generatedStroke)
        {
            TruncateHistoryAfterCurrentIndex();
            _currentStrokeHistory.Add(new TimeMachineHistory(generatedStroke,
                TimeMachineHistoryType.ShapeRecognition,
                false,
                strokeToBeReplaced));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        public void CommitStrokeManipulationHistory(Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary)
        {
            TruncateHistoryAfterCurrentIndex();
            _currentStrokeHistory.Add(
                new TimeMachineHistory(stylusPointDictionary,
                    TimeMachineHistoryType.Manipulation));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        public void CommitStrokeDrawingAttributesHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes)
        {
            TruncateHistoryAfterCurrentIndex();
            _currentStrokeHistory.Add(
                new TimeMachineHistory(drawingAttributes,
                    TimeMachineHistoryType.DrawingAttributes));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        public void CommitStrokeEraseHistory(StrokeCollection stroke, StrokeCollection sourceStroke = null)
        {
            TruncateHistoryAfterCurrentIndex();
            _currentStrokeHistory.Add(new TimeMachineHistory(stroke, TimeMachineHistoryType.Clear, true, sourceStroke));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        public void ClearStrokeHistory()
        {
            _currentStrokeHistory.Clear();
            _currentIndex = -1;
            // 强制重置状态缓存
            _lastUndoState = true;
            _lastRedoState = true;
            NotifyUndoRedoState();
        }

        public TimeMachineHistory Undo()
        {
            var item = _currentStrokeHistory[_currentIndex];
            item.StrokeHasBeenCleared = !item.StrokeHasBeenCleared;
            _currentIndex--;
            NotifyUndoRedoState();
            return item;
        }

        public TimeMachineHistory Redo()
        {
            var item = _currentStrokeHistory[++_currentIndex];
            item.StrokeHasBeenCleared = !item.StrokeHasBeenCleared;
            NotifyUndoRedoState();
            return item;
        }

        public TimeMachineHistory[] ExportTimeMachineHistory()
        {
            TruncateHistoryAfterCurrentIndex();
            return _currentStrokeHistory.ToArray();
        }

        public bool ImportTimeMachineHistory(TimeMachineHistory[] sourceHistory)
        {
            _currentStrokeHistory.Clear();
            if (sourceHistory != null && sourceHistory.Length > 0)
            {
                // 预分配容量
                _currentStrokeHistory.Capacity = Math.Max(_currentStrokeHistory.Capacity, sourceHistory.Length);
                _currentStrokeHistory.AddRange(sourceHistory);
            }
            _currentIndex = _currentStrokeHistory.Count - 1;
            // 强制重置状态缓存
            _lastUndoState = !(_currentIndex > -1);
            _lastRedoState = true;
            NotifyUndoRedoState();
            return true;
        }

        /// <summary>
        /// 通知撤销/重做状态变化（带状态缓存优化）
        /// </summary>
        private void NotifyUndoRedoState()
        {
            bool canUndo = _currentIndex > -1;
            bool canRedo = _currentStrokeHistory.Count - _currentIndex - 1 > 0;
            
            // 只有状态发生变化时才触发事件
            if (canUndo != _lastUndoState)
            {
                _lastUndoState = canUndo;
                OnUndoStateChanged?.Invoke(canUndo);
            }
            
            if (canRedo != _lastRedoState)
            {
                _lastRedoState = canRedo;
                OnRedoStateChanged?.Invoke(canRedo);
            }
        }
    }

    public class TimeMachineHistory
    {
        public TimeMachineHistoryType CommitType;
        public bool StrokeHasBeenCleared = false;
        public StrokeCollection CurrentStroke;
        public StrokeCollection ReplacedStroke;
        //这里说一下 Tuple的 Value1 是初始值 ; Value 2 是改变值
        public Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> StylusPointDictionary;
        public Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> DrawingAttributes;
        public TimeMachineHistory(StrokeCollection currentStroke, TimeMachineHistoryType commitType, bool strokeHasBeenCleared)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            StrokeHasBeenCleared = strokeHasBeenCleared;
            ReplacedStroke = null;
        }
        public TimeMachineHistory(Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary, TimeMachineHistoryType commitType)
        {
            CommitType = commitType;
            StylusPointDictionary = stylusPointDictionary;
        }
        public TimeMachineHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes, TimeMachineHistoryType commitType)
        {
            CommitType = commitType;
            DrawingAttributes = drawingAttributes;
        }
        public TimeMachineHistory(StrokeCollection currentStroke, TimeMachineHistoryType commitType, bool strokeHasBeenCleared, StrokeCollection replacedStroke)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            StrokeHasBeenCleared = strokeHasBeenCleared;
            ReplacedStroke = replacedStroke;
        }
    }

    public enum TimeMachineHistoryType
    {
        UserInput,
        ShapeRecognition,
        Clear,
        Manipulation,
        DrawingAttributes
    }
}
