using System;
using System.Windows;

namespace Ink_Canvas {
    public partial class MainWindow {
        public static bool StartAutomaticallyCreate(string exeName) {
            try {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + exeName + ".lnk");
                //设置快捷方式的目标所在的位置(源程序完整路径)
                shortcut.TargetPath = System.Windows.Forms.Application.ExecutablePath;
                //应用程序的工作目录
                //当用户没有指定一个具体的目录时，快捷方式的目标应用程序将使用该属性所指定的目录来装载或保存文件。
                shortcut.WorkingDirectory = Environment.CurrentDirectory;
                //目标应用程序窗口类型(1.Normal window普通窗口,3.Maximized最大化窗口,7.Minimized最小化)
                shortcut.WindowStyle = 1;
                //快捷方式的描述
                shortcut.Description = exeName + "_Ink";
                //设置快捷键(如果有必要的话.)
                //shortcut.Hotkey = "CTRL+ALT+D";
                shortcut.Save();
                Helpers.LogHelper.WriteLogToFile($"Auto-start shortcut created successfully for {exeName}", Helpers.LogHelper.LogType.Info);
                return true;
            }
            catch (Exception ex) {
                Helpers.LogHelper.WriteLogToFile($"Failed to create auto-start shortcut: {ex.Message}", Helpers.LogHelper.LogType.Error);
                Helpers.LogHelper.NewLog(ex);
            }

            return false;
        }

        public static bool StartAutomaticallyDel(string exeName) {
            try {
                System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + exeName +
                                      ".lnk");
                Helpers.LogHelper.WriteLogToFile($"Auto-start shortcut deleted successfully for {exeName}", Helpers.LogHelper.LogType.Info);
                return true;
            }
            catch (Exception ex) {
                Helpers.LogHelper.WriteLogToFile($"Failed to delete auto-start shortcut: {ex.Message}", Helpers.LogHelper.LogType.Warning);
                Helpers.LogHelper.NewLog(ex);
            }

            return false;
        }
    }
}