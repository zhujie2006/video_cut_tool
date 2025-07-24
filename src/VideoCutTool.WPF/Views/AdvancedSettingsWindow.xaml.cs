using System.Windows;
using VideoCutTool.WPF.ViewModels;

namespace VideoCutTool.WPF.Views
{
    /// <summary>
    /// Interaction logic for AdvancedSettingsWindow.xaml
    /// </summary>
    public partial class AdvancedSettingsWindow : Window
    {
        public AdvancedSettingsWindow()
        {
            InitializeComponent();
        }

        private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdvancedSettingsViewModel viewModel)
            {
                viewModel.ResetToDefaults();
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdvancedSettingsViewModel viewModel)
            {
                viewModel.SaveSettings();
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 