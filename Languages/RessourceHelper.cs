using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Resources;
using System.Windows;
namespace PlanCheck.Languages
{
    public static class ResourceHelper
    {
        // Initialise le ResourceManager pour accéder aux ressources de traduction
        private static ResourceManager resManager = new ResourceManager("PlanCheck.Messages", typeof(ResourceHelper).Assembly);

        // Méthode pour configurer la culture en fonction de la langue choisie (par exemple, "FR" ou "UK")
        public static void SetLanguage(string lang)
        {
            if (lang == "FR")
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr");
            else if (lang == "UK")
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            else
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
        }

        // Méthode pour récupérer un message traduit par clé
        public static string GetMessage(string key)
        {
            return resManager.GetString(key);
        }

        public static void displayMessage(string s)
        {
            MessageBox.Show(GetMessage(s));
        }
    }
}
