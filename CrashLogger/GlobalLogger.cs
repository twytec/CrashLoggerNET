using System;
using System.Collections.Generic;
using System.Text;

namespace CrashLogger
{
    /// <summary>
    /// 
    /// </summary>
    public static class GlobalLogger
    {
        /// <summary>
        /// Static Logger. Init before use
        /// </summary>
        public static Logger Log { get; set; }

        /// <summary>
        /// Init before use
        /// </summary>
        /// <param name="appGuidId">AppGuidId</param>
        public static void Init(string appGuidId)
        {
            Log = new Logger(appGuidId);
        }
    }
}
