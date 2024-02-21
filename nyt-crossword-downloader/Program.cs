using nyt_crossword_downloader;
using static nyt_crossword_downloader.Methods;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;
using static nyt_crossword_downloader.Routing;
using System.Threading.Tasks;
using System.Globalization;

// Options

PuzzleType typeToDownload = PuzzleType.All;
PuzzleFormat fmtToDownload = PuzzleFormat.All;

bool overwrite = false;

const int DEFAULT_RETRIES = 3;
const string DEFAULT_COOKIE_PATH = "cookies.txt";
const string DEFAULT_OUT_DIR = "Puzzles";

int retryCount = DEFAULT_RETRIES;
string cookiePath = DEFAULT_COOKIE_PATH;
string outDir = DEFAULT_OUT_DIR;

DateTime dailyStartDate = new(1993, 11, 21);
DateTime bonusStartDate = new(1997, 2, 1);
DateTime miniStartDate = new(2014, 8, 21);

DateTime startDate = dailyStartDate;
DateTime endDate = DateTime.Now;

void printHelpText()
{
    Log(
        $"usage: nyt-crossword-downloader [options]\r\n" +
        $"  options:\r\n" +
        $"    -h, --help                    Show this help text.\r\n" +
        $"    -c, --cookies path            Uses cookies at specified path to authenticate with NYT API. Default = \"{DEFAULT_COOKIE_PATH}\" in working directory.\r\n" +
        $"    -o, --output-path path        Saves puzzles to directory at specified path. Default = folder named \"{DEFAULT_OUT_DIR}\" in working directory.\r\n" +
        $"    -t, --type type               Download only puzzles of the specified type(s). Options are \"daily\", \"mini\", and \"bonus\". Multiple can be specified with commas separating them (no spaces). Default = all.\r\n" +
        $"    -fmt, --format format         Download only puzzles of the specified format(s). Options are \"json\", \"pdf\", \"answers\", and \"newspaper\". Multiple can be specified with commas separating them (no spaces). Default = all.\r\n" +
        $"    -f, --force-overwrite         Overwrite existing puzzles.\r\n" +
        $"    -s, --start-date date         Starts scraping at specified puzzle date. Date must be in format yyyy-MM-dd. Default = {dailyStartDate:yyyy-MM-dd} (date of oldest puzzle available).\r\n" +
        $"    -e, --end-date date           Stops scraping at specified puzzle date, inclusive. Date must be in format yyyy-MM-dd. Default = current date.\r\n" +
        $"    -r, --retries number          Attempts to retry failed downloads up specified number of times. Default = {DEFAULT_RETRIES}."
    );
}
// Show help text if requested
for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    if (arg == "-h" || arg == "--help" || arg == "/?")
    {
        printHelpText();
        return;
    }
}

// Parse command line arguments
for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    switch (arg)
    {
        case "-c":
        case "--cookies":
            i++;
            if (args.Length < i + 1)
            {
                LogError($"No file provided to {arg}");
                return;
            }
            cookiePath = args[i];
            break;

        case "-o":
        case "--output-path":
            i++;
            if (args.Length < i + 1)
            {
                LogError($"No output path provided to {arg}");
                return;
            }
            outDir = args[i];
            break;

        case "-t":
        case "--type":
            i++;
            if (args.Length < i + 1)
            {
                LogError($"No puzzle type provided to {arg}");
                return;
            }
            arg = args[i];
            typeToDownload = PuzzleType.None;
            var types = arg.Split(',');
            foreach (var type in types)
            {
                if (Enum.TryParse(type, true, out PuzzleType puzzleTypeResult))
                    typeToDownload |= puzzleTypeResult;
                else
                {
                    LogError($"Unrecognized puzzle type '{type}'");
                    return;
                }
            }
            break;

        case "-fmt":
        case "--format":
            i++;
            if (args.Length < i + 1)
            {
                LogError($"No puzzle format provided to {arg}");
                return;
            }
            arg = args[i];
            fmtToDownload = PuzzleFormat.None;
            var fmts = arg.Split(',');
            foreach (var fmt in fmts)
            {
                if (Enum.TryParse(fmt, true, out PuzzleFormat puzzleFmtResult))
                    fmtToDownload |= puzzleFmtResult;
                else
                {
                    LogError($"Unrecognized puzzle type '{fmt}'");
                    return;
                }
            }
            break;

        case "-f":
        case "--force-overwrite":
            overwrite = true;
            break;

        case "-s":
        case "--start-date":
            i++;
            if (args.Length < i + 1)
            {
                LogError($"No start date provided to {arg}");
                return;
            }
            arg = args[i];
            if (!DateTime.TryParseExact(arg, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out startDate))
            {
                LogError($"Invalid start date provided. Value must be in format 'yyyy-MM-dd'");
                return;
            }
            break;

        case "-e":
        case "--end-date":
            i++;
            if (args.Length < i + 1)
            {
                LogError($"No end date provided to {arg}");
                return;
            }
            arg = args[i];
            if (!DateTime.TryParseExact(arg, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out endDate))
            {
                LogError($"Invalid end date provided. Value must be in format 'yyyy-MM-dd'");
                return;
            }
            break;


        case "-r":
        case "--retries":
            i++;
            if (args.Length < i + 1)
            {
                LogError($"No number provided to {arg}");
                return;
            }
            arg = args[i];
            if (!int.TryParse(arg, out retryCount) || retryCount < 0)
            {
                LogError($"Invalid value provided to {arg}. Must be non-negative integer.");
                return;
            }
            break;

        default:
            LogError($"Unrecognized argument: {arg}");
            printHelpText();
            return;
    }
}

// Extra command line argument validation

if (startDate.Date > endDate.Date)
{
    LogError($"Start date must not be later than end date");
    return;
}

if (!File.Exists(cookiePath))
{
    LogError($"Could not find cookie file at \"{cookiePath}\"");
    return;
}

Downloader downloader = new(cookiePath, retryCount, overwrite);

// Scrape puzzles

// Daily
if (typeToDownload.HasFlag(PuzzleType.Daily))
{

    if (startDate < dailyStartDate)
        startDate = dailyStartDate;

    Log($"Fetching daily puzzles...");

    string jsonBasePath = @$"{outDir}\JSON\Daily";
    string pdfBasePath = @$"{outDir}\PDF\Daily";
    string newspaperPdfBasePath = @$"{outDir}\PDF (Newspaper)\Daily";

    for (int year = startDate.Year; year <= endDate.Year; year++)
    {
        string jsonYearPath = $@"{jsonBasePath}\{year}";
        string pdfYearPath = $@"{pdfBasePath}\{year}";
        string newspaperPdfYearPath = $@"{newspaperPdfBasePath}\{year}";

        for (int month = 1; month <= 12; month++)
        {

            if (year == startDate.Year && month < startDate.Month)
                month = startDate.Month;
            if (year == endDate.Year && month > endDate.Month)
                break;

            Log($"Fetching daily puzzles for {CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames[month - 1]}, {year}...");

            PuzzleSet? results = await downloader.DownloadFromJson<PuzzleSet>(GetListUrl(PuzzleType.Daily, year, month));
            if (results?.Results != null && results.Results.Length > 0)
            {
                string jsonMonthPath = $@"{jsonYearPath}\{month.PadTwo()}";
                string pdfMonthPath = $@"{pdfYearPath}\{month.PadTwo()}";
                string pdfAnswersPath = $@"{pdfMonthPath}\Answers";
                string newspaperPdfMonthPath = $@"{newspaperPdfYearPath}\{month.PadTwo()}";
                if (fmtToDownload.HasFlag(PuzzleFormat.Json)) Directory.CreateDirectory(jsonMonthPath);
                if (fmtToDownload.HasFlag(PuzzleFormat.Pdf)) Directory.CreateDirectory(pdfMonthPath);
                if (fmtToDownload.HasFlag(PuzzleFormat.Answers)) Directory.CreateDirectory(pdfAnswersPath);

                bool fetchNewspaper = false;
                if (fmtToDownload.HasFlag(PuzzleFormat.Newspaper) && ((year > 2011) || ((year == 2011) && (month >= 4))))
                {
                    fetchNewspaper = true;
                    Directory.CreateDirectory(newspaperPdfMonthPath);
                }

                List<Task> tasks = [];

                foreach (var puzzleInfo in results.Results)
                {

                    if (!puzzleInfo.IsInRange(startDate, endDate))
                        continue;

                    if (fmtToDownload.HasFlag(PuzzleFormat.Json))
                    {
                        string puzzlePath = $@"{jsonMonthPath}\{puzzleInfo.DateString}.json";
                        tasks.Add(downloader.DownloadToFile(GetPuzzleUrl(PuzzleType.Daily, PuzzleFormat.Json, puzzleInfo), puzzlePath));
                    }
                    if (fmtToDownload.HasFlag(PuzzleFormat.Pdf))
                    {
                        string puzzlePath = $@"{pdfMonthPath}\{puzzleInfo.DateString}.pdf";
                        tasks.Add(downloader.DownloadToFile(GetPuzzleUrl(PuzzleType.Daily, PuzzleFormat.Pdf, puzzleInfo), puzzlePath));
                    }
                    if (fmtToDownload.HasFlag(PuzzleFormat.Answers))
                    {
                        string puzzlePath = $@"{pdfAnswersPath}\{puzzleInfo.DateString}_answers.pdf";
                        tasks.Add(downloader.DownloadToFile(GetPuzzleUrl(PuzzleType.Daily, PuzzleFormat.Answers, puzzleInfo), puzzlePath));
                    }
                    if (fetchNewspaper)
                    {
                        string puzzlePath = $@"{newspaperPdfMonthPath}\{puzzleInfo.DateString}.pdf";
                        tasks.Add(downloader.DownloadToFile(GetPuzzleUrl(PuzzleType.Daily, PuzzleFormat.Newspaper, puzzleInfo), puzzlePath));
                    }
                }

                Task.WaitAll([.. tasks]);
            }
        }
    }
}

// Bonus
if (typeToDownload.HasFlag(PuzzleType.Bonus))
{

    if (startDate < bonusStartDate)
        startDate = bonusStartDate;

    Log($"Fetching bonus puzzles...");

    string jsonBasePath = @$"{outDir}\JSON\Bonus";
    string pdfBasePath = @$"{outDir}\PDF\Bonus";

    for (int year = startDate.Year; year <= endDate.Year; year++)
    {
        Log($"Fetching bonus puzzles for {year}...");

        PuzzleSet? results = await downloader.DownloadFromJson<PuzzleSet>(GetListUrl(PuzzleType.Bonus, year));
        if (results?.Results != null && results.Results.Length > 0)
        {

            string jsonYearPath = $@"{jsonBasePath}\{year}";
            string pdfYearPath = $@"{pdfBasePath}\{year}";
            string pdfAnswersPath = $@"{pdfYearPath}\Answers";
            if (fmtToDownload.HasFlag(PuzzleFormat.Json)) Directory.CreateDirectory(jsonYearPath);
            if (fmtToDownload.HasFlag(PuzzleFormat.Pdf)) Directory.CreateDirectory(pdfYearPath);
            if (fmtToDownload.HasFlag(PuzzleFormat.Answers)) Directory.CreateDirectory(pdfAnswersPath);

            List<Task> tasks = [];

            foreach (var puzzleInfo in results.Results)
            {
                if (!puzzleInfo.IsInRange(startDate, endDate))
                    continue;

                if (fmtToDownload.HasFlag(PuzzleFormat.Json))
                {
                    string puzzlePath = $@"{jsonYearPath}\{puzzleInfo.DateString}.json";
                    tasks.Add(downloader.DownloadToFile(GetPuzzleUrl(PuzzleType.Bonus, PuzzleFormat.Json, puzzleInfo), puzzlePath));
                }
                if (fmtToDownload.HasFlag(PuzzleFormat.Pdf))
                {
                    string puzzlePath = $@"{pdfYearPath}\{puzzleInfo.DateString}.pdf";
                    tasks.Add(downloader.DownloadToFile(GetPuzzleUrl(PuzzleType.Bonus, PuzzleFormat.Pdf, puzzleInfo), puzzlePath));
                }
                if (fmtToDownload.HasFlag(PuzzleFormat.Answers))
                {
                    string puzzlePath = $@"{pdfAnswersPath}\{puzzleInfo.DateString}-answers.pdf";
                    tasks.Add(downloader.DownloadToFile(GetPuzzleUrl(PuzzleType.Bonus, PuzzleFormat.Answers, puzzleInfo), puzzlePath));
                }
            }

            Task.WaitAll([.. tasks]);

        }
    }
}

// Mini
if (typeToDownload.HasFlag(PuzzleType.Mini) && fmtToDownload.HasFlag(PuzzleFormat.Json))
{

    if (startDate < miniStartDate)
        startDate = miniStartDate;

    Log($"Fetching mini puzzles...");

    string basePath = @$"{outDir}\JSON\Mini";

    for (int year = startDate.Year; year <= endDate.Year; year++)
    {
        string yearPath = $@"{basePath}\{year}";

        for (int month = 1; month <= 12; month++)
        {

            if (year == startDate.Year && month < startDate.Month)
                month = startDate.Month;
            if (year == endDate.Year && month > endDate.Month)
                break;

            Log($"Fetching mini puzzles for {CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames[month - 1]}, {year}...");

            PuzzleSet? results = await downloader.DownloadFromJson<PuzzleSet>(GetListUrl(PuzzleType.Mini, year, month));
            if (results?.Results != null && results.Results.Length > 0)
            {
                string monthPath = $@"{yearPath}\{month.PadTwo()}";
                Directory.CreateDirectory(monthPath);

                List<Task> tasks = [];

                foreach (var puzzleInfo in results.Results)
                {
                    if (!puzzleInfo.IsInRange(startDate, endDate))
                        continue;

                    var puzzlePath = $@"{monthPath}\{puzzleInfo.DateString}.json";
                    tasks.Add(downloader.DownloadToFile(GetPuzzleUrl(PuzzleType.Mini, PuzzleFormat.Json, puzzleInfo), puzzlePath));
                }

                Task.WaitAll([.. tasks]);

            }
        }
    }
}