namespace KarmaWEBApp.Controllers
{
    using System.Diagnostics;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using Boilerplate.Web.Mvc;
    using Boilerplate.Web.Mvc.Filters;
    using KarmaWEBApp.Constants;
    using KarmaWEBApp.Services;
    using KarmaWEBApp.Models;
    using System.Linq;
    using System.Web.UI.WebControls;
    using System.Collections;
    using System.IO;
    using System.Data.SqlClient;
    using MySql.Data.MySqlClient;
    using System;
    using System.Collections.Generic;
    using System.Web.WebPages;
    using System.Runtime.InteropServices;
    using System.Data;
    using System.Data.Entity;
    using System.Web.Routing;
    using System.Web;
    using System.Configuration;

    public class HomeController : Controller
    {
        #region Fields

        private readonly IBrowserConfigService browserConfigService;
        private readonly IFeedService feedService;
        private readonly IManifestService manifestService;
        private readonly IOpenSearchService openSearchService;
        private readonly IRobotsService robotsService;
        private readonly ISitemapService sitemapService; 

        #endregion

        #region Constructors

        public HomeController(
            IBrowserConfigService browserConfigService,
            IFeedService feedService,
            IManifestService manifestService,
            IOpenSearchService openSearchService,
            IRobotsService robotsService,
            ISitemapService sitemapService)
        {
            this.browserConfigService = browserConfigService;
            this.feedService = feedService;
            this.manifestService = manifestService;
            this.openSearchService = openSearchService;
            this.robotsService = robotsService;
            this.sitemapService = sitemapService;
        }

        #endregion

        private KarmaDBEntities db = new KarmaDBEntities();

        public string CharAssembler(string IDPlayer)
        {
            KarmaDBEntities db = new KarmaDBEntities();
            var DBRosterCharNames = db.chartable.Where(t => t.idplayer == IDPlayer);
            string CharacterString = "";
            foreach (var Character in DBRosterCharNames)
            {
                CharacterString = CharacterString + Character.charname + "(" + Character.char_class + "/" + Character.char_role + "),";
            }
            CharacterString = CharacterString.Substring(0,(CharacterString.Length-1));
            return CharacterString;
        }

        public class RL
        {
            public string ID { get; set; }
            public string CHARS { get; set; }
            public sbyte ACTIV { get; set; }
            public double KARMA { get; set; }
            public double NADEZ { get; set; }
        }

        public class KL
        {
            public string ID { get; set; }
            public string CHARS { get; set; }
            public string KARMA { get; set; }
            public DateTime Date { get; set; }
        }


        public double KarmaAsemble(string ID)
        {
            string cnnString = "server=localhost;user id=root;password=Kageretsu;persistsecurityinfo=True;database=staticdb";
            string ConStr = @"SELECT KarmaCounter(""" + ID.ToString() + @""") AS Karma FROM Chartable where Chartable.idplayer=""" + ID.ToString() + @""" group by idplayer;";
            MySqlConnection cnn = new MySqlConnection(cnnString);
            MySqlCommand cmd = new MySqlCommand(ConStr, cnn);
            cnn.Open();
            DataTable dataTable = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);
            da.Fill(dataTable);
            string STR = "";
            foreach (DataRow row in dataTable.Rows)
            {
                STR = row["Karma"].ToString();
            }
            double result = Convert.ToDouble(STR);
            cnn.Close();
            da.Dispose();
            return result;
        }

        public double NadezAsemble(string ID)
        {
            string cnnString = "server=localhost;user id=root;password=Kageretsu;persistsecurityinfo=True;database=staticdb";
            string ConStr = @"SELECT NadezCounter(""" + ID.ToString() + @""") AS Nadez FROM Chartable where Chartable.idplayer=""" + ID.ToString() + @""" group by idplayer;";
            MySqlConnection cnn = new MySqlConnection(cnnString);
            MySqlCommand cmd = new MySqlCommand(ConStr, cnn);
            cnn.Open();
            DataTable dataTable = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);
            da.Fill(dataTable);
            string STR = "";
            foreach (DataRow row in dataTable.Rows)
            {
                STR = row["Nadez"].ToString();
            }
            double result = Convert.ToDouble(STR) * 100;
            cnn.Close();
            da.Dispose();
            return result;
        }

        public string KarmaGet(string ID, DateTime RaidDate)
        {
            string cnnString = "server=localhost;user id=root;password=Kageretsu;persistsecurityinfo=True;database=staticdb";
            string ConStr = "SELECT IDAttendance AS Atten FROM raiddates where IDPlayer=" + ID.ToString() + " AND  RaidDate="+ '\u0022' + RaidDate.ToString("yyyy-MM-dd")+ '\u0022' + ";";
            MySqlConnection cnn = new MySqlConnection(cnnString);
            MySqlCommand cmd = new MySqlCommand(ConStr, cnn);
            cnn.Open();
            DataTable dataTable = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);
            da.Fill(dataTable);
            string STR = "";
            foreach (DataRow row in dataTable.Rows)
            {
                STR = row["Atten"].ToString();
            }
            string result = "";
            if (STR != "") { result = STR; } else { result = "4"; };
            cnn.Close();
            da.Dispose();
            return result;
        }

        public IEnumerable<List<RL>> CharsAssembler(sbyte ActiveS)
        {
            KarmaDBEntities db = new KarmaDBEntities();
            var DBRosterList = db.chartable.Join(db.playerdata, c => c.idplayer, p => p.idPlayer, (c, p) => new { c, p }).Where(t => t.p.IsActive == ActiveS).OrderBy(s => s.p.idPlayer).ToList();
            string ID = "";
            sbyte isActive = 0;
            List<RL> ResultList = new List<RL>();
            RL Row = new RL();
            foreach (var Character in DBRosterList)
            {
                ID = Character.p.idPlayer;
                isActive = Character.p.IsActive ?? 1;
                if (ID != "" && ID != null)
                {
                Row = new RL();
                Row.ID = ID;
                Row.CHARS = CharAssembler(ID);
                Row.ACTIV = isActive;
                Row.KARMA = KarmaAsemble(ID);
                Row.NADEZ = NadezAsemble(ID);
                }
                ResultList.Add(Row);
            }
            IEnumerable<List<RL>> RGList = ResultList.Where(t => t.ACTIV == ActiveS).GroupBy(x => x.ID)
                                            .Select(group => group.ToList())
                                            .ToList();
            return RGList;
        }

        public IEnumerable<List<KL>> KarmaAssembler(DateTime RaidDate)
        {
            KarmaDBEntities db = new KarmaDBEntities();
            var DBRosterList = db.chartable.Join(db.playerdata, c => c.idplayer, p => p.idPlayer, (c, p) => new { c, p }).Where(t => t.p.IsActive == 1).OrderBy(s => s.p.idPlayer).ToList();
            string ID = "";
            sbyte isActive = 0;
            List<KL> ResultList = new List<KL>();
            KL Row = new KL();
            foreach (var Character in DBRosterList)
            {
                ID = Character.p.idPlayer;
                isActive = Character.p.IsActive ?? 1;
                if (ID != "" && ID != null)
                {
                    Row = new KL();
                    //Получить данные о персе
                    Row.ID = ID;
                    Row.CHARS = CharAssembler(ID);
                    //Получить данные о аттендансе
                    Row.KARMA = KarmaGet(ID,RaidDate);
                    Row.Date = RaidDate;
                }
                ResultList.Add(Row);
            }
            IEnumerable<List<KL>> RGList = ResultList.GroupBy(x => x.ID)
                                            .Select(group => group.ToList())
                                            .ToList();
            return RGList;
        }

        public void dataInsert (string par1, string par2, string par3, string par4)
        {
            string cnnString = "server=localhost;user id=root;password=Kageretsu;persistsecurityinfo=True;database=staticdb";
            MySqlConnection cnn = new MySqlConnection(cnnString);
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = cnn;
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "CharInsert";
            cmd.Parameters.Add(new MySqlParameter("InputNickname", par1));
            cmd.Parameters.Add(new MySqlParameter("InputPlayerID", par2));
            cmd.Parameters.Add(new MySqlParameter("InputCharRole", par3));
            cmd.Parameters.Add(new MySqlParameter("InputCharClass", par4));
            //add any parameters the stored procedure might require
            cnn.Open();
            var result = cmd.ExecuteNonQuery();
            cnn.Close();
        }

        public void ActivChanger(string IDPlayer, sbyte Activator)
        {
            string cnnString = "server=localhost;user id=root;password=Kageretsu;persistsecurityinfo=True;database=staticdb";
            MySqlConnection cnn = new MySqlConnection(cnnString);
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = cnn;
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "CharActive";
            cmd.Parameters.Add(new MySqlParameter("InputActivator", Activator));
            cmd.Parameters.Add(new MySqlParameter("InputPlayerID", IDPlayer));
            //add any parameters the stored procedure might require
            cnn.Open();
            var result = cmd.ExecuteNonQuery();
            cnn.Close();
        }

        public void AbsebtInsert(string par1, string par2, DateTime par3, DateTime par4)
        {
            string cnnString = "server=localhost;user id=root;password=Kageretsu;persistsecurityinfo=True;database=staticdb";

            MySqlConnection cnn = new MySqlConnection(cnnString);
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = cnn;
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "AbsentInsert";
            cmd.Parameters.Add(new MySqlParameter("reason", par1));
            cmd.Parameters.Add(new MySqlParameter("InputPlayerID", par2));
            cmd.Parameters.Add(new MySqlParameter("date1", par3));
            cmd.Parameters.Add(new MySqlParameter("date2", par4));
            //add any parameters the stored procedure might require
            cnn.Open();
            var result = cmd.ExecuteNonQuery();
            cnn.Close();
        }

        [Route("", Name = HomeControllerRoute.GetIndex)]
        public ActionResult Index()
        {
            return this.View(HomeControllerAction.Index);
        }

        [HttpGet]
        [Route("GetAbsForm", Name = HomeControllerRoute.GetAbsForm)]
        public ActionResult GetAbsForm()
        {
            ViewBag.NewAbsent = true;
            ViewBag.Description = "Введите ID игрока и выберите даты отсутствия, укажите причину отсутствия";
            return this.View(HomeControllerAction.AbsForm);
        }

        [HttpGet]
        [Route("Login", Name = HomeControllerRoute.Login)]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string User, string Password)
        {
            var SysUser = db.user.Where(u => u.username == User && u.password == Password).ToList();
            if (SysUser.Count() > 0)
            {
                ConfigurationManager.AppSettings["RLState"] = "true";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult GetAbsForm(KarmaDBEntities Roster, string Reason, string IDPlayer, DateTime? Date1, DateTime? Date2)
        {
            ViewBag.NewAbsent = false;
            ViewBag.Description = "Ваша заявка внесена";
            if (Date2.HasValue)
            { Date2 = Date1; };
            DateTime RealDate1 = Date1 ?? DateTime.Now;
            DateTime RealDate2 = Date2 ?? DateTime.Now;
            AbsebtInsert(Reason, IDPlayer, RealDate1, RealDate2);
            return this.View(HomeControllerAction.AbsForm);
        }

        [HttpGet]
        [Route("GetSettings", Name = HomeControllerRoute.GetSettings)]
        public ActionResult GetSettings()
        {
            KarmaDBEntities db = new KarmaDBEntities();
            var DBRolesList = db.settingstable.ToList();
            ViewBag.kwOValue = DBRolesList[0].ValueSetting;
            ViewBag.AValue = DBRolesList[1].ValueSetting;
            ViewBag.wMValue = DBRolesList[2].ValueSetting;
            ViewBag.kwAValue = DBRolesList[3].ValueSetting;
            ViewBag.STValue = DBRolesList[4].ValueSetting;
            ViewBag.wPValue = DBRolesList[5].ValueSetting;
            ViewBag.kwBValue = DBRolesList[6].ValueSetting;
            return this.View(HomeControllerAction.GetSettings);
        }

        [HttpPost]
        public ActionResult GetSettings(KarmaDBEntities Roster, int AValue, int wPValue, int wMValue, int kwBValue, int kwOValue, int kwAValue, int STValue)
        {
            KarmaDBEntities db = new KarmaDBEntities();
            var DBRolesList = db.settingstable.ToList();
            DBRolesList[0].ValueSetting = kwOValue;
            DBRolesList[1].ValueSetting = AValue;
            DBRolesList[2].ValueSetting = wMValue;
            DBRolesList[3].ValueSetting = kwAValue;
            DBRolesList[4].ValueSetting = STValue;
            DBRolesList[5].ValueSetting = wPValue;
            DBRolesList[6].ValueSetting = kwBValue;
            ViewBag.kwOValue = DBRolesList[0].ValueSetting;
            ViewBag.AValue = DBRolesList[1].ValueSetting;
            ViewBag.wMValue = DBRolesList[2].ValueSetting;
            ViewBag.kwAValue = DBRolesList[3].ValueSetting;
            ViewBag.STValue = DBRolesList[4].ValueSetting;
            ViewBag.wPValue = DBRolesList[5].ValueSetting;
            ViewBag.kwBValue = DBRolesList[6].ValueSetting;
            db.SaveChanges();
            return this.View(HomeControllerAction.GetSettings);
        }

        [HttpGet]
        [Route("GetRegChar", Name = HomeControllerRoute.GetRegChar)]
        public ActionResult GetRegChar()
        {
            KarmaDBEntities db = new KarmaDBEntities();
            var DBRolesList = db.roles_list.ToList();
            string ItemName = "";
            DropDownList DropDownListRoles = new DropDownList();
            ArrayList items = new ArrayList();
            foreach (var item in DBRolesList)
            {
                ItemName = item.Id_Roles;
                items.Add(new ListItem(ItemName));
            }
            DropDownListRoles.DataSource = items;
            ViewBag.DropRolesList = new SelectList(db.roles_list.Select(i => i.Id_Roles).ToList(), "Id_Roles");
            var DBClassList = db.class_list.ToList();
            ItemName = "";
            DropDownList DropDownListClass = new DropDownList();
            items = new ArrayList();
            foreach (var item in DBRolesList)
            {
                ItemName = item.Id_Roles;
                items.Add(new ListItem(ItemName));
            }
            DropDownListRoles.DataSource = items;
            ViewBag.DropClassList = new SelectList(db.class_list.Select(i => i.Id_Class).ToList(), "Id_Class");
            ViewBag.Description = "Заполните данные чтобы ваш персонаж добавился в ростер. Укажите ID для привязки персонажа.";
            ViewBag.NewRegistr = true;
            return this.View(HomeControllerAction.GetRegChar);
        }

        [HttpPost]
        public ViewResult GetRegChar(KarmaDBEntities Roster, string Nickname, string IDPlayer, string DropDownListClass, string DropDownListRoles)
        {
            ViewBag.Description = "Персонаж зарегистрирован";
            ViewBag.NewRegistr = false;
            dataInsert(Nickname, IDPlayer, DropDownListRoles, DropDownListClass);
            return this.View(HomeControllerAction.GetRegChar);
        }

        [HttpGet]
        [Route("Roster", Name = HomeControllerRoute.Roster)]
        public ActionResult Roster()
        {
            KarmaDBEntities db = new KarmaDBEntities();
            var DBRosterList = db.chartable.Join(db.playerdata, c => c.idplayer, p => p.idPlayer, (c, p) => new { c, p });
            var DBCharsList = db.playerdata.OrderByDescending(t => t.IsActive).ToList();
            var QueryData = DBRosterList.Where(t => t.c.char_role == "ТАНК" && t.p.IsActive == 1).ToList();
            ViewBag.ATanksCount = QueryData.Count();
            QueryData = DBRosterList.Where(t => t.c.char_role == "МДД" && t.p.IsActive == 1).ToList();
            ViewBag.AMddCount = QueryData.Count();
            QueryData = DBRosterList.Where(t => t.c.char_role == "РДД" && t.p.IsActive == 1).ToList();
            ViewBag.ARddCount = QueryData.Count();
            QueryData = DBRosterList.Where(t => t.c.char_role == "ХИЛЛ" && t.p.IsActive == 1).ToList();
            ViewBag.AHealsCount = QueryData.Count();
            QueryData = DBRosterList.Where(t => t.c.char_role == "ТАНК" && t.p.IsActive == 0).ToList();
            ViewBag.ITanksCount = QueryData.Count();
            QueryData = DBRosterList.Where(t => t.c.char_role == "МДД" && t.p.IsActive == 0).ToList();
            ViewBag.IMddCount = QueryData.Count();
            QueryData = DBRosterList.Where(t => t.c.char_role == "РДД" && t.p.IsActive == 0).ToList();
            ViewBag.IRddCount = QueryData.Count();
            QueryData = DBRosterList.Where(t => t.c.char_role == "ХИЛЛ" && t.p.IsActive == 0).ToList();
            ViewBag.IHealsCount = QueryData.Count();
            return this.View(DBCharsList);
        }

        [HttpPost]
        public ActionResult Roster(string IDPlayer, sbyte Active)
        {
            ActivChanger(IDPlayer, Active);
            return RedirectToAction("Roster");
        }

        [HttpGet]
        public ActionResult RosterMember(string id)
        {
            var chartable = db.chartable.Where(t=>t.idplayer == id).ToList();
            ViewBag.ID = id;
            ViewBag.Karma = KarmaAsemble(id).ToString("#.##");
            ViewBag.Attendance = NadezAsemble(id).ToString("0.##") + "%";
            return PartialView(chartable);
        }

        [HttpGet]
        public ActionResult RosterCharacter(string CharName)
        {
            chartable chartable = db.chartable.Where(t => t.charname == CharName).First();
            switch (chartable.char_class)
            {
                case "Рыцарь смерти":
                    ViewBag.ClassName = "DK";
                    break;
                case "Друид":
                    ViewBag.ClassName = "DRU";
                    break;
                case "Варлок":
                    ViewBag.ClassName = "LOCK";
                    break;
                case "Воин":
                    ViewBag.ClassName = "WAR";
                    break;
                case "Маг":
                    ViewBag.ClassName = "MAG";
                    break;
                case "Монах":
                    ViewBag.ClassName = "MONK";
                    break;
                case "Охотник":
                    ViewBag.ClassName = "HUNT";
                    break;
                case "Охотник на демонов":
                    ViewBag.ClassName = "PIDOR";
                    break;
                case "Паладин":
                    ViewBag.ClassName = "PAL";
                    break;
                case "Прист":
                    ViewBag.ClassName = "PRIST";
                    break;
                case "Разбойник":
                    ViewBag.ClassName = "GAY";
                    break;
                case "Шаман":
                    ViewBag.ClassName = "DOG";
                    break;
                default:
                    ViewBag.ClassName = "";
                    break;
            }
            return PartialView(chartable);
        }

        [HttpGet]
        [Route("Karma", Name = HomeControllerRoute.Karma)]
        public ActionResult Karma()
        {
            return RedirectToAction("Index", "raiddates");
        }
        /// <summary>
        /// Gets the Atom 1.0 feed for the current site. Note that Atom 1.0 is used over RSS 2.0 because Atom 1.0 is a 
        /// newer and more well defined format. Atom 1.0 is a standard and RSS is not. See
        /// http://rehansaeed.com/building-rssatom-feeds-for-asp-net-mvc/
        /// </summary>
        /// <returns>The Atom 1.0 feed for the current site.</returns>
        [OutputCache(CacheProfile = CacheProfileName.Feed)]
        [Route("feed", Name = HomeControllerRoute.GetFeed)]
        public async Task<ActionResult> Feed()
        {
            // A CancellationToken signifying if the request is cancelled. See
            // http://www.davepaquette.com/archive/2015/07/19/cancelling-long-running-queries-in-asp-net-mvc-and-web-api.aspx
            CancellationToken cancellationToken = this.Response.ClientDisconnectedToken;
            return new AtomActionResult(await this.feedService.GetFeed(cancellationToken));
        }

        [Route("search", Name = HomeControllerRoute.GetSearch)]
        public ActionResult Search(string query)
        {
            // You can implement a proper search function here and add a Search.cshtml page.
            // return this.View(HomeControllerAction.Search);

            // Or you could use Google Custom Search (https://cse.google.co.uk/cse) to index your site and display your 
            // search results in your own page.

            // For simplicity we are just assuming your site is indexed on Google and redirecting to it.
            return this.Redirect(string.Format(
                "https://www.google.co.uk/?q=site:{0} {1}", 
                this.Url.AbsoluteRouteUrl(HomeControllerRoute.GetIndex),
                query));
        }

        /// <summary>
        /// Gets the browserconfig XML for the current site. This allows you to customize the tile, when a user pins 
        /// the site to their Windows 8/10 start screen. See http://www.buildmypinnedsite.com and 
        /// https://msdn.microsoft.com/en-us/library/dn320426%28v=vs.85%29.aspx
        /// </summary>
        /// <returns>The browserconfig XML for the current site.</returns>
        [NoTrailingSlash]
        [OutputCache(CacheProfile = CacheProfileName.BrowserConfigXml)]
        [Route("browserconfig.xml", Name = HomeControllerRoute.GetBrowserConfigXml)]
        public ContentResult BrowserConfigXml()
        {
            Trace.WriteLine(string.Format(
                "browserconfig.xml requested. User Agent:<{0}>.",
                this.Request.Headers.Get("User-Agent")));
            string content = this.browserConfigService.GetBrowserConfigXml();
            return this.Content(content, ContentType.Xml, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the manifest JSON for the current site. This allows you to customize the icon and other browser 
        /// settings for Chrome/Android and FireFox (FireFox support is coming). See https://w3c.github.io/manifest/
        /// for the official W3C specification. See http://html5doctor.com/web-manifest-specification/ for more 
        /// information. See https://developer.chrome.com/multidevice/android/installtohomescreen for Chrome's 
        /// implementation.
        /// </summary>
        /// <returns>The manifest JSON for the current site.</returns>
        [NoTrailingSlash]
        [OutputCache(CacheProfile = CacheProfileName.ManifestJson)]
        [Route("manifest.json", Name = HomeControllerRoute.GetManifestJson)]
        public ContentResult ManifestJson()
        {
            Trace.WriteLine(string.Format(
                "manifest.jsonrequested. User Agent:<{0}>.",
                this.Request.Headers.Get("User-Agent")));
            string content = this.manifestService.GetManifestJson();
            return this.Content(content, ContentType.Json, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the Open Search XML for the current site. You can customize the contents of this XML here. The open 
        /// search action is cached for one day, adjust this time to whatever you require. See
        /// http://www.hanselman.com/blog/CommentView.aspx?guid=50cc95b1-c043-451f-9bc2-696dc564766d
        /// http://www.opensearch.org
        /// </summary>
        /// <returns>The Open Search XML for the current site.</returns>
        [NoTrailingSlash]
        [OutputCache(CacheProfile = CacheProfileName.OpenSearchXml)]
        [Route("opensearch.xml", Name = HomeControllerRoute.GetOpenSearchXml)]
        public ContentResult OpenSearchXml()
        {
            Trace.WriteLine(string.Format(
                "opensearch.xml requested. User Agent:<{0}>.", 
                this.Request.Headers.Get("User-Agent")));
            string content = this.openSearchService.GetOpenSearchXml();
            return this.Content(content, ContentType.Xml, Encoding.UTF8);
        }

        /// <summary>
        /// Tells search engines (or robots) how to index your site. 
        /// The reason for dynamically generating this code is to enable generation of the full absolute sitemap URL
        /// and also to give you added flexibility in case you want to disallow search engines from certain paths. The 
        /// sitemap is cached for one day, adjust this time to whatever you require. See
        /// http://rehansaeed.com/dynamically-generating-robots-txt-using-asp-net-mvc/
        /// </summary>
        /// <returns>The robots text for the current site.</returns>
        [NoTrailingSlash]
        [OutputCache(CacheProfile = CacheProfileName.RobotsText)]
        [Route("robots.txt", Name = HomeControllerRoute.GetRobotsText)]
        public ContentResult RobotsText()
        {
            Trace.WriteLine(string.Format(
                "robots.txt requested. User Agent:<{0}>.", 
                this.Request.Headers.Get("User-Agent")));
            string content = this.robotsService.GetRobotsText();
            return this.Content(content, ContentType.Text, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the sitemap XML for the current site. You can customize the contents of this XML from the 
        /// <see cref="SitemapService"/>. The sitemap is cached for one day, adjust this time to whatever you require.
        /// http://www.sitemaps.org/protocol.html
        /// </summary>
        /// <param name="index">The index of the sitemap to retrieve. <c>null</c> if you want to retrieve the root 
        /// sitemap file, which may be a sitemap index file.</param>
        /// <returns>The sitemap XML for the current site.</returns>
        [NoTrailingSlash]
        [Route("sitemap.xml", Name = HomeControllerRoute.GetSitemapXml)]
        public ActionResult SitemapXml(int? index = null)
        {
            string content = this.sitemapService.GetSitemapXml(index);

            if (content == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Sitemap index is out of range.");
            }

            return this.Content(content, ContentType.Xml, Encoding.UTF8);
        }  
    }
    
    
}