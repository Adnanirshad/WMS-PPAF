using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;
using WMS.Controllers.Filters;
using WMS.HelperClass;
using WMS.Models;
using System.DirectoryServices;
using System.Linq.Dynamic;
using System.DirectoryServices.AccountManagement;
using System.Data;
using System.Drawing;
using System.Management;
using System.Net.NetworkInformation;
using WMS.CustomClass;
namespace WMS.Controllers
{
    public class HomeController : Controller
    {
        public byte[] imageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            return ms.ToArray();
        }
        //
        // GET: /Home/
        public ActionResult Index()
        {
            try
            {
                SetGlobalVaribale();
                if (CheckForValidLicense("Client"))
                {
                    if (Session["LogedUserID"] == null)
                    {
                        Session["LogedUserID"] = "";
                        Session["Role"] = "";
                        Session["MHR"] = "0";
                        Session["MDevice"] = "0";
                        Session["MLeave"] = "0";
                        Session["MEditAtt"] = "0";
                        Session["MUser"] = "0";
                        Session["LogedUserFullname"] = "";
                        Session["UserCompany"] = "";
                        Session["MRDailyAtt"] = "0";
                        Session["MRLeave"] = "0";
                        Session["MRMonthly"] = "0";
                        Session["MRAudit"] = "0";
                        Session["MRManualEditAtt"] = "0";
                        Session["MREmployee"] = "0";
                        Session["MRoster"] = "0";
                        Session["MRDetail"] = "0";
                        Session["MRSummary"] = "0";
                        Session["MProcess"] = "0";
                        return View();
                    }
                    else if (Session["LogedUserID"].ToString() == "")
                    {
                        return View();
                    }
                    else
                    {
                        return View("AfterLogin");
                    }
                }
                else
                {
                    return View("LoadLicense");
                }
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        private void AdjustLeaves()
        {
            using (var mydb = new TAS2013Entities())
            {
                List<LvApplication> lvApps = new List<LvApplication>();
                lvApps = mydb.LvApplications.ToList();
                foreach (var lv in lvApps)
                {
                    string empLvYear = lv.EmpID.ToString()+lv.LeaveTypeID.ToString()+"2016";
                    List<LvConsumed> lvcon = new List<LvConsumed>();
                    lvcon = mydb.LvConsumeds.Where(aa => aa.EmpID == lv.EmpID && aa.EmpLvTypeYear == empLvYear).ToList();
                    if (lvcon.Count > 0)
                    {
                        lvcon.FirstOrDefault().YearRemaining = lvcon.FirstOrDefault().YearRemaining - lv.NoOfDays;
                        lvcon.FirstOrDefault().GrandTotalRemaining = lvcon.FirstOrDefault().GrandTotalRemaining - lv.NoOfDays;
                        mydb.SaveChanges();
                    }
                }
            }
        }

        private void SetGlobalVaribale()
        {
            using (var db = new TAS2013Entities())
            {
                GlobalVaribales.ServerPath = db.Options.FirstOrDefault().ServerFilePath;
                if (db.LicenseInfoes.Count() > 0)
                {
                    LicenseInfo li = new LicenseInfo();
                    li = db.LicenseInfoes.FirstOrDefault();
                    GlobalVaribales.NoOfDevices = StringCipher.Decrypt(li.NoOfDevices, "1234");
                    GlobalVaribales.NoOfEmps = StringCipher.Decrypt(li.NoOfEmps, "1234");
                    GlobalVaribales.NoOfUsers = StringCipher.Decrypt(li.NoOfUsers, "1234");
                    GlobalVaribales.DeviceType = StringCipher.Decrypt(li.DeviceType, "1234");
                    GlobalVaribales.LicenseType = StringCipher.Decrypt(li.LicenseType, "1234");
                }
                db.Dispose();
            }
        }

        #region --License--
        [HttpPost]
        public ActionResult LoadLicense(HttpPostedFileBase uploadFile)
        {
            if (uploadFile.ContentLength > 0)
            {
                string filePath = Path.GetFileName(uploadFile.FileName);
                uploadFile.SaveAs(GlobalVaribales.ServerPath + filePath);
                ReadFile(GlobalVaribales.ServerPath + filePath);
            }
            return RedirectToAction("Index");
        }

        private void ReadFile(string LicensePath)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(LicensePath);
            string line;
            int CurrentlineNo = 0;
            string InvoiceNo="";
            string CustomerName="";
            string LicenseType="";
            string DeviceType="";
            string ClientMac="";
            string NoOfEmps="";
            string NoOfUsers="";
            string NoOfDevices="";
            List<string> DeviceMacs = new List<string>();
            var fileLines = new List<string>();
            while ((line = file.ReadLine()) != null)
            {
                if (CurrentlineNo == 0)
                    InvoiceNo = line;
                if (CurrentlineNo == 1)
                    CustomerName = line;
                if (CurrentlineNo == 2)
                    LicenseType = line;
                if (CurrentlineNo == 3)
                    DeviceType = line;
                if (CurrentlineNo == 4)
                    ClientMac = line;
                if (CurrentlineNo == 5)
                    NoOfEmps = line;
                if (CurrentlineNo == 6)
                    NoOfUsers = line;
                if (CurrentlineNo == 7)
                    NoOfDevices = line;
                if (CurrentlineNo > 7)
                    DeviceMacs.Add(line);
                CurrentlineNo++;
            }
            TAS2013Entities db = new TAS2013Entities();
            string DBMACAddress = StringCipher.Decrypt(ClientMac,"1234");
            string SystemMACAdress = GetClientMacAddress();
            if (DBMACAddress == SystemMACAdress)
            {
                LicenseInfo li = new LicenseInfo();
                if (db.LicenseInfoes.Count() > 0)
                {
                    li = db.LicenseInfoes.FirstOrDefault();
                }
                li.ID = 1;
                li.InvoiceNo = InvoiceNo;
                li.LicenseType = LicenseType;
                li.NoOfDevices = NoOfDevices;
                li.NoOfEmps = NoOfEmps;
                li.NoOfUsers = NoOfUsers;
                li.CustomerName = CustomerName;
                li.ClientMAC = ClientMac;
                li.DeviceType = DeviceType;
                li.ValidLicense = StringCipher.Encrypt("1","1234");
                if (db.LicenseInfoes.Count() == 0)
                {
                    db.LicenseInfoes.Add(li);
                }
                db.SaveChanges();
                foreach (var item in db.LicenseDeviceInfoes.ToList())
                {
                    db.LicenseDeviceInfoes.Remove(item);
                }
                db.SaveChanges();
                byte count =1;
                foreach (var item in DeviceMacs)
                {
                    LicenseDeviceInfo ldi = new LicenseDeviceInfo();
                    ldi.DeviceID = count;
                    ldi.DeviceMAC = item;
                    db.LicenseDeviceInfoes.Add(ldi);
                    count++;
                }
                db.SaveChanges();
            }
        }

        private bool CheckForValidLicense(string DevUser)
        {
            bool valid = false;
            if (DevUser != "Server")
            {
                try
                {
                    using (var db = new TAS2013Entities())
                    {
                        if (db.LicenseInfoes.ToList().Count > 0)
                        {
                            LicenseInfo li = new LicenseInfo();
                            li = db.LicenseInfoes.FirstOrDefault();
                            string val = StringCipher.Decrypt(li.ValidLicense, "1234");
                            if (val == "1")
                            {
                                string ClientMAC = GetClientMacAddress();
                                string DatabaseMac = StringCipher.Decrypt(li.ClientMAC, "1234");
                                if (ClientMAC == DatabaseMac)
                                    valid = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    valid = false;
                }
            }
            else
                return true;
            return valid;
        }
        //private bool CheckForValidLicense()
        //{
        //    bool valid;
            
        //    return true;
        //}
        public static string GetClientMacAddress()
        {
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            string mac="";
            foreach (NetworkInterface adapter in nics)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    
                             mac=  adapter.GetPhysicalAddress().ToString();
                    
                }
            }
            return mac;
        }
        #endregion
        private void SaveImage()
        {
            //image to byteArray
            Image img = Image.FromFile("E:\\air.png");
            byte[] bArr = imgToByteArray(img);
            //byte[] bArr = imgToByteConverter(img);
            //Again convert byteArray to image and displayed in a picturebox
            TAS2013Entities ctx = new TAS2013Entities();
            Option oo = new Option();
            oo = ctx.Options.First(aa=>aa.ID==1);
            oo.CompanyLogo = bArr;
            ctx.SaveChanges();
        }
        //convert image to bytearray
        public byte[] imgToByteArray(Image img)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                img.Save(mStream, img.RawFormat);
                return mStream.ToArray();
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User u)
        {
            try
            {
                      if (ModelState.IsValid) // this is check validity
                      {
                          using (TAS2013Entities dc = new TAS2013Entities())
                          {
                              List<User> users = new List<Models.User>();
                              int NoOfUsres = Convert.ToInt32(GlobalVaribales.NoOfUsers);
                              users = dc.Users.Where(aa => aa.Deleted == false).ToList();
                              var usr = users.Take(NoOfUsres);
                              var v = usr.Where(a => a.UserName.ToUpper().Equals(u.UserName.ToUpper()) && a.Password.ToUpper() == u.Password.ToUpper() && a.Status == true).FirstOrDefault();
                              //login for emplioyee
                              if (v != null)
                              {

                                  Session["MDevice"] = "0";
                                  Session["MHR"] = "0";
                                  Session["MDevice"] = "0";
                                  Session["MLeave"] = "0";
                                  Session["MEditAtt"] = "0";
                                  Session["MUser"] = "0";
                                  Session["LogedUserFullname"] = "";
                                  Session["UserCompany"] = "";
                                  Session["MRDailyAtt"] = "0";
                                  Session["MRLeave"] = "0";
                                  Session["MRMonthly"] = "0";
                                  Session["MRAudit"] = "0";
                                  Session["MRManualEditAtt"] = "0";
                                  Session["MREmployee"] = "0";
                                  Session["MRDetail"] = "0";
                                  Session["MRSummary"] = "0";
                                  Session["LogedUserID"] = v.UserID.ToString();
                                  Session["LogedUserFullname"] = v.UserName;
                                  Session["LoggedUser"] = v;
                                  if (v.MHR == true)
                                      Session["MHR"] = "1";
                                  if (v.MDevice == true)
                                      Session["MDevice"] = "1";
                                  if (v.MLeave == true)
                                      Session["MLeave"] = "1";
                                  if (v.MEditAtt == true)
                                      Session["MEditAtt"] = "1";
                                  if (v.MUser == true)
                                      Session["MUser"] = "1";
                                  if (v.MRDailyAtt == true)
                                      Session["MRDailyAtt"] = "1";
                                  if (v.MRLeave == true)
                                      Session["MRLeave"] = "1";
                                  if (v.MRMonthly == true)
                                      Session["MRMonthly"] = "1";
                                  if (v.MRAudit == true)
                                      Session["MRAudit"] = "1";
                                  if (v.MRManualEditAtt == true)
                                      Session["MRManualEditAtt"] = "1";
                                  if (v.MProcess == true)
                                      Session["MProcess"] = "1";
                                  if (v.MREmployee == true)
                                      Session["MREmployee"] = "1";
                                  if (v.MRDetail == true)
                                      Session["MRDetail"] = "1";
                                  
                                  if (v.MRoster == true)
                                      Session["MRoster"] = "1";
                                  
                                  HelperClass.MyHelper.SaveAuditLog(v.UserID, (byte)MyEnums.FormName.LogIn, (byte)MyEnums.Operation.LogIn, DateTime.Now);
                                  return RedirectToAction("AfterLogin");
                              }
                              else
                              {
                                  int LoginCount = 0;
                                  bool successOnConversion = int.TryParse(Session["LoginCount"] as string, out LoginCount);
                                  if (successOnConversion == true)
                                  {
                                      LoginCount++;
                                      Session["LoginCount"] = LoginCount + "";
                                  }
                                  else
                                  {
                                      Session["LoginCount"] = "1";
                                  }


                              }
                          }
                      }
                  return RedirectToAction("index");

            }
            catch (Exception ex)
            {
                ViewBag.Message = "There seems to be a problem with the network. Please contact your network administrator";
                return RedirectToAction("Index");
            }
        }
        public ActionResult AfterLogin()
        {
            try
            {
                if (Session["LogedUserID"] != null)
                {
                    return View();
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                return View("Index");
            }
        }
        public ActionResult Logout()
        {
            try
            {
                int _userID = Convert.ToInt32(Session["LogedUserID"].ToString());
                HelperClass.MyHelper.SaveAuditLog(_userID, (byte)MyEnums.FormName.LogIn, (byte)MyEnums.Operation.LogOut, DateTime.Now);
                Session["LogedUserID"] = "";
                Session["LogedUserFullname"] = null;
                Session["LogedUserRegion"] = null;
                Session["LoggedUser"] = null;
                Session["MHR"] = null;
                Session["MDevice"] = null;
                Session["MLeave"] = null;
                Session["MEditAtt"] = null;
                Session["MUser"] = null;
                Session["MRDailyAtt"] = null;
                Session["MRLeave"] = null;
                Session["MRMonthly"] = null;
                Session["MRAudit"] = null;
                Session["MRManualEditAtt"] = null;
                Session["MREmployee"] = null;
                Session["MRDetail"] = null;
                Session["MRSummary"] = null;
                Session["FiltersModel"] = new WMSLibrary.FiltersModel();
                Session["LoginCount"] = null;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return View("Index");
            }
        }
       
        private string SerializeObject(object myObject)
        {
            var stream = new MemoryStream();
            var xmldoc = new XmlDocument();
            var serializer = new XmlSerializer(myObject.GetType());
            using (stream)
            {
                serializer.Serialize(stream, myObject);
                stream.Seek(0, SeekOrigin.Begin);
                xmldoc.Load(stream);
            }

            return xmldoc.InnerXml;
        }

        private object DeSerializeObject(object myObject, Type objectType)
        {
            var xmlSerial = new XmlSerializer(objectType);
            var xmlStream = new StringReader(myObject.ToString());
            return xmlSerial.Deserialize(xmlStream);
        }
        #region --Dashboard--
        public ActionResult GetDahboard()
        {
            DateTime dt = DateTime.Today.AddDays(-1);
            //DateTime dt = new DateTime(2016, 02, 02);
            DashboardValues dv = new DashboardValues();
            TAS2013Entities db = new TAS2013Entities();
            List<DailySummary> ds = new List<DailySummary>();
            List<JobCardDetail> jcEmp = new List<JobCardDetail>();
            if (dt.DayOfWeek == DayOfWeek.Saturday)
                dt = dt.AddDays(-1);
            if (dt.DayOfWeek == DayOfWeek.Sunday)
                dt = dt.AddDays(-2);
            jcEmp = db.JobCardDetails.Where(aa => aa.Dated == dt).ToList();
            ds = db.DailySummaries.Where(aa => aa.Date == dt && aa.Criteria == "C").ToList();
            if (ds.Count > 0)
            {
                dv.DateTime = dt.ToString("dd-MMM-yyy");
                dv.TotalEmps = (short)ds.FirstOrDefault().TotalEmps;
                dv.Present = (short)ds.FirstOrDefault().PresentEmps;
                dv.Absent = (short)ds.FirstOrDefault().AbsentEmps;
                dv.Leaves = (short)ds.FirstOrDefault().LvEmps;
                dv.LateIn = (short)ds.FirstOrDefault().LIEmps;
                dv.LateOut = (short)ds.FirstOrDefault().LOEmps;
                dv.EarlyIn = (short)ds.FirstOrDefault().EIEmps;
                dv.EarlyOut = (short)ds.FirstOrDefault().EOEmps;
                dv.OverTime = (short)ds.FirstOrDefault().OTEmps;
                dv.ShortLeaves = (short)ds.FirstOrDefault().ShortLvEmps;
                dv.JCFieldTour = jcEmp.Where(aa => aa.WrkCardID == 5).ToList().Count;
                dv.JCTraining = jcEmp.Where(aa => aa.WrkCardID == 7).ToList().Count;
                dv.JCSeminar = jcEmp.Where(aa => aa.WrkCardID == 8).ToList().Count;
                dv.JCOD = jcEmp.Where(aa => aa.WrkCardID == 1).ToList().Count;
                //dv.JCFieldTour = 80;
                //dv.JCTraining = 90;
                //dv.JCSeminar = 80;
                //dv.JCOD = 80;
                dv.EWork = (int)(ds.FirstOrDefault().ExpectedWorkMins / 60);
                dv.AWork = (int)(ds.FirstOrDefault().ActualWorkMins / 60);
                dv.LWork = (int)(ds.FirstOrDefault().LossWorkMins / 60);
            }
            else
            {
                var countOfRows = db.DailySummaries.Count();
                if (countOfRows > 1)
                {
                    ds = db.DailySummaries.ToList();
                    DailySummary dss = ds[countOfRows - 1];
                    dv.DateTime = ds[countOfRows - 1].Date.Value.Date.ToString("dd-MMM-yyy");
                    dv.TotalEmps = (short)ds[countOfRows - 1].TotalEmps;
                    dv.Present = (short)ds[countOfRows - 1].PresentEmps;
                    dv.Absent = (short)ds[countOfRows - 1].AbsentEmps;
                    dv.Leaves = (short)ds[countOfRows - 1].LvEmps;
                    dv.LateIn = (short)ds[countOfRows - 1].LIEmps;
                    dv.LateOut = (short)ds[countOfRows - 1].LOEmps;
                    dv.EarlyIn = (short)ds[countOfRows - 1].EIEmps;
                    dv.EarlyOut = (short)ds[countOfRows - 1].EOEmps;
                    dv.OverTime = (short)ds[countOfRows - 1].OTEmps;
                    dv.ShortLeaves = (short)ds[countOfRows - 1].ShortLvEmps;
                    dv.JCFieldTour = jcEmp.Where(aa => aa.WrkCardID == 5).ToList().Count;
                    dv.JCTraining = jcEmp.Where(aa => aa.WrkCardID == 7).ToList().Count;
                    dv.JCSeminar = jcEmp.Where(aa => aa.WrkCardID == 8).ToList().Count;
                    dv.JCOD = jcEmp.Where(aa => aa.WrkCardID == 1).ToList().Count;
                    //dv.JCFieldTour = 80;
                    //dv.JCTraining = 90;
                    //dv.JCSeminar = 80;
                    //dv.JCOD = 80;
                    dv.EWork = (int)(ds.FirstOrDefault().ExpectedWorkMins / 60);
                    dv.AWork = (int)(ds.FirstOrDefault().ActualWorkMins / 60);
                    dv.LWork = (int)(ds.FirstOrDefault().LossWorkMins / 60);
                }
            }
            if (HttpContext.Request.IsAjaxRequest())
                return Json(dv
                           , JsonRequestBehavior.AllowGet);

            return RedirectToAction("Index");
        }
        public ActionResult GetDahboardGraph()
        {
            //DateTime dtE =  new DateTime(2016,02,02);
            DateTime dtE = DateTime.Today.AddDays(-1);
            DateTime dtS = dtE.AddDays(-31);
            TAS2013Entities db = new TAS2013Entities();
            List<DailySummary> ds = new List<DailySummary>();
            List<DailySummary> dsTemp = new List<DailySummary>();
            DashboardGraph dg = new DashboardGraph();
            ds = db.DailySummaries.Where(aa => aa.Date >= dtS && aa.Date <= dtE && aa.Criteria == "C").ToList();
            if (ds.Count > 0)
            {
                dg.DateTime1 = ds[0].Date.Value.ToString("dd-MMM");
                dg.LateIn1 = (int)ds[0].LIEmps;

                if (ds.Count > 1)
                {
                    dg.DateTime2 = ds[2].Date.Value.ToString("dd-MMM");
                    dg.LateIn2 = (int)ds[2].LIEmps;
                }
                else
                {
                    dg.DateTime2 = ds[0].Date.Value.ToString("dd-MMM");
                    dg.LateIn2 = (int)ds[0].LIEmps;
                }
                if (ds.Count > 2)
                {
                    dg.DateTime3 = ds[4].Date.Value.ToString("dd-MMM");
                    dg.LateIn3 = (int)ds[4].LIEmps;
                }
                else
                {
                    dg.DateTime3 = ds[0].Date.Value.ToString("dd-MMM");
                    dg.LateIn3 = (int)ds[0].LIEmps;
                }
                if (ds.Count > 3)
                {
                    dg.DateTime4 = ds[7].Date.Value.ToString("dd-MMM");
                    dg.LateIn4 = (int)ds[7].LIEmps;
                }
                else
                {
                    dg.DateTime4 = ds[0].Date.Value.ToString("dd-MMM");
                    dg.LateIn4 = (int)ds[0].LIEmps;
                }
                if (ds.Count > 4)
                {
                    dg.DateTime5 = ds[10].Date.Value.ToString("dd-MMM");
                    dg.LateIn5 = (int)ds[10].LIEmps;
                }
                else
                {
                    dg.DateTime5 = ds[0].Date.Value.ToString("dd-MMM");
                    dg.LateIn5 = (int)ds[0].LIEmps;
                }
                if (ds.Count > 5)
                {
                    dg.DateTime6 = ds[12].Date.Value.ToString("dd-MMM");
                    dg.LateIn6 = (int)ds[12].LIEmps;
                }
                else
                {
                    dg.DateTime6 = ds[0].Date.Value.ToString("dd-MMM");
                    dg.LateIn6 = (int)ds[0].LIEmps;
                }
                if (ds.Count > 6)
                {
                    dg.DateTime7 = ds[15].Date.Value.ToString("dd-MMM");
                    dg.LateIn7 = (int)ds[15].LIEmps;
                }
                else
                {
                    dg.DateTime7 = ds[0].Date.Value.ToString("dd-MMM");
                    dg.LateIn7 = (int)ds[0].LIEmps;
                }
                if (ds.Count > 7)
                {
                    dg.DateTime8 = ds[17].Date.Value.ToString("dd-MMM");
                    dg.LateIn8 = (int)ds[17].LIEmps;
                }
                else
                {
                    dg.DateTime8 = ds[0].Date.Value.ToString("dd-MMM");
                    dg.LateIn8 = (int)ds[0].LIEmps;
                }
                if (ds.Count > 8)
                {
                    dg.DateTime9 = ds[19].Date.Value.ToString("dd-MMM");
                    dg.LateIn9 = (int)ds[19].LIEmps;
                }
                else
                {
                    dg.DateTime9 = ds[0].Date.Value.ToString("dd-MMM");
                    dg.LateIn9 = (int)ds[0].LIEmps;
                }
                if (ds.Count > 9)
                {
                    dg.DateTime10 = ds[ds.Count - 1].Date.Value.ToString("dd-MMM");
                    dg.LateIn10 = (int)ds[ds.Count - 1].LIEmps;
                }
                else
                {
                    dg.DateTime10 = ds[0].Date.Value.ToString("dd-MMM");
                    dg.LateIn10 = (int)ds[0].LIEmps;
                }
            }
            else
            {

            }
            if (HttpContext.Request.IsAjaxRequest())
                return Json(dg
                           , JsonRequestBehavior.AllowGet);

            return RedirectToAction("Index");
        }
        #endregion
    }
    public class DashboardValues
    {
        public string DateTime { get; set; }
        public int TotalEmps { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Leaves { get; set; }
        public int LateIn { get; set; }
        public int LateOut { get; set; }
        public int EarlyIn { get; set; }
        public int EarlyOut { get; set; }
        public int OverTime { get; set; }
        public int ShortLeaves { get; set; }
        public int JCFieldTour { get; set; }
        public int JCTraining { get; set; }
        public int JCSeminar { get; set; }
        public int JCOD { get; set; }
        public int EWork { get; set; }
        public int AWork { get; set; }
        public int LWork { get; set; }
    }

    public class DashboardGraph
    {
        public string DateTime1 { get; set; }
        public int LateIn1 { get; set; }
        public string DateTime2 { get; set; }
        public int LateIn2 { get; set; }
        public string DateTime3 { get; set; }
        public int LateIn3 { get; set; }
        public string DateTime4 { get; set; }
        public int LateIn4 { get; set; }
        public string DateTime5 { get; set; }
        public int LateIn5 { get; set; }
        public string DateTime6 { get; set; }
        public int LateIn6 { get; set; }
        public string DateTime7 { get; set; }
        public int LateIn7 { get; set; }
        public string DateTime8 { get; set; }
        public int LateIn8 { get; set; }
        public string DateTime9 { get; set; }
        public int LateIn9 { get; set; }
        public string DateTime10 { get; set; }
        public int LateIn10 { get; set; }
    }
}