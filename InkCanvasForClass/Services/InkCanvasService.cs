using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// InkCanvas 服务实现 - 包装 IccInkCanvasModern
    /// </summary>
    public class InkCanvasService : IInkCanvasService
    {
        private readonly IccInkCanvasModern _inkCanvas;
        private InkCanvasMode _currentMode = InkCanvasMode.Ink;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="inkCanvas">IccInkCanvasModern 实例</param>
        public InkCanvasService(IccInkCanvasModern inkCanvas)
        {
            _inkCanvas = inkCanvas ?? throw new ArgumentNullException(nameof(inkCanvas));
            
            // 订阅 InkCanvas 事件
            _inkCanvas.Strokes.StrokesChanged += OnStrokesChanged;
            _inkCanvas.SelectionChanged += OnSelectionChanged;
            _inkCanvas.DefaultDrawingAttributesReplaced += OnDrawingAttributesReplaced;
            _inkCanvas.EditingModeChanged += OnEditingModeChanged;
        }

        #region 事件

        /// <inheritdoc />
        public event EventHandler<StrokesChangedEventArgs> StrokesChanged;

        /// <inheritdoc />
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        /// <inheritdoc />
        public event EventHandler<InkCanvasMode> EditingModeChanged;

        /// <inheritdoc />
        public event EventHandler<DrawingAttributes> DrawingAttributesChanged;

        #endregion

        #region 内部事件处理

        private void OnStrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            StrokesChanged?.Invoke(this, new StrokesChangedEventArgs(e.Added, e.Removed));
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_inkCanvas.GetSelectedStrokes()));
        }

        private void OnDrawingAttributesReplaced(object sender, DrawingAttributesReplacedEventArgs e)
        {
            DrawingAttributesChanged?.Invoke(this, e.NewDrawingAttributes);
        }

        private void OnEditingModeChanged(object sender, RoutedEventArgs e)
        {
            _currentMode = ConvertFromInkCanvasEditingMode(_inkCanvas.EditingMode);
            EditingModeChanged?.Invoke(this, _currentMode);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 获取内部 IccInkCanvasModern 实例（用于兼容现有代码）
        /// </summary>
        public IccInkCanvasModern InternalInkCanvas => _inkCanvas;

        /// <inheritdoc />
        public StrokeCollection Strokes
        {
            get => _inkCanvas.Strokes;
            set => _inkCanvas.Strokes = value ?? new StrokeCollection();
        }

        /// <inheritdoc />
        public StrokeCollection SelectedStrokes => _inkCanvas.GetSelectedStrokes();

        /// <inheritdoc />
        public InkCanvasMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    _inkCanvas.EditingMode = ConvertToInkCanvasEditingMode(value);
                    EditingModeChanged?.Invoke(this, value);
                }
            }
        }

        /// <inheritdoc />
        public DrawingAttributes DefaultDrawingAttributes
        {
            get => _inkCanvas.DefaultDrawingAttributes;
            set
            {
                if (value != null)
                {
                    _inkCanvas.DefaultDrawingAttributes = value;
                    DrawingAttributesChanged?.Invoke(this, value);
                }
            }
        }

        /// <inheritdoc />
        public double ActualWidth => _inkCanvas.ActualWidth;

        /// <inheritdoc />
        public double ActualHeight => _inkCanvas.ActualHeight;

        /// <inheritdoc />
        public bool HasSelection => SelectedStrokes?.Count > 0;

        /// <inheritdoc />
        public int StrokeCount => _inkCanvas.Strokes?.Count ?? 0;

        #endregion

        #region 笔画操作

        /// <inheritdoc />
        public void AddStrokes(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0) return;
            _inkCanvas.Strokes.Add(strokes);
        }

        /// <inheritdoc />
        public void AddStroke(Stroke stroke)
        {
            if (stroke == null) return;
            _inkCanvas.Strokes.Add(stroke);
        }

        /// <inheritdoc />
        public void RemoveStrokes(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0) return;
            foreach (var stroke in strokes)
            {
                if (_inkCanvas.Strokes.Contains(stroke))
                {
                    _inkCanvas.Strokes.Remove(stroke);
                }
            }
        }

        /// <inheritdoc />
        public void RemoveStroke(Stroke stroke)
        {
            if (stroke == null) return;
            if (_inkCanvas.Strokes.Contains(stroke))
            {
                _inkCanvas.Strokes.Remove(stroke);
            }
        }

        /// <inheritdoc />
        public void ClearStrokes()
        {
            _inkCanvas.Strokes.Clear();
        }

        /// <inheritdoc />
        public void ReplaceStrokes(StrokeCollection oldStrokes, StrokeCollection newStrokes)
        {
            if (oldStrokes != null)
            {
                RemoveStrokes(oldStrokes);
            }
            if (newStrokes != null)
            {
                AddStrokes(newStrokes);
            }
        }

        /// <inheritdoc />
        public bool ContainsStroke(Stroke stroke)
        {
            return stroke != null && _inkCanvas.Strokes.Contains(stroke);
        }

        #endregion

        #region 选择操作

        /// <inheritdoc />
        public void Select(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0) return;
            _inkCanvas.Select(strokes);
        }

        /// <inheritdoc />
        public void SelectAll()
        {
            if (_inkCanvas.Strokes.Count > 0)
            {
                _inkCanvas.Select(_inkCanvas.Strokes);
            }
        }

        /// <inheritdoc />
        public void ClearSelection()
        {
            _inkCanvas.Select(new StrokeCollection());
        }

        /// <inheritdoc />
        public void DeleteSelection()
        {
            var selectedStrokes = SelectedStrokes;
            if (selectedStrokes != null && selectedStrokes.Count > 0)
            {
                RemoveStrokes(selectedStrokes);
            }
        }

        /// <inheritdoc />
        public StrokeCollection CopySelection()
        {
            var selectedStrokes = SelectedStrokes;
            if (selectedStrokes == null || selectedStrokes.Count == 0)
            {
                return new StrokeCollection();
            }

            var copiedStrokes = new StrokeCollection();
            foreach (var stroke in selectedStrokes)
            {
                copiedStrokes.Add(stroke.Clone());
            }
            return copiedStrokes;
        }

        #endregion

        #region 绘图属性操作

        /// <inheritdoc />
        public void SetStrokeColor(Color color)
        {
            _inkCanvas.DefaultDrawingAttributes.Color = color;
        }

        /// <inheritdoc />
        public void SetStrokeWidth(double width)
        {
            if (width > 0)
            {
                _inkCanvas.DefaultDrawingAttributes.Width = width;
            }
        }

        /// <inheritdoc />
        public void SetStrokeHeight(double height)
        {
            if (height > 0)
            {
                _inkCanvas.DefaultDrawingAttributes.Height = height;
            }
        }

        /// <inheritdoc />
        public void SetHighlighter(bool isHighlighter)
        {
            _inkCanvas.DefaultDrawingAttributes.IsHighlighter = isHighlighter;
        }

        /// <inheritdoc />
        public void ApplyDrawingAttributesToSelection(DrawingAttributes attributes)
        {
            if (attributes == null) return;
            
            var selectedStrokes = SelectedStrokes;
            if (selectedStrokes == null || selectedStrokes.Count == 0) return;

            foreach (var stroke in selectedStrokes)
            {
                stroke.DrawingAttributes = attributes.Clone();
            }
        }

        #endregion

        #region 视图操作

        /// <inheritdoc />
        public void ScrollTo(Point point)
        {
            // InkCanvas 本身不支持滚动，需要通过父容器实现
            // 这里留作扩展接口
        }

        /// <inheritdoc />
        public Rect GetStrokesBounds(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0)
            {
                return Rect.Empty;
            }
            return strokes.GetBounds();
        }

        /// <inheritdoc />
        public Rect GetAllStrokesBounds()
        {
            return GetStrokesBounds(_inkCanvas.Strokes);
        }

        #endregion

        #region 命中测试

        /// <inheritdoc />
        public StrokeCollection HitTest(Point point)
        {
            return _inkCanvas.Strokes.HitTest(point);
        }

        /// <inheritdoc />
        public StrokeCollection HitTest(Rect rect)
        {
            return _inkCanvas.Strokes.HitTest(rect, 50);
        }

        /// <inheritdoc />
        public StrokeCollection HitTest(StylusPointCollection points)
        {
            if (points == null || points.Count == 0)
            {
                return new StrokeCollection();
            }

            var pointArray = new Point[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                pointArray[i] = new Point(points[i].X, points[i].Y);
            }

            return _inkCanvas.Strokes.HitTest(pointArray, new RectangleStylusShape(1, 1));
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 将 InkCanvasMode 转换为 InkCanvasEditingMode
        /// </summary>
        private static InkCanvasEditingMode ConvertToInkCanvasEditingMode(InkCanvasMode mode)
        {
            return mode switch
            {
                InkCanvasMode.None => InkCanvasEditingMode.None,
                InkCanvasMode.Ink => InkCanvasEditingMode.Ink,
                InkCanvasMode.GestureOnly => InkCanvasEditingMode.GestureOnly,
                InkCanvasMode.InkAndGesture => InkCanvasEditingMode.InkAndGesture,
                InkCanvasMode.Select => InkCanvasEditingMode.Select,
                InkCanvasMode.EraseByPoint => InkCanvasEditingMode.EraseByPoint,
                InkCanvasMode.EraseByStroke => InkCanvasEditingMode.EraseByStroke,
                _ => InkCanvasEditingMode.Ink
            };
        }

        /// <summary>
        /// 将 InkCanvasEditingMode 转换为 InkCanvasMode
        /// </summary>
        private static InkCanvasMode ConvertFromInkCanvasEditingMode(InkCanvasEditingMode mode)
        {
            return mode switch
            {
                InkCanvasEditingMode.None => InkCanvasMode.None,
                InkCanvasEditingMode.Ink => InkCanvasMode.Ink,
                InkCanvasEditingMode.GestureOnly => InkCanvasMode.GestureOnly,
                InkCanvasEditingMode.InkAndGesture => InkCanvasMode.InkAndGesture,
                InkCanvasEditingMode.Select => InkCanvasMode.Select,
                InkCanvasEditingMode.EraseByPoint => InkCanvasMode.EraseByPoint,
                InkCanvasEditingMode.EraseByStroke => InkCanvasMode.EraseByStroke,
                _ => InkCanvasMode.Ink
            };
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 释放事件订阅
        /// </summary>
        public void Dispose()
        {
            _inkCanvas.Strokes.StrokesChanged -= OnStrokesChanged;
            _inkCanvas.SelectionChanged -= OnSelectionChanged;
            _inkCanvas.DefaultDrawingAttributesReplaced -= OnDrawingAttributesReplaced;
            _inkCanvas.EditingModeChanged -= OnEditingModeChanged;
        }

        #endregion
    }
}