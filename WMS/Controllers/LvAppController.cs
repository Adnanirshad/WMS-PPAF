using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WMS.Models;
using PagedList;
using WMS.CustomClass;
using WMS.Controllers.Filters;
using WMS.HelperClass;
namespace WMS.Controllers
{
    [CustomControllerAttributes]
    public class LvAppController : Controller
    {
        private TAS2013Entities db = new TAS2013Entities();

        // GET: /LvApp/
        public ActionResult Index(string sortOrder, string searchString, string currentFilter, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.TypeSortParm = sortOrder == "LvType" ? "LvType_desc" : "LvType";
            ViewBag.DateSortParm = sortOrder == "Date" ? "Date_desc" : "Date";
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
            //string query = qb.MakeCustomizeQuery(LoggedInUser);
            DateTime dt1 = DateTime.Today;
            DateTime dt2 = new DateTime(dt1.Year, 1, 1);
            string date = dt2.Year.ToString()+"-"+dt2.Month.ToString()+"-"+dt2.Day.ToString()+" ";
            DataTable dt = qb.GetValuesfromDB("select * from ViewLvApplication  where(ToDate >= '" + date + "')");
            List<ViewLvApplication> lvapplications = dt.ToList<ViewLvApplication>();


            ViewBag.CurrentFilter = searchString;
            //var lvapplications = db.LvApplications.Where(aa=>aa.ToDate>=dt2).Include(l => l.Emp).Include(l => l.LvType1);
            if (!String.IsNullOrEmpty(searchString))
            {
                lvapplications = lvapplications.Where(s => s.EmpName.ToUpper().Contains(searchString.ToUpper())
                     || s.EmpNo.ToUpper().Contains(searchString.ToUpper())).ToList();
            }

            switch (sortOrder)
            {
                case "name_desc":
                    lvapplications = lvapplications.OrderByDescending(s => s.EmpName).ToList();
                    break;

                case "LvType_desc":
                    lvapplications = lvapplications.OrderByDescending(s => s.LeaveTypeID).ToList();
                    break;
                case "LvType":
                    lvapplications = lvapplications.OrderBy(s => s.LeaveTypeID).ToList();
                    break;
                case "Date_desc":
                    lvapplications = lvapplications.OrderByDescending(s => s.LvDate).ToList();
                    break;
                case "Date":
                    lvapplications = lvapplications.OrderBy(s => s.LvDate).ToList();
                    break;
                default:
                    lvapplications = lvapplications.OrderBy(s => s.EmpName).ToList();
                    break;
            }
            int pageSize = 8;
            int pageNumber = (page ?? 1);
            return View(lvapplications.OrderBy(aa=>aa.LvDate).ToPagedList(pageNumber, pageSize));
        }

        // GET: /LvApp/Details/5
          [CustomActionAttribute]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LvApplication lvapplication = db.LvApplications.Find(id);
            if (lvapplication == null)
            {
                return HttpNotFound();
            }
            return View(lvapplication);
        }

        // GET: /LvApp/Create
          [CustomActionAttribute]
        public ActionResult Create()
        {
            ViewBag.EmpID = new SelectList(db.Emps.OrderBy(s=>s.EmpName), "EmpID", "EmpNo");
            ViewBag.LeaveTypeID = new SelectList(db.LvTypes.Where(aa => aa.Enable == true).OrderBy(s => s.LvTypeID).ToList(), "LvTypeID", "LvDesc");
            return View();
        }
        
        // POST: /LvApp/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomActionAttribute]
          public ActionResult Create([Bind(Include = "LvID,LvDate,LeaveTypeID,EmpID,FromDate,ToDate,NoOfDays,IsHalf,FirstHalf,HalfAbsent,LvReason,LvAddress,CreatedBy,ApprovedBy,Status")] LvApplication lvapplication)
            {
            User LoggedInUser = Session["LoggedUser"] as User;
            if (lvapplication.FromDate.Date > lvapplication.ToDate.Date)
                ModelState.AddModelError("FromDate", "From Date should be smaller than To Date");
            string _EmpNo = Request.Form["EmpNo"].ToString();
            List<Emp> _emp = db.Emps.Where(aa => aa.EmpNo == _EmpNo).ToList();
            if (_emp.Count == 0 )
            {
                ModelState.AddModelError("EmpNo", "Emp No not exist");
            }
            else
            {
                lvapplication.EmpID = _emp.FirstOrDefault().EmpID;
            }
            if (ModelState.IsValid)
            {
                LvType lvType = db.LvTypes.First(aa => aa.LvTypeID == lvapplication.LeaveTypeID);
                LeaveController LvProcessController = new LeaveController();
                if (LvProcessController.CheckDuplicateLeave(lvapplication))
                {
                    // max days
                    float noofDays = LvProcessController.CalculateNoOfDays(lvapplication, lvType);
                    lvapplication.NoOfDays = noofDays;
                    int _UserID = Convert.ToInt32(Session["LogedUserID"].ToString());
                    lvapplication.CreatedBy = _UserID;
                    if (lvType.UpdateBalance == true)
                    {
                        if (LvProcessController.HasLeaveQuota(lvapplication.EmpID, lvapplication.LeaveTypeID, lvType))
                        {
                            if (LvProcessController.CheckLeaveBalance(lvapplication, lvType))
                            {
                                if (LvProcessController.CheckForMaxMonthDays(lvapplication, lvType))
                                {
                                    if (lvType.MaxDaysConsective == 0)
                                    {
                                        CreateLeave(lvapplication, lvType);
                                        return RedirectToAction("Index");
                                    }
                                    if (noofDays <= lvType.MaxDaysConsective)
                                    {
                                        CreateLeave(lvapplication, lvType);
                                        return RedirectToAction("Index");
                                    }
                                    else
                                        ModelState.AddModelError("FromDate", "Leave Consective days exceeds");
                                }
                                else
                                    ModelState.AddModelError("FromDate", "Leave Monthly Quota Exceeds");
                            }
                            else
                                ModelState.AddModelError("LeaveTypeID", "Leave Balance Exceeds, Please check the balance");
                        }
                        else
                            ModelState.AddModelError("LeaveTypeID", "Leave Quota does not exist");
                    }
                    else
                    {
                        CreateLeave(lvapplication, lvType);
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    ModelState.AddModelError("FromDate", "This Employee already has leave of this date ");
                }
            }
            ViewBag.EmpID = new SelectList(db.Emps.OrderBy(s => s.EmpName), "EmpID", "EmpNo", lvapplication.EmpID);
            ViewBag.LeaveTypeID = new SelectList(db.LvTypes.Where(aa => aa.Enable == true).OrderBy(s => s.LvTypeID), "LvTypeID", "LvDesc", lvapplication.LvType);
            return View(lvapplication);
        }
        public void CreateLeave(LvApplication lvapplication, LvType lvType)
        {
            LeaveController LvProcessController = new LeaveController();
            lvapplication.LvDate = DateTime.Today;
            int _userID = Convert.ToInt32(Session["LogedUserID"].ToString());
            lvapplication.CreatedBy = _userID;
            lvapplication.Active = true;
            db.LvApplications.Add(lvapplication);
            if (db.SaveChanges() > 0)
            {
                HelperClass.MyHelper.SaveAuditLog(_userID, (byte)MyEnums.FormName.Leave, (byte)MyEnums.Operation.Add, DateTime.Now);
                LvProcessController.AddLeaveToLeaveData(lvapplication, lvType);
                LvProcessController.AddLeaveToLeaveAttData(lvapplication, lvType);
            }
            else
            {
                ModelState.AddModelError("LvType", "There is an error while creating leave.");
            }
        }
          [CustomActionAttribute]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LvApplication lvapplication = db.LvApplications.Find(id);
            if (lvapplication == null)
            {
                return HttpNotFound();
            }
            ViewBag.EmpID = new SelectList(db.Emps.OrderBy(s=>s.EmpName), "EmpID", "EmpNo", lvapplication.EmpID);
            ViewBag.LvType = new SelectList(db.LvTypes.Where(aa => aa.Enable == true).OrderBy(s=>s.LvTypeID).ToList(), "LvType1", "LvDesc", lvapplication.LvType);
            return View(lvapplication);
        }

        // POST: /LvApp/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomActionAttribute]
        public ActionResult Edit([Bind(Include = "LvID,LvDate,LvType,EmpID,FromDate,ToDate,NoOfDays,IsHalf,HalfAbsent,LvReason,LvAddress,CreatedBy,ApprovedBy,Status")] LvApplication lvapplication)
        {
            if (ModelState.IsValid)
            {
                User LoggedInUser = Session["LoggedUser"] as User;
                db.Entry(lvapplication).State = EntityState.Modified;
                db.SaveChanges();
                int _userID = Convert.ToInt32(Session["LogedUserID"].ToString());
                HelperClass.MyHelper.SaveAuditLog(_userID, (byte)MyEnums.FormName.Leave, (byte)MyEnums.Operation.Edit, DateTime.Now);
                return RedirectToAction("Index");
            }
            ViewBag.EmpID = new SelectList(db.Emps.OrderBy(s=>s.EmpName), "EmpID", "EmpNo", lvapplication.EmpID);
            ViewBag.LvType = new SelectList(db.LvTypes.Where(aa => aa.Enable == true).OrderBy(s=>s.LvTypeID).ToList(), "LvType1", "LvDesc", lvapplication.LvType);
            return View(lvapplication);
        }

        // GET: /LvApp/Delete/5
          [CustomActionAttribute]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LvApplication lvapplication = db.LvApplications.Find(id);
            if (lvapplication == null)
            {
                return HttpNotFound();
            }
            return View(lvapplication);
        }

        // POST: /LvApp/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomActionAttribute]
        public ActionResult DeleteConfirmed(int id)
        {
            LeaveController LvProcessController = new LeaveController();
            LvApplication lvapplication = db.LvApplications.Find(id);
            if (lvapplication.IsHalf == false)
            {
                LvProcessController.DeleteFromLVData(lvapplication);
                LvProcessController.DeleteLeaveFromAttData(lvapplication);
                LvProcessController.UpdateLeaveBalance(lvapplication);
                //lvapplication.Active = false;
                db.LvApplications.Remove(lvapplication);
            }
            else
            {
                LvProcessController.DeleteHLFromLVData(lvapplication);
                LvProcessController.DeleteHLFromAttData(lvapplication);
                LvProcessController.UpdateHLeaveBalance(lvapplication);
                db.LvApplications.Remove(lvapplication);
            }
            db.SaveChanges();
            //UpdateLeaveBalance(lvapplication);
            //db.LvApplications.Remove(lvapplication);
            //db.SaveChanges();
            int _userID = Convert.ToInt32(Session["LogedUserID"].ToString());
            HelperClass.MyHelper.SaveAuditLog(_userID, (byte)MyEnums.FormName.Leave, (byte)MyEnums.Operation.Delete, DateTime.Now);
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
