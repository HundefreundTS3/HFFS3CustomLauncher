using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;

namespace HFFS3CustomLauncher
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            int errorCode = 1;
            MainLogic logic = null;
            try
            {
                string userName = Environment.UserName;
                string userDocPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Unix:
                        userDocPath += Path.DirectorySeparatorChar + "Documents";
                        break;
                    case PlatformID.MacOSX:
                        userDocPath += Path.DirectorySeparatorChar + "Documents";
                        break;
                }

                errorCode = 2;
                string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                errorCode = 3;
                string directoryToUse = string.Empty;
                string[] currUICultureData = CultureInfo.CurrentUICulture.Name.Split('-');
                logic = new MainLogic(currentDir + Path.DirectorySeparatorChar + "UserPreferences.xml", userName, currUICultureData[0], currUICultureData[currUICultureData.Length - 1]);

                try
                {
                    directoryToUse = logic.GetSims3UserFilesDirectory();
                }
                catch (Exception)
                {
                    logic.ClearStoredDirectoriesFile();
                }

                errorCode = 4;
                if (string.IsNullOrEmpty(directoryToUse) || !Directory.Exists(directoryToUse))
                {
                    string sims3FolderLocation = userDocPath + Path.DirectorySeparatorChar + "Electronic Arts" + Path.DirectorySeparatorChar;
                    string sims3FolderName;

                    try
                    {
                        switch (logic.DS.Language)
                        {
                            case "zh":
                                if (logic.DS.Country.Equals("CN")) sims3FolderName = "模拟人生3";
                                else sims3FolderName = "模擬市民3"; // default cultureCountry: TW
                                break;
                            case "nl":
                                sims3FolderName = "De Sims 3"; // default cultureCountry: NL
                                break;
                            case "fr":
                                sims3FolderName = "Les Sims 3"; // default cultureCountry: FR
                                break;
                            case "de":
                                sims3FolderName = "Die Sims 3"; // default cultureCountry: DE
                                break;
                            case "ja":
                                sims3FolderName = "ザ・シムズ３"; // default cultureCountry: JP
                                break;
                            case "ko":
                                sims3FolderName = "심즈 3"; // default cultureCountry: KR
                                break;
                            case "es":
                                if (logic.DS.Country.Equals("ES")) sims3FolderName = "Los Sims 3";
                                else sims3FolderName = "The Sims 3"; // default cultureCountry: MX
                                break;
                            case "pt":
                                if (logic.DS.Country.Equals("PT")) sims3FolderName = "Os Sims 3";
                                else sims3FolderName = "The Sims 3"; // default cultureCountry: BR
                                break;
                            case "th":
                                sims3FolderName = "เดอะซิมส์ 3"; // default cultureCountry: TH
                                break;
                            default:
                                sims3FolderName = "The Sims 3"; // gets here with: en-US, (en-GB,) cs-CZ, da-DK, fi-FI, el-GR, hu-HU, it-IT, no-NO, pl-PL, ru-RU, sv-SE
                                break;
                        }

                        directoryToUse = sims3FolderLocation + sims3FolderName;
                        if (!Directory.Exists(directoryToUse))
                        {
                            if (Directory.Exists(sims3FolderLocation + "The Sims 3"))
                            {
                                directoryToUse = sims3FolderLocation + "The Sims 3";
                            }
                            else
                            {
                                DirectoryInfo[] eaDirFolders = new DirectoryInfo(sims3FolderLocation).GetDirectories();
                                Regex sWhitespace = new Regex(@"\s+");
                                foreach (DirectoryInfo eaDirFolder in eaDirFolders)
                                {
                                    string folderName = eaDirFolder.Name;
                                    string checkValue = sWhitespace.Replace(eaDirFolder.Name, "");
                                    if (checkValue.EndsWith("Sims3") || checkValue.EndsWith("模擬市民3") || checkValue.EndsWith("模拟人生3") || checkValue.EndsWith("シムズ３") || checkValue.EndsWith("심즈3") || checkValue.EndsWith("เดอะซิมส์3"))
                                    {
                                        sims3FolderName = folderName;
                                        directoryToUse = sims3FolderLocation + sims3FolderName;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        sims3FolderName = "The Sims 3";
                        directoryToUse = sims3FolderLocation + sims3FolderName;
                    }

                    ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    logic.Sims3UserFilesDir = directoryToUse;
                    var initWindow = new InitializerWindow(logic);
                    bool? iResult = initWindow.ShowDialog();
                    if (!iResult.HasValue || !iResult.Value)
                    {
                        Shutdown(0);
                        return;
                    }

                    try
                    {
                        logic.UpdateStoredDirectoriesFile();
                    }
                    catch (Exception)
                    {
                    }
                }

                errorCode = 6;
                var window = new MainWindow(logic);
                ShutdownMode = ShutdownMode.OnLastWindowClose;

                errorCode = 7;
                if (string.IsNullOrEmpty(logic.DS.Language))
                {
                    logic.DS.Language = "en";
                }
                if (string.IsNullOrEmpty(logic.DS.Country))
                {
                    logic.DS.Country = "US";
                }
                logic.LoadLanguage();
                logic.LoadSettings(window);

                errorCode = 8;
                logic.StartBackgroundFileCheckerAndUpdater();

                errorCode = 9;
                window.Show();
            }
            catch (Exception ex)
            {
                try
                {
                    if (errorCode < 0 || errorCode > 9)
                    {
                        errorCode = 0;
                    }
                    string[] errorTextExtras = new string[] { "" };
                    if (ex.Message.Contains("System.Core") && ex.Message.Contains("Version=2.0.5.0"))
                    {
                        errorTextExtras[0] = logic.DS.GetDynamicResource("ErrorText_8_Addendum");
                    }

                    try
                    {
                        var dialogueWindow = new DialogueWindow(null, new DialogueLogic(logic.DS, errorCode, errorTextExtras));
                        dialogueWindow.ShowDialog();
                    }
                    catch (Exception)
                    {
                        var dict = new ResourceDictionary
                        {
                            Source = new Uri("..\\Resources\\StringResources.en-US.xaml", UriKind.Relative)
                        };
                        string caption = string.Format(dict?["Error_Code"] as string ?? "Error", errorCode);
                        string content = string.Format(dict?["ErrorText_" + errorCode] as string ?? "", errorTextExtras);
                        MessageBox.Show(content, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (errorCode == 7)
                    {
                        logic.StopBackgroundFileCheckerAndUpdater();
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    Shutdown(errorCode);
                }
            }
        }
    }
}
