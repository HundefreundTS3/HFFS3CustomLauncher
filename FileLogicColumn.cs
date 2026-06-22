using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HFFS3CustomLauncher
{
    public class FileLogicColumn : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static DataStore ds = null;
        internal static DataStore DS
        {
            private get
            {
                return ds;
            }
            set
            {
                if (ds == null)
                {
                    ds = value;
                }
            }
        }

        private FileLogicColumnSort ColumnSort { get; set; }

        internal bool SortReversedIsDefault { get; private set; } = false;

        internal string Key { get; private set; }
        public string LocalisedName
        {
            get
            {
                return HideLocalisedName ? "" : DS.GetDynamicResource(Key);
            }
        }
        public string LocalisedNameOverride
        {
            get
            {
                return DS.GetDynamicResource(Key);
            }
        }

        private bool display;
        public bool Display
        {
            get
            {
                return display;
            }
            set
            {
                display = value;
                OnPropertyChanged();
            }
        }

        public bool IsSortActive
        {
            get
            {
                return ColumnSort.ActiveSort == this && !ColumnSort.IsReversed;
            }
            set
            {
            }
        }

        public bool IsSortReversedActive
        {
            get
            {
                return ColumnSort.ActiveSort == this && ColumnSort.IsReversed;
            }
            set
            {
            }
        }

        internal bool HideLocalisedName { get; private set; } = false;
        internal bool IsLocalisedNameColumn { get; private set; } = false;

        internal void UpdateIsSortActive(bool isReversed)
        {
            if (isReversed)
            {
                OnPropertyChanged("IsSortReversedActive");
            }
            else
            {
                OnPropertyChanged("IsSortActive");
            }
        }

        internal void UpdateName()
        {
            OnPropertyChanged("LocalisedName");
        }

        internal readonly Comparison<TS3_File> Comparison;
        internal readonly Func<List<TS3_File>, Comparison<TS3_File>, bool, List<TS3_File>> SortMethodForPhysicalFiles;
        internal readonly Func<List<TS3_Package>, Comparison<TS3_Package>, bool, List<TS3_Package>> SortMethodForPackages;
        internal readonly Action<List<TS3_File>, Collection<TS3_File>, Comparison<TS3_File>, bool> SortMethodForAll;

        internal FileLogicColumn(string key, bool _display, bool sortReversedIsDefaultAndHideLocalisedName, FileLogicColumnSort columnSort, Comparison<TS3_File> comparison, Func<List<TS3_File>, Comparison<TS3_File>, bool, List<TS3_File>> sortMethodForPhysicalFiles, Func<List<TS3_Package>, Comparison<TS3_Package>, bool, List<TS3_Package>> sortMethodForPackages, Action<List<TS3_File>, Collection<TS3_File>, Comparison<TS3_File>, bool> sortMethodForAll) : this(key, _display, columnSort, comparison, sortMethodForPhysicalFiles, sortMethodForPackages, sortMethodForAll)
        {
            if (sortReversedIsDefaultAndHideLocalisedName)
            {
                SortReversedIsDefault = true;
                HideLocalisedName = true;
            }
        }
        internal FileLogicColumn(string key, bool _display, FileLogicColumnSort columnSort, Comparison<TS3_File> comparison, Func<List<TS3_File>, Comparison<TS3_File>, bool, List<TS3_File>> sortMethodForPhysicalFiles, Func<List<TS3_Package>, Comparison<TS3_Package>, bool, List<TS3_Package>> sortMethodForPackages, Action<List<TS3_File>, Collection<TS3_File>, Comparison<TS3_File>, bool> sortMethodForAll)
        {
            Key = key;
            display = _display;
            ColumnSort = columnSort;
            Comparison = comparison;
            SortMethodForPhysicalFiles = sortMethodForPhysicalFiles;
            SortMethodForPackages = sortMethodForPackages;
            SortMethodForAll = sortMethodForAll;
        }

        internal FileLogicColumn(string key, bool _display, FileLogicColumnSort columnSort, Comparison<TS3_File> comparison) : this(key, _display, false, columnSort, comparison) { }
        internal FileLogicColumn(string key, bool _display, bool isLocalisedNameColumn, FileLogicColumnSort columnSort, Comparison<TS3_File> comparison)
        {
            Key = key;
            display = _display;
            IsLocalisedNameColumn = isLocalisedNameColumn;
            ColumnSort = columnSort;
            Comparison = comparison;
            SortMethodForPhysicalFiles = DataStore.ContainedMergeSort;
            SortMethodForPackages = DataStore.ContainedMergeSort;
            SortMethodForAll = DataStore.ContainedMergeSort;
        }
    }
}
