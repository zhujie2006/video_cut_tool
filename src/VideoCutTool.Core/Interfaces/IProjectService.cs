using VideoCutTool.Core.Models;

namespace VideoCutTool.Core.Interfaces
{
    /// <summary>
    /// 项目服务接口
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// 保存项目
        /// </summary>
        /// <param name="project">项目文件</param>
        /// <param name="filePath">保存路径</param>
        /// <returns>是否成功</returns>
        Task<bool> SaveProjectAsync(ProjectInfo project, string filePath);

        /// <summary>
        /// 加载项目
        /// </summary>
        /// <param name="filePath">项目文件路径</param>
        /// <returns>项目文件</returns>
        Task<ProjectInfo?> LoadProjectAsync(string filePath);

        /// <summary>
        /// 获取最近项目文件
        /// </summary>
        /// <returns>项目文件列表</returns>
        Task<List<ProjectInfo>> GetRecentProjectsAsync();

        /// <summary>
        /// 保存最新的项目
        /// </summary>
        /// <param name="filePath">视频文件路径</param>
        Task SaveRecentProjectAsync(string filePath);
    }
}