using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Pytocs.Gui
{
    public class FolderConverterTab : UserControl
    {
        private object SyncRoot { get; } = new object();

        private bool _isConversionInProgress;

        private TextBox TargetFolderBox { get; }

        private TextBox SourceFolderBox { get; }

        private TextBox ConversionLogBox { get; }

        private Func<string, Task> AppendLog { get; }

        public FolderConverterTab()
        {
            this.InitializeComponent();
            SourceFolderBox = this.FindControl<TextBox>(nameof(SourceFolderBox));
            TargetFolderBox = this.FindControl<TextBox>(nameof(TargetFolderBox));
            ConversionLogBox = this.FindControl<TextBox>(nameof(ConversionLogBox));

            AppendLog = x => Dispatcher.UIThread.InvokeAsync(() => ConversionLogBox.Text += x);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync();

            if (!string.IsNullOrWhiteSpace(result))
            {
                SourceFolderBox.Text = result;
            }
        }

        private async void BrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync();

            if (!string.IsNullOrWhiteSpace(result))
            {
                TargetFolderBox.Text = result;
            }
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            var (sourceFolder, targetFolder) = GetValidPaths();

            if (sourceFolder == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_isConversionInProgress)
                {
                    return;
                }

                _isConversionInProgress = true;
            }

            ConversionLogBox.Text = string.Empty;

            await ConversionUtils.ConvertFolder(sourceFolder, targetFolder, new DelegateLogger(AppendLog));

            lock (SyncRoot)
            {
                _isConversionInProgress = false;
            }
        }

        private (string, string) GetValidPaths()
        {
            if (string.IsNullOrWhiteSpace(SourceFolderBox.Text))
            {
                //todo: use a configuration file for UI message
                ConversionLogBox.Text = "Error! Source directory path is empty";
                return (null, null);
            }

            if (string.IsNullOrWhiteSpace(TargetFolderBox.Text))
            {
                //todo: use a configuration file for UI message
                ConversionLogBox.Text = "Error! Target directory path is empty";
                return (null, null);
            }


            var sourceFolder = Path.GetFullPath(SourceFolderBox.Text);
            var targetFolder = Path.GetFullPath(TargetFolderBox.Text);

            SourceFolderBox.Text = sourceFolder;
            TargetFolderBox.Text = targetFolder;

            if (!Directory.Exists(sourceFolder))
            {
                //todo: use a configuration file for UI message
                ConversionLogBox.Text = "Error! Invalid source directory path";
                return (null, null);
            }

            if (!Directory.Exists(targetFolder))
            {
                try
                {
                    Directory.CreateDirectory(targetFolder);
                }
                catch (Exception ex)
                {
                    //todo: use a configuration file for UI message
                    ConversionLogBox.Text = "Error! Couldn't create target directory\n" + ex.Message;
                    return (null, null);
                }
            }

            return (sourceFolder, targetFolder);
        }
    }
}
