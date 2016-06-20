using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using WMS.Models;
using WMS.CustomClass;
using System.Data;
using System.Net;
namespace WMS.Controllers
{
    public class JobCardController : Controller
    {
        //
        // GET: /JobCard/
        public ActionResult Index()
        {
            return RedirectToAction("JCCreate");
        }
        private TAS2013Entities db = new TAS2013Entities();

        public ActionResult JCCreate(string sortOrder, string searchString, string currentFilter, int? page)
        {
            ViewData["JobDateFrom"] = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            ViewData["JobDateTo"] = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.JobCardType = new SelectList(db.JobCards, "WorkCardID", "WorkCardName");
            ViewBag.CrewID = new SelectList(db.Crews, "CrewID", "CrewName");
            return View();
        }
        // Job Card create Action
        [HttpPost]
        public ActionResult EditAttJobCard()
        {
            User LoggedInUser = Session["LoggedUser"] as User;
            string Message = "";
            QueryBuilder qb = new QueryBuilder();
            string query = qb.MakeCustomizeQuery(LoggedInUser);
            DataTable dt = qb.GetValuesfromDB("select * from EmpView " + query);
            string criteria = Request.Form["CreateJCCriteria"].ToString();
            short _WorkCardID = Convert.ToInt16(Request.Form["JobCardType"].ToString());
            //First Save Job Card Application
            JobCardApp jobCardApp = new JobCardApp();
            jobCardApp.CardType = _WorkCardID;
            jobCardApp.DateCreated = DateTime.Now;
            jobCardApp.DateStarted = Convert.ToDateTime(Request.Form["JobDateFrom"]);
            jobCardApp.DateEnded = Convert.ToDateTime(Request.Form["JobDateTo"]);
            jobCardApp.Status = false;
            string Remakrs = Request.Form["Remakrs"].ToString();
            if (Remakrs != "")
                jobCardApp.Remarks = Remakrs;
            jobCardApp.UserID = LoggedInUser.UserID;
            switch (criteria)
            {
                case "ByCrew":
                    jobCardApp.CriteriaDate = Convert.ToInt32(Request.Form["CrewID"].ToString());
                    jobCardApp.JobCardCriteria = "C";
                    if (ValidateJobCard(jobCardApp))
                    {
                        db.JobCardApps.Add(jobCardApp);
                        if (db.SaveChanges() > 0)
                            AddJobCardAppToJobCardData();
                    }
                    else
                        Message = "Job Card Validation failed";
                    break;
                case "ByEmployee":
                    string EmpNo = "";
                    EmpNo = Request.Form["JobEmpNo"];
                    List<Emp> emptemp = db.Emps.Where(aa => aa.EmpNo == EmpNo).ToList();
                    if (emptemp.Count > 0)
                    {
                        jobCardApp.CriteriaDate = emptemp.FirstOrDefault().EmpID;
                        jobCardApp.JobCardCriteria = "E";
                        if (ValidateJobCard(jobCardApp))
                        {
                            db.JobCardApps.Add(jobCardApp);
                            if (db.SaveChanges() > 0)
                                AddJobCardAppToJobCardData();
                        }
                        else
                            Message = "Job Card Validation failed";
                    }
                    else
                        Message = "There is no employee found";
                    break;
            }
            //List<EmpView> emps = new List<EmpView>();
            ViewData["JobDateFrom"] = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            ViewData["JobDateTo"] = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.JobCardType = new SelectList(db.JobCards, "WorkCardID", "WorkCardName");
            ViewBag.CrewID = new SelectList(db.Crews, "CrewID", "CrewName");
            if (Message == "")
                Message = "Job Card created sucessfully";
            ViewBag.CMessage = Message;
            return View("JCCreate");
        }

        public ActionResult JobCardList(string sortOrder, string searchString, string currentFilter, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DesigSortParm = sortOrder == "designation" ? "designation_desc" : "designation";
            ViewBag.SectionSortParm = sortOrder == "section" ? "section_desc" : "section";
            //List<EmpView> emps = new List<EmpView>();
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            User LoggedInUser = Session["LoggedUser"] as User;
            QueryBuilder qb = new QueryBuilder();
            string query = qb.MakeCustomizeQuery(LoggedInUser);
            DataTable dt = qb.GetValuesfromDB("select * from ViewJobCardApp ");
            List<ViewJobCardApp> jobCardsApps = dt.ToList<ViewJobCardApp>();

            // List<EmpView> emps = db.EmpViews.ToList();
            ViewBag.CurrentFilter = searchString;

            if (!String.IsNullOrEmpty(searchString))
            {
                try
                {

                    jobCardsApps = jobCardsApps.Where(s => s.EmpName.ToUpper().Contains(searchString.ToUpper())
                        || s.EmpNo.ToUpper().Contains(searchString.ToUpper()) || s.DesignationName.ToString().Contains(searchString)
                    ).OrderByDescending(aa => aa.DateCreated).ToList();
                }
                catch (Exception ex)
                {

                }
            }

            switch (sortOrder)
            {
                case "name_desc":
                    jobCardsApps = jobCardsApps.OrderByDescending(s => s.EmpName).ToList();
                    break;
                case "designation_desc":
                    jobCardsApps = jobCardsApps.OrderByDescending(s => s.DesignationName).ToList();
                    break;
                case "designation":
                    jobCardsApps = jobCardsApps.OrderBy(s => s.DesignationName).ToList();
                    break;
                case "section_desc":
                    jobCardsApps = jobCardsApps.OrderByDescending(s => s.SectionName).ToList();
                    break;
                case "section":
                    jobCardsApps = jobCardsApps.OrderBy(s => s.SectionName).ToList();
                    break;
                default:
                    jobCardsApps = jobCardsApps.OrderBy(s => s.EmpName).ToList();
                    break;
            }
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(jobCardsApps.ToPagedList(pageNumber, pageSize));
        }

        #region -- Job Card Create Helper --
        private bool ValidateJobCard(JobCardApp jobCardApp)
        {
            List<JobCardApp> _Lc = new List<JobCardApp>();
            DateTime _DTime = new DateTime();
            DateTime _DTimeLV = new DateTime();
            _Lc = db.JobCardApps.Where(aa => aa.CriteriaDate == jobCardApp.CriteriaDate).ToList();
            foreach (var item in _Lc)
            {
                _DTime = (DateTime)item.DateStarted;
                _DTimeLV = (DateTime)jobCardApp.DateStarted;
                while (_DTime <= item.DateEnded)
                {
                    while (_DTimeLV <= jobCardApp.DateEnded)
                    {
                        if (_DTime == _DTimeLV)
                            return false;
                        _DTimeLV = _DTimeLV.AddDays(1);
                    }
                    _DTime = _DTime.AddDays(1);
                }
            }
            return true;
        }
        private void AddJobCardAppToJobCardData()
        {
            List<JobCardApp> _jobCardApp = new List<JobCardApp>();
            _jobCardApp = db.JobCardApps.Where(aa => aa.Status == false).ToList();
            List<Emp> _Emp = new List<Emp>();
            foreach (var jcApp in _jobCardApp)
            {
                jcApp.Status = true;
                switch (jcApp.JobCardCriteria)
                {

                    case "E":
                        int _EmpID = (int)jcApp.CriteriaDate;
                        _Emp = db.Emps.Where(aa => aa.EmpID == _EmpID).ToList();
                        break;
                    case "C":
                        _Emp = db.Emps.Where(aa => aa.CrewID == jcApp.CriteriaDate).ToList();
                        break;
                }
                foreach (var selectedEmp in _Emp)
                {
                    AddJobCardData(selectedEmp, jcApp);
                }
            }
        }

        private bool AddJobCardDataToDatabase(string _empDate, int _empID, DateTime _currentDate, int _userID, JobCardApp jcApp)
        {
            bool check = false;
            try
            {
                JobCardDetail _jobCardEmp = new JobCardDetail();
                _jobCardEmp.EmpDate = _empDate;
                _jobCardEmp.EmpID = _empID;
                _jobCardEmp.Dated = _currentDate;
                //_jobCardEmp.SubmittedFrom = _userID;
                _jobCardEmp.WrkCardID = jcApp.CardType;
                //_jobCardEmp.DateCreated = DateTime.Now;
                //_jobCardEmp.WorkMin = jcApp.WorkMin;
                _jobCardEmp.JCAppID = jcApp.JobCardID;
                _jobCardEmp.Remarks = jcApp.Remarks;
                db.JobCardDetails.Add(_jobCardEmp);
                if (db.SaveChanges() > 0)
                {
                    check = true;
                }
            }
            catch (Exception ex)
            {
                check = false;
            }
            return check;
        }

        private void AddJobCardData(Emp _selEmp, JobCardApp jcApp)
        {
            int _empID = _selEmp.EmpID;
            string _empDate = "";
            int _userID = (int)jcApp.UserID;
            DateTime _Date = (DateTime)jcApp.DateStarted;
            while (_Date <= jcApp.DateEnded)
            {
                _empDate = _selEmp.EmpID + _Date.ToString("yyMMdd");
                AddJobCardDataToDatabase(_empDate, _empID, _Date, _userID, jcApp);
                if (db.AttProcesses.Where(aa => aa.ProcessDate == _Date).Count() > 0)
                {
                    //1	Official Duty
                    //2	Present
                    //3	Absent
                    //5	Special Holiday
                    switch (jcApp.CardType)
                    {

                        case 1:// Official Duty
                            AddJCToAttData(_empDate, _empID, _Date,"O",false,false,false,false,true,"Offical Duty");
                            break;
                        case 2:// Present
                            AddJCToAttData(_empDate, _empID, _Date, "D", false, false, false, false, true, "Present");
                            break;
                        case 3:// Absent
                            AddJCToAttData(_empDate, _empID, _Date, "D",true, false, false, false, false, "Absent");
                            break;
                        case 5:// Field Visit
                            AddJCToAttData(_empDate, _empID, _Date, "D", false, false, false, false, true, "Field Visit");
                            break;
                        case 6:// Monitoring Visit
                            AddJCToAttData(_empDate, _empID, _Date, "D", false, false, false, false, true, "Monitoring Visit");
                            break;
                        case 7:// Training
                            AddJCToAttData(_empDate, _empID, _Date, "D", false, false, false, false, true, "Training");
                            break;
                        case 8:// Seminar / Workshop
                            AddJCToAttData(_empDate, _empID, _Date, "D", false, false, false, false, true, "Seminar / Workshop");
                            break;
                        case 9:// Official Meeting
                            AddJCToAttData(_empDate, _empID, _Date, "D", false, false, false, false, true, "Official Meeting");
                            break;
                    }
                }
                _Date = _Date.AddDays(1);
            }
        }

        private bool AddJCToAttData(string _empDate, int _empID, DateTime _Date, string dutyCode, bool statusAB, bool statusDO, 
            bool statusLeave, bool statusGZ, bool statusP, string Remarks)
        {
            bool check = false;
            try
            {
                using (var context = new TAS2013Entities())
                {
                    AttData _attdata = context.AttDatas.FirstOrDefault(aa => aa.EmpDate == _empDate);
                    if (_attdata != null)
                    {
                        _attdata.DutyCode = dutyCode;
                        _attdata.StatusAB = statusAB;
                        _attdata.StatusDO = statusDO;
                        _attdata.StatusLeave = statusLeave;
                        _attdata.StatusGZ = statusGZ;
                        _attdata.StatusP = statusP;
                        if (_attdata.StatusAB != true)
                            _attdata.WorkMin = _attdata.ShifMin;
                        else
                            _attdata.WorkMin = 0;
                        if (_attdata.StatusDO == true)
                            _attdata.WorkMin = 0;
                        _attdata.Remarks = Remarks;
                    }
                    context.SaveChanges();
                    if (context.SaveChanges() > 0)
                        check = true;
                    context.Dispose();
                }
            }
            catch (Exception ex)
            {
            }
            return check;
        }
        #endregion



        public ActionResult DeleteJC(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            JobCardApp jobcardapp = db.JobCardApps.Find(id);
            if (jobcardapp != null)
            {
                List<JobCardDetail> jcEmps = new List<JobCardDetail>();
                jcEmps = db.JobCardDetails.Where(aa => aa.JCAppID == id).ToList();
                if (jcEmps.Count > 0)
                {
                    foreach (var item in jcEmps)
                    {
                        db.JobCardDetails.Remove(item);
                        db.SaveChanges();
                        RemoveFromAttData(item);
                    }
                }
                db.JobCardApps.Remove(jobcardapp);
                db.SaveChanges();
                //return HttpNotFound();
            }
            return RedirectToAction("JobCardList");
        }

        private void RemoveFromAttData(JobCardDetail item)
        {
            try
            {
                DateTime date = item.Dated.Value;
                List<Emp> emp = db.Emps.Where(aa => aa.EmpID == item.EmpID).ToList();
                //ManualProcess mp = new ManualProcess();
                //mp.ManualProcessAttendance(date, emp);
            }
            catch (Exception ex)
            {
            }
        }



        // GET: /JobCard/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            JobCardApp jobcardapp = db.JobCardApps.Find(id);
            if (jobcardapp == null)
            {
                return HttpNotFound();
            }
            return View(jobcardapp);
        }

        // GET: /JobCard/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /JobCard/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "JobCardID,DateCreated,DateStarted,DateEnded,CardType,UserID,JobCardCriteria,CriteriaData,Status,TimeIn,TimeOut,WorkMin")] JobCardApp jobcardapp)
        {
            if (ModelState.IsValid)
            {
                db.JobCardApps.Add(jobcardapp);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(jobcardapp);
        }

        // GET: /JobCard/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            JobCardApp jobcardapp = db.JobCardApps.Find(id);
            if (jobcardapp == null)
            {
                return HttpNotFound();
            }
            return View(jobcardapp);
        }

        // POST: /JobCard/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "JobCardID,DateCreated,DateStarted,DateEnded,CardType,UserID,JobCardCriteria,CriteriaData,Status,TimeIn,TimeOut,WorkMin")] JobCardApp jobcardapp)
        {
            if (ModelState.IsValid)
            {
                //db.Entry(jobcardapp).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(jobcardapp);
        }

        // GET: /JobCard/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            JobCardApp jobcardapp = db.JobCardApps.Find(id);
            if (jobcardapp == null)
            {
                return HttpNotFound();
            }
            return View(jobcardapp);
        }

        // POST: /JobCard/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            JobCardApp jobcardapp = db.JobCardApps.Find(id);
            db.JobCardApps.Remove(jobcardapp);
            db.SaveChanges();
            return RedirectToAction("Index");
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