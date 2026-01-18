using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Pen = System.Windows.Media.Pen;

namespace Ink_Canvas.Helpers
{
    
    public class InkStrokesOverlay : FrameworkElement
    {
        private VisualCollection _children;
        private ImprovedDrawingVisual _layer = new ImprovedDrawingVisual();
        private StrokeCollection cachedStrokeCollection = new StrokeCollection();
        private DrawingGroup cachedDrawingGroup = new DrawingGroup();
        private bool isCached = false;
        private DrawingContext context;
        
        // 缓存的画刷，避免重复创建
        private static readonly SolidColorBrush WhiteBrush;
        
        static InkStrokesOverlay()
        {
            WhiteBrush = new SolidColorBrush(Colors.White);
            WhiteBrush.Freeze(); // 冻结画刷以提高性能
        }

        public class ImprovedDrawingVisual: DrawingVisual {
            public ImprovedDrawingVisual() {
                CacheMode = new BitmapCache() {
                    EnableClearType = false,
                    RenderAtScale = 1,
                    SnapsToDevicePixels = false
                };
            }
        }

        public InkStrokesOverlay()
        {
            _children = new VisualCollection(this) {
                _layer
            };
        }

        protected override int VisualChildrenCount => _children.Count;

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count) throw new ArgumentOutOfRangeException();
            return _children[index];
        }

        public DrawingContext Open() {
            context = _layer.RenderOpen();
            return context;
        }

        public void Close() {
            context.Close();
        }

        public void DrawStrokes(StrokeCollection strokes, Matrix? matrixTransform, bool isOneTimeDrawing = true) {
            if (isOneTimeDrawing) {
                context = _layer.RenderOpen();
            }

            MatrixTransform cachedMatrixTransform = null;
            if (matrixTransform != null) {
                cachedMatrixTransform = new MatrixTransform((Matrix)matrixTransform);
                cachedMatrixTransform.Freeze();
                context.PushTransform(cachedMatrixTransform);
            }

            if (strokes.Count != 0) {
                if (!isCached || !ReferenceEquals(strokes, cachedStrokeCollection)) {
                    cachedStrokeCollection = strokes;
                    cachedDrawingGroup = new DrawingGroup();
                    var gp_context = cachedDrawingGroup.Open();
                    
                    var stks_cloned = strokes.Clone();
                    foreach (var stroke in stks_cloned) {
                        stroke.DrawingAttributes.Width += 2;
                        stroke.DrawingAttributes.Height += 2;
                    }
                    stks_cloned.Draw(gp_context);
                    
                    foreach (var geo in strokes.Select(ori_stk => ori_stk.GetGeometry())) {
                        gp_context.DrawGeometry(WhiteBrush, null, geo);
                    }
                    gp_context.Close();
                    
                    cachedDrawingGroup.Freeze();
                    isCached = true;
                }
            }

            context.DrawDrawing(cachedDrawingGroup);

            if (matrixTransform != null) context.Pop();

            if (isOneTimeDrawing) {
                context.Close();
            }
        }
        
        /// <summary>
        /// 清除缓存，在墨迹发生变化时调用
        /// </summary>
        public void InvalidateCache()
        {
            isCached = false;
            cachedStrokeCollection = new StrokeCollection();
            cachedDrawingGroup = new DrawingGroup();
        }
    }
}
