using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// NamesInputWindow ViewModel - 管理名单输入窗口状态和逻辑
    /// </summary>
    public partial class NamesInputWindowViewModel : ViewModelBase
    {
        #region 构造函数

        public NamesInputWindowViewModel()
        {
            _ = LoadNamesAsync();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 名单文本内容
        /// </summary>
        [ObservableProperty]
        private string _namesText = string.Empty;

        /// <summary>
        /// 原始文本内容（用于比较是否修改）
        /// </summary>
        [ObservableProperty]
        private string _originalText = string.Empty;

        /// <summary>
        /// 窗口标题
        /// </summary>
        [ObservableProperty]
        private string _windowTitle = "Ink Canvas 抽奖 - 名单导入";

        /// <summary>
        /// 提示文本
        /// </summary>
        [ObservableProperty]
        private string _hintText = "请在下方输入名单，每行一人（建议直接粘贴表格姓名列）";

        /// <summary>
        /// 是否已修改
        /// </summary>
        public bool IsModified => NamesText != OriginalText;

        #endregion

        #region 命令

        /// <summary>
        /// 加载名单命令
        /// </summary>
        [RelayCommand]
        private async Task LoadNamesAsync()
        {
            try
            {
                string namesPath = Path.Combine(App.RootPath, "Names.txt");
                if (File.Exists(namesPath))
                {
                    NamesText = await File.ReadAllTextAsync(namesPath);
                    OriginalText = NamesText;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"加载名单失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存名单命令
        /// </summary>
        [RelayCommand]
        private async Task SaveNamesAsync()
        {
            try
            {
                string namesPath = Path.Combine(App.RootPath, "Names.txt");
                await File.WriteAllTextAsync(namesPath, NamesText);
                OriginalText = NamesText;
                NamesSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"保存名单失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭窗口命令
        /// </summary>
        [RelayCommand]
        private void Close()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 请求保存并关闭命令
        /// </summary>
        [RelayCommand]
        private async Task SaveAndCloseAsync()
        {
            if (IsModified)
            {
                await SaveNamesAsync();
            }
            Close();
        }

        #endregion

        #region 事件

        /// <summary>
        /// 请求关闭窗口事件
        /// </summary>
        public event EventHandler RequestClose;

        /// <summary>
        /// 名单已保存事件
        /// </summary>
        public event EventHandler NamesSaved;

        /// <summary>
        /// 发生错误事件
        /// </summary>
        public event EventHandler<string> ErrorOccurred;

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查是否需要保存
        /// </summary>
        /// <returns>如果内容已修改返回 true</returns>
        public bool CheckNeedsSave()
        {
            return IsModified;
        }

        #endregion
    }
}
