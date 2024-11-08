using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using System.Windows;
using System.Windows.Navigation;
using System.Drawing;
using System.Globalization;
using PlanCheck.Users;
using PlanCheck.Languages;

namespace PlanCheck
{
    internal class Check_Course
    {
        private ScriptContext _ctx;
        private PreliminaryInformation _pinfo;

        private int maxNumberOfDays = 8;
        public Check_Course(PreliminaryInformation pinfo, ScriptContext ctx)  //Constructor
        {
            _ctx = ctx;
            _pinfo = pinfo;

            Check();

        }

        private List<Item_Result> _result = new List<Item_Result>();
        // private PreliminaryInformation _pinfo;
        private string _title = ResourceHelper.GetMessage("Planandcoursestatus1");

        public void Check()
        {
            //MessageBox.Show("toto \n"+ResourceHelper.GetMessage("generalError"));

            if (_pinfo.actualUserPreference.userWantsTheTest("currentCourseStatus"))
            {
                #region IS ACTUAL COURSE "EN COURS" ? 

                Item_Result currentCourseStatus = new Item_Result();
                currentCourseStatus.Label = "Course " + _ctx.Course.Id + " " + ResourceHelper.GetMessage("String2");
                currentCourseStatus.ExpectedValue = ResourceHelper.GetMessage("String1");

                if (_ctx.Course.CompletedDateTime == null)
                {
                    currentCourseStatus.MeasuredValue = ResourceHelper.GetMessage("String1");
                    currentCourseStatus.setToTRUE();
                }
                else
                {
                    currentCourseStatus.MeasuredValue = ResourceHelper.GetMessage("String3");
                    currentCourseStatus.setToFALSE();
                }
                currentCourseStatus.Infobulle = ResourceHelper.GetMessage("String4");

                this._result.Add(currentCourseStatus);

                #endregion
            }


            if (_pinfo.actualUserPreference.userWantsTheTest("planApprove"))
            {
                #region Is actual Plan PlanningApproved ? 

                Item_Result planApprove = new Item_Result();
                planApprove.Label = ResourceHelper.GetMessage("String5");
                planApprove.ExpectedValue = "EN COURS";



                planApprove.Infobulle = ResourceHelper.GetMessage("String6");
                if (!_pinfo.isTOMO)
                {
                    String[] beautifulDoctorName = _ctx.PlanSetup.PlanningApprover.Split('\\');
                    String[] TAname = _ctx.PlanSetup.TreatmentApprover.Split('\\');

                    if (_ctx.PlanSetup.ApprovalStatus.ToString() == "PlanningApproved")
                    {

                        planApprove.MeasuredValue = ResourceHelper.GetMessage("String7") + " " + beautifulDoctorName[1].ToUpper();// + _ctx.PlanSetup.PlanningApprover;s[0].ToString().ToUpper() + s.Substring(1);
                        planApprove.setToTRUE();

                    }
                    else if (_ctx.PlanSetup.ApprovalStatus.ToString() == "TreatmentApproved")
                    {

                        planApprove.MeasuredValue = "Treatment approved";
                        //Approuvé par le Dr " + _ctx.PlanSetup.TreatmentApprover + [1].ToUpper();// + _ctx.PlanSetup.PlanningApprover;s[0].ToString().ToUpper() + s.Substring(1);
                        planApprove.Infobulle += "\n\n" + ResourceHelper.GetMessage("String8");
                        planApprove.Infobulle += "\nPlanning approver: " + beautifulDoctorName[1].ToUpper() + "\nTreatment approver " + TAname[1].ToUpper();
                        planApprove.setToWARNING();
                    }
                    else
                    {
                        planApprove.MeasuredValue = _ctx.PlanSetup.ApprovalStatus.ToString();// "Différent de Planning Approved";
                        planApprove.setToFALSE();
                        //planApprove.Infobulle = "Le plan doit être Planning Approved";
                    }
                }
                else // else this is a tomo plan. A plan SEA must be planning approved with non-zero UMs  and dose to ref point
                {

                    if (_pinfo.SEAplanName != "null")
                    {
                        PlanSetup p = _ctx.Course.PlanSetups.FirstOrDefault(x => x.Id.Equals(_pinfo.SEAplanName, StringComparison.OrdinalIgnoreCase));
                        string msg1 = p.PrimaryReferencePoint.DailyDoseLimit.Dose.ToString();
                        string msg2 = p.Beams.First().Meterset.Value.ToString();
                        // MessageBox.Show(msg);

                        String[] beautifulDoctorName = p.PlanningApprover.Split('\\');
                        String[] TAname = p.TreatmentApprover.Split('\\');

                        if (p.ApprovalStatus.ToString() == "PlanningApproved")
                        {
                            planApprove.MeasuredValue = ResourceHelper.GetMessage("String10") + " " + p.Id + " " + ResourceHelper.GetMessage("String9") + " " + beautifulDoctorName[1].ToUpper();// + _ctx.PlanSetup.PlanningApprover;s[0].ToString().ToUpper() + s.Substring(1);
                            planApprove.setToTRUE();

                        }
                        else if (p.ApprovalStatus.ToString() == "TreatmentApproved")
                        {
                            planApprove.MeasuredValue = "Treatment approved";
                            //Approuvé par le Dr " + _ctx.PlanSetup.TreatmentApprover + [1].ToUpper();// + _ctx.PlanSetup.PlanningApprover;s[0].ToString().ToUpper() + s.Substring(1);
                            planApprove.Infobulle += "\n\n"+ ResourceHelper.GetMessage("String11") + " " + p.Id + " " + ResourceHelper.GetMessage("String12");
                            planApprove.Infobulle += "\nPlanning approver: " + beautifulDoctorName[1].ToUpper() + "\nTreatment approver " + TAname[1].ToUpper();
                            planApprove.setToWARNING();
                        }
                        else
                        {
                            planApprove.MeasuredValue = "TOMO : " + p.Id + " : " + p.ApprovalStatus.ToString();// "Différent de Planning Approved";
                            planApprove.setToFALSE();

                        }

                        if (msg1.Contains("NaN"))
                        {
                            planApprove.setToFALSE();
                            planApprove.Infobulle += "\n\n" + ResourceHelper.GetMessage("String13") + " " + p.Id + " " + ResourceHelper.GetMessage("String18");
                        }
                        else if (msg2.Contains("NaN"))
                        {
                            planApprove.setToFALSE();
                            planApprove.Infobulle += "\n\n" + ResourceHelper.GetMessage("String13") + " " + p.Id + " " + ResourceHelper.GetMessage("String14");
                        }
                        else
                            planApprove.Infobulle += "\n\n" + ResourceHelper.GetMessage("String15") + " " + p.Id;


                    }
                    else
                    {
                        planApprove.MeasuredValue = ResourceHelper.GetMessage("String16");
                        planApprove.Infobulle += "\n\n"+ ResourceHelper.GetMessage("String17");
                        planApprove.setToFALSE();
                    }

                }

                this._result.Add(planApprove);
                #endregion
            }


            if (_pinfo.actualUserPreference.userWantsTheTest("coursesStatus"))
            {
                #region other courses

                Item_Result coursesStatus = new Item_Result();
                coursesStatus.Label = ResourceHelper.GetMessage("String19");

                List<string> otherCoursesTerminated = new List<string>();
                List<string> otherCoursesNotOKNotQA_butRecent = new List<string>();
                List<string> otherQACoursesOK = new List<string>();
                List<string> oldCourses = new List<string>();
                coursesStatus.ExpectedValue = "...";



                coursesStatus.Infobulle = ResourceHelper.GetMessage("String20") +"\n";
                /*  coursesStatus.Infobulle += "\nERREUR si au moins un course (CQ ou non) est EN COURS cours depuis > " + maxNumberOfDays + " jours";
                  coursesStatus.Infobulle += "\nWARNING si au moins un course (non CQ) est en cours depuis moins de " + maxNumberOfDays + " jours";
                  coursesStatus.Infobulle += "\nOK si tous les course sont TERMINE (CQ ou non) ou EN COURS (CQ) depuis moins de " + maxNumberOfDays + " jours";
                */


                foreach (Course courseN in _ctx.Patient.Courses) // loop on the courses
                {

                    if (courseN.Id != _ctx.Course.Id) // do not test current course
                        if (courseN.CompletedDateTime != null) // --> terminated courses = there is a  completed date time
                        {
                            otherCoursesTerminated.Add(courseN.Id + " "+ ResourceHelper.GetMessage("String22") + " " + courseN.CompletedDateTime.ToString());
                        }
                        else // course not terminated
                        {
                            DateTime myToday = DateTime.Today;
                            int nDays = (myToday - (DateTime)courseN.StartDateTime).Days;
                            if (nDays < maxNumberOfDays) // if recent
                            {
                                int itIsaQA_Course = 0;
                                foreach (PlanSetup p in courseN.PlanSetups)
                                {
                                    if (p.PlanIntent.ToString() != "VERIFICATION") // is there at least one  nonQA plan in the course ? 
                                    {
                                        itIsaQA_Course = 0;  // yes --> not a QA course
                                        break;
                                    }
                                    itIsaQA_Course = 1;  // only QA Plans in the course --> QA course
                                }
                                if (itIsaQA_Course == 0) // en cours, recent, non QA
                                {
                                    otherCoursesNotOKNotQA_butRecent.Add(courseN.Id + " (" + nDays + " "+ ResourceHelper.GetMessage("String21"));

                                }
                                else // en cours, recent,  QA
                                {
                                    otherQACoursesOK.Add(courseN.Id + " (" + nDays + " "+ ResourceHelper.GetMessage("String21"));
                                }
                            }
                            else // if not recent
                            {
                                oldCourses.Add(courseN.Id + " (" + nDays + " " + ResourceHelper.GetMessage("String21"));
                            }


                        }
                }
                #region infobulle
                // coursesStatus.Infobulle += "\n\nListe des courses\n";
                if (oldCourses.Count() > 0)
                {
                    coursesStatus.Infobulle += "\n"+ ResourceHelper.GetMessage("String23") + " : \n";
                    foreach (string s in oldCourses)
                        coursesStatus.Infobulle += " - " + s + "\n";
                }
                if (otherCoursesNotOKNotQA_butRecent.Count() > 0)
                {
                    coursesStatus.Infobulle += "\n"+ ResourceHelper.GetMessage("String24") + " : \n";
                    foreach (string s in otherCoursesNotOKNotQA_butRecent)
                        coursesStatus.Infobulle += " - " + s + "\n";
                }
                if (otherQACoursesOK.Count() > 0)
                {
                    coursesStatus.Infobulle += "\n"+ ResourceHelper.GetMessage("String25") + " : \n";
                    foreach (string s in otherQACoursesOK)
                        coursesStatus.Infobulle += " - " + s + "\n";
                }
                if (otherCoursesTerminated.Count() > 0)
                {
                    coursesStatus.Infobulle += "\n"+ ResourceHelper.GetMessage("String26") + " : \n";
                    foreach (string s in otherCoursesTerminated)
                        coursesStatus.Infobulle += " - " + s + "\n";
                }
                #endregion

                if (oldCourses.Count() > 0)
                {
                    coursesStatus.setToFALSE();
                    coursesStatus.MeasuredValue = ResourceHelper.GetMessage("String27") + "\n";
                }
                else if (otherCoursesNotOKNotQA_butRecent.Count() > 0)
                {
                    coursesStatus.setToWARNING();
                    coursesStatus.MeasuredValue = ResourceHelper.GetMessage("String28") + "\n";
                }
                else
                {
                    coursesStatus.setToTRUE();
                    coursesStatus.MeasuredValue = ResourceHelper.GetMessage("String29");
                }
                this._result.Add(coursesStatus);
                #endregion
            }


            if (_pinfo.actualUserPreference.userWantsTheTest("tomoReportApproved"))
            {
                #region Tomo report approved ?  
                if (_pinfo.isTOMO)
                {

                    Item_Result tomoReportApproved = new Item_Result();
                    tomoReportApproved.Label = ResourceHelper.GetMessage("String30");
                    tomoReportApproved.ExpectedValue = "";

                    if (_pinfo.planReportIsFound)
                    {
                        if (_pinfo.tprd.Trd.approvalStatus == "Approved")
                        {
                            string str = _pinfo.tprd.Trd.approverID.Trim();
                            string str2 = char.ToUpper(str[0]) + str.Substring(1);
                            tomoReportApproved.MeasuredValue = ResourceHelper.GetMessage("String31") + " " + str2; // Dr Dalmasso
                            tomoReportApproved.setToTRUE();
                        }
                        else
                        {
                            tomoReportApproved.MeasuredValue = ResourceHelper.GetMessage("String32");
                            tomoReportApproved.setToFALSE();
                        }



                    }
                    else
                    {
                        tomoReportApproved.setToFALSE();
                        tomoReportApproved.MeasuredValue = ResourceHelper.GetMessage("String33");
                    }

                    tomoReportApproved.Infobulle = ResourceHelper.GetMessage("String34");
                    this._result.Add(tomoReportApproved);

                }

                #endregion
            }
        }
        public string Title
        {
            get { return _title; }
        }
        public List<Item_Result> Result
        {
            get { return _result; }
            set { _result = value; }
        }


    }
}
