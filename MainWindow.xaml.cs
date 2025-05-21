using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AppMeteo.Controllers;
using MeteoApp.MODELS;
using Steema.TeeChart;
using Steema.TeeChart.Drawing;
using Steema.TeeChart.Styles;
using Steema.TeeChart.Tools;
using Steema.TeeChart.WPF.Drawing;
using Annotation = Steema.TeeChart.Tools.Annotation;
using Color = System.Drawing.Color;

namespace AppMeteo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region PRIVATE FIELDS
        private WeatherController weatherController;
        private WeatherResponse currentTemperature;
        private Forecast allTemperatures;

        // Dictionaries
        private Dictionary<DateTime, string> daysWithDates = new Dictionary<DateTime, string>();
        private Dictionary<double, string> hoursWithDates = new Dictionary<double, string>();
        private Dictionary<string, List<string>> iconsForDay = new Dictionary<string, List<string>>();
        private Dictionary<string, BitmapImage> imageCacheWPF = new();
        private Dictionary<string, Tuple<double, double>> rankDays = new Dictionary<string, Tuple<double, double>>();

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
        public bool btnDayActivate = true;
        public string currentCity;
        #endregion


        public MainWindow()
        {
            InitializeComponent();
            weatherController = new WeatherController();
            currentTemperature = new WeatherResponse();
            AppMeteo.Languages.Language.ChangeLenguage(Properties.Settings.Default.lang);
            cmbDays.Visibility = Visibility.Collapsed;
            scrollBarChart.Visibility = Visibility.Collapsed;

            ChartTemp.Zoom.Allow = false;
            ChartTempAndHumidity.Zoom.Allow = false;

            ChartTemp.Panning.Allow = ScrollModes.None;
            ChartTempAndHumidity.Panning.Allow = ScrollModes.None;

            #region CONFIGURACION ESTILO DEL CHART
            ChartTemp.Panel.Gradient.Visible = true;
            ChartTemp.Panel.Gradient.StartColor = Color.FromArgb(255, 255, 255);
            ChartTemp.Panel.Gradient.EndColor = Color.FromArgb(250, 252, 255);
            ChartTemp.Panel.Gradient.Direction = Steema.TeeChart.Drawing.LinearGradientMode.Vertical;

            ChartTemp.Axes.Bottom.Grid.Visible = false;
            ChartTemp.Axes.Bottom.Labels.Font.Size = 8;
            ChartTemp.Axes.Bottom.Labels.Font.Bold = true;
            ChartTemp.Axes.Bottom.Labels.Font.Color = Color.FromArgb(80, 102, 120);
            #endregion

            #region CONFIGURACION DEL TITULO DEL CHART
            ChartTemp.Header.Font.Size = 14;
            ChartTemp.Header.Font.Bold = true;
            ChartTemp.Header.Font.Color = Color.FromArgb(80, 102, 120);
            ChartTemp.Header.Font.Name = "Segoe UI";
            ChartTemp.Header.Alignment = (Steema.TeeChart.Drawing.StringAlignment)TextAlignment.Right;
            ChartTemp.Header.Transparent = true;

            ChartTempAndHumidity.Header.Font.Size = 14;
            ChartTempAndHumidity.Header.Font.Bold = true;
            ChartTempAndHumidity.Header.Font.Color = Color.FromArgb(80, 102, 120);
            ChartTempAndHumidity.Header.Font.Name = "Segoe UI";
            ChartTempAndHumidity.Header.Alignment = (Steema.TeeChart.Drawing.StringAlignment)TextAlignment.Right;
            ChartTempAndHumidity.Header.Transparent = true;

            #endregion


        }


        #region UPDATE METHODS
        /// <summary>
        /// Updates the annotations on the chart, adding labels for each vertical line.
        /// </summary>
        public void UpdateAnnotations()
        {
            if (ChartTemp != null)
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

                    string day1 = daysWithDates.FirstOrDefault(d => d.Key.Date == dateFrom.Date).Value;
                    string day2 = daysWithDates.FirstOrDefault(d => d.Key.Date == dateTo.Date).Value;

                    day1 = day1?.Trim();
                    day2 = day2?.Trim();

                    if (day2 != null)
                    {
                        //// If the condition is met, then it translates the translation of its respective attribute+
                        day1 = Languages.Language.info.ContainsKey(day1) ? Languages.Language.info[day1] : day1;
                        day2 = Languages.Language.info.ContainsKey(day2) ? Languages.Language.info[day2] : day2;
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

        /// <summary>
        /// Adds the days of the week in a cmbBox and updates depending on the chosen language.
        /// </summary>
        public void UpdateAndLoadForecastDays()
        {
            if (allTemperatures == null || allTemperatures.forecastday.Count < 3)
            {
                return;
            }

            int selectedIndex = cmbDays.SelectedIndex; 
            string selectedDay = (cmbDays.SelectedItem != null) ? cmbDays.SelectedItem.ToString() : "";

            cmbDays.Items.Clear();

            for (int i = 0; i < 3; i++)
            {
                DateTime date = DateTime.Parse(allTemperatures.forecastday[i].date);
                string dayOfTheWeek = date.ToString("dddd");

                if (Languages.Language.info.ContainsKey(dayOfTheWeek))
                {
                    dayOfTheWeek = Languages.Language.info[dayOfTheWeek];
                }

                string dayFormatted = $"{dayOfTheWeek} ({date:dd/MM})";
                string dateKey = date.ToString("yyyy-MM-dd");

                daysWithDates[date] = dayOfTheWeek;
                cmbDays.Items.Add(dayFormatted);
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

        /// <summary>
        /// Updates the language of the chart headers and series titles in the `tChart2` chart.
        /// It translates the evolution of the day, temperature, and humidity texts based on the selected language.
        /// If translations are not available, it uses default values.
        /// </summary>
        public void UpdateChartLanguage()
        {
            if (ChartTempAndHumidity == null || allTemperatures == null || allTemperatures.forecastday.Count == 0)
                return;

            string headerTxt = Languages.Language.info.ContainsKey("Forecast_by_hour") ? Languages.Language.info["Forecast_by_hour"] : "PREVICIÓ PER HORES";
            ChartTemp.Header.Text = headerTxt;

            string evolutionText = Languages.Language.info.ContainsKey("EVOLUTION_OF_DAY") ? Languages.Language.info["EVOLUTION_OF_DAY"] : "EVOLUCIÓ DEL DIA";
            string tempHumText = Languages.Language.info.ContainsKey("TEMP_HUMIDITY") ? Languages.Language.info["TEMP_HUMIDITY"] : "Temperatura / Humitat relativa";

            ChartTempAndHumidity.Header.Text = $"{evolutionText}: {allTemperatures.forecastday[0].date}";
            ChartTempAndHumidity.SubHeader.Text = tempHumText;

            if (ChartTempAndHumidity.Series.Count >= 2)
            {
                string tempText = Languages.Language.info.ContainsKey("TemperatureTchart2") ? Languages.Language.info["TemperatureTchart2"] : "Temperature (ºC)";
                string humText = Languages.Language.info.ContainsKey("HumidityTchart2") ? Languages.Language.info["HumidityTchart2"] : "Humidity (%)";

                ChartTempAndHumidity.Series[0].Title = tempText;
                ChartTempAndHumidity.Series[1].Title = humText;
            }

            ChartTemp.UpdateLayout();

        }
        #endregion


        #region TEMPERATURE METHODS
        /// <summary>
        ///  Gets the current temperature of the specified city and updates the UI elements.
        /// </summary>
        /// <param name="city">The city for which to get the temperature</param>
        private async void GetCurrentTemperature(string city)
        {
            currentTemperature = await weatherController.GetCurrentTemperatura(city);

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
                txtCity.Text = "City not found";
                txtRegion.Text = "";
                imgWeatherIcon.Source = null;
            }
        }
        

        /// <summary>
        /// Retrieves the 7-day weather forecast for the specified city and displays it in a bar chart.
        /// </summary>
        /// <param name="city">The name of the city to fetch the weather forecast for</param>
        public async void GetAllTemperaturesByDays(string city)
        {
            Bar barSeries;
            btnDayActivate = true;
            currentCity = city;
            cmbDays.Visibility = Visibility.Collapsed;

            if (ChartTemp.Series.Count == 0)
            {
                barSeries = new Bar();

                ChartTemp.Series.Add(barSeries);
                ChartTemp.Legend.Visible = false;
                ChartTemp[0].Marks.Visible = false;
                ChartTemp.Axes.Left.Labels.Visible = false;
            }
            else
            {
                barSeries = (Bar)ChartTemp.Series[0];
            }

            scrollBarChart.Visibility = Visibility.Collapsed;
            barSeries.Clear();
            iconsForDay.Clear();
            daysWithDates.Clear();
            verticalLinePositions.Clear();

            #region CONFIGURACION BAR SERIES
            barSeries.ColorEach = false;
            barSeries.Color = Color.FromArgb(52, 152, 219); // Color principal de las barras
            barSeries.Gradient.Visible = true;
            barSeries.Gradient.StartColor = Color.LightBlue;
            barSeries.Gradient.EndColor = Color.FromArgb(136, 193, 231);
            barSeries.Gradient.Direction = Steema.TeeChart.Drawing.LinearGradientMode.ForwardDiagonal;
            barSeries.Transparency = 20; // Reducimos la transparencia para un aspecto más sólido
            barSeries.BarWidthPercent = 60; // Reducimos el ancho para un aspecto más elegante
            barSeries.BarStyle = BarStyles.RectGradient;
            #endregion

            // Elimina anotaciones anteriores
            for (int i = ChartTemp.Tools.Count - 1; i >= 0; i--)
            {
                if (ChartTemp.Tools[i] is Steema.TeeChart.Tools.Annotation)
                {
                    ChartTemp.Tools.RemoveAt(i);
                }
            }

            if (city == null)
                return;

            allTemperatures = await weatherController.GetPrevisionBy10Days(city);

            if (allTemperatures != null && allTemperatures.forecastday.Count > 0)
            {
                string headerTxt = Languages.Language.info.ContainsKey("FORECAST_7_DAYS") ? Languages.Language.info["FORECAST_7_DAYS"] : "FORECAST(7 DAYS)";
                ChartTemp.Header.Text = headerTxt;

                foreach (Forecastday day in allTemperatures.forecastday)
                {
                    DateTime date = DateTime.Parse(day.date);
                    string dayOfTheWeek = date.ToString("dddd");

                    if (Languages.Language.info.ContainsKey(dayOfTheWeek))
                        dayOfTheWeek = Languages.Language.info[dayOfTheWeek];

                    string dateKey = date.ToString("yyyy-MM-dd");
                    daysWithDates[date] = dayOfTheWeek;
                    barSeries.Add(day.day.avgtemp_c, dayOfTheWeek);

                    string iconUrl = $"https:{day.day.condition.Icon}";
                    iconsForDay[dateKey] = new List<string> { iconUrl };
                }

                ChartTemp.Axes.Bottom.SetMinMax(0, 0); // Forzamos reset
                ChartTemp.Axes.Bottom.Automatic = true;
                ChartTemp.Invalidate();
                ChartTemp.UpdateLayout();
            }

        }
        

        /// <summary>
        /// Retrieves the hourly weather evolution for the specified city and displays it in a bar chart.
        /// </summary>
        /// <param name="city">The name of the city to fetch the weather evolution for.</param>
        public async void GetAllTemperatures(string city)
        {
            Bar barSeries;
            scrollBarChart.Visibility = Visibility.Visible;
            cmbDays.Visibility = Visibility.Visible;
            scrollBarChart.Visibility = Visibility.Visible;


            ChartTemp.Axes.Bottom.Labels.Separation = 90;

            //ChartTemp.Axes.Bottom.Increment = Steema.TeeChart.Utils.GetDateTimeStep(Steema.TeeChart.DateTimeSteps.OneHour);

            btnDayActivate = false;

            verticalLinePositions.Clear();

            if (ChartTemp.Series.Count == 0)
            {
                barSeries = new Bar();
                ChartTemp.Series.Add(barSeries);
                ChartTemp.Legend.Visible = false;
                ChartTemp.Series[0].Marks.Visible = false;
            }
            else
            {
                // Asegurarse de que solo tenemos una serie
                while (ChartTemp.Series.Count > 1)
                {
                    ChartTemp.Series.RemoveAt(1);
                }
                barSeries = (Bar)ChartTemp.Series[0];
                barSeries.Clear();
            }

            ChartTemp.Axes.Left.Labels.Visible = false;

            barSeries.ColorEach = false;
            barSeries.Transparency = 70;
            barSeries.BarWidthPercent = 20;
            barSeries.BarStyle = BarStyles.RectGradient;
            barSeries.Pen.Visible = true;
            barSeries.Pen.Width = 1;

            if (ChartTemp.Series.Count > 0)
            {
                ChartTemp.Series[0].Clear();
                iconsForDay.Clear();
                daysWithDates.Clear();
            }

            string headerTxt = Languages.Language.info.ContainsKey("Forecast_by_hour") ? Languages.Language.info["Forecast_by_hour"] : "FORECAST BY HOUR";
            ChartTemp.Header.Text = headerTxt;


            if (city == null)
                return;

            allTemperatures = await weatherController.GetEvolutionOfWeatherByCity(city);

            if (allTemperatures != null && allTemperatures.forecastday.Count > 0)
            {
                double? firstBarX = null;
                DateTime? lastBarTime = null;
                double? lastBarX = null;
                double? firstBarXNextDay = null;

                foreach (Forecastday dia in allTemperatures.forecastday)
                {
                    DateTime date = DateTime.Parse(dia.date);
                    string dateKey = date.ToString("yyyy-MM-dd");
                    daysWithDates[date] = date.ToString("dddd");

                    List<string> tempIcons = new List<string>();
                    firstBarX = null;

                    foreach (var hora in dia.hour)
                    {
                        DateTime dateHour = DateTime.Parse(hora.time.ToString());

                        if (dateHour < DateTime.Now)
                            continue;
                        double hourValue = dateHour.ToOADate();
                        barSeries.Add(hourValue, hora.temp_c, dateHour.ToString("HH:mm"));
                        hoursWithDates[hourValue] = dateKey;
                        tempIcons.Add($"https:{hora.condition.Icon}");

                        if (firstBarX == null)
                            firstBarX = hourValue;

                        lastBarTime = dateHour;
                        lastBarX = hourValue;

                        rankDays[dateKey] = new Tuple<double, double>(firstBarX ?? 0, lastBarX ?? 0);
                    }

                    if (tempIcons.Count > 0)
                        iconsForDay[dateKey] = tempIcons;

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

                    scrollBarChart.Minimum = 0;
                    scrollBarChart.Maximum = 990;
                    scrollBarChart.Value = 0;
                    scrollBarChart.LargeChange = 10;

                    double initialMax = minX + visibleRange;
                    ChartTemp.Axes.Bottom.SetMinMax(minX, initialMax);
                }
                else
                {
                    MessageBox.Show("No data were found to display in the graph.");
                }

                UpdateAndLoadForecastDays();
                UpdateAnnotations();
                ChartTemp.Invalidate();
            }
        }


        /// <summary>
        /// Obtains and displays the evolution of the temperature and relative humidity of a city on a graph.
        /// </summary>
        /// <param name="city">The city for which you wish to obtain temperature and humidity data</param>
        public async void GetTemperatureAndHumidity(string city)
        {
            if (ChartTempAndHumidity?.Chart == null)
                return;

            SetupChartAppearance();

            ChartTempAndHumidity.Series.Clear();

            for (int i = ChartTempAndHumidity.Tools.Count - 1; i >= 0; i--)
            {
                if (ChartTempAndHumidity.Tools[i] is NearestPoint)
                    ChartTempAndHumidity.Tools.RemoveAt(i);
            }

            toolNPHumidity = null;
            toolNPTemp = null;


            Line lineTemperature = CreateTemperatureSeries();
            Line lineHumidity = CreateHumiditySeries();

            Color tempColor = lineTemperature.Color;

            lineTemperature.Color = lineHumidity.Color;
            lineHumidity.Color = tempColor;

            ChartTempAndHumidity.Series.Add(lineTemperature);
            ChartTempAndHumidity.Series.Add(lineHumidity);

            if (toolNPHumidity == null)
            {
                toolNPHumidity = new NearestPoint(ChartTempAndHumidity.Chart)
                {
                    Series = lineHumidity,
                    Style = NearestPointStyles.Rectangle,
                    Size = 5,
                    Brush = { Color = lineHumidity.Color }
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

            allTemperatures = await weatherController.GetEvolutionOfHumidityAndTemperatureByCity(city);

            ConfigureAnottation();

            if (allTemperatures != null)
            {
                foreach (Forecastday day in allTemperatures.forecastday)
                {
                    string evolutionText = Languages.Language.info.ContainsKey("EVOLUTION_OF_DAY") ? Languages.Language.info["EVOLUTION_OF_DAY"] : "EVOLUTION OF THE DAY";
                    string tempHumText = Languages.Language.info.ContainsKey("TEMP_HUMIDITY") ? Languages.Language.info["TEMP_HUMIDITY"] : "Temperatura / Humitat relativa";

                    ChartTempAndHumidity.Header.Text = $"{evolutionText}: {allTemperatures.forecastday[0].date}";
                    ChartTempAndHumidity.SubHeader.Text = tempHumText;

                    foreach (var hour in day.hour)
                    {
                        DateTime dateHour = DateTime.Parse(hour.time.ToString());
                        if (dateHour.Hour >= 0 && dateHour.Hour <= 9 && (dateHour.Minute == 0 || dateHour.Minute == 30))
                        {
                            double hourValue = dateHour.ToOADate();

                            lineTemperature.XValues.DateTime = true;
                            lineHumidity.XValues.DateTime = true;

                            lineTemperature.Add(hourValue, hour.temp_c);
                            lineHumidity.Add(hourValue, hour.humidity);

                            string iconUrl = $"https:{hour.condition.Icon}";
                            lstIconUrls.Add(iconUrl);
                        }
                    }
                }
            }

            ConfigureAxes();
            
            ChartTempAndHumidity.Axes.Bottom.SetMinMax(0, 0); 
            ChartTempAndHumidity.Axes.Bottom.Automatic = true;
            ChartTempAndHumidity.Invalidate();
            ChartTempAndHumidity.UpdateLayout();
            ChartTempAndHumidity.Invalidate();
        }

        public void ConfigureAxes()
        {
            
            ChartTempAndHumidity.Axes.Bottom.Labels.Style = AxisLabelStyle.Value;
            ChartTempAndHumidity.Axes.Bottom.Labels.ValueFormat = "HH:mm";
            ChartTempAndHumidity.Axes.Bottom.Labels.DateTimeFormat = "HH:mm";
            ChartTempAndHumidity.Axes.Bottom.Labels.Font.Size = 9;
            ChartTempAndHumidity.Axes.Bottom.Labels.Font.Bold = true;
            ChartTempAndHumidity.Axes.Bottom.Labels.Angle = 45;
            ChartTempAndHumidity.Axes.Bottom.Title.Font.Size = 10;
            ChartTempAndHumidity.Axes.Bottom.Title.Font.Bold = true;
            ChartTempAndHumidity.Axes.Bottom.Grid.Color = Color.FromArgb(230, 230, 230);
            
            ChartTempAndHumidity.Axes.Bottom.Increment = 1.0 / 24.0;  // Increment by 1 hour

            ChartTempAndHumidity.Axes.Left.SetMinMax(0, 20);
            ChartTempAndHumidity.Axes.Left.Increment = 5;
            ChartTempAndHumidity.Axes.Left.Automatic = false;
            string tempText = Languages.Language.info.ContainsKey("TemperatureTchart2") ? Languages.Language.info["TemperatureTchart2"] : "Temperature (ºC)";
            ChartTempAndHumidity.Axes.Left.Title.Text = tempText;
            ChartTempAndHumidity.Axes.Left.Title.Font.Size = 10;
            ChartTempAndHumidity.Axes.Left.Title.Font.Bold = true;

            ChartTempAndHumidity.Axes.Right.SetMinMax(0, 100);
            ChartTempAndHumidity.Axes.Right.Increment = 50;
            ChartTempAndHumidity.Axes.Right.Automatic = false;
            string humText = Languages.Language.info.ContainsKey("HumidityTchart2") ? Languages.Language.info["HumidityTchart2"] : "Humidity (%)";
            ChartTempAndHumidity.Axes.Right.Title.Text = humText;
            ChartTempAndHumidity.Axes.Right.Title.Font.Size = 10;
            ChartTempAndHumidity.Axes.Right.Title.Font.Bold = true;

          
            ChartTempAndHumidity.Axes.Bottom.Labels.ExactDateTime = true;

            ChartTempAndHumidity.Axes.Bottom.Maximum = 10;

            ChartTempAndHumidity.Invalidate();
            ChartTempAndHumidity.UpdateLayout();
        }

        private void ConfigureAnottation()
        {
            annotation = new Steema.TeeChart.Tools.Annotation(ChartTempAndHumidity.Chart);

            annotation.Shape.Font.Size = 9;
            annotation.Shape.Font.Bold = false;
            annotation.Shape.Font.Name = "Segoe UI";
            annotation.Shape.Transparent = true;
            annotation.Shape.Shadow.Visible = true;
            annotation.Shape.Shadow.Transparency = 70;

            vertAxis = ChartTempAndHumidity.Axes.Left;
            horizAxis = ChartTempAndHumidity.Axes.Bottom;

            ChartTempAndHumidity.MouseMove += ChartTempAndHumidity_MouseMove;
        }

        private Line CreateTemperatureSeries()
        {
            string titleTemp = Languages.Language.info.ContainsKey("TemperatureTchart2") ? Languages.Language.info["TemperatureTchart2"] : "Temperature (ºC)";
            Line lineTemperature = new Line(ChartTempAndHumidity.Chart)
            {
                Title = titleTemp,
                Smoothed = false,
                Stairs = false,
                VertAxis = VerticalAxis.Left
            };

            lineTemperature.Pointer.Visible = true;
            lineTemperature.Pointer.Style = PointerStyles.Circle;
            lineTemperature.Pointer.HorizSize = 6;
            lineTemperature.Pointer.VertSize = 6;
            lineTemperature.Pointer.Gradient.Visible = true;
            lineTemperature.Pointer.Brush.Gradient.StartColor = Color.White;
            lineTemperature.Pointer.Brush.Gradient.EndColor = Color.FromArgb(220, 53, 69);
            lineTemperature.Pointer.Brush.Gradient.Direction = Steema.TeeChart.Drawing.LinearGradientMode.Vertical;

            lineTemperature.LinePen.Width = 3;
            lineTemperature.LinePen.EndCap = (Steema.TeeChart.Drawing.LineCap)System.Drawing.Drawing2D.LineCap.Round;

            // Añadir marcas en los puntos
            lineTemperature.Marks.Visible = false;
            lineTemperature.Marks.Arrow.Visible = false;
            lineTemperature.Marks.Transparent = true;
            return lineTemperature;
        }

        private Line CreateHumiditySeries()
        {
            string titleHum = Languages.Language.info.ContainsKey("HumidityTchart2") ? Languages.Language.info["HumidityTchart2"] : "Humidity (%)";
            Line lineHumidity = new Line(ChartTempAndHumidity.Chart)
            {
                Title = titleHum,
                Smoothed = false,
                Stairs = false,
                VertAxis = VerticalAxis.Right
            };

            lineHumidity.Pointer.Visible = true;
            lineHumidity.Pointer.Style = PointerStyles.Rectangle;
            lineHumidity.Pointer.HorizSize = 6;
            lineHumidity.Pointer.VertSize = 6;
            lineHumidity.Pointer.Brush.Gradient.Visible = true;
            lineHumidity.Pointer.Brush.Gradient.StartColor = Color.White;
            lineHumidity.Pointer.Brush.Gradient.EndColor = Color.FromArgb(0, 123, 255);
            lineHumidity.Pointer.Brush.Gradient.Direction = (Steema.TeeChart.Drawing.LinearGradientMode)GradientDirection.Radial;

            // Configurar apariencia de línea
            lineHumidity.LinePen.Width = 3;
            lineHumidity.LinePen.Style = Steema.TeeChart.Drawing.DashStyle.Dash;
            lineHumidity.LinePen.EndCap = (Steema.TeeChart.Drawing.LineCap)System.Drawing.Drawing2D.LineCap.Round;

            // Añadir marcas en los puntos
            lineHumidity.Marks.Visible = false;
            lineHumidity.Marks.Arrow.Visible = false;
            lineHumidity.Marks.Transparent = true;
            return lineHumidity;
        }

        private void SetupChartAppearance()
        {
            ChartTempAndHumidity.Legend.Alignment = LegendAlignments.Bottom;
            ChartTempAndHumidity.Legend.Font.Size = 10;
            ChartTempAndHumidity.Legend.Font.Bold = true;

            // Panel y fondo
            ChartTempAndHumidity.Panel.Gradient.Visible = true;
            ChartTempAndHumidity.Panel.Gradient.StartColor = Color.White;
            ChartTempAndHumidity.Panel.Gradient.EndColor = Color.FromArgb(245, 245, 245);
            ChartTempAndHumidity.Panel.Gradient.Direction = Steema.TeeChart.Drawing.LinearGradientMode.Vertical;
            ChartTempAndHumidity.Panel.Bevel.Inner = BevelStyles.None;
            ChartTempAndHumidity.Panel.Bevel.Outer = BevelStyles.None;
            ChartTempAndHumidity.Panel.BorderRound = 10;


            //ChartTempAndHumidity.SubHeader.Text = "Temperatura (°C) / Humedad relativa (%)";
            ChartTempAndHumidity.SubHeader.Font.Size = 10;
            ChartTempAndHumidity.SubHeader.Font.Color = Color.FromArgb(100, 100, 100);

        }
        #endregion


        #region EVENT HANDLERS

        // Search button (executed when the search button is clicked)
        private void btnSearch_Click_1(object sender, RoutedEventArgs e)
        {
            string city = txtSearch.Text.Trim();

            if (!eventAdded)
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

        // Button for displaying hourly temperatures (loads the 3-day graph but displays each day's temperature by hour)
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

                if (ChartTemp.Series.Count > 0 && ChartTemp.Series[0] is Bar barSeries)
                {
                    barSeries.BarWidthPercent = 20;
                    barSeries.BarStyle = BarStyles.RectGradient;
                    ChartTemp.Invalidate(); 
                }
            }
        }

        // Button to display temperatures per day (loads the temperature graph for each day of the week)
        private void btnDays_Click(object sender, RoutedEventArgs e)
        {
            btnDayActivate = true;
            string city = txtSearch.Text.Trim();

            if (!string.IsNullOrEmpty(city))
            {
                GetAllTemperaturesByDays(city);
            }

            if (ChartTemp.Series.Count > 0 && ChartTemp.Series[0] is Bar barSeries)
            {
                barSeries.BarWidthPercent = 80;
                barSeries.BarStyle = BarStyles.RectGradient;
                ChartTemp.Invalidate(); 
            }
        }
        private void txtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSearch_Click_1(sender, e);
            }
        }
        #endregion


        #region TChart DRAWING HANDLERS
        /// <summary>
        /// Draws vertical lines on the chart at specified X-axis positions after the chart is rendered.
        /// </summary>
        private void ChartTemp_AfterDraw1(object sender, Steema.TeeChart.Drawing.IGraphics3D g)
        {
            int offsetX = 34;
            foreach (var verticalLineX in verticalLinePositions)
            {
                int pixelX = ChartTemp.Axes.Bottom.CalcXPosValue(verticalLineX);
                g.Line(pixelX, ChartTemp.Axes.Left.IStartPos, pixelX, ChartTemp.Axes.Left.IEndPos);
            }
        }

        /// <summary>
        ///  Draw the images on the graph for the temperatures per day, and also for the temperatures per hour.
        /// </summary>
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

                Dictionary<string, List<int>> dataGroups = new Dictionary<string, List<int>>();

                for (int i = 0; i < s.Count; i++)
                {
                    string dateKey;

                    // Case 1: Data represents hours (GetAllTemperatures)
                    if (hoursWithDates.ContainsKey(s.XValues[i]))
                    {
                        dateKey = hoursWithDates[s.XValues[i]];
                    }
                    // Case 2: Data represents days (GetAllTemperaturesByDays)
                    else if (daysWithDates.ContainsValue(s.Labels[i]))
                    {
                        dateKey = daysWithDates.FirstOrDefault(x => x.Value == s.Labels[i]).Key.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        continue;
                    }

                    if (!dataGroups.ContainsKey(dateKey))
                        dataGroups[dateKey] = new List<int>();

                    dataGroups[dateKey].Add(i);
                }

                foreach (var group in dataGroups)
                {
                    int p = 0;
                    string dateKey = group.Key;

                    if (!iconsForDay.ContainsKey(dateKey) || iconsForDay[dateKey].Count == 0)
                        continue;

                    foreach (int index in group.Value)
                    {
                        BitmapImage bitmap;
                        if (s.Labels[index].Contains(":"))
                        {
                            int iconIndex = p % iconsForDay[dateKey].Count;
                            bitmap = LoadBitmapFromUrl(iconsForDay[dateKey][iconIndex]);
                        }
                        else
                        {
                            bitmap = LoadBitmapFromUrl(iconsForDay[dateKey][0]);
                        }

                        var tChartImage = new Steema.TeeChart.WPF.Drawing.TImage(bitmap);

                        double dpiScale = VisualTreeHelper.GetDpi(Application.Current.MainWindow).DpiScaleX;

                        int iconWidth = (int)(bitmap.PixelWidth * dpiScale);
                        int iconHeight = (int)(bitmap.PixelHeight * dpiScale);

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

        /// <summary>
        /// Downloads an image from a given URL and returns it as a BitmapImage for use in WPF.
        /// Utilizes a cache to avoid repeated downloads of the same image.
        /// </summary>
        /// <param name="url">The URL of the image to download.</param>
        /// <returns>A BitmapImage loaded from the specified URL.</returns>
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
        #endregion


        #region Language Menu Events
        private void catalanMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UncheckAllLanguageMenuItems();
            catalanMenuItem.IsChecked = true;
            Languages.Language.ChangeLenguage("ca.txt");
        }

        private void spanishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UncheckAllLanguageMenuItems();
            spanishMenuItem.IsChecked = true;
            Languages.Language.ChangeLenguage("es.txt");
        }

        private void englishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UncheckAllLanguageMenuItems();
            englishMenuItem.IsChecked = true;
            Languages.Language.ChangeLenguage("en.txt");

        }

        private void UncheckAllLanguageMenuItems()
        {
            spanishMenuItem.IsChecked = false;
            catalanMenuItem.IsChecked = false;
            englishMenuItem.IsChecked = false;
        }
        #endregion


        #region Chart Interaction and Visualization
        private void ChartTempAndHumidity_MouseMove(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            Point position = e.GetPosition(ChartTempAndHumidity);

            double mouseX = position.X;
            double mouseY = position.Y;

            toolNPHumidity.Active = mouseX >= horizAxis.IStartPos && mouseX <= horizAxis.IEndPos &&
            mouseY >= vertAxis.IStartPos && mouseY <= vertAxis.IEndPos;

            toolNPTemp.Active = mouseX >= horizAxis.IStartPos && mouseX <= horizAxis.IEndPos &&
            mouseY >= vertAxis.IStartPos && mouseY <= vertAxis.IEndPos;

            // Activate annotation if at least one of the tools is active
            annotation.Active = toolNPHumidity.Active || toolNPTemp.Active;
        }

        private void ToolNPHumidity_Change(object? sender, EventArgs e)
        {
            Line graphLineTemperature = (Line)ChartTempAndHumidity.Series[0];
            Line graphLineHumidity = (Line)ChartTempAndHumidity.Series[1];

            string tempText = Languages.Language.info.ContainsKey("Temperature") ? Languages.Language.info["Temperature"] : "Temperature";
            string humText = Languages.Language.info.ContainsKey("Humidity") ? Languages.Language.info["Humidity"] : "Humidity";


            int indexTemp = toolNPTemp.Point;
            int indexHum = toolNPHumidity.Point;

            // Check if the points are valid
            if (indexTemp >= 0 && indexTemp < graphLineTemperature.Count &&
                indexHum >= 0 && indexHum < graphLineHumidity.Count)
            {
                annotation.Text = $" {tempText}: {graphLineTemperature.YValues[toolNPTemp.Point]}ºC \n " +
                              $"{humText}: {graphLineHumidity.YValues[toolNPHumidity.Point]}%";
            }
            else
            {
                annotation.Text = string.Empty;
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

            string city = txtSearch.Text.Trim();

            if (!eventAdded)
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

        private void ChartTemp_Scroll(object sender, EventArgs e)
        {
            if (ChartTemp.Page.Current >= scrollBarChart.Minimum && ChartTemp.Page.Current <= scrollBarChart.Maximum)
                scrollBarChart.Value = ChartTemp.Page.Current - 1;
        }

        private void scrollBarChart_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            ChartTemp.Axes.Bottom.Automatic = false;

            if (ChartTemp.Series.Count == 0 || ChartTemp.Series[0].Count == 0)
                return;

            // Get the minimum and maximum value of the X-axis according to the position of the ScrollBar
            Bar barSeries = (Bar)ChartTemp.Series[0];
            double minX = barSeries.MinXValue();
            double maxX = barSeries.MaxXValue();
            double visibleRange = (maxX - minX) / 7;

            // Calculate the new display range on the chart
            double newMin = minX + e.NewValue * (maxX - minX - visibleRange) / scrollBarChart.Maximum;
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
            foreach (var kvp in rankDays)
            {
                // Gets the minimum and maximum values for the selected day in the dictionary.
                double minDay = kvp.Value.Item1;
                double maxDay = kvp.Value.Item2;

                if (newMin >= minDay && newMin <= maxDay)
                {
                    // Gets the formatted name of the corresponding day in ‘daysWithDates’.
                    string formattedDay = daysWithDates[DateTime.Parse(kvp.Key)];

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

        private void ChartTemp_BeforeDrawSeries(object sender, Steema.TeeChart.Drawing.IGraphics3D g)
        {
            if ((ChartTemp.Series[0] is Bar) && ((ChartTemp.Series[0].LastVisibleIndex - ChartTemp.Series[0].FirstVisibleIndex) > 10))
            {
                ((Bar)(ChartTemp.Series[0])).CustomBarWidth = 33;
            }
            else
                ((Bar)(ChartTemp.Series[0])).CustomBarWidth = 0;
        }

        private void BtnVisitWebsite_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "https://www.weatherapi.com/",
                UseShellExecute = true
            };

            Process.Start(psi);
        }

        //private void scrollBarChart_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    if (ChartTemp.Series.Count == 0 || ChartTemp.Series[0].Count == 0)
        //        return;

        //    // Obtener el rango global
        //    double globalMinX = ChartTemp.Series[0].MinXValue();
        //    double globalMaxX = ChartTemp.Series[0].MaxXValue();
        //    double totalRange = globalMaxX - globalMinX;

        //    // Calcular nuevo rango visible en base al scrollbar
        //    double visibleRange = totalRange / 7; // por ejemplo, mostrar un día de 7
        //    double newMin = globalMinX + scrollBarChart.Value * (totalRange - visibleRange) / scrollBarChart.Maximum;
        //    double newMax = newMin + visibleRange;

        //    if (newMax > globalMaxX)
        //    {
        //        newMax = globalMaxX;
        //        newMin = newMax - visibleRange;
        //    }

        //    if (newMin < globalMinX)
        //    {
        //        newMin = globalMinX;
        //        newMax = newMin + visibleRange;
        //    }

        //    // Aplicar el nuevo rango al gráfico
        //    ChartTemp.Axes.Bottom.SetMinMax(newMin, newMax);
        //    ChartTemp.UpdateLayout();
        //    ChartTemp.Invalidate();
        //    UpdateAnnotations();

        //    // Buscar el día que corresponde al nuevo rango mínimo
        //    foreach (var kvp in rankDays)
        //    {
        //        double minDay = kvp.Value.Item1;
        //        double maxDay = kvp.Value.Item2;

        //        if (newMin >= minDay && newMin <= maxDay)
        //        {
        //            if (DateTime.TryParseExact(kvp.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        //            {
        //                string dayName = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(date.DayOfWeek);
        //                dayName = char.ToUpper(dayName[0]) + dayName.Substring(1); // Capitalize

        //                string displayText = $"{dayName} ({date:dd/MM})";

        //                int index = cmbDays.Items.IndexOf(displayText);

        //                if (index != -1 && cmbDays.SelectedIndex != index)
        //                {
        //                    cmbDays.SelectedIndex = index;
        //                }
        //            }

        //            break;
        //        }
        //    }
        //}


        private void cmbDays_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDays.SelectedItem == null) return;

            string selectedDay = cmbDays.SelectedItem.ToString();

            try
            {
                string fechaStr = selectedDay.Split('(')[1].Split(')')[0];
                if (DateTime.TryParseExact(fechaStr, "dd/MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    string dateKey = parsedDate.ToString("yyyy-MM-dd");

                    if (rankDays.ContainsKey(dateKey))
                    {
                        double minX = rankDays[dateKey].Item1;
                        double maxX = rankDays[dateKey].Item2;

                        ChartTemp.Axes.Bottom.SetMinMax(minX, maxX);
                        double globalMinX = ChartTemp.Series[0].MinXValue();
                        double globalMaxX = ChartTemp.Series[0].MaxXValue();
                        double totalRange = globalMaxX - globalMinX;

                        double selectedRange = minX - globalMinX;
                        int sliderValue = (int)((selectedRange / totalRange) * scrollBarChart.Maximum);

                        if (sliderValue >= scrollBarChart.Minimum && sliderValue <= scrollBarChart.Maximum)
                        {
                            scrollBarChart.Value = sliderValue;
                        }

                        ChartTemp.UpdateLayout();
                        ChartTemp.Invalidate();
                        UpdateAnnotations();
                    }
                }
                else { MessageBox.Show("Error converting the selected date."); }
            }
            catch (Exception ex) { MessageBox.Show($"Incorrect day format: {ex}"); }
        }
        #endregion

    }
}