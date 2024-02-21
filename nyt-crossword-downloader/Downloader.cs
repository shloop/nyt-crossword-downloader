using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using static nyt_crossword_downloader.Methods;

namespace nyt_crossword_downloader
{

    /// <summary>
    /// Class for managing HTTP requests.
    /// </summary>
    class Downloader
    {
        private readonly int retryCount;
        private readonly bool overwrite;

        private readonly HttpClient? client;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cookieFilePath"></param>
        /// <param name="retryCount"></param>
        /// <exception cref="FormatException"></exception>
        public Downloader(string cookieFilePath, int retryCount, bool overwrite)
        {
            this.retryCount = retryCount;
            this.overwrite = overwrite;

            // Parse cookies
            using var cookieStream = File.OpenRead(cookieFilePath);
            using StreamReader streamReader = new(cookieStream);
            CookieCollection cookieCollection = [];

            int num = 0;
            while (!streamReader.EndOfStream)
            {
                string text = streamReader.ReadLine()!;
                num++;
                if (text.Length > 0 && text[0] != '#' && text.Trim() != "")
                {
                    string[] array = text.Split(['\t']);
                    if (array.Length != 7)
                    {
                        throw new FormatException($"Line {num} has {array.Length} columns. Expected 7");
                    }

                    string domain = array[0];
                    string path = array[2];
                    bool secure = array[3] == "TRUE";
                    DateTime utcDateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(array[4])).UtcDateTime;
                    string name = array[5];
                    string value = array[6];
                    Cookie item = new(name, value, path, domain)
                    {
                        Secure = secure,
                        Expires = utcDateTime
                    };
                    cookieCollection.Add(item);
                }
            }
            CookieContainer cookieContainer = new();
            cookieContainer.Add(cookieCollection);

            // Create HTTP client
            HttpClientHandler handler = new() { CookieContainer = cookieContainer };
            client = new(handler);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public async Task<T?> Download<T>(string url, DownloadMethod<T> f) where T : class
        {
            for (int i = 1; i <= retryCount + 1; i++)
            {
                try
                {
                    return await f(url);
                }
                catch (Exception)
                {
                    if (i < retryCount + 1)
                        Console.WriteLine($"Failed to download {url}. Retrying...");
                    else
                        LogError($"Failed to download {url}.");
                }
            }
            return null;
        }

        /// <summary>
        /// Fetches contents at provided URL as string.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<string?> DownloadString(string url)
        {
            async Task<string> f(string x) => await client!.GetStringAsync(url);
            return await Download(url, f);
        }

        /// <summary>
        /// Fetches JSON at provided URL as deserializes to specified class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<T?> DownloadFromJson<T>(string url) where T : class
        {
            string json = await client!.GetStringAsync(url);
            return json == null ? null : JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// Fetches contents at provided URL as byte array.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<byte[]?> DownloadBytes(string url)
        {
            async Task<byte[]> f(string x) => await client!.GetByteArrayAsync(url);
            return await Download(url, f);
        }

        /// <summary>
        /// Fetches contents at provided URL and saves to file at specified path.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="localPath"></param>
        public async Task DownloadToFile(string url, string localPath)
        {
            if (overwrite || !File.Exists(localPath))
            {
                byte[]? data = await DownloadBytes(url);
                if (data != null) File.WriteAllBytes(localPath, data);
            }
        }

    }
}
