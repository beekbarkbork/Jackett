using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Jackett.Indexers
{
    /**
     * Based on the TVChaos UK class, as the site uses the exact same software
     */
    public class RareShare : BaseIndexer, IIndexer
    {
        string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        string GetRSSKeyUrl { get { return SiteLink + "getrss.php"; } }
        string SearchUrl { get { return SiteLink + "browse.php"; } }
        string RSSUrl { get { return SiteLink + "rss.php?secret_key={0}&feedtype=download&timezone=0&showrows=50&categories=all"; } }
        string CommentUrl { get { return SiteLink + "details.php?id={0}"; } }
        string DownloadUrl { get { return SiteLink + "download.php?id={0}"; } }

        new ConfigurationDataBasicLoginWithRSS configData
        {
            get { return (ConfigurationDataBasicLoginWithRSS)base.configData; }
            set { base.configData = value; }
        }

        public RareShare(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "RareShare.me",
                description: "Rare Share",
                link: "http://rareshare.me",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSS())
        {
            AddCategoryMapping(99, TorznabCatType.TV);              // Animation
            AddCategoryMapping(110, TorznabCatType.AudioAudiobook); // Audiobook

            AddCategoryMapping(7, TorznabCatType.TV);               // Comedy
            AddCategoryMapping(43, TorznabCatType.TV);              // Comedy-Drama

            AddCategoryMapping(11, TorznabCatType.TVDocumentary);   // Documentaries
            AddCategoryMapping(12, TorznabCatType.TV);              // Drama

            AddCategoryMapping(16, TorznabCatType.TV);              // Entertainment

            AddCategoryMapping(100, TorznabCatType.TV);             // Food and Cooking
            AddCategoryMapping(121, TorznabCatType.PC);             // Freeware

            AddCategoryMapping(23, TorznabCatType.TV);              // Game Shows

            AddCategoryMapping(111, TorznabCatType.TV);             // Home & Property

            AddCategoryMapping(49, TorznabCatType.TV);              // Kids

            AddCategoryMapping(26, TorznabCatType.TV);              // Motoring
            AddCategoryMapping(123, TorznabCatType.Movies);         // Movies
            AddCategoryMapping(50, TorznabCatType.Audio);           // Music
            AddCategoryMapping(19, TorznabCatType.TV);              // Mystery & Crime Fiction

            AddCategoryMapping(114, TorznabCatType.TV);             // News and Current Affairs

            AddCategoryMapping(8, TorznabCatType.Other);            // Other

            AddCategoryMapping(104, TorznabCatType.Audio);          // Radio
            AddCategoryMapping(105, TorznabCatType.Audio);          // Radio/Comedy
            AddCategoryMapping(106, TorznabCatType.Audio);          // Radio/Drama
            AddCategoryMapping(107, TorznabCatType.Audio);          // Radio/Factual
            AddCategoryMapping(108, TorznabCatType.Audio);          // Radio/Music
            AddCategoryMapping(122, TorznabCatType.Audio);          // Radio/Readings
            AddCategoryMapping(109, TorznabCatType.Audio);          // Radio/Sport
            AddCategoryMapping(114, TorznabCatType.TV);             // Reality

            AddCategoryMapping(20, TorznabCatType.TV);              // Sci-Fi
            AddCategoryMapping(20, TorznabCatType.TV);              // Soaps
            AddCategoryMapping(124, TorznabCatType.TV);             // Soaps/Monthly Archives
            AddCategoryMapping(22, TorznabCatType.TVSport);         // Sport
            AddCategoryMapping(65, TorznabCatType.TVSport);         // Sport/Formula 1

            AddCategoryMapping(18, TorznabCatType.TV);              // Talkshow
            AddCategoryMapping(21, TorznabCatType.TV);              // Trains & Planes
            AddCategoryMapping(69, TorznabCatType.TVDocumentary);   // True Crime

            AddCategoryMapping(101, TorznabCatType.TVDocumentary);  // Wildlife

            // RSS categories, mapped by name
            AddCategoryMapping("Animation", TorznabCatType.TV);
            AddCategoryMapping("Audiobook", TorznabCatType.AudioAudiobook);

            AddCategoryMapping("Comedy", TorznabCatType.TV);
            AddCategoryMapping("Comedy-Drama", TorznabCatType.TV);

            AddCategoryMapping("Documentries", TorznabCatType.TVDocumentary);
            AddCategoryMapping("Drama", TorznabCatType.TV);

            AddCategoryMapping("Entertainment", TorznabCatType.TV);

            AddCategoryMapping("Food and Cooking", TorznabCatType.TV);
            AddCategoryMapping("Freeware", TorznabCatType.PC);

            AddCategoryMapping("Game Shows", TorznabCatType.TV);

            AddCategoryMapping("Home & Property", TorznabCatType.TV);

            AddCategoryMapping("Kids", TorznabCatType.TV);

            AddCategoryMapping("Motoring", TorznabCatType.TV);
            AddCategoryMapping("Movies", TorznabCatType.Movies);
            AddCategoryMapping("Music", TorznabCatType.Audio);
            AddCategoryMapping("Mystery & Crime Fiction", TorznabCatType.TV);

            AddCategoryMapping("News and Current Affairs", TorznabCatType.TV);

            AddCategoryMapping("Other", TorznabCatType.Other);

            /*
             * Some of the Radio/* categories are commented out as their names clash with the top-level categories
             * mapped above. If these change later on, or a mechanism for handling them better is implemented, then
             * these can be re-implemented with the necessary changes.
             */
            AddCategoryMapping("Radio", TorznabCatType.Audio);
            // AddCategoryMapping("Comedy", TorznabCatType.Audio); // Radio/Comedy
            // AddCategoryMapping("Drama", TorznabCatType.Audio); // Radio/Drama
            AddCategoryMapping("Factual", TorznabCatType.Audio); // Radio/Factual
            // AddCategoryMapping("Music", TorznabCatType.Audio); // Radio/Music
            AddCategoryMapping("Readings", TorznabCatType.Audio); // Radio/Readings
            // AddCategoryMapping("Sport", TorznabCatType.Audio); // Radio/Sport
            AddCategoryMapping("Reality", TorznabCatType.TV);

            AddCategoryMapping("Sci-Fi", TorznabCatType.Audio);
            AddCategoryMapping("Soaps", TorznabCatType.Audio);
            AddCategoryMapping("Monthly Archives", TorznabCatType.Audio);
            AddCategoryMapping("Sport", TorznabCatType.TVSport);
            AddCategoryMapping("Formula 1", TorznabCatType.TVSport);

            AddCategoryMapping("Talkshow", TorznabCatType.TV);
            AddCategoryMapping("Trains & Planes", TorznabCatType.TV);
            AddCategoryMapping("True Crime", TorznabCatType.TVDocumentary);

            AddCategoryMapping("Wildlife", TorznabCatType.TVDocumentary);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, SearchUrl, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () => {
                CQ dom = result.Content;
                var errorMessage = dom[".left_side table:eq(0) tr:eq(1)"].Text().Trim().Replace("\n\t", " ");
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            try {
                // Get RSS key
                var rssParams = new Dictionary<string, string> {
                { "feedtype", "download" },
                { "timezone", "0" },
                { "showrows", "50" }
            };
                var rssPage = await PostDataWithCookies(GetRSSKeyUrl, rssParams, result.Cookies);
                var match = Regex.Match(rssPage.Content, "(?<=secret_key\\=)([a-zA-z0-9]*)");
                configData.RSSKey.Value = match.Success ? match.Value : string.Empty;
                if (string.IsNullOrWhiteSpace(configData.RSSKey.Value))
                    throw new Exception("Failed to get RSS Key");
                SaveConfig();
            } catch (Exception e) {
                IsConfigured = false;
                throw e;
            }
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            // If we have no query use the RSS Page as their server is slow enough at times!
            if (string.IsNullOrWhiteSpace(searchString)) {
                var rssPage = await RequestStringWithCookiesAndRetry(string.Format(RSSUrl, configData.RSSKey.Value));
                var rssDoc = XDocument.Parse(rssPage.Content);

                foreach (var item in rssDoc.Descendants("item")) {
                    var title = item.Descendants("title").First().Value;
                    var description = item.Descendants("description").First().Value;
                    var link = item.Descendants("link").First().Value;
                    var category = item.Descendants("category").First().Value;
                    var date = item.Descendants("pubDate").First().Value;

                    var torrentIdMatch = Regex.Match(link, "(?<=id=)(\\d)*");
                    var torrentId = torrentIdMatch.Success ? torrentIdMatch.Value : string.Empty;
                    if (string.IsNullOrWhiteSpace(torrentId))
                        throw new Exception("Missing torrent id");

                    var infoMatch = Regex.Match(description, @"Category:\W(?<cat>.*)\W\/\WSeeders:\W(?<seeders>\d*)\W\/\WLeechers:\W(?<leechers>\d*)\W\/\WSize:\W(?<size>[\d\.]*\W\S*)");
                    if (!infoMatch.Success)
                        throw new Exception("Unable to find info");

                    var release = new ReleaseInfo() {
                        Title = title,
                        Description = title,
                        Guid = new Uri(string.Format(DownloadUrl, torrentId)),
                        Comments = new Uri(string.Format(CommentUrl, torrentId)),
                        PublishDate = DateTime.ParseExact(date, "yyyy-MM-dd H:mm:ss", CultureInfo.InvariantCulture), //2015-08-08 21:20:31 
                        Link = new Uri(string.Format(DownloadUrl, torrentId)),
                        Seeders = ParseUtil.CoerceInt(infoMatch.Groups["seeders"].Value),
                        Peers = ParseUtil.CoerceInt(infoMatch.Groups["leechers"].Value),
                        Size = ReleaseInfo.GetBytes(infoMatch.Groups["size"].Value),
                        Category = MapTrackerCatToNewznab(infoMatch.Groups["cat"].Value)
                    };

                    // If its not apps or audio we can only mark as general TV
                    if (release.Category == 0)
                        release.Category = 5030;

                    release.Peers += release.Seeders;
                    releases.Add(release);
                }
            } else {
                // As per TVChaos UK, the RareShares search engine requires an exact match of the search string, however it
                // seems like they just send the unfiltered search to the SQL server in a like query (eg, `LIKE '%$searchstring%'`),
                // thus, we just switch out any non-alphanumeric characters with a % to return more usable results.
                Regex ReplaceRegex = new Regex("[^a-zA-Z0-9]+");
                searchString = ReplaceRegex.Replace(searchString, "%");

                var searchParams = new Dictionary<string, string> {
                    { "do", "search" },
                    { "keywords",  searchString },
                    { "search_type", "t_name" },
                    { "category", "0" },
                    { "include_dead_torrents", "no" }
                };

                var searchPage = await PostDataWithCookiesAndRetry(SearchUrl, searchParams);
                try {
                    CQ dom = searchPage.Content;
                    var rows = dom["#listtorrents tbody tr"];
                    foreach (var row in rows.Skip(1)) {
                        var release = new ReleaseInfo();
                        var qRow = row.Cq();

                        release.Title = qRow.Find("td:eq(1) .tooltip-content div:eq(0)").Text();

                        if (string.IsNullOrWhiteSpace(release.Title))
                            continue;

                        release.Description = release.Title;
                        release.Guid = new Uri(qRow.Find("td:eq(2) a").Attr("href"));
                        release.Link = release.Guid;
                        release.Comments = new Uri(qRow.Find("td:eq(1) .tooltip-target a").Attr("href"));
                        release.PublishDate = DateTime.ParseExact(qRow.Find("td:eq(1) div").Last().Text().Trim(), "dd-MM-yyyy H:mm", CultureInfo.InvariantCulture); //08-08-2015 12:51 
                        release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:eq(6)").Text());
                        release.Peers = release.Seeders + ParseUtil.CoerceInt(qRow.Find("td:eq(7)").Text().Trim());
                        release.Size = ReleaseInfo.GetBytes(qRow.Find("td:eq(4)").Text().Trim());


                        var cat = row.Cq().Find("td:eq(0) a").First().Attr("href");
                        var catSplit = cat.LastIndexOf('=');
                        if (catSplit > -1)
                            cat = cat.Substring(catSplit + 1);
                        release.Category = MapTrackerCatToNewznab(cat);

                        // If its not apps or audio we can only mark as general TV
                        if (release.Category == 0)
                            release.Category = 5030;

                        var grabs = qRow.Find("td:nth-child(6)").Text();
                        release.Grabs = ParseUtil.CoerceInt(grabs);

                        if (qRow.Find("img[alt*=\"Free Torrent\"]").Length >= 1)
                            release.DownloadVolumeFactor = 0;
                        else
                            release.DownloadVolumeFactor = 1;

                        if (qRow.Find("img[alt*=\"x2 Torrent\"]").Length >= 1)
                            release.UploadVolumeFactor = 2;
                        else
                            release.UploadVolumeFactor = 1;

                        releases.Add(release);
                    }
                } catch (Exception ex) {
                    OnParseError(searchPage.Content, ex);
                }
            }

            return releases;
        }
    }
}
