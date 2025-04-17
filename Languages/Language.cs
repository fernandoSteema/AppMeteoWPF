using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.IO;

namespace AppMeteo.Languages
{
    public static class Language
    {
        public static Dictionary<string, string> info = new Dictionary<string, string>();

        private static void Load(string file)
        {
            info.Clear();
          
            foreach (string line in File.ReadLines($"..\\..\\..\\Languages\\{file}"))
            {
                if (line.Contains("="))
                {
                    string[] camps = line.Split('=');
                    info[camps[0].Trim()] = camps[1].Trim();
                }
            }
        }

        public static void ChangeLenguage(string file)
        {
            Properties.Settings config = new Properties.Settings();
            config.lang = file;
            config.Save();
            Load(file);

            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                
                mainWindow.UpdateChartLanguage();

                if (mainWindow.btnDays != null)
                    mainWindow.btnDays.Content = Language.info.ContainsKey("btnDays") ? Language.info["btnDays"] : "Days";

                if (mainWindow.btnHours != null)
                    mainWindow.btnHours.Content = Language.info.ContainsKey("btnHours") ? Language.info["btnHours"] : "Hours";

                mainWindow.UpdateAnnotations();
                
                mainWindow.UpdateAndLoadForecastDays();

                if (mainWindow.btnDayActivate)
                {
                    if (mainWindow.ChartTemp == null) return;

                    mainWindow.GetAllTemperaturesByDays(mainWindow.currentCity);
                }


            }
        }
    }
}
 