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

using Avalonia;

namespace Pytocs.Gui
{
    class Program
    {
        // This method is needed for IDE previewer infrastructure
        public static AppBuilder BuildAvaloniaApp()
          => AppBuilder.Configure<App>().UsePlatformDetect()
                .LogToTrace();

        // The entry point. Things aren't ready yet, so at this point
        // you shouldn't use any Avalonia types or anything that expects
        // a SynchronizationContext to be ready
        public static int Main(string[] args)
        {
            //$DEBUG: Uncomment the following lines to force culture to Russian.
            //var ru = new System.Globalization.CultureInfo("ru");
            //System.Threading.Thread.CurrentThread.CurrentUICulture = ru;
            //System.Threading.Thread.CurrentThread.CurrentCulture = ru;
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
    }
}
