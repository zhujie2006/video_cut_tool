using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using WinForms = System.Windows.Forms;

namespace VideoCutTool.WPF.Services
{
    public class FileDialogService : IFileDialogService
    {
        public string? SelectVideoFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择视频文件",
                Filter = "视频文件|*.mp4;*.avi;*.mov;*.mkv;*.wmv;*.flv;*.webm|所有文件|*.*",
                Multiselect = false
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
            
            return null;
        }
        
        public string? SelectSaveFile(string defaultName, string filter)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "保存文件",
                FileName = defaultName,
                Filter = filter
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }
            
            return null;
        }
        
        public string? SelectFolder()
        {
            var folderBrowserDialog = new WinForms.FolderBrowserDialog
            {
                Description = "选择输出文件夹"
            };
            
            if (folderBrowserDialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                return folderBrowserDialog.SelectedPath;
            }
            
            return null;
        }
        
        public void OpenFileInExplorer(string filePath)
        {
            if (File.Exists(filePath))
            {
                // 打开文件资源管理器并选中文件
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            else if (Directory.Exists(filePath))
            {
                // 打开文件夹
                Process.Start("explorer.exe", $"\"{filePath}\"");
            }
        }
    }
} 