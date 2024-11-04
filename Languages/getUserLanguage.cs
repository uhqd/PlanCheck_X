using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace PlanCheck.Languages
{
    public static class getUserLanguage
    {

        private static string userLangFilePath = String.Empty;

        private static string defaultLangFilePATH = String.Empty;
        private static string userLang = String.Empty;

        public static void init(string fulluserID)
        {
            string userid; //i.e admin\simon_lu --> cut admin\
            if (fulluserID.Contains("\\"))
            {
                string[] sub = fulluserID.Split('\\');
                userid = sub[1];
            }
            else
                userid = fulluserID;



            userLangFilePath = userid + "_lang.txt";
            userLangFilePath = Directory.GetCurrentDirectory() + @"\plancheck_data\users\UserPrefs\" + userLangFilePath;
            defaultLangFilePATH = Directory.GetCurrentDirectory() + @"\plancheck_data\users\UserPrefs\defaut_lang.txt";
            if (!File.Exists(userLangFilePath))
            {
                try
                {
                    // Copier le fichier
                    File.Copy(defaultLangFilePATH, userLangFilePath, true);

                }
                catch
                {
                    MessageBox.Show("Impossible de copier " + defaultLangFilePATH + " vers " + userLangFilePath);
                }
            }


            string userLang = File.ReadAllText(userLangFilePath);
            ResourceHelper.SetLanguage(userLang);
        }

        public static string myLang
        {
            get { return userLang; }
            set
            {

                userLang = value;
                try
                {
                    File.Delete(userLangFilePath);
                    File.WriteAllText(userLangFilePath, value);
                }
                catch
                {
                    MessageBox.Show("Impossible de trouver ou d'écrire dans " + userLangFilePath);
                }
            }
        }


    }
}
