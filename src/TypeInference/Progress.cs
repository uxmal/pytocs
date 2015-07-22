using System;

namespace Pytocs.TypeInference
{
    public class Progress
    {
        private const int MAX_SPEED_DIGITS = 5;

        private Analyzer analyzer;
        private DateTime startTime;
        private DateTime lastTickTime;
        private long lastCount;
        private int lastRate;
        private int lastAvgRate;
        private long total;
        private long count;
        private long width;
        private long segSize;
        private bool quiet;

        public Progress(Analyzer analyzer, long total, long width, bool quiet)
        {
            this.analyzer = analyzer;
            this.startTime = DateTime.Now;
            this.lastTickTime = DateTime.Now;
            this.lastCount = 0;
            this.lastRate = 0;
            this.lastAvgRate = 0;
            this.total = total;
            this.width = width;
            this.segSize = total / width;
            if (segSize == 0)
            {
                segSize = 1;
            }
            this.quiet = quiet;
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
                analyzer.msg_("\r");
                int dlen = (int) Math.Ceiling(Math.Log10((double) total));
                analyzer.msg_(analyzer.percent(count, total) + " (" +
                        analyzer.formatNumber(count, dlen) +
                        " of " + analyzer.formatNumber(total, dlen) + ")");

                int rate;
                if (elapsed > 1)
                {
                    rate = (int) ((count - lastCount) / (elapsed / 1000.0));
                }
                else
                {
                    rate = lastRate;
                }

                lastRate = rate;
                analyzer.msg_("   SPEED: " + analyzer.formatNumber(rate, MAX_SPEED_DIGITS) + "/s");

                double totalElapsed = (DateTime.Now - startTime).TotalMilliseconds;
                int avgRate;

                if (totalElapsed > 1)
                {
                    avgRate = (int) (count / (totalElapsed / 1000.0));
                }
                else
                {
                    avgRate = lastAvgRate;
                }
                avgRate = avgRate == 0 ? 1 : avgRate;

                analyzer.msg_("   AVG SPEED: " + analyzer.formatNumber(avgRate, MAX_SPEED_DIGITS) + "/s");

                long remain = total - count;
                //long remainTime = remain / avgRate * 1000;
                //_.msg_("   ETA: " + _.formatTime(remainTime));


                analyzer.msg_("       ");      // overflow area

                lastTickTime = DateTime.Now;
                lastAvgRate = avgRate;
                lastCount = count;
            }
        }


        public void tick()
        {
            if (!quiet)
            {
                tick(1);
            }
        }
    }
}
