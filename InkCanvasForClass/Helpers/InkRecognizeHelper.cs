using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

namespace Ink_Canvas.Helpers
{
    public class InkRecognizeHelper
    {
        //识别形状
        public static ShapeRecognizeResult RecognizeShape(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0)
                return default;

            var analyzer = new InkAnalyzer();
            analyzer.AddStrokes(strokes);
            analyzer.SetStrokesType(strokes, System.Windows.Ink.StrokeType.Drawing);

            AnalysisAlternate analysisAlternate = null;
            int strokesCount = strokes.Count;
            var sfsaf = analyzer.Analyze();
            if (sfsaf.Successful)
            {
                var alternates = analyzer.GetAlternates();
                if (alternates.Count > 0)
                {
                    while ((!alternates[0].Strokes.Contains(strokes.Last()) ||
                        !IsContainShapeType(((InkDrawingNode)alternates[0].AlternateNodes[0]).GetShapeName()))
                        && strokesCount >= 2)
                    {
                        analyzer.RemoveStroke(strokes[strokes.Count - strokesCount]);
                        strokesCount--;
                        sfsaf = analyzer.Analyze();
                        if (sfsaf.Successful)
                        {
                            alternates = analyzer.GetAlternates();
                        }
                    }
                    analysisAlternate = alternates[0];
                }
            }

            analyzer.Dispose();

            if (analysisAlternate != null && analysisAlternate.AlternateNodes.Count > 0)
            {
                var node = analysisAlternate.AlternateNodes[0] as InkDrawingNode;
                return new ShapeRecognizeResult(node.Centroid, node.HotPoints, analysisAlternate, node);
            }

            return default;
        }

        public static bool IsContainShapeType(string name)
        {
            if (name.Contains("Triangle") || name.Contains("Circle") ||
                name.Contains("Rectangle") || name.Contains("Diamond") ||
                name.Contains("Parallelogram") || name.Contains("Square")
                || name.Contains("Ellipse"))
            {
                return true;
            }
            return false;
        }
    }

    //Recognizer 的实现

    public class ShapeRecognizeResult
    {
        public ShapeRecognizeResult(Point centroid, PointCollection hotPoints, AnalysisAlternate analysisAlternate, InkDrawingNode node)
        {
            Centroid = centroid;
            HotPoints = hotPoints;
            AnalysisAlternate = analysisAlternate;
            InkDrawingNode = node;
        }

        public AnalysisAlternate AnalysisAlternate { get; }

        public Point Centroid { get; set; }

        public PointCollection HotPoints { get; }

        public InkDrawingNode InkDrawingNode { get; }
    }

    /// <summary>
    /// 用于自动控制其他形状相对于圆的位置
    /// </summary>

    public class Circle
    {
        public Circle(Point centroid, double r, Stroke stroke)
        {
            Centroid = centroid;
            R = r;
            Stroke = stroke;
        }

        public Point Centroid { get; set; }

        public double R { get; set; }

        public Stroke Stroke { get; set; }
    }
}
