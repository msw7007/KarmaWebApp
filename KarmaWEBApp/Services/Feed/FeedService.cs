namespace KarmaWEBApp.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.ServiceModel.Syndication;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using Boilerplate.Web.Mvc;
    using KarmaWEBApp.Constants;
    using KarmaWEBApp.Models;
    using System.Linq;

    /// <summary>
    /// Builds <see cref="SyndicationFeed"/>'s containing meta data about the feed and the feed entries.
    /// Note: We are targeting Atom 1.0 over RSS 2.0 because Atom 1.0 is a newer and more well defined format. Atom 1.0
    /// is a standard and RSS is not. See http://rehansaeed.com/building-rssatom-feeds-for-asp-net-mvc/.
    /// </summary>
    public sealed class FeedService : IFeedService
    {
        /// <summary>
        /// The feed universally unique identifier. Do not use the URL of your feed as this can change.
        /// A much better ID is to use a GUID which you can generate from Tools->Create GUID in Visual Studio.
        /// </summary>
        private const string FeedId = "";
        private const string PubSubHubbubHubUrl = "https://pubsubhubbub.appspot.com/";
        private KarmaDBEntities db = new KarmaDBEntities();
        private readonly HttpClient httpClient;
        private readonly UrlHelper urlHelper;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedService"/> class.
        /// </summary>
        /// <param name="urlHelper">The URL helper.</param>
        public FeedService(UrlHelper urlHelper)
        {
            this.urlHelper = urlHelper;
            this.httpClient = new HttpClient();
        }

        #endregion

        #region Public Methods

        public async Task<SyndicationFeed> GetFeed(CancellationToken cancellationToken)
        {
            SyndicationFeed feed = new SyndicationFeed()
            {
                // id (Required) - The feed universally unique identifier.
                Id = FeedId,
                // title (Required) - Contains a human readable title for the feed. Often the same as the title of the 
                //                    associated website. This value should not be blank.
                Title = SyndicationContent.CreatePlaintextContent("Система кармы"),
                // items (Required) - The items to add to the feed.
                Items = await this.GetItems(cancellationToken),
            };
            return feed;
        }

        public Task PublishUpdate()
        {
            return httpClient.PostAsync(
                PubSubHubbubHubUrl, 
                new FormUrlEncodedContent(
                    new KeyValuePair<string, string>[]
                    {
                        new KeyValuePair<string, string>("hub.mode", "publish"),
                        new KeyValuePair<string, string>(
                            "hub.url", 
                            this.urlHelper.AbsoluteRouteUrl(HomeControllerRoute.GetFeed))
                    }));
        }

        #endregion

        #region Private Methods
        public string CharAssembler(string IDPlayer)
        {
            KarmaDBEntities db = new KarmaDBEntities();
            var DBRosterCharNames = db.chartable.Where(t => t.idplayer == IDPlayer);
            string CharacterString = "";
            foreach (var Character in DBRosterCharNames)
            {
                CharacterString = CharacterString + Character.charname + "(" + Character.char_class + "/" + Character.char_role + "),";
            }
            CharacterString = CharacterString.Substring(0, (CharacterString.Length - 1));
            return CharacterString;
        }
        /// <summary>
        /// Gets the collection of <see cref="SyndicationItem"/>'s that represent the atom entries.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> signifying if the request is cancelled.</param>
        /// <returns>A collection of <see cref="SyndicationItem"/>'s.</returns>
        private async Task<List<SyndicationItem>> GetItems(CancellationToken cancellationToken)
        {
            List<SyndicationItem> items = new List<SyndicationItem>();

            foreach (var DBitem in db.absentdata)
            {
                var DTvar1 = DBitem.AbsentDate1 ?? DateTime.Now;
                var DTvar2 = DBitem.AbsentDate2 ?? DateTime.Now;
                var DateString1 = DTvar1.ToString("MM.dd.yyyy");
                var DateString2 = DTvar2.ToString("MM.dd.yyyy");
                SyndicationItem item = new SyndicationItem()
                {
                    // id (Required) - Identifies the entry using a universally unique and permanent URI. Two entries 
                    //                 in a feed can have the same value for id if they represent the same entry at 
                    //                 different points in time.
                    Id = FeedId + DBitem.idAbsentData,
                    // title (Required) - Contains a human readable title for the entry. This value should not be blank.
                    Title = SyndicationContent.CreatePlaintextContent(CharAssembler(DBitem.PlayerID)),
                    // description (Recommended) - A summary of the entry.
                    Summary = SyndicationContent.CreatePlaintextContent(DBitem.AbsentText),
                    // updated (Optional) - Indicates the last time the entry was modified in a significant way. This 
                    //                      value need not change after a typo is fixed, only after a substantial 
                    //                      modification. Generally, different entries in a feed will have different 
                    //                      updated timestamps.
                    Content = SyndicationContent.CreatePlaintextContent("отсутствие с " + DateString1  + " по " + DateString2),
                    // published (Optional) - Contains the time of the initial creation or first availability of the entry.
                };
                items.Add(item);
            }

            return items;
        }

        #endregion
    }
}