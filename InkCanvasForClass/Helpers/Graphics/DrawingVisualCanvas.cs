using System;
using System.Windows;
using System.Windows.Media;

namespace Ink_Canvas.Helpers
{
    
    public class DrawingVisualCanvas : FrameworkElement
    {
        private readonly VisualCollection _children;
        public DrawingVisual DrawingVisual = new();

        public DrawingVisualCanvas()
        {
            _children = new VisualCollection(this) {
                DrawingVisual // 初始化DrawingVisual
            };
        }

        protected override int VisualChildrenCount => _children.Count;

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _children[index];
        }
    }
}
