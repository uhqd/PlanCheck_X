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
    internal class Check_contours
    {
        private ScriptContext _ctx;
        private PreliminaryInformation _pinfo;
        private read_check_protocol _rcp;

        public Check_contours(PreliminaryInformation pinfo, ScriptContext ctx, read_check_protocol rcp)  //Constructor
        {
            _rcp = rcp;
            _ctx = ctx;
            _pinfo = pinfo;
            Check();

        }
        public static int _GetSlice(double z, StructureSet SS)
        {
            var imageRes = SS.Image.ZRes;
            return Convert.ToInt32((z - SS.Image.Origin.z) / imageRes);
        }

        /*
         private Structure isExistAndNotEmpty(String id)
        {

            bool isok = false;
            Structure s = _ctx.StructureSet.Structures.FirstOrDefault(x => x.Id.ToUpper() == id.ToUpper());
            if (s != null)
                if (!s.IsEmpty)
                    isok = true;

            if (isok)
                return s;
            else
                return null;

        }
        */
        private double volumeMin(String volumeName, double volumeValue, String sex)
        {
            double result = -1.0;
            if (sex == "Female")
            {
                foreach (OARvolume oar in _pinfo.womanOARVolumes)
                {
                    if (volumeName == oar.volumeName)
                    {
                        result = oar.volumeMin;
                    }

                }
            }
            else // is male
            {
                foreach (OARvolume oar in _pinfo.manOARVolumes)
                {
                    if (volumeName == oar.volumeName)
                    {
                        result = oar.volumeMin;
                    }

                }

            }
            return result;
        }
        private double volumeMax(String volumeName, double volumeValue, String sex)
        {
            double result = -1.0;
            if (sex == "Female")
            {
                foreach (OARvolume oar in _pinfo.womanOARVolumes)
                {
                    if (volumeName == oar.volumeName)
                    {
                        result = oar.volumeMax;
                    }

                }
            }
            else // is male
            {
                foreach (OARvolume oar in _pinfo.manOARVolumes)
                {
                    if (volumeName == oar.volumeName)
                    {
                        result = oar.volumeMax;
                    }

                }

            }
            return result;
        }
        private bool nPartsIsOk(String volumeName, int nParts, String sex)
        {
            bool result = true;

            if (sex == "Female")
            {
                foreach (OARvolume oar in _pinfo.womanOARVolumes)
                {
                    if (volumeName.ToUpper() == oar.volumeName.ToUpper())
                    {
                        if (oar.nExpectedPart == -1) // no expected number of parts is specified
                            result = true;
                        else if (nParts == oar.nExpectedPart)
                            result = true;
                        else
                            result = false;
                    }
                }
            }
            else // is male
            {
                foreach (OARvolume oar in _pinfo.manOARVolumes)
                {
                    if (volumeName.ToUpper() == oar.volumeName.ToUpper())
                    {

                        if (oar.nExpectedPart == -1) // no expected number of parts is specified
                            result = true;
                        else if (nParts == oar.nExpectedPart)
                            result = true;
                        else
                            result = false;
                    }

                }

            }


            return result;

        }
        private string getExpectedLaterality(string volumeName, string sex)
        {// L left   R right  N none
            string result = "N";
            if (sex == "Female")
            {
                foreach (OARvolume oar in _pinfo.womanOARVolumes)
                {
                    if (volumeName.ToUpper() == oar.volumeName.ToUpper())
                    {
                        result = oar.laterality;

                    }
                }
            }
            else // is male
            {
                foreach (OARvolume oar in _pinfo.womanOARVolumes)
                {
                    if (volumeName.ToUpper() == oar.volumeName.ToUpper())
                    {
                        result = oar.laterality;

                    }

                }
            }

            return result;

        }
        /*
        public double getXcenter()
        {
            double xCenter = 0.0;

            Structure centralStruct = isExistAndNotEmpty("CHIASMA");


            if (centralStruct == null)
                centralStruct = isExistAndNotEmpty("CANAL MED");

            if (centralStruct == null)
                centralStruct = isExistAndNotEmpty("RECTUM");

            if (centralStruct == null)
                centralStruct = isExistAndNotEmpty("VESSIE");

            if (centralStruct == null)
                centralStruct = isExistAndNotEmpty("CERVEAU");

            if (centralStruct == null)
                centralStruct = isExistAndNotEmpty("TRONC CEREBRAL");

            if (centralStruct == null)
                centralStruct = isExistAndNotEmpty("PROSTATE");

            if (centralStruct == null)
                centralStruct = isExistAndNotEmpty("HYPOPHYSE");

            if (centralStruct == null)
                centralStruct = isExistAndNotEmpty("BODY");

            if (centralStruct == null)
                centralStruct = isExistAndNotEmpty("CONTOUR EXTERNE");

            if (centralStruct != null)
                xCenter = centralStruct.MeshGeometry.Bounds.X + (centralStruct.MeshGeometry.Bounds.SizeX / 2.0);


            return xCenter;
        }
        */
        public static int getNumberOfMissingSlices(Structure S, StructureSet SS)
        {

            int nHoles = 0;
            try
            {
                var mesh = S.MeshGeometry.Bounds;
                int meshLow = _GetSlice(mesh.Z, SS);
                int meshUp = _GetSlice(mesh.Z + mesh.SizeZ, SS);



                for (int i = meshLow; i <= meshUp; i++)
                {
                    VMS.TPS.Common.Model.Types.VVector[][] vvv = S.GetContoursOnImagePlane(i);

                    if (vvv.Length == 0)
                        nHoles++;

                }
            }
            catch
            {
                nHoles = 0;
            }
            return nHoles;
        }

        private List<Item_Result> _result = new List<Item_Result>();

        private string _title = "Contours";

        public void Check()
        {
            var allStructures = _rcp.myClinicalExpectedStructures.Concat(_rcp.myOptExpectedStructures).Concat(_rcp.myCouchExpectedStructures).ToList();
            if (_pinfo.actualUserPreference.userWantsTheTest("approbationStatus"))
            {
                #region APPROVE ?  
                Item_Result approbationStatus = new Item_Result();
                approbationStatus.Label = ResourceHelper.GetMessage("approStructLabel");// "Approbation du groupe de structures";
                approbationStatus.ExpectedValue = "...";
                approbationStatus.Infobulle = ResourceHelper.GetMessage("structMustBeApproved"); //"Les structures doivent être approuvées";
                if (_ctx.StructureSet.Structures.First().ApprovalHistory.First().ApprovalStatus.ToString() == "Approved")
                {
                    approbationStatus.setToTRUE();
                    approbationStatus.MeasuredValue = ResourceHelper.GetMessage("approved"); //"Approuvé";
                }
                else
                {
                    approbationStatus.setToWARNING();
                    approbationStatus.MeasuredValue = ResourceHelper.GetMessage("notApproved"); //""Non Approuvé";
                }



                this._result.Add(approbationStatus);
                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("couchStructExist"))
            {
                #region COUCH STRUCTURES 

                if (!_pinfo.isTOMO)
                {
                    Item_Result couchStructExist = new Item_Result();
                    couchStructExist.Label = ResourceHelper.GetMessage("coucStruct");// "Structures de table";
                    couchStructExist.ExpectedValue = "EN COURS";

                    List<string> missingCouchStructures = new List<string>();
                    List<string> wrongHUCouchStructures = new List<string>();
                    List<string> mandatoryMissingCouchStructures = new List<string>();
                    List<string> overlapStructList = new List<string>();
                    // double tolerancedOV = 4.0; // Tolerance for overlap couch vs. body
                    foreach (expectedStructure es in _rcp.myCouchExpectedStructures) // foreach couch element in the xls protocol file
                    {
                        double mydouble = 0;
                        Structure struct1 = _ctx.StructureSet.Structures.FirstOrDefault(x => x.Id.ToUpper() == es.Name.ToUpper()); // find a structure in ss with the same name
                        if (struct1 == null) // if structure doesnt exist in ss
                        {
                            missingCouchStructures.Add(es.Name);
                            if (es.isMandatory)
                                mandatoryMissingCouchStructures.Add(es.Name);
                        }
                        else if (struct1.IsEmpty) // else if it exists but empty --> same
                        {
                            missingCouchStructures.Add(es.Name);
                            if (es.isMandatory)
                                mandatoryMissingCouchStructures.Add(es.Name);
                        }
                        else // else struct is not empty
                        {
                            if (es.HU != 9999)
                            {
                                struct1.GetAssignedHU(out mydouble);   // check assigned HU


                                if (mydouble != es.HU)
                                    wrongHUCouchStructures.Add(es.Name);
                            }

                            try
                            {
                                Structure body = _ctx.StructureSet.Structures.FirstOrDefault(x => x.DicomType == "EXTERNAL"); // find a structure BODY

                                double yBodyMax = body.MeshGeometry.Bounds.Y + body.MeshGeometry.Bounds.SizeY; // post limit of body
                                double ySetUpMin = struct1.MeshGeometry.Bounds.Y;
                                if ((yBodyMax - 4.0) > ySetUpMin) // overlap suspected 4 mm allowed
                                {
                                    overlapStructList.Add(struct1.Id);
                                }
                                /*else // no overlap suspected
                                {
                                    ;
                                }*/
                            }
                            catch
                            {
                                MessageBox.Show("No Body found for check contours: couch structures item " + es.Name);
                            }
                        }
                    }


                    if ((wrongHUCouchStructures.Count == 0) && (missingCouchStructures.Count == 0))
                    {
                        couchStructExist.setToTRUE();
                        couchStructExist.MeasuredValue = ResourceHelper.GetMessage("presentandcorrHU") + " " + _rcp.myCouchExpectedStructures.Count.ToString() + "/" + _rcp.myCouchExpectedStructures.Count.ToString();
                        couchStructExist.Infobulle = ResourceHelper.GetMessage("expectedStruc") + " " + _rcp.protocolName + " :\n";

                        foreach (expectedStructure es in _rcp.myCouchExpectedStructures) // foreach couch element in the xls protocol file
                        {
                            couchStructExist.Infobulle += " - " + es.Name + "\n";
                        }
                    }
                    else
                    {
                        couchStructExist.setToWARNING();
                        couchStructExist.MeasuredValue = missingCouchStructures.Count + " " + ResourceHelper.GetMessage("absentStruct1");
                        if (missingCouchStructures.Count > 0)
                            couchStructExist.Infobulle = missingCouchStructures.Count + " " + ResourceHelper.GetMessage("absentStruct2") + " " + _rcp.protocolName + " " + ResourceHelper.GetMessage("absentStruct3") + " : \n";
                        foreach (string ms in missingCouchStructures)
                            couchStructExist.Infobulle += " - " + ms + "\n";
                        if (wrongHUCouchStructures.Count > 0)
                            couchStructExist.Infobulle += wrongHUCouchStructures.Count + " " + ResourceHelper.GetMessage("absentStruct4") + "\n";
                        foreach (string ms in wrongHUCouchStructures)
                            couchStructExist.Infobulle += " - " + ms + "\n";

                        if (mandatoryMissingCouchStructures.Count > 0)
                        {
                            couchStructExist.setToFALSE();
                            couchStructExist.Infobulle += "\n" + mandatoryMissingCouchStructures.Count + " " + ResourceHelper.GetMessage("absentStruct5") + " : \n";
                            foreach (string ms in mandatoryMissingCouchStructures)
                                couchStructExist.Infobulle += " - " + ms + "\n";
                        }
                    }

                    this._result.Add(couchStructExist);
                }

                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("correctCouch"))
            {
                #region COUCH STRUCTURES CHECK TABLE IN COMMENT 

                if (!_pinfo.isTOMO)
                {
                    Item_Result correctCouch = new Item_Result();
                    correctCouch.Label = ResourceHelper.GetMessage("correctCouchLabel");//  "Table correcte";
                    correctCouch.ExpectedValue = "EN COURS";

                    Structure s = _ctx.StructureSet.Structures.FirstOrDefault(p => p.Id.Equals("CouchSurface", StringComparison.OrdinalIgnoreCase));

                    if (s != null)
                        if (!s.IsEmpty)
                        {
                            String testingString = s.Comment;
                            correctCouch.MeasuredValue = testingString;
                            if (_pinfo.isHALCYON)
                            {
                                correctCouch.Infobulle = ResourceHelper.GetMessage("couchDetail1") + "\n";
                                correctCouch.Infobulle += ResourceHelper.GetMessage("couchDetail2") + "\n";

                                if (testingString.ToUpper().Contains("HALCYON"))
                                    correctCouch.setToTRUE();
                                else
                                    correctCouch.setToWARNING();
                            }
                            if (_pinfo.isNOVA)
                            {
                                correctCouch.Infobulle = ResourceHelper.GetMessage("couchDetail3") + " \n";// Le commentaire de la structure CouchSurface doit être TABLE EXACT\n";
                                correctCouch.Infobulle += ResourceHelper.GetMessage("couchDetail4") + " \n";// "En principe, il faut la table épaisse pour les plans prostate, rectum, etc.,\n";
                                correctCouch.Infobulle += ResourceHelper.GetMessage("couchDetail5") + " \n";//"la table fine pour la tête...\n";
                                correctCouch.Infobulle += ResourceHelper.GetMessage("couchDetail6") + " \n";//"\nNote : la table Exact moyenne et la table Halcyon sont les mêmes...\n";
                                if (testingString.ToUpper().Contains("EXACT"))
                                    correctCouch.setToTRUE();
                                else
                                    correctCouch.setToWARNING();

                            }

                            this._result.Add(correctCouch);

                        }
                }

                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("clinicalStructuresItem"))
            {
                #region CLINICAL STRUCTURES 

                Item_Result clinicalStructuresItem = new Item_Result();
                clinicalStructuresItem.Label = ResourceHelper.GetMessage("clinStruct1");// "Structures cliniques";
                clinicalStructuresItem.ExpectedValue = "EN COURS";


                List<string> missingClinicalStructures = new List<string>();
                List<string> wrongHUClinicalStructures = new List<string>();
                List<string> mandatoryMissingClinicalStructures = new List<string>();

                foreach (expectedStructure es in _rcp.myClinicalExpectedStructures) // foreach clinical struct in the xls check-protocol file
                {
                    //MessageBox.Show("here is " + es.Name);
                    double mydouble = 0;
                    Structure struct1 = _ctx.StructureSet.Structures.FirstOrDefault(x => x.Id.ToUpper() == es.Name.ToUpper()); // find a structure in ss with the same name
                    if (struct1 == null) // if structure doesnt exist in ss
                    {
                        missingClinicalStructures.Add(es.Name);
                        if (es.isMandatory)
                            mandatoryMissingClinicalStructures.Add(es.Name);
                    }
                    else if (struct1.IsEmpty) // else if it exists but empty --> same
                    {
                        missingClinicalStructures.Add(es.Name);
                        if (es.isMandatory)
                            mandatoryMissingClinicalStructures.Add(es.Name);
                    }
                    else
                    {
                        if (es.HU != 9999) // 9999 if no assigned HU 
                        {
                            struct1.GetAssignedHU(out mydouble);
                            if (mydouble != es.HU)
                                wrongHUClinicalStructures.Add(es.Name);
                        }
                    }
                }

                if ((wrongHUClinicalStructures.Count == 0) && (missingClinicalStructures.Count == 0))
                {
                    clinicalStructuresItem.setToTRUE();
                    clinicalStructuresItem.MeasuredValue = ResourceHelper.GetMessage("clinStruct2") + " " + _rcp.myClinicalExpectedStructures.Count.ToString() + "/" + _rcp.myClinicalExpectedStructures.Count.ToString();
                    clinicalStructuresItem.Infobulle = ResourceHelper.GetMessage("clinStruct3") + " " + _rcp.protocolName + " :\n";
                    foreach (expectedStructure es in _rcp.myClinicalExpectedStructures)
                    {
                        clinicalStructuresItem.Infobulle += " - " + es.Name + "\n";
                    }
                }
                else
                {
                    clinicalStructuresItem.setToINFO(); // just info except if wrong HU --> warrning
                    if (wrongHUClinicalStructures.Count > 0)
                        clinicalStructuresItem.setToWARNING();

                    clinicalStructuresItem.MeasuredValue = missingClinicalStructures.Count + " " + ResourceHelper.GetMessage("clinStruct4");
                    if (missingClinicalStructures.Count > 0)
                        clinicalStructuresItem.Infobulle = missingClinicalStructures.Count + " " + ResourceHelper.GetMessage("clinStruct5") + " " + _rcp.protocolName + " " + ResourceHelper.GetMessage("clinStruct6") + " :\n";
                    foreach (string ms in missingClinicalStructures)
                        clinicalStructuresItem.Infobulle += " - " + ms + "\n";
                    if (wrongHUClinicalStructures.Count > 0)
                        clinicalStructuresItem.Infobulle += wrongHUClinicalStructures.Count + " " + ResourceHelper.GetMessage("clinStruct7") + " :\n";
                    foreach (string ms in wrongHUClinicalStructures)
                        clinicalStructuresItem.Infobulle += " - " + ms + "\n";

                    if (mandatoryMissingClinicalStructures.Count > 0)
                    {
                        clinicalStructuresItem.setToWARNING();
                        clinicalStructuresItem.Infobulle += mandatoryMissingClinicalStructures.Count + " " + ResourceHelper.GetMessage("clinStruct8") + " :\n";
                        foreach (string ms in mandatoryMissingClinicalStructures)
                            clinicalStructuresItem.Infobulle += " - " + ms + "\n";
                    }

                }


                this._result.Add(clinicalStructuresItem);

                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("optStructuresItem"))
            {
                #region OPT STRUCTURES 

                Item_Result optStructuresItem = new Item_Result();
                optStructuresItem.Label = ResourceHelper.GetMessage("optStruct1");// "Structures d'optimisation";
                optStructuresItem.ExpectedValue = "EN COURS";


                List<string> missingOptStructures = new List<string>();
                List<string> wrongHUOptStructures = new List<string>();
                List<string> mandatoryMissingOptStructures = new List<string>();
                foreach (expectedStructure es in _rcp.myOptExpectedStructures)
                {
                    double mydouble = 0;
                    Structure struct1 = _ctx.StructureSet.Structures.FirstOrDefault(x => x.Id.ToUpper() == es.Name.ToUpper()); // find a structure in ss with the same name

                    if (struct1 == null) // if structure doesnt exist in ss
                    {
                        missingOptStructures.Add(es.Name);
                        if (es.isMandatory)
                            mandatoryMissingOptStructures.Add(es.Name);
                    }
                    else if (struct1.IsEmpty) // else if it exists but empty --> same
                    {
                        missingOptStructures.Add(es.Name);

                        if (es.isMandatory)
                            mandatoryMissingOptStructures.Add(es.Name);
                    }
                    else
                    {
                        if (es.HU != 9999) // 9999 if no assigned HU 
                        {
                            struct1.GetAssignedHU(out mydouble);
                            if (mydouble != es.HU)
                                wrongHUOptStructures.Add(es.Name);
                        }
                        //MessageBox.Show("YES we found in ss " + el.Item1 + " " + struct1.Id + " " + mydouble.ToString());
                    }
                }



                if ((wrongHUOptStructures.Count == 0) && (missingOptStructures.Count == 0))
                {
                    optStructuresItem.setToTRUE();
                    optStructuresItem.MeasuredValue = ResourceHelper.GetMessage("clinStruct2") + " " + _rcp.myOptExpectedStructures.Count.ToString() + "/" + _rcp.myOptExpectedStructures.Count.ToString();
                    optStructuresItem.Infobulle = ResourceHelper.GetMessage("clinStruct3") + " " + _rcp.protocolName + " :\n";
                    foreach (expectedStructure es in _rcp.myOptExpectedStructures)
                    {
                        optStructuresItem.Infobulle += " - " + es.Name + "\n";
                    }
                }
                else
                {
                    optStructuresItem.setToINFO();
                    optStructuresItem.MeasuredValue = missingOptStructures.Count + " " + ResourceHelper.GetMessage("clinStruct4");
                    if (missingOptStructures.Count > 0)
                        optStructuresItem.Infobulle = missingOptStructures.Count + " " + ResourceHelper.GetMessage("clinStruct5") + " " + _rcp.protocolName + " " + ResourceHelper.GetMessage("clinStruct6") + " :\n";
                    foreach (string ms in missingOptStructures)
                        optStructuresItem.Infobulle += " - " + ms + "\n";
                    if (wrongHUOptStructures.Count > 0)
                        optStructuresItem.Infobulle += wrongHUOptStructures.Count + " " + ResourceHelper.GetMessage("clinStruct7") + " :\n";
                    foreach (string ms in wrongHUOptStructures)
                        optStructuresItem.Infobulle += " - " + ms + "\n";

                    if (mandatoryMissingOptStructures.Count > 0)
                    {
                        optStructuresItem.setToWARNING();
                        optStructuresItem.Infobulle += mandatoryMissingOptStructures.Count + " " + ResourceHelper.GetMessage("clinStruct8") + " :\n";
                        foreach (string ms in mandatoryMissingOptStructures)
                            optStructuresItem.Infobulle += " - " + ms + "\n";
                    }
                }


                this._result.Add(optStructuresItem);

                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("fixedHUVolumeList"))
            {
                #region  List of structures with assigned  HU

                List<string> fixedHUVolumeList = new List<string>();

                Item_Result fixedHUVolume = new Item_Result();
                fixedHUVolume.Label = ResourceHelper.GetMessage("forcedHUlabel");
                fixedHUVolume.ExpectedValue = "EN COURS";

                double myHU = 0.0;
                bool isAssigned = false;
                foreach (Structure struct1 in _ctx.StructureSet.Structures)
                {
                    if (struct1 != null) // if structure  exist 
                        if (!struct1.IsEmpty) //  and if not empty 
                        {
                            isAssigned = struct1.GetAssignedHU(out myHU); // return TRUE if HU assigned and HU value is sent to mydouble
                            if (isAssigned)
                            {
                                bool isAClinicalStructure = false;
                                bool isACouchStructure = false;
                                bool isAnOptStructure = false;

                                #region check if the structure is not already in the 3 previous check blocks

                                foreach (expectedStructure es in _rcp.myClinicalExpectedStructures)
                                {
                                    if (es.Name == struct1.Id)
                                    {
                                        isAClinicalStructure = true;
                                        //  break;
                                    }
                                }
                                foreach (expectedStructure es in _rcp.myCouchExpectedStructures)
                                {
                                    if (es.Name == struct1.Id)
                                    {
                                        isACouchStructure = true;
                                        // break;
                                    }
                                }
                                foreach (expectedStructure es in _rcp.myOptExpectedStructures)
                                {
                                    if (es.Name == struct1.Id)
                                    {
                                        isAnOptStructure = true;
                                        // break;
                                    }
                                }

                                #endregion

                                if (!isAClinicalStructure && !isACouchStructure && !isAnOptStructure)
                                    fixedHUVolumeList.Add(struct1.Id + " " + myHU.ToString("F2") + " HU");
                            }
                        }
                }
                if (fixedHUVolumeList.Count > 0)
                {
                    fixedHUVolume.Infobulle = ResourceHelper.GetMessage("forcedHU1") + " : \n"; // "Structure avec HU forcées (autres que celles vérifiées dans les tests précédents)"+" : \n";
                    foreach (string ms in fixedHUVolumeList)
                        fixedHUVolume.Infobulle += ms + "\n";
                    fixedHUVolume.MeasuredValue = fixedHUVolumeList.Count.ToString() + " " + ResourceHelper.GetMessage("forcedHU2");
                    fixedHUVolume.setToINFO();
                }
                else
                {
                    fixedHUVolume.setToTRUE();
                    fixedHUVolume.Infobulle = ResourceHelper.GetMessage("forcedHU3");// "Aucune autre structure avec HU forcées (autres que celles testées dans la liste des structures de tables, cliniques et optim.)";
                    fixedHUVolume.MeasuredValue = ResourceHelper.GetMessage("forcedHU4");// "Aucune autre structure avec HU forcées";
                }

                this._result.Add(fixedHUVolume);


                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("anormalVolumeList"))
            {
                #region  Anormal Volume values (cc)

                List<string> anormalVolumeList = new List<string>();
                List<string> normalVolumeList = new List<string>();
                Item_Result anormalVolumeItem = new Item_Result();
                anormalVolumeItem.Label = ResourceHelper.GetMessage("structVolume1"); //"Volume des structures";xxxx
                anormalVolumeItem.ExpectedValue = "EN COURS";


                foreach (Structure struct1 in _ctx.StructureSet.Structures)
                {
                    if (struct1 != null) // if structure  exist 
                        if (!struct1.IsEmpty) //  and if not empty 
                        {
                            double volume = struct1.Volume;
                            double volumeMinimum = volumeMin(struct1.Id, volume, _ctx.Patient.Sex);
                            double volumeMaximum = volumeMax(struct1.Id, volume, _ctx.Patient.Sex);
                            //                            int isOk = volumeIsOk(struct1.Id, volume, _ctx.Patient.Sex);

                            if ((volumeMinimum != -1) && (volumeMaximum != -1)) // found in list
                            {
                                if ((volumeMinimum < volume) && (volumeMaximum > volume))
                                {
                                    normalVolumeList.Add(struct1.Id);
                                }
                                else
                                {
                                    anormalVolumeList.Add(struct1.Id + " (" + volume.ToString("F2") + " cc. " + ResourceHelper.GetMessage("structVolume2") + ": " + volumeMinimum + "-" + volumeMaximum + ")");

                                }
                            }

                            /*    if (isOk == 1)
                                normalVolumeList.Add(struct1.Id);
                            else if (isOk == 2)
                                anormalVolumeList.Add(struct1.Id + " (" + volume.ToString("F2") + " cc)");//. Attendu: " + es.volMin.ToString("F2") + " - " + es.volMax.ToString("F2") + " cc");
                            */
                        }
                }
                if (anormalVolumeList.Count > 0)
                {
                    anormalVolumeItem.setToWARNING();
                    if (anormalVolumeList.Count == 0)
                        anormalVolumeItem.MeasuredValue = ResourceHelper.GetMessage("structVolume3");
                    else if (anormalVolumeList.Count == 1)
                        anormalVolumeItem.MeasuredValue = ResourceHelper.GetMessage("structVolume4");
                    else
                        anormalVolumeItem.MeasuredValue = anormalVolumeList.Count.ToString() + " " + ResourceHelper.GetMessage("structVolume5");

                    anormalVolumeItem.Infobulle = ResourceHelper.GetMessage("structVolume6") + " " + anormalVolumeList.Count + " " + ResourceHelper.GetMessage("structVolume7") + "\n";
                    foreach (string avs in anormalVolumeList)
                        anormalVolumeItem.Infobulle += " - " + avs + "\n";
                }
                else if (normalVolumeList.Count > 0)
                {
                    anormalVolumeItem.setToTRUE();
                    anormalVolumeItem.MeasuredValue = normalVolumeList.Count + " " + ResourceHelper.GetMessage("structVolume8");
                    anormalVolumeItem.Infobulle = ResourceHelper.GetMessage("structVolume6") + " " + normalVolumeList.Count + " " + ResourceHelper.GetMessage("structVolume9") + "\n";
                    foreach (string avs in normalVolumeList)
                        anormalVolumeItem.Infobulle += " - " + avs + "\n";
                }
                else
                {
                    anormalVolumeItem.setToINFO();
                    anormalVolumeItem.MeasuredValue = ResourceHelper.GetMessage("structVolume10");
                    anormalVolumeItem.Infobulle = ResourceHelper.GetMessage("structVolume11") + "\n";
                }

                this._result.Add(anormalVolumeItem);


                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("shapeAnalyser"))
            {
                #region Number of expected parts of the structures
                /* Check if a structrure has the expected number of parts e.g. if a slice is missing */

                Item_Result shapeAnalyser = new Item_Result();
                shapeAnalyser.Label = ResourceHelper.GetMessage("NumberStruct1");
                shapeAnalyser.ExpectedValue = "wip...";


                List<string> correctStructs = new List<string>();
                List<string> uncorrectStructs = new List<string>();



                foreach (Structure struct1 in _ctx.StructureSet.Structures)
                {

                    if (struct1 != null)
                        if (!struct1.IsEmpty)
                        {

                            try
                            {
                                int n = struct1.GetNumberOfSeparateParts();
                                bool nExpectedPartsisOk = nPartsIsOk(struct1.Id, n, _ctx.Patient.Sex);

                                
                                if (nExpectedPartsisOk)
                                {
                                    correctStructs.Add(struct1.Id + " :\t\t" + n + " "+ ResourceHelper.GetMessage("NumberStruct2") + ")");
                                  
                                }
                                else
                                {
                                    uncorrectStructs.Add(struct1.Id + " :\t\t" + n + " " + ResourceHelper.GetMessage("NumberStruct2") + ")");
                                  
                                }
                            }
                            catch
                            {
                                //none
                            }

                           

                        }
                }
                if (uncorrectStructs.Count > 0)
                {
                    shapeAnalyser.setToWARNING();
                    shapeAnalyser.MeasuredValue = uncorrectStructs.Count + " "+ ResourceHelper.GetMessage("NumberStruct3");
                    shapeAnalyser.Infobulle =  uncorrectStructs.Count + " "+ ResourceHelper.GetMessage("NumberStruct4") + " :\n";
                    foreach (string s in uncorrectStructs)
                        shapeAnalyser.Infobulle += s + "\n";
                }
                else if (correctStructs.Count > 0)
                {
                    shapeAnalyser.setToTRUE();

                    shapeAnalyser.MeasuredValue = correctStructs.Count + " " + ResourceHelper.GetMessage("NumberStruct5");
                    shapeAnalyser.Infobulle = ResourceHelper.GetMessage("NumberStruct6") + "\n";
                    //                   foreach (string s in correctStructs)
                    //                     shapeAnalyser.Infobulle += s + "\n";

                }
                else
                {
                    shapeAnalyser.setToINFO();
                    shapeAnalyser.MeasuredValue = " " + ResourceHelper.GetMessage("NumberStruct7");
                    shapeAnalyser.Infobulle = ResourceHelper.GetMessage("NumberStruct8") + "\n";
                }



                this._result.Add(shapeAnalyser);

                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("missingSlicesItem"))
            {
                #region Missing slices

                Item_Result missingSlicesItem = new Item_Result();
                missingSlicesItem.Label = ResourceHelper.GetMessage("MissingContours1");
                missingSlicesItem.ExpectedValue = "wip...";

                int m = 0;
                int nAnalysedStructures = 0;
                List<string> structureswithAGap = new List<string>();
                foreach (Structure s in _ctx.StructureSet.Structures)
                {
                    string structName = s.Id.ToUpper();
                    if (!s.IsEmpty) // maybe a list of string would be a good idea
                        if ((!structName.Contains("PLOMB")) && (!structName.Contains("-")) && (!structName.Contains("OVERLA")) && (!structName.ToUpper().Contains("DOSE"))) // do no check marker structures
                            if (!structName.Contains("ENCOMP") && (!structName.Contains("ORFIT")))
                            {
                                nAnalysedStructures++;
                                m = getNumberOfMissingSlices(s, _ctx.StructureSet);
                                if (m > 0)
                                    structureswithAGap.Add(m.ToString() + " " + ResourceHelper.GetMessage("MissingContours2") + " : " + s.Id);
                            }
                }
                if (structureswithAGap.Count > 0)
                {
                    missingSlicesItem.MeasuredValue = structureswithAGap.Count + " " + ResourceHelper.GetMessage("MissingContours3");
                    missingSlicesItem.setToWARNING();
                    foreach (string s in structureswithAGap)
                        missingSlicesItem.Infobulle += s + "\n";

                }
                else
                {
                    missingSlicesItem.MeasuredValue = ResourceHelper.GetMessage("MissingContours4");
                    missingSlicesItem.setToTRUE();
                    missingSlicesItem.Infobulle = nAnalysedStructures.ToString() + " " + ResourceHelper.GetMessage("MissingContours5");
                }
                this._result.Add(missingSlicesItem);

                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("laterality"))
            {
                #region Laterality
                Item_Result laterality = new Item_Result();
                laterality.Label = ResourceHelper.GetMessage("Laterality1");
                laterality.ExpectedValue = "wip...";

                List<string> goodLaterality = new List<string>();
                List<string> badLaterality = new List<string>();


                // Structure sbody = _ctx.StructureSet.Structures.FirstOrDefault(x => x.Id.ToUpper() == "BODY"); // find body

                /*            if (sbody == null)
                                sbody = _ctx.StructureSet.Structures.FirstOrDefault(x => x.Id.ToUpper() == "CONTOUR EXTERNE"); // find body

                            if (sbody == null)
                                MessageBox.Show("BODY NOT FOUND");

                            double bodyXcenter = sbody.MeshGeometry.Bounds.X + (sbody.MeshGeometry.Bounds.SizeX / 2.0);
                */
                double bodyXcenter = _pinfo.theXcenter;
                //getXcenter();

                foreach (Structure s in _ctx.StructureSet.Structures)
                {
                    //    foreach (expectedStructure es in allStructures)
                    // {
                    string expectedLateralty = getExpectedLaterality(s.Id, _ctx.Patient.Sex);
                    //if (es.laterality != "NONE")
                    if (expectedLateralty != "N")  // if a laterality is expected, i.e. left lung should be left
                    {
                        //                    Structure s = _ctx.StructureSet.Structures.FirstOrDefault(x => x.Id == es.Name); // find a structure in ss with the same name
                        double xpos = 0.0;
                        if (s != null)
                            if (!s.IsEmpty)
                            {
                                xpos = s.MeshGeometry.Bounds.X + (s.MeshGeometry.Bounds.SizeX / 2.0);  // (Left limit + size) /2

                                //MessageBox.Show("orientation : " + _ctx.Image.ImagingOrientation.ToString());
                                //if(_ctx.Image.ImagingOrientation) //
                                //MessageBox.Show("body " + bodyXcenter + " x " + xpos + " " + es.Name);
                                if (xpos > bodyXcenter) // THIS IS LEFT,  if Supine HF but also Prone HF, Supine FF...
                                {
                                    if (expectedLateralty == "L")
                                        goodLaterality.Add(s.Id);
                                    else if (expectedLateralty == "R")
                                        badLaterality.Add(s.Id);
                                }
                                else
                                {
                                    if (expectedLateralty == "R")
                                        goodLaterality.Add(s.Id);
                                    else if (expectedLateralty == "L")
                                        badLaterality.Add(s.Id);

                                }
                            }
                    }
                }

                if (badLaterality.Count > 0)
                {
                    laterality.MeasuredValue = badLaterality.Count + " "+ ResourceHelper.GetMessage("Laterality2");
                    laterality.setToFALSE();

                    laterality.Infobulle = badLaterality.Count + " "+ ResourceHelper.GetMessage("Laterality3") + " : \n";
                    foreach (string s in badLaterality)
                        laterality.Infobulle += " - " + s + "\n";
                }
                else
                {
                    laterality.MeasuredValue = ResourceHelper.GetMessage("Laterality4") + " " + goodLaterality.Count() + " "+ ResourceHelper.GetMessage("Laterality5");
                    laterality.setToTRUE();

                    laterality.Infobulle = goodLaterality.Count() + " "+ ResourceHelper.GetMessage("Laterality6") + " : \n";
                    foreach (string s in goodLaterality)
                        laterality.Infobulle += " - " + s + "\n";

                    if (goodLaterality.Count == 0)
                    {
                        laterality.Infobulle = ResourceHelper.GetMessage("Laterality7") + "\n";
                    }

                }


                this._result.Add(laterality);
                #endregion
            }
            if (_pinfo.actualUserPreference.userWantsTheTest("aPTVforEveryone"))
            {
                #region A PTV for each CTV/GTV
                Item_Result aPTVforEveryone = new Item_Result();
                aPTVforEveryone.Label = ResourceHelper.GetMessage("missingPTV1");// "GTV/CTV/ITV sans PTV";
                aPTVforEveryone.ExpectedValue = "wip...";

                List<string> CTVandGTVs = new List<string>();
                List<string> PTVs = new List<string>();
                List<string> CTVwithoutAnyPTV = new List<string>();
                List<string> CTVwithPTV = new List<string>();
                foreach (Structure s in _ctx.StructureSet.Structures) // list all GTV/CTVs and PTVs
                {
                    if ((s.Id.ToUpper().Contains("CTV")) || (s.Id.ToUpper().Contains("GTV")) || (s.Id.ToUpper().Contains("ITV"))) // look for ctv or Gtv or itv in name, case insensitive thanks to ToUpper
                    {
                        if ((!s.Id.ToUpper().Contains("-CTV")) && (!s.Id.ToUpper().Contains("-GTV")) && (!s.Id.ToUpper().Contains("-ITV")) && (!s.Id.ToUpper().Contains("GTVS"))) // excludes lung-CTV and GTVs
                            if ((!s.Id.ToUpper().Contains("RING"))) // exlude rings
                                if (!s.IsEmpty)
                                    CTVandGTVs.Add(s.Id);
                    }

                    if (s.Id.ToUpper().Contains("PTV")) // look for ptv in name, case insensitive thanks to ToUpper
                    {
                        if (!s.Id.ToUpper().Contains("-PTV")) // exlude lung-PTV
                            if (!s.IsEmpty)
                                PTVs.Add(s.Id);
                    }
                }


                foreach (string CTV_ID in CTVandGTVs)
                {
                    Structure myCTV = _ctx.StructureSet.Structures.FirstOrDefault(x => x.Id == CTV_ID); // get the CTV
                    double CTV_xmin = myCTV.MeshGeometry.Bounds.X;
                    double CTV_xmax = CTV_xmin + myCTV.MeshGeometry.Bounds.SizeX;
                    double CTV_ymin = myCTV.MeshGeometry.Bounds.Y;
                    double CTV_ymax = CTV_ymin + myCTV.MeshGeometry.Bounds.SizeY;
                    double CTV_zmin = myCTV.MeshGeometry.Bounds.Z;
                    double CTV_zmax = CTV_zmin + myCTV.MeshGeometry.Bounds.SizeZ;

                    /*                double CTV_xmin = myCTV.MeshGeometry.Bounds.X;
                                    double CTV_xmax = myCTV.MeshGeometry.Bounds.SizeX;
                                    double CTV_ymin = myCTV.MeshGeometry.Bounds.Y;
                                    double CTV_ymax = myCTV.MeshGeometry.Bounds.SizeY;
                                    double CTV_zmin = myCTV.MeshGeometry.Bounds.Z;
                                    double CTV_zmax = myCTV.MeshGeometry.Bounds.SizeZ;
                    */

                    bool found = false;
                    foreach (string PTV_ID in PTVs)
                    {
                        Structure myPTV = _ctx.StructureSet.Structures.FirstOrDefault(x => x.Id == PTV_ID); // loop on PTV
                        double PTV_xmin = myPTV.MeshGeometry.Bounds.X;
                        double PTV_xmax = PTV_xmin + myPTV.MeshGeometry.Bounds.SizeX;
                        double PTV_ymin = myPTV.MeshGeometry.Bounds.Y;
                        double PTV_ymax = PTV_ymin + myPTV.MeshGeometry.Bounds.SizeY;
                        double PTV_zmin = myPTV.MeshGeometry.Bounds.Z;
                        double PTV_zmax = PTV_zmin + myPTV.MeshGeometry.Bounds.SizeZ;

                        /*                    double PTV_xmin = myPTV.MeshGeometry.Bounds.X;
                                            double PTV_xmax = myPTV.MeshGeometry.Bounds.SizeX;
                                            double PTV_ymin = myPTV.MeshGeometry.Bounds.Y;
                                            double PTV_ymax = myPTV.MeshGeometry.Bounds.SizeY;
                                            double PTV_zmin = myPTV.MeshGeometry.Bounds.Z;
                                            double PTV_zmax = myPTV.MeshGeometry.Bounds.SizeZ;
                        */

                        if ((PTV_xmin < CTV_xmin) && ((PTV_xmax > CTV_xmax)))
                            if ((PTV_ymin < CTV_ymin) && ((PTV_ymax > CTV_ymax)))
                                if ((PTV_zmin < CTV_zmin) && ((PTV_zmax > CTV_zmax)))
                                {

                                    found = true;
                                    break; // exit as soon as a PTV is found
                                }



                    }
                    if (found == false)
                        CTVwithoutAnyPTV.Add(CTV_ID);
                    else
                        CTVwithPTV.Add(CTV_ID);

                }

                /*String infoMsg = null;
                foreach (string tttt in CTVwithPTV)
                {
                    infoMsg += "\n" + tttt;
                }
                MessageBox.Show(infoMsg);*/

                if (CTVwithoutAnyPTV.Count() > 0) // at least one GTV/CTV has no PTV
                {
                    aPTVforEveryone.setToFALSE();
                    aPTVforEveryone.MeasuredValue = CTVwithoutAnyPTV.Count.ToString() + " "+ ResourceHelper.GetMessage("missingPTV2");
                    aPTVforEveryone.Infobulle = CTVwithoutAnyPTV.Count + " "+ ResourceHelper.GetMessage("missingPTV3") + " : \n";
                    foreach (string s in CTVwithoutAnyPTV)
                        aPTVforEveryone.Infobulle += " - " + s + "\n";
                }
                else
                {
                    aPTVforEveryone.setToTRUE();
                    aPTVforEveryone.MeasuredValue = CTVwithPTV.Count.ToString() + " "+ ResourceHelper.GetMessage("missingPTV4");
                    aPTVforEveryone.Infobulle = CTVwithPTV.Count + " "+ ResourceHelper.GetMessage("missingPTV5") + " : \n";
                    foreach (string s in CTVwithPTV)
                        aPTVforEveryone.Infobulle += " - " + s + "\n";
                }

                aPTVforEveryone.Infobulle += "\n\n"+ ResourceHelper.GetMessage("missingPTV6") + " \n";



                this._result.Add(aPTVforEveryone);
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
