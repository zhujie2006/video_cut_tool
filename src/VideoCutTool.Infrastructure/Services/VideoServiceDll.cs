#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Serilog;
using VideoCutTool.Core.Interfaces;
using VideoCutTool.Core.Models;

namespace VideoCutTool.Infrastructure.Services;

/// <summary>
/// 基于 FFmpeg.AutoGen 的视频服务实现（DLL 模式）
/// - 剪辑/导出采用无重编码 remux，CPU 占用极低
/// - 多片段拼接按时间范围顺序写入，实现快速导出
/// - 进度回调与 CPU 使用率节流（默认 30%）
/// - 缩略图/波形暂沿用现有外部方式（后续可切 DLL）
/// </summary>
public unsafe class VideoServiceDll : IVideoService
{
    private readonly ILogger _logger = Log.ForContext<VideoServiceDll>();
    private const int CpuUsageLimitPercent = 30;

    static VideoServiceDll()
    {
        FfmpegInitializer.EnsureRegistered();
    }

    public Task<VideoInfo> GetVideoInfoAsync(string filePath)
    {
        return Task.Run(() =>
        {
            _logger.Information("[DLL] 获取视频信息: {File}", filePath);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("视频文件不存在", filePath);
            }

            AVFormatContext* inputCtx = null;
            try
            {
                fixed (AVFormatContext** pInput = &inputCtx)
                {
                    ffmpeg.avformat_open_input(pInput, filePath, null, null).ThrowIfError();
                }
                ffmpeg.avformat_find_stream_info(inputCtx, null).ThrowIfError();

                int videoStreamIndex = -1;
                for (var i = 0; i < inputCtx->nb_streams; i++)
                {
                    if (inputCtx->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                    {
                        videoStreamIndex = i;
                        break;
                    }
                }

                var vi = new VideoInfo
                {
                    FilePath = filePath,
                    Name = Path.GetFileName(filePath),
                    Format = Path.GetExtension(filePath).TrimStart('.'),
                    FileSize = new FileInfo(filePath).Length,
                    CreatedDate = File.GetCreationTime(filePath),
                    ModifiedDate = File.GetLastWriteTime(filePath)
                };

                if (inputCtx->duration != ffmpeg.AV_NOPTS_VALUE)
                {
                    vi.Duration = TimeSpan.FromSeconds(inputCtx->duration / (double)ffmpeg.AV_TIME_BASE);
                }

                if (videoStreamIndex >= 0)
                {
                    var vStream = inputCtx->streams[videoStreamIndex];
                    var par = vStream->codecpar;
                    vi.Width = par->width;
                    vi.Height = par->height;
                    vi.Resolution = $"{par->width}x{par->height}";
                    var fr = vStream->r_frame_rate;
                    if (fr.num > 0 && fr.den > 0)
                    {
                        vi.FrameRate = $"{fr.num / (double)fr.den:0.##} fps";
                    }
                }

                _logger.Information("[DLL] 视频信息: {Resolution}, {Duration}, {FrameRate}", vi.Resolution, vi.Duration, vi.FrameRate);
                return vi;
            }
            finally
            {
                CloseInput(inputCtx);
            }
        });
    }

    public Task<string> SplitVideoAsync(string inputPath, double startTime, double endTime, string outputPath)
    {
        var duration = Math.Max(0, endTime - startTime);
        return ExportByRemuxAsync(inputPath, outputPath, TimeSpan.FromSeconds(startTime), TimeSpan.FromSeconds(duration))
            .ContinueWith(t =>
            {
                if (!t.Result)
                {
                    throw new Exception("分割失败（DLL）");
                }
                return outputPath;
            });
    }

    public Task<bool> ExportSegmentAsync(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration, ExportSettings settings, IProgress<int>? progress = null)
    {
        return ExportByRemuxAsync(inputPath, outputPath, startTime, duration, progress);
    }

    public Task<bool> ExportMultipleSegmentsAsync(string inputPath, string outputPath, IEnumerable<TimelineSegment> segments, ExportSettings settings, IProgress<int>? progress = null)
    {
        return Task.Run(() =>
        {
            var active = segments.Where(s => !s.IsDeleted).OrderBy(s => s.StartTime).ToList();
            if (active.Count == 0) return false;

            _logger.Information("[DLL] 多片段导出（remux）: 片段数 {Count}", active.Count);

            AVFormatContext* inputCtx = null;
            AVFormatContext* outputCtx = null;
            AVIOContext* ioCtx = null;
            var lastProgress = 0;

            try
            {
                fixed (AVFormatContext** pInput = &inputCtx)
                {
                    ffmpeg.avformat_open_input(pInput, inputPath, null, null).ThrowIfError();
                }
                ffmpeg.avformat_find_stream_info(inputCtx, null).ThrowIfError();

                fixed (AVFormatContext** pOut = &outputCtx)
                {
                    ffmpeg.avformat_alloc_output_context2(pOut, null, null, outputPath).ThrowIfError();
                }

                var streamMap = CreateOutputStreamsByCopyingParameters(inputCtx, outputCtx);

                if ((outputCtx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                {
                    AVIOContext* pb;
                    ffmpeg.avio_open(&pb, outputPath, ffmpeg.AVIO_FLAG_WRITE).ThrowIfError();
                    outputCtx->pb = pb;
                    ioCtx = pb;
                }

                ffmpeg.avformat_write_header(outputCtx, null).ThrowIfError();

                var outputTsOffset = new long[inputCtx->nb_streams];
                Array.Fill(outputTsOffset, 0L);

                for (int segIndex = 0; segIndex < active.Count; segIndex++)
                {
                    var seg = active[segIndex];
                    var segStart = seg.StartTime;
                    var segDuration = seg.EndTime - seg.StartTime;

                    RemuxOneSegment(inputCtx, outputCtx, streamMap, segStart, segDuration, ref outputTsOffset);

                    if (progress != null)
                    {
                        var p = (int)Math.Round((segIndex + 1.0) / active.Count * 100);
                        if (p != lastProgress)
                        {
                            lastProgress = p;
                            progress.Report(p);
                        }
                    }
                }

                ffmpeg.av_write_trailer(outputCtx).ThrowIfError();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[DLL] 多片段导出失败");
                return false;
            }
            finally
            {
                if (ioCtx != null)
                {
                    ffmpeg.avio_closep(&ioCtx);
                }
                CloseOutput(outputCtx);
                CloseInput(inputCtx);
            }
        });
    }

    public Task<string> GenerateThumbnailAsync(string videoPath, double time)
    {
        // 暂用现有外部方式，后续可切换为 DLL 解码（不影响剪辑性能）
        var fallback = new VideoService();
        return fallback.GenerateThumbnailAsync(videoPath, time);
    }

    public Task<List<double>> GenerateAudioWaveformAsync(string videoPath, double duration, int dataPointsPerSecond = 10)
    {
        // 暂用现有外部方式，后续可切换为 DLL 解码
        var fallback = new VideoService();
        return fallback.GenerateAudioWaveformAsync(videoPath, duration, dataPointsPerSecond);
    }

    // --- 内部实现 ---

    private Task<bool> ExportByRemuxAsync(string inputPath, string outputPath, TimeSpan start, TimeSpan duration, IProgress<int>? progress = null)
    {
        return Task.Run(() =>
        {
            _logger.Information("[DLL] Remux 导出: {Input} -> {Output} @ {Start}+{Duration}", inputPath, outputPath, start, duration);

            AVFormatContext* inputCtx = null;
            AVFormatContext* outputCtx = null;
            AVIOContext* ioCtx = null;

            try
            {
                fixed (AVFormatContext** pInput = &inputCtx)
                {
                    ffmpeg.avformat_open_input(pInput, inputPath, null, null).ThrowIfError();
                }
                ffmpeg.avformat_find_stream_info(inputCtx, null).ThrowIfError();

                fixed (AVFormatContext** pOut = &outputCtx)
                {
                    ffmpeg.avformat_alloc_output_context2(pOut, null, null, outputPath).ThrowIfError();
                }

                var streamMap = CreateOutputStreamsByCopyingParameters(inputCtx, outputCtx);

                if ((outputCtx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                {
                    AVIOContext* pb;
                    ffmpeg.avio_open(&pb, outputPath, ffmpeg.AVIO_FLAG_WRITE).ThrowIfError();
                    outputCtx->pb = pb;
                    ioCtx = pb;
                }
                ffmpeg.avformat_write_header(outputCtx, null).ThrowIfError();

                var outputTsOffset = new long[inputCtx->nb_streams];
                Array.Fill(outputTsOffset, 0L);

                RemuxOneSegment(inputCtx, outputCtx, streamMap, start, duration, ref outputTsOffset, progress);

                ffmpeg.av_write_trailer(outputCtx).ThrowIfError();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[DLL] Remux 导出失败");
                return false;
            }
            finally
            {
                if (ioCtx != null)
                {
                    ffmpeg.avio_closep(&ioCtx);
                }
                CloseOutput(outputCtx);
                CloseInput(inputCtx);
            }
        });
    }

    private static Dictionary<int, int> CreateOutputStreamsByCopyingParameters(AVFormatContext* inputCtx, AVFormatContext* outputCtx)
    {
        var map = new Dictionary<int, int>();
        for (int i = 0; i < inputCtx->nb_streams; i++)
        {
            var inStream = inputCtx->streams[i];
            var outStream = ffmpeg.avformat_new_stream(outputCtx, null);
            if (outStream == null) throw new ApplicationException("创建输出流失败");
            outStream->time_base = inStream->time_base;
            ffmpeg.avcodec_parameters_copy(outStream->codecpar, inStream->codecpar).ThrowIfError();
            outStream->codecpar->codec_tag = 0;
            map[i] = outStream->index;
        }
        return map;
    }

    private void RemuxOneSegment(
        AVFormatContext* inputCtx,
        AVFormatContext* outputCtx,
        Dictionary<int, int> streamMap,
        TimeSpan start,
        TimeSpan duration,
        ref long[] outputTsOffset,
        IProgress<int>? progress = null)
    {
        int videoStreamIndex = -1;
        for (int i = 0; i < inputCtx->nb_streams; i++)
        {
            if (inputCtx->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
            {
                videoStreamIndex = i; break;
            }
        }
        if (videoStreamIndex < 0) throw new ApplicationException("未找到视频流");

        var vStream = inputCtx->streams[videoStreamIndex];
        long startTs = (long)(start.TotalSeconds / ffmpeg.av_q2d(vStream->time_base));
        long endTs = (long)((start + duration).TotalSeconds / ffmpeg.av_q2d(vStream->time_base));

        ffmpeg.av_seek_frame(inputCtx, videoStreamIndex, startTs, ffmpeg.AVSEEK_FLAG_BACKWARD).ThrowIfError();

        var firstDts = Enumerable.Repeat((long)ffmpeg.AV_NOPTS_VALUE, inputCtx->nb_streams).ToArray();

        var pkt = ffmpeg.av_packet_alloc();
        try
        {
            var throttler = new CpuUsageLimiter(CpuUsageLimitPercent);
            int lastProgress = 0;

            while (ffmpeg.av_read_frame(inputCtx, pkt) >= 0)
            {
                var inIndex = pkt->stream_index;
                var inStream = inputCtx->streams[inIndex];
                var outIndex = streamMap[inIndex];
                var outStream = outputCtx->streams[outIndex];

                if (inIndex == videoStreamIndex)
                {
                    long curTs = pkt->pts == ffmpeg.AV_NOPTS_VALUE ? pkt->dts : pkt->pts;
                    if (curTs != ffmpeg.AV_NOPTS_VALUE && curTs > endTs)
                    {
                        break;
                    }
                }

                if (firstDts[inIndex] == ffmpeg.AV_NOPTS_VALUE)
                {
                    long base = pkt->dts != ffmpeg.AV_NOPTS_VALUE ? pkt->dts : pkt->pts;
                    if (base != ffmpeg.AV_NOPTS_VALUE)
                    {
                        firstDts[inIndex] = base;
                    }
                }

                long pktTs = pkt->pts == ffmpeg.AV_NOPTS_VALUE ? pkt->dts : pkt->pts;
                long startBase = (long)(start.TotalSeconds / ffmpeg.av_q2d(inStream->time_base));
                if (pktTs != ffmpeg.AV_NOPTS_VALUE && pktTs < startBase)
                {
                    ffmpeg.av_packet_unref(pkt);
                    continue;
                }

                if (firstDts[inIndex] != ffmpeg.AV_NOPTS_VALUE)
                {
                    if (pkt->pts != ffmpeg.AV_NOPTS_VALUE) pkt->pts -= firstDts[inIndex];
                    if (pkt->dts != ffmpeg.AV_NOPTS_VALUE) pkt->dts -= firstDts[inIndex];
                }
                if (pkt->pts != ffmpeg.AV_NOPTS_VALUE) pkt->pts += outputTsOffset[inIndex];
                if (pkt->dts != ffmpeg.AV_NOPTS_VALUE) pkt->dts += outputTsOffset[inIndex];

                pkt->pos = -1;

                pkt->pts = ffmpeg.av_rescale_q_rnd(pkt->pts, inStream->time_base, outStream->time_base, ffmpeg.AV_ROUND_NEAR_INF | ffmpeg.AV_ROUND_PASS_MINMAX);
                pkt->dts = ffmpeg.av_rescale_q_rnd(pkt->dts, inStream->time_base, outStream->time_base, ffmpeg.AV_ROUND_NEAR_INF | ffmpeg.AV_ROUND_PASS_MINMAX);
                pkt->duration = (int)ffmpeg.av_rescale_q(pkt->duration, inStream->time_base, outStream->time_base);
                pkt->stream_index = outIndex;

                ffmpeg.av_interleaved_write_frame(outputCtx, pkt).ThrowIfError();

                long last = pkt->dts != ffmpeg.AV_NOPTS_VALUE ? pkt->dts : pkt->pts;
                if (last != ffmpeg.AV_NOPTS_VALUE && last > outputTsOffset[inIndex])
                {
                    outputTsOffset[inIndex] = last + 1;
                }

                if (progress != null && inIndex == videoStreamIndex && endTs > startTs)
                {
                    long cur = pktTs;
                    var percent = cur <= startTs ? 0 : (int)Math.Clamp((cur - startTs) * 100.0 / (endTs - startTs), 0, 100);
                    if (percent != lastProgress)
                    {
                        lastProgress = percent;
                        progress.Report(percent);
                    }
                }

                throttler.MaybeThrottle();

                ffmpeg.av_packet_unref(pkt);
            }
        }
        finally
        {
            if (pkt != null)
            {
                ffmpeg.av_packet_free(&pkt);
            }
        }
    }

    private static void CloseInput(AVFormatContext* ctx)
    {
        if (ctx != null)
        {
            AVFormatContext* tmp = ctx;
            ffmpeg.avformat_close_input(&tmp);
        }
    }

    private static void CloseOutput(AVFormatContext* ctx)
    {
        if (ctx != null)
        {
            if ((ctx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0 && ctx->pb != null)
            {
                var pb = ctx->pb;
                ffmpeg.avio_closep(&pb);
            }
            AVFormatContext* tmp = ctx;
            ffmpeg.avformat_free_context(tmp);
        }
    }
}

internal static class FfmpegInitializer
{
    private static bool _initialized;
    private static readonly object _lock = new();

    public static void EnsureRegistered()
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            // 告知 AutoGen 在何处寻找原生 DLL（发布包中需包含）
            ffmpeg.RootPath = AppContext.BaseDirectory;
            ffmpeg.avformat_network_init();
            _initialized = true;
        }
    }
}

internal sealed class CpuUsageLimiter
{
    private readonly int _limitPercent;
    private readonly System.Diagnostics.Process _proc;
    private TimeSpan _lastTotal;
    private DateTime _lastTime;
    private readonly int _processorCount;

    public CpuUsageLimiter(int limitPercent)
    {
        _limitPercent = Math.Clamp(limitPercent, 1, 100);
        _proc = System.Diagnostics.Process.GetCurrentProcess();
        _lastTotal = _proc.TotalProcessorTime;
        _lastTime = DateTime.UtcNow;
        _processorCount = Environment.ProcessorCount;
    }

    public void MaybeThrottle()
    {
        var now = DateTime.UtcNow;
        var total = _proc.TotalProcessorTime;
        var deltaCpu = (total - _lastTotal).TotalMilliseconds;
        var deltaTime = (now - _lastTime).TotalMilliseconds;
        if (deltaTime <= 0)
        {
            return;
        }
        var cpuPercent = deltaCpu / (deltaTime * _processorCount) * 100.0;
        if (cpuPercent > _limitPercent)
        {
            var excess = cpuPercent - _limitPercent;
            var sleepMs = Math.Clamp((int)(excess / _limitPercent * 10), 1, 15);
            Task.Delay(sleepMs).Wait();
        }
        _lastTime = now;
        _lastTotal = total;
    }
}

internal static class FfmpegErrorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfError(this int error)
    {
        if (error < 0)
        {
            Throw(error);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfErrorSilently(this int error)
    {
        if (error < 0 && error != ffmpeg.AVERROR(ffmpeg.EAGAIN) && error != ffmpeg.AVERROR_EOF)
        {
            Throw(error);
        }
    }

    private static void Throw(int error)
    {
        var buffer = stackalloc byte[1024];
        ffmpeg.av_strerror(error, buffer, 1024);
        var msg = Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"ffmpeg error {error}";
        throw new ApplicationException(msg);
    }
}
