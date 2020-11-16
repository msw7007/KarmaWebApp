using KarmaWEBApp.Constants;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using MySql.Data.MySqlClient;

namespace KarmaWEBApp.Models
{
    public class raiddatesController : Controller
    {
        private KarmaDBEntities db = new KarmaDBEntities();

        internal class GlobalValues
        {
            public static bool NewRaidVariable { get; internal set; }
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
        public string DateAssembler(int IDDate)
        {
            KarmaDBEntities db = new KarmaDBEntities();
            var DBDates = db.raiddatetable.Where(t => t.idRaidDateTable == IDDate).First();
            string DateString = "";
            var DTvar = DBDates.RaidDate ?? DateTime.Now;
            DateString = DTvar.ToString("MM.dd.yyyy");
            return DateString;
        }
        [HttpGet]
        public ActionResult Index(int? id, DateTime? RTDate)
        {

            var RDI = id ?? 0;

            var modelDB = db.raiddates.Where(rd => rd.RaidDate == RDI).Include(r => r.attendance_list);
            var ModelDBList = modelDB.ToList();
            var ModelItem = new raiddatetable();
            if (ModelDBList.Count()>0)
            { ModelItem = db.raiddatetable.Where(r => r.idRaidDateTable == RDI).First(); }

            ViewBag.CanKarma = true;
            ViewBag.Attenlist = db.attendance_list;
            ViewBag.RTdate = ModelItem.RaidDate;
            ViewBag.LogsHref = ModelItem.LogRef;
            return View(ModelDBList);
        }

        [HttpPost]
        public ActionResult Index([Bind(Include = "IdRaidValue,RaidDate,IDPlayer,IDAttendance,MinusAddition,PlusAddition,IDIsActive")] raiddates raiddates, int? id, DateTime? RTDate, string LogsHref)
        {
            var RDI = id ?? 0;
            if (ModelState.IsValid && raiddates.IDPlayer != "" && raiddates.IDPlayer != null)
            {
                db.Entry(raiddates).State = EntityState.Modified;
                db.SaveChanges(); 
            }
            var RTC = -1;
            if (id.HasValue) { RTC = db.raiddatetable.Where(t => t.idRaidDateTable == id).ToList().Count(); }
            if (RTDate.HasValue) { RTC = db.raiddatetable.Where(t => t.RaidDate == RTDate).ToList().Count(); } 
            if (RTC == 0)
            {
                string cnnString = "server=localhost;user id=root;password=Kageretsu;persistsecurityinfo=True;database=staticdb";
                MySqlConnection cnn = new MySqlConnection(cnnString);
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = cnn;
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = "NewRaidData";
                cmd.Parameters.Add(new MySqlParameter("InputDate", RTDate));
                //add any parameters the stored procedure might require
                cnn.Open();
                var result = cmd.ExecuteNonQuery();
                cnn.Close();
                RDI = db.raiddatetable.Where(t => t.RaidDate == RTDate).ToList().First().idRaidDateTable;
            }
            else
            {
                if (RTDate.HasValue)
                {
                    raiddatetable raiddatetable = new raiddatetable();
                    raiddatetable = db.raiddatetable.Where(t => t.RaidDate == RTDate).First();
                    raiddatetable.LogRef = LogsHref;
                    db.SaveChanges();
                }
                if (raiddates.RaidDate.HasValue)
                {
                    raiddatetable raiddatetable = new raiddatetable();
                    raiddatetable = db.raiddatetable.Where(t => t.idRaidDateTable == raiddates.RaidDate).First();
                    raiddatetable.LogRef = LogsHref;
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Index", new { id = RDI});
        }

        [HttpGet]
        public ActionResult TableKarmaFull(bool? RLState)
        {
            int TKFR = db.raiddatetable.Count() + 2;
            int[] DateArray = new int[TKFR-2] ;
            double KarmaVal = 0;
            string LogRef = "", DC = "0";
            int i = 1, j = 2, DT = 0;
            var DateArrayDB = db.raiddatetable.OrderByDescending(t => t.RaidDate).ToList();
            foreach (var DItem in DateArrayDB)
            { DateArray[j-2] = DItem.idRaidDateTable;
                j++;
            }
            int TKFC = db.playerdata.Where(r=>r.IsActive==1).Count() + 1;
            string[] CharArray = new string[TKFC];
            CharArray[0] = "0";
            foreach (var DItem in db.playerdata.Where(r => r.IsActive == 1).OrderBy(t => t.idPlayer).ToList())
            {
                CharArray[i - 1] = DItem.idPlayer;
                i++;
            }
            string[,] TableArray = new string[TKFC + 1, TKFR];
            for (i = 1; i < TKFC; i++)
            {
                TableArray[i, 0] = CharAssembler(CharArray[i - 1]);
            }
            for (i = 1; i < TKFC; i++)
            {
                TableArray[i, 1] = KarmaAsemble(CharArray[i - 1]).ToString("#.##");
            }
            for (j = 2; j < TKFR; j++)
            {
                TableArray[0, j] = DateAssembler(DateArray[j-2]);
            }
            TableArray[0, 0] = "Персонажи/Дата";
            var KarmaItem = db.raiddates.ToList();
            for (i=1; i <= TKFC; i++)
            {
                for (j = 2; j < TKFR; j++)
                {
                    DC = CharArray[i-1];
                    DT = DateArray[j-2];
                    KarmaItem = db.raiddates.Where(t => t.IDPlayer == DC && t.RaidDate == DT).ToList();
                    if (KarmaItem.Count != 0)
                    {
                        TableArray[i, j] = "Б:" + KarmaItem.First().IDAttendance;
                        if (KarmaItem.First().PlusAddition.HasValue) { TableArray[i, j] = TableArray[i, j] + "+:" + KarmaItem.First().PlusAddition; } else { TableArray[i, j] = TableArray[i, j] + "+:0"; };
                        if (KarmaItem.First().MinusAddition.HasValue) { TableArray[i, j] = TableArray[i, j] + "-:" + KarmaItem.First().MinusAddition; } else { TableArray[i, j] = TableArray[i, j] + "-:0"; };
                    }
                    else {
                        TableArray[i, j] = "Нет данных";
                    };
                    if (i == TKFC)
                    {
                        if (RLState ?? false)
                        {
                            TableArray[i, j] = DT.ToString();
                        }
                        else
                        {
                            var DBDates = db.raiddatetable.Where(t => t.idRaidDateTable == DT).First();
                            TableArray[i, j] = DBDates.LogRef;
                        }
                    }
                    
                }
            }
            ViewBag.RLMode = RLState ?? false;
            ViewBag.TableArray = TableArray;
            ViewBag.TKFR = TKFR;
            ViewBag.TKFC = TKFC;
            return PartialView();
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            raiddates raiddates = db.raiddates.Find(id);
            ViewBag.IDAttendance = new SelectList(db.attendance_list, "ID_attendance", "ID_attendance", raiddates.IDAttendance);
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (raiddates == null)
            {
                return HttpNotFound();
            }
            return PartialView(raiddates);
        }

        // POST: raiddates/Edit/5
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в статье https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public ActionResult Edit([Bind(Include = "IdRaidValue,RaidDate,IDPlayer,IDAttendance,MinusAddition,PlusAddition,IDIsActive")] raiddates raiddates, int? id)
        {
            raiddates raiddatesItem = db.raiddates.Where(r => r.IdRaidValue == id).First();
            ViewBag.IDAttendance = new SelectList(db.attendance_list, "ID_attendance", "ID_attendance", raiddatesItem.IDAttendance);
            if (id.HasValue)
            {
                var ModelDB = db.raiddates;
                var ModelItem = ModelDB.Where(r => r.IdRaidValue == id).First();
                raiddates = ModelItem;
            }
            return PartialView(raiddates);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
