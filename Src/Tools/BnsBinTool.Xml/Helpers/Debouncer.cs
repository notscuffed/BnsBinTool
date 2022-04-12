using System;
using System.Threading;

namespace BnsBinTool.Xml.Helpers
{
    public static class Debouncer
    {
        public static Action<T> Debounce<T>(Action<T> action, TimeSpan timeSpan)
        {
            Timer timer = null;

            return arg =>
            {
                if (timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }

                timer = new Timer(_ => action(arg), null, timeSpan, TimeSpan.FromMilliseconds(-1));
            };
        }
    }
}