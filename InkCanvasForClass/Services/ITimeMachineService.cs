using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// TimeMachine 服务接口 - 提供撤销/重做历史记录管理
    /// </summary>
    public interface ITimeMachineService
    {
        #region 事件

        /// <summary>
        /// 撤销状态变化事件
        /// </summary>
        event Action<bool> UndoStateChanged;

        /// <summary>
        /// 重做状态变化事件
        /// </summary>
        event Action<bool> RedoStateChanged;

        #endregion

        #region 属性

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// 是否可以重做
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// 获取历史记录数量
        /// </summary>
        int HistoryCount { get; }

        /// <summary>
        /// 获取当前索引
        /// </summary>
        int CurrentIndex { get; }

        #endregion

        #region 提交历史方法

        /// <summary>
        /// 提交用户输入笔画历史
        /// </summary>
        /// <param name="strokes">笔画集合</param>
        void CommitStrokeUserInputHistory(StrokeCollection strokes);

        /// <summary>
        /// 提交形状识别历史
        /// </summary>
        /// <param name="strokeToBeReplaced">被替换的笔画</param>
        /// <param name="generatedStroke">生成的笔画</param>
        void CommitStrokeShapeHistory(StrokeCollection strokeToBeReplaced, StrokeCollection generatedStroke);

        /// <summary>
        /// 提交笔画操作历史（移动、缩放等）
        /// </summary>
        /// <param name="stylusPointDictionary">笔画点变化字典</param>
        void CommitStrokeManipulationHistory(Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary);

        /// <summary>
        /// 提交绘图属性变化历史
        /// </summary>
        /// <param name="drawingAttributes">绘图属性变化字典</param>
        void CommitStrokeDrawingAttributesHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes);

        /// <summary>
        /// 提交笔画擦除历史
        /// </summary>
        /// <param name="strokes">被擦除的笔画</param>
        /// <param name="sourceStrokes">源笔画（可选）</param>
        void CommitStrokeEraseHistory(StrokeCollection strokes, StrokeCollection sourceStrokes = null);

        #endregion

        #region 撤销/重做操作

        /// <summary>
        /// 执行撤销操作
        /// </summary>
        /// <returns>撤销的历史记录项</returns>
        TimeMachineHistory Undo();

        /// <summary>
        /// 执行重做操作
        /// </summary>
        /// <returns>重做的历史记录项</returns>
        TimeMachineHistory Redo();

        #endregion

        #region 历史记录管理

        /// <summary>
        /// 清除所有历史记录
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// 导出历史记录
        /// </summary>
        /// <returns>历史记录数组</returns>
        TimeMachineHistory[] ExportHistory();

        /// <summary>
        /// 导入历史记录
        /// </summary>
        /// <param name="history">历史记录数组</param>
        /// <returns>是否成功</returns>
        bool ImportHistory(TimeMachineHistory[] history);

        #endregion
    }
}