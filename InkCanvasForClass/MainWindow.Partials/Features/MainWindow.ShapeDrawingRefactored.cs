using Ink_Canvas.Services;
using Ink_Canvas.ShapeDrawing.Core;
// 使用别名避免与 MainWindow.ShapeDrawingType 冲突
using NewShapeType = Ink_Canvas.ShapeDrawing.Core.ShapeDrawingType;

namespace Ink_Canvas {
    public partial class MainWindow {

        #region 重构后的形状绘制系统

        /// <summary>
        /// 形状绘制服务实例（在 MW_ShapeDrawing.cs 的 TryDrawShapeWithRefactoredSystem 中使用）
        /// </summary>
        private readonly Ink_Canvas.Services.ShapeDrawingService _shapeDrawingService = Ink_Canvas.Services.ShapeDrawingService.Instance;

        /// <summary>
        /// 当前形状绘制模式（使用枚举替代魔法数字）
        /// </summary>
        private NewShapeType? _currentShapeType;

        /// <summary>
        /// 设置形状绘制模式（新系统）
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        public void SetShapeDrawingModeRefactored(NewShapeType shapeType) {
            _currentShapeType = shapeType;
            // 同时更新旧系统的魔法数字以保持兼容
            drawingShapeMode = (int)shapeType;

            forceEraser = true;
            inkCanvas.EditingMode = System.Windows.Controls.InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        /// <summary>
        /// 清除形状绘制模式（新系统）
        /// </summary>
        public void ClearShapeDrawingModeRefactored() {
            _currentShapeType = null;
            drawingShapeMode = 0;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取当前形状类型的显示名称
        /// </summary>
        public string GetCurrentShapeDisplayName() {
            return _currentShapeType != null
                ? ShapeDrawingTypeExtensions.GetDisplayName(_currentShapeType.Value)
                : "无";
        }

        /// <summary>
        /// 检查当前是否处于形状绘制模式
        /// </summary>
        public bool IsInShapeDrawingMode() {
            return _currentShapeType != null || drawingShapeMode != 0;
        }

        #endregion
    }
}
