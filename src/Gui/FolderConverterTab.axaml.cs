#region License
//  Copyright 2015-2018 John Källén
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Pytocs.Gui
{
    public class FolderConverterTab : UserControl
    {
        private TextBox TargetFolderBox { get; }

        private TextBox SourceFolderBox { get; }

        private TextBox ConversionLogBox { get; }

        private Button ConvertButton { get; }

        private Func<string, Task> AppendLog { get; }

        public FolderConverterTab()
        {
            this.InitializeComponent();
            SourceFolderBox = this.FindControl<TextBox>(nameof(SourceFolderBox));
            TargetFolderBox = this.FindControl<TextBox>(nameof(TargetFolderBox));
            ConversionLogBox = this.FindControl<TextBox>(nameof(ConversionLogBox));
            ConvertButton = this.FindControl<Button>(nameof(ConvertButton));

            AppendLog = x => Dispatcher.UIThread.InvokeAsync(() => ConversionLogBox.Text += x);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }


        private async void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is not Window window)
                return;
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(window);

            if (!string.IsNullOrWhiteSpace(result))
            {
                SourceFolderBox.Text = result;
            }
        }

        private async void BrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is not Window window)
                return;

            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(window);

            if (!string.IsNullOrWhiteSpace(result))
            {
                TargetFolderBox.Text = result;
            }
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            ConvertButton.IsEnabled = false;

            var (sourceFolder, targetFolder) = GetValidConversionFolders();

            if (sourceFolder == null || targetFolder == null)
            {
                ConvertButton.IsEnabled = true;
                return;
            }

            ConversionLogBox.Text = string.Empty;

            await ConversionUtils.ConvertFolderAsync(sourceFolder, targetFolder, new DelegateLogger(AppendLog));

            ConvertButton.IsEnabled = true;
        }

        private (string? sourceFolder, string? targetFolder) GetValidConversionFolders()
        {
            if (string.IsNullOrWhiteSpace(SourceFolderBox.Text))
            {
                ConversionLogBox.Text = Gui.Resources.ErrSourceDirectoryPathEmpty;
                return (null, null);
            }

            if (string.IsNullOrWhiteSpace(TargetFolderBox.Text))
            {
                ConversionLogBox.Text = Gui.Resources.ErrTargetDirectoryPathEmpty;
                return (null, null);
            }


            var sourceFolder = Path.GetFullPath(SourceFolderBox.Text);
            var targetFolder = Path.GetFullPath(TargetFolderBox.Text);

            SourceFolderBox.Text = sourceFolder;
            TargetFolderBox.Text = targetFolder;

            if (!Directory.Exists(sourceFolder))
            {
                ConversionLogBox.Text = Gui.Resources.ErrInvalidSourceDirectoryPath;
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
                    ConversionLogBox.Text = string.Format(
                        Gui.Resources.ErrCouldntCreateTargetDirectory,
                        targetFolder,
                        ex.Message);
                    return (null, null);
                }
            }

            return (sourceFolder, targetFolder);
        }
    }
}
