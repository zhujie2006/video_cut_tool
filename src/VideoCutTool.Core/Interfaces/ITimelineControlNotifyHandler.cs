using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCutTool.Core.Interfaces
{
    public interface ITimelineControlNotifyHandler
    {
        /// <summary>
        /// 几个 Canvas 重绘控件内容
        /// </summary>
        public void RefreshControlContent();

        /// <summary>
        /// 只更新切分点和视频段
        /// </summary>
        public void RefreshSegmentAndSplitPoints();
    }
}
