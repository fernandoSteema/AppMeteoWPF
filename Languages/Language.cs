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
          
            foreach (string line in File.ReadLines($"..\\..\\..\\Languages\\{file}.txt"))
            {
                if (line.Contains("="))
                {
                    string[] camps = line.Split('=');
                    info.Add(camps[0].Trim(), camps[1].Trim());
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
                // Actualiza etiquetas y contenido traducido
                //mainWindow.UpdateChartLanguage();
                //mainWindow.btmDays.Content = Language.info.ContainsKey("btmDays") ? Language.info["btmDays"] : "Days";
                //mainWindow.btnHours.Content = Language.info.ContainsKey("btnHours") ? Language.info["btnHours"] : "Hours";
                mainWindow.UpdateAnnotations();

                // Solo actualizamos nombres de días
                mainWindow.UpdateAndLoadForecastDays();


            }
        }

        //public static void Controllers(Form form)
        //{
        //    if (form == null)
        //    {
        //        MessageBox.Show("ERROR: El formulario es nulo.");
        //        return;
        //    }

        //    if (info == null || info.Count == 0)
        //    {
        //        MessageBox.Show("ERROR: La colección 'info' está vacía o no inicializada.");
        //        return;
        //    }

        //    foreach (string control in info.Keys)
        //    {
        //        try
        //        {
        //            Control foundControl = form.Controls[control];

        //            if (foundControl != null)
        //            {
        //                foundControl.Text = info[control];
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("ERROR: " + ex.Message);
        //        }
        //    }
        //}

        //public static void UpdateMenuStrip(MenuStrip menu)
        //{
        //    foreach (ToolStripMenuItem item in menu.Items)
        //    {
        //        UpdateMenuItem(item);
        //    }
        //}

        //private static void UpdateMenuItem(ToolStripMenuItem item)
        //{
        //    if (info.ContainsKey(item.Name))
        //    {
        //        item.Text = info[item.Name];
        //    }

        //    foreach (ToolStripItem subItem in item.DropDownItems)
        //    {
        //        if (subItem is ToolStripMenuItem subMenuItem)
        //        {
        //            UpdateMenuItem(subMenuItem);
        //        }
        //    }
        //}
    }
}
 