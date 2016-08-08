using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;

namespace WMS.Controllers
{
    public class PollDataImportController : Controller
    {
        //
        // GET: /PollDataImport/
        public ActionResult Index()
        {
            return View();
        }
        TAS2013Entities db = new TAS2013Entities();
        [HttpPost]
        public ActionResult SavePollData(FormCollection form)
        {
            string _empid = Request.Form["EmpID"].ToString();
            int empid = Convert.ToInt32(_empid);
            string Message ="";
            List<PollDataError> pd = new List<PollDataError>();
            pd = db.PollDataErrors.Where(aa => aa.DeviceRegID == empid).ToList();
            if (pd.Count > 0)
            {
                if (CopyIntoPoll(pd))
                    Message = "Data has been Imported Sucessfully";
                else
                    Message = "There is some problem while import of data";
                
            }
            else
            {
                Message = "No entry Found";
            }
            ViewBag.Message = Message;
            return RedirectToAction("Index");
        }
        private bool CopyIntoPoll(List<PollDataError> pde)
        {
            bool check = false;
            try
            {
                foreach (var item in pde)
                {
                    PollData pd = new PollData();
                    pd.EmpID = (int)item.DeviceRegID;
                    pd.EntDate = item.EntryDate.Value.Date;
                    pd.EntTime = item.EntryTime.Value;
                    pd.EmpDate = pd.EmpID.ToString() + pd.EntDate.Date.ToString("yyMMdd");
                    pd.CardNo = "0";
                    pd.RdrID = 107;
                    pd.FpID = (int)item.DeviceRegID;
                    pd.RdrDuty = 8;
                    pd.Split = false;
                    pd.Process = false;
                    pd.AddedDate = DateTime.Today;
                    db.PollDatas.Add(pd);
                    db.SaveChanges();
                }
                check = true;
            }
            catch (Exception ex)
            {
                check = false;
            }
            return check;
        }
	}
}