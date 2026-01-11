using Ink_Canvas.ViewModels;
using System.Windows.Controls;

namespace Ink_Canvas.Views.FloatingBar
{
    /// <summary>
    /// FloatingBarView.xaml 的交互逻辑
    /// </summary>
    public partial class FloatingBarView : UserControl
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public FloatingBarView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ViewModel 属性
        /// </summary>
        public FloatingBarViewModel ViewModel
        {
            get => DataContext as FloatingBarViewModel;
            set => DataContext = value;
        }
    }
}
