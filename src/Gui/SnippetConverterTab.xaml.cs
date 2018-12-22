using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Pytocs.TypeInference;

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
                var xlator = new Translator("", "Program", null, new ConsoleLogger());
                CSharpEditor.Text = xlator.TranslateSnippet(PythonEditor.Text);
            }
            catch (Exception ex)
            {
                //todo: use a configuration file for UI message
                CSharpEditor.Text = "// Conversion error!\n// " + ex.Message;
            }
        }

        private async void InsertFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();

            var fileName = (await dialog.ShowAsync()).FirstOrDefault();

            if (fileName != null)
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
            TextCopy.Clipboard.SetText(CSharpEditor.Text);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();

            var fileName = await dialog.ShowAsync(null);

            if (fileName != null)
            {
                File.WriteAllText(fileName, CSharpEditor.Text);
            }
        }
    }
}
