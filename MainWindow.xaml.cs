using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using static HFFS3CustomLauncher.FileLogic;

namespace HFFS3CustomLauncher
{
    public partial class MainWindow : Window
    {
        private void UpdateLanguage()
        {
            ResourceDictionary dict = new ResourceDictionary();
            switch (Logic.DS.LanguageCode)
            {
                case LanguageCode.fr_FR:
                    dict.Source = new Uri("..\\Resources\\StringResources.fr-FR.xaml", UriKind.Relative);
                    break;
                case LanguageCode.de_DE:
                    dict.Source = new Uri("..\\Resources\\StringResources.de-DE.xaml", UriKind.Relative);
                    break;
                default:
                    dict = null;
                    break;
            }

            if (Resources.MergedDictionaries.Count > 0)
            {
                Resources.MergedDictionaries.Clear();
            }

            if (dict != null)
            {
                Resources.MergedDictionaries.Add(dict);
                Logic.DS.CurrLangDict = dict;
            }
            else
            {
                Logic.DS.CurrLangDict = null;
            }

            Logic.UpdateLanguage();
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(Logic.DS.Language + "-" + Logic.DS.Country);
        }

        private void OnChangeLanguage(object sender, RoutedEventArgs e)
        {
            if (e.Source is ComboBox srcCombobox)
            {
                if (srcCombobox.SelectedItem is LanguageData language)
                {
                    Logic.DS.LanguageCode = language.Code;
                    UpdateLanguage();
                }
            }
        }

        private MainLogic Logic { set; get; }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public MainWindow(MainLogic _logic)
        {
            InitializeComponent();
            Logic = _logic;
            DataContext = Logic;
            UpdateLanguage();

            var menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            Action setAlignmentValue = () => {
                if (SystemParameters.MenuDropAlignment && menuDropAlignmentField != null) menuDropAlignmentField.SetValue(null, false);
            };
            setAlignmentValue();

            if (Logic.IsWindowsXP)
            {
                AllowsTransparency = false;
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                Logic.IsMaximized = true;
                Logic.AdaptFullscreenState(this);
            }
            base.OnStateChanged(e);
        }

        private void TopPanel_OnMouseMove(object sender, RoutedEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && !Logic.IsAdaptingWindow && IsMaximized)
            {
                try
                {
                    Point mouseScreenCoordinates = PointToScreen(Mouse.GetPosition(this));
                    Point mouseWindowCoordinates = Mouse.GetPosition(this);
                    double windowHeight = Height == 0 ? 0.1 : Height;
                    double windowWidth = Width == 0 ? 0.1 : Width;
                    Logic.WindowRestoreTopValue = mouseScreenCoordinates.Y - (mouseWindowCoordinates.Y / windowHeight) * Logic.WindowRestoreHeightValue;
                    Logic.WindowRestoreLeftValue = mouseScreenCoordinates.X - (mouseWindowCoordinates.X / windowWidth) * Logic.WindowRestoreWidthValue;

                    Logic.IsMaximized = false;
                    WindowState = WindowState.Normal;
                    Logic.LoadWindowRestoreValues(this);

                    Logic.IsAdaptingWindow = true;
                    if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove();
                }
                catch (Exception)
                {
                }
                finally
                {
                    Logic.IsAdaptingWindow = false;
                }
            }
        }

        private void OnMinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeWindow(object sender, RoutedEventArgs e)
        {
            if (IsMaximized)
            {
                Logic.IsMaximized = false;
                WindowState = WindowState.Normal;
            }
            else
            {
                Logic.IsMaximized = true;
                Logic.SaveWindowRestoreValues(this);
            }
            Logic.AdaptFullscreenState(this);
        }

        private void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnCloseWindow(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int exitCode = 0;
            try
            {
                if (IsLoaded)
                {
                    Logic.StopBackgroundFileCheckerAndUpdater();
                    Logic.SaveSettings(this);
                }
            }
            catch (Exception)
            {
                var dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, 7));
                dialogueWindow.ShowDialog();
                exitCode = 7;
            }
            finally
            {
                Application.Current.Shutdown(exitCode);
            }
        }

        private void OnMoveWindow(object sender, RoutedEventArgs e)
        {
            try
            {
                MouseButtonEventArgs mouseEventArgs = e as MouseButtonEventArgs;
                if (!IsMaximized && mouseEventArgs.LeftButton == MouseButtonState.Pressed)
                {
                    Logic.SaveWindowRestoreValues(this);
                    Logic.IsAdaptingWindow = true;
                    if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                Logic.IsAdaptingWindow = false;
            }

            e.Handled = true;
        }

        private void PreventAnyAction(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void OnPanelButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton)
            {
                switch (srcButton.Name)
                {
                    case "button_Downloads":
                        Logic.SetSelectedMenu(MainLogic.OpenMenu.Downloads);
                        Logic.DownloadsTool.GotUpdate = false;
                        break;
                    case "button_Info":
                        Logic.SetSelectedMenu(MainLogic.OpenMenu.Info);
                        break;
                    case "button_Settings":
                        Logic.SetSelectedMenu(MainLogic.OpenMenu.Settings);
                        break;
                }
            }
        }

        private void OnEditSims3UserFilesDir(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult dResult = dialog.ShowDialog();
                if (dResult.ToString().Equals("OK"))
                {
                    if (!Directory.Exists(dialog.SelectedPath))
                    {
                        var dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, 5, new string[] { dialog.SelectedPath }));
                        dialogueWindow.ShowDialog();
                    }
                    else
                    {
                        Logic.StopBackgroundFileCheckerAndUpdater();

                        try
                        {
                            Logic.SaveSettings(this);
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            Logic.Sims3UserFilesDir = dialog.SelectedPath;
                            Logic.UpdateStoredDirectoriesFile();
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            Logic.LoadLanguage();
                            Logic.LoadSettings(this);
                        }
                        catch (Exception)
                        {
                            var dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, 7, new string[] { dialog.SelectedPath }));
                            dialogueWindow.ShowDialog();
                            Application.Current.Shutdown(7);
                        }

                        Logic.StartBackgroundFileCheckerAndUpdater();
                    }
                }
            }
        }

        private void OnHelp(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton && srcButton.DataContext is FileLogic fileTool)
            {
                fileTool.OpenHelp();
            }
        }

        private void OnFilter(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton && srcButton.DataContext is FileLogic fileTool)
            {
                switch (srcButton.Name)
                {
                    case "button_FilterAll":
                        fileTool.State_SelectedFilter = PrimaryFilter.All;
                        fileTool.ApplyPrimaryFilter();
                        break;
                    case "button_FilterSims3Packs":
                        fileTool.State_SelectedFilter = PrimaryFilter.Sims3Packs;
                        fileTool.ApplyPrimaryFilter();
                        break;
                    case "button_FilterPackages":
                        fileTool.State_SelectedFilter = PrimaryFilter.Packages;
                        fileTool.ApplyPrimaryFilter();
                        break;
                    default:
                        return;
                }
            }
        }

        private void OnSelectAllOrNone(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton && srcButton.DataContext is FileLogic fileTool)
            {
                switch (srcButton.Name)
                {
                    case "button_SelectAll":
                        fileTool.SelectAllOrNone(true);
                        fileTool.RefreshSelectionCount();
                        break;
                    case "button_SelectNone":
                        fileTool.SelectAllOrNone(false);
                        fileTool.RefreshSelectionCount();
                        break;
                    default:
                        return;
                }
            }
        }

        private void OnExpandOrCollapseAll(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton && srcButton.DataContext is FileLogic fileTool)
            {
                switch (srcButton.Name)
                {
                    case "button_ExpandAll":
                        fileTool.ExpandOrCollapseAll(true);
                        break;
                    case "button_CollapseAll":
                        fileTool.ExpandOrCollapseAll(false);
                        break;
                    default:
                        return;
                }
            }
        }

        private void OnDelete(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton && srcButton.DataContext is FileLogic fileTool)
            {
                var dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, true, "Deleting_files", "DeletingFilesText"));
                bool? dResult = dialogueWindow.ShowDialog();
                if (dResult.HasValue && dResult.Value)
                {
                    fileTool.DeleteSelectedFiles();
                }
            }
        }

        private void OnOpenFolder(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton && srcButton.DataContext is FileLogic fileTool)
            {
                try
                {
                    fileTool.OpenFolder();
                }
                catch (Exception)
                {
                    var dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, "Error", "CouldNotOpenFolder"));
                    dialogueWindow.ShowDialog();
                }
            }
        }

        private void OnDecrypt(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton && srcButton.DataContext is FileLogic fileTool)
            {
                Cryption_ResultCode resultCode = fileTool.ModifyEncryption();

                DialogueWindow dialogueWindow;
                switch (resultCode)
                {
                    case Cryption_ResultCode.WrongOperation:
                        dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, 13));
                        break;
                    case Cryption_ResultCode.NoFilesSelected:
                        dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, 15));
                        break;
                    case Cryption_ResultCode.Success:
                        dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, "Success", "Decrypt_SuccessText"));
                        break;
                    default:
                        dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, 10));
                        break;
                }
                dialogueWindow.ShowDialog();
            }
        }

        private void OnExpandOrCollapse(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton && srcButton.DataContext is TS3_File ts3File)
            {
                FileLogic fileTool = GetParentDataContext<FileLogic>(srcButton);
                if (fileTool != null)
                {
                    ts3File.ShowChildren = !ts3File.ShowChildren;
                    fileTool.ShowOrHideChildren(ts3File);
                    fileTool.RefreshExpansionCount();
                }
            }
        }

        private void OnSelect(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton)
            {
                TS3_File ts3File = GetParentDataContext<TS3_File>(srcButton);
                FileLogic fileTool = GetParentDataContext<FileLogic>(srcButton);
                if (fileTool != null)
                {
                    if (srcButton.Name == "button_SelectionCheckbox")
                    {
                        ts3File.IsSelected = !ts3File.IsSelected;
                        fileTool.LastSelection = ts3File;
                    }
                    else
                    {
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        {
                            int currSelectionIndex = fileTool.FilesFrontendColl.IndexOf(ts3File);
                            int lastSelectionIndex = fileTool.LastSelectionIndex;
                            if (currSelectionIndex < 0 || currSelectionIndex >= fileTool.FilesFrontendColl.Count || lastSelectionIndex >= fileTool.FilesFrontendColl.Count)
                            {
                                return;
                            }

                            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                            {
                                fileTool.SelectAllOrNone(false);
                            }

                            if (currSelectionIndex < lastSelectionIndex)
                            {
                                for (int i = currSelectionIndex; i <= lastSelectionIndex; i++)
                                {
                                    fileTool.FilesFrontendColl[i].IsSelected = true;
                                }
                            }
                            else if (currSelectionIndex > lastSelectionIndex)
                            {
                                for (int i = currSelectionIndex; i >= lastSelectionIndex; i--)
                                {
                                    fileTool.FilesFrontendColl[i].IsSelected = true;
                                }
                            }
                            else
                            {
                                ts3File.IsSelected = true;
                            }
                        }
                        else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                        {
                            ts3File.IsSelected = !ts3File.IsSelected;
                            fileTool.LastSelection = ts3File;
                        }
                        else
                        {
                            fileTool.SelectAllOrNone(false);
                            ts3File.IsSelected = true;
                            fileTool.LastSelection = ts3File;
                        }
                    }

                    fileTool.RefreshSelectionCount();
                }
            }
        }

        private void OnSort(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button srcButton && srcButton.DataContext is FileLogicColumn column)
            {
                FileLogic fileTool = GetParentDataContext<FileLogic>(srcButton);
                if (fileTool != null)
                {
                    fileTool.RefreshSort(column);
                }
            }
        }

        private void OnContextMenuSort(object sender, RoutedEventArgs e)
        {
            if (e.Source is MenuItem srcMenuItem && srcMenuItem.DataContext is FileLogicColumn column)
            {
                FileLogic fileTool = GetParentDataContext<FileLogic>(srcMenuItem);
                if (fileTool != null)
                {
                    fileTool.RefreshSort(column, false);
                }
            }
        }

        private void OnContextMenuSortReversed(object sender, RoutedEventArgs e)
        {
            if (e.Source is MenuItem srcMenuItem && srcMenuItem.DataContext is FileLogicColumn column)
            {
                FileLogic fileTool = GetParentDataContext<FileLogic>(srcMenuItem);
                if (fileTool != null)
                {
                    fileTool.RefreshSort(column, true);
                }
            }
        }

        private void OnContextMenuDisplay(object sender, RoutedEventArgs e)
        {
            if (e.Source is MenuItem srcMenuItem && srcMenuItem.DataContext is FileLogicColumn fileLogicColumn)
            {
                FileLogic fileTool = GetParentDataContext<FileLogic>(srcMenuItem);
                if (fileTool != null)
                {
                    DataGrid dataGrid;
                    switch (fileTool.DirName)
                    {
                        case "Downloads":
                            dataGrid = dataGrid_Downloads;
                            break;
                        default:
                            return;
                    }

                    for (int i = 0; i < dataGrid.Columns.Count; i++)
                    {
                        DataGridColumn dataGridColumn = dataGrid.Columns[i];
                        if ((dataGridColumn.Header?.ToString() ?? "") == fileLogicColumn.Key)
                        {
                            dataGridColumn.Visibility = dataGridColumn.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                            fileLogicColumn.Display = !fileLogicColumn.Display;
                        }
                    }
                }
            }
        }

        private void OnContextMenuMouseHover(object sender, RoutedEventArgs e)
        {
            if (e.Source is MenuItem menuItem && menuItem.DataContext is FileLogic fileTool)
            {
                fileTool.AdaptContextMenu(menuItem.Name);
            }
        }

        private T GetParentDataContext<T>(DependencyObject sourceObj)
        {
            DependencyObject currObj = sourceObj;
            while (currObj != null)
            {
                if (currObj is FrameworkElement frEl && frEl.DataContext is T dataContext)
                {
                    return dataContext;
                }
                currObj = VisualTreeHelper.GetParent(currObj);
            }
            return default;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private bool IsMaximized
        {
            get
            {
                if (Logic.IsMaximized || WindowState == WindowState.Maximized)
                {
                    return true;
                }
                return false;
            }
        }

        private void OnOpenHyperlink(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT:
                        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                        break;
                    case PlatformID.Unix:
                        Process.Start("xdg-open", e.Uri.AbsoluteUri);
                        break;
                    case PlatformID.MacOSX:
                        Process.Start("open", e.Uri.AbsoluteUri);
                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        private async void AdaptWindowSizeInBackground(CancellationToken ct, InvisibleWindow invisibleWindow)
        {
            System.Drawing.Point storedMousePos = System.Windows.Forms.Cursor.Position;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool ignoreVerticalOperation = invisibleWindow.IgnoreVerticalOperation;
            bool ignoreHorizontalOperation = invisibleWindow.IgnoreHorizontalOperation;
            try
            {
                while (true)
                {
                    if (System.Windows.Forms.Cursor.Position != storedMousePos)
                    {
                        storedMousePos = System.Windows.Forms.Cursor.Position;
                        sw.Restart();
                    }
                    else if (sw.ElapsedMilliseconds >= 500)
                    {
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (!ignoreVerticalOperation)
                            {
                                if (Top >= invisibleWindow.Top)
                                {
                                    Top = invisibleWindow.Top;
                                    Height = invisibleWindow.ActualHeight;
                                }
                                else
                                {
                                    Height = invisibleWindow.ActualHeight;
                                    Top = invisibleWindow.Top;
                                }
                            }

                            if (!ignoreHorizontalOperation)
                            {
                                if (Left >= invisibleWindow.Left)
                                {
                                    Left = invisibleWindow.Left;
                                    Width = invisibleWindow.ActualWidth;
                                }
                                else
                                {
                                    Width = invisibleWindow.ActualWidth;
                                    Left = invisibleWindow.Left;
                                }
                            }
                        }));

                        sw.Restart();
                    }

                    ct.ThrowIfCancellationRequested();
                    if ((System.Windows.Forms.Control.MouseButtons & System.Windows.Forms.MouseButtons.Left) == 0)
                    {
                        await TaskEx.Delay(3000);
                        ct.ThrowIfCancellationRequested();
                        try { Logic.SaveSettings(this); }
                        catch (Exception) { }
                        Application.Current.Shutdown(-1);
                    }
                }
            }
            catch (Exception)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    if (Top >= invisibleWindow.Top)
                    {
                        Top = invisibleWindow.Top;
                        Height = invisibleWindow.ActualHeight;
                    }
                    else
                    {
                        Height = invisibleWindow.ActualHeight;
                        Top = invisibleWindow.Top;
                    }
                    if (Left >= invisibleWindow.Left)
                    {
                        Left = invisibleWindow.Left;
                        Width = invisibleWindow.ActualWidth;
                    }
                    else
                    {
                        Width = invisibleWindow.ActualWidth;
                        Left = invisibleWindow.Left;
                    }

                    invisibleWindow.Close();
                    IsHitTestVisible = true;
                }));

                sw.Stop();
            }

            await TaskEx.Delay(0);
        }

        private void WindowResize(InvisibleWindow.Operation operation, ResizeDirection resizeDirection, Cursor cursorOverride)
        {
            if (IsMaximized)
            {
                return;
            }

            InvisibleWindow invisibleWindow = null;
            try
            {
                Logic.IsAdaptingWindow = true;

                invisibleWindow = new InvisibleWindow(this, operation, Logic.IsWindowsXP);
                Mouse.OverrideCursor = cursorOverride;

                invisibleWindow.Width = ActualWidth;
                invisibleWindow.Height = ActualHeight;
                invisibleWindow.Top = Top;
                invisibleWindow.Left = Left;
                invisibleWindow.Show();

                CancellationTokenSource cts = new CancellationTokenSource();
                IsHitTestVisible = false;
                Task task = TaskEx.Run(() => AdaptWindowSizeInBackground(cts.Token, invisibleWindow));

                var invHwndSource = PresentationSource.FromVisual(invisibleWindow) as HwndSource;
                SendMessage(invHwndSource.Handle, 0x112, (IntPtr)resizeDirection, IntPtr.Zero);

                cts.Cancel();
                task.Wait();
            }
            catch (Exception)
            {
            }
            finally
            {
                Activate();
                Logic.IsAdaptingWindow = false;
            }
        }
        private void WindowResizeNorth(object sender, MouseButtonEventArgs e)
        {
            WindowResize(InvisibleWindow.Operation.TOP, ResizeDirection.Top, Cursors.SizeNS);
        }

        private void WindowResizeSouth(object sender, MouseButtonEventArgs e)
        {
            WindowResize(InvisibleWindow.Operation.BOTTOM, ResizeDirection.Bottom, Cursors.SizeNS);
        }

        private void WindowResizeWest(object sender, MouseButtonEventArgs e)
        {
            WindowResize(InvisibleWindow.Operation.LEFT, ResizeDirection.Left, Cursors.SizeWE);
        }

        private void WindowResizeEast(object sender, MouseButtonEventArgs e)
        {
            WindowResize(InvisibleWindow.Operation.RIGHT, ResizeDirection.Right, Cursors.SizeWE);
        }

        private void WindowResizeSouthEast(object sender, MouseButtonEventArgs e)
        {
            WindowResize(InvisibleWindow.Operation.BOTTOM_RIGHT, ResizeDirection.BottomRight, Cursors.SizeNWSE);
        }

        private void WindowResizeSouthWest(object sender, MouseButtonEventArgs e)
        {
            WindowResize(InvisibleWindow.Operation.BOTTOM_LEFT, ResizeDirection.BottomLeft, Cursors.SizeNESW);
        }

        private void WindowResizeNorthEast(object sender, MouseButtonEventArgs e)
        {
            WindowResize(InvisibleWindow.Operation.TOP_RIGHT, ResizeDirection.TopRight, Cursors.SizeNESW);
        }

        private void WindowResizeNorthWest(object sender, MouseButtonEventArgs e)
        {
            WindowResize(InvisibleWindow.Operation.TOP_LEFT, ResizeDirection.TopLeft, Cursors.SizeNWSE);
        }

        private enum ResizeDirection { Left = 61441, Right = 61442, Top = 61443, TopLeft = 61444, TopRight = 61445, Bottom = 61446, BottomLeft = 61447, BottomRight = 61448, }

        private void BorderVertical_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!Logic.IsAdaptingWindow && !IsMaximized) Mouse.OverrideCursor = Cursors.SizeWE;
        }

        private void BorderHorizontal_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!Logic.IsAdaptingWindow && !IsMaximized) Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void BorderAll_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!Logic.IsAdaptingWindow && !IsMaximized) Mouse.OverrideCursor = null;
        }

        private void BorderSouthEast_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!Logic.IsAdaptingWindow && !IsMaximized) Mouse.OverrideCursor = Cursors.SizeNWSE;
        }

        private void BorderSouthWest_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!Logic.IsAdaptingWindow && !IsMaximized) Mouse.OverrideCursor = Cursors.SizeNESW;
        }

        private void BorderNorthEast_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!Logic.IsAdaptingWindow && !IsMaximized) Mouse.OverrideCursor = Cursors.SizeNESW;
        }

        private void BorderNorthWest_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!Logic.IsAdaptingWindow && !IsMaximized) Mouse.OverrideCursor = Cursors.SizeNWSE;
        }

        private void General_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!Logic.IsAdaptingWindow) Mouse.OverrideCursor = null;
        }

        private const string LEFT = "PART_LeftHeaderGripper";
        private const string RIGHT = "PART_RightHeaderGripper";
        private Point startPoint;
        private double startWidth;
        private DataGridColumn targetColumn;
        private void DataGridColumnHeader_Loaded(object sender, RoutedEventArgs e)
        {
            var header = (DataGridColumnHeader)sender;

            var thumbLeft = header.Template.FindName(LEFT, header) as Thumb;
            thumbLeft.AddHandler(Thumb.DragStartedEvent, (DragStartedEventHandler)Thumb_DragStarted, true);
            thumbLeft.AddHandler(Thumb.DragCompletedEvent, (DragCompletedEventHandler)Thumb_DragCompleted, true);

            var thumbRight = header.Template.FindName(RIGHT, header) as Thumb;
            thumbRight.AddHandler(Thumb.DragStartedEvent, (DragStartedEventHandler)Thumb_DragStarted, true);
            thumbRight.AddHandler(Thumb.DragCompletedEvent, (DragCompletedEventHandler)Thumb_DragCompleted, true);
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            var thumb = (Thumb)sender;
            DataGrid dg = GetParent<DataGrid>(thumb);
            startPoint = Mouse.GetPosition(dg);

            DataGridColumnHeader header = GetParent<DataGridColumnHeader>(thumb);

            if (thumb.Name == RIGHT)
            {
                targetColumn = header.Column;
            }
            else
            {
                int index = header.Column.DisplayIndex - 1;
                if (index < 0)
                {
                    return;
                }
                targetColumn = null;
                for (int i = 0; i < dg.Columns.Count; i++)
                {
                    if (dg.Columns[i].DisplayIndex == index)
                    {
                        targetColumn = dg.Columns[i];
                        break;
                    }
                }
            }
            startWidth = targetColumn.ActualWidth;
            thumb.PreviewMouseMove += Thumb_PreviewMouseMove;
        }

        private void Thumb_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var thumb = (Thumb)sender;
            if (!thumb.IsDragging || targetColumn == null) { return; }

            e.Handled = true;

            DataGrid dg = GetParent<DataGrid>(thumb);
            Point currentPoint = Mouse.GetPosition(dg);
            double diffX = (currentPoint - startPoint).X;
            double newWidth = Math.Max(targetColumn.MinWidth, Math.Min(startWidth + diffX, targetColumn.MaxWidth));
            var length = new DataGridLength(newWidth);

            targetColumn.SetValue(DataGridColumn.WidthProperty, length);
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            ((Thumb)sender).PreviewMouseMove -= Thumb_PreviewMouseMove;
            targetColumn = null;
        }

        private T GetParent<T>(DependencyObject d) where T : DependencyObject
        {
            T t = null;
            while (t == null) { t = d as T; d = VisualTreeHelper.GetParent(d); }
            return t;
        }
    }
}
