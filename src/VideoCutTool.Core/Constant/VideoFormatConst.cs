using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCutTool.Core.Constant
{
    public static class VideoFormatConst
    {
        public const string QualityHigh = "高质量 (1080p)";

        public const int QualityHighValue = 85;

        public const string QualityMedium = "中等质量 (720p)";

        public const int QualityMediumValue = 60;

        public const string QualityLow = "低质量 (480p)";

        public const int QualityLowValue = 40;

        public const string LowFrameRate = "15 fps";

        public const int LowFrameRateValue = 15;

        public const string MediumFrameRate = "30 fps";

        public const int MediumFrameRateValue = 30;

        public const string HighFrameRate = "45 fps";

        public const int HighFrameRateValue = 45;

        public const string MaxFrameRate = "60 fps";

        public const int MaxFrameRateValue = 60;
    }
}
