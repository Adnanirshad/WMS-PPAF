using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WMS.Controllers.Filters;
using WMS.Models;

namespace WMS.Controllers.EditAttendance
{

    public class ManualAttendanceProcess
    {
        TAS2013Entities context = new TAS2013Entities();
        AttData _OldAttData = new AttData();
        AttData _NewAttData = new AttData();
        AttDataManEdit _ManualEditData = new AttDataManEdit();

        //Replace New TimeIn and Out with Old TimeIN and Out in Attendance Data
        public ManualAttendanceProcess(string EmpDate, string JobCardName, bool JobCardStatus, DateTime NewTimeIn, DateTime NewTimeOut, string NewDutyCode, int _UserID, TimeSpan _NewDutyTime, string _Remarks,short _ShiftMins)
        {
            _OldAttData = context.AttDatas.First(aa => aa.EmpDate == EmpDate);
            if (_OldAttData != null)
            {
                if (JobCardStatus == false)
                {
                    SaveOldAttData(_OldAttData, _UserID);
                    if (SaveNewAttData(NewTimeIn, NewTimeOut, NewDutyCode, _NewDutyTime, _Remarks,_ShiftMins))
                    {

                        _OldAttData.TimeIn = NewTimeIn;
                        _OldAttData.TimeOut = NewTimeOut;
                        _OldAttData.DutyCode = NewDutyCode;
                        _OldAttData.DutyTime = _NewDutyTime;
                        _OldAttData.WorkMin = 0;
                        _OldAttData.GZOTMin = 0;
                        _OldAttData.OTMin = 0;
                        _OldAttData.LateIn = 0;
                        _OldAttData.EarlyIn = 0;
                        _OldAttData.EarlyOut = 0;
                        _OldAttData.LateOut = 0;
                        _OldAttData.StatusAB = false;
                        _OldAttData.StatusP = true;
                        _OldAttData.StatusEO = null;
                        _OldAttData.StatusEI = null;
                        _OldAttData.StatusOT = null;
                        _OldAttData.StatusLI = null;
                        _OldAttData.StatusLO = null;
                        _OldAttData.StatusDO = null;
                        _OldAttData.StatusGZOT = null;
                        _OldAttData.StatusLeave = null;
                        _OldAttData.StatusMN = true;
                        _OldAttData.ShifMin = _ShiftMins;
                        _OldAttData.Remarks = "";
                        if (NewDutyCode == "G")
                            _OldAttData.StatusGZ = true;
                        else
                            _OldAttData.StatusGZ = false;
                        if (NewDutyCode == "R")
                            _OldAttData.StatusDO = true;
                        else
                            _OldAttData.StatusDO = false;
                        if (_Remarks != "")
                            _OldAttData.Remarks = "[" + _Remarks + "]";
                        _OldAttData.StatusLeave = null;
                        ProcessDailyAttendance(_OldAttData);
                    }
                }
            }
        }

        //Save Old and New Attendance Data in Manual Attendance Table
        private bool SaveNewAttData(DateTime _NewTimeIn, DateTime _NewTimeOut, string _NewDutyCode, TimeSpan _NewDutyTime, string _remarks,short _ShiftMins)
        {
            bool check = false;
            _ManualEditData.NewTimeIn = _NewTimeIn;
            _ManualEditData.NewTimeOut = _NewTimeOut;
            _ManualEditData.NewDutyCode = _NewDutyCode;
            _ManualEditData.EditDateTime = DateTime.Now;
            _ManualEditData.NewDutyTime = _NewDutyTime;
            _ManualEditData.NewRemarks = "[" + _remarks + "]";
            _ManualEditData.NewShiftMin = _ShiftMins;
            try
            {
                context.AttDataManEdits.Add(_ManualEditData);
                context.SaveChanges();
                check = true;
            }
            catch (Exception ex)
            {
                check = false;
            }
            return check;
        }

        private void SaveOldAttData(AttData _OldAttData, int Userid)
        {
            try
            {
                _ManualEditData.OldDutyCode = _OldAttData.DutyCode;
                _ManualEditData.OldTimeIn = _OldAttData.TimeIn;
                _ManualEditData.OldTimeOut = _OldAttData.TimeOut;
                _ManualEditData.EmpDate = _OldAttData.EmpDate;
                _ManualEditData.UserID = Userid;
                _ManualEditData.EditDateTime = DateTime.Now;
                _ManualEditData.EmpID = _OldAttData.EmpID;
                _ManualEditData.OldRemarks = _OldAttData.Remarks;
            }
            catch (Exception ex)
            {

            }
        }

        //Work Times calculation controller
        public void ProcessDailyAttendance(AttData _attData)
        {
            try
            {
                AttData attendanceRecord = _attData;
                Emp employee = attendanceRecord.Emp;
                Shift shift = employee.Shift;
                //If TimeIn and TimeOut are not null, then calculate other Atributes
                if (attendanceRecord.TimeIn != null && attendanceRecord.TimeOut != null)
                {
                    //If TimeIn = TimeOut then calculate according to DutyCode
                    if (attendanceRecord.TimeIn == attendanceRecord.TimeOut)
                    {
                        CalculateInEqualToOut(attendanceRecord);
                    }
                    else
                    {
                        if (attendanceRecord.DutyTime == new TimeSpan(0,0,0))
                        {
                            CalculateOpenShiftTimes(attendanceRecord, shift);
                        }
                        else
                        {
                            //if (attendanceRecord.TimeIn.Value.Date.Day == attendanceRecord.TimeOut.Value.Date.Day)
                            //{
                                CalculateShiftTimes(attendanceRecord, shift);
                            //}
                            //else
                            //{
                            //    CalculateOpenShiftTimes(attendanceRecord, shift);
                            //}
                        }
                    }

                }
            }
            catch (Exception ex)
            {
            }

            context.SaveChanges();
            context.Dispose();
        }

        TimeSpan OpenShiftThresholdStart = new TimeSpan(17, 00, 00);
        TimeSpan OpenShiftThresholdEnd = new TimeSpan(11, 00, 00);

        #region --Calculate Work Times--

        private void CalculateInEqualToOut(AttData attendanceRecord)
        {
            switch (attendanceRecord.DutyCode)
            {
                case "G":
                    attendanceRecord.StatusAB = false;
                    attendanceRecord.StatusGZ = true;
                    attendanceRecord.WorkMin = 0;
                    attendanceRecord.EarlyIn = 0;
                    attendanceRecord.EarlyOut = 0;
                    attendanceRecord.LateIn = 0;
                    attendanceRecord.LateOut = 0;
                    attendanceRecord.OTMin = 0;
                    attendanceRecord.GZOTMin = 0;
                    attendanceRecord.StatusGZOT = false;
                    attendanceRecord.TimeIn = null;
                    attendanceRecord.TimeOut = null;
                    attendanceRecord.Remarks = "[GZ][Manual]";
                    break;
                case "R":
                    attendanceRecord.StatusAB = false;
                    attendanceRecord.StatusGZ = false;
                    attendanceRecord.WorkMin = 0;
                    attendanceRecord.EarlyIn = 0;
                    attendanceRecord.EarlyOut = 0;
                    attendanceRecord.LateIn = 0;
                    attendanceRecord.LateOut = 0;
                    attendanceRecord.OTMin = 0;
                    attendanceRecord.GZOTMin = 0;
                    attendanceRecord.StatusGZOT = false;
                    attendanceRecord.TimeIn = null;
                    attendanceRecord.TimeOut = null;
                    attendanceRecord.StatusDO = true;
                    attendanceRecord.Remarks = "[DO][Manual]";
                    break;
                case "D":
                    attendanceRecord.StatusAB = true;
                    attendanceRecord.StatusGZ = false;
                    attendanceRecord.WorkMin = 0;
                    attendanceRecord.EarlyIn = 0;
                    attendanceRecord.EarlyOut = 0;
                    attendanceRecord.LateIn = 0;
                    attendanceRecord.LateOut = 0;
                    attendanceRecord.OTMin = 0;
                    attendanceRecord.GZOTMin = 0;
                    attendanceRecord.StatusGZOT = false;
                    attendanceRecord.TimeIn = null;
                    attendanceRecord.TimeOut = null;
                    attendanceRecord.StatusDO = false;
                    attendanceRecord.StatusP = false;
                    attendanceRecord.Remarks = "[Absent][Manual]";
                    break;
            }
        }

        private void CalculateShiftTimes(AttData attendanceRecord, Shift shift)
        {
            try
            {
                //Calculate WorkMin
                attendanceRecord.Remarks=attendanceRecord.Remarks.Replace("[LI]", "");
                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[EI]", "");
                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[EO]", "");
                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[LO]", "");
                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[G-OT]", "");
                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[R-OT]", "");
                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[N-OT]", "");
                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[Manual]", "");
                TimeSpan mins = (TimeSpan)(attendanceRecord.TimeOut - attendanceRecord.TimeIn);
                attendanceRecord.WorkMin = (short)(mins.TotalMinutes);
                //Check if GZ holiday then place all WorkMin in GZOTMin
                if (attendanceRecord.StatusGZ == true)
                {
                    attendanceRecord.PDays = 0;
                    attendanceRecord.ABDays = 0;
                    attendanceRecord.LeaveDays = 0;
                    attendanceRecord.GZOTMin = (short)mins.TotalMinutes;
                    attendanceRecord.WorkMin = (short)mins.TotalMinutes;
                    attendanceRecord.StatusGZOT = true;
                    attendanceRecord.Remarks = attendanceRecord.Remarks + "[G-OT]";
                    attendanceRecord.ABDays = 0;
                    attendanceRecord.PDays = 0;
                    attendanceRecord.LeaveDays = 0;
                }
                //if Rest day then place all WorkMin in OTMin
                else if (attendanceRecord.StatusDO == true)
                {
                    if (attendanceRecord.Emp.HasOT != false)
                    {
                        attendanceRecord.PDays = 0;
                        attendanceRecord.ABDays = 0;
                        attendanceRecord.LeaveDays = 0;
                        attendanceRecord.OTMin = (short)mins.TotalMinutes;
                        attendanceRecord.WorkMin = (short)mins.TotalMinutes;
                        attendanceRecord.StatusOT = true;
                        attendanceRecord.Remarks = attendanceRecord.Remarks + "[R-OT]";
                        attendanceRecord.ABDays = 0;
                        attendanceRecord.PDays = 0;
                        attendanceRecord.LeaveDays = 0;
                    }
                }
                else
                {
                    /////////// to-do -----calculate Margins for those shifts which has break mins 
                    if (attendanceRecord.StatusBreak == true)
                    {
                        attendanceRecord.WorkMin = (short)(attendanceRecord.WorkMin - attendanceRecord.BreakMin);
                        attendanceRecord.ShifMin = (short)(ProcessSupportFunc.CalculateShiftMinutes(shift, attendanceRecord.AttDate.Value.DayOfWeek) - (short)attendanceRecord.BreakMin);
                    }
                    else
                    {
                        if (attendanceRecord.StatusHL == true && attendanceRecord.StatusLeave != true)
                        {
                            attendanceRecord.ABDays = 0;
                            attendanceRecord.PDays = 0.5;
                            attendanceRecord.LeaveDays = 0.5;
                        }
                        else
                        {
                            attendanceRecord.ABDays = 0;
                            attendanceRecord.PDays = 1;
                            attendanceRecord.LeaveDays = 0;
                        }
                        attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[Absent]", "");
                        attendanceRecord.StatusAB = false;
                        attendanceRecord.StatusP = true;
                        #region -- Margins--
                        //Calculate Late IN, Compare margin with Shift Late In
                        if (attendanceRecord.TimeIn.Value.TimeOfDay > attendanceRecord.DutyTime)
                        {
                            TimeSpan lateMinsSpan = (TimeSpan)(attendanceRecord.TimeIn.Value.TimeOfDay - attendanceRecord.DutyTime);
                            if (lateMinsSpan.TotalMinutes > shift.LateIn)
                            {
                                attendanceRecord.LateIn = (short)lateMinsSpan.TotalMinutes;
                                attendanceRecord.StatusLI = true;
                                attendanceRecord.EarlyIn = null;
                                attendanceRecord.Remarks = attendanceRecord.Remarks + "[LI]";
                            }
                            else
                            {
                                attendanceRecord.StatusLI = null;
                                attendanceRecord.LateIn = null;
                                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[LI]", "");
                            }
                        }
                        else
                        {
                            attendanceRecord.StatusLI = null;
                            attendanceRecord.LateIn = null;
                            attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[LI]", "");
                        }

                        //Calculate Early In, Compare margin with Shift Early In
                        if (attendanceRecord.TimeIn.Value.TimeOfDay < attendanceRecord.DutyTime)
                        {
                            TimeSpan EarlyInMinsSpan = (TimeSpan)(attendanceRecord.DutyTime - attendanceRecord.TimeIn.Value.TimeOfDay);
                            if (EarlyInMinsSpan.TotalMinutes > shift.EarlyIn)
                            {
                                attendanceRecord.EarlyIn = (short)EarlyInMinsSpan.TotalMinutes;
                                attendanceRecord.StatusEI = true;
                                attendanceRecord.LateIn = null;
                                attendanceRecord.Remarks = attendanceRecord.Remarks + "[EI]";
                            }
                            else
                            {
                                attendanceRecord.StatusEI = null;
                                attendanceRecord.EarlyIn = null;
                                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[EI]", "");
                            }
                        }
                        else
                        {
                            attendanceRecord.StatusEI = null;
                            attendanceRecord.EarlyIn = null;
                            attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[EI]", "");
                        }

                        // CalculateShiftEndTime = ShiftStart + DutyHours
                        DateTime shiftEnd = ProcessSupportFunc.CalculateShiftEndTimeWithAttData(attendanceRecord.AttDate.Value, attendanceRecord.DutyTime.Value, (short)(attendanceRecord.ShifMin + attendanceRecord.BreakMin));

                        //Calculate Early Out, Compare margin with Shift Early Out
                        if (attendanceRecord.TimeOut < shiftEnd)
                        {
                            TimeSpan EarlyOutMinsSpan = (TimeSpan)(shiftEnd - attendanceRecord.TimeOut);
                            if (EarlyOutMinsSpan.TotalMinutes > shift.EarlyOut)
                            {
                                attendanceRecord.EarlyOut = (short)EarlyOutMinsSpan.TotalMinutes;
                                attendanceRecord.StatusEO = true;
                                attendanceRecord.LateOut = null;
                                attendanceRecord.Remarks = attendanceRecord.Remarks + "[EO]";
                            }
                            else
                            {
                                attendanceRecord.StatusEO = null;
                                attendanceRecord.EarlyOut = null;
                                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[EO]", "");
                            }
                        }
                        else
                        {
                            attendanceRecord.StatusEO = null;
                            attendanceRecord.EarlyOut = null;
                            attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[EO]", "");
                        }
                        //Calculate Late Out, Compare margin with Shift Late Out
                        if (attendanceRecord.TimeOut > shiftEnd)
                        {
                            TimeSpan LateOutMinsSpan = (TimeSpan)(attendanceRecord.TimeOut - shiftEnd);
                            if (LateOutMinsSpan.TotalMinutes > shift.LateOut)
                            {
                                attendanceRecord.LateOut = (short)LateOutMinsSpan.TotalMinutes;
                                // Late Out cannot have an early out, In case of poll at multiple times before and after shiftend
                                attendanceRecord.EarlyOut = null;
                                attendanceRecord.StatusLO = true;
                                attendanceRecord.Remarks = attendanceRecord.Remarks + "[LO]";
                            }
                            else
                            {
                                attendanceRecord.StatusLO = null;
                                attendanceRecord.LateOut = null;
                                attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[LO]", "");
                            }
                        }
                        else
                        {
                            attendanceRecord.StatusLO = null;
                            attendanceRecord.LateOut = null;
                            attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[LO]", "");
                        }
                        #endregion
                        if ((attendanceRecord.StatusGZ != true || attendanceRecord.StatusDO != true) && attendanceRecord.Emp.HasOT == true)
                        {
                            if (attendanceRecord.LateOut != null)
                            {
                                attendanceRecord.OTMin = attendanceRecord.LateOut;
                                attendanceRecord.StatusOT = true;
                                attendanceRecord.Remarks = attendanceRecord.Remarks + "[N-OT]";
                            }
                        }
                        //Subtract EarlyIn and LateOut from Work Minutes
                        if (shift.SubtractEIFromWork == true)
                        {
                            if (attendanceRecord.EarlyIn != null && attendanceRecord.EarlyIn > shift.EarlyIn)
                            {
                                attendanceRecord.WorkMin = (short)(attendanceRecord.WorkMin - attendanceRecord.EarlyIn);
                            }
                        }
                        if (shift.SubtractOTFromWork == true)
                        {
                            if (attendanceRecord.LateOut != null && attendanceRecord.LateOut > shift.LateOut)
                            {
                                attendanceRecord.WorkMin = (short)(attendanceRecord.WorkMin - attendanceRecord.LateOut);
                            }
                        }
                        if (shift.AddEIInOT == true && attendanceRecord.Emp.HasOT == true)
                        {
                            if (attendanceRecord.EarlyIn != null)
                            {
                                if (attendanceRecord.OTMin != null)
                                {
                                    attendanceRecord.OTMin = (short)(attendanceRecord.OTMin + attendanceRecord.EarlyIn);
                                }
                                else
                                    attendanceRecord.OTMin = (short)attendanceRecord.EarlyIn;
                            }
                        }
                        if (shift.RoundOffWorkMin == true)
                        {
                            if (attendanceRecord.LateOut != null || attendanceRecord.EarlyIn != null)
                            {
                                if (attendanceRecord.WorkMin > attendanceRecord.ShifMin && (attendanceRecord.WorkMin <= (attendanceRecord.ShifMin + shift.OverTimeMin)))
                                {
                                    attendanceRecord.WorkMin = attendanceRecord.ShifMin;
                                }
                            }
                        }
                        if (attendanceRecord.StatusHL == true)
                        {
                            attendanceRecord.PDays = 0.5;
                            attendanceRecord.ABDays = 0;
                            attendanceRecord.LeaveDays = 0.5;
                        }
                        else
                        {
                            attendanceRecord.PDays = 1;
                            attendanceRecord.ABDays = 0;
                            attendanceRecord.LeaveDays = 0;
                        }
                        //Mark Absent if less than 4 hours
                        if (attendanceRecord.AttDate.Value.DayOfWeek != DayOfWeek.Friday && attendanceRecord.StatusDO != true && attendanceRecord.StatusGZ != true)
                        {
                            short MinShiftMin = (short)shift.MinHrs;
                            if (attendanceRecord.WorkMin < MinShiftMin)
                            {
                                attendanceRecord.StatusAB = true;
                                attendanceRecord.StatusP = false;
                                attendanceRecord.Remarks = "[Absent]";
                            }
                            else
                            {
                                attendanceRecord.StatusAB = false;
                                attendanceRecord.StatusP = true;
                                attendanceRecord.Remarks.Replace("[Absent]", "");
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        private void CalculateOpenShiftTimes(AttData attendanceRecord, Shift shift)
        {
            try
            {
                //Calculate WorkMin
                //Calculate WorkMin
                attendanceRecord.Remarks.Replace("[LI]", "");
                attendanceRecord.Remarks.Replace("[EI]", "");
                attendanceRecord.Remarks.Replace("[EO]", "");
                attendanceRecord.Remarks.Replace("[LO]", "");
                attendanceRecord.Remarks.Replace("[G-OT]", "");
                attendanceRecord.Remarks.Replace("[R-OT]", "");
                attendanceRecord.Remarks.Replace("[N-OT]", "");
                attendanceRecord.Remarks.Replace("[Manual]", "");
                attendanceRecord.Remarks = attendanceRecord.Remarks + "[Manual]";
                if (attendanceRecord != null)
                {
                    if (attendanceRecord.TimeOut != null && attendanceRecord.TimeIn != null)
                    {
                        attendanceRecord.Remarks = "";
                        TimeSpan mins = (TimeSpan)(attendanceRecord.TimeOut - attendanceRecord.TimeIn);
                        attendanceRecord.WorkMin = (short)mins.TotalMinutes;
                        //Check if GZ holiday then place all WorkMin in GZOTMin
                        if (attendanceRecord.StatusGZ == true)
                        {
                            attendanceRecord.GZOTMin = (short)mins.TotalMinutes;
                            attendanceRecord.WorkMin = (short)mins.TotalMinutes;
                            attendanceRecord.StatusGZOT = true;
                            attendanceRecord.Remarks = attendanceRecord.Remarks + "[GZ-OT]";
                        }
                        else if (attendanceRecord.StatusDO == true)
                        {
                            attendanceRecord.OTMin = (short)mins.TotalMinutes;
                            attendanceRecord.WorkMin = (short)mins.TotalMinutes;
                            attendanceRecord.StatusOT = true;
                            attendanceRecord.Remarks = attendanceRecord.Remarks + "[R-OT]";
                        }
                        else
                        {
                            if (shift.HasBreak == true)
                            {
                                attendanceRecord.WorkMin = (short)(attendanceRecord.WorkMin - shift.BreakMin);
                                attendanceRecord.ShifMin = (short)(ProcessSupportFunc.CalculateShiftMinutes(shift, attendanceRecord.AttDate.Value.DayOfWeek) - (short)attendanceRecord.BreakMin);
                            }
                            else
                            {
                                attendanceRecord.Remarks.Replace("[Absent]", "");
                                attendanceRecord.StatusAB = false;
                                attendanceRecord.StatusP = true;
                                // CalculateShiftEndTime = ShiftStart + DutyHours
                                //Calculate OverTIme, 
                                if ((mins.TotalMinutes > (attendanceRecord.ShifMin + shift.OverTimeMin)) && attendanceRecord.Emp.HasOT == true)
                                {
                                    attendanceRecord.OTMin = (Int16)(Convert.ToInt16(mins.TotalMinutes) - attendanceRecord.ShifMin);
                                    attendanceRecord.WorkMin = (short)((mins.TotalMinutes) - attendanceRecord.OTMin);
                                    attendanceRecord.StatusOT = true;
                                    attendanceRecord.Remarks = attendanceRecord.Remarks + "[N-OT]";
                                }
                                //Calculate Early Out
                                if (mins.TotalMinutes < (attendanceRecord.ShifMin - shift.EarlyOut))
                                {
                                    Int16 EarlyoutMin = (Int16)(attendanceRecord.ShifMin - Convert.ToInt16(mins.TotalMinutes));
                                    if (EarlyoutMin > shift.EarlyOut)
                                    {
                                        attendanceRecord.EarlyOut = EarlyoutMin;
                                        attendanceRecord.StatusEO = true;
                                        attendanceRecord.Remarks = attendanceRecord.Remarks + "[EO]";
                                    }
                                    else
                                    {
                                        attendanceRecord.StatusEO = null;
                                        attendanceRecord.EarlyOut = null;
                                        attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[EO]", "");
                                    }
                                }
                                else
                                {
                                    attendanceRecord.StatusEO = null;
                                    attendanceRecord.EarlyOut = null;
                                    attendanceRecord.Remarks = attendanceRecord.Remarks.Replace("[EO]", "");
                                }
                                if ((attendanceRecord.StatusGZ != true || attendanceRecord.StatusDO != true) && attendanceRecord.Emp.HasOT == true)
                                {
                                    if (attendanceRecord.LateOut != null)
                                    {
                                        attendanceRecord.OTMin = attendanceRecord.LateOut;
                                        attendanceRecord.StatusOT = true;
                                        attendanceRecord.Remarks = attendanceRecord.Remarks + "[N-OT]";
                                    }
                                }
                                if (shift.SubtractOTFromWork == true)
                                {
                                    if (attendanceRecord.LateOut != null && attendanceRecord.LateOut > shift.LateOut)
                                    {
                                        attendanceRecord.WorkMin = (short)(attendanceRecord.WorkMin - attendanceRecord.LateOut);
                                    }
                                }
                                if (shift.RoundOffWorkMin == true)
                                {
                                    if (attendanceRecord.LateOut != null || attendanceRecord.EarlyIn != null)
                                    {
                                        // round off work mins if overtime less than shift.OverTimeMin >
                                        if (attendanceRecord.WorkMin > attendanceRecord.ShifMin && (attendanceRecord.WorkMin <= (attendanceRecord.ShifMin + shift.OverTimeMin)))
                                        {
                                            attendanceRecord.WorkMin = attendanceRecord.ShifMin;
                                        }
                                    }
                                }

                                //Mark Absent if less than 4 hours
                                if (attendanceRecord.AttDate.Value.DayOfWeek != DayOfWeek.Friday && attendanceRecord.StatusDO != true && attendanceRecord.StatusGZ != true)
                                {
                                    short MinShiftMin = (short)shift.MinHrs;
                                    if (attendanceRecord.WorkMin < MinShiftMin)
                                    {
                                        attendanceRecord.StatusAB = true;
                                        attendanceRecord.StatusP = false;
                                        attendanceRecord.Remarks = attendanceRecord.Remarks + "[Absent]";
                                    }
                                    else
                                    {
                                        attendanceRecord.StatusAB = false;
                                        attendanceRecord.StatusP = true;
                                        attendanceRecord.Remarks.Replace("[Absent]", "");
                                    }

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        #endregion

        #region --Helper Functions--
        private string ReturnDayOfWeek(DayOfWeek dayOfWeek)
        {
            string _DayName = "";
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday:
                    _DayName = "Monday";
                    break;
                case DayOfWeek.Tuesday:
                    _DayName = "Tuesday";
                    break;
                case DayOfWeek.Wednesday:
                    _DayName = "Wednesday";
                    break;
                case DayOfWeek.Thursday:
                    _DayName = "Thursday";
                    break;
                case DayOfWeek.Friday:
                    _DayName = "Friday";
                    break;
                case DayOfWeek.Saturday:
                    _DayName = "Saturday";
                    break;
                case DayOfWeek.Sunday:
                    _DayName = "Sunday";
                    break;
            }
            return _DayName;
        }

        private TimeSpan CalculateShiftEndTime(Shift shift, DayOfWeek dayOfWeek)
        {
            Int16 workMins = 0;
            try
            {
                switch (dayOfWeek)
                {
                    case DayOfWeek.Monday:
                        workMins = shift.MonMin;
                        break;
                    case DayOfWeek.Tuesday:
                        workMins = shift.TueMin;
                        break;
                    case DayOfWeek.Wednesday:
                        workMins = shift.WedMin;
                        break;
                    case DayOfWeek.Thursday:
                        workMins = shift.ThuMin;
                        break;
                    case DayOfWeek.Friday:
                        workMins = shift.FriMin;
                        break;
                    case DayOfWeek.Saturday:
                        workMins = shift.SatMin;
                        break;
                    case DayOfWeek.Sunday:
                        workMins = shift.SunMin;
                        break;
                }
            }
            catch (Exception ex)
            {

            }
            return shift.StartTime + (new TimeSpan(0, workMins, 0));
        }
        private TimeSpan CalculateShiftEndTime(Shift shift,short ShiftMins)
        {

            return shift.StartTime + (new TimeSpan(0, ShiftMins, 0));
        }
        private DateTime CalculateShiftEndTime(Shift shift, DateTime _AttDate, TimeSpan _DutyTime)
        {
            Int16 workMins = 0;
            try
            {
                switch (_AttDate.Date.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        workMins = shift.MonMin;
                        break;
                    case DayOfWeek.Tuesday:
                        workMins = shift.TueMin;
                        break;
                    case DayOfWeek.Wednesday:
                        workMins = shift.WedMin;
                        break;
                    case DayOfWeek.Thursday:
                        workMins = shift.ThuMin;
                        break;
                    case DayOfWeek.Friday:
                        workMins = shift.FriMin;
                        break;
                    case DayOfWeek.Saturday:
                        workMins = shift.SatMin;
                        break;
                    case DayOfWeek.Sunday:
                        workMins = shift.SunMin;
                        break;
                }
            }
            catch (Exception ex)
            {

            }
            DateTime _datetime = new DateTime();
            TimeSpan _Time = new TimeSpan(0, workMins, 0);
            _datetime = _AttDate.Date.Add(_DutyTime);
            _datetime = _datetime.Add(_Time);
            return _datetime;
        }

        private DateTime CalculateShiftEndTime(Shift shift, DateTime _AttDate, TimeSpan _DutyTime,short ShiftMins)
        {
            DateTime _datetime = new DateTime();
            TimeSpan _Time = new TimeSpan(0, ShiftMins, 0);
            _datetime = _AttDate.Date.Add(_DutyTime);
            _datetime = _datetime.Add(_Time);
            return _datetime;
        }
        private Int16 CalculateShiftMinutes(Shift shift, DayOfWeek dayOfWeek)
        {
            Int16 workMins = 0;
            try
            {
                switch (dayOfWeek)
                {
                    case DayOfWeek.Monday:
                        workMins = shift.MonMin;
                        break;
                    case DayOfWeek.Tuesday:
                        workMins = shift.TueMin;
                        break;
                    case DayOfWeek.Wednesday:
                        workMins = shift.WedMin;
                        break;
                    case DayOfWeek.Thursday:
                        workMins = shift.ThuMin;
                        break;
                    case DayOfWeek.Friday:
                        workMins = shift.FriMin;
                        break;
                    case DayOfWeek.Saturday:
                        workMins = shift.SatMin;
                        break;
                    case DayOfWeek.Sunday:
                        workMins = shift.SunMin;
                        break;
                }
            }
            catch (Exception ex)
            {

            }
            return workMins;
        }

        #endregion

    }
}

