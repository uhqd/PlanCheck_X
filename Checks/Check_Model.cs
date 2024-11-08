﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Runtime.CompilerServices;
using System.Reflection;
using PlanCheck;
using PlanCheck.Users;
using System.Threading.Tasks;
using System.Runtime.Remoting.Contexts;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows.Navigation;
using System.Drawing;
using PlanCheck.Languages;



namespace PlanCheck
{
    internal class Check_Model
    {


        private List<Item_Result> _result = new List<Item_Result>();
        private PreliminaryInformation _pinfo;
        private ScriptContext _pcontext;
        private string _title = ResourceHelper.GetMessage("String237");
        private read_check_protocol _rcp;

        public Check_Model(PreliminaryInformation pinfo, ScriptContext context, read_check_protocol rcp)  //Constructor
        {
            // _testpartlabel = "Algorithme";
            _rcp = rcp;
            _pinfo = pinfo;
            _pcontext = context;
            Check();
        }

        public bool CompareNTO(OptimizationNormalTissueParameter planNTO, NTO protocolNTO)
        {

            //if (protocolNTO == null) MessageBox.Show("proto null");
            //if (planNTO == null) MessageBox.Show("planNTO null");
            bool result = true;



            if (planNTO.DistanceFromTargetBorderInMM != protocolNTO.distanceTotTarget) result = false;
            if (planNTO.StartDosePercentage != protocolNTO.startPercentageDose) result = false;
            if (planNTO.EndDosePercentage != protocolNTO.stopPercentageDose) result = false;
            if (planNTO.FallOff != protocolNTO.theFalloff) result = false;
            if (planNTO.Priority != protocolNTO.priority) result = false;

            bool autoMode = false;
            if (protocolNTO.mode == "Manual") autoMode = false;
            else if (protocolNTO.mode == "Auto") autoMode = true;

            //MessageBox.Show("automode:     " + planNTO.IsAutomatic.ToString());
            if (planNTO.IsAutomatic != autoMode) result = false;

            return result;



        }
        private bool isField3x3() // check if a least a CP has jaw aperture < 31x31 mm
        {
            bool itis = false;
            //Beam b = _pcontext.PlanSetup.Beams.FirstOrDefault(x => x.IsSetupField == false);
            foreach (Beam b in _pcontext.PlanSetup.Beams)
            {
                if (!b.IsSetupField)
                {

                    // must check if it works for rtc
                    foreach (ControlPoint cp in b.ControlPoints)
                    {
                        if ((-cp.JawPositions.X1 + cp.JawPositions.X2 < 31) || (-cp.JawPositions.Y1 + cp.JawPositions.Y2 < 31))
                        {
                            // MessageBox.Show(cp.JawPositions.X1.ToString() + " " + cp.JawPositions.X2.ToString() + " " + cp.JawPositions.Y1 + " " + cp.JawPositions.Y2);
                            itis = true;
                            break;
                        }
                    }

                }
                if (itis)
                    break;

            }

            return itis;

        }

        //test
        public void Check()
        {




            Comparator testing = new Comparator();
            String algoNameStatus = String.Empty;
            if (_pinfo.actualUserPreference.userWantsTheTest("algo_name"))
            {
                #region Nom de l'algo
                Item_Result algo_name = new Item_Result();
                algo_name.Label = ResourceHelper.GetMessage("String207");
                if (!_pinfo.isTOMO)
                {



                    algo_name.ExpectedValue = _rcp.algoName;
                    algo_name.MeasuredValue = _pinfo.AlgoName;
                    algo_name.Comparator = "=";
                    algo_name.Infobulle = ResourceHelper.GetMessage("String208") + " " + _rcp.protocolName + " : " + algo_name.ExpectedValue;
                    algo_name.Infobulle += "\n" + ResourceHelper.GetMessage("String209");
                    algo_name.ResultStatus = testing.CompareDatas(algo_name.ExpectedValue, algo_name.MeasuredValue, algo_name.Comparator);

                }
                else
                {
                    if (_pinfo.planReportIsFound)
                    {
                        string tomoAlgo = _pinfo.tprd.Trd.algorithm;
                        string planningMethod = _pinfo.tprd.Trd.planningMethod;
                        algo_name.MeasuredValue = tomoAlgo + ":" + planningMethod;

                        if ((tomoAlgo.Contains("Convolution-Superposition")) && (planningMethod.ToUpper().Contains("ULTRA")))  // Change to VOLO Ultra
                            algo_name.setToTRUE();
                        else
                            algo_name.setToFALSE();
                        algo_name.Infobulle = ResourceHelper.GetMessage("String210")+" ";


                    }
                    else
                    {
                        algo_name.setToINFO();
                        algo_name.MeasuredValue = ResourceHelper.GetMessage("String191");

                    }

                }
                this._result.Add(algo_name);
                algoNameStatus = algo_name.ResultStatus.Item1; // for the following tests
                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("algo_grid"))
            {
                #region Grille de resolution
                Item_Result algo_grid = new Item_Result();
                algo_grid.Label = ResourceHelper.GetMessage("String211");
                algo_grid.ExpectedValue = _rcp.gridSize.ToString();//"1.25";// TO GET IN PRTOCOLE
                algo_grid.MeasuredValue = _pcontext.PlanSetup.Dose.XRes.ToString("0.00");
                //algo_grid.Comparator = "=";
                algo_grid.Infobulle = ResourceHelper.GetMessage("String212") + " " + _rcp.protocolName + " " + algo_grid.ExpectedValue + " mm";

                //algo_grid.ResultStatus = testing.CompareDatas(algo_grid.ExpectedValue, algo_grid.MeasuredValue, algo_grid.Comparator);
                if (_rcp.gridSize == _pcontext.PlanSetup.Dose.XRes)
                {
                    algo_grid.setToTRUE();
                }
                else
                {
                    algo_grid.setToFALSE();
                }
                if (_pinfo.isTOMO)
                {
                    algo_grid.Infobulle = ResourceHelper.GetMessage("String213") +"\n"+ ResourceHelper.GetMessage("String238");
                    if (_pcontext.PlanSetup.Dose.XRes - 1.2695 < 0.01)
                    {
                        algo_grid.setToTRUE();
                    }
                    else
                    {
                        algo_grid.setToFALSE();
                    }
                    if (_pinfo.planReportIsFound)
                    {
                        if (!_pinfo.tprd.Trd.resolutionCalculation.ToLower().Contains("high"))
                            algo_grid.setToFALSE();

                    }
                    else
                        algo_grid.Infobulle = ResourceHelper.GetMessage("String214");

                }

                this._result.Add(algo_grid);


                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("algoOptions"))
            {
                #region LES OPTIONS DE CALCUL
                if (!_pinfo.isTOMO)
                {


                    // ------------------
                    // uncomment to display algoOptions !
                    /*
                     * String msg = null;
                                    Dictionary<String, String> map = new Dictionary<String, String>();
                                    map = _pcontext.PlanSetup.PhotonCalculationOptions;
                                    foreach (String s in map.Keys)
                                        msg += s + "\n";
                                    msg += "\n";
                                    foreach (String s in map.Values)
                                        msg += s + "\n";
                                    MessageBox.Show(msg);
                    */
                    // ----------------------

                    if (algoNameStatus != "X")// algoOptions are not checked if the algo is not the same
                    {
                        Item_Result algoOptions = new Item_Result();
                        algoOptions.Label = ResourceHelper.GetMessage("String215");

                        int optionsAreOK = 1;
                        int myOpt = 0;



                        Dictionary<String, String> map = new Dictionary<String, String>();
                        map = _pcontext.PlanSetup.PhotonCalculationOptions;

                        foreach (KeyValuePair<String, String> kvp in map)
                        {

                            //                            MessageBox.Show(_rcp.optionComp[myOpt] + " ?? ");

                            if (kvp.Value != _rcp.optionComp[myOpt]) // if one computation option is different test is error
                            {


                                algoOptions.Infobulle += "\n" + ResourceHelper.GetMessage("String216") + ": " + kvp.Key + "\n ";
                                algoOptions.Infobulle +=  ResourceHelper.GetMessage("String217") + ": " + _rcp.optionComp[myOpt] + "\n " + ResourceHelper.GetMessage("String218") + ": " + kvp.Value;
                                algoOptions.MeasuredValue = ResourceHelper.GetMessage("String219");
                                optionsAreOK = 0;
                            }
                            myOpt++;
                        }


                        if (optionsAreOK == 0)
                        {
                            algoOptions.setToFALSE();
                        }
                        else
                        {
                            algoOptions.setToTRUE();
                            algoOptions.MeasuredValue = "OK";
                            algoOptions.Infobulle = ResourceHelper.GetMessage("String235") + " " + myOpt + " " + ResourceHelper.GetMessage("String217") + ": " + _rcp.protocolName + "\n";
                            foreach (KeyValuePair<String, String> kvp in map)
                                algoOptions.Infobulle += " - " + kvp.Key + " : " + kvp.Value + "\n";

                        }

                        this._result.Add(algoOptions);
                    }
                }
                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("NTO"))
            {
                #region NTO


                if ((!_pinfo.isTOMO) && (!_pinfo.isHyperArc))
                {
                    if (_pcontext.PlanSetup.OptimizationSetup.Parameters.Count() > 0) // if there is an optim. pararam
                    {
                        //foreach (OptimizationObjective oo in _ctx.PlanSetup.OptimizationSetup.Objectives)
                        // foreach (OptimizationParameter op in _ctx.PlanSetup.OptimizationSetup.Parameters)




                        OptimizationNormalTissueParameter ontp = _pcontext.PlanSetup.OptimizationSetup.Parameters.FirstOrDefault(x => x.GetType().Name == "OptimizationNormalTissueParameter") as OptimizationNormalTissueParameter;

                        // OptimizationNormalTissueParameter ontp = op as OptimizationNormalTissueParameter;
                        bool noNTOuse = false;
                        bool NTOparamsOk = false;
                        if (ontp != null)
                            NTOparamsOk = CompareNTO(ontp, _rcp.NTOparams);
                        else
                            noNTOuse = true;

                        Item_Result NTO = new Item_Result();
                        NTO.Label = "NTO";
                        if (noNTOuse)
                        {
                            NTO.MeasuredValue = ResourceHelper.GetMessage("String221");
                            NTO.Infobulle = ResourceHelper.GetMessage("String222");
                            NTO.setToWARNING();
                        }
                        else if (NTOparamsOk)
                        {
                            NTO.MeasuredValue = ResourceHelper.GetMessage("String223");
                            NTO.Infobulle = ResourceHelper.GetMessage("String224") + " " + _rcp.protocolName;
                            NTO.setToTRUE();
                        }
                        else
                        {
                            NTO.MeasuredValue = ResourceHelper.GetMessage("String225");
                            NTO.Infobulle = ResourceHelper.GetMessage("String226") + " " + _rcp.protocolName;
                            NTO.setToFALSE();
                        }


                        NTO.Infobulle += "\n " + ResourceHelper.GetMessage("String227") + " :";
                        if (noNTOuse)
                        {
                            NTO.Infobulle += "\n " + ResourceHelper.GetMessage("String228");
                        }
                        else
                        {
                            NTO.Infobulle += "\n Distance : " + ontp.DistanceFromTargetBorderInMM + " vs. " + _rcp.NTOparams.distanceTotTarget;
                            NTO.Infobulle += "\n Fall off : " + ontp.FallOff + " vs. " + _rcp.NTOparams.theFalloff;
                            NTO.Infobulle += "\n Start Dose : " + ontp.StartDosePercentage + " vs. " + _rcp.NTOparams.startPercentageDose;
                            NTO.Infobulle += "\n End Dose : " + ontp.EndDosePercentage + " vs. " + _rcp.NTOparams.stopPercentageDose;
                            NTO.Infobulle += "\n Priority : " + ontp.Priority + " vs. " + _rcp.NTOparams.priority;
                            NTO.Infobulle += "\n Auto Mode : " + ontp.IsAutomatic;// + " vs. " +
                            if (ontp.IsAutomatic)
                                NTO.Infobulle += " (Auto) vs. ";
                            else
                                NTO.Infobulle += " (Manual) vs. ";
                            NTO.Infobulle += _rcp.NTOparams.mode;
                        }
                        /*
                        NTO.Infobulle += "\n Paramètres NTO du protocole :";
                        NTO.Infobulle += "\n Distance : " + _rcp.NTOparams.distanceTotTarget;
                        NTO.Infobulle += "\n Fall off : " + _rcp.NTOparams.theFalloff;
                        NTO.Infobulle += "\n Start Dose : " + _rcp.NTOparams.startPercentageDose;
                        NTO.Infobulle += "\n End Dose : " + _rcp.NTOparams.stopPercentageDose;
                        NTO.Infobulle += "\n Priority : " + _rcp.NTOparams.priority;
                        NTO.Infobulle += "\n Auto Mode : " + _rcp.NTOparams.mode;
                        */
                        this._result.Add(NTO);

                        //OptimizationIMRTBeamParameter oibp = _pcontext.PlanSetup.OptimizationSetup.Parameters.FirstOrDefault(x => x.GetType().Name == "OptimizationIMRTBeamParameter") as OptimizationIMRTBeamParameter;
                        /*
                        OptimizationExcludeStructureParameter oesp = op as OptimizationExcludeStructureParameter;
                        OptimizationIMRTBeamParameter oibp = op as OptimizationIMRTBeamParameter;                
                        OptimizationPointCloudParameter opcp = op as OptimizationPointCloudParameter;

                        */


                    }
                }


                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("jawTrack"))
            {
                #region Jaw tracking
                //  This method doesnt work:
                //  OptimizationJawTrackingUsedParameter ojtup = op as OptimizationJawTrackingUsedParameter;
                //  (found on the reddit )
                // check only for nova

                // en fait c'est actif systemetiquement au nova. pas fait a l'halcyon 
                // sauf toute petite lésion : 3x3

                if ((_pinfo.isNOVA) && (!_pinfo.isHyperArc))
                {
                    if (_pcontext.PlanSetup.OptimizationSetup.Parameters.Count() > 0) // if there is an optim. pararam
                    {
                        Item_Result jawTrack = new Item_Result();
                        jawTrack.Label = ResourceHelper.GetMessage("String229");
                        //OptimizationJawTrackingUsedParameter ojtup = _ctx.PlanSetup.OptimizationSetup.Parameters.FirstOrDefault(x => x.GetType().Name == "OptimizationJawTrackingUsedParameter") as OptimizationJawTrackingUsedParameter;
                        // jawTrack.Infobulle = "Selon le protocole " + _rcp.protocolName + " le jaw tracking doit être " + _rcp.JawTracking;

                        bool isJawTrackingOn = _pcontext.PlanSetup.OptimizationSetup.Parameters.Any(x => x is OptimizationJawTrackingUsedParameter);
                        jawTrack.MeasuredValue = isJawTrackingOn.ToString();

                        if (!isJawTrackingOn)
                        {
                            /*if (isField3x3())
                            {
                                jawTrack.setToTRUE();
                                jawTrack.Infobulle += "\nJaw Track désactivé car jaws < 3.1 cm";
                            }
                            else
                            {*/
                            jawTrack.setToFALSE();
                            jawTrack.Infobulle += "\n" + ResourceHelper.GetMessage("String230");

                        }
                        else if (isJawTrackingOn) // != _rcp.JawTracking)
                        {
                            jawTrack.setToTRUE();
                            /*                        if (isField3x3())
                                                    {
                                                        jawTrack.setToFALSE();
                                                        jawTrack.Infobulle += "\nJawTrack activé mais jaws < 3.1";
                                                    }
                                                    else
                                                    {*/
                            jawTrack.Infobulle += "\n" + ResourceHelper.GetMessage("String231");

                        }
                        this._result.Add(jawTrack);
                    }
                }
                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("POoptions"))
            {
                #region LES OPTIONS DU PO
                if (!_pinfo.isTOMO)
                {
                    if (algoNameStatus != "X")// algoOptions are not checked if the algo is not the same
                    {
                        Item_Result POoptions = new Item_Result();
                        POoptions.Label = ResourceHelper.GetMessage("String232");

                        POoptions.ExpectedValue = "N/A";// TO GET IN PRTOCOLE




                        int myOpt = 0;

                        bool optionsPOareOK = true;
                        foreach (string s in _rcp.POoptions)
                        {
                            //                    MessageBox.Show("Comp " + s + " vs. " + _pinfo.POoptions[myOpt]);
                            if (s != _pinfo.POoptions[myOpt])
                                optionsPOareOK = false;
                            myOpt++;
                        }

                        if (!optionsPOareOK)
                        {
                            POoptions.setToWARNING();
                            POoptions.MeasuredValue = ResourceHelper.GetMessage("String233");
                            POoptions.Infobulle = ResourceHelper.GetMessage("String234") + " " + _rcp.protocolName + "\n";
                            myOpt = 0;
                            foreach (string s in _rcp.POoptions)
                            {
                                POoptions.Infobulle += s + " vs. " + _pinfo.POoptions[myOpt] + "\n";
                                myOpt++;
                            }
                        }
                        else
                        {
                            POoptions.setToTRUE();
                            POoptions.Infobulle = "Les"+" " + myOpt + " "+ResourceHelper.GetMessage("String236")+": " + _rcp.protocolName;
                            POoptions.MeasuredValue = "OK";

                        }

                        this._result.Add(POoptions);
                    }
                }
                #endregion
            }
        }



        //_pcontext.PlanSetup.PhotonCalculationOptions
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
