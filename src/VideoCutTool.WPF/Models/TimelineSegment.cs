using CommunityToolkit.Mvvm.ComponentModel;

namespace VideoCutTool.WPF.Models
{
    public partial class TimelineSegment : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;
        
        [ObservableProperty]
        private TimeSpan _startTime;
        
        [ObservableProperty]
        private TimeSpan _endTime;
        
        [ObservableProperty]
        private string _thumbnailPath = string.Empty;
        
        [ObservableProperty]
        private bool _isSelected;
        
        [ObservableProperty]
        private bool _isDeleted;
        
        [ObservableProperty]
        private DateTime _createdDate = DateTime.Now;
        
        public TimeSpan Duration => EndTime - StartTime;
    }
} 