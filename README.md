# NYT Crossword Downloader
This is a utility for scraping New York Times crossword puzzles and answers in their native JSON format as well as PDF for the purposes of offline play or data science-related research. It requires an active subscription to the New York Times to work.

## Usage

### Basic

You can download the latest Windows x64 release [here](https://github.com/shloop/nyt-crossword-downloader/releases/latest/download/nyt-crossword-downloader.exe). You must have the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download) or later installed to use it.

If you haven't already, log into the New York Times website and verify you have access to the crossword puzzles (https://www.nytimes.com/crosswords), then save your cookies to a file. This can be done via a browser extension. I use [this one](https://addons.mozilla.org/en-US/firefox/addon/cookies-txt/) for Firefox.

Once you have your cookies file with the needed authentication info, rename it to `cookies.txt`, place it in the same directory as `nyt-crossword-downloader.exe` and then run the executable to scrape all puzzles in all formats.

### Advanced

You can use command line argument -h (or --help, or /?) to see the full list of options:

```
usage: nyt-crossword-downloader [options]
  options:
    -h, --help                    Show this help text.
    -c, --cookies path            Uses cookies at specified path to authenticate with NYT API. Default = "cookies.txt" in working directory.
    -o, --output-path path        Saves puzzles to directory at specified path. Default = folder named "Puzzles" in working directory.
    -t, --type type               Download only puzzles of the specified type(s). Options are "daily", "mini", and "bonus". Multiple can be specified with commas separating them (no spaces). Default = all.
    -fmt, --format format         Download only puzzles of the specified format(s). Options are "json", "pdf", "answers", and "newspaper". Multiple can be specified with commas separating them (no spaces). Default = all.
    -f, --force-overwrite         Overwrite existing puzzles.
    -s, --start-date date         Starts scraping at specified puzzle date. Date must be in format yyyy-MM-dd. Default = 1993-11-21 (date of oldest puzzle available).
    -e, --end-date date           Stops scraping at specified puzzle date, inclusive. Date must be in format yyyy-MM-dd. Default = current date.
    -r, --retries number          Attempts to retry failed downloads up to specified number of times. Default = 3.
```
