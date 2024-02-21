using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace nyt_crossword_downloader
{

    /// <summary>
    /// Methods for getting API endpoints for fetching puzzles or related info.
    /// </summary>
    static class Routing
    {

        const string pdfPrefix = "https://www.nytimes.com/svc/crosswords/v2/puzzle";
        static readonly string[] monthAbbreviations = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        /// <summary>
        /// Gets the URL for a given puzzle in the desired format.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="printDate"></param>
        /// <returns></returns>
        public static string GetPuzzleUrl(PuzzleType t, PuzzleFormat f, PuzzleInfo puzzleInfo) =>
         f switch
         {
             PuzzleFormat.Json => $"https://www.nytimes.com/svc/crosswords/v6/puzzle/{t.ToString().ToLower()}/{puzzleInfo.DateString}.json",
             PuzzleFormat.Pdf => $"{pdfPrefix}/{puzzleInfo.ID}.pdf",
             PuzzleFormat.Answers => $"{pdfPrefix}/{puzzleInfo.ID}.ans.pdf",
             PuzzleFormat.Newspaper => $"{pdfPrefix}/print/{monthAbbreviations[puzzleInfo.Month - 1]}{puzzleInfo.Day.PadTwo()}{puzzleInfo.Year % 100}.pdf",
             _ => throw new NotImplementedException(),
         };

        /// <summary>
        /// Gets the URL for a JSON containing information about each available puzzle for a given month.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        public static string GetListUrl(PuzzleType t, int year, int month)
        {
            string ym = $"{year}-{month.PadTwo()}";
            int lastDay = DateTime.DaysInMonth(year, month);
            return $"https://www.nytimes.com/svc/crosswords/v3//puzzles.json?publish_type={t.ToString().ToLower()}&sort_order=asc&sort_by=print_date&date_start={ym}-01&date_end={ym}-{lastDay.PadTwo()}";
        }

        /// <summary>
        /// Gets the URL for a JSON containing information about each available puzzle for a given year.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public static string GetListUrl(PuzzleType t, int year)
        {
            return $"https://www.nytimes.com/svc/crosswords/v3//puzzles.json?publish_type={t.ToString().ToLower()}&sort_order=asc&sort_by=print_date&date_start={year}-01-01&date_end={year}-12-31";
        }
    }
}
