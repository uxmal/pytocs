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

using Avalonia;
using Avalonia.Logging.Serilog;

namespace Pytocs.Gui
{
    class Program
    {
        static void Main(string[] args)
        {
            //$DEBUG: Uncomment the following lines to force culture to Russian.
            var ru = new System.Globalization.CultureInfo("ru");
            System.Threading.Thread.CurrentThread.CurrentUICulture = ru;
            System.Threading.Thread.CurrentThread.CurrentCulture = ru;

            BuildAvaloniaApp().Start<MainWindow>();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();
    }
}
