using System.Windows;

namespace MCLauncher
{
    /// <summary>
    /// Interaction logic for DownloadOptionsDialog.xaml
    /// </summary>
    public partial class DownloadOptionsDialog : Window
    {
        public const int MinimumChunkCount = 1;
        public const int MaximumChunkCount = 64;

        public int DownloadChunkCount { get; private set; }

        public bool ManualMsixvcDownloadUrlSelection { get; private set; }

        public DownloadOptionsDialog(int currentChunkCount, bool currentManualMsixvcDownloadUrlSelection)
        {
            InitializeComponent();
            DownloadChunkCount = currentChunkCount;
            ManualMsixvcDownloadUrlSelection = currentManualMsixvcDownloadUrlSelection;
            ChunkCountTextBox.Text = currentChunkCount.ToString();
            ManualMsixvcDownloadUrlSelectionCheckBox.IsChecked = currentManualMsixvcDownloadUrlSelection;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            int chunkCount;
            if (!int.TryParse(ChunkCountTextBox.Text, out chunkCount) ||
                chunkCount < MinimumChunkCount ||
                chunkCount > MaximumChunkCount) {
                MessageBox.Show(
                    "Downloader chunk count must be a number from " + MinimumChunkCount + " to " + MaximumChunkCount + ".",
                    "Invalid download options",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            DownloadChunkCount = chunkCount;
            ManualMsixvcDownloadUrlSelection = ManualMsixvcDownloadUrlSelectionCheckBox.IsChecked ?? false;
            DialogResult = true;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
