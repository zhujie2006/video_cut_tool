using System.Text.Json;
using VideoCutTool.Core.Models;
using VideoCutTool.Core.Interfaces;
using Serilog;
using System.IO;

namespace VideoCutTool.Infrastructure.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ILogger _logger;
        private readonly string _recentProjectsPath;

        public ProjectService()
        {
            _logger = Log.ForContext<ProjectService>();
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoCutTool");
            _recentProjectsPath = Path.Combine(appDataPath, "recent_projects.json");
        }

        public async Task<bool> SaveProjectAsync(ProjectInfo projectFile, string filePath)
        {
            try
            {
                _logger.Information("开始保存项目到: {FilePath}", filePath);
                
                projectFile.LastModifiedDate = DateTime.Now;
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(projectFile, options);
                await File.WriteAllTextAsync(filePath, json);
                
                // 保存到最近项目列表
                await SaveRecentProjectAsync(filePath);
                
                _logger.Information("项目保存成功: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存项目失败: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<ProjectInfo> LoadProjectAsync(string filePath)
        {
            try
            {
                _logger.Information("开始加载项目: {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.Warning("项目文件不存在: {FilePath}", filePath);
                    return null;
                }
                
                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var project = JsonSerializer.Deserialize<ProjectInfo>(json, options);
                
                if (project != null)
                {
                    // 保存到最近项目列表
                    await SaveRecentProjectAsync(filePath);
                    _logger.Information("项目加载成功: {FilePath}", filePath);
                }
                
                return project;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载项目失败: {FilePath}", filePath);
                return null;
            }
        }

        public async Task<List<ProjectInfo>> GetRecentProjectsAsync()
        {
            try
            {
                if (!File.Exists(_recentProjectsPath))
                {
                    return new List<ProjectInfo>();
                }
                
                var json = await File.ReadAllTextAsync(_recentProjectsPath);
                var recentPaths = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                
                var recentProjects = new List<ProjectInfo>();
                foreach (var path in recentPaths.Take(10)) // 只保留最近10个项目
                {
                    if (File.Exists(path))
                    {
                        var project = await LoadProjectAsync(path);
                        if (project != null)
                        {
                            recentProjects.Add(project);
                        }
                    }
                }
                
                return recentProjects;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取最近项目列表失败");
                return new List<ProjectInfo>();
            }
        }

        public async Task SaveRecentProjectAsync(string filePath)
        {
            try
            {
                var appDataPath = Path.GetDirectoryName(_recentProjectsPath);
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath!);
                }
                
                var recentPaths = new List<string>();
                if (File.Exists(_recentProjectsPath))
                {
                    var json = await File.ReadAllTextAsync(_recentProjectsPath);
                    recentPaths = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
                
                // 移除重复项并添加到开头
                recentPaths.Remove(filePath);
                recentPaths.Insert(0, filePath);
                
                // 只保留最近20个项目
                recentPaths = recentPaths.Take(20).ToList();
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = JsonSerializer.Serialize(recentPaths, options);
                await File.WriteAllTextAsync(_recentProjectsPath, updatedJson);
                
                _logger.Debug("最近项目列表已更新: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存最近项目列表失败");
            }
        }

        public async Task<bool> ValidateProjectFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var project = JsonSerializer.Deserialize<ProjectInfo>(json);
                return project != null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "验证项目文件失败: {FilePath}", filePath);
                return false;
            }
        }

    }
} 