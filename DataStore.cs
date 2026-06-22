using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace HFFS3CustomLauncher
{
    public enum LanguageCode
    {
        en_US,
        fr_FR,
        es_ES,
        ja_JP,
        it_IT,
        ko_KR,
        de_DE,
        zh_TW,
        zh_CHS,
        zh_CN = zh_CHS,
        cs_CZ,
        da_DK,
        nl_NL,
        fi_FI,
        el_GR,
        hu_HU,
        no,
        pl_PL,
        pt_PT,
        ru_RU,
        sv_SE,
        th_TH,
        es_MX,
        pt_BR
    }

    internal class DataStore
    {
        internal bool IsWindowsXP { get; private set; } = false;
        internal string Language { get; set; } = "";
        internal string Country { get; set; } = "";

        internal LanguageCode LanguageCode
        {
            get
            {
                switch (Language)
                {
                    case "fr":
                        return LanguageCode.fr_FR;
                    case "es":
                        if (Country == "ES")
                        {
                            return LanguageCode.es_ES;
                        }
                        return LanguageCode.es_MX;
                    case "jp":
                        return LanguageCode.ja_JP;
                    case "it":
                        return LanguageCode.it_IT;
                    case "ko":
                        return LanguageCode.ko_KR;
                    case "de":
                        return LanguageCode.de_DE;
                    case "zh":
                        if (Country == "CN")
                        {
                            return LanguageCode.zh_CHS;
                        }
                        return LanguageCode.zh_TW;
                    case "cs":
                        return LanguageCode.cs_CZ;
                    case "da":
                        return LanguageCode.da_DK;
                    case "nl":
                        return LanguageCode.nl_NL;
                    case "fi":
                        return LanguageCode.fi_FI;
                    case "el":
                        return LanguageCode.el_GR;
                    case "hu":
                        return LanguageCode.hu_HU;
                    case "no":
                        return LanguageCode.no;
                    case "pl":
                        return LanguageCode.pl_PL;
                    case "pt":
                        if (Country == "PT")
                        {
                            return LanguageCode.pt_PT;
                        }
                        return LanguageCode.pt_BR;
                    case "ru":
                        return LanguageCode.ru_RU;
                    case "sv":
                        return LanguageCode.sv_SE;
                    case "th":
                        return LanguageCode.th_TH;
                    default:
                        return LanguageCode.en_US;
                }
            }
            set
            {
                switch (value)
                {
                    case LanguageCode.fr_FR:
                        Language = "fr";
                        break;
                    case LanguageCode.es_ES:
                        Language = "es";
                        Country = "ES";
                        break;
                    case LanguageCode.ja_JP:
                        Language = "ja";
                        break;
                    case LanguageCode.it_IT:
                        Language = "it";
                        break;
                    case LanguageCode.ko_KR:
                        Language = "ko";
                        break;
                    case LanguageCode.de_DE:
                        Language = "de";
                        break;
                    case LanguageCode.zh_TW:
                        Language = "zh";
                        if (Country == "CN")
                        {
                            Country = "TW";
                        }
                        break;
                    case LanguageCode.zh_CHS:
                        Language = "zh";
                        Country = "CN";
                        break;
                    case LanguageCode.cs_CZ:
                        Language = "cs";
                        break;
                    case LanguageCode.da_DK:
                        Language = "da";
                        break;
                    case LanguageCode.nl_NL:
                        Language = "nl";
                        break;
                    case LanguageCode.fi_FI:
                        Language = "fi";
                        break;
                    case LanguageCode.el_GR:
                        Language = "el";
                        break;
                    case LanguageCode.hu_HU:
                        Language = "hu";
                        break;
                    case LanguageCode.no:
                        Language = "no";
                        break;
                    case LanguageCode.pl_PL:
                        Language = "pl";
                        break;
                    case LanguageCode.pt_PT:
                        Language = "pt";
                        Country = "PT";
                        break;
                    case LanguageCode.ru_RU:
                        Language = "ru";
                        break;
                    case LanguageCode.sv_SE:
                        Language = "sv";
                        break;
                    case LanguageCode.th_TH:
                        Language = "th";
                        break;
                    case LanguageCode.es_MX:
                        Language = "es";
                        if (Country == "ES")
                        {
                            Country = "MX";
                        }
                        break;
                    case LanguageCode.pt_BR:
                        Language = "pt";
                        if (Country == "PT")
                        {
                            Country = "BR";
                        }
                        break;
                    default:
                        Language = "en";
                        break;
                }
            }
        }

        private readonly ResourceDictionary ResDict = new ResourceDictionary();
        private readonly ResourceDictionary DefaultLangDict = new ResourceDictionary();
        internal ResourceDictionary CurrLangDict { get; set; } = null;

        internal DataStore(string language, string country, bool isWindowsXP)
        {
            Language = language;
            Country = country;
            IsWindowsXP = isWindowsXP;
            ResDict.Source = new Uri("..\\Resources\\ResourceDictionary.xaml", UriKind.Relative);
            DefaultLangDict.Source = new Uri("..\\Resources\\StringResources.en-US.xaml", UriKind.Relative);
        }

        internal string GetDynamicResource(string key)
        {
            try
            {
                if (CurrLangDict != null)
                {
                    return CurrLangDict[key] as string ?? "";
                }
                return DefaultLangDict[key] as string ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        internal string ColumnKey_ExpandCollapse { get { return ResDict["ColumnKey_ExpandCollapse"] as string ?? ""; } }
        internal string ColumnKey_Select { get { return ResDict["ColumnKey_Select"] as string ?? ""; } }
        internal string ColumnKey_Filename { get { return ResDict["ColumnKey_Filename"] as string ?? ""; } }
        internal string ColumnKey_Displaytitle { get { return ResDict["ColumnKey_Displaytitle"] as string ?? ""; } }
        internal string ColumnKey_ResourceName { get { return ResDict["ColumnKey_ResourceName"] as string ?? ""; } }
        internal string ColumnKey_Item { get { return ResDict["ColumnKey_Item"] as string ?? ""; } }
        internal string ColumnKey_Type { get { return ResDict["ColumnKey_Type"] as string ?? ""; } }
        internal string ColumnKey_SuperType { get { return ResDict["ColumnKey_SuperType"] as string ?? ""; } }
        internal string ColumnKey_SubType { get { return ResDict["ColumnKey_SubType"] as string ?? ""; } }
        internal string ColumnKey_Size { get { return ResDict["ColumnKey_Size"] as string ?? ""; } }
        internal string ColumnKey_Encryption { get { return ResDict["ColumnKey_Encryption"] as string ?? ""; } }
        internal string ColumnKey_PackageId { get { return ResDict["ColumnKey_PackageId"] as string ?? ""; } }
        internal string ColumnKey_Date { get { return ResDict["ColumnKey_Date"] as string ?? ""; } }
        internal string ColumnKey_PackageCount { get { return ResDict["ColumnKey_PackageCount"] as string ?? ""; } }
        internal string ColumnKey_Order { get { return ResDict["ColumnKey_Order"] as string ?? ""; } }
        internal string ColumnKey_PaidContent { get { return ResDict["ColumnKey_PaidContent"] as string ?? ""; } }
        internal string ColumnKey_Gender { get { return ResDict["ColumnKey_Gender"] as string ?? ""; } }
        internal string ColumnKey_Species { get { return ResDict["ColumnKey_Species"] as string ?? ""; } }
        internal string ColumnKey_Age { get { return ResDict["ColumnKey_Age"] as string ?? ""; } }
        internal string ColumnKey_Image { get { return ResDict["ColumnKey_Image"] as string ?? ""; } }
        internal string ColumnKey_EnglishName { get { return ResDict["ColumnKey_EnglishName"] as string ?? ""; } }
        internal string ColumnKey_FrenchName { get { return ResDict["ColumnKey_FrenchName"] as string ?? ""; } }
        internal string ColumnKey_SpanishSpainName { get { return ResDict["ColumnKey_SpanishSpainName"] as string ?? ""; } }
        internal string ColumnKey_JapaneseName { get { return ResDict["ColumnKey_JapaneseName"] as string ?? ""; } }
        internal string ColumnKey_ItalianName { get { return ResDict["ColumnKey_ItalianName"] as string ?? ""; } }
        internal string ColumnKey_KoreanName { get { return ResDict["ColumnKey_KoreanName"] as string ?? ""; } }
        internal string ColumnKey_GermanName { get { return ResDict["ColumnKey_GermanName"] as string ?? ""; } }
        internal string ColumnKey_ChineseTaiwanName { get { return ResDict["ColumnKey_ChineseTaiwanName"] as string ?? ""; } }
        internal string ColumnKey_ChineseChinaName { get { return ResDict["ColumnKey_ChineseChinaName"] as string ?? ""; } }
        internal string ColumnKey_CzechName { get { return ResDict["ColumnKey_CzechName"] as string ?? ""; } }
        internal string ColumnKey_DanishName { get { return ResDict["ColumnKey_DanishName"] as string ?? ""; } }
        internal string ColumnKey_DutchName { get { return ResDict["ColumnKey_DutchName"] as string ?? ""; } }
        internal string ColumnKey_FinnishName { get { return ResDict["ColumnKey_FinnishName"] as string ?? ""; } }
        internal string ColumnKey_GreekName { get { return ResDict["ColumnKey_GreekName"] as string ?? ""; } }
        internal string ColumnKey_HungarianName { get { return ResDict["ColumnKey_HungarianName"] as string ?? ""; } }
        internal string ColumnKey_NorwegianName { get { return ResDict["ColumnKey_NorwegianName"] as string ?? ""; } }
        internal string ColumnKey_PolishName { get { return ResDict["ColumnKey_PolishName"] as string ?? ""; } }
        internal string ColumnKey_PortuguesePortugalName { get { return ResDict["ColumnKey_PortuguesePortugalName"] as string ?? ""; } }
        internal string ColumnKey_RussianName { get { return ResDict["ColumnKey_RussianName"] as string ?? ""; } }
        internal string ColumnKey_SwedishName { get { return ResDict["ColumnKey_SwedishName"] as string ?? ""; } }
        internal string ColumnKey_ThaiName { get { return ResDict["ColumnKey_ThaiName"] as string ?? ""; } }
        internal string ColumnKey_SpanishMexicoName { get { return ResDict["ColumnKey_SpanishMexicoName"] as string ?? ""; } }
        internal string ColumnKey_PortugueseBrazilName { get { return ResDict["ColumnKey_PortugueseBrazilName"] as string ?? ""; } }

        internal string GetColumnSavename(string columnKey)
        {
            return ResDict[columnKey + "_Savename"] as string ?? "";
        }

        public const byte ASCII_B = 0x42;
        public const byte ASCII_D = 0x44;
        public const byte ASCII_F = 0x46;
        public const byte ASCII_G = 0x47;
        public const byte ASCII_N = 0x4E;
        public const byte ASCII_P = 0x50;
        public const byte PNG_STARTBYTE = 0x89;

        internal string GetErrorCodeDescription(int errorCode)
        {
            string errorCodeDescription = GetDynamicResource("ErrorText_" + errorCode);
            if (string.IsNullOrEmpty(errorCodeDescription))
            {
                errorCodeDescription = GetDynamicResource("ErrorText_0");
            }
            return errorCodeDescription;
        }

        internal string GetErrorCodeDescription(int errorCode, string[] args)
        {
            string rawErrorCodeDescription = GetDynamicResource("ErrorText_" + errorCode);
            if (string.IsNullOrEmpty(rawErrorCodeDescription))
            {
                return GetDynamicResource("ErrorText_0");
            }
            else
            {
                return string.Format(rawErrorCodeDescription, args);
            }
        }

        internal static List<T> NoSort<T>(List<T> sourceList, Comparison<T> _, bool __)
        {
            return sourceList;
        }

        internal static void SkipSort<T>(List<T> sourceList, Collection<T> destColl, Comparison<T> comparison, bool reverse)
        {
            destColl.Clear();
            if (sourceList.Count > 1)
            {
                int sign = reverse ? -1 : 1;
                List<int> indeces = new List<int>
                {
                    1
                };
                destColl.Add(sourceList[0]);
                for (int i = 1; i < sourceList.Count; i++)
                {
                    int j = 0;
                    for (int k = 0; k < indeces.Count; k++)
                    {
                        int comp = comparison.Invoke(sourceList[i], destColl[j]) * sign;
                        if (comp < 0)
                        {
                            destColl.Insert(j, sourceList[i]);
                            indeces.Insert(k, 1);
                            break;
                        }
                        else if (comp == 0)
                        {
                            destColl.Insert(j + indeces[k], sourceList[i]);
                            indeces[k]++;
                            break;
                        }
                        else
                        {
                            j += indeces[k];
                        }
                    }

                    if (j >= i)
                    {
                        destColl.Add(sourceList[i]);
                        indeces.Add(1);
                    }
                }
            }
            else if (sourceList.Count == 1)
            {
                destColl.Add(sourceList[0]);
            }
        }
        internal static List<T> SkipSort<T>(List<T> sourceList, Comparison<T> comparison, bool reverse)
        {
            if (sourceList.Count > 1)
            {
                int sign = reverse ? -1 : 1;
                List<int> indeces = new List<int>
                {
                    1
                };
                List<T> destList = new List<T>(sourceList.Count) { sourceList[0] };
                for (int i = 1; i < sourceList.Count; i++)
                {
                    int j = 0;
                    for (int k = 0; k < indeces.Count; k++)
                    {
                        int comp = comparison.Invoke(sourceList[i], destList[j]) * sign;
                        if (comp < 0)
                        {
                            destList.Insert(j, sourceList[i]);
                            indeces.Insert(k, 1);
                            break;
                        }
                        else if (comp == 0)
                        {
                            destList.Insert(j + indeces[k], sourceList[i]);
                            indeces[k]++;
                            break;
                        }
                        else
                        {
                            j += indeces[k];
                        }
                    }

                    if (j >= i)
                    {
                        destList.Add(sourceList[i]);
                        indeces.Add(1);
                    }
                }
                return destList;
            }
            return sourceList;
        }

        internal static void ContainedMergeSort<T>(List<T> sourceList, Collection<T> destColl, Comparison<T> comparison, bool reverse)
        {
            destColl.Clear();
            if (sourceList.Count > 1)
            {
                int sign = reverse ? -1 : 1;
                int sourceListCount = sourceList.Count;
                int lowerOffset = 1;
                int numberOfSequences = (int)Math.Ceiling(sourceListCount / 2.0);
                while (numberOfSequences > 1)
                {
                    int greaterOffset = lowerOffset * 2;
                    for (int j = 0; j < numberOfSequences; j++)
                    {
                        int firstPos = j * greaterOffset;
                        int firstEnd = firstPos + lowerOffset;
                        int secondPos = firstEnd;
                        int secondEnd = Math.Min(secondPos + lowerOffset, sourceListCount);
                        while (firstPos < firstEnd && secondPos < secondEnd)
                        {
                            if (comparison.Invoke(sourceList[firstPos], sourceList[secondPos]) * sign <= 0)
                            {
                                firstPos++;
                            }
                            else
                            {
                                T ts3File = sourceList[secondPos];
                                sourceList.RemoveAt(secondPos);
                                sourceList.Insert(firstPos, ts3File);
                                secondPos++;
                                firstPos++;
                                firstEnd++;
                            }
                        }
                    }

                    numberOfSequences = (int)Math.Ceiling(numberOfSequences / 2.0);
                    lowerOffset = greaterOffset;
                }
                int lastFirstPos = 0;
                int lastFirstEnd = lastFirstPos + lowerOffset;
                int lastSecondPos = lastFirstEnd;
                int lastSecondEnd = Math.Min(lastSecondPos + lowerOffset, sourceListCount);
                while (lastFirstPos < lastFirstEnd && lastSecondPos < lastSecondEnd)
                {
                    if (comparison.Invoke(sourceList[lastFirstPos], sourceList[lastSecondPos]) * sign <= 0)
                    {
                        destColl.Add(sourceList[lastFirstPos]);
                        lastFirstPos++;
                    }
                    else
                    {
                        destColl.Add(sourceList[lastSecondPos]);
                        lastSecondPos++;
                    }
                }
                while (lastFirstPos < lastFirstEnd)
                {
                    destColl.Add(sourceList[lastFirstPos]);
                    lastFirstPos++;
                }
                while (lastSecondPos < lastSecondEnd)
                {
                    destColl.Add(sourceList[lastSecondPos]);
                    lastSecondPos++;
                }
            }
            else if (sourceList.Count == 1)
            {
                destColl.Add(sourceList[0]);
            }
        }
        internal static List<T> ContainedMergeSort<T>(List<T> sourceList, Comparison<T> comparison, bool reverse)
        {
            if (sourceList.Count > 1)
            {
                int sign = reverse ? -1 : 1;
                int sourceListCount = sourceList.Count;
                int lowerOffset = 1;
                int numberOfSequences = (int)Math.Ceiling(sourceListCount / 2.0);
                while (numberOfSequences > 1)
                {
                    int greaterOffset = lowerOffset * 2;
                    for (int j = 0; j < numberOfSequences; j++)
                    {
                        int firstPos = j * greaterOffset;
                        int firstEnd = firstPos + lowerOffset;
                        int secondPos = firstEnd;
                        int secondEnd = Math.Min(secondPos + lowerOffset, sourceListCount);
                        while (firstPos < firstEnd && secondPos < secondEnd)
                        {
                            if (comparison.Invoke(sourceList[firstPos], sourceList[secondPos]) * sign <= 0)
                            {
                                firstPos++;
                            }
                            else
                            {
                                T ts3File = sourceList[secondPos];
                                sourceList.RemoveAt(secondPos);
                                sourceList.Insert(firstPos, ts3File);
                                secondPos++;
                                firstPos++;
                                firstEnd++;
                            }
                        }
                    }

                    numberOfSequences = (int)Math.Ceiling(numberOfSequences / 2.0);
                    lowerOffset = greaterOffset;
                }
                List<T> destList = new List<T>(sourceListCount);
                int lastFirstPos = 0;
                int lastFirstEnd = lastFirstPos + lowerOffset;
                int lastSecondPos = lastFirstEnd;
                int lastSecondEnd = Math.Min(lastSecondPos + lowerOffset, sourceListCount);
                while (lastFirstPos < lastFirstEnd && lastSecondPos < lastSecondEnd)
                {
                    if (comparison.Invoke(sourceList[lastFirstPos], sourceList[lastSecondPos]) * sign <= 0)
                    {
                        destList.Add(sourceList[lastFirstPos]);
                        lastFirstPos++;
                    }
                    else
                    {
                        destList.Add(sourceList[lastSecondPos]);
                        lastSecondPos++;
                    }
                }
                while (lastFirstPos < lastFirstEnd)
                {
                    destList.Add(sourceList[lastFirstPos]);
                    lastFirstPos++;
                }
                while (lastSecondPos < lastSecondEnd)
                {
                    destList.Add(sourceList[lastSecondPos]);
                    lastSecondPos++;
                }
                return destList;
            }
            return sourceList;
        }

        internal static void InsertionSort<T>(ObservableCollection<T> sourceColl, Comparison<T> comparison)
        {
            for (int i = 1; i < sourceColl.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (comparison.Invoke(sourceColl[i], sourceColl[j]) < 0)
                    {
                        T tmp = sourceColl[i];
                        for (int k = i; k > j; k--)
                        {
                            sourceColl[k] = sourceColl[k - 1];
                        }
                        sourceColl[j] = tmp;
                        break;
                    }
                }
            }
        }
    }
}
