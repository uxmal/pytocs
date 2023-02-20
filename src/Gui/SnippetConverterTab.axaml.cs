#region License
//  Copyright 2015-2021 John Källén
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Pytocs.Core;
using Pytocs.Core.TypeInference;

namespace Pytocs.Gui
{
    public class SnippetConverterTab : UserControl
    {
        private TextBox PythonEditor { get; }
        private TextBox CSharpEditor { get; }

        public SnippetConverterTab()
        {
            this.InitializeComponent();

            PythonEditor = this.FindControl<TextBox>(nameof(PythonEditor));
            CSharpEditor = this.FindControl<TextBox>(nameof(CSharpEditor));
            PythonEditor.Text = string.Empty;
            CSharpEditor.Text = string.Empty;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var postProcessors = new List<IPostProcessor>();
                var xlator = new Translator("", "Program", postProcessors, new FileSystem(), new ConsoleLogger());
                CSharpEditor.Text = xlator.TranslateSnippet(PythonEditor.Text);
            }
            catch (Exception ex)
            {
                CSharpEditor.Text = string.Format(
                    Gui.Resources.ConversionError,
                    Environment.NewLine,
                    ex.Message);
            }
        }

        private async void InsertFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            var files = await dialog.ShowAsync((Window) VisualRoot!);
            if (files is null || files.Length == 0)
                return;
            var fileName = files[0];
            if (fileName is not null)
            {
                PythonEditor.Text = File.ReadAllText(fileName);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            PythonEditor.Text = string.Empty;
        }
        
        private void SelectAndCopyAll_Click(object sender, RoutedEventArgs e)
        {
            TextCopy.ClipboardService.SetText(CSharpEditor.Text);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();

            var fileName = await dialog.ShowAsync((Window)VisualRoot!);

            if (fileName != null)
            {
                File.WriteAllText(fileName, CSharpEditor.Text);
            }
        }
    }
}
