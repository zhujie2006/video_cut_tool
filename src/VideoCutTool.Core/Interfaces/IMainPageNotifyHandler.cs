using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCutTool.Core.Interfaces
{
    public interface IMainPageNotifyHandler
    {
        /// <summary>
        /// 通知界面显示状态信息
        /// </summary>
        /// <param name="message">信息内容</param>
        public void NotifyStatusMessage(string message);

        /// <summary>
        /// 通知界面更新项目信息
        /// </summary>
        public void UpdateProjectInfo();

        /// <summary>
        /// 设置当前时间
        /// </summary>
        public void SetCurrentTime(TimeSpan time);

        /// <summary>
        /// 获取当前时间
        /// </summary>
        public TimeSpan GetCurrentTime();
    }
}
