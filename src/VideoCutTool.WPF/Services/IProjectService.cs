using VideoCutTool.WPF.Models;

namespace VideoCutTool.WPF.Services
{
    public interface IProjectService
    {
        Task<bool> SaveProjectAsync(ProjectFile project, string filePath);
        Task<ProjectFile?> LoadProjectAsync(string filePath);
        Task<List<ProjectFile>> GetRecentProjectsAsync();
        Task SaveRecentProjectAsync(string filePath);
    }
} 