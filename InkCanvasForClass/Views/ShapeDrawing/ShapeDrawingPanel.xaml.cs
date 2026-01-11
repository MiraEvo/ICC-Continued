using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ink_Canvas.Views.ShapeDrawing
{
    public partial class ShapeDrawingPanel : UserControl
    {
        public ShapeDrawingPanel()
        {
            InitializeComponent();
        }

        private MainWindow MainWindow => Application.Current.MainWindow as MainWindow;

        private void Image_MouseDown(object sender, MouseButtonEventArgs e) => MainWindow?.Image_MouseDown(sender, e);
        private void BtnDrawLine_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawLine_Click(sender, e);
        private void BtnDrawDashedLine_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawDashedLine_Click(sender, e);
        private void BtnDrawDotLine_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawDotLine_Click(sender, e);
        private void BtnDrawArrow_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawArrow_Click(sender, e);
        private void BtnDrawParallelLine_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawParallelLine_Click(sender, e);
        private void BtnDrawRectangleCenter_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawRectangleCenter_Click(sender, e);
        private void BtnDrawCircle_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCircle_Click(sender, e);
        private void BtnDrawDashedCircle_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawDashedCircle_Click(sender, e);
        private void BtnDrawCenterEllipse_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCenterEllipse_Click(sender, e);
        private void BtnDrawCoordinate1_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCoordinate1_Click(sender, e);
        private void BtnDrawCoordinate2_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCoordinate2_Click(sender, e);
        private void BtnDrawCoordinate3_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCoordinate3_Click(sender, e);
        private void BtnDrawCoordinate4_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCoordinate4_Click(sender, e);
        private void BtnDrawCoordinate5_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCoordinate5_Click(sender, e);
        private void BtnDrawCuboid_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCuboid_Click(sender, e);
        private void BtnDrawRectangle_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawRectangle_Click(sender, e);
        private void BtnDrawCylinder_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCylinder_Click(sender, e);
        private void BtnDrawCone_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCone_Click(sender, e);
        private void BtnDrawEllipse_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawEllipse_Click(sender, e);
        private void BtnDrawCenterEllipseWithFocalPoint_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawCenterEllipseWithFocalPoint_Click(sender, e);
        private void BtnDrawHyperbola_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawHyperbola_Click(sender, e);
        private void BtnDrawHyperbolaWithFocalPoint_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawHyperbolaWithFocalPoint_Click(sender, e);
        private void BtnDrawParabola1_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawParabola1_Click(sender, e);
        private void BtnDrawParabolaWithFocalPoint_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawParabolaWithFocalPoint_Click(sender, e);
        private void BtnDrawParabola2_Click(object sender, MouseButtonEventArgs e) => MainWindow?.BtnDrawParabola2_Click(sender, e);
    }
}
