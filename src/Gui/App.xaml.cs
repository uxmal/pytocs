using Avalonia;
using Avalonia.Markup.Xaml;

namespace Pytocs.Gui
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
