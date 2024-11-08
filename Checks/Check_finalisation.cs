﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using System.Windows;
using PlanCheck.Languages;

namespace PlanCheck
{
    internal class Check_finalisation
    {
        private ScriptContext _ctx;
        private PreliminaryInformation _pinfo;
        private read_check_protocol _rcp;
        public Check_finalisation(PreliminaryInformation pinfo, ScriptContext ctx, read_check_protocol rcp)  //Constructor
        {
            _ctx = ctx;
            _pinfo = pinfo;
            _rcp = rcp;
            Check();

        }

        private List<Item_Result> _result = new List<Item_Result>();
        // private PreliminaryInformation _pinfo;
        private string _title = "Finalisation";
        private bool haveTheSameMU(PlanSetup p1, PlanSetup p2)
        {



            double umplan1 = 0.0;
            double umplan2 = 0.0;

            foreach (Beam b in p1.Beams)
            {
                if (!b.IsSetupField)
                    umplan1 += b.Meterset.Value;
            }
            foreach (Beam b in p2.Beams)
            {
                if (!b.IsSetupField)
                    umplan2 += b.Meterset.Value;
            }


            if (Math.Abs(umplan1 - umplan2) > 0.1) return (false);
            else return (true);

        }

        private bool isCalibrationFieldOK(string QAtype, Course c)
        {
            bool calibok = true;
            /*
            if (QAtype == "RUBY")
            { // calibation plan must be 6xFFF + 200MU + tolerance table ok and plan approved
                calibok = false;
                foreach (PlanSetup p in c.PlanSetups)
                {
                    Beam b = p.Beams.FirstOrDefault(b1 => !b1.IsSetupField);
                    if (p.Id.ToUpper().Contains("10X10"))
                        if (b.EnergyModeDisplayName == "6X-FFF")
                            if (b.Meterset.Value == 200.0)
                                if (b.ToleranceTableLabel == "Physique_0tolera")
                                    if (p.ApprovalStatus.ToString() == "PlanningApproved")
                                        calibok = true;


                    //p.id.ToUpper().contains
                }


            }
            */
            // no more calib field for octa
            // no more calib field check for ruby


            return calibok;

        }


        public void Check()
        {
            if (_pinfo.actualUserPreference.userWantsTheTest("ariaDocuments"))
            {
                #region aria documents
                Item_Result ariaDocuments = new Item_Result();
                ariaDocuments.Label = ResourceHelper.GetMessage("String88");
                ariaDocuments.Infobulle = ResourceHelper.GetMessage("String89") + "  : \n";
                ariaDocuments.Infobulle += "  -  " + ResourceHelper.GetMessage("String90") + "\n";
                ariaDocuments.Infobulle += "  -  " + ResourceHelper.GetMessage("String110") + "\n";

                if (_pinfo.isTOMO)
                    ariaDocuments.Infobulle += "  -  " + ResourceHelper.GetMessage("String91") + "\n\n";
                else
                    ariaDocuments.Infobulle += "  -  " + ResourceHelper.GetMessage("String92") + "\n\n";

                //ariaDocuments.Infobulle += "Le système peut détecter une absence de ces documents mais ne peut pas vérifier qu'ils sont corrects\n";
                //ariaDocuments.Infobulle += "(sauf la dosimétrie Tomotherapy, pour lequel la dose max du plan est comparée à la dose max indiquée dans le rapport pdf\n";

                bool allisgood = true;



                if (!_pinfo.doseCheckReportIsFound)
                {
                    if (_pinfo.doseCheckIsNeeded)
                    {
                        allisgood = false;
                        ariaDocuments.MeasuredValue += ResourceHelper.GetMessage("String93") + ", ";
                    }
                }
                if (!_pinfo.positionReportIsFound)
                {
                    allisgood = false;
                    ariaDocuments.MeasuredValue += ResourceHelper.GetMessage("String94") + ", ";
                }
                //MessageBox.Show("planreportfound " + _pinfo.planReportIsFound + "\n " + _pinfo.isTOMO + " \n" + _pinfo.EclipseReportMessage);

                if ((!_pinfo.planReportIsFound))
                {

                    ariaDocuments.MeasuredValue += ResourceHelper.GetMessage("String95");

                    allisgood = false;
                }
                else if ((!_pinfo.isTOMO) && (_pinfo.EclipseReportMessage != "ok"))
                {
                    ariaDocuments.MeasuredValue += ResourceHelper.GetMessage("String95");
                    allisgood = false;


                }


                if (!allisgood)
                {
                    //ariaDocuments.MeasuredValue += "absent(s) ou > 30 jours";
                    ariaDocuments.setToFALSE();
                }
                else
                {
                    ariaDocuments.setToTRUE();
                    ariaDocuments.MeasuredValue = ResourceHelper.GetMessage("String96");
                }
                this._result.Add(ariaDocuments);

                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("preparedQA"))
            {
                #region QA plans

                if (!_pinfo.isTOMO) // no check for tomo
                {
                    Item_Result preparedQA = new Item_Result();
                    preparedQA.Label = "CQ";
                    preparedQA.ExpectedValue = "EN COURS";
                    String nameOfMatch = null;
                    List<PlanSetup> qaPlans = new List<PlanSetup>();
                    List<String> qaPlansPresent = new List<String>();
                    List<String> qaPlansMissing = new List<String>();
                    List<String> unapprovedQAplans = new List<String>();
                    List<String> wrongAlgoQAplans = new List<String>();
                    List<String> wrongCalibrationQAplans = new List<String>();
                    foreach (Course c in _ctx.Patient.Courses) // list QA plans of the patient
                    {
                        foreach (PlanSetup p in c.PlanSetups)
                        {
                            try
                            {
                                if (p.PlanIntent.ToString() == "VERIFICATION") // QA plan                        
                                    qaPlans.Add(p);

                                // MessageBox.Show("making the list " + p.PlanIntent.ToString());
                            }
                            catch
                            {
                                ; // do nothing
                            }
                        }
                    }

                    if (_rcp.listQAplans.Count > 0) // list needed QA plans in protocol
                    {
                        foreach (String qa in _rcp.listQAplans) // loop on required QAplans
                        {
                            bool found = false;
                            if ((_pinfo.isModulated) || (_pinfo.treatmentType.ToUpper().Contains("DCA")))
                                if (qa == "PDIP") // protocol wants a pdip qa
                                {


                                    foreach (PlanSetup p in qaPlans) // loop on present QA plans
                                    {

                                        if (p.Id.ToUpper().Contains("PDIP") || (p.Course.Id.ToUpper().Contains("PDIP")))
                                        {


                                            if (haveTheSameMU(p, _ctx.PlanSetup))
                                            {


                                                nameOfMatch = p.Id + " (Course:" + p.Course.Id + ")";
                                                found = true;
                                                if (p.ApprovalStatus.ToString() != "PlanningApproved")
                                                    unapprovedQAplans.Add(p.Id);

                                                //String machine = _ctx.PlanSetup.Beams.FirstOrDefault().Id.ToUpper();
                                                if (!_pinfo.isHALCYON) // if not HALCYON 
                                                {
                                                    if (p.PhotonCalculationModel != _ctx.PlanSetup.PhotonCalculationModel)
                                                        wrongAlgoQAplans.Add(p.Id + " " + p.PhotonCalculationModel);
                                                }
                                                else // if HALCYON PDIP must be in AAA
                                                {
                                                    if (p.PhotonCalculationModel != "AAA_15605New")
                                                        wrongAlgoQAplans.Add(p.Id + " " + p.PhotonCalculationModel);
                                                }
                                                break;
                                            }
                                        }
                                    }

                                }
                                else if (qa == "RUBY")
                                {
                                    foreach (PlanSetup p in qaPlans) // loop on present QA plans
                                    {
                                        if (p.Id.ToUpper().Contains("RUBY") || (p.Course.Id.ToUpper().Contains("RUBY")))
                                        {
                                            if (haveTheSameMU(p, _ctx.PlanSetup))
                                            {
                                                nameOfMatch = p.Id + " (Course:" + p.Course.Id + ")";
                                                found = true;
                                                if (p.ApprovalStatus.ToString() != "PlanningApproved")
                                                    unapprovedQAplans.Add(p.Id);

                                                bool calibrationFieldisOK = isCalibrationFieldOK("RUBY", p.Course);
                                                //MessageBox.Show("calibration field " + calibrationFieldisOK);
                                                if (!calibrationFieldisOK)
                                                    wrongCalibrationQAplans.Add(p.Id);

                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (qa == "Octa4D")
                                {
                                    foreach (PlanSetup p in qaPlans) // loop on present QA plans
                                    {
                                        if (p.Id.ToUpper().Contains("OCT") || (p.Course.Id.ToUpper().Contains("OCT")))
                                        {
                                            if (haveTheSameMU(p, _ctx.PlanSetup))
                                            {
                                                nameOfMatch = p.Id + " (Course:" + p.Course.Id + ")";
                                                found = true;
                                                if (p.ApprovalStatus.ToString() != "PlanningApproved")
                                                    unapprovedQAplans.Add(p.Id);
                                                bool calibrationFieldisOK = true;// no more needed calib plan // isCalibrationFieldOK("Octa", p.Course);
                                                if (!calibrationFieldisOK)
                                                    wrongCalibrationQAplans.Add(p.Id);
                                                break;
                                            }
                                        }
                                    }
                                }

                            if (found == true)
                            {
                                qaPlansPresent.Add(qa + " --> " + nameOfMatch);
                            }
                            else
                            {
                                qaPlansMissing.Add(qa);
                            }

                        }
                        if ((qaPlansMissing.Count > 0) || (wrongAlgoQAplans.Count() > 0))
                        {
                            preparedQA.setToFALSE();
                            preparedQA.MeasuredValue = ResourceHelper.GetMessage("String97");// "Différent de Planning Approved";
                            if (qaPlansMissing.Count > 0)
                                preparedQA.Infobulle = ResourceHelper.GetMessage("String98") + " " + _rcp.protocolName;
                            if (wrongAlgoQAplans.Count() > 0)
                            {
                                preparedQA.Infobulle += "\n\n"+ ResourceHelper.GetMessage("String99");
                                foreach (String s in wrongAlgoQAplans)
                                    preparedQA.Infobulle += "\n - " + s;
                            }

                        }
                        else if (unapprovedQAplans.Count > 0)
                        {
                            preparedQA.MeasuredValue = ResourceHelper.GetMessage("String100");
                            preparedQA.Infobulle = ResourceHelper.GetMessage("String101") + " :\n";
                            foreach (String s in unapprovedQAplans)
                                preparedQA.Infobulle += "\n - " + s;
                            preparedQA.setToWARNING();
                        }
                        else
                        {
                            preparedQA.setToTRUE();
                            preparedQA.MeasuredValue = ResourceHelper.GetMessage("String102");// "Différent de Planning Approved";
                            preparedQA.Infobulle = ResourceHelper.GetMessage("String103") + " " + _rcp.protocolName + " "+ ResourceHelper.GetMessage("String104");


                            if (wrongCalibrationQAplans.Count > 0)
                            {
                                preparedQA.setToWARNING();
                                preparedQA.MeasuredValue = ResourceHelper.GetMessage("String105");// "Différent de Planning Approved";

                                preparedQA.Infobulle += "\n\n"+ ResourceHelper.GetMessage("String106") + " :";
                                foreach (String s in wrongCalibrationQAplans)
                                    preparedQA.Infobulle += "\n - " + s;
                            }

                        }
                        if (qaPlansPresent.Count() > 0)
                        {
                            preparedQA.Infobulle += "\n\n"+ ResourceHelper.GetMessage("String107") + " :";
                            foreach (String s in qaPlansPresent)
                                preparedQA.Infobulle += "\n - " + s;
                        }

                        if (qaPlansMissing.Count() > 0)
                        {
                            preparedQA.Infobulle += "\n\n"+ ResourceHelper.GetMessage("String108") + "  :";
                            foreach (String s in qaPlansMissing)
                                preparedQA.Infobulle += "\n - " + s;
                        }


                    }
                    else // no QA in protocol
                    {
                        preparedQA.setToINFO();
                        preparedQA.MeasuredValue = ResourceHelper.GetMessage("String109");// "Différent de Planning Approved";
                        preparedQA.Infobulle = ResourceHelper.GetMessage("String109")+": " + _rcp.protocolName;
                    }



                    this._result.Add(preparedQA);
                }
                #endregion
            }
            /*
            if (_pinfo.actualUserPreference.userWantsTheTest("testNew"))
            {
                #region CECI EST UN NOUVEAU TEST 
                Item_Result leNouveauTest = new Item_Result();
                leNouveauTest.Label = "Titre du test";
                leNouveauTest.ExpectedValue = "1";
                leNouveauTest.MeasuredValue = "2";
                leNouveauTest.Infobulle = "ceci permet de vérifier blabla ";

                if (leNouveauTest.ExpectedValue == leNouveauTest.MeasuredValue)
                    leNouveauTest.setToTRUE();
                else
                    leNouveauTest.setToWARNING();

              
                this._result.Add(leNouveauTest);

                #endregion
            }
            */
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