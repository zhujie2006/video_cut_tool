namespace VideoCutTool.Core.Interfaces
{
    /// <summary>
    /// 文件对话框服务接口
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>
        /// 选择视频文件
        /// </summary>
        string? SelectVideoFile();
        
        /// <summary>
        /// 选择保存文件
        /// </summary>
        string? SelectSaveFile(string defaultName, string filter);
        
        /// <summary>
        /// 选择文件夹
        /// </summary>
        string? SelectFolder();
        
        /// <summary>
        /// 打开文件资源管理器并选中文件
        /// </summary>
        void OpenFileInExplorer(string filePath);
        
        /// <summary>
        /// 保存项目文件
        /// <param name="defaultName">默认文件名</param>
        /// </summary>
        string? SaveProjectFile(string defaultName);
        
        /// <summary>
        /// 打开项目文件
        /// </summary>
        string? OpenProjectFile();
    }
}