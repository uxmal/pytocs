#region License

//  Copyright 2015-2020 John Källén
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

#endregion License

using System;

namespace Pytocs.Core.TypeInference
{
    public class Progress : IProgress
    {
        private const int MAX_SPEED_DIGITS = 5;
        private long count;
        private int lastAvgRate;
        private long lastCount;
        private int lastRate;
        private DateTime lastTickTime;

        private readonly Action<string> msg_;
        private readonly bool quiet;
        private readonly long segSize;
        private readonly DateTime startTime;
        private long total;
        private readonly long width;

        public Progress(Action<string> msg_, long total, long width, bool quiet)
        {
            this.msg_ = msg_;
            startTime = DateTime.Now;
            lastTickTime = DateTime.Now;
            lastCount = 0;
            lastRate = 0;
            lastAvgRate = 0;
            this.total = total;
            this.width = width;
            segSize = total / width;
            if (segSize == 0)
            {
                segSize = 1;
            }

            this.quiet = quiet;
        }

        public void Tick()
        {
            if (!quiet)
            {
                tick(1);
            }
        }

        public void tick(int n)
        {
            count += n;
            if (count > total)
            {
                total = count;
            }

            double elapsed = (DateTime.Now - lastTickTime).TotalMilliseconds;

            if (elapsed > 500 || count == total)
            {
                msg_("\r");
                int dlen = (int)Math.Ceiling(Math.Log10(total));
                msg_(AnalyzerImpl.Percent(count, total) + " (" +
                     FormatNumber(count, dlen) +
                     " of " + FormatNumber(total, dlen) + ")");

                int rate;
                if (elapsed > 1)
                {
                    rate = (int)((count - lastCount) / (elapsed / 1000.0));
                }
                else
                {
                    rate = lastRate;
                }

                lastRate = rate;
                msg_("   SPEED: " + FormatNumber(rate, MAX_SPEED_DIGITS) + "/s");

                double totalElapsed = (DateTime.Now - startTime).TotalMilliseconds;
                int avgRate;

                if (totalElapsed > 1)
                {
                    avgRate = (int)(count / (totalElapsed / 1000.0));
                }
                else
                {
                    avgRate = lastAvgRate;
                }

                avgRate = avgRate == 0 ? 1 : avgRate;

                msg_("   AVG SPEED: " + FormatNumber(avgRate, MAX_SPEED_DIGITS) + "/s");

                long remain = total - count;
                //long remainTime = remain / avgRate * 1000;
                //_.msg_("   ETA: " + _.formatTime(remainTime));

                msg_("       "); // overflow area

                lastTickTime = DateTime.Now;
                lastAvgRate = avgRate;
                lastCount = count;
            }
        }

        /// <summary>
        ///     format number with fixed width
        /// </summary>
        public string FormatNumber(object n, int length)
        {
            if (length == 0)
            {
                length = 1;
            }

            if (n is int)
            {
                return string.Format("{0," + length + "}", (int)n);
            }

            if (n is long)
            {
                return string.Format("{0," + length + "}", (long)n);
            }

            return string.Format("{0," + length + "}", n);
        }
    }
}