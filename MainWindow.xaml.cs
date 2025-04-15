using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Controls;

using System.Windows.Input;
using System.Windows.Media.Imaging;
using AppMeteo.Controllers;
using AppMeteo.Languages;
using MeteoApp.MODELS;
using Steema.TeeChart;
using Steema.TeeChart.Styles;
using Steema.TeeChart.Tools;
using Annotation = Steema.TeeChart.Tools.Annotation;

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

        // Variables
        bool eventAdded = false;

        public MainWindow()
        {
            InitializeComponent();
            metoController = new MeteoController();
            currentTemperature = new WeatherResponse();
            AppMeteo.Languages.Language.ChangeLenguage(Properties.Settings.Default.lang);
            cmbDays.Visibility = Visibility.Collapsed;
            scrollBarGrafico.Visibility = Visibility.Collapsed;

        }

        private void btnSearch_Click_1(object sender, RoutedEventArgs e)
        {
            string city = txtSearch.Text.Trim();

            if(!eventAdded)
            {
                ChartTemp.AfterDraw += ChartTemp_AfterDraw1;
                eventAdded = true;
            }

            if (!string.IsNullOrEmpty(city))
            {
                GetCurrentTemperature(city);
                GetAllTemperaturesByDays(city);
                GetTemperatureAndHumidity(city);
            }
        }

        private void ChartTemp_AfterDraw1(object sender, Steema.TeeChart.Drawing.IGraphics3D g)
        {
            int offsetX = 34;
            foreach (var verticalLineX in verticalLinePositions)
            {
                int pixelX = ChartTemp.Axes.Bottom.CalcXPosValue(verticalLineX);
                g.Line(pixelX, ChartTemp.Axes.Left.IStartPos, pixelX, ChartTemp.Axes.Left.IEndPos);
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

        //public async void GetAllTemperaturesByDays(string city)
        //{
        //    Bar barSeries;
        //    if (ChartTemp.Series.Count == 0)
        //    {
        //        barSeries = new Bar();

        //        ChartTemp.Series.Add(barSeries);
        //        ChartTemp.Legend.Visible = false;
        //        ChartTemp[0].Marks.Visible = false;
        //    }
        //    else
        //    {
        //        barSeries = (Bar)ChartTemp.Series[0];   
        //    }

        //    scrollBarGrafico.Visibility = Visibility.Collapsed;
        //    barSeries.Clear();
        //    iconosPorDia.Clear();   
        //    diasConFechas.Clear();

        //    barSeries.ColorEach = false;
        //    barSeries.Transparency = 70;

        //    ChartTemp.Panning.Allow = ScrollModes.None;
        //    ChartTemp.Zoom.Active = false;

        //    // Elimina anotaciones anteriores
        //    for (int i = ChartTemp.Tools.Count - 1; i >= 0; i--)
        //    {
        //        if(ChartTemp.Tools[i] is Steema.TeeChart.Tools.Annotation)
        //        {
        //            ChartTemp.Tools.RemoveAt(i);
        //        }
        //    }

        //    allTemperatures = await metoController.GetPrevisionBy10Days(city);

        //    if(allTemperatures != null && allTemperatures.forecastday.Count > 0)
        //    {
        //        ChartTemp.Header.Text = "FORECAST 7 DAYS";

        //        foreach(var dia in allTemperatures.forecastday)
        //        {
        //            DateTime fecha = DateTime.Parse(dia.date);
        //            string diaSemana = fecha.ToString("dddd");

        //            //if (Language.info.ContainsKey(diaSemana))
        //            //    diaSemana = Language.info[diaSemana];

        //            string dateKey = fecha.ToString("yyyy-MM-dd");
        //            diasConFechas[fecha] = diaSemana;
        //            barSeries.Add(dia.day.avgtemp_c, diaSemana);

        //            string iconUrl = $"https:{dia.day.condition.Icon}";
        //            iconosPorDia[dateKey] = new List<string> { iconUrl };
        //        }

        //        ChartTemp.Axes.Bottom.Automatic = true;
        //        ChartTemp.Invalidate();
        //    }

        //}

        //public async void GetAllTemperatures(string city)
        //{
        //    Bar barSeries;
        //    verticalLinePositions.Clear();
        //    scrollBarGrafico.Visibility = Visibility.Visible;
        //    cmbDays.Visibility = Visibility.Visible;
        //    scrollBarGrafico.Visibility = Visibility.Visible;

        //    if (ChartTemp.Series.Count == 0)
        //    {
        //        barSeries = new Bar();

        //        ChartTemp.Series.Add(barSeries);
        //        ChartTemp.Legend.Visible = false;
        //        ChartTemp[0].Marks.Visible = false;
        //    }
        //    else
        //    {
        //        barSeries = (Bar)ChartTemp.Series[0];
        //    }

        //    if (ChartTemp.Series.Count > 0)
        //    {
        //        ChartTemp.Series[0].Clear();
        //        iconosPorDia.Clear();
        //        diasConFechas.Clear();
        //        horasConFechas.Clear();
        //    }

        //    barSeries.ColorEach = false;
        //    barSeries.Transparency = 70;
        //    ChartTemp.Panning.Allow = ScrollModes.None;
        //    ChartTemp.Zoom.Active = false;

        //    //string headerTxt = Language.info.ContainsKey("Forecast_by_hour") ? Language.info["Forecast_by_hour"] : "Previsió per hores";
        //    //ChartTemp.Header.Text = headerTxt;
        //    ChartTemp.Header.Text = "FORECAST BY HOURS";
        //    allTemperatures = await metoController.GetEvolutionOfWeatherByCity(city);

        //    if (allTemperatures != null && allTemperatures.forecastday.Count > 0)
        //    {
        //        double? firstBarX = null;
        //        DateTime? lastBarTime = null;
        //        double? lastBarX = null;
        //        double? firstBarXNextDay = null;

        //        foreach (var dia in allTemperatures.forecastday)
        //        {
        //            DateTime date = DateTime.Parse(dia.date);
        //            string dateKey = date.ToString("yyyy-MM-dd");
        //            diasConFechas[date] = date.ToString("dddd");

        //            List<string> tempIcons = new List<string>();
        //            firstBarX = null;

        //            foreach (var hora in dia.hour)
        //            {
        //                DateTime fechaHora = DateTime.Parse(hora.time.ToString());

        //                if (fechaHora < DateTime.Now)
        //                    continue;
        //                double horaValor = fechaHora.ToOADate();
        //                barSeries.Add(horaValor, hora.temp_c, fechaHora.ToString("HH:mm"));
        //                horasConFechas[horaValor] = dateKey;
        //                tempIcons.Add($"https:{hora.condition.Icon}");

        //                if (firstBarX == null)
        //                    firstBarX = horaValor;

        //                lastBarTime = fechaHora;
        //                lastBarX = horaValor;

        //                rangoDias[dateKey] = new Tuple<double, double>(firstBarX ?? 0, lastBarX ?? 0);
        //            }

        //            if (tempIcons.Count > 0)
        //                iconosPorDia[dateKey] = tempIcons;
        //            if (firstBarX != null)
        //                firstBarXNextDay = firstBarX;

        //            if (lastBarTime != null && firstBarXNextDay != null)
        //            {
        //                DateTime date23 = date.AddHours(23);
        //                DateTime date00 = date.AddDays(1).AddHours(0);

        //                double x23 = date23.ToOADate();
        //                double x00 = date00.ToOADate();

        //                double verticalLineX = (x23 + x00) / 2;
        //                verticalLinePositions.Add(verticalLineX);
        //            }

        //            ChartTemp.Page.MaxPointsPerPage = 11;
        //            ChartTemp.Page.Current = 1;
        //        }
        //        await Task.Delay(100);

        //        if (barSeries.Count > 0)
        //        {
        //            double minX = barSeries.MinXValue();
        //            double maxX = barSeries.MaxXValue();

        //            if (maxX - minX < 10)
        //            {
        //                maxX = minX + 10;
        //            }

        //            double visibleRange = (maxX - minX) / 20;

        //            scrollBarGrafico.Minimum = 0;
        //            scrollBarGrafico.Maximum = 990;
        //            scrollBarGrafico.Value = 0;
        //            scrollBarGrafico.LargeChange = 10;

        //            double initialMax = minX + visibleRange;
        //            ChartTemp.Axes[0].Minimum = minX;
        //            ChartTemp.Axes[0].Maximum = initialMax;
        //        }
        //        else
        //        {
        //            MessageBox.Show("No se encontraron datos para mostrar en el gráfico.");
        //        }
        //        UpdateAndLoadForecastDays();
        //        UpdateAnnotations();
        //        ChartTemp.Invalidate();
        //    }
        //}
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

            scrollBarGrafico.Visibility = Visibility.Collapsed;
            barSeries.Clear();
            iconosPorDia.Clear();
            diasConFechas.Clear();
            // Limpiar también verticalLinePositions aquí
            verticalLinePositions.Clear();

            barSeries.ColorEach = false;
            barSeries.Transparency = 70;

            ChartTemp.Panning.Allow = ScrollModes.None;
            ChartTemp.Zoom.Active = false;

            // Elimina anotaciones anteriores
            for (int i = ChartTemp.Tools.Count - 1; i >= 0; i--)
            {
                if (ChartTemp.Tools[i] is Steema.TeeChart.Tools.Annotation)
                {
                    ChartTemp.Tools.RemoveAt(i);
                }
            }

            allTemperatures = await metoController.GetPrevisionBy10Days(city);

            if (allTemperatures != null && allTemperatures.forecastday.Count > 0)
            {
                ChartTemp.Header.Text = "FORECAST 7 DAYS";

                foreach (var dia in allTemperatures.forecastday)
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

        public async void GetAllTemperatures(string city)
        {
            Bar barSeries;
            scrollBarGrafico.Visibility = Visibility.Visible;
            cmbDays.Visibility = Visibility.Visible;
            scrollBarGrafico.Visibility = Visibility.Visible;

            // Limpiar verticalLinePositions al inicio
            verticalLinePositions.Clear();

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

            if (ChartTemp.Series.Count > 0)
            {
                ChartTemp.Series[0].Clear();
                iconosPorDia.Clear();
                diasConFechas.Clear();
            }

            barSeries.ColorEach = false;
            barSeries.Transparency = 70;
            ChartTemp.Panning.Allow = ScrollModes.None;
            ChartTemp.Zoom.Active = false;

            ChartTemp.Header.Text = "FORECAST BY HOURS";
            allTemperatures = await metoController.GetEvolutionOfWeatherByCity(city);

            if (allTemperatures != null && allTemperatures.forecastday.Count > 0)
            {
                double? firstBarX = null;
                DateTime? lastBarTime = null;
                double? lastBarX = null;
                double? firstBarXNextDay = null;

                foreach (var dia in allTemperatures.forecastday)
                {
                    DateTime date = DateTime.Parse(dia.date);
                    string dateKey = date.ToString("yyyy-MM-dd");
                    diasConFechas[date] = date.ToString("dddd");

                    List<string> tempIcons = new List<string>();
                    firstBarX = null;

                    foreach (var hora in dia.hour)
                    {
                        DateTime fechaHora = DateTime.Parse(hora.time.ToString());

                        if (fechaHora < DateTime.Now)
                            continue;
                        double horaValor = fechaHora.ToOADate();
                        barSeries.Add(horaValor, hora.temp_c, fechaHora.ToString("HH:mm"));
                        horasConFechas[horaValor] = dateKey;
                        tempIcons.Add($"https:{hora.condition.Icon}");

                        if (firstBarX == null)
                            firstBarX = horaValor;

                        lastBarTime = fechaHora;
                        lastBarX = horaValor;

                        rangoDias[dateKey] = new Tuple<double, double>(firstBarX ?? 0, lastBarX ?? 0);
                    }

                    if (tempIcons.Count > 0)
                        iconosPorDia[dateKey] = tempIcons;
                    if (firstBarX != null)
                        firstBarXNextDay = firstBarX;

                    if (lastBarTime != null && firstBarXNextDay != null)
                    {
                        DateTime date23 = date.AddHours(23);
                        DateTime date00 = date.AddDays(1).AddHours(0);

                        double x23 = date23.ToOADate();
                        double x00 = date00.ToOADate();

                        double verticalLineX = (x23 + x00) / 2;
                        verticalLinePositions.Add(verticalLineX);
                    }

                    ChartTemp.Page.MaxPointsPerPage = 11;
                    ChartTemp.Page.Current = 1;
                }
                await Task.Delay(100);

                if (barSeries.Count > 0)
                {
                    double minX = barSeries.MinXValue();
                    double maxX = barSeries.MaxXValue();

                    if (maxX - minX < 10)
                    {
                        maxX = minX + 10;
                    }

                    double visibleRange = (maxX - minX) / 20;

                    scrollBarGrafico.Minimum = 0;
                    scrollBarGrafico.Maximum = 990;
                    scrollBarGrafico.Value = 0;
                    scrollBarGrafico.LargeChange = 10;

                    double initialMax = minX + visibleRange;
                    ChartTemp.Axes[0].Minimum = minX;
                    ChartTemp.Axes[0].Maximum = initialMax;
                }
                else
                {
                    MessageBox.Show("No se encontraron datos para mostrar en el gráfico.");
                }
                UpdateAndLoadForecastDays();
                UpdateAnnotations();
                ChartTemp.Invalidate();
            }
        }

        public async void GetTemperatureAndHumidity(string city)
        {
            if (ChartTempAndHumidity?.Chart == null)
                return;

            ChartTempAndHumidity.Series.Clear();
            
            for (int i = ChartTempAndHumidity.Tools.Count - 1; i >= 0; i--)
            {
                if (ChartTempAndHumidity.Tools[i] is NearestPoint)
                    ChartTempAndHumidity.Tools.RemoveAt(i);
            }
            toolNPHumidity = null;
            toolNPTemp = null;

            ChartTempAndHumidity.Legend.Alignment = LegendAlignments.Bottom;
            var lineTemperature = new Line(ChartTempAndHumidity.Chart)
            {
                Title = "Temperature",
                Smoothed = false,
                Stairs = false,
                VertAxis = VerticalAxis.Left
            };
            lineTemperature.Pointer.Visible = true;
            lineTemperature.Pointer.Style = PointerStyles.Circle;
            lineTemperature.Pointer.HorizSize = 4;
            lineTemperature.Pointer.VertSize = 4;
            lineTemperature.LinePen.Width = 4;


            var lineHumidity = new Line(ChartTempAndHumidity.Chart)
            {
                Title = "Humidity",
                Smoothed = false,
                Stairs = false,
                VertAxis = VerticalAxis.Right
            };
            lineHumidity.Pointer.Visible = true;
            lineHumidity.Pointer.Style = PointerStyles.Rectangle;
            lineHumidity.Pointer.HorizSize = 4;
            lineHumidity.Pointer.VertSize = 4;
            lineHumidity.LinePen.Width = 4;

            Color tempColor = lineTemperature.Color;
            lineTemperature.Color = lineHumidity.Color;
            lineHumidity.Color = tempColor;

            ChartTempAndHumidity.Series.Add(lineTemperature);
            ChartTempAndHumidity.Series.Add(lineHumidity);

            // Crear herramientas NearestPoint (una para cada serie)
            if (toolNPHumidity == null)
            {
                toolNPHumidity = new NearestPoint(ChartTempAndHumidity.Chart)
                {
                    Series = lineHumidity,
                    Style = NearestPointStyles.Rectangle,
                    Size = 5,
                    Brush = {Color = lineHumidity.Color}
                };
                toolNPHumidity.Pen.Visible = true;
                toolNPHumidity.Pen.Color = Color.Black;
                toolNPHumidity.Pen.Width = 2;
                toolNPHumidity.DrawLine = false;
                toolNPHumidity.Pen.Style = Steema.TeeChart.Drawing.DashStyle.Solid;
                ChartTempAndHumidity.Tools.Add(toolNPHumidity);
                
                toolNPHumidity.Change += ToolNPHumidity_Change; 
            }
            if (toolNPTemp == null)
            {
                toolNPTemp = new NearestPoint(ChartTempAndHumidity.Chart)
                {
                    Series = lineTemperature,
                    Style = NearestPointStyles.Circle,
                    Size = 5,
                    Brush = { Color = lineTemperature.Color }

                };

                toolNPTemp.Pen.Visible = true;
                toolNPTemp.Pen.Color = Color.Black;
                toolNPTemp.Pen.Width = 2;
                toolNPTemp.DrawLine = false;
                toolNPTemp.Pen.Style = Steema.TeeChart.Drawing.DashStyle.Solid;
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
                            double horaValor = fechaHora.ToOADate();
                            lineTemperature.Add(horaValor, hora.temp_c);
                            lineHumidity.Add(horaValor, hora.humidity);

                            string iconUrl = $"https:{hora.condition.Icon}";
                            lstIconUrls.Add(iconUrl);
                        }
                    }
                }
            }

            //Configurar formato para mostra la hora
            ChartTempAndHumidity.Axes.Bottom.Labels.Style = AxisLabelStyle.PointValue;
            ChartTempAndHumidity.Axes.Bottom.Labels.DateTimeFormat = "HH:mm";
            ChartTempAndHumidity.Axes.Bottom.Labels.Font.Size = 10;
            ChartTempAndHumidity.Axes.Bottom.Labels.Angle = 45;
           

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
                annotation.Text = $" Temperature: {graficoLiniaTemperature.YValues[toolNPTemp.Point]}ºC \n " +
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

        private void btnHours_Click(object sender, RoutedEventArgs e)
        {
            string city = txtSearch.Text.Trim();

            if (!eventAdded)
            {
                ChartTemp.AfterDraw += ChartTemp_AfterDraw1;
                eventAdded = true;
            }

            if (!string.IsNullOrEmpty(city)) 
            {
                GetAllTemperatures(city); 
                GetCurrentTemperature(city);
                GetTemperatureAndHumidity(city);
            }
        }

        private void ChartTemp_Scroll(object sender, EventArgs e)
        {
            if (ChartTemp.Page.Current >= scrollBarGrafico.Minimum && ChartTemp.Page.Current <= scrollBarGrafico.Maximum)
                scrollBarGrafico.Value = ChartTemp.Page.Current - 1;
        }
    
        public void UpdateAndLoadForecastDays()
        {
            if (allTemperatures == null || allTemperatures.forecastday.Count < 3)
            {
                return;
            }

            int selectedIndex = cmbDays.SelectedIndex; // Save current selection
            string selectedDay = (cmbDays.SelectedItem != null) ? cmbDays.SelectedItem.ToString() : "";

            cmbDays.Items.Clear();

            for (int i = 0; i < 3; i++)
            {
                DateTime fecha = DateTime.Parse(allTemperatures.forecastday[i].date);
                string diaSemana = fecha.ToString("dddd");

                // Translate if exists in the language dictionary
                if (Languages.Language.info.ContainsKey(diaSemana))
                {
                    diaSemana = Languages.Language.info[diaSemana];
                }

                string diaFormateado = $"{diaSemana} ({fecha:dd/MM})";
                string dateKey = fecha.ToString("yyyy-MM-dd");

                diasConFechas[fecha] = diaFormateado;
                cmbDays.Items.Add(diaFormateado);
            }

            // Restore previous selection if it still exists
            if (!string.IsNullOrEmpty(selectedDay) && cmbDays.Items.Contains(selectedDay))
            {
                cmbDays.SelectedItem = selectedDay;
            }
            else if (cmbDays.Items.Count > 0)
            {
                cmbDays.SelectedIndex = selectedIndex >= 0 && selectedIndex < cmbDays.Items.Count ? selectedIndex : 0;
            }
        }

        private void scrollBarGrafico_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            //if (btnDay) return;

            ChartTemp.Axes.Bottom.Automatic = false;

            if (ChartTemp.Series.Count == 0 || ChartTemp.Series[0].Count == 0)
                return;

            // Get the minimum and maximum value of the X-axis according to the position of the ScrollBar
            Bar barSeries = (Bar)ChartTemp.Series[0];
            double minX = barSeries.MinXValue();
            double maxX = barSeries.MaxXValue();
            double visibleRange = (maxX - minX) / 7;

            // Calculate the new display range on the chart
            double newMin = minX + e.NewValue * (maxX - minX - visibleRange) / scrollBarGrafico.Maximum;
            double newMax = newMin + visibleRange;

            // Prevent the range from going out of bounds
            if (newMax > maxX)
            {
                newMax = maxX;
                newMin = newMax - visibleRange;
            }

            // Avoid out-of-range values
            if (newMin < minX)
            {
                newMin = minX;
                newMax = newMin + visibleRange;
            }

            ChartTemp.Axes.Bottom.SetMinMax(newMin, newMax);
            UpdateAnnotations();

            //PART OF THE CMBOX:
            foreach (var kvp in rangoDias)
            {
                // Gets the minimum and maximum values for the selected day in the dictionary.
                double minDay = kvp.Value.Item1;
                double maxDay = kvp.Value.Item2;

                if (newMin >= minDay && newMin <= maxDay)
                {
                    // Gets the formatted name of the corresponding day in ‘daysWithDates’.
                    string formattedDay = diasConFechas[DateTime.Parse(kvp.Key)];

                    // Find the index of the day in the ComboBox.
                    int index = cmbDays.Items.IndexOf(formattedDay);

                    if (index != -1 && cmbDays.SelectedIndex != index)
                    {
                        cmbDays.SelectedIndex = index;
                    }
                    break;
                }
            }

        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSearch_Click_1(sender, e);
            }
        }

        private void cmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLanguage.SelectedItem is ComboBoxItem selectedItem)
            {
                string langFile = selectedItem.Tag.ToString();
                Languages.Language.ChangeLenguage(langFile);

            }
        }

        public void UpdateAnnotations()
        {
            for (int i = ChartTemp.Tools.Count - 1; i >= 0; i--)
            {
                if (ChartTemp.Tools[i] is Steema.TeeChart.Tools.Annotation)
                {
                    ChartTemp.Tools.RemoveAt(i);
                }
            }

            foreach (var verticalLineX in verticalLinePositions)
            {
                int pixelX = ChartTemp.Axes.Bottom.CalcXPosValue(verticalLineX);
                DateTime dateFrom = DateTime.FromOADate(verticalLineX);
                DateTime dateTo = dateFrom.AddDays(1);

                string day1 = diasConFechas.FirstOrDefault(d => d.Key.Date == dateFrom.Date).Value;
                string day2 = diasConFechas.FirstOrDefault(d => d.Key.Date == dateTo.Date).Value;

                day1 = day1?.Trim();
                day2 = day2?.Trim();

                if (day2 != null)
                {
                    //// If the condition is met, then it translates the translation of its respective attribute+
                    //day1 = Language.info.ContainsKey(day1) ? Language.info[day1] : day1;
                    //day2 = Language.info.ContainsKey(day2) ? Language.info[day2] : day2;
                }

                // Annotation of the first day 
                Steema.TeeChart.Tools.Annotation annotationLeft = new Annotation(ChartTemp.Chart);
                annotationLeft.Text = day1;
                annotationLeft.Left = pixelX - 95 - 40;
                annotationLeft.Top = ChartTemp.Axes.Left.IStartPos + 10;
                annotationLeft.Shape.Transparent = true;
                ChartTemp.Tools.Add(annotationLeft);

                // Create second day annotation only if `day2` is not null
                if (day2 != null)
                {
                    Annotation annotationRight = new Annotation(ChartTemp.Chart);
                    annotationRight.Text = day2;
                    annotationRight.Left = pixelX + 24;
                    annotationRight.Top = ChartTemp.Axes.Left.IStartPos + 10;
                    annotationRight.Shape.Transparent = true;
                    ChartTemp.Tools.Add(annotationRight);
                }

            }
        }

    }
}