using System.Drawing;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AppMeteo.Controllers;
using MeteoApp.MODELS;
using Steema.TeeChart;
using Steema.TeeChart.Styles;
using Steema.TeeChart.Tools;

namespace AppMeteo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MeteoController metoController;
        private WeatherResponse currentTemperature;
        private Forecast allTemperatures;

        // Dictionaries
        private Dictionary<DateTime, string> diasConFechas = new Dictionary<DateTime, string>();
        private Dictionary<double, string> horasConFechas = new Dictionary<double, string>();
        private Dictionary<string, List<string>> iconosPorDia = new Dictionary<string, List<string>>();
        private Dictionary<string, BitmapImage> imageCacheWPF = new();
        private Dictionary<string, Tuple<double, double>> rangoDias = new Dictionary<string, Tuple<double, double>>();

        // Chart Tools
        NearestPoint toolNPTemp = null;
        NearestPoint toolNPHumidity = null;
        Steema.TeeChart.Tools.Annotation annotation;
        Axis vertAxis, horizAxis;

        // Lists
        List<string> lstIconUrls = new List<string>();
        private List<double> verticalLinePositions = new List<double>();

        public MainWindow()
        {
            InitializeComponent();
            metoController = new MeteoController();
            currentTemperature = new WeatherResponse();
            
        }

        private void btnSearch_Click_1(object sender, RoutedEventArgs e)
        {
            string city = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(city))
            {
                GetCurrentTemperature(city);
                GetAllTemperaturesByDays(city);
                GetTemperatureAndHumidity(city);
            }
        }
       
        private async void GetCurrentTemperature(string city)
        {
            currentTemperature = await metoController.GetCurrentTemperatura(city);

            if (currentTemperature != null)
            {
                txtTemperature.Text = Math.Truncate(currentTemperature.Current.temp_c).ToString() + "ºC";
                txtCity.Text = currentTemperature.Location.Name;
                txtRegion.Text = $"{currentTemperature.Location.Region}, {currentTemperature.Location.Country}";
                string iconUrl = $"https:{currentTemperature.Current.condition.Icon}";
                imgWeatherIcon.Source = new BitmapImage(new Uri(iconUrl));
            }
            else
            {
                txtTemperature.Text = "--°C";
                txtCity.Text = "Ciudad no encontrada";
                txtRegion.Text = "";
                imgWeatherIcon.Source = null;
            }
        }

        public async void GetAllTemperaturesByDays(string city)
        {
            Bar barSeries;
            if (ChartTemp.Series.Count == 0)
            {
                barSeries = new Bar();
             
                ChartTemp.Series.Add(barSeries);
                ChartTemp.Legend.Visible = false;
                ChartTemp[0].Marks.Visible = false;
            }
            else
            {
                barSeries = (Bar)ChartTemp.Series[0];   
            }

            sliderZoom.Visibility = Visibility.Collapsed;
            barSeries.Clear();
            iconosPorDia.Clear();   
            diasConFechas.Clear();

            barSeries.ColorEach = false;
            barSeries.Transparency = 70;

            // Elimina anotaciones anteriores
            for (int i = ChartTemp.Tools.Count - 1; i >= 0; i--)
            {
                if(ChartTemp.Tools[i] is Steema.TeeChart.Tools.Annotation)
                {
                    ChartTemp.Tools.RemoveAt(i);
                }
            }
            
            allTemperatures = await metoController.GetPrevisionBy10Days(city);

            if(allTemperatures != null && allTemperatures.forecastday.Count > 0)
            {
                ChartTemp.Header.Text = "FORECAST 7 DAYS";

                foreach(var dia in allTemperatures.forecastday)
                {
                    DateTime fecha = DateTime.Parse(dia.date);
                    string diaSemana = fecha.ToString("dddd");

                    //if (Language.info.ContainsKey(diaSemana))
                    //    diaSemana = Language.info[diaSemana];

                    string dateKey = fecha.ToString("yyyy-MM-dd");
                    diasConFechas[fecha] = diaSemana;
                    barSeries.Add(dia.day.avgtemp_c, diaSemana);

                    string iconUrl = $"https:{dia.day.condition.Icon}";
                    iconosPorDia[dateKey] = new List<string> { iconUrl };
                }

                ChartTemp.Axes.Bottom.Automatic = true;
                ChartTemp.Invalidate();
            }
            
        }
        public async void GetTemperatureAndHumidity(string city)
        {
            // Asegurar que el Chart está inicializado correctamente
            if (ChartTempAndHumidity?.Chart == null)
                return;

            ChartTempAndHumidity.Series.Clear();

            // Crear serie de temperatura
            var lineTemperature = new Line(ChartTempAndHumidity.Chart)
            {
                Title = "Temperature",
                Smoothed = false,
                Stairs = false,
                VertAxis = VerticalAxis.Left
            };
            lineTemperature.Pointer.Visible = true;

            // Crear serie de humedad
            var lineHumidity = new Line(ChartTempAndHumidity.Chart)
            {
                Title = "Humidity",
                Smoothed = false,
                Stairs = false,
                VertAxis = VerticalAxis.Right
            };
            lineHumidity.Pointer.Visible = true;

            ChartTempAndHumidity.Series.Add(lineTemperature);
            ChartTempAndHumidity.Series.Add(lineHumidity);

            // Crear herramientas NearestPoint (una para cada serie)
            if (toolNPHumidity == null)
            {
                toolNPHumidity = new NearestPoint(ChartTempAndHumidity.Chart)
                {
                    Series = lineHumidity,
                    Style = NearestPointStyles.Rectangle,
                    Size = 10
                };
                ChartTempAndHumidity.Tools.Add(toolNPHumidity);
                toolNPHumidity.Change += ToolNPHumidity_Change; 
            }
            if (toolNPTemp == null)
            {
                toolNPTemp = new NearestPoint(ChartTempAndHumidity.Chart)
                {
                    Series = lineTemperature,
                    Style = NearestPointStyles.Circle,
                    Size = 10
                };
                ChartTempAndHumidity.Tools.Add(toolNPTemp);
               
            }


            // Obtener los datos
            allTemperatures = await metoController.GetEvolutionOfHumidityAndTemperatureByCity(city);

            annotation = new Steema.TeeChart.Tools.Annotation(ChartTempAndHumidity.Chart);
            vertAxis = ChartTempAndHumidity.Axes.Left;
            horizAxis = ChartTempAndHumidity.Axes.Bottom;

            ChartTempAndHumidity.MouseMove += ChartTempAndHumidity_MouseMove;

            if (allTemperatures != null)
            {
                foreach (var dia in allTemperatures.forecastday)
                {
                    ChartTempAndHumidity.Header.Text = "EVOLUTION OF THE DAY";
                    ChartTempAndHumidity.SubHeader.Text = "Temperatura / Humitat relativa";

                    foreach (var hora in dia.hour)
                    {
                        DateTime fechaHora = DateTime.Parse(hora.time.ToString());
                        if (fechaHora.Hour >= 0 && fechaHora.Hour <= 9 && (fechaHora.Minute == 0 || fechaHora.Minute == 30))
                        {
                            lineTemperature.Add(fechaHora, hora.temp_c);
                            lineHumidity.Add(fechaHora, hora.humidity);

                            string iconUrl = $"https:{hora.condition.Icon}";
                            lstIconUrls.Add(iconUrl);
                        }
                    }
                }
            }

            // Configurar ejes
            ChartTempAndHumidity.Axes.Left.SetMinMax(0, 20);
            ChartTempAndHumidity.Axes.Left.Increment = 10;
            ChartTempAndHumidity.Axes.Left.Automatic = false;

            ChartTempAndHumidity.Axes.Right.SetMinMax(0, 100);
            ChartTempAndHumidity.Axes.Right.Increment = 50;
            ChartTempAndHumidity.Axes.Right.Automatic = false;

            // Forzar la actualización del gráfico
            ChartTempAndHumidity.Invalidate();
        }

        private void ChartTempAndHumidity_MouseMove(object? sender, MouseEventArgs e)
        {
            var position = e.GetPosition(ChartTempAndHumidity);

            double mouseX = position.X;
            double mouseY = position.Y;
            
            toolNPHumidity.Active = mouseX >= horizAxis.IStartPos && mouseX <= horizAxis.IEndPos &&
            mouseY >= vertAxis.IStartPos && mouseY <= vertAxis.IEndPos;

            toolNPTemp.Active = mouseX >= horizAxis.IStartPos && mouseX <= horizAxis.IEndPos &&
            mouseY>= vertAxis.IStartPos && mouseY <= vertAxis.IEndPos;

            // Activate annotation if at least one of the tools is active
            annotation.Active = toolNPHumidity.Active || toolNPTemp.Active;
        }

        private void ToolNPHumidity_Change(object? sender, EventArgs e)
        {
            Line graficoLiniaTemperature = (Line)ChartTempAndHumidity.Series[0];
            Line graficoLiniaHumidity = (Line)ChartTempAndHumidity.Series[1];

            //string tempText = Language.info.ContainsKey("Temperature") ? Language.info["Temperature"] : "Temperature";
            //string humText = Language.info.ContainsKey("Humidity") ? Language.info["Humidity"] : "Humidity";

            int indexTemp = toolNPTemp.Point;
            int indexHum = toolNPHumidity.Point;
            // Verificar si los puntos son válidos
            if (indexTemp >= 0 && indexTemp < graficoLiniaTemperature.Count &&
                indexHum >= 0 && indexHum < graficoLiniaHumidity.Count)
            {
                annotation.Text = $":Temperature {graficoLiniaTemperature.YValues[toolNPTemp.Point]}ºC \n " +
                              $"Humidity: {graficoLiniaHumidity.YValues[toolNPHumidity.Point]}%";
            }
            else
            {
                annotation.Text = string.Empty;
            }
                
        }

        private void ChartTemp_AfterDraw(object sender, Steema.TeeChart.Drawing.IGraphics3D g)
        {
            // Removes any previous clipping regions that may have been configured
            ChartTemp.Graphics3D.ClearClipRegions();

            // Creates a rectangle that respects the horizontal boundaries of the chart (X and Width)
            // but extends from the top of the screen (Y=0) to the bottom of the chart
            Rect openTopRect = new Rect(
                                    ChartTemp.Chart.ChartRect.X,
                                    0, ChartTemp.Chart.ChartRect.Width,
                                    ChartTemp.Chart.ChartRect.Height +
                                    ChartTemp.Chart.ChartRect.Y
            );

            // Set this rectangle as the clipping region.
            // Anything drawn outside this region will be clipped (not visible).
            ChartTemp.Graphics3D.ClipRectangle(openTopRect);

            foreach (Series s in ChartTemp.Series)
            {
                if (!(s is Bar)) continue;

                Dictionary<string, List<int>> datosAgrupados = new Dictionary<string, List<int>>();

                for (int i = 0; i < s.Count; i++)
                {
                    string dateKey;

                    // Case 1: Data represents hours (GetAllTemperatures)
                    if (horasConFechas.ContainsKey(s.XValues[i]))
                    {
                        dateKey = horasConFechas[s.XValues[i]];
                    }
                    // Case 2: Data represents days (GetAllTemperaturesByDays)
                    else if (diasConFechas.ContainsValue(s.Labels[i]))
                    {
                        dateKey = diasConFechas.FirstOrDefault(x => x.Value == s.Labels[i]).Key.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        continue;
                    }

                    if (!datosAgrupados.ContainsKey(dateKey))
                        datosAgrupados[dateKey] = new List<int>();

                    datosAgrupados[dateKey].Add(i);
                }

                foreach (var grupo in datosAgrupados)
                {
                    int p = 0;
                    string dateKey = grupo.Key;

                    if (!iconosPorDia.ContainsKey(dateKey) || iconosPorDia[dateKey].Count == 0)
                        continue;

                    foreach (int index in grupo.Value)
                    {
                        BitmapImage bitmap;
                        if (s.Labels[index].Contains(":"))
                        {
                            int iconIndex = p % iconosPorDia[dateKey].Count;
                            bitmap = LoadBitmapFromUrl(iconosPorDia[dateKey][iconIndex]);
                        }
                        else
                        {
                            bitmap = LoadBitmapFromUrl(iconosPorDia[dateKey][0]);
                        }

                        var tChartImage = new Steema.TeeChart.WPF.Drawing.TImage(bitmap);

                        int iconWidth = bitmap.PixelWidth;
                        int iconHeight = bitmap.PixelHeight;
                        int xPos = (int)ChartTemp.Axes.Bottom.CalcPosValue(s.XValues[index]) - (iconWidth / 2);
                        int yPos = (int)ChartTemp.Axes.Left.CalcPosValue(s.YValues[index]) - (iconHeight / 2);

                        g.Draw(xPos, yPos, tChartImage);

                        string txt = $"{s.YValues[index]}ºC";
                        int textWidth = (int)(g.TextWidth(txt) / 2);
                        int textYPos = yPos + iconHeight + 5;
                        int textXPos = xPos + (iconWidth / 2) - textWidth;

                        g.TextOut(textXPos, textYPos, txt);

                        p++;
                    }
                }
            }

            ChartTemp.Graphics3D.ClearClipRegions();
        }

        private BitmapImage LoadBitmapFromUrl(string url)
        {
            if (imageCacheWPF.ContainsKey(url))
            {
                return imageCacheWPF[url];
            }

            using (WebClient client = new WebClient())
            {
                byte[] imageBytes = client.DownloadData(url);

                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze(); 
                    imageCacheWPF[url] = bitmap;
                    return bitmap;
                }
            }
        }

        private void lstCities_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lstCities.SelectedItem is ListBoxItem selectedItem)
            {
                txtSearch.Text = selectedItem.Content.ToString();
                txtSearch.Focus();
                txtSearch.CaretIndex = txtSearch.Text.Length;
            }
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSearch_Click_1(sender, e);
            }
        }

    }
}