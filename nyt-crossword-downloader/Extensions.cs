using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nyt_crossword_downloader
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    static internal class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string PadTwo(this int x) => x.ToString().PadLeft(2, '0');
    }
}
