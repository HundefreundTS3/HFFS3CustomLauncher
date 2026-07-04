using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml;

namespace HFFS3CustomLauncher
{
    public class MainLogic : INotifyPropertyChanged
    {
        internal readonly DataStore DS;
        public ObservableCollection<LanguageData> Languages { get; set; } = new ObservableCollection<LanguageData>();
        private LanguageData currLang;
        public LanguageData CurrLang
        {
            get
            {
                return currLang;
            }
            set
            {
                currLang = value;
            }
        }
        internal LanguageData DefaultLang { get; private set; }

        public enum OpenMenu
        {
            None,
            Downloads,
            Info,
            Settings
        }

        private bool isAdaptingWindow = false;
        public bool IsAdaptingWindow
        {
            get
            {
                return isAdaptingWindow;
            }
            set
            {
                isAdaptingWindow = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private Task BackgroundFileCheckerAndUpdaterTask { get; set; } = null;
        private CancellationTokenSource BackgroundFileCheckerAndUpdaterCTS;
        private const int BACKGROUNDFILECHECKANDUPDATE_DELAY = 1000;

        private DecryptionLogic DecryptionTool;
        private FileLogic downloadsTool;
        public FileLogic DownloadsTool
        {
            get { return downloadsTool; }
            private set { downloadsTool = value; }
        }

        private string storedDirectoriesPath;
        private XmlDocument storedDirectoriesDoc;
        private XmlDocument settingsDoc;
        private string userName;

        public bool IsWindowsXP
        {
            get
            {
                return DS.IsWindowsXP;
            }
        }

        private bool isMaximized = false;
        public bool IsMaximized
        {
            get { return isMaximized; }
            set
            {
                isMaximized = value;
                OnPropertyChanged();
            }
        }

        private double windowRestoreTopValue = 0;
        public double WindowRestoreTopValue
        {
            get { return windowRestoreTopValue; }
            set { windowRestoreTopValue = value; }
        }

        private double windowRestoreLeftValue = 0;
        public double WindowRestoreLeftValue
        {
            get { return windowRestoreLeftValue; }
            set { windowRestoreLeftValue = value; }
        }

        private double windowRestoreHeightValue = 650;
        public double WindowRestoreHeightValue
        {
            get { return windowRestoreHeightValue; }
            set { windowRestoreHeightValue = value; }
        }

        private double windowRestoreWidthValue = 855;
        public double WindowRestoreWidthValue
        {
            get { return windowRestoreWidthValue; }
            set { windowRestoreWidthValue = value; }
        }

        private OpenMenu state_SelectedMenu = OpenMenu.None;
        public int State_SelectedMenu
        {
            get
            {
                return (int)state_SelectedMenu;
            }
            set
            {
                state_SelectedMenu = (OpenMenu)value;
                OnPropertyChanged();
            }
        }
        internal void SetSelectedMenu(OpenMenu newMenu)
        {
            state_SelectedMenu = newMenu;
            OnPropertyChanged("State_SelectedMenu");
        }

        private string sims3UserFilesDir;
        public string Sims3UserFilesDir
        {
            get { return sims3UserFilesDir; }
            set
            {
                sims3UserFilesDir = value;
                OnPropertyChanged();
            }
        }

        private string DataDir
        {
            get { return Sims3UserFilesDir + "\\HFCL_Data"; }
        }

        private string SettingsFileDir
        {
            get { return Sims3UserFilesDir + "\\HFCL_Data" + "\\Settings.xml"; }
        }

        public bool ShowTopPanelButtonsOnLeftSide
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.MacOSX;
            }
        }

        internal void StopBackgroundFileCheckerAndUpdater()
        {
            BackgroundFileCheckerAndUpdaterCTS.Cancel();
            BackgroundFileCheckerAndUpdaterTask.Wait();
            DownloadsTool.ResetFiles();
            DownloadsTool.Initialized = false;
        }

        internal void StartBackgroundFileCheckerAndUpdater()
        {
            DownloadsTool.RefreshDirPath(Sims3UserFilesDir);
            BackgroundFileCheckerAndUpdaterCTS = new CancellationTokenSource();
            BackgroundFileCheckerAndUpdaterTask = TaskEx.Run(() => BackgroundFileCheckerAndUpdater(BackgroundFileCheckerAndUpdaterCTS.Token));
        }

        private async void BackgroundFileCheckerAndUpdater(CancellationToken cToken)
        {
            try
            {
                DownloadsTool.CheckAndUpdateFilesInBackground(cToken);
                DownloadsTool.Initialized = true;
                while (true)
                {
                    DownloadsTool.CheckAndUpdateFilesInBackground(cToken);
                    cToken.ThrowIfCancellationRequested();
                    await TaskEx.Delay(BACKGROUNDFILECHECKANDUPDATE_DELAY);
                }
            }
            catch (Exception)
            {
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        internal MainLogic(string _storedDirectoriesPath, string _userName, string language, string country)
        {
            storedDirectoriesPath = _storedDirectoriesPath;

            storedDirectoriesDoc = new XmlDocument();
            if (!File.Exists(_storedDirectoriesPath))
            {
                XmlElement entriesNode = storedDirectoriesDoc.CreateElement("entries");
                storedDirectoriesDoc.AppendChild(entriesNode);
                storedDirectoriesDoc.Save(storedDirectoriesPath);
            }
            else
            {
                storedDirectoriesDoc.Load(_storedDirectoriesPath);
            }

            userName = _userName;
            DecryptionTool = new DecryptionLogic();

            bool isWindowsXP = false;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major == 5)
            {
                isWindowsXP = true;
            }
            DS = new DataStore(language, country, isWindowsXP);

            FileLogicColumn.DS = DS;
            TS3_File.DS = DS;
            LanguageData.DS = DS;
            DownloadsTool = new FileLogic(DecryptionTool, "Downloads", "Help_Download.html", DS);

            DefaultLang = new LanguageData(LanguageCode.en_US, "Lngg_English");
            CurrLang = DefaultLang;
            Languages.Add(DefaultLang);
            Languages.Add(new LanguageData(LanguageCode.de_DE, "Lngg_German"));
            Languages.Add(new LanguageData(LanguageCode.fr_FR, "Lngg_French"));
            Languages.Add(new LanguageData(LanguageCode.cs_CZ, "Lngg_Czech", 50));
            Languages.Add(new LanguageData(LanguageCode.da_DK, "Lngg_Danish", 50));
            Languages.Add(new LanguageData(LanguageCode.el_GR, "Lngg_Greek", 50));
            Languages.Add(new LanguageData(LanguageCode.es_ES, "Lngg_SpanishSpain", 50));
            Languages.Add(new LanguageData(LanguageCode.es_MX, "Lngg_SpanishMexico", 50));
            Languages.Add(new LanguageData(LanguageCode.fi_FI, "Lngg_Finnish", 50));
            Languages.Add(new LanguageData(LanguageCode.hu_HU, "Lngg_Hungarian", 50));
            Languages.Add(new LanguageData(LanguageCode.it_IT, "Lngg_Italian", 50));
            Languages.Add(new LanguageData(LanguageCode.ja_JP, "Lngg_Japanese", 50));
            Languages.Add(new LanguageData(LanguageCode.ko_KR, "Lngg_Korean", 50));
            Languages.Add(new LanguageData(LanguageCode.nl_NL, "Lngg_Dutch", 50));
            Languages.Add(new LanguageData(LanguageCode.no, "Lngg_Norwegian", 50));
            Languages.Add(new LanguageData(LanguageCode.pl_PL, "Lngg_Polish", 50));
            Languages.Add(new LanguageData(LanguageCode.pt_BR, "Lngg_PortugueseBrazil", 50));
            Languages.Add(new LanguageData(LanguageCode.pt_PT, "Lngg_PortuguesePortugal", 50));
            Languages.Add(new LanguageData(LanguageCode.ru_RU, "Lngg_Russian", 50));
            Languages.Add(new LanguageData(LanguageCode.sv_SE, "Lngg_Swedish", 50));
            Languages.Add(new LanguageData(LanguageCode.zh_TW, "Lngg_ChineseTaiwan", 50));
            Languages.Add(new LanguageData(LanguageCode.th_TH, "Lngg_Thai", 5));
            Languages.Add(new LanguageData(LanguageCode.zh_CHS, "Lngg_ChineseChina", 5));
        }

        public void ClearStoredDirectoriesFile()
        {
            storedDirectoriesDoc.RemoveAll();
            XmlElement entriesNode = storedDirectoriesDoc.CreateElement("entries");
            storedDirectoriesDoc.AppendChild(entriesNode);
            storedDirectoriesDoc.Save(storedDirectoriesPath);
        }

        public string GetSims3UserFilesDirectory()
        {
            XmlNodeList entryNodes = storedDirectoriesDoc.DocumentElement.SelectNodes("/entries/entry");

            string directoryToUse = "";
            foreach (XmlNode entryNode in entryNodes)
            {
                if (entryNode.SelectNodes("user").Item(0).InnerText.Equals(userName))
                {
                    directoryToUse = entryNode.SelectNodes("path").Item(0).InnerText;
                    Sims3UserFilesDir = directoryToUse;
                    break;
                }
            }

            return directoryToUse;
        }

        public void UpdateStoredDirectoriesFile()
        {
            XmlNodeList entryNodes = storedDirectoriesDoc.DocumentElement.SelectNodes("/entries/entry");

            bool foundUser = false;
            foreach (XmlNode entryNode in entryNodes)
            {
                if (entryNode.SelectNodes("user").Item(0).InnerText.Equals(userName))
                {
                    foundUser = true;
                    entryNode.SelectNodes("path").Item(0).InnerText = Sims3UserFilesDir;
                    storedDirectoriesDoc.Save(storedDirectoriesPath);
                    break;
                }
            }

            if (!foundUser)
            {
                XmlElement entryNode = storedDirectoriesDoc.CreateElement("entry");
                XmlElement nameNode = storedDirectoriesDoc.CreateElement("user");
                XmlElement pathNode = storedDirectoriesDoc.CreateElement("path");
                nameNode.InnerText = userName;
                pathNode.InnerText = Sims3UserFilesDir;
                entryNode.AppendChild(nameNode);
                entryNode.AppendChild(pathNode);
                storedDirectoriesDoc.DocumentElement.SelectNodes("/entries").Item(0).AppendChild(entryNode);
                storedDirectoriesDoc.Save(storedDirectoriesPath);
            }
        }

        private void CreateSettingsFile()
        {
            settingsDoc = new XmlDocument();
            XmlElement settingsNode = settingsDoc.CreateElement("settings");

            XmlElement windowNode = settingsDoc.CreateElement("window");
            settingsNode.AppendChild(windowNode);

            settingsDoc.AppendChild(settingsNode);
            settingsDoc.Save(SettingsFileDir);
        }

        private void CheckIfOnAScreenAndAdaptOtherwise(MainWindow window)
        {
            double windowLeft = window.Left;
            double windowRight = window.Left + window.Width;
            double windowTop = window.Top;
            double windowBottom = window.Top + window.Height;

            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Rectangle currScreenBounds = Screen.AllScreens[i].WorkingArea;

                double a = 0;
                if (windowRight >= currScreenBounds.Left && windowLeft <= currScreenBounds.Right)
                {
                    a = Math.Min(windowRight, currScreenBounds.Right) - Math.Max(windowLeft, currScreenBounds.Left);
                }

                double b = 0;
                if (windowBottom >= currScreenBounds.Top && windowTop <= currScreenBounds.Bottom)
                {
                    b = Math.Min(windowBottom, currScreenBounds.Bottom) - Math.Max(windowTop, currScreenBounds.Top);
                }

                if (a * b > 0)
                {
                    return;
                }
            }

            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Height = 660;
            window.Width = 865;
        }

        internal void UpdateLanguage()
        {
            DownloadsTool.UpdateLanguage();

            Comparison<LanguageData> comp = new Comparison<LanguageData>((x, y) =>
            {
                if (x.CompletionState != y.CompletionState)
                {
                    return -x.CompletionState.CompareTo(y.CompletionState);
                }
                return x.Name.CompareTo(y.Name);
            });
            DataStore.InsertionSort(Languages, comp);

            LanguageCode currLangCode = DS.LanguageCode;
            for (int i = 0; i < Languages.Count; i++)
            {
                if (Languages[i].Code == currLangCode)
                {
                    CurrLang = Languages[i];
                    break;
                }
            }
        }

        internal void LoadLanguage()
        {
            LanguageCode oldLangCode = DS.LanguageCode;
            if (Directory.Exists(DataDir) && File.Exists(SettingsFileDir))
            {
                settingsDoc = new XmlDocument();
                settingsDoc.Load(SettingsFileDir);

                XmlNode settingsNode = settingsDoc.DocumentElement.SelectNodes("/settings").Item(0);
                if (settingsNode != null)
                {
                    XmlNode cultureNode = settingsNode.SelectNodes("culture").Item(0);
                    if (cultureNode != null)
                    {
                        XmlNode cultureLanguageNode = cultureNode.SelectNodes("language").Item(0);
                        if (cultureLanguageNode != null)
                        {
                            DS.Language = cultureLanguageNode.InnerText;
                        }
                        XmlNode cultureCountryNode = cultureNode.SelectNodes("country").Item(0);
                        if (cultureCountryNode != null)
                        {
                            DS.Country = cultureCountryNode.InnerText;
                        }
                    }
                }
            }

            LanguageCode currLangCode = DS.LanguageCode;
            if (oldLangCode != currLangCode)
            {
                for (int i = 0; i < Languages.Count; i++)
                {
                    if (Languages[i].Code == currLangCode)
                    {
                        CurrLang = Languages[i];
                        break;
                    }
                }
            }
        }
        public void LoadSettings(MainWindow window)
        {
            if (!Directory.Exists(DataDir))
            {
                Directory.CreateDirectory(DataDir);
            }

            if (!File.Exists(SettingsFileDir))
            {
                CreateSettingsFile();
            }
            else
            {
                settingsDoc = new XmlDocument();
                settingsDoc.Load(SettingsFileDir);
            }

            XmlNode settingsNode = settingsDoc.DocumentElement.SelectNodes("/settings").Item(0);
            if (settingsNode == null)
            {
                settingsNode = settingsDoc.CreateElement("settings");
                settingsDoc.DocumentElement.AppendChild(settingsNode);
            }

            XmlNode windowNode = settingsNode.SelectNodes("window").Item(0);
            if (windowNode == null)
            {
                windowNode = settingsDoc.CreateElement("window");
                settingsNode.AppendChild(windowNode);
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                XmlNode windowTopNode = windowNode.SelectNodes("top").Item(0);
                if (windowTopNode != null)
                {
                    try
                    {
                        window.Top = double.Parse(windowTopNode.InnerText);
                    }
                    catch (Exception)
                    {
                    }
                }

                XmlNode windowLeftNode = windowNode.SelectNodes("left").Item(0);
                if (windowLeftNode != null)
                {
                    try
                    {
                        window.Left = double.Parse(windowLeftNode.InnerText);
                    }
                    catch (Exception)
                    {
                    }
                }

                XmlNode windowHeightNode = windowNode.SelectNodes("height").Item(0);
                if (windowHeightNode != null)
                {
                    try
                    {
                        window.Height = double.Parse(windowHeightNode.InnerText);
                    }
                    catch (Exception)
                    {
                    }
                }

                XmlNode windowWidthNode = windowNode.SelectNodes("width").Item(0);
                if (windowWidthNode != null)
                {
                    try
                    {
                        window.Width = double.Parse(windowWidthNode.InnerText);
                    }
                    catch (Exception)
                    {
                    }
                }

                XmlNode windowMaximizedNode = windowNode.SelectNodes("maximized").Item(0);
                if (windowMaximizedNode != null)
                {
                    try
                    {
                        IsMaximized = bool.Parse(windowMaximizedNode.InnerText);
                    }
                    catch (Exception)
                    {
                    }
                }

                CheckIfOnAScreenAndAdaptOtherwise(window);
                SaveWindowRestoreValues(window);
                AdaptFullscreenState(window);

            }

            XmlNode tablesNode = settingsNode.SelectNodes("tables").Item(0);
            if (tablesNode == null)
            {
                tablesNode = settingsDoc.CreateElement("tables");
                settingsNode.AppendChild(tablesNode);
            }

            XmlNode downloadsNode = tablesNode.SelectNodes("downloads").Item(0);
            if (downloadsNode == null)
            {
                downloadsNode = settingsDoc.CreateElement("downloads");
                tablesNode.AppendChild(downloadsNode);
            }

            XmlNode columnsNode = downloadsNode.SelectNodes("columns").Item(0);
            if (columnsNode == null)
            {
                columnsNode = settingsDoc.CreateElement("columns");
                downloadsNode.AppendChild(columnsNode);
            }

            ObservableCollection<DataGridColumn> dataGridColumns = window.dataGrid_Downloads.Columns;
            Dictionary<object, FileLogicColumn> fileLogicColumns = downloadsTool.Columns;
            for (int i = 0; i < dataGridColumns.Count; i++)
            {
                DataGridColumn dataGridColumn = dataGridColumns[i];
                FileLogicColumn fileLogicColumn = fileLogicColumns[dataGridColumn.Header?.ToString() ?? ""];
                string savename = DS.GetColumnSavename(dataGridColumn.Header?.ToString() ?? "");
                if (fileLogicColumn != null && !string.IsNullOrEmpty(savename))
                {
                    XmlNode columnNode = columnsNode.SelectNodes(savename).Item(0);
                    if (columnNode != null)
                    {
                        string display = columnNode.Attributes.GetNamedItem("display")?.Value;
                        if (!String.IsNullOrEmpty(display))
                        {
                            if (bool.TryParse(display, out bool displayValue))
                            {
                                fileLogicColumn.Display = displayValue;
                            }
                        }

                        string width = columnNode.Attributes.GetNamedItem("width")?.Value;
                        if (!String.IsNullOrEmpty(width))
                        {
                            if (double.TryParse(width, out double widthValue))
                            {
                                dataGridColumn.Width = widthValue;
                            }
                        }

                        string order = columnNode.Attributes.GetNamedItem("order")?.Value;
                        if (!String.IsNullOrEmpty(order))
                        {
                            if (int.TryParse(order, out int orderValue))
                            {
                                if (orderValue >= 0)
                                {
                                    dataGridColumn.DisplayIndex = orderValue;
                                }
                            }
                        }
                    }

                    if (!fileLogicColumn.Display)
                    {
                        dataGridColumn.Visibility = Visibility.Collapsed;
                    }
                }
            }

            XmlNode advancedSortOptionsNode = downloadsNode.SelectNodes("advancedsortoptions").Item(0);
            if (advancedSortOptionsNode == null)
            {
                advancedSortOptionsNode = settingsDoc.CreateElement("advancedsortoptions");
                downloadsNode.AppendChild(advancedSortOptionsNode);
            }
            else
            {
                XmlNode keepCollapsableContentAtParentNode = advancedSortOptionsNode.SelectNodes("keepcollapsablecontentatparent").Item(0);
                if (keepCollapsableContentAtParentNode != null && bool.TryParse(keepCollapsableContentAtParentNode.InnerText, out bool adv1Value))
                {
                    downloadsTool.KeepCollapsableContentAtParent = adv1Value;
                }

                XmlNode sortOnlyCollapsableContentNode = advancedSortOptionsNode.SelectNodes("sortonlycollapsablecontent").Item(0);
                if (sortOnlyCollapsableContentNode != null && bool.TryParse(sortOnlyCollapsableContentNode.InnerText, out bool adv2Value))
                {
                    downloadsTool.SortOnlyCollapsableContent = adv2Value;
                }
            }
        }

        public void SaveSettings(MainWindow window)
        {
            if (!IsMaximized)
            {
                SaveWindowRestoreValues(window);
            }

            if (!Directory.Exists(DataDir))
            {
                Directory.CreateDirectory(DataDir);
            }

            XmlNode settingsNode = settingsDoc.DocumentElement.SelectNodes("/settings").Item(0);
            if (settingsNode == null)
            {
                settingsNode = settingsDoc.CreateElement("settings");
                settingsDoc.DocumentElement.AppendChild(settingsNode);
            }

            XmlNode windowNode = settingsNode.SelectNodes("window").Item(0);
            if (windowNode == null)
            {
                windowNode = settingsDoc.CreateElement("window");
                settingsNode.AppendChild(windowNode);
            }

            XmlNode windowTopNode = windowNode.SelectNodes("top").Item(0);
            if (windowTopNode == null)
            {
                windowTopNode = settingsDoc.CreateElement("top");
                windowNode.AppendChild(windowTopNode);
            }
            windowTopNode.InnerText = WindowRestoreTopValue.ToString();

            XmlNode windowLeftNode = windowNode.SelectNodes("left").Item(0);
            if (windowLeftNode == null)
            {
                windowLeftNode = settingsDoc.CreateElement("left");
                windowNode.AppendChild(windowLeftNode);
            }
            windowLeftNode.InnerText = WindowRestoreLeftValue.ToString();

            XmlNode windowHeightNode = windowNode.SelectNodes("height").Item(0);
            if (windowHeightNode == null)
            {
                windowHeightNode = settingsDoc.CreateElement("height");
                windowNode.AppendChild(windowHeightNode);
            }
            windowHeightNode.InnerText = WindowRestoreHeightValue.ToString();

            XmlNode windowWidthNode = windowNode.SelectNodes("width").Item(0);
            if (windowWidthNode == null)
            {
                windowWidthNode = settingsDoc.CreateElement("width");
                windowNode.AppendChild(windowWidthNode);
            }
            windowWidthNode.InnerText = WindowRestoreWidthValue.ToString();

            XmlNode windowMaximizedNode = windowNode.SelectNodes("maximized").Item(0);
            if (windowMaximizedNode == null)
            {
                windowMaximizedNode = settingsDoc.CreateElement("maximized");
                windowNode.AppendChild(windowMaximizedNode);
            }
            windowMaximizedNode.InnerText = IsMaximized.ToString();

            XmlNode cultureNode = settingsNode.SelectNodes("culture").Item(0);
            if (cultureNode == null)
            {
                cultureNode = settingsDoc.CreateElement("culture");
                settingsNode.AppendChild(cultureNode);
            }

            XmlNode cultureLanguageNode = cultureNode.SelectNodes("language").Item(0);
            if (cultureLanguageNode == null)
            {
                cultureLanguageNode = settingsDoc.CreateElement("language");
                cultureNode.AppendChild(cultureLanguageNode);
            }
            cultureLanguageNode.InnerText = DS.Language;

            XmlNode cultureCountryNode = cultureNode.SelectNodes("country").Item(0);
            if (cultureCountryNode == null)
            {
                cultureCountryNode = settingsDoc.CreateElement("country");
                cultureNode.AppendChild(cultureCountryNode);
            }
            cultureCountryNode.InnerText = DS.Country;

            XmlNode tablesNode = settingsNode.SelectNodes("tables").Item(0);
            if (tablesNode == null)
            {
                tablesNode = settingsDoc.CreateElement("tables");
                settingsNode.AppendChild(tablesNode);
            }

            XmlNode downloadsNode = tablesNode.SelectNodes("downloads").Item(0);
            if (downloadsNode == null)
            {
                downloadsNode = settingsDoc.CreateElement("downloads");
                tablesNode.AppendChild(downloadsNode);
            }

            XmlNode columnsNode = downloadsNode.SelectNodes("columns").Item(0);
            if (columnsNode == null)
            {
                columnsNode = settingsDoc.CreateElement("columns");
                downloadsNode.AppendChild(columnsNode);
            }

            ObservableCollection<DataGridColumn> dataGridColumns = window.dataGrid_Downloads.Columns;
            for (int i = 0; i < dataGridColumns.Count; i++)
            {
                DataGridColumn dataGridColumn = dataGridColumns[i];
                string savename = DS.GetColumnSavename(dataGridColumn.Header?.ToString() ?? "");
                if (!string.IsNullOrEmpty(savename))
                {
                    XmlNode columnNode = columnsNode.SelectNodes(savename).Item(0);
                    if (columnNode == null)
                    {
                        columnNode = settingsDoc.CreateElement(savename);
                        columnsNode.AppendChild(columnNode);
                    }

                    XmlAttribute displayAttr = settingsDoc.CreateAttribute("display");
                    displayAttr.Value = dataGridColumn.Visibility == Visibility.Visible ? "true" : "false";
                    columnNode.Attributes.SetNamedItem(displayAttr);

                    XmlAttribute widthAttr = settingsDoc.CreateAttribute("width");
                    widthAttr.Value = dataGridColumn.ActualWidth.ToString();
                    columnNode.Attributes.SetNamedItem(widthAttr);

                    XmlAttribute orderAttr = settingsDoc.CreateAttribute("order");
                    orderAttr.Value = dataGridColumn.DisplayIndex.ToString();
                    columnNode.Attributes.SetNamedItem(orderAttr);
                }
            }

            XmlNode advancedSortOptionsNode = downloadsNode.SelectNodes("advancedsortoptions").Item(0);
            if (advancedSortOptionsNode == null)
            {
                advancedSortOptionsNode = settingsDoc.CreateElement("advancedsortoptions");
                downloadsNode.AppendChild(advancedSortOptionsNode);
            }

            XmlNode keepCollapsableContentAtParentNode = advancedSortOptionsNode.SelectNodes("keepcollapsablecontentatparent").Item(0);
            if (keepCollapsableContentAtParentNode == null)
            {
                keepCollapsableContentAtParentNode = settingsDoc.CreateElement("keepcollapsablecontentatparent");
                advancedSortOptionsNode.AppendChild(keepCollapsableContentAtParentNode);
            }
            keepCollapsableContentAtParentNode.InnerText = downloadsTool.KeepCollapsableContentAtParent.ToString().ToLower();

            XmlNode sortOnlyCollapsableContentNode = advancedSortOptionsNode.SelectNodes("sortonlycollapsablecontent").Item(0);
            if (sortOnlyCollapsableContentNode == null)
            {
                sortOnlyCollapsableContentNode = settingsDoc.CreateElement("sortonlycollapsablecontent");
                advancedSortOptionsNode.AppendChild(sortOnlyCollapsableContentNode);
            }
            sortOnlyCollapsableContentNode.InnerText = downloadsTool.SortOnlyCollapsableContent.ToString().ToLower();

            settingsDoc.Save(SettingsFileDir);
        }

        public void SaveWindowRestoreValues(MainWindow window)
        {
            WindowRestoreTopValue = window.Top;
            WindowRestoreLeftValue = window.Left;
            WindowRestoreHeightValue = window.Height;
            WindowRestoreWidthValue = window.Width;
        }

        public void LoadWindowRestoreValues(MainWindow window)
        {
            window.Top = WindowRestoreTopValue;
            window.Left = WindowRestoreLeftValue;
            window.Height = WindowRestoreHeightValue;
            window.Width = WindowRestoreWidthValue;
        }

        public void AdaptFullscreenState(MainWindow window)
        {
            if (IsMaximized && Screen.AllScreens.Length > 0)
            {
                double windowLeft = window.Left;
                double windowRight = window.Left + window.Width;
                double windowTop = window.Top;
                double windowBottom = window.Top + window.Height;

                double[] monitorScreenPixelCounts = new double[Screen.AllScreens.Length];
                for (int i = 0; i < monitorScreenPixelCounts.Length; i++)
                {
                    Rectangle currScreenBounds = Screen.AllScreens[i].Bounds;
                    double a = 0;
                    if (windowRight >= currScreenBounds.Left && windowLeft <= currScreenBounds.Right)
                    {
                        a = Math.Min(windowRight, currScreenBounds.Right) - Math.Max(windowLeft, currScreenBounds.Left);
                    }
                    double b = 0;
                    if (windowBottom >= currScreenBounds.Top && windowTop <= currScreenBounds.Bottom)
                    {
                        b = Math.Min(windowBottom, currScreenBounds.Bottom) - Math.Max(windowTop, currScreenBounds.Top);
                    }
                    monitorScreenPixelCounts[i] = a * b;
                }

                int selectedMonitorIndex = -1;
                double greatestMonitorScreenPixelCount = 0;
                for (int i = 0; i < monitorScreenPixelCounts.Length; i++)
                {
                    if (monitorScreenPixelCounts[i] > greatestMonitorScreenPixelCount)
                    {
                        greatestMonitorScreenPixelCount = monitorScreenPixelCounts[i];
                        selectedMonitorIndex = i;
                    }
                }

                if (selectedMonitorIndex < 0)
                {
                    window.Top = Screen.PrimaryScreen.WorkingArea.Top;
                    window.Left = Screen.PrimaryScreen.WorkingArea.Left;
                    window.Height = Screen.PrimaryScreen.WorkingArea.Height;
                    window.Width = Screen.PrimaryScreen.WorkingArea.Width;
                }
                else
                {
                    window.Top = Screen.AllScreens[selectedMonitorIndex].WorkingArea.Top;
                    window.Left = Screen.AllScreens[selectedMonitorIndex].WorkingArea.Left;
                    window.Height = Screen.AllScreens[selectedMonitorIndex].WorkingArea.Height;
                    window.Width = Screen.AllScreens[selectedMonitorIndex].WorkingArea.Width;
                }

            }
            else
            {
                IsMaximized = false;
                LoadWindowRestoreValues(window);
            }
        }

        internal static byte[] ReadBuffer(FileStream fs, int readLength, long readPosition)
        {
            fs.Position = readPosition;
            return ReadBuffer(fs, readLength);
        }

        internal static byte[] ReadBuffer(FileStream fs, int readLength)
        {
            byte[] fileReadBuffer = new byte[readLength];
            int globalReadOffset = 0;
            int localReadOffset = 0;
            do
            {
                localReadOffset = fs.Read(fileReadBuffer, globalReadOffset, readLength - globalReadOffset);
                globalReadOffset += localReadOffset;
            } while (localReadOffset > 0);
            return fileReadBuffer;
        }
    }
}
