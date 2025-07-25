using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoCutTool.Core.Constant;

namespace VideoCutTool.WPF.ViewModels
{
    public partial class SettingViewModel : ObservableObject
    {
        /// <summary>
        /// 导出的质量选项
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _exportQualities = new() { VideoFormatConst.QualityHigh, VideoFormatConst.QualityMedium, VideoFormatConst.QualityLow };

        /// <summary>
        /// 导出的格式选项
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _exportFormats = new() { "MP4", "AVI", "MOV", "MKV" };

        /// <summary>
        /// 导出的帧率选项
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _frameRates = new() { VideoFormatConst.LowFrameRate, VideoFormatConst.MediumFrameRate, VideoFormatConst.HighFrameRate, VideoFormatConst.MaxFrameRate };

        /// <summary>
        /// 4.6像素/秒，1分钟=276像素(1x 时)
        /// </summary>
        public static readonly double PIXELS_PER_SECOND = 4.6;

        /// <summary>
        /// 缩略图固定宽度
        /// </summary>
        public static readonly double THUMBNAIL_WIDTH = 64.0;
    }
}
