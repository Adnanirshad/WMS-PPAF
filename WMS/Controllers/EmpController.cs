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
using System.IO;
using System.Web.Helpers;
using WMS.Controllers.Filters;
using WMS.HelperClass;
using WMS.CustomClass;
using System.Data.SqlClient;
using System.Configuration;
using System.Reflection;
using System.Data.Entity.Validation;
using System.Text;

namespace WMS.Controllers
{
    [CustomControllerAttributes]
    public class EmpController : Controller
    {
        private TAS2013Entities db = new TAS2013Entities();
        // GET: /Emp/
        public ActionResult Index(string sortOrder, string searchString, string currentFilter, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DesigSortParm = sortOrder == "designation" ? "designation_desc" : "designation";
            ViewBag.LocSortParm = sortOrder == "location" ? "location_desc" : "location";
            ViewBag.SectionSortParm = sortOrder == "section" ? "section_desc" : "section";
            ViewBag.DepartmentSortParm = sortOrder == "wing" ? "wing_desc" : "wing";
            ViewBag.ShiftSortParm = sortOrder == "shift" ? "shift_desc" : "shift";
            ViewBag.TypeSortParm = sortOrder == "type" ? "type_desc" : "type";
           
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            User LoggedInUser = Session["LoggedUser"] as User;
            int NoOfEmps = Convert.ToInt32(GlobalVaribales.NoOfEmps);
            QueryBuilder qb = new QueryBuilder();
            string query = qb.MakeCustomizeQueryForEmp(LoggedInUser);
            DataTable dt = qb.GetValuesfromDB("select * from EmpView"+query);
            List<EmpView> emps = new List<EmpView>();
            emps = dt.ToList<EmpView>();

            emps = emps.Take(NoOfEmps).ToList();
            ViewBag.CurrentFilter = searchString;

            if (!String.IsNullOrEmpty(searchString))
            {
                if (searchString == "Active" || searchString == "active")
                {
                    emps = emps.Where(aa => aa.Status == true).ToList();
                }
                else if (searchString == "Inactive" || searchString == "inactive")
                {
                    emps = emps.Where(aa => aa.Status == false).ToList();
                }
                else
                {
                    try
                    {
                        emps = emps.Where(s => s.EmpName.ToUpper().Contains(searchString.ToUpper())
                         || s.EmpNo.Contains(searchString.ToUpper())).ToList();

                    }

                    catch (Exception)
                    {
                        
                        throw;
                    }       
                    
                }
            }

            switch (sortOrder)
            {
                case "name_desc":
                    emps = emps.OrderByDescending(s => s.EmpName).ToList();
                    break;
                case "designation_desc":
                    emps = emps.OrderByDescending(s => s.DesignationName).ToList();
                    break;
                case "designation":
                    emps = emps.OrderBy(s => s.DesignationName).ToList();
                    break;
                case "location_desc":
                    emps = emps.OrderByDescending(s => s.LocName).ToList();
                    break;
                case "location":
                    emps = emps.OrderBy(s => s.LocName).ToList();
                    break;
                case "section_desc":
                    emps = emps.OrderByDescending(s => s.SectionName).ToList();
                    break;
                case "section":
                    emps = emps.OrderBy(s => s.SectionName).ToList();
                    break;
                //case "wing_desc":
                //    emps = emps.OrderByDescending(s => s.sec.DeptName).ToList();
                //    break;
                //case "wing":
                //    emps = emps.OrderBy(s => s.DeptName).ToList();
                //    break;
                case "shift_desc":
                    emps = emps.OrderByDescending(s => s.ShiftName).ToList();
                    break;
                case "shift":
                    emps = emps.OrderBy(s => s.ShiftName).ToList();
                    break;
                case "type_desc":
                    emps = emps.OrderByDescending(s => s.TypeName).ToList();
                    break;
                case "type":
                    emps = emps.OrderBy(s => s.TypeName).ToList();
                    break;
                default:
                    emps = emps.OrderBy(s => s.EmpName).ToList();
                    break;
            }
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(emps.ToPagedList(pageNumber, pageSize));
            //var emps = db.Emps.Include(e => e.Company).Include(e => e.Crew).Include(e => e.Designation).Include(e => e.Grade).Include(e => e.JobTitle).Include(e => e.Location).Include(e => e.Section).Include(e => e.Shift).Include(e => e.EmpType).Include(e => e.EmpFace).Include(e => e.EmpFp).Include(e => e.LvQuota);
            //return View(emps.ToList());
        }

        // GET: /Emp/Details/5
         [CustomActionAttribute]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Emp emp = db.Emps.Find(id);
            if (emp == null)
            {
                return HttpNotFound();
            }
            return View(emp);
        }

        // GET: /Emp/Create
         [CustomActionAttribute]
        public ActionResult Create()
        {

            using (TAS2013Entities context = new TAS2013Entities())
            {
                //_wings = context.Divisions.ToList();
            }
             
            ViewBag.CrewID = new SelectList(db.Crews.OrderBy(s => s.CrewName), "CrewID", "CrewName");
            ViewBag.DesigID = new SelectList(db.Designations.OrderBy(s => s.DesignationName), "DesignationID", "DesignationName");
            //ViewBag.GradeID = new SelectList(db.Grades.OrderBy(s=>s.GradeName), "GradeID", "GradeName");
            //ViewBag.JobID = new SelectList(db.JobTitles.OrderBy(s=>s.JobTitle1), "JobID", "JobTitle1");
            ViewBag.LocID = new SelectList(db.Locations.OrderBy(s=>s.LocName), "LocID", "LocName");
            ViewBag.SecID = new SelectList(db.Sections.OrderBy(s=>s.SectionName), "SectionID", "SectionName");
            ViewBag.ShiftID = new SelectList(db.Shifts.OrderBy(s=>s.ShiftName), "ShiftID", "ShiftName");
            ViewBag.TypeID = new SelectList(db.EmpTypes.OrderBy(s=>s.TypeName), "TypeID", "TypeName");
            //ViewBag.CatID = new SelectList(dbCategories.OrderBy(s=>s.CatName), "CatID", "CatName");
            ViewBag.DeptID = new SelectList(db.Departments.OrderBy(s=>s.DeptName), "DeptID", "DeptName");
            return View();
        }

        // POST: /Emp/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomActionAttribute]
        public ActionResult Create([Bind(Include="EmpID,EmpNo,EmpName,DesigID,JobID,Gender,ShiftID,LocID,TypeID,GradeID,SecID,CardNo,FpID,PinCode,NicNo,FatherName,BloodGroup,BirthDate,MarStatus,JoinDate,ValidDate,IssueDate,ResignDate,HomeAdd,ProcessAtt,ProcessIn,Status,PhoneNo,Remarks,Email,CellNo,CrewID,FlagFP,FlagFace,FlagCard,EmpImageID,HasOT")] Emp emp)
        {
            string empNo = "";
            int cardno = Convert.ToInt32(emp.CardNo);
            emp.CardNo = cardno.ToString("0000000000");
            if(db.Emps.Where(aa=>aa.Status==true && aa.Deleted!=true).Count()>=Convert.ToInt32(GlobalVaribales.NoOfEmps))
                ModelState.AddModelError("EmpNo", "Active Number of employees are exceeded from license ");
            if (string.IsNullOrEmpty(emp.EmpNo))
                ModelState.AddModelError("EmpNo", "Emp No is required!");
            if (string.IsNullOrEmpty(emp.EmpName))
                ModelState.AddModelError("EmpName", "Name is required!");
            if (emp.EmpID != null)
            {
                if (db.Emps.Where(aa => aa.EmpID == emp.EmpID).Count() > 0)
                    ModelState.AddModelError("EmpID", "Emp ID should be unique!");
            }
            else
                ModelState.AddModelError("EmpID", "Emp ID is a required fields!");

            if (emp.EmpNo != null)
            {
                if (emp.EmpNo.Length > 15)
                    ModelState.AddModelError("EmpNo", "String length exceeds!");
                if (db.Emps.Where(aa => aa.EmpNo.ToUpper() == emp.EmpNo.ToUpper()).Count() > 0 )
                    ModelState.AddModelError("EmpNo", "Emp No should be unique!");
            }
            if (emp.CardNo != null)
            {
                if (db.Emps.Where(aa => aa.CardNo == emp.CardNo).Count() > 0)
                    if(emp.CardNo!="0000000000")
                        ModelState.AddModelError("CardNo", "Card No should be unique!");
                if (emp.CardNo.Length > 10)
                    ModelState.AddModelError("CardNo", "String length exceeds!");
            }
            if (emp.EmpName != null)
            {
                if (emp.EmpName.Length > 40)
                    ModelState.AddModelError("EmpName", "String length exceeds!");
            }
            if(emp.SecID== null)
                ModelState.AddModelError("SecID", "Please Specify section!");
            if (emp.TypeID == null)
                ModelState.AddModelError("TypeID", "Please Specify Type!");
            //if (emp.GradeID == null)
                //ModelState.AddModelError("GradeID", "Please Specify Grade!");
            if(db.Emps.Where(aa=>aa.Status==true).Count()> Convert.ToInt32(GlobalVaribales.NoOfEmps))
                ModelState.AddModelError("EmpNo", "Your Employees has exceeded from License, Please upgrade your license");
            if (ModelState.IsValid)
            {
                emp.ProcessAtt = true;
                //emp.ProcessI = true;
                emp.EmpNo = emp.EmpNo.ToUpper();
                if (emp.CardNo != "")
                {
                    int card = Convert.ToInt32(emp.CardNo);
                    emp.CardNo = card.ToString("0000000000");
                }
                else
                {
                    emp.CardNo = "0000000000";
                }
                empNo = emp.EmpNo;
                emp.Deleted = false;
                //emp.FpID = emp.EmpID;
                db.Emps.Add(emp);
                //db.SaveChanges();
                SaveChanges(db);
                int _userID = Convert.ToInt32(Session["LogedUserID"].ToString());
                HelperClass.MyHelper.SaveAuditLog(_userID, (byte)MyEnums.FormName.Employee, (byte)MyEnums.Operation.Add, DateTime.Now);
                HttpPostedFileBase file = Request.Files["ImageData"];
                if (file != null)
                {
                    ImageConversion _Image = new ImageConversion();
                    int imageID = _Image.UploadImageInDataBase(file, emp.EmpNo);
                    if (imageID!=0)
                    {
                        using (var ctx = new TAS2013Entities())
                        {
                            var _emp = ctx.Emps.Where(aa => aa.EmpNo == empNo).ToList();
                            if (_emp.Count > 0)
                            {
                                _emp.FirstOrDefault().EmpImageID = imageID;
                                ctx.SaveChanges();
                                ctx.Dispose();
                            }
                        }
                    }
                    else
                    {

                    }
                }
                return RedirectToAction("Index");
            }
            //var _wings = new List<Division>();
            using (TAS2013Entities context = new TAS2013Entities())
            {

            //ViewBag.Wing = new SelectList(_wings.OrderBy(s=>s.DivisionName), "WingID", "WingName");
            User LoggedInUser = Session["LoggedUser"] as User;
            //ViewBag.CrewID = new SelectList(db.Crews.OrderBy(s=>s.CrewName), "CrewID", "CrewName");
            ViewBag.DesigID = new SelectList(db.Designations.OrderBy(s=>s.DesignationName), "DesignationID", "DesignationName");
            //ViewBag.GradeID = new SelectList(db.Grades.OrderBy(s=>s.GradeName), "GradeID", "GradeName");
            //ViewBag.JobID = new SelectList(db.JobTitles.OrderBy(s=>s.JobTitle1), "JobID", "JobTitle1");
            ViewBag.LocID = new SelectList(db.Locations.OrderBy(s=>s.LocName), "LocID", "LocName");
            ViewBag.SecID = new SelectList(db.Sections.OrderBy(s=>s.SectionName), "SectionID", "SectionName");
            ViewBag.ShiftID = new SelectList(db.Shifts.OrderBy(s=>s.ShiftName), "ShiftID", "ShiftName");
            ViewBag.TypeID = new SelectList(db.EmpTypes.OrderBy(s=>s.TypeName), "TypeID", "TypeName");
            //ViewBag.CatID = new SelectList(db.Categories.OrderBy(s=>s.CatName), "CatID", "CatName");
            ViewBag.DeptID = new SelectList(db.Departments.OrderBy(s=>s.DeptName), "DeptID", "DeptName");
            }
            return View(emp);
        }
        private void SaveChanges(DbContext context)
        {
            try
            {
                context.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var failure in ex.EntityValidationErrors)
                {
                    sb.AppendFormat("{0} failed validation\n", failure.Entry.Entity.GetType());
                    foreach (var error in failure.ValidationErrors)
                    {
                        sb.AppendFormat("- {0} : {1}", error.PropertyName, error.ErrorMessage);
                        sb.AppendLine();
                    }
                }

                throw new DbEntityValidationException(
                    "Entity Validation Failed - errors follow:\n" +
                    sb.ToString(), ex
                ); // Add the original exception as the innerException
            }
        }
        // GET: /Emp/Edit/5
         [CustomActionAttribute]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Emp emp = db.Emps.Find(id);
            if (emp == null)
            {
                return HttpNotFound();
            }
            try
            {
                EmpType et = db.EmpTypes.Where(aa => aa.TypeID == emp.TypeID).FirstOrDefault();
                //ViewBag.CatID = new SelectList(db.Categories.OrderBy(s=>s.CatName), "CatID", "CatName", et.CatID);
                ViewBag.CrewID = new SelectList(db.Crews.OrderBy(s=>s.CrewName), "CrewID", "CrewName", emp.CrewID);
                ViewBag.DesigID = new SelectList(db.Designations.OrderBy(s=>s.DesignationName), "DesignationID", "DesignationName", emp.DesigID);
                //ViewBag.GradeID = new SelectList(db.Grades.OrderBy(s=>s.GradeName), "GradeID", "GradeName", emp.GradeID);
                //ViewBag.JobID = new SelectList(db.JobTitles.OrderBy(s=>s.JobTitle1), "JobID", "JobTitle1", emp.JobID);
                ViewBag.LocID = new SelectList(db.Locations.OrderBy(s=>s.LocName), "LocID", "LocName", emp.LocID);
                ViewBag.SecID = new SelectList(db.Sections.OrderBy(s=>s.SectionName), "SectionID", "SectionName", emp.SecID);
                ViewBag.ShiftID = new SelectList(db.Shifts.OrderBy(s=>s.ShiftName), "ShiftID", "ShiftName", emp.ShiftID);
                ViewBag.TypeID = new SelectList(db.EmpTypes.OrderBy(s=>s.TypeName), "TypeID", "TypeName", emp.TypeID);
                ViewBag.EmpID = new SelectList(db.EmpFaces.OrderBy(s=>s.Face1), "EmpID", "Face1");
                ViewBag.EmpID = new SelectList(db.EmpFps.OrderBy(s=>s.Fp1), "EmpID", "Fp1");
                ViewBag.DeptID = new SelectList(db.Departments.OrderBy(s=>s.DeptName), "DeptID", "DeptName", emp.Section.DeptID);
            }
             catch(Exception ex)
            {

             }
            return View(emp);
        }

        // POST: /Emp/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomActionAttribute]
         public ActionResult Edit([Bind(Include = "EmpID,EmpNo,EmpName,DesigID,JobID,Gender,ShiftID,LocID,TypeID,GradeID,SecID,CardNo,FpID,PinCode,NicNo,FatherName,BloodGroup,BirthDate,MarStatus,JoinDate,ValidDate,IssueDate,ResignDate,HomeAdd,ProcessAtt,ProcessIn,Status,PhoneNo,Remarks,Email,CellNo,CrewID,FlagFP,FlagFace,FlagCard,EmpImageID,HasOT")] Emp emp)
        {
            try
            {
                ViewBag.Message = "";
                HttpPostedFileBase file = Request.Files["ImageData"];
                if (file != null)
                {
                    ImageConversion _Image = new ImageConversion();
                    int imageid = _Image.UploadImageInDataBase(file, emp);
                    if (imageid!=0)
                    {
                        emp.EmpImageID= imageid;
                    }
                    else
                    {

                    }
                }

                
                if (string.IsNullOrEmpty(emp.EmpNo))
                    ModelState.AddModelError("EmpNo", "Emp No field is required!");
                if (string.IsNullOrEmpty(emp.EmpName))
                    ModelState.AddModelError("EmpName", "Namefield is required!");
                if (emp.EmpNo != null)
                {
                    if (emp.EmpNo.Length > 15)
                        ModelState.AddModelError("EmpNo", "String length exceeds!");
                }
                if (emp.EmpName != null)
                {
                    if (emp.EmpName.Length > 40)
                        ModelState.AddModelError("EmpName", "String length exceeds!");
                }
                if (emp.SecID == null)
                    ModelState.AddModelError("SecID", "Please Specify section!");
                if (emp.TypeID == null)
                    ModelState.AddModelError("TypeID", "Please Specify Type!");
                //if (emp.GradeID == null)
                //    ModelState.AddModelError("GradeID", "Please Specify Grade!");
                if (ModelState.IsValid)
                {
                    if (emp.CardNo != "")
                    {
                        int card = Convert.ToInt32(emp.CardNo);
                        emp.CardNo = card.ToString("0000000000");
                    }
                    else
                    {
                        emp.CardNo = "0000000000";
                    }
                    emp.EmpNo = emp.EmpNo.ToUpper();
                    emp.Deleted = false;
                    db.Entry(emp).State = EntityState.Modified;
                   
                    db.SaveChanges();
                    int _userID = Convert.ToInt32(Session["LogedUserID"].ToString());
                    HelperClass.MyHelper.SaveAuditLog(_userID, (byte)MyEnums.FormName.Employee, (byte)MyEnums.Operation.Edit, DateTime.Now);
                    return RedirectToAction("Index");
                }
                User LoggedInUser = Session["LoggedUser"] as User;
              ViewBag.CrewID = new SelectList(db.Crews.OrderBy(s=>s.CrewName), "CrewID", "CrewName");
                ViewBag.DesigID = new SelectList(db.Designations.OrderBy(s=>s.DesignationName), "DesignationID", "DesignationName");
                //ViewBag.GradeID = new SelectList(db.Grades.OrderBy(s=>s.GradeName), "GradeID", "GradeName");
                //ViewBag.JobID = new SelectList(db.JobTitles.OrderBy(s=>s.JobTitle1), "JobID", "JobTitle1");
                ViewBag.LocID = new SelectList(db.Locations.OrderBy(s=>s.LocName), "LocID", "LocName");
                ViewBag.SecID = new SelectList(db.Sections.OrderBy(s=>s.SectionName), "SectionID", "SectionName");
                ViewBag.ShiftID = new SelectList(db.Shifts.OrderBy(s=>s.ShiftName), "ShiftID", "ShiftName");
                ViewBag.TypeID = new SelectList(db.EmpTypes.OrderBy(s=>s.TypeName), "TypeID", "TypeName");
                ViewBag.EmpID = new SelectList(db.EmpFaces.OrderBy(s=>s.Face1), "EmpID", "Face1");
                ViewBag.EmpID = new SelectList(db.EmpFps.OrderBy(s=>s.Fp1), "EmpID", "Fp1");
                //ViewBag.CatID = new SelectList(db.Categories.OrderBy(s=>s.CatName), "CatID", "CatName");
                ViewBag.DeptID = new SelectList(db.Departments.OrderBy(s=>s.DeptName), "DeptID", "DeptName");
                return View(emp);
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.InnerException.ToString();
                User LoggedInUser = Session["LoggedUser"] as User;
                ViewBag.CrewID = new SelectList(db.Crews.OrderBy(s=>s.CrewName), "CrewID", "CrewName");
                ViewBag.DesigID = new SelectList(db.Designations.OrderBy(s=>s.DesignationName), "DesignationID", "DesignationName");
                //ViewBag.GradeID = new SelectList(db.Grades.OrderBy(s=>s.GradeName), "GradeID", "GradeName");
                //ViewBag.JobID = new SelectList(db.JobTitles.OrderBy(s=>s.JobTitle1), "JobID", "JobTitle1");
                ViewBag.LocID = new SelectList(db.Locations.OrderBy(s=>s.LocName), "LocID", "LocName");
                ViewBag.SecID = new SelectList(db.Sections.OrderBy(s=>s.SectionName), "SectionID", "SectionName");
                ViewBag.ShiftID = new SelectList(db.Shifts.OrderBy(s=>s.ShiftName), "ShiftID", "ShiftName");
                ViewBag.TypeID = new SelectList(db.EmpTypes.OrderBy(s=>s.TypeName), "TypeID", "TypeName");
                ViewBag.EmpID = new SelectList(db.EmpFaces.OrderBy(s=>s.Face1), "EmpID", "Face1");
                ViewBag.EmpID = new SelectList(db.EmpFps.OrderBy(s=>s.Fp1), "EmpID", "Fp1");
                //ViewBag.CatID = new SelectList(db.Categories.OrderBy(s=>s.CatName), "CatID", "CatName");
                ViewBag.DeptID = new SelectList(db.Departments.OrderBy(s=>s.DeptName), "DeptID", "DeptName");
                return View(emp);
            }
        }

        // GET: /Emp/Delete/5
         [CustomActionAttribute]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Emp emp = db.Emps.Find(id);
            if (emp == null)
            {
                return HttpNotFound();
            }
            return View(emp);
        }

        // POST: /Emp/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomActionAttribute]
        public ActionResult DeleteConfirmed(int id)
        {
            Emp emp = db.Emps.Find(id);
            emp.Deleted = true;
            emp.Status = false;
            ViewBag.JS = "toastr.success('" + emp.EmpName + " removed');";
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

        #region --Cascade DropDown--
        public ActionResult DepartmentList()
        {
           
            var dept = db.Departments.ToList();
            if (HttpContext.Request.IsAjaxRequest())
                return Json(new SelectList(
                                dept.ToArray(),
                                "DeptID",
                                "DeptName")
                           , JsonRequestBehavior.AllowGet);

            return RedirectToAction("Index");
        }
        public ActionResult SectionList(string ID)
        {
            short id = Convert.ToInt16(ID);
            var secs = db.Sections.Where(aa => aa.DeptID == id).OrderBy(s => s.SectionName);
            if (HttpContext.Request.IsAjaxRequest())
                return Json(new SelectList(
                                secs.ToArray(),
                                "SectionID",
                                "SectionName")
                           , JsonRequestBehavior.AllowGet);

            return RedirectToAction("Index");
        }
        //public ActionResult CrewList()
        //{
          
        //    var secs = db.Crews.OrderBy(s => s.CrewName);
        //    if (HttpContext.Request.IsAjaxRequest())
        //        return Json(new SelectList(
        //                        secs.ToArray(),
        //                        "CrewID",
        //                        "CrewName")
        //                   , JsonRequestBehavior.AllowGet);

        //    return RedirectToAction("Index");
        //}
        //public ActionResult EmpTypeList(string ID)
        //{
        //    short CatID = Convert.ToInt16(ID);
        //    var types = db.EmpTypes.Where(aa => aa.CatID == CatID).OrderBy(s=>s.TypeName);
        //    if (HttpContext.Request.IsAjaxRequest())
        //        return Json(new SelectList(
        //                        types.ToArray(),
        //                        "TypeID",
        //                        "TypeName")
        //                   , JsonRequestBehavior.AllowGet);

        //    return RedirectToAction("Index");
        //}
        #endregion

        public ActionResult RetrieveImage(int id)
        {
            byte[] cover = GetImageFromDataBase(id);
            if (cover != null)
            {
                return File(cover, "image/jpg");
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public byte[] GetImageFromDataBase(int Id)
        {
            try
            {
                byte[] cover;
                List<EmpPhoto> eps = new List<EmpPhoto>();
                eps = db.EmpPhotoes.Where(aa => aa.EmpID == Id).ToList();
                if (eps.Count > 0)
                    cover = eps.First().EmpPic;
                else
                    cover = null;
                return cover;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // Avatar Controller
        private int _avatarWidth = 250; // ToDo - Change the size of the stored avatar image
        private int _avatarHeight = 250; // ToDo - Change the size of the stored avatar image

        #region --Upload Picture--
        [HttpGet]
        public ActionResult Upload()
        {
            return View();
        }

        [HttpGet]
        public ActionResult _Upload()
        {
            return PartialView();
        }

        [ValidateAntiForgeryToken]
        public ActionResult _Upload(IEnumerable<HttpPostedFileBase> files)
        {
            string errorMessage = "";

            if (files != null && files.Count() > 0)
            {
                // Get one only
                var file = files.FirstOrDefault();
                // Check if the file is an image
                if (file != null && IsImage(file))
                {
                    // Verify that the user selected a file
                    if (file != null && file.ContentLength > 0)
                    {
                        var webPath = SaveTemporaryFile(file);
                        return Json(new { success = true, fileName = webPath.Replace("/", "\\") }); // success
                    }
                    errorMessage = "File cannot be zero length."; //failure
                }
                errorMessage = "File is of wrong format."; //failure
            }
            errorMessage = "No file uploaded."; //failure

            return Json(new { success = false, errorMessage = errorMessage });
        }

        [HttpPost]
        public ActionResult Save(string t, string l, string h, string w, string fileName)
        {
            try
            {
                // Get file from temporary folder
                var fn = Path.Combine(Server.MapPath("~/Temp"), Path.GetFileName(fileName));

                // Calculate dimesnions
                int top = Convert.ToInt32(t.Replace("-", "").Replace("px", ""));
                int left = Convert.ToInt32(l.Replace("-", "").Replace("px", ""));
                int height = Convert.ToInt32(h.Replace("-", "").Replace("px", ""));
                int width = Convert.ToInt32(w.Replace("-", "").Replace("px", ""));

                // Get image and resize it, ...
                var img = new WebImage(fn);
                img.Resize(width, height);
                // ... crop the part the user selected, ...
                img.Crop(top, left, img.Height - top - _avatarHeight, img.Width - left - _avatarWidth);
                // ... delete the temporary file,...
                System.IO.File.Delete(fn);
                // ... and save the new one.
                string newFileName = "/Avatars/" + Path.GetFileName(fn);
                Session["imagePath"] = newFileName;
                string newFileLocation = HttpContext.Server.MapPath(newFileName);
                if (Directory.Exists(Path.GetDirectoryName(newFileLocation)) == false)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newFileLocation));
                }

                img.Save(newFileLocation);
                Session["imageFullPath"] = newFileLocation;

                return Json(new { success = true, avatarFileLocation = newFileName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = "Unable to upload file.\nERRORINFO: " + ex.Message });
            }
        }
        public ActionResult GradeList()
        {
           
            //var states = db.Grades.OrderBy(s => s.GradeName);
            if (HttpContext.Request.IsAjaxRequest())
                return Json(new SelectList(
                                //states.ToArray(),
                                "GradeID",
                                "GradeName")
                           , JsonRequestBehavior.AllowGet);

            return RedirectToAction("Index");
        }
        public ActionResult DesignationList()
        {
           
            var secs = db.Designations.OrderBy(s => s.DesignationName);
            if (HttpContext.Request.IsAjaxRequest())
                return Json(new SelectList(
                                secs.ToArray(),
                                "DesignationID",
                                "DesignationName")
                           , JsonRequestBehavior.AllowGet);

            return RedirectToAction("Index");
        }
        private bool IsImage(HttpPostedFileBase file)
        {
            if (file.ContentType.Contains("image"))
            {
                return true;
            }

            var extensions = new string[] { ".jpg", ".png", ".gif", ".jpeg" }; // add more if you like...

            // linq from Henrik Stenbæk
            return extensions.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        private string SaveTemporaryFile(HttpPostedFileBase file)
        {
            // Define destination
            var folderName = "/Temp";
            var serverPath = HttpContext.Server.MapPath(folderName);
            if (Directory.Exists(serverPath) == false)
            {
                Directory.CreateDirectory(serverPath);
            }

            // Generate unique file name
            var fileName = Path.GetFileName(file.FileName);
            fileName = SaveTemporaryAvatarFileImage(file, serverPath, fileName);

            // Clean up old files after every save
            CleanUpTempFolder(1);

            return Path.Combine(folderName, fileName);
        }

        private string SaveTemporaryAvatarFileImage(HttpPostedFileBase file, string serverPath, string fileName)
        {
            var img = new WebImage(file.InputStream);
            double ratio = (double)img.Height / (double)img.Width;

            string fullFileName = Path.Combine(serverPath, fileName);

            img.Resize(400, (int)(400 * ratio)); // ToDo - Change the value of the width of the image oin the screen

            if (System.IO.File.Exists(fullFileName))
                System.IO.File.Delete(fullFileName);

            img.Save(fullFileName);

            return Path.GetFileName(img.FileName);
        }

        private void CleanUpTempFolder(int hoursOld)
        {
            try
            {
                DateTime fileCreationTime;
                DateTime currentUtcNow = DateTime.UtcNow;

                var serverPath = HttpContext.Server.MapPath("/Temp");
                if (Directory.Exists(serverPath))
                {
                    string[] fileEntries = Directory.GetFiles(serverPath);
                    foreach (var fileEntry in fileEntries)
                    {
                        fileCreationTime = System.IO.File.GetCreationTimeUtc(fileEntry);
                        var res = currentUtcNow - fileCreationTime;
                        if (res.TotalHours > hoursOld)
                        {
                            System.IO.File.Delete(fileEntry);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        public ActionResult GetEmpInfo(string empNo)
        {
            List<Emp> emp = db.Emps.Where(aa => aa.EmpNo == empNo).ToList();
            if (emp.Count > 0)
            {
                string year = DateTime.Today.Year.ToString();
                int empID = emp.First().EmpID;
                List<LvConsumed> lvConsumed = db.LvConsumeds.Where(aa => aa.EmpID == empID && aa.LvYear == year).ToList();
                string DOB = "";
                if (emp.FirstOrDefault().JoinDate != null)
                    DOB = emp.FirstOrDefault().JoinDate.Value.ToString("dd-MMM-yyyy");
                if (lvConsumed.Count > 0)
                {
                    string emplvTypeCL = emp.First().EmpID.ToString() + "A"+DateTime.Today.Year.ToString("0000");
                    string emplvTypeAL = emp.First().EmpID.ToString() + "B" + DateTime.Today.Year.ToString("0000");
                    string emplvTypeSL = emp.First().EmpID.ToString() + "C" + DateTime.Today.Year.ToString("0000");
                    string emplvTypeCPL = emp.First().EmpID.ToString() + "E" + DateTime.Today.Year.ToString("0000");
                    string CL = lvConsumed.Where(aa => aa.EmpLvTypeYear == emplvTypeCL).First().YearRemaining.ToString();
                    string AL = lvConsumed.Where(aa => aa.EmpLvTypeYear == emplvTypeAL).First().YearRemaining.ToString();
                    string SL = lvConsumed.Where(aa => aa.EmpLvTypeYear == emplvTypeSL).First().YearRemaining.ToString();
                    string CPL = "0";
                    if (lvConsumed.Where(aa => aa.EmpLvTypeYear == emplvTypeCPL).Count() > 0)
                        if (lvConsumed.Where(aa => aa.EmpLvTypeYear == emplvTypeCPL).First().YearRemaining != null)
                            CPL = lvConsumed.Where(aa => aa.EmpLvTypeYear == emplvTypeCPL).First().YearRemaining.ToString();
                    if (HttpContext.Request.IsAjaxRequest())
                        return Json(emp.FirstOrDefault().EmpName + "@" + emp.FirstOrDefault().Designation.DesignationName + "@" +
                            emp.FirstOrDefault().Section.SectionName + "@" + CL + "@" + AL + "@" + SL + "@" + CPL + "@" + emp.FirstOrDefault().FatherName
                            + "@" + DOB
                           , JsonRequestBehavior.AllowGet);
                }
                else
                {
                    if (HttpContext.Request.IsAjaxRequest())
                        return Json(emp.FirstOrDefault().EmpName + "@" + emp.FirstOrDefault().Designation.DesignationName + "@" +
                            emp.FirstOrDefault().Section.SectionName + "@" + "No Quota" + "@" + "No Quota" + "@" + "No Quota" + "@" + "No Quota"
                            + "@" + emp.FirstOrDefault().FatherName
                            + "@" + DOB
                           , JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                if (HttpContext.Request.IsAjaxRequest())
                    return Json("Not found" + "@" + "Not found" + "@" +
                        "Not found" + "@" + "No Quota" + "@" + "No Quota" + "@" + "No Quota" + "@" + "No Quota"
                        + "@" + "Not found"
                        + "@" + "Not found"
                       , JsonRequestBehavior.AllowGet);
            }

            return RedirectToAction("Index");
        }
    }

}
