using System;
using System.Windows;
using System.Windows.Ink;

namespace Ink_Canvas.ShapeDrawing.Core {
    /// <summary>
    /// 形状绘制服务，提供统一的形状绘制接口
    /// </summary>
    public class ShapeDrawingService {
        private static readonly Lazy<ShapeDrawingService> _instance = 
            new Lazy<ShapeDrawingService>(() => new ShapeDrawingService());

        /// <summary>
        /// 获取服务单例
        /// </summary>
        public static ShapeDrawingService Instance => _instance.Value;

        /// <summary>
        /// 形状绘制器工厂
        /// </summary>
        private readonly ShapeDrawerFactory _factory;

        /// <summary>
        /// 当前使用的绘制器
        /// </summary>
        private IShapeDrawer _currentDrawer;

        /// <summary>
        /// 当前形状类型
        /// </summary>
        public ShapeDrawingType? CurrentShapeType { get; private set; }

        /// <summary>
        /// 是否正在绘制形状
        /// </summary>
        public bool IsDrawing { get; private set; }

        /// <summary>
        /// 绘制起点
        /// </summary>
        public Point? StartPoint { get; private set; }

        /// <summary>
        /// 当前绘制步骤
        /// </summary>
        public int CurrentStep { get; private set; }

        private ShapeDrawingService() {
            _factory = ShapeDrawerFactory.Instance;
        }

        /// <summary>
        /// 开始绘制形状
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        /// <param name="startPoint">起点</param>
        public void BeginDrawing(ShapeDrawingType shapeType, Point startPoint) {
            CurrentShapeType = shapeType;
            StartPoint = startPoint;
            CurrentStep = 0;
            IsDrawing = true;
            
            try {
                _currentDrawer = _factory.GetOrCreate(shapeType);
                _currentDrawer.Reset();
            } catch (ArgumentException) {
                // 未注册的形状类型
                _currentDrawer = null;
                IsDrawing = false;
            }
        }

        /// <summary>
        /// 开始绘制形状（通过旧版魔法数字）
        /// </summary>
        /// <param name="legacyMode">旧版形状模式数字</param>
        /// <param name="startPoint">起点</param>
        public void BeginDrawingLegacy(int legacyMode, Point startPoint) {
            if (_factory.TryConvertFromLegacyMode(legacyMode, out var shapeType)) {
                BeginDrawing(shapeType, startPoint);
            } else {
                IsDrawing = false;
                _currentDrawer = null;
            }
        }

        /// <summary>
        /// 绘制形状（用于实时预览）
        /// </summary>
        /// <param name="endPoint">终点</param>
        /// <param name="drawingAttributes">绘制属性</param>
        /// <returns>生成的笔画集合</returns>
        public StrokeCollection Draw(Point endPoint, DrawingAttributes drawingAttributes) {
            if (!IsDrawing || _currentDrawer == null || !StartPoint.HasValue) {
                return new StrokeCollection();
            }

            var context = new ShapeDrawingContext {
                StartPoint = StartPoint.Value,
                EndPoint = endPoint,
                DrawingAttributes = drawingAttributes,
                CurrentStep = CurrentStep
            };

            return _currentDrawer.Draw(context);
        }

        /// <summary>
        /// 完成绘制并返回最终结果
        /// </summary>
        /// <param name="endPoint">终点</param>
        /// <param name="drawingAttributes">绘制属性</param>
        /// <returns>最终生成的笔画集合</returns>
        public StrokeCollection FinishDrawing(Point endPoint, DrawingAttributes drawingAttributes) {
            var result = Draw(endPoint, drawingAttributes);
            Reset();
            return result;
        }

        /// <summary>
        /// 进入下一绘制步骤（用于多步绘制的形状）
        /// </summary>
        /// <param name="nextPoint">下一个点</param>
        public void NextStep(Point nextPoint) {
            if (!IsDrawing || _currentDrawer == null) return;

            if (_currentDrawer.SupportsMultiStep && CurrentStep < _currentDrawer.TotalSteps - 1) {
                CurrentStep++;
                // 可以在这里记录中间点用于多步绘制
            }
        }

        /// <summary>
        /// 取消当前绘制
        /// </summary>
        public void CancelDrawing() {
            Reset();
        }

        /// <summary>
        /// 重置绘制状态
        /// </summary>
        private void Reset() {
            IsDrawing = false;
            StartPoint = null;
            CurrentStep = 0;
            _currentDrawer?.Reset();
            _currentDrawer = null;
            CurrentShapeType = null;
        }

        /// <summary>
        /// 检查是否支持指定的形状类型
        /// </summary>
        public bool IsShapeTypeSupported(ShapeDrawingType shapeType) {
            return _factory.IsSupported(shapeType);
        }

        /// <summary>
        /// 检查是否支持指定的旧版模式
        /// </summary>
        public bool IsLegacyModeSupported(int legacyMode) {
            return _factory.TryConvertFromLegacyMode(legacyMode, out _);
        }

        /// <summary>
        /// 将旧版模式转换为形状类型
        /// </summary>
        public ShapeDrawingType? ConvertFromLegacyMode(int legacyMode) {
            if (_factory.TryConvertFromLegacyMode(legacyMode, out var shapeType)) {
                return shapeType;
            }
            return null;
        }

        /// <summary>
        /// 直接绘制形状（不需要开始/结束流程）
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        /// <param name="startPoint">起点</param>
        /// <param name="endPoint">终点</param>
        /// <param name="drawingAttributes">绘制属性</param>
        /// <returns>生成的笔画集合</returns>
        public StrokeCollection DrawShape(ShapeDrawingType shapeType, Point startPoint, Point endPoint, 
            DrawingAttributes drawingAttributes) {
            try {
                var drawer = _factory.GetOrCreate(shapeType);
                var context = new ShapeDrawingContext {
                    StartPoint = startPoint,
                    EndPoint = endPoint,
                    DrawingAttributes = drawingAttributes,
                    CurrentStep = 0
                };
                return drawer.Draw(context);
            } catch (ArgumentException) {
                return new StrokeCollection();
            }
        }

        /// <summary>
        /// 直接绘制形状（通过旧版模式）
        /// </summary>
        public StrokeCollection DrawShapeLegacy(int legacyMode, Point startPoint, Point endPoint,
            DrawingAttributes drawingAttributes) {
            if (_factory.TryConvertFromLegacyMode(legacyMode, out var shapeType)) {
                return DrawShape(shapeType, startPoint, endPoint, drawingAttributes);
            }
            return new StrokeCollection();
        }
    }
}