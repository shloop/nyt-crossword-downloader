using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace nyt_crossword_downloader
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="url"></param>
    /// <returns></returns>
    delegate Task<T> DownloadMethod<T>(string url);

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    enum PuzzleType
    {
        None = 0,
        Daily = 1,
        Mini = 2,
        Bonus = 4,
        All = Daily | Mini | Bonus
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    enum PuzzleFormat
    {
        None = 0,
        Json = 1,
        Pdf = 2,
        Answers = 4,
        Newspaper = 8,
        All = Json | Pdf | Answers | Newspaper
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ID"></param>
    /// <param name="Date"></param>
    class PuzzleInfo
    {
        [JsonPropertyName("puzzle_id")]
        public int ID { get; set; }
        [JsonPropertyName("print_date")]
        public string? DateString { get; set; }

        private bool InvalidDate => DateString == null || DateString.Length != 10;

        public int Year => InvalidDate ? -1 : int.Parse(DateString![..4]);

        public int Month => InvalidDate ? -1 : int.Parse(DateString!.Substring(5, 2));

        public int Day => InvalidDate ? -1 : int.Parse(DateString!.Substring(8, 2));

        private DateTime Date => InvalidDate ? DateTime.MaxValue : new(Year, Month, Day);

        public bool IsInRange(DateTime start, DateTime end) => InvalidDate || ((Date >= start) && (Date <= end));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Results"></param>
    record class PuzzleSet(
        [property: JsonPropertyName("results")] PuzzleInfo[] Results);

}
