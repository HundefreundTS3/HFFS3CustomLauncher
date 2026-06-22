using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace HFFS3CustomLauncher
{
    public class FileLogic : INotifyPropertyChanged
    {
        private readonly DataStore DS;
        public enum PrimaryFilter
        {
            Invalid = -1,
            All,
            Sims3Packs,
            Packages
        }

        public Dictionary<object, FileLogicColumn> Columns { get; private set; } = new Dictionary<object, FileLogicColumn>();
        public ObservableCollection<FileLogicColumn> Columns_Main { get; private set; } = new ObservableCollection<FileLogicColumn>();
        public ObservableCollection<FileLogicColumn> Columns_LocalisedNames { get; private set; } = new ObservableCollection<FileLogicColumn>();

        public event PropertyChangedEventHandler PropertyChanged;

        private DecryptionLogic DecryptionTool;

        internal bool Initialized { get; set; } = false;

        internal string DirName { get; private set; }
        private string DirPath { get; set; }

        private string HelpPageTitle { get; set; }

        private bool isLoading = false;
        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                isLoading = value;
                OnPropertyChanged();
                if (!value)
                {
                    RefreshDisplaySelectionAndExpansionCount();
                    OnPropertyChanged("FileLogicLoadOutput");
                }
            }
        }

        private bool IsModidyingFiles { get; set; } = false;

        private bool gotUpdate = false;
        public bool GotUpdate
        {
            get { return gotUpdate; }
            set
            {
                gotUpdate = value;
                OnPropertyChanged();
            }
        }

        internal TS3_File LastSelection { get; set; } = null;
        internal int LastSelectionIndex
        {
            get
            {
                if (LastSelection == null)
                {
                    return 0;
                }
                int index = FilesFrontendColl.IndexOf(LastSelection);
                if (index < 0)
                {
                    index = 0;
                }
                return index;
            }
        }

        private PrimaryFilter state_SelectedFilter = 0;
        public PrimaryFilter State_SelectedFilter
        {
            get { return state_SelectedFilter; }
            set
            {
                state_SelectedFilter = value;
                OnPropertyChanged();
            }
        }
        internal void UpdateLanguage()
        {
            Comparison<FileLogicColumn> comp = new Comparison<FileLogicColumn>((x, y) => x.LocalisedNameOverride.CompareTo(y.LocalisedNameOverride));
            DataStore.InsertionSort(Columns_Main, comp);
            DataStore.InsertionSort(Columns_LocalisedNames, comp);

            foreach (KeyValuePair<object, FileLogicColumn> column in Columns)
            {
                column.Value.UpdateName();
            }

            ResetSort();
        }

        private FileLogicColumnSort ColumnSort { get; set; }
        internal void ResetSort()
        {
            FileLogicColumn oldSortColumn = ColumnSort.ActiveSort;
            bool oldReversedValue = ColumnSort.IsReversed;
            ColumnSort.ActiveSort = null;
            ColumnSort.IsReversed = false;
            oldSortColumn?.UpdateIsSortActive(oldReversedValue);
        }
        internal void RefreshSort(FileLogicColumn newSortColumn)
        {
            if (IsLoading)
            {
                return;
            }

            bool sortReversed = newSortColumn.SortReversedIsDefault;
            if (ColumnSort.ActiveSort == newSortColumn)
            {
                sortReversed = !ColumnSort.IsReversed;
            }

            SortFiles(newSortColumn, sortReversed);
            FileLogicColumn oldSortColumn = ColumnSort.ActiveSort;
            bool oldReversedValue = ColumnSort.IsReversed;
            ColumnSort.ActiveSort = newSortColumn;
            ColumnSort.IsReversed = sortReversed;

            oldSortColumn?.UpdateIsSortActive(oldReversedValue);
            newSortColumn?.UpdateIsSortActive(sortReversed);
        }
        internal void RefreshSort(FileLogicColumn newSortColumn, bool sortReversed)
        {
            if (IsLoading || ColumnSort.ActiveSort == newSortColumn && ColumnSort.IsReversed == sortReversed)
            {
                return;
            }

            SortFiles(newSortColumn, sortReversed);
            FileLogicColumn oldSortColumn = ColumnSort.ActiveSort;
            bool oldReversedValue = ColumnSort.IsReversed;
            ColumnSort.ActiveSort = newSortColumn;
            ColumnSort.IsReversed = sortReversed;

            oldSortColumn?.UpdateIsSortActive(oldReversedValue);
            newSortColumn?.UpdateIsSortActive(sortReversed);
        }


        private bool keepCollapsableContentAtParent = true;
        public bool KeepCollapsableContentAtParent
        {
            get { return keepCollapsableContentAtParent; }
            set
            {
                keepCollapsableContentAtParent = value;
                OnPropertyChanged();

                if (value == false)
                {
                    ResetSort();
                }
                else
                {
                    for (int i = 0; i < FilesFrontendColl.Count; i++)
                    {
                        TS3_File ts3File = FilesFrontendColl[i];
                        if (ts3File.ShowChildren)
                        {
                            for (int j = 0; j < ts3File.Packages.Count; j++)
                            {
                                FilesFrontendColl.Remove(ts3File.Packages[j]);
                            }
                            int ts3FileIndex = FilesFrontendColl.IndexOf(ts3File);
                            for (int j = 0; j < ts3File.Packages.Count; j++)
                            {
                                FilesFrontendColl.Insert(ts3FileIndex + 1 + j, ts3File.Packages[j]);
                            }
                        }
                    }
                }
            }
        }

        private bool sortOnlyCollapsableContent = false;
        public bool SortOnlyCollapsableContent
        {
            get { return sortOnlyCollapsableContent; }
            set
            {
                sortOnlyCollapsableContent = value;
                OnPropertyChanged();

                if (value == false)
                {
                    ResetSort();
                }
            }
        }

        private bool isContextmenuOpen_Display = false;
        public bool IsContextmenuOpen_Display
        {
            get
            {
                return isContextmenuOpen_Display;
            }
            set
            {
                isContextmenuOpen_Display = value;
                OnPropertyChanged();
            }
        }
        private bool isContextmenuOpen_Display_LocalisedNames = false;
        public bool IsContextmenuOpen_Display_LocalisedNames
        {
            get
            {
                return isContextmenuOpen_Display_LocalisedNames;
            }
            set
            {
                isContextmenuOpen_Display_LocalisedNames = value;
                OnPropertyChanged();
            }
        }
        private bool isContextmenuOpen_Sort = false;
        public bool IsContextmenuOpen_Sort
        {
            get
            {
                return isContextmenuOpen_Sort;
            }
            set
            {
                isContextmenuOpen_Sort = value;
                OnPropertyChanged();
            }
        }
        private bool isContextmenuOpen_Sort_Ascending = false;
        public bool IsContextmenuOpen_Sort_Ascending
        {
            get
            {
                if (!IsContextmenuOpen_Sort)
                {
                    return false;
                }
                return isContextmenuOpen_Sort_Ascending;
            }
            set
            {
                isContextmenuOpen_Sort_Ascending = value;
                OnPropertyChanged();
            }
        }
        private bool isContextmenuOpen_Sort_Descending = false;
        public bool IsContextmenuOpen_Sort_Descending
        {
            get
            {
                if (!IsContextmenuOpen_Sort)
                {
                    return false;
                }
                return isContextmenuOpen_Sort_Descending;
            }
            set
            {
                isContextmenuOpen_Sort_Descending = value;
                OnPropertyChanged();
            }
        }
        private bool isContextmenuOpen_Sort_Advanced = false;
        public bool IsContextmenuOpen_Sort_Advanced
        {
            get
            {
                if (!IsContextmenuOpen_Sort)
                {
                    return false;
                }
                return isContextmenuOpen_Sort_Advanced;
            }
            set
            {
                isContextmenuOpen_Sort_Advanced = value;
                OnPropertyChanged();
            }
        }
        private bool isContextmenuOpen_Sort_Ascending_LocalisedNames = false;
        public bool IsContextmenuOpen_Sort_Ascending_LocalisedNames
        {
            get
            {
                if (!IsContextmenuOpen_Sort || !IsContextmenuOpen_Sort_Ascending)
                {
                    return false;
                }
                return isContextmenuOpen_Sort_Ascending_LocalisedNames;
            }
            set
            {
                isContextmenuOpen_Sort_Ascending_LocalisedNames = value;
                OnPropertyChanged();
            }
        }
        private bool isContextmenuOpen_Sort_Descending_LocalisedNames = false;
        public bool IsContextmenuOpen_Sort_Descending_LocalisedNames
        {
            get
            {
                if (!IsContextmenuOpen_Sort || !IsContextmenuOpen_Sort_Descending)
                {
                    return false;
                }
                return isContextmenuOpen_Sort_Descending_LocalisedNames;
            }
            set
            {
                isContextmenuOpen_Sort_Descending_LocalisedNames = value;
                OnPropertyChanged();
            }
        }

        internal void AdaptContextMenu(string menuItemName)
        {
            switch (menuItemName)
            {
                case "ContextMenuItem_Display":
                    IsContextmenuOpen_Sort = false;
                    IsContextmenuOpen_Display = true;
                    break;
                case "ContextMenuItem_Display_LocalisedNames":
                    IsContextmenuOpen_Display_LocalisedNames = true;
                    break;
                case "ContextMenuItem_Sort":
                    IsContextmenuOpen_Display = false;
                    IsContextmenuOpen_Sort = true;
                    break;
                case "ContextMenuItem_Sort_Ascending":
                    IsContextmenuOpen_Sort_Descending = false;
                    IsContextmenuOpen_Sort_Advanced = false;
                    IsContextmenuOpen_Sort_Ascending = true;
                    break;
                case "ContextMenuItem_Sort_Descending":
                    IsContextmenuOpen_Sort_Ascending = false;
                    IsContextmenuOpen_Sort_Advanced = false;
                    IsContextmenuOpen_Sort_Descending = true;
                    break;
                case "ContextMenuItem_Sort_Advanced":
                    IsContextmenuOpen_Sort_Ascending = false;
                    IsContextmenuOpen_Sort_Descending = false;
                    IsContextmenuOpen_Sort_Advanced = true;
                    break;
                case "ContextMenuItem_Sort_Ascending_LocalisedNames":
                    IsContextmenuOpen_Sort_Ascending_LocalisedNames = true;
                    break;
                case "ContextMenuItem_Sort_Descending_LocalisedNames":
                    IsContextmenuOpen_Sort_Descending_LocalisedNames = true;
                    break;
            }
        }

        private int allFilesCount = 0;
        public int AllFilesCount
        {
            get
            {
                return allFilesCount;
            }
            set
            {
                allFilesCount = value;
                OnPropertyChanged();
            }
        }

        private int loadedFilesCount = 0;
        public int LoadedFilesCount
        {
            get
            {
                return loadedFilesCount;
            }
            set
            {
                loadedFilesCount = value;
                OnPropertyChanged();
            }
        }

        public bool DisplaysAnyFile
        {
            get
            {
                return FilesFrontendColl.Count > 0;
            }
        }

        public bool DisplaysAnyExpandableFile
        {
            get
            {
                foreach (TS3_File ts3File in FilesFrontendColl)
                {
                    if (ts3File.HasChildren)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private List<AnyFile> otherFiles = new List<AnyFile>();
        private List<AnyFile> OtherFiles
        {
            get { return otherFiles; }
            set { otherFiles = value; }
        }

        private List<TS3_File> filesBackendList = new List<TS3_File>();
        private List<TS3_File> FilesBackendList
        {
            get { return filesBackendList; }
            set { filesBackendList = value; }
        }
        public ObservableCollection<TS3_File> FilesFrontendColl { get; set; } = new ObservableCollection<TS3_File>();

        public string FileLogicLoadOutput
        {
            get
            {
                int fileCount = FilesBackendList.Count;
                int faultyFileCount = 0;
                for (int i = 0; i < FilesBackendList.Count; i++)
                {
                    if (FilesBackendList[i].IsErroneous)
                    {
                        faultyFileCount++;
                    }
                }
                int faultyUnknownFileCount = 0;
                for (int i = 0; i < OtherFiles.Count; i++)
                {
                    if (OtherFiles[i].IsErroneous)
                    {
                        faultyUnknownFileCount++;
                    }
                }
                int unretaledFileCount = OtherFiles.Count - faultyUnknownFileCount;
                string fragmentA;
                string fragmentB;
                string fragmentC1;
                string fragmentC2;
                string fragmentCAll;
                switch (DS.Language)
                {
                    case "fr":
                        fragmentA = "Liste " + fileCount + " fichier" + (fileCount == 1 ? "" : "s");
                        fragmentB = faultyFileCount == 0 ? "" : "; avec " + faultyFileCount + " erreur" + (faultyFileCount == 1 ? "" : "s");
                        fragmentC1 = unretaledFileCount == 0 ? "" : (unretaledFileCount + " négligeable" + (unretaledFileCount == 1 ? "" : "s"));
                        fragmentC2 = faultyUnknownFileCount == 0 ? "" : faultyUnknownFileCount + " inconnu " + (faultyUnknownFileCount == 1 ? "" : "s") + " avec erreurs";
                        fragmentCAll = unretaledFileCount == 0 && faultyUnknownFileCount == 0 ? "" : " (ignore " + fragmentC1 + (unretaledFileCount == 0 || faultyUnknownFileCount == 0 ? "" : " et ") + fragmentC2 + ")";
                        break;
                    case "de":
                        fragmentA = fileCount + " Datei" + (fileCount == 1 ? "" : "en") + " geladen";
                        fragmentB = faultyFileCount == 0 ? "" : "; mit " + faultyFileCount + " Fehler" + (faultyFileCount == 1 ? "" : "n");
                        fragmentC1 = unretaledFileCount == 0 ? "" : unretaledFileCount + " unbedeutende";
                        fragmentC2 = faultyUnknownFileCount == 0 ? "" : faultyUnknownFileCount + " unbekannte mit Fehlern";
                        fragmentCAll = unretaledFileCount == 0 && faultyUnknownFileCount == 0 ? "" : " (" + fragmentC1 + (unretaledFileCount == 0 || faultyUnknownFileCount == 0 ? "" : " und ") + fragmentC2 + " ausgelassen)";
                        break;
                    default:
                        fragmentA = "Loaded " + fileCount + " file" + (fileCount == 1 ? "" : "s");
                        fragmentB = faultyFileCount == 0 ? "" : "; with " + faultyFileCount + " error" + (faultyFileCount == 1 ? "" : "s");
                        fragmentC1 = unretaledFileCount == 0 ? "" : unretaledFileCount + " unrelated";
                        fragmentC2 = faultyUnknownFileCount == 0 ? "" : faultyUnknownFileCount + " erroneous unknown";
                        fragmentCAll = unretaledFileCount == 0 && faultyUnknownFileCount == 0 ? "" : " (ignoring " + fragmentC1 + (unretaledFileCount == 0 || faultyUnknownFileCount == 0 ? "" : " and ") + fragmentC2 + ")";
                        break;
                }
                return fragmentA + fragmentB + fragmentCAll;
            }
        }

        internal FileLogic(DecryptionLogic decryptionTool, string dirName, string helpPageTitle, DataStore ds)
        {
            DecryptionTool = decryptionTool;
            DirName = dirName;
            HelpPageTitle = helpPageTitle;
            DS = ds;
            ColumnSort = new FileLogicColumnSort();
            Columns.Add(DS.ColumnKey_ExpandCollapse, new FileLogicColumn(DS.ColumnKey_ExpandCollapse, true, true, ColumnSort, (x, y) =>
            {
                int compValueX = 0;
                int compValueY = 0;

                if (x.HasChildren)
                {
                    compValueX++;
                    if (x.ShowChildren)
                    {
                        compValueX++;
                    }
                }
                if (y.HasChildren)
                {
                    compValueY++;
                    if (y.ShowChildren)
                    {
                        compValueY++;
                    }
                }

                return compValueX.CompareTo(compValueY);
            },
            DataStore.SkipSort, DataStore.SkipSort, DataStore.SkipSort));
            Columns.Add(DS.ColumnKey_Select, new FileLogicColumn(DS.ColumnKey_Select, true, true, ColumnSort, (x, y) =>
            {
                int compValueX = 0;
                int compValueY = 0;

                if (x.IsSelected)
                {
                    compValueX += 2;
                }
                else if (x.ContainsSelection)
                {
                    compValueX++;
                }
                if (y.IsSelected)
                {
                    compValueY += 2;
                }
                else if (y.ContainsSelection)
                {
                    compValueY++;
                }

                return compValueX.CompareTo(compValueY);
            }, DataStore.SkipSort, DataStore.SkipSort, DataStore.SkipSort));
            Columns.Add(DS.ColumnKey_Filename, new FileLogicColumn(DS.ColumnKey_Filename, true, ColumnSort, (x, y) => x.PhysicalFilename.CompareTo(y.PhysicalFilename), DataStore.ContainedMergeSort, DataStore.NoSort, DataStore.ContainedMergeSort));
            Columns.Add(DS.ColumnKey_ResourceName, new FileLogicColumn(DS.ColumnKey_ResourceName, false, ColumnSort, (x, y) => x.ResourceName.CompareTo(y.ResourceName), DataStore.NoSort, DataStore.ContainedMergeSort, DataStore.ContainedMergeSort));
            Columns.Add(DS.ColumnKey_Displaytitle, new FileLogicColumn(DS.ColumnKey_Displaytitle, false, ColumnSort, (x, y) => x.Displaytitle.CompareTo(y.Displaytitle)));
            Columns.Add(DS.ColumnKey_Item, new FileLogicColumn(DS.ColumnKey_Item, true, ColumnSort, (x, y) => x.Itemname.CompareTo(y.Itemname)));
            Columns.Add(DS.ColumnKey_Type, new FileLogicColumn(DS.ColumnKey_Type, true, ColumnSort, (x, y) => x.Type.CompareTo(y.Type), DataStore.SkipSort, DataStore.SkipSort, DataStore.SkipSort));
            Columns.Add(DS.ColumnKey_SuperType, new FileLogicColumn(DS.ColumnKey_SuperType, false, ColumnSort, (x, y) => x.SuperTypeText.CompareTo(y.SuperTypeText), DataStore.SkipSort, DataStore.SkipSort, DataStore.SkipSort));
            Columns.Add(DS.ColumnKey_SubType, new FileLogicColumn(DS.ColumnKey_SubType, false, ColumnSort, (x, y) => x.SubType.CompareTo(y.SubType), DataStore.SkipSort, DataStore.SkipSort, DataStore.SkipSort));
            Columns.Add(DS.ColumnKey_Size, new FileLogicColumn(DS.ColumnKey_Size, true, ColumnSort, (x, y) => x.Length.CompareTo(y.Length)));
            Columns.Add(DS.ColumnKey_Encryption, new FileLogicColumn(DS.ColumnKey_Encryption, true, ColumnSort, (x, y) => x.EncryptionStateText.CompareTo(y.EncryptionStateText), DataStore.SkipSort, DataStore.SkipSort, DataStore.SkipSort));
            Columns.Add(DS.ColumnKey_PackageId, new FileLogicColumn(DS.ColumnKey_PackageId, false, ColumnSort, (x, y) => x.PackageId.CompareTo(y.PackageId)));
            Columns.Add(DS.ColumnKey_Date, new FileLogicColumn(DS.ColumnKey_Date, false, ColumnSort, (x, y) => x.Date.CompareTo(y.Date)));
            Columns.Add(DS.ColumnKey_PackageCount, new FileLogicColumn(DS.ColumnKey_PackageCount, false, ColumnSort, (x, y) => x.ContainedPackages.CompareTo(y.ContainedPackages)));
            Columns.Add(DS.ColumnKey_Order, new FileLogicColumn(DS.ColumnKey_Order, false, ColumnSort, (x, y) => x.Order.CompareTo(y.Order), DataStore.NoSort, DataStore.ContainedMergeSort, DataStore.ContainedMergeSort));
            Columns.Add(DS.ColumnKey_PaidContent, new FileLogicColumn(DS.ColumnKey_PaidContent, false, ColumnSort, (x, y) => x.IsPaidContent.CompareTo(y.IsPaidContent), DataStore.SkipSort, DataStore.SkipSort, DataStore.SkipSort));
            Columns.Add(DS.ColumnKey_Gender, new FileLogicColumn(DS.ColumnKey_Gender, false, ColumnSort, (x, y) => x.GenderPriority.CompareTo(y.GenderPriority), DataStore.SkipSort, DataStore.SkipSort, DataStore.SkipSort));
            Columns.Add(DS.ColumnKey_Species, new FileLogicColumn(DS.ColumnKey_Species, false, ColumnSort, (x, y) => x.Species.CompareTo(y.Species), DataStore.SkipSort, DataStore.SkipSort, DataStore.SkipSort));
            Columns.Add(DS.ColumnKey_Age, new FileLogicColumn(DS.ColumnKey_Age, false, ColumnSort, (x, y) =>
            {
                byte xAgeValue = x.AgeValue;
                byte yAgeValue = y.AgeValue;

                for (int i = 0x01; i <= 0x80; i *= 2)
                {
                    byte xBit = (byte)(xAgeValue & i);
                    byte yBit = (byte)(yAgeValue & i);

                    if (xBit != yBit)
                    {
                        i *= 2;
                        if (xBit != 0)
                        {
                            for (; i <= 0x80; i *= 2)
                            {
                                if ((yAgeValue & i) != 0)
                                {
                                    return 1;
                                }
                            }
                            return -1;
                        }
                        else
                        {
                            for (; i <= 0x80; i *= 2)
                            {
                                if ((xAgeValue & i) != 0)
                                {
                                    return -1;
                                }
                            }
                            return 1;
                        }
                    }
                }

                return 0;
            }, DataStore.SkipSort, DataStore.SkipSort, DataStore.SkipSort));
            Columns.Add(DS.ColumnKey_Image, new FileLogicColumn(DS.ColumnKey_Image, false, ColumnSort, (x, y) =>
            {
                if (!x.HasThumbnail && !y.HasThumbnail)
                {
                    return 0;
                }
                if (!x.HasThumbnail && y.HasThumbnail)
                {
                    return -1;
                }
                if (x.HasThumbnail && !y.HasThumbnail)
                {
                    return 1;
                }

                int sizeCompValue = x.Thumbnail.Length.CompareTo(y.Thumbnail.Length);
                if (sizeCompValue != 0)
                {
                    return sizeCompValue;
                }

                int length = x.Thumbnail.Length;
                for (int i = 0; i < length; i++)
                {
                    int diff = x.Thumbnail[i] - y.Thumbnail[i];
                    if (Math.Abs(diff) > 0xF)
                    {
                        return x.Thumbnail[i].CompareTo(y.Thumbnail[i]);
                    }
                }

                return 0;
            }));
            Columns.Add(DS.ColumnKey_EnglishName, new FileLogicColumn(DS.ColumnKey_EnglishName, false, true, ColumnSort, (x, y) => x.EnglishName.CompareTo(y.EnglishName)));
            Columns.Add(DS.ColumnKey_FrenchName, new FileLogicColumn(DS.ColumnKey_FrenchName, false, true, ColumnSort, (x, y) => x.FrenchName.CompareTo(y.FrenchName)));
            Columns.Add(DS.ColumnKey_SpanishSpainName, new FileLogicColumn(DS.ColumnKey_SpanishSpainName, false, true, ColumnSort, (x, y) => x.SpanishSpainName.CompareTo(y.SpanishSpainName)));
            Columns.Add(DS.ColumnKey_JapaneseName, new FileLogicColumn(DS.ColumnKey_JapaneseName, false, true, ColumnSort, (x, y) => x.JapaneseName.CompareTo(y.JapaneseName)));
            Columns.Add(DS.ColumnKey_ItalianName, new FileLogicColumn(DS.ColumnKey_ItalianName, false, true, ColumnSort, (x, y) => x.ItalianName.CompareTo(y.ItalianName)));
            Columns.Add(DS.ColumnKey_KoreanName, new FileLogicColumn(DS.ColumnKey_KoreanName, false, true, ColumnSort, (x, y) => x.KoreanName.CompareTo(y.KoreanName)));
            Columns.Add(DS.ColumnKey_GermanName, new FileLogicColumn(DS.ColumnKey_GermanName, false, true, ColumnSort, (x, y) => x.GermanName.CompareTo(y.GermanName)));
            Columns.Add(DS.ColumnKey_ChineseTaiwanName, new FileLogicColumn(DS.ColumnKey_ChineseTaiwanName, false, true, ColumnSort, (x, y) => x.ChineseTaiwanName.CompareTo(y.ChineseTaiwanName)));
            Columns.Add(DS.ColumnKey_ChineseChinaName, new FileLogicColumn(DS.ColumnKey_ChineseChinaName, false, true, ColumnSort, (x, y) => x.ChineseChinaName.CompareTo(y.ChineseChinaName)));
            Columns.Add(DS.ColumnKey_CzechName, new FileLogicColumn(DS.ColumnKey_CzechName, false, true, ColumnSort, (x, y) => x.CzechName.CompareTo(y.CzechName)));
            Columns.Add(DS.ColumnKey_DanishName, new FileLogicColumn(DS.ColumnKey_DanishName, false, true, ColumnSort, (x, y) => x.DanishName.CompareTo(y.DanishName)));
            Columns.Add(DS.ColumnKey_DutchName, new FileLogicColumn(DS.ColumnKey_DutchName, false, true, ColumnSort, (x, y) => x.DutchName.CompareTo(y.DutchName)));
            Columns.Add(DS.ColumnKey_FinnishName, new FileLogicColumn(DS.ColumnKey_FinnishName, false, true, ColumnSort, (x, y) => x.FinnishName.CompareTo(y.FinnishName)));
            Columns.Add(DS.ColumnKey_GreekName, new FileLogicColumn(DS.ColumnKey_GreekName, false, true, ColumnSort, (x, y) => x.GreekName.CompareTo(y.GreekName)));
            Columns.Add(DS.ColumnKey_HungarianName, new FileLogicColumn(DS.ColumnKey_HungarianName, false, true, ColumnSort, (x, y) => x.HungarianName.CompareTo(y.HungarianName)));
            Columns.Add(DS.ColumnKey_NorwegianName, new FileLogicColumn(DS.ColumnKey_NorwegianName, false, true, ColumnSort, (x, y) => x.NorwegianName.CompareTo(y.NorwegianName)));
            Columns.Add(DS.ColumnKey_PolishName, new FileLogicColumn(DS.ColumnKey_PolishName, false, true, ColumnSort, (x, y) => x.PolishName.CompareTo(y.PolishName)));
            Columns.Add(DS.ColumnKey_PortuguesePortugalName, new FileLogicColumn(DS.ColumnKey_PortuguesePortugalName, false, true, ColumnSort, (x, y) => x.PortuguesePortugalName.CompareTo(y.PortuguesePortugalName)));
            Columns.Add(DS.ColumnKey_RussianName, new FileLogicColumn(DS.ColumnKey_RussianName, false, true, ColumnSort, (x, y) => x.RussianName.CompareTo(y.RussianName)));
            Columns.Add(DS.ColumnKey_SwedishName, new FileLogicColumn(DS.ColumnKey_SwedishName, false, true, ColumnSort, (x, y) => x.SwedishName.CompareTo(y.SwedishName)));
            Columns.Add(DS.ColumnKey_ThaiName, new FileLogicColumn(DS.ColumnKey_ThaiName, false, true, ColumnSort, (x, y) => x.ThaiName.CompareTo(y.ThaiName)));
            Columns.Add(DS.ColumnKey_SpanishMexicoName, new FileLogicColumn(DS.ColumnKey_SpanishMexicoName, false, true, ColumnSort, (x, y) => x.SpanishMexicoName.CompareTo(y.SpanishMexicoName)));
            Columns.Add(DS.ColumnKey_PortugueseBrazilName, new FileLogicColumn(DS.ColumnKey_PortugueseBrazilName, false, true, ColumnSort, (x, y) => x.PortugueseBrazilName.CompareTo(y.PortugueseBrazilName)));

            foreach (KeyValuePair<object, FileLogicColumn> column in Columns)
            {
                FileLogicColumn currColumn = column.Value;
                if (currColumn.IsLocalisedNameColumn)
                {
                    Columns_LocalisedNames.Add(currColumn);
                }
                else if (!currColumn.HideLocalisedName)
                {
                    Columns_Main.Add(currColumn);
                }
            }

            UpdateLanguage();
        }


        internal void OpenHelp()
        {
            try
            {
                string helpPagePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "HelpPages" + Path.DirectorySeparatorChar + "ENG_US" + Path.DirectorySeparatorChar + HelpPageTitle;
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT:
                        Process.Start(new ProcessStartInfo(helpPagePath) { UseShellExecute = true });
                        break;
                    case PlatformID.Unix:
                        Process.Start("xdg-open", helpPagePath);
                        break;
                    case PlatformID.MacOSX:
                        Process.Start("open", helpPagePath);
                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        internal void RefreshDirPath(string newSims3UserFilesDir)
        {
            DirPath = newSims3UserFilesDir + Path.DirectorySeparatorChar + DirName;
        }

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void RefreshDisplaySelectionAndExpansionCount()
        {
            OnPropertyChanged("DisplaysAnyFile");
            OnPropertyChanged("DisplaysAnyExpandableFile");
            RefreshSelectionCount();
            RefreshExpansionCount();
        }
        internal void RefreshSelectionCount()
        {
            OnPropertyChanged("SelectedAnyFile");
            OnPropertyChanged("SelectedPhysicalFile");
            if (ColumnSort.ActiveSort == Columns[DS.ColumnKey_Select])
            {
                ResetSort();
            }
        }

        internal void RefreshExpansionCount()
        {
            OnPropertyChanged("ExpandedAnyFile");

            if (ColumnSort.ActiveSort == Columns[DS.ColumnKey_ExpandCollapse])
            {
                ResetSort();
            }
        }

        private void AddToFilesFrontendCollRegardingPrimaryFilter(TS3_File ts3File)
        {
            switch (State_SelectedFilter)
            {
                case PrimaryFilter.All:
                    FilesFrontendColl.Add(ts3File);
                    AddChildrenToFilesFrontendColl(ts3File);
                    break;
                case PrimaryFilter.Sims3Packs:
                    if (ts3File is TS3_Sims3Pack)
                    {
                        FilesFrontendColl.Add(ts3File);
                        AddChildrenToFilesFrontendColl(ts3File);
                    }
                    break;
                case PrimaryFilter.Packages:
                    if (ts3File is TS3_Package)
                    {
                        FilesFrontendColl.Add(ts3File);
                        AddChildrenToFilesFrontendColl(ts3File);
                    }
                    break;
            }
        }

        private void AddChildrenToFilesFrontendColl(TS3_File ts3File)
        {
            if (ts3File.ShowChildren)
            {
                if (ts3File is TS3_Sims3Pack sims3Pack)
                {
                    for (int i = 0; i < sims3Pack.Packages.Count; i++)
                    {
                        FilesFrontendColl.Add(sims3Pack.Packages[i]);
                    }
                }
            }
        }

        internal void ApplyPrimaryFilter()
        {
            FilesFrontendColl.Clear();
            for (int i = 0; i < FilesBackendList.Count; i++)
            {
                AddToFilesFrontendCollRegardingPrimaryFilter(FilesBackendList[i]);
            }
            ResetSort();
            RefreshDisplaySelectionAndExpansionCount();
        }

        private void SortFiles(FileLogicColumn newSortColumn, bool sortReversed)
        {
            if (FilesFrontendColl.Count <= 0 || newSortColumn == null)
            {
                return;
            }

            Comparison<TS3_File> comparison;
            Func<List<TS3_File>, Comparison<TS3_File>, bool, List<TS3_File>> sortMethodForPhysicalFiles;
            Func<List<TS3_Package>, Comparison<TS3_Package>, bool, List<TS3_Package>> sortMethodForPackages;
            Action<List<TS3_File>, Collection<TS3_File>, Comparison<TS3_File>, bool> sortMethodForAll;
            if (newSortColumn == ColumnSort.ActiveSort)
            {
                comparison = (x, y) => 0;
                sortMethodForPhysicalFiles = (sourceList, _, __) =>
                {
                    if (sourceList.Count > 0)
                    {
                        List<TS3_File> destList = new List<TS3_File>(sourceList.Count);
                        for (int i = sourceList.Count - 1; i >= 0; i--)
                        {
                            destList.Add(sourceList[i]);
                        }
                        return destList;
                    }
                    return new List<TS3_File>();
                };
                sortMethodForPackages = (sourceList, _, __) =>
                {
                    if (sourceList.Count > 0)
                    {
                        List<TS3_Package> destList = new List<TS3_Package>(sourceList.Count);
                        for (int i = sourceList.Count - 1; i >= 0; i--)
                        {
                            destList.Add(sourceList[i]);
                        }
                        return destList;
                    }
                    return new List<TS3_Package>();
                };
                sortMethodForAll = (sourceList, destColl, _, __) =>
                {
                    destColl.Clear();
                    if (sourceList.Count > 0)
                    {
                        for (int i = sourceList.Count - 1; i >= 0; i--)
                        {
                            destColl.Add(sourceList[i]);
                        }
                    }
                };
            }
            else
            {
                comparison = newSortColumn.Comparison;
                sortMethodForPhysicalFiles = newSortColumn.SortMethodForPhysicalFiles;
                sortMethodForPackages = newSortColumn.SortMethodForPackages;
                sortMethodForAll = newSortColumn.SortMethodForAll;
            }

            if (SortOnlyCollapsableContent)
            {
                sortMethodForPhysicalFiles = DataStore.NoSort;
            }

            if (KeepCollapsableContentAtParent)
            {
                List<TS3_File> filesFrontendColl_Copy = new List<TS3_File>();
                for (int i = 0; i < FilesFrontendColl.Count; i++)
                {
                    TS3_File ts3File = FilesFrontendColl[i];
                    if (ts3File.HasChildren)
                    {
                        ts3File.Packages = sortMethodForPackages.Invoke(ts3File.Packages, comparison, sortReversed);
                    }
                    if (!ts3File.HasParent)
                    {
                        filesFrontendColl_Copy.Add(ts3File);
                    }
                }

                FilesFrontendColl.Clear();
                filesFrontendColl_Copy = sortMethodForPhysicalFiles.Invoke(filesFrontendColl_Copy, comparison, sortReversed);

                for (int i = 0; i < filesFrontendColl_Copy.Count; i++)
                {
                    TS3_File ts3File = filesFrontendColl_Copy[i];
                    FilesFrontendColl.Add(ts3File);
                    if (ts3File.ShowChildren)
                    {
                        for (int j = 0; j < ts3File.Packages.Count; j++)
                        {
                            FilesFrontendColl.Add(ts3File.Packages[j]);
                        }
                    }
                }
            }
            else
            {
                sortMethodForAll.Invoke(new List<TS3_File>(FilesFrontendColl), FilesFrontendColl, comparison, sortReversed);
                for (int i = 0; i < FilesFrontendColl.Count; i++)
                {
                    TS3_File ts3File = FilesFrontendColl[i];
                    if (ts3File.HasChildren)
                    {
                        ts3File.Packages = sortMethodForPackages.Invoke(ts3File.Packages, comparison, sortReversed);
                    }
                }
            }
        }

        public bool SelectedAnyFile
        {
            get
            {
                for (int i = 0; i < FilesFrontendColl.Count; i++)
                {
                    if (FilesFrontendColl[i].ContainsSelection)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool SelectedPhysicalFile
        {
            get
            {
                for (int i = 0; i < FilesFrontendColl.Count; i++)
                {
                    TS3_File ts3File = FilesFrontendColl[i];
                    if (ts3File.IsSelected && !ts3File.HasParent)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool ExpandedAnyFile
        {
            get
            {
                for (int i = 0; i < FilesFrontendColl.Count; i++)
                {
                    if (FilesFrontendColl[i].ShowChildren)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal void DeleteSelectedFiles()
        {
            string directory = DirPath;

            for (int i = 0; i < FilesFrontendColl.Count; i++)
            {
                TS3_File ts3File = FilesFrontendColl[i];
                if (!ts3File.HasParent && ts3File.IsSelected)
                {
                    File.Delete(directory + Path.DirectorySeparatorChar + ts3File.PhysicalFilename);
                    FilesBackendList.Remove(ts3File);
                    for (int j = 0; j < ts3File.Packages.Count; j++)
                    {
                        FilesFrontendColl.Remove(ts3File.Packages[j]);
                    }
                    FilesFrontendColl.Remove(ts3File);
                }
            }
        }

        internal void OpenFolder()
        {
            try
            {
                if (Directory.Exists(DirPath))
                {
                    Process.Start(DirPath);
                    return;
                }
            }
            catch (Exception)
            {
            }

            throw new Exception("Could not find or open " + DirName + " folder.");
        }

        internal void ResetFiles()
        {
            FilesFrontendColl.Clear();
            FilesBackendList.Clear();
            OtherFiles.Clear();
        }

        internal void CheckAndUpdateFilesInBackground(CancellationToken cToken/*, bool doThoroughCheck, bool indicateUpdate*/)
        {
            if (IsModidyingFiles)
            {
                return;
            }

            FileInfo[] dirFiles;
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(DirPath);
                dirFiles = dirInfo.GetFiles();
            }
            catch (Exception)
            {
                if (FilesFrontendColl.Count > 0)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        FilesFrontendColl.Clear();
                    }));
                }
                FilesBackendList.Clear();
                OtherFiles.Clear();
                return;
            }

            // Check files
            List<TS3_File> filesToRemove = new List<TS3_File>();
            int filesToInsert = dirFiles.Length;
            for (int i = 0; i < FilesBackendList.Count; i++)
            {
                TS3_File ts3File = FilesBackendList[i];

                bool fileStillExists = false;
                for (int j = 0; j < dirFiles.Length; j++)
                {
                    FileInfo dirFile = dirFiles[j];
                    if (dirFile != null && dirFile.Name == ts3File.Filename && dirFile.LastWriteTime == ts3File.LastWriteTime)
                    {
                        fileStillExists = true;
                        dirFiles[j] = null;
                        filesToInsert--;
                        break;
                    }
                }
                if (!fileStillExists)
                {
                    filesToRemove.Add(ts3File);
                }
            }
            bool removedOtherFile = false;
            for (int i = 0; i < OtherFiles.Count; i++)
            {
                AnyFile otherFile = OtherFiles[i];

                bool fileStillExists = false;
                for (int j = 0; j < dirFiles.Length; j++)
                {
                    FileInfo dirFile = dirFiles[j];
                    if (dirFile != null && dirFile.Name == otherFile.Filename && dirFile.LastWriteTime == otherFile.LastWriteTime)
                    {
                        fileStillExists = true;
                        dirFiles[j] = null;
                        filesToInsert--;
                        break;
                    }
                }
                if (!fileStillExists)
                {
                    OtherFiles.Remove(otherFile);
                    removedOtherFile = true;
                }
            }

            cToken.ThrowIfCancellationRequested();

            //Update files
            if (filesToInsert > 0 || filesToRemove.Count > 0)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    IsLoading = true;
                    LoadedFilesCount = 0;
                    AllFilesCount = filesToInsert + filesToRemove.Count;
                    if (filesToInsert > 0)
                    {
                        RefreshSort(null);
                    }
                }));

                if (filesToRemove.Count > 0)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        foreach (TS3_File ts3File in filesToRemove)
                        {
                            FilesFrontendColl.Remove(ts3File);
                            if (ts3File.ShowChildren)
                            {
                                for (int i = 0; i < ts3File.Packages.Count; i++)
                                {
                                    FilesFrontendColl.Remove(ts3File.Packages[i]);
                                }
                            }
                        }
                        LoadedFilesCount++;
                    }));
                    foreach (TS3_File ts3File in filesToRemove)
                    {
                        FilesBackendList.Remove(ts3File);
                    }
                }

                bool insertedTS3File = false;
                if (filesToInsert > 0)
                {
                    foreach (FileInfo dirFile in dirFiles)
                    {
                        if (dirFile == null)
                        {
                            continue;
                        }
                        cToken.ThrowIfCancellationRequested();
                        insertedTS3File = InitializeTS3FileInBackground(dirFile) || insertedTS3File;
                    }
                }

                if (Initialized && insertedTS3File)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        IsLoading = false;
                        GotUpdate = true;
                    }));
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => IsLoading = false));
                }
            }
            else if (removedOtherFile)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => OnPropertyChanged("FileLogicLoadOutput")));
            }
        }

        private bool InitializeTS3FileInBackground(FileInfo file)
        {
            try
            {
                using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] fileReadBuffer = MainLogic.ReadBuffer(fs, 4);
                    switch (TS3_File.GetTS3FileType(fileReadBuffer, file.Length))
                    {
                        case TS3_FileType.Sims3Pack:
                            TS3_Sims3Pack newSims3Pack;
                            try
                            {
                                if (file.Length == TS3_Sims3Pack.EMPTY_PACK_LENGTH)
                                {
                                    newSims3Pack = new TS3_Sims3Pack(file, fs);
                                }
                                else
                                {
                                    newSims3Pack = new TS3_Sims3Pack(file, fs, DecryptionTool);
                                }
                            }
                            catch (Exception)
                            {
                                newSims3Pack = new TS3_Sims3Pack(false, file);
                            }

                            FilesBackendList.Add(newSims3Pack);
                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                                AddToFilesFrontendCollRegardingPrimaryFilter(newSims3Pack);
                                LoadedFilesCount++;
                            }));
                            break;
                        case TS3_FileType.Package:
                            TS3_Package newPackage;
                            try
                            {
                                newPackage = new TS3_Package(file, fs, DecryptionTool);
                            }
                            catch (Exception)
                            {
                                newPackage = new TS3_Package(false, file);
                            }

                            FilesBackendList.Add(newPackage);
                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                                AddToFilesFrontendCollRegardingPrimaryFilter(newPackage);
                                LoadedFilesCount++;
                            }));
                            break;
                        default:
                            AnyFile otherFile = new AnyFile(file.Name, file.LastWriteTime);

                            OtherFiles.Add(otherFile);
                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LoadedFilesCount++));
                            return false;
                    }
                }
            }
            catch (Exception)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    OtherFiles.Add(new AnyFile(false, file.Name, file.LastWriteTime));
                    LoadedFilesCount++;
                }));
                return false;
            }
            return true;
        }

        internal void ShowOrHideChildren(TS3_File ts3File)
        {
            if (ts3File.ShowChildren)
            {
                try
                {
                    int afterParentIndex = FilesFrontendColl.IndexOf(ts3File) + 1;
                    for (int i = 0; i < ts3File.Packages.Count; i++)
                    {
                        FilesFrontendColl.Insert(afterParentIndex + i, ts3File.Packages[i]);
                    }
                }
                catch (Exception)
                {
                }

                if (!KeepCollapsableContentAtParent)
                {
                    ResetSort();
                }
            }
            else
            {
                for (int i = ts3File.Packages.Count - 1; i >= 0; i--)
                {
                    FilesFrontendColl.Remove(ts3File.Packages[i]);
                }
            }
        }

        internal Cryption_ResultCode ModifyEncryption()
        {
            string directory = DirPath;

            int selectionCount = 0;
            foreach (TS3_File file in FilesFrontendColl)
            {
                if (file.ContainsSelection)
                {
                    selectionCount++;
                }
            }
            if (selectionCount <= 0)
            {
                return Cryption_ResultCode.NoFilesSelected;
            }

            IsModidyingFiles = true;
            foreach (TS3_File file in FilesFrontendColl)
            {
                if (file.HasParent || !file.ContainsSelection)
                {
                    continue;
                }

                DecryptionTool.Decrypt(directory + Path.DirectorySeparatorChar + file.Filename, file);
                try
                {
                    file.LastWriteTime = new FileInfo(directory + Path.DirectorySeparatorChar + file.Filename).LastWriteTime;
                }
                catch (Exception)
                {
                }

                if (file is TS3_Sims3Pack sims3Pack)
                {
                    foreach (TS3_Package package in sims3Pack.Packages)
                    {
                        switch (package.EncryptionState)
                        {
                            case FileEncryptionState.None:
                                break;
                            case FileEncryptionState.Present:
                                break;
                            case FileEncryptionState.Unknown:
                                break;
                            case FileEncryptionState.Error_AlreadyUnencrypted:
                                package.EncryptionState = FileEncryptionState.None;
                                break;
                            default:
                                package.EncryptionState = FileEncryptionState.Pending;
                                break;
                        }
                    }
                    sims3Pack.ReevaluateEncryptionState();
                }
                else if (file is TS3_Package package)
                {
                    switch (package.EncryptionState)
                    {
                        case FileEncryptionState.None:
                            break;
                        case FileEncryptionState.Present:
                            break;
                        case FileEncryptionState.Unknown:
                            break;
                        case FileEncryptionState.Error_AlreadyUnencrypted:
                            package.EncryptionState = FileEncryptionState.None;
                            break;
                        default:
                            package.EncryptionState = FileEncryptionState.Pending;
                            break;
                    }
                }
            }

            IsModidyingFiles = false;
            return Cryption_ResultCode.Success;
        }

        internal void SelectAllOrNone(bool value)
        {
            for (int i = 0; i < FilesFrontendColl.Count; i++)
            {
                FilesFrontendColl[i].IsSelected = value;
            }
        }

        internal void ExpandOrCollapseAll(bool value)
        {
            List<TS3_File> filesFrontendColl_Copy = new List<TS3_File>(FilesFrontendColl.Count);
            for (int i = 0; i < FilesFrontendColl.Count; i++)
            {
                TS3_File ts3File = FilesFrontendColl[i];
                if (!ts3File.HasParent)
                {
                    filesFrontendColl_Copy.Add(ts3File);
                }
            }
            FilesFrontendColl.Clear();

            if (value)
            {
                for (int i = 0; i < filesFrontendColl_Copy.Count; i++)
                {
                    TS3_File ts3File = filesFrontendColl_Copy[i];
                    FilesFrontendColl.Add(ts3File);
                    ts3File.ShowChildren = true;
                    for (int j = 0; j < ts3File.Packages.Count; j++)
                    {
                        FilesFrontendColl.Add(ts3File.Packages[j]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < filesFrontendColl_Copy.Count; i++)
                {
                    TS3_File ts3File = filesFrontendColl_Copy[i];
                    FilesFrontendColl.Add(ts3File);
                    ts3File.ShowChildren = false;
                }
            }

            RefreshExpansionCount();
            if (!KeepCollapsableContentAtParent && value == true)
            {
                ResetSort();
            }
        }
    }
}
