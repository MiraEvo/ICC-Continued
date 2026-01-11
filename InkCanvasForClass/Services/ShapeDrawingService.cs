using System;
using System.Windows;
using System.Windows.Ink;
using Ink_Canvas.ShapeDrawing.Core;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 形状绘制服务实现
    /// 提供形状绘制功能，支持依赖注入
    /// </summary>
    public class ShapeDrawingService : IShapeDrawingService
    {
        private static readonly Lazy<ShapeDrawingService> _instance =
            new Lazy<ShapeDrawingService>(() => new ShapeDrawingService());

        /// <summary>
        /// 获取服务单例
        /// </summary>
        public static ShapeDrawingService Instance => _instance.Value;

        #region 私有字段

        private readonly ShapeDrawerFactory _factory;
        private IShapeDrawer _currentDrawer;
        private ShapeDrawingType? _currentShapeType;
        private bool _isDrawingMode;
        private bool _isDrawing;
        private Point? _startPoint;
        private int _currentStep;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建形状绘制服务实例
        /// </summary>
        public ShapeDrawingService()
        {
            _factory = ShapeDrawerFactory.Instance;
        }

        /// <summary>
        /// 创建形状绘制服务实例（用于测试）
        /// </summary>
        /// <param name="factory">形状绘制器工厂</param>
        internal ShapeDrawingService(ShapeDrawerFactory factory)
        {
            _factory = factory ?? ShapeDrawerFactory.Instance;
        }

        #endregion

        #region 属性

        /// <inheritdoc/>
        public ShapeDrawingType? CurrentShapeType => _currentShapeType;

        /// <inheritdoc/>
        public bool IsDrawingMode => _isDrawingMode;

        /// <inheritdoc/>
        public bool IsDrawing => _isDrawing;

        /// <inheritdoc/>
        public int CurrentStep => _currentStep;

        /// <inheritdoc/>
        public Point? StartPoint => _startPoint;

        #endregion

        #region 事件

        /// <inheritdoc/>
        public event EventHandler<ShapeModeChangedEventArgs> ShapeModeChanged;

        /// <inheritdoc/>
        public event EventHandler<ShapeDrawnEventArgs> ShapeDrawn;

        #endregion

        #region 绘制模式控制

        /// <inheritdoc/>
        public void StartDrawing(ShapeDrawingType shapeType)
        {
            var oldType = _currentShapeType;
            _currentShapeType = shapeType;
            _isDrawingMode = true;

            try
            {
                _currentDrawer = _factory.GetOrCreate(shapeType);
                _currentDrawer.Reset();
            }
            catch (ArgumentException)
            {
                // 未注册的形状类型
                _currentDrawer = null;
                _isDrawingMode = false;
                _currentShapeType = null;
            }

            OnShapeModeChanged(oldType, _currentShapeType, _isDrawingMode);
        }

        /// <inheritdoc/>
        public void EndDrawing()
        {
            var oldType = _currentShapeType;

            Reset();

            OnShapeModeChanged(oldType, null, false);
        }

        /// <inheritdoc/>
        public void SetShapeType(ShapeDrawingType shapeType)
        {
            var oldType = _currentShapeType;
            _currentShapeType = shapeType;

            try
            {
                _currentDrawer = _factory.GetOrCreate(shapeType);
            }
            catch (ArgumentException)
            {
                _currentDrawer = null;
                _currentShapeType = null;
            }

            if (oldType != _currentShapeType)
            {
                OnShapeModeChanged(oldType, _currentShapeType, _isDrawingMode);
            }
        }

        #endregion

        #region 形状绘制

        /// <inheritdoc/>
        public void BeginShape(Point startPoint)
        {
            if (!_isDrawingMode || _currentDrawer == null)
            {
                return;
            }

            _startPoint = startPoint;
            _currentStep = 0;
            _isDrawing = true;
            _currentDrawer.Reset();
        }

        /// <inheritdoc/>
        public StrokeCollection DrawPreview(Point endPoint, DrawingAttributes drawingAttributes)
        {
            if (!_isDrawing || _currentDrawer == null || !_startPoint.HasValue)
            {
                return new StrokeCollection();
            }

            var context = new ShapeDrawingContext
            {
                StartPoint = _startPoint.Value,
                EndPoint = endPoint,
                DrawingAttributes = drawingAttributes,
                CurrentStep = _currentStep,
                IsPreview = true
            };

            if (!_currentDrawer.ValidateContext(context))
            {
                return new StrokeCollection();
            }

            return _currentDrawer.Draw(context);
        }

        /// <inheritdoc/>
        public StrokeCollection FinishShape(Point endPoint, DrawingAttributes drawingAttributes)
        {
            if (!_isDrawing || _currentDrawer == null || !_startPoint.HasValue)
            {
                return new StrokeCollection();
            }

            var context = new ShapeDrawingContext
            {
                StartPoint = _startPoint.Value,
                EndPoint = endPoint,
                DrawingAttributes = drawingAttributes,
                CurrentStep = _currentStep,
                IsPreview = false
            };

            StrokeCollection result;
            if (_currentDrawer.ValidateContext(context))
            {
                result = _currentDrawer.Draw(context);

                // 触发形状绘制完成事件
                if (_currentShapeType.HasValue && result.Count > 0)
                {
                    OnShapeDrawn(_currentShapeType.Value, result, _startPoint.Value, endPoint);
                }
            }
            else
            {
                result = new StrokeCollection();
            }

            // 重置绘制状态（但保持绘制模式）
            _isDrawing = false;
            _startPoint = null;
            _currentStep = 0;
            _currentDrawer?.Reset();

            return result;
        }

        /// <inheritdoc/>
        public void CancelShape()
        {
            _isDrawing = false;
            _startPoint = null;
            _currentStep = 0;
            _currentDrawer?.Reset();
        }

        /// <inheritdoc/>
        public StrokeCollection CreateShape(Point startPoint, Point endPoint, ShapeDrawingType shapeType, DrawingAttributes drawingAttributes)
        {
            try
            {
                var drawer = _factory.GetOrCreate(shapeType);
                var context = new ShapeDrawingContext
                {
                    StartPoint = startPoint,
                    EndPoint = endPoint,
                    DrawingAttributes = drawingAttributes,
                    CurrentStep = 0,
                    IsPreview = false
                };

                if (!drawer.ValidateContext(context))
                {
                    return new StrokeCollection();
                }

                var result = drawer.Draw(context);

                // 触发形状绘制完成事件
                if (result.Count > 0)
                {
                    OnShapeDrawn(shapeType, result, startPoint, endPoint);
                }

                return result;
            }
            catch (ArgumentException)
            {
                return new StrokeCollection();
            }
        }

        #endregion

        #region 绘制器访问

        /// <inheritdoc/>
        public IShapeDrawer GetDrawer(ShapeDrawingType shapeType)
        {
            try
            {
                return _factory.GetOrCreate(shapeType);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public bool IsShapeTypeSupported(ShapeDrawingType shapeType)
        {
            return _factory.IsSupported(shapeType);
        }

        #endregion

        #region 旧版兼容

        /// <inheritdoc/>
        public void StartDrawingLegacy(int legacyMode)
        {
            if (_factory.TryConvertFromLegacyMode(legacyMode, out var shapeType))
            {
                StartDrawing(shapeType);
            }
        }

        /// <inheritdoc/>
        public ShapeDrawingType? ConvertFromLegacyMode(int legacyMode)
        {
            if (_factory.TryConvertFromLegacyMode(legacyMode, out var shapeType))
            {
                return shapeType;
            }
            return null;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 重置所有状态
        /// </summary>
        private void Reset()
        {
            _isDrawing = false;
            _isDrawingMode = false;
            _startPoint = null;
            _currentStep = 0;
            _currentDrawer?.Reset();
            _currentDrawer = null;
            _currentShapeType = null;
        }

        /// <summary>
        /// 触发形状模式变化事件
        /// </summary>
        protected virtual void OnShapeModeChanged(ShapeDrawingType? oldType, ShapeDrawingType? newType, bool isDrawingMode)
        {
            ShapeModeChanged?.Invoke(this, new ShapeModeChangedEventArgs(oldType, newType, isDrawingMode));
        }

        /// <summary>
        /// 触发形状绘制完成事件
        /// </summary>
        protected virtual void OnShapeDrawn(ShapeDrawingType shapeType, StrokeCollection strokes, Point startPoint, Point endPoint)
        {
            ShapeDrawn?.Invoke(this, new ShapeDrawnEventArgs(shapeType, strokes, startPoint, endPoint));
        }

        #endregion
    }
}
