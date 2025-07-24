using System.Diagnostics;
using System.IO;
using VideoCutTool.WPF.Models;
using Serilog;

namespace VideoCutTool.WPF.Services
{
    public class VideoService : IVideoService
    {
        private readonly string _ffmpegPath;
        private readonly string _tempDirectory;
        private readonly ILogger _logger;
        
        public VideoService()
        {
            _logger = Log.ForContext<VideoService>();
            
            // 设置FFmpeg路径（需要确保FFmpeg已安装或包含在项目中）
            _ffmpegPath = "ffmpeg";
            _tempDirectory = Path.Combine(Path.GetTempPath(), "VideoCutTool");
            
            _logger.Information("VideoService 初始化 - FFmpeg路径: {FFmpegPath}, 临时目录: {TempDirectory}", _ffmpegPath, _tempDirectory);
            
            // 确保临时目录存在
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
                _logger.Information("创建临时目录: {TempDirectory}", _tempDirectory);
            }
        }
        
        public async Task<VideoInfo> GetVideoInfoAsync(string filePath)
        {
            _logger.Information("开始获取视频信息: {FilePath}", filePath);
            
            if (!File.Exists(filePath))
            {
                _logger.Error("视频文件不存在: {FilePath}", filePath);
                throw new FileNotFoundException("视频文件不存在", filePath);
            }
            
            var videoInfo = new VideoInfo
            {
                FilePath = filePath,
                Name = Path.GetFileName(filePath),
                Format = Path.GetExtension(filePath).TrimStart('.'),
                CreatedDate = File.GetCreationTime(filePath),
                ModifiedDate = File.GetLastWriteTime(filePath),
                FileSize = new FileInfo(filePath).Length
            };
            
            _logger.Debug("视频基本信息 - 名称: {Name}, 格式: {Format}, 大小: {FileSize} bytes", 
                videoInfo.Name, videoInfo.Format, videoInfo.FileSize);
            
            try
            {
                // 首先尝试使用ffprobe获取视频信息
                var ffprobeArgs = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"";
                _logger.Debug("执行ffprobe命令: {Command} {Args}", "ffprobe", ffprobeArgs);
                
                var result = await ExecuteFFmpegCommandAsync("ffprobe", ffprobeArgs);
                
                if (result.Success)
                {
                    _logger.Debug("ffprobe执行成功，输出长度: {OutputLength}", result.Output?.Length ?? 0);
                    // 解析JSON输出获取视频信息
                    ParseVideoInfo(result.Output, videoInfo);
                }
                else
                {
                    _logger.Warning("ffprobe执行失败，错误信息: {Error}", result.Error);
                    
                    // 如果ffprobe失败，使用ffmpeg获取基本信息
                    var ffmpegArgs = $"-i \"{filePath}\"";
                    _logger.Debug("尝试使用ffmpeg获取信息: {Command} {Args}", _ffmpegPath, ffmpegArgs);
                    
                    var ffmpegResult = await ExecuteFFmpegCommandAsync(_ffmpegPath, ffmpegArgs);
                    
                    if (ffmpegResult.Success)
                    {
                        _logger.Debug("ffmpeg执行成功，错误输出长度: {ErrorLength}", ffmpegResult.Error?.Length ?? 0);
                        ParseVideoInfoFromFFmpeg(ffmpegResult.Error, videoInfo);
                    }
                    else
                    {
                        // 如果都失败了，设置默认值并记录错误
                        _logger.Error("FFmpeg命令执行失败，错误信息: {Error}", ffmpegResult.Error);
                        videoInfo.Duration = TimeSpan.FromMinutes(5); // 默认5分钟
                        videoInfo.Resolution = "1920x1080"; // 默认分辨率
                        videoInfo.FrameRate = "30 fps"; // 默认帧率
                        _logger.Warning("使用默认视频信息 - 时长: {Duration}, 分辨率: {Resolution}, 帧率: {FrameRate}", 
                            videoInfo.Duration, videoInfo.Resolution, videoInfo.FrameRate);
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果出现异常，设置默认值
                _logger.Error(ex, "获取视频信息过程中发生异常");
                videoInfo.Duration = TimeSpan.FromMinutes(5); // 默认5分钟
                videoInfo.Resolution = "1920x1080"; // 默认分辨率
                videoInfo.FrameRate = "30 fps"; // 默认帧率
                _logger.Warning("使用默认视频信息 - 时长: {Duration}, 分辨率: {Resolution}, 帧率: {FrameRate}", 
                    videoInfo.Duration, videoInfo.Resolution, videoInfo.FrameRate);
            }
            
            _logger.Information("视频信息获取完成 - 时长: {Duration}, 分辨率: {Resolution}, 帧率: {FrameRate}", 
                videoInfo.Duration, videoInfo.Resolution, videoInfo.FrameRate);
            
            return videoInfo;
        }
        
        public async Task<bool> SplitVideoAsync(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration)
        {
            var args = $"-i \"{inputPath}\" -ss {startTime.TotalSeconds:F3} -t {duration.TotalSeconds:F3} -c copy \"{outputPath}\"";
            var result = await ExecuteFFmpegCommandAsync(_ffmpegPath, args);
            return result.Success;
        }
        
        public async Task<bool> ExportSegmentAsync(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration, ExportSettings settings, IProgress<int>? progress = null)
        {
            // 构建FFmpeg参数
            var args = BuildExportArgs(inputPath, outputPath, startTime, duration, settings);
            var result = await ExecuteFFmpegCommandAsync(_ffmpegPath, args, progress);
            return result.Success;
        }
        
        public async Task<string> GenerateThumbnailAsync(string videoPath, TimeSpan time, string outputPath)
        {
            var args = $"-i \"{videoPath}\" -ss {time.TotalSeconds:F3} -vframes 1 -q:v 2 \"{outputPath}\"";
            var result = await ExecuteFFmpegCommandAsync(_ffmpegPath, args);
            
            if (result.Success && File.Exists(outputPath))
            {
                return outputPath;
            }
            
            return string.Empty;
        }
        
        private async Task<(bool Success, string Output, string Error)> ExecuteFFmpegCommandAsync(string command, string arguments, IProgress<int>? progress = null)
        {
            _logger.Debug("执行FFmpeg命令: {Command} {Arguments}", command, arguments);
            
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                _logger.Debug("进程启动信息 - 文件名: {FileName}, 参数: {Arguments}", startInfo.FileName, startInfo.Arguments);
                
                using var process = new Process { StartInfo = startInfo };
                var output = new System.Text.StringBuilder();
                var error = new System.Text.StringBuilder();
                
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        output.AppendLine(e.Data);
                        
                        // 解析进度信息（如果存在）
                        if (progress != null && e.Data.Contains("time="))
                        {
                            ParseProgress(e.Data, progress);
                        }
                    }
                };
                
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        error.AppendLine(e.Data);
                    }
                };
                
                _logger.Debug("启动进程");
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                await process.WaitForExitAsync();
                
                var success = process.ExitCode == 0;
                var outputStr = output.ToString();
                var errorStr = error.ToString();
                
                _logger.Debug("进程执行完成 - 退出码: {ExitCode}, 成功: {Success}, 输出长度: {OutputLength}, 错误长度: {ErrorLength}", 
                    process.ExitCode, success, outputStr.Length, errorStr.Length);
                
                if (!success)
                {
                    _logger.Warning("FFmpeg命令执行失败 - 退出码: {ExitCode}, 错误信息: {Error}", process.ExitCode, errorStr);
                }
                
                return (success, outputStr, errorStr);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "执行FFmpeg命令时发生异常");
                return (false, string.Empty, ex.Message);
            }
        }
        
        private void ParseVideoInfo(string jsonOutput, VideoInfo videoInfo)
        {
            _logger.Debug("开始解析JSON视频信息，JSON长度: {JsonLength}", jsonOutput?.Length ?? 0);
            
            try
            {
                if (string.IsNullOrEmpty(jsonOutput))
                {
                    _logger.Warning("JSON输出为空");
                    return;
                }
                
                // 记录JSON的前200个字符用于调试
                _logger.Debug("JSON输出前200字符: {JsonPreview}", jsonOutput.Substring(0, Math.Min(200, jsonOutput.Length)));
                
                // 查找format部分（时长通常在这里）
                if (jsonOutput.Contains("\"format\""))
                {
                    var formatStart = jsonOutput.IndexOf("\"format\"");
                    var formatEnd = jsonOutput.IndexOf("}", formatStart);
                    if (formatEnd > formatStart)
                    {
                        var formatSection = jsonOutput.Substring(formatStart, formatEnd - formatStart + 1);
                        _logger.Debug("Format部分: {FormatSection}", formatSection);
                    }
                }
                
                // 解析时长
                if (jsonOutput.Contains("\"duration\""))
                {
                    var durationMatch = System.Text.RegularExpressions.Regex.Match(jsonOutput, "\"duration\":\\s*\"([^\"]+)\"");
                    if (durationMatch.Success)
                    {
                        var durationStr = durationMatch.Groups[1].Value;
                        _logger.Debug("找到时长字符串: {DurationString}", durationStr);
                        
                        if (double.TryParse(durationStr, out var duration))
                        {
                            videoInfo.Duration = TimeSpan.FromSeconds(duration);
                            _logger.Debug("解析时长成功: {Duration}", videoInfo.Duration);
                        }
                        else
                        {
                            _logger.Warning("无法解析时长字符串: {DurationString}", durationStr);
                        }
                    }
                    else
                    {
                        _logger.Warning("未找到时长匹配项");
                    }
                }
                else
                {
                    _logger.Warning("JSON中未包含duration字段");
                }
                
                // 解析分辨率 - 查找视频流中的分辨率
                var videoStreamMatch = System.Text.RegularExpressions.Regex.Match(jsonOutput, "\"streams\":\\s*\\[\\s*\\{[^}]*\"codec_type\":\\s*\"video\"[^}]*\"width\":\\s*(\\d+)[^}]*\"height\":\\s*(\\d+)");
                if (videoStreamMatch.Success)
                {
                    var width = videoStreamMatch.Groups[1].Value;
                    var height = videoStreamMatch.Groups[2].Value;
                    videoInfo.Resolution = $"{width}x{height}";
                    _logger.Debug("解析分辨率成功: {Resolution}", videoInfo.Resolution);
                }
                else
                {
                    // 尝试更简单的匹配
                    var widthMatch = System.Text.RegularExpressions.Regex.Match(jsonOutput, "\"width\":\\s*(\\d+)");
                    var heightMatch = System.Text.RegularExpressions.Regex.Match(jsonOutput, "\"height\":\\s*(\\d+)");
                    if (widthMatch.Success && heightMatch.Success)
                    {
                        videoInfo.Resolution = $"{widthMatch.Groups[1].Value}x{heightMatch.Groups[1].Value}";
                        _logger.Debug("解析分辨率成功(简单匹配): {Resolution}", videoInfo.Resolution);
                    }
                    else
                    {
                        _logger.Warning("未找到分辨率信息");
                    }
                }
                
                // 解析帧率
                if (jsonOutput.Contains("\"r_frame_rate\""))
                {
                    var frameRateMatch = System.Text.RegularExpressions.Regex.Match(jsonOutput, "\"r_frame_rate\":\\s*\"([^\"]+)\"");
                    if (frameRateMatch.Success)
                    {
                        videoInfo.FrameRate = frameRateMatch.Groups[1].Value;
                        _logger.Debug("解析帧率成功: {FrameRate}", videoInfo.FrameRate);
                    }
                    else
                    {
                        _logger.Warning("未找到帧率匹配项");
                    }
                }
                else
                {
                    _logger.Warning("JSON中未包含r_frame_rate字段");
                }
                
                _logger.Information("JSON解析完成 - 时长: {Duration}, 分辨率: {Resolution}, 帧率: {FrameRate}", 
                    videoInfo.Duration, videoInfo.Resolution, videoInfo.FrameRate);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "JSON解析过程中发生异常");
                // 解析失败时使用默认值
                videoInfo.Duration = TimeSpan.Zero;
                videoInfo.Resolution = "未知";
                videoInfo.FrameRate = "未知";
            }
        }
        
        private void ParseVideoInfoFromFFmpeg(string ffmpegOutput, VideoInfo videoInfo)
        {
            try
            {
                // 解析FFmpeg的输出信息
                var lines = ffmpegOutput.Split('\n');
                
                foreach (var line in lines)
                {
                    // 解析时长
                    if (line.Contains("Duration:"))
                    {
                        var durationMatch = System.Text.RegularExpressions.Regex.Match(line, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
                        if (durationMatch.Success)
                        {
                            var hours = int.Parse(durationMatch.Groups[1].Value);
                            var minutes = int.Parse(durationMatch.Groups[2].Value);
                            var seconds = double.Parse(durationMatch.Groups[3].Value);
                            videoInfo.Duration = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
                        }
                    }
                    
                    // 解析分辨率
                    if (line.Contains("Video:") && line.Contains("x"))
                    {
                        var resolutionMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d{3,4})x(\d{3,4})");
                        if (resolutionMatch.Success)
                        {
                            videoInfo.Resolution = $"{resolutionMatch.Groups[1].Value}x{resolutionMatch.Groups[2].Value}";
                        }
                    }
                    
                    // 解析帧率
                    if (line.Contains("fps"))
                    {
                        var fpsMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+(?:\.\d+)?)\s*fps");
                        if (fpsMatch.Success)
                        {
                            videoInfo.FrameRate = $"{fpsMatch.Groups[1].Value} fps";
                        }
                    }
                }
            }
            catch
            {
                // 解析失败时使用默认值
                videoInfo.Duration = TimeSpan.Zero;
                videoInfo.Resolution = "未知";
                videoInfo.FrameRate = "未知";
            }
        }
        
        private string BuildExportArgs(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration, ExportSettings settings)
        {
            var args = $"-i \"{inputPath}\" -ss {startTime.TotalSeconds:F3} -t {duration.TotalSeconds:F3}";
            
            // 根据质量设置编码参数
            switch (settings.Quality)
            {
                case "高质量 (1080p)":
                    args += " -vf scale=1920:1080 -c:v libx264 -crf 18";
                    break;
                case "中等质量 (720p)":
                    args += " -vf scale=1280:720 -c:v libx264 -crf 23";
                    break;
                case "低质量 (480p)":
                    args += " -vf scale=854:480 -c:v libx264 -crf 28";
                    break;
            }
            
            // 设置帧率
            var frameRate = settings.FrameRate.Replace(" fps", "");
            args += $" -r {frameRate}";
            
            // 设置音频编码
            args += " -c:a aac -b:a 128k";
            
            // 输出文件
            args += $" \"{outputPath}\"";
            
            return args;
        }
        
        private void ParseProgress(string output, IProgress<int> progress)
        {
            try
            {
                // 解析FFmpeg的进度输出
                var timeMatch = System.Text.RegularExpressions.Regex.Match(output, "time=(\\d{2}):(\\d{2}):(\\d{2}\\.\\d{2})");
                if (timeMatch.Success)
                {
                    var hours = int.Parse(timeMatch.Groups[1].Value);
                    var minutes = int.Parse(timeMatch.Groups[2].Value);
                    var seconds = double.Parse(timeMatch.Groups[3].Value);
                    var currentTime = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
                    
                    // 这里需要知道总时长来计算百分比
                    // 暂时使用一个估算值
                    var estimatedProgress = (int)((currentTime.TotalSeconds / 60.0) * 100); // 假设1分钟视频
                    progress.Report(Math.Min(estimatedProgress, 100));
                }
            }
            catch
            {
                // 解析失败时不报告进度
            }
        }
    }
} 