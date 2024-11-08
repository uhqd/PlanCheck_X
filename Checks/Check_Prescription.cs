using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows;
using System.Windows.Navigation;
using PlanCheck.Languages;



namespace PlanCheck
{
    internal class Check_Prescription
    {
        private ScriptContext _ctx;
        private PreliminaryInformation _pinfo;
        private read_check_protocol _rcp;
        public Check_Prescription(PreliminaryInformation pinfo, ScriptContext ctx, read_check_protocol rcp)  //Constructor
        {
            _ctx = ctx;
            _pinfo = pinfo;
            _rcp = rcp;
            Check();

        }

        private List<Item_Result> _result = new List<Item_Result>();
        // private PreliminaryInformation _pinfo;
        private string _title = "Prescription";

        public void Check()
        {


            #region LISTE DES CIBLES DE LA PRESCRIPTION
            if (_pinfo.actualUserPreference.userWantsTheTest("prescriptionVolumes"))
            {
                Item_Result prescriptionVolumes = new Item_Result();
                if (_ctx.PlanSetup.RTPrescription.Status == "Approved")
                {
                    prescriptionVolumes.MeasuredValue = ResourceHelper.GetMessage("String150") + ": ";
                    prescriptionVolumes.setToTRUE();
                }
                else
                {
                    prescriptionVolumes.MeasuredValue = ResourceHelper.GetMessage("String151") + ": ";

                    //                    prescriptionVolumes.Label = " Prescription non approuvée (" + targetNumber + " cible(s))";
                    prescriptionVolumes.setToFALSE();
                }
                int targetNumber = 0;
                //prescriptionVolumes.MeasuredValue = "";
                prescriptionVolumes.Infobulle = ResourceHelper.GetMessage("String152") + "\n";
                foreach (var target in _ctx.PlanSetup.RTPrescription.Targets) //boucle sur les différents niveaux de dose de la prescription
                {
                    targetNumber++;
                    double tot = target.NumberOfFractions * target.DosePerFraction.Dose;
                    prescriptionVolumes.Infobulle += target.TargetId + " : " + target.NumberOfFractions + " x " + target.DosePerFraction.Dose.ToString("N2") + " Gy " + "(" + tot.ToString("N2") + " Gy)\n";
                    prescriptionVolumes.MeasuredValue += target.TargetId + " (" + tot.ToString("N2") + " Gy)  ";
                }

                prescriptionVolumes.ExpectedValue = "info";
                prescriptionVolumes.Label = " " + ResourceHelper.GetMessage("String153") + " " + targetNumber + " " + ResourceHelper.GetMessage("String154") + " : ";


                this._result.Add(prescriptionVolumes);
            }
            #endregion

            #region FRACTIONNEMENT - CIBLE LA PLUS HAUTE
            if (_pinfo.actualUserPreference.userWantsTheTest("fractionation"))
            {
                Item_Result fractionation = new Item_Result();
                //fractionation.Label = "Fractionnement du PTV principal";
                int nPrescribedNFractions = 0;
                double nPrescribedDosePerFraction = 0;
                string PrescriptionName = null;
                double PrescriptionValue = 0;

                fractionation.ExpectedValue = nPrescribedNFractions + " x " + nPrescribedDosePerFraction.ToString("N2") + " Gy";
                double diffDose = 0.0;
                double myDosePerFraction = 0.0;
                int nFraction = 0;
                foreach (var target in _ctx.PlanSetup.RTPrescription.Targets) //boucle sur les différents niveaux de dose de la prescription
                {


                    nPrescribedNFractions = target.NumberOfFractions;
                    if (target.DosePerFraction.Dose > nPrescribedDosePerFraction)  // get the highest dose per fraction level
                    {
                        nPrescribedDosePerFraction = target.DosePerFraction.Dose;
                        PrescriptionValue = target.Value;
                        PrescriptionName = target.TargetId;
                    }
                }
                if (!_pinfo.isTOMO)
                {
                    myDosePerFraction = _ctx.PlanSetup.DosePerFraction.Dose;
                    nFraction = (int)_ctx.PlanSetup.NumberOfFractions;
                }
                else //is tomo
                {
                    if (_pinfo.planReportIsFound)
                    {
                        myDosePerFraction = _pinfo.tprd.Trd.prescriptionDosePerFraction;
                        nFraction = _pinfo.tprd.Trd.prescriptionNumberOfFraction;
                        fractionation.Infobulle = ResourceHelper.GetMessage("String155") + " : " + _pinfo.tprd.Trd.planName;
                    }
                }
                if (((_pinfo.isTOMO) && (_pinfo.planReportIsFound)) || (!_pinfo.isTOMO))
                {


                    diffDose = Math.Abs(nPrescribedDosePerFraction - myDosePerFraction);
                    fractionation.MeasuredValue = "Plan : " + nFraction + " x " + myDosePerFraction.ToString("0.00") + " " + ResourceHelper.GetMessage("String156") + " : " + nPrescribedNFractions + " x " + nPrescribedDosePerFraction.ToString("0.00") + " Gy";
                    if ((nPrescribedNFractions == nFraction) && (diffDose < 0.005))
                        fractionation.setToTRUE();
                    else
                        fractionation.setToFALSE();

                    fractionation.Infobulle += "\n\n" + ResourceHelper.GetMessage("String157") + "\n" + ResourceHelper.GetMessage("String158") + " (" + _ctx.PlanSetup.RTPrescription.Id +
                        ") : " + nPrescribedNFractions.ToString() + " x " + nPrescribedDosePerFraction.ToString("N2") + " Gy.";

                }
                else
                {
                    fractionation.setToINFO();
                    fractionation.MeasuredValue = ResourceHelper.GetMessage("String159");
                    fractionation.Infobulle = ResourceHelper.GetMessage("String159");
                }

                fractionation.Label = ResourceHelper.GetMessage("String160") + " (" + PrescriptionName + ")";
                this._result.Add(fractionation);
            }
            #endregion

            // pas réussi à attraper le % dans la prescription (que dans le plan)
            #region POURCENTAGE DE LA PRESCRIPTION
            if (_pinfo.actualUserPreference.userWantsTheTest("percentage"))
                if (!_pinfo.isTOMO)
                {
                    Item_Result percentage = new Item_Result();
                    double myTreatPercentage = _ctx.PlanSetup.TreatmentPercentage;
                    myTreatPercentage = 100 * myTreatPercentage;
                    percentage.Label = ResourceHelper.GetMessage("String161");
                    percentage.ExpectedValue = _rcp.prescriptionPercentage;
                    percentage.MeasuredValue = myTreatPercentage.ToString() + "%";
                    if (percentage.ExpectedValue == percentage.MeasuredValue)
                        percentage.setToTRUE();
                    else
                        percentage.setToFALSE();
                    percentage.Infobulle = ResourceHelper.GetMessage("String162");
                    percentage.Infobulle += "\n" + ResourceHelper.GetMessage("String163") + " " + _rcp.protocolName + " (" + _rcp.prescriptionPercentage + ")";
                    this._result.Add(percentage);
                }
            #endregion

            #region NORMALISATION DU PLAN
            if (_pinfo.actualUserPreference.userWantsTheTest("normalisation"))
            {
                Item_Result normalisation = new Item_Result();
                normalisation.Label = ResourceHelper.GetMessage("String164");
                if (!_pinfo.isTOMO)
                {

                    //string normMethod = _ctx.PlanSetup.PlanNormalizationMethod;
                    normalisation.ExpectedValue = _rcp.normalisationMode;
                    normalisation.MeasuredValue = _ctx.PlanSetup.PlanNormalizationMethod;
                    normalisation.setToINFO();
                    if (normalisation.MeasuredValue.Contains("volume")) // si le mode de normalisation contient le mot volume
                    {
                        if (normalisation.ExpectedValue == normalisation.MeasuredValue)
                            normalisation.setToTRUE();
                        else
                            normalisation.setToFALSE();

                        normalisation.MeasuredValue += ": " + _ctx.PlanSetup.TargetVolumeID; // afficher ce volume

                    }
                    if (normalisation.MeasuredValue.Contains("point"))
                    {
                        if (normalisation.MeasuredValue.Contains(ResourceHelper.GetMessage("String165")))
                        {
                            if (normalisation.ExpectedValue.Contains(ResourceHelper.GetMessage("String165")))
                                normalisation.setToTRUE();
                            else
                                normalisation.setToFALSE();

                            if (normalisation.MeasuredValue.Contains(ResourceHelper.GetMessage("String175")))
                                normalisation.MeasuredValue += " (" + _ctx.PlanSetup.PrimaryReferencePoint.Id + ")";

                        }
                        else
                        {
                            normalisation.setToFALSE();
                        }
                    }

                    if (normalisation.MeasuredValue == ResourceHelper.GetMessage("String166"))
                        normalisation.setToWARNING();




                    normalisation.Infobulle = ResourceHelper.GetMessage("String167");
                    //normalisation.Infobulle += "\nPour la TOMO l'item est mis en INFO";


                }
                else // tomo
                {
                    if (_pinfo.planReportIsFound)
                    {
                        normalisation.MeasuredValue = _pinfo.tprd.Trd.prescriptionMode;
                        normalisation.Infobulle = ResourceHelper.GetMessage("String168");
                        if (_pinfo.tprd.Trd.prescriptionMode.Contains("Median"))
                        {

                            normalisation.setToTRUE();
                        }
                        else
                        {

                            normalisation.setToFALSE();
                        }
                    }
                    else
                    {
                        normalisation.MeasuredValue = ResourceHelper.GetMessage("String169");
                        normalisation.setToWARNING();
                    }
                }


                this._result.Add(normalisation);
            }
            #endregion

            #region NOM DE LA PRESCRIPTION

            if (_pinfo.actualUserPreference.userWantsTheTest("prescriptionName"))
            {
                Item_Result prescriptionName = new Item_Result();
                prescriptionName.Label = ResourceHelper.GetMessage("String170");
                prescriptionName.MeasuredValue = _ctx.PlanSetup.RTPrescription.Id;

                String planName = String.Concat(_ctx.PlanSetup.Id.Where(c => !Char.IsWhiteSpace(c))); // remove spaces
                planName = planName.ToUpper();
                String prescriptionId = String.Concat(_ctx.PlanSetup.RTPrescription.Id.Where(c => !Char.IsWhiteSpace(c)));
                prescriptionId = prescriptionId.ToUpper();
                if (planName == prescriptionId)
                {
                    //prescriptionName.MeasuredValue ="OK";
                    prescriptionName.setToTRUE();
                }
                else
                {
                    prescriptionName.MeasuredValue += " "+ ResourceHelper.GetMessage("String171");
                    prescriptionName.setToINFO();
                }
                prescriptionName.Infobulle = ResourceHelper.GetMessage("String172");
                prescriptionName.Infobulle += "\n"+ ResourceHelper.GetMessage("String173") + "\n";
                if (_ctx.Course.Comment == _ctx.PlanSetup.RTPrescription.Id)
                    prescriptionName.Infobulle += ResourceHelper.GetMessage("String174");
                this._result.Add(prescriptionName);
            }
            #endregion

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
