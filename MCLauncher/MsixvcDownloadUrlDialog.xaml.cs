using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MCLauncher
{
    /// <summary>
    /// Interaction logic for MsixvcDownloadUrlDialog.xaml
    /// </summary>
    public partial class MsixvcDownloadUrlDialog : Window
    {
        public string SelectedUrl { get; private set; }

        public MsixvcDownloadUrlDialog(string versionName, IList<string> downloadUrls)
        {
            InitializeComponent();
            VersionLabel.Text = versionName;

            var choices = downloadUrls
                .Select((url, index) => new DownloadUrlChoice(index + 1, url))
                .ToList();
            UrlsListBox.ItemsSource = choices;
            OkButton.IsEnabled = choices.Count > 0;

            if (choices.Count > 0) {
                UrlsListBox.SelectedIndex = 0;
            }
        }

        private void UrlsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var choice = UrlsListBox.SelectedItem as DownloadUrlChoice;
            SelectedUrlTextBox.Text = choice?.Url ?? "";
        }

        private void UrlsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AcceptSelection();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            AcceptSelection();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AcceptSelection()
        {
            var choice = UrlsListBox.SelectedItem as DownloadUrlChoice;
            if (choice == null) {
                return;
            }

            SelectedUrl = choice.Url;
            DialogResult = true;
            Close();
        }

        private class DownloadUrlChoice
        {
            public string DisplayName { get; }

            public string Url { get; }

            public DownloadUrlChoice(int index, string url)
            {
                Url = url;
                DisplayName = BuildDisplayName(index, url);
            }

            private static string BuildDisplayName(int index, string url)
            {
                try {
                    var uri = new Uri(url);
                    var fileName = Path.GetFileName(uri.AbsolutePath);
                    if (!string.IsNullOrEmpty(fileName)) {
                        return index + ". " + uri.Host + " - " + Uri.UnescapeDataString(fileName);
                    }
                    return index + ". " + uri.Host + uri.AbsolutePath;
                } catch (UriFormatException) {
                    return index + ". " + url;
                }
            }
        }
    }
}
