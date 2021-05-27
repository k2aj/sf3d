using System;
using System.Collections.Generic;

namespace DGL
{
    internal class DebugUtils
    {
        private static HashSet<string> stackTraces = new();
        public static void LogUniqueStackTrace(string message, int extraSkippedStackFrames=0)
        {
            var stackTrace = System.Environment.StackTrace;
            if(stackTraces.Add(stackTrace))
            {
                Console.WriteLine(message);

                // Slightly declutter stack trace by ommiting this method and everything below in the call stack
                int splitPos = 0;
                for(int lineFeeds=0; lineFeeds < extraSkippedStackFrames+2; ++splitPos)
                    if(stackTrace[splitPos] == '\n') 
                        ++lineFeeds;

                Console.WriteLine(stackTrace.Substring(splitPos));
            }
        }
    }
}