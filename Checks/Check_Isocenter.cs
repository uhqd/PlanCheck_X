using System;
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
    internal class Check_Isocenter
    {
        private ScriptContext _ctx;
        private PreliminaryInformation _pinfo;
        private read_check_protocol _rcp;
        public Check_Isocenter(PreliminaryInformation pinfo, ScriptContext ctx, read_check_protocol rcp)  //Constructor
        {
            _ctx = ctx;
            _pinfo = pinfo;
            _rcp = rcp;

            Check();
        }



        private List<Item_Result> _result = new List<Item_Result>();
        // private PreliminaryInformation _pinfo;
        private string _title = "Isocentre";

        public void Check()
        {

            int numberOfIso = 0;
            double myx = 999999.0;
            double myy = 999999.0;
            double myz = 999999.0;

            foreach (Beam b in _ctx.PlanSetup.Beams)
            {
                if ((myx != b.IsocenterPosition.x) || (myy != b.IsocenterPosition.y) || (myz != b.IsocenterPosition.z))
                {
                    myx = b.IsocenterPosition.x;
                    myy = b.IsocenterPosition.y;
                    myz = b.IsocenterPosition.z;
                    numberOfIso++;
                }
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("allFieldsSameIso"))
            {
                #region Tous les champs ont le même iso
                if (!_pinfo.isTOMO)
                {
                    Item_Result allFieldsSameIso = new Item_Result();

                    allFieldsSameIso.Label =ResourceHelper.GetMessage("String111");
                    allFieldsSameIso.ExpectedValue = "1";



                    if (numberOfIso > 1)
                    {
                        allFieldsSameIso.setToFALSE();
                        allFieldsSameIso.MeasuredValue = ResourceHelper.GetMessage("String112");
                    }
                    else
                    {
                        allFieldsSameIso.setToTRUE();
                        allFieldsSameIso.MeasuredValue = ResourceHelper.GetMessage("String113");
                    }

                    allFieldsSameIso.Infobulle = ResourceHelper.GetMessage("String114");
                    this._result.Add(allFieldsSameIso);
                }
                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("isoAtCenterOfPTV"))
            {
                #region Iso au centre du PTV

                if (!_pinfo.isTOMO)
                {

                    double tolerance = 0.15; // 0.1 means that we expect the isocenter in a region  from + or -10% around the center of PTV
                    if (_rcp.protocolName.ToUpper().Contains("SEIN"))
                        tolerance = 0.25;
                    Item_Result isoAtCenterOfPTV = new Item_Result();

                    isoAtCenterOfPTV.Label = ResourceHelper.GetMessage("String115");
                    isoAtCenterOfPTV.ExpectedValue = "1";
                    isoAtCenterOfPTV.setToTRUE();

                    Structure ptvTarget = null;// = new Structure;

                    // GET THE GREATESET PTV
                    double volmax = 0.0;
                    foreach (Structure s in _ctx.StructureSet.Structures)
                    {
                        //if (s.Id == _ctx.PlanSetup.TargetVolumeID)
                        if ((!s.Id.ToUpper().Contains("-PTV")) && (!s.Id.ToUpper().Contains("(PTV")))   // avoid struct-PTV  and struct-(PTV)
                            if (s.Id.ToUpper().Contains("PTV"))
                            {
                                if (s.Volume > volmax)
                                {
                                    volmax = s.Volume;
                                    ptvTarget = s;
                                }
                            }
                    }

                    bool doit = false;
                    if (ptvTarget != null)
                        if (!ptvTarget.IsEmpty)
                            doit = true;



                    if (doit)
                    {
                        // looking if isocenter is close to the ptv center
                        // Coordinates are in DICOM ref 



                        double centerPTVxmin = ptvTarget.MeshGeometry.Bounds.X + (0.5 - tolerance) * (ptvTarget.MeshGeometry.Bounds.SizeX);
                        double centerPTVymin = ptvTarget.MeshGeometry.Bounds.Y + (0.5 - tolerance) * (ptvTarget.MeshGeometry.Bounds.SizeY);
                        double centerPTVzmin = ptvTarget.MeshGeometry.Bounds.Z + (0.5 - tolerance) * (ptvTarget.MeshGeometry.Bounds.SizeZ);

                        double centerPTVxmax = ptvTarget.MeshGeometry.Bounds.X + (0.5 + tolerance) * (ptvTarget.MeshGeometry.Bounds.SizeX);
                        double centerPTVymax = ptvTarget.MeshGeometry.Bounds.Y + (0.5 + tolerance) * (ptvTarget.MeshGeometry.Bounds.SizeY);
                        double centerPTVzmax = ptvTarget.MeshGeometry.Bounds.Z + (0.5 + tolerance) * (ptvTarget.MeshGeometry.Bounds.SizeZ);

                        double fractionX = (myx - ptvTarget.MeshGeometry.Bounds.X) / ptvTarget.MeshGeometry.Bounds.SizeX;
                        double fractionY = (myy - ptvTarget.MeshGeometry.Bounds.Y) / ptvTarget.MeshGeometry.Bounds.SizeY;
                        double fractionZ = (myz - ptvTarget.MeshGeometry.Bounds.Z) / ptvTarget.MeshGeometry.Bounds.SizeZ;



                        int iswrong = 0;
                        if ((myx > centerPTVxmax) || (myx < centerPTVxmin))
                        {
                            iswrong = 1;
                        }
                        if ((myy > centerPTVymax) || (myy < centerPTVymin))
                        {
                            iswrong = 1;
                        }
                        if ((myz > centerPTVzmax) || (myz < centerPTVzmin))
                        {
                            iswrong = 1;
                        }
                        if (iswrong == 1)
                        {
                            isoAtCenterOfPTV.MeasuredValue = " " + ResourceHelper.GetMessage("String116") + " " + ptvTarget.Id;
                            isoAtCenterOfPTV.setToINFO();
                        }
                        else
                        {
                            isoAtCenterOfPTV.MeasuredValue = " " + ResourceHelper.GetMessage("String117") + " " + ptvTarget.Id;
                            isoAtCenterOfPTV.setToTRUE();
                        }

                        double tolmin = 0.5 - tolerance;
                        double tolmax = 0.5 + tolerance;
                        isoAtCenterOfPTV.Infobulle = ResourceHelper.GetMessage("String118") + " " + ptvTarget.Id;
                        isoAtCenterOfPTV.Infobulle += "\n" + ResourceHelper.GetMessage("String119");
                        isoAtCenterOfPTV.Infobulle += "\n" + ResourceHelper.GetMessage("String120") + " " + (tolerance * 100).ToString("N1") + ResourceHelper.GetMessage("String121");
                        isoAtCenterOfPTV.Infobulle += "\n\n" + ResourceHelper.GetMessage("String122") + "\n" + Math.Round(fractionX, 2) + "\t" + Math.Round(fractionY, 2) + "\t" + Math.Round(fractionZ, 2);
                        isoAtCenterOfPTV.Infobulle += "\n\n" + ResourceHelper.GetMessage("String113");
                        isoAtCenterOfPTV.Infobulle += "\n" + ResourceHelper.GetMessage("String124") + " " + tolmin + " " + ResourceHelper.GetMessage("String125") + " " + tolmax;
                    }
                    else
                    {
                        isoAtCenterOfPTV.MeasuredValue = ResourceHelper.GetMessage("String126");
                        isoAtCenterOfPTV.setToINFO();
                        isoAtCenterOfPTV.Infobulle += ResourceHelper.GetMessage("String127");

                    }


                    this._result.Add(isoAtCenterOfPTV);
                }

                #endregion
            }

            if (_pinfo.actualUserPreference.userWantsTheTest("distanceToOrigin"))
            {
                #region Distance à l'origine en z
                if (!_pinfo.isTOMO)
                {
                    Item_Result distanceToOrigin = new Item_Result();

                    //double maxDistanceX = 15.0;
                    //double maxDistanceY = 15.0;
                    double maxDistanceZ = 20.0;
                    distanceToOrigin.Label = ResourceHelper.GetMessage("String128");
                    distanceToOrigin.ExpectedValue = "1";
                    //double distanceX = (myx - _ctx.Image.UserOrigin.x) / 10.0;
                    //double distanceY = (myy - _ctx.Image.UserOrigin.y) / 10.0;
                    double distanceZ = (myz - _ctx.Image.UserOrigin.z) / 10.0;

                    //                if ((distanceX > maxDistanceX) || (distanceX < -maxDistanceX) || (distanceZ > maxDistanceZ) || (distanceZ < -maxDistanceZ))
                    if ((distanceZ > maxDistanceZ) || (distanceZ < -maxDistanceZ))
                    {
                        if (_pinfo.isHALCYON)
                            distanceToOrigin.setToFALSE();
                        else
                            distanceToOrigin.setToWARNING();
                    }
                    else
                        distanceToOrigin.setToTRUE();

                    //             distanceToOrigin.MeasuredValue = distanceX.ToString("0.##") + " cm (x) / " + distanceZ.ToString("0.##") + " cm (z)";
                    distanceToOrigin.MeasuredValue = distanceZ.ToString("0.##") + " cm (z)";
                    // distanceToOrigin.Infobulle = "L'isocentre doit être à < " + maxDistanceX + " cm  (en x) et < " + maxDistanceZ + " cm (en z) de l'origine";
                    distanceToOrigin.Infobulle = ResourceHelper.GetMessage("String129") + " " + maxDistanceZ + " "+ ResourceHelper.GetMessage("String130");
                    distanceToOrigin.Infobulle += "\n "+ ResourceHelper.GetMessage("String131");

                    this._result.Add(distanceToOrigin);
                }
                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("isoTomo"))
            {
                #region position iso tomo
                // impossible to get green laser position in the pdf report
                // there is the red laser, the dose max position (ref point)
                // and the origin of dicom image

                if (_pinfo.isTOMO)
                {
                    Item_Result isoTomo = new Item_Result();

                    isoTomo.Label = "Red laser Tomotherapy";
                    isoTomo.ExpectedValue = "1";
                    if (_pinfo.planReportIsFound)
                    {
                        isoTomo.MeasuredValue = _pinfo.tprd.Trd.redLaserXoffset + " " + _pinfo.tprd.Trd.redLaserYoffset + " " + _pinfo.tprd.Trd.redLaserZoffset + " mm";
                        isoTomo.Infobulle = "z < 160 mm";
                        if (_pinfo.tprd.Trd.redLaserZoffset < 160)
                            isoTomo.setToTRUE();
                        else
                            isoTomo.setToFALSE();
                    }
                    else
                    {
                        isoTomo.MeasuredValue = ResourceHelper.GetMessage("String132");
                        isoTomo.setToUNCHECK();
                    }
                    this._result.Add(isoTomo);
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
