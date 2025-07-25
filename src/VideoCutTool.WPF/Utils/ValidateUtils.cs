using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoCutTool.WPF.ViewModels;

namespace VideoCutTool.WPF.Utils
{
    public static class ValidateUtils
    {
        public static bool IsTimelineViewModelValid(this TimelineControlViewModel? vm)
        {
            if (vm == null)
            {
                return false;
            }

            if (vm.CurrentVideo == null || string.IsNullOrEmpty(vm.CurrentVideo.FilePath))
            {
                return false;
            }

            return true;
        }
    }
}
