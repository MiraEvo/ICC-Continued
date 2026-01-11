using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ink_Canvas.Views.ShapeDrawing
{
    /// <summary>
    /// ShapeDrawingView.xaml 的交互逻辑
    /// 提供几何图形绘制面板
    /// </summary>
    public partial class ShapeDrawingView : UserControl
    {
        #region Shape Types Enum

        public enum ShapeType
        {
            Line,
            DashedLine,
            DotLine,
            Arrow,
            ParallelLine,
            RectangleCenter,
            Circle,
            DashedCircle,
            CenterEllipse,
            Cuboid,
            Rectangle,
            Cylinder,
            Cone
        }

        #endregion

        #region Events

        /// <summary>
        /// 形状选择事件
        /// </summary>
        public event EventHandler<ShapeSelectedEventArgs> ShapeSelected;

        #endregion

        public ShapeDrawingView()
        {
            InitializeComponent();
        }

        #region Event Handlers

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.Opacity = 0.7;
            }
        }

        private void BtnDrawLine_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.Line);
        }

        private void BtnDrawDashedLine_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.DashedLine);
        }

        private void BtnDrawDotLine_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.DotLine);
        }

        private void BtnDrawArrow_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.Arrow);
        }

        private void BtnDrawParallelLine_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.ParallelLine);
        }

        private void BtnDrawRectangleCenter_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.RectangleCenter);
        }

        private void BtnDrawCircle_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.Circle);
        }

        private void BtnDrawDashedCircle_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.DashedCircle);
        }

        private void BtnDrawCenterEllipse_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.CenterEllipse);
        }

        private void BtnDrawCuboid_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.Cuboid);
        }

        private void BtnDrawRectangle_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.Rectangle);
        }

        private void BtnDrawCylinder_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.Cylinder);
        }

        private void BtnDrawCone_Click(object sender, MouseButtonEventArgs e)
        {
            ResetOpacity(sender);
            OnShapeSelected(ShapeType.Cone);
        }

        #endregion

        #region Helper Methods

        private void ResetOpacity(object sender)
        {
            if (sender is FrameworkElement element)
            {
                element.Opacity = 1.0;
            }
        }

        private void OnShapeSelected(ShapeType shapeType)
        {
            ShapeSelected?.Invoke(this, new ShapeSelectedEventArgs(shapeType));
        }

        #endregion
    }

    #region Event Args

    public class ShapeSelectedEventArgs : EventArgs
    {
        public ShapeDrawingView.ShapeType ShapeType { get; }

        public ShapeSelectedEventArgs(ShapeDrawingView.ShapeType shapeType)
        {
            ShapeType = shapeType;
        }
    }

    #endregion
}
