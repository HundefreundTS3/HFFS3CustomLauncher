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
                case LanguageCode.es_ES:
                    dict.Source = new Uri("..\\Resources\\StringResources.es-ES.xaml", UriKind.Relative);
                    break;
                case LanguageCode.ja_JP:
                    dict.Source = new Uri("..\\Resources\\StringResources.ja-JP.xaml", UriKind.Relative);
                    break;
                case LanguageCode.it_IT:
                    dict.Source = new Uri("..\\Resources\\StringResources.it-IT.xaml", UriKind.Relative);
                    break;
                case LanguageCode.ko_KR:
                    dict.Source = new Uri("..\\Resources\\StringResources.ko-KR.xaml", UriKind.Relative);
                    break;
                case LanguageCode.de_DE:
                    dict.Source = new Uri("..\\Resources\\StringResources.de-DE.xaml", UriKind.Relative);
                    break;
                case LanguageCode.zh_TW:
                    dict.Source = new Uri("..\\Resources\\StringResources.zh-TW.xaml", UriKind.Relative);
                    break;
                case LanguageCode.zh_CHS:
                    dict.Source = new Uri("..\\Resources\\StringResources.zh-CHS.xaml", UriKind.Relative);
                    break;
                case LanguageCode.cs_CZ:
                    dict.Source = new Uri("..\\Resources\\StringResources.cs-CZ.xaml", UriKind.Relative);
                    break;
                case LanguageCode.da_DK:
                    dict.Source = new Uri("..\\Resources\\StringResources.da-DK.xaml", UriKind.Relative);
                    break;
                case LanguageCode.nl_NL:
                    dict.Source = new Uri("..\\Resources\\StringResources.nl-NL.xaml", UriKind.Relative);
                    break;
                case LanguageCode.fi_FI:
                    dict.Source = new Uri("..\\Resources\\StringResources.fi-FI.xaml", UriKind.Relative);
                    break;
                case LanguageCode.el_GR:
                    dict.Source = new Uri("..\\Resources\\StringResources.el-GR.xaml", UriKind.Relative);
                    break;
                case LanguageCode.hu_HU:
                    dict.Source = new Uri("..\\Resources\\StringResources.hu-HU.xaml", UriKind.Relative);
                    break;
                case LanguageCode.no:
                    dict.Source = new Uri("..\\Resources\\StringResources.no.xaml", UriKind.Relative);
                    break;
                case LanguageCode.pl_PL:
                    dict.Source = new Uri("..\\Resources\\StringResources.pl-PL.xaml", UriKind.Relative);
                    break;
                case LanguageCode.pt_PT:
                    dict.Source = new Uri("..\\Resources\\StringResources.pt-PT.xaml", UriKind.Relative);
                    break;
                case LanguageCode.ru_RU:
                    dict.Source = new Uri("..\\Resources\\StringResources.ru-RU.xaml", UriKind.Relative);
                    break;
                case LanguageCode.sv_SE:
                    dict.Source = new Uri("..\\Resources\\StringResources.sv-SE.xaml", UriKind.Relative);
                    break;
                case LanguageCode.th_TH:
                    dict.Source = new Uri("..\\Resources\\StringResources.th-TH.xaml", UriKind.Relative);
                    break;
                case LanguageCode.es_MX:
                    dict.Source = new Uri("..\\Resources\\StringResources.es-MX.xaml", UriKind.Relative);
                    break;
                case LanguageCode.pt_BR:
                    dict.Source = new Uri("..\\Resources\\StringResources.pt-BR.xaml", UriKind.Relative);
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
