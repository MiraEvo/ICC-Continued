using System;
using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas.Services.PalmEraser
{
    /// <summary>
    /// 触摸特征数据模型 - 用于手掌检测的多维度特征
    /// </summary>
    public class TouchCharacteristics
    {
        /// <summary>
        /// 触摸设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 触摸面积（宽 x 高）
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// 触摸宽度
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 触摸高度
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 长宽比
        /// </summary>
        public double AspectRatio { get; set; }

        /// <summary>
        /// 圆度（0-1，1表示完美圆形）
        /// </summary>
        public double Circularity { get; set; }

        /// <summary>
        /// 压力值（如果设备支持）
        /// </summary>
        public double Pressure { get; set; }

        /// <summary>
        /// 当前位置
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// 移动方向向量
        /// </summary>
        public Vector Direction { get; set; }

        /// <summary>
        /// 移动速度（像素/毫秒）
        /// </summary>
        public double Velocity { get; set; }

        /// <summary>
        /// 触摸边界矩形
        /// </summary>
        public Rect Bounds { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 触摸持续时间（毫秒）
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// 从 TouchEventArgs 创建触摸特征
        /// </summary>
        public static TouchCharacteristics FromTouchEventArgs(TouchEventArgs e, Point? previousPosition = null, DateTime? previousTimestamp = null)
        {
            var touchPoint = e.GetTouchPoint(null);
            var bounds = touchPoint.Bounds;
            var position = touchPoint.Position;
            var now = DateTime.UtcNow;

            var characteristics = new TouchCharacteristics
            {
                DeviceId = e.TouchDevice.Id,
                Width = bounds.Width,
                Height = bounds.Height,
                Area = bounds.Width * bounds.Height,
                AspectRatio = bounds.Width > 0 ? bounds.Height / bounds.Width : 1,
                Circularity = CalculateCircularity(bounds),
                Pressure = 0.5, // TouchPoint 不支持 Pressure，使用默认值
                Position = position,
                Bounds = bounds,
                Timestamp = now
            };

            // 计算移动特征
            if (previousPosition.HasValue && previousTimestamp.HasValue)
            {
                var delta = position - previousPosition.Value;
                var timeDelta = (now - previousTimestamp.Value).TotalMilliseconds;

                if (timeDelta > 0)
                {
                    characteristics.Velocity = delta.Length / timeDelta;
                    characteristics.Direction = delta.Length > 0 ? delta / delta.Length : new Vector(0, 0);
                    characteristics.Duration = timeDelta;
                }
            }

            return characteristics;
        }

        /// <summary>
        /// 计算圆度（基于面积和周长的关系）
        /// </summary>
        private static double CalculateCircularity(Rect bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return 1.0;

            // 使用椭圆近似计算圆度
            // 圆度 = 4π × 面积 / 周长²
            double a = bounds.Width / 2;
            double b = bounds.Height / 2;

            // 椭圆面积
            double area = Math.PI * a * b;

            // 椭圆周长近似（Ramanujan近似）
            double h = Math.Pow((a - b) / (a + b), 2);
            double perimeter = Math.PI * (a + b) * (1 + (3 * h) / (10 + Math.Sqrt(4 - 3 * h)));

            double circularity = (4 * Math.PI * area) / (perimeter * perimeter);
            return Math.Min(1.0, Math.Max(0.0, circularity));
        }

        /// <summary>
        /// 获取有效宽度（考虑特殊屏幕）
        /// </summary>
        public double GetEffectiveWidth(bool isQuadIr)
        {
            return isQuadIr ? Math.Sqrt(Area) : Width;
        }

        /// <summary>
        /// 克隆当前特征（用于历史记录）
        /// </summary>
        public TouchCharacteristics Clone()
        {
            return new TouchCharacteristics
            {
                DeviceId = DeviceId,
                Width = Width,
                Height = Height,
                Area = Area,
                AspectRatio = AspectRatio,
                Circularity = Circularity,
                Pressure = Pressure,
                Position = Position,
                Direction = Direction,
                Velocity = Velocity,
                Bounds = Bounds,
                Timestamp = Timestamp,
                Duration = Duration
            };
        }

        /// <summary>
        /// 判断两个特征是否来自同一设备且时间接近
        /// </summary>
        public bool IsContinuationOf(TouchCharacteristics other, double maxTimeGapMs = 100)
        {
            return DeviceId == other.DeviceId &&
                   (Timestamp - other.Timestamp).TotalMilliseconds <= maxTimeGapMs;
        }
    }
}
