using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace HFFS3CustomLauncher
{
    public partial class InitializerWindow : Window
    {
        private MainLogic Logic { get; set; }

        public InitializerWindow(MainLogic _logic)
        {
            InitializeComponent();
            Logic = _logic;
            DataContext = Logic;

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
            if (dict != null)
            {
                Resources.MergedDictionaries.Add(dict);
            }

            Language = System.Windows.Markup.XmlLanguage.GetLanguage(Logic.DS.Language + "-" + Logic.DS.Country);

            if (Logic.IsWindowsXP)
            {
                AllowsTransparency = false;
            }
        }

        private void OnMoveWindow(object sender, RoutedEventArgs e)
        {
            if (e is MouseButtonEventArgs mouseEventArgs && mouseEventArgs.LeftButton == MouseButtonState.Pressed && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnClickInitYes(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Logic.Sims3UserFilesDir))
            {
                var dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, 5, new string[] { Logic.Sims3UserFilesDir }));
                dialogueWindow.ShowDialog();
            }
            else
            {
                DialogResult = true;
            }
        }

        private void OnClickInitNo(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult dResult = dialog.ShowDialog();
                if (dResult.ToString().Equals("OK"))
                {
                    Logic.Sims3UserFilesDir = dialog.SelectedPath;
                    if (!Directory.Exists(Logic.Sims3UserFilesDir))
                    {
                        var dialogueWindow = new DialogueWindow(this, new DialogueLogic(Logic.DS, 5, new string[] { Logic.Sims3UserFilesDir }));
                        dialogueWindow.ShowDialog();
                    }
                }
            }
        }

        private void OnClickInitExit(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
