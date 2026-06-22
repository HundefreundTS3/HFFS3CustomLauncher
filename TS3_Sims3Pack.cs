using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace HFFS3CustomLauncher
{
    public class TS3_Sims3Pack : TS3_File
    {
        public const int PREAMBLE_LENGTH = 0x11;
        public const int EMPTY_PACK_LENGTH = 0x70;

        internal TS3_Sims3Pack(bool _, FileInfo file) : base(file.Name, file.LastWriteTime)
        {
            IsErroneous = true;
            encryptionState = FileEncryptionState.Unknown;
            SamplePackage = new TS3_Package(this);
        }

        internal TS3_Sims3Pack(FileInfo file, FileStream fs) : base(file.Name, file.LastWriteTime)
        {
            length = (int)file.Length;

            byte[] fileReadBuffer = MainLogic.ReadBuffer(fs, 4, 0xD);
            offset = fileReadBuffer[0] + fileReadBuffer[1] * 0x100 + fileReadBuffer[2] * 0x10000 + fileReadBuffer[3] * 0x1000000;
            fileReadBuffer = MainLogic.ReadBuffer(fs, offset);

            XmlDocument sims3PackXML = new XmlDocument();
            using (MemoryStream ms = new MemoryStream(fileReadBuffer))
            {
                sims3PackXML.Load(ms);
            }

            XmlNode sims3Package = sims3PackXML.DocumentElement.SelectNodes("/Sims3Package").Item(0);
            if (sims3Package == null)
            {
                sims3Package = sims3PackXML.DocumentElement.SelectNodes("/Sims3ChallengePackage").Item(0);
                if (sims3Package == null)
                {
                    throw new Exception();
                }
            }

            string _type = sims3Package.Attributes.GetNamedItem("Type")?.Value;
            if (!String.IsNullOrEmpty(_type))
            {
                if (Enum.TryParse(_type, true, out ContentType contentType))
                {
                    SuperType = contentType;
                }
            }
            string _subType = sims3Package.Attributes.GetNamedItem("SubType")?.Value;
            if (!String.IsNullOrEmpty(_subType))
            {
                try
                {
                    SubType = Convert.ToUInt32(_subType, 16);
                }
                catch (Exception)
                {
                }
            }

            SamplePackage = new TS3_Package(this);
            Packages.Add(SamplePackage);

            ReevaluateEncryptionState();
        }

        internal TS3_Sims3Pack(FileInfo file, FileStream fs, DecryptionLogic decryptionTool) : base(file.Name, file.LastWriteTime)
        {
            if (file.Length > Int32.MaxValue)
            {
                throw new Exception();
            }
            length = (int)file.Length;
            Initialization_ResultCode resultCode = Initialize(fs, decryptionTool);
            if (resultCode != Initialization_ResultCode.Success)
            {
                throw new Exception();
            }
            ReevaluateEncryptionState();
        }

        private TS3_Package SamplePackage { get; set; }

        public override bool IsSelected
        {
            get
            {
                if (isSelected)
                {
                    return true;
                }

                if (Packages.Count > 0)
                {
                    int selectedPackages = 0;
                    foreach (TS3_Package package in Packages)
                    {
                        if (package.IsSelected)
                        {
                            selectedPackages++;
                        }
                    }

                    if (selectedPackages == Packages.Count)
                    {
                        return true;
                    }
                }

                return false;
            }
            set
            {
                isSelected = value;
                foreach (TS3_Package package in Packages)
                {
                    package.EditSelectedAsParent(value);
                }
                OnPropertyChanged();
                OnPropertyChanged("ContainsSelection");
            }
        }

        public void ReevaluateSelectedValue(bool addedASelection)
        {
            if (addedASelection)
            {
                int selectedPackages = 0;
                foreach (TS3_Package package in Packages)
                {
                    if (package.IsSelected)
                    {
                        selectedPackages++;
                    }
                }

                if (selectedPackages == Packages.Count)
                {
                    isSelected = true;
                    OnPropertyChanged("IsSelected");
                }
            }
            else if (isSelected)
            {
                isSelected = false;
                OnPropertyChanged("IsSelected");
            }
            OnPropertyChanged("ContainsSelection");
        }

        public override int GetOffset()
        {
            return PREAMBLE_LENGTH + offset;
        }

        public override string Itemname
        {
            get
            {
                if (LocalizedNames.TryGetValue(DS.LanguageCode, out string name))
                {
                    return name;
                }
                if (SamplePackage.LocalizedNames.TryGetValue(DS.LanguageCode, out name))
                {
                    return name;
                }
                if (LocalizedNames.TryGetValue(LanguageCode.en_US, out name))
                {
                    return name;
                }
                if (SamplePackage.LocalizedNames.TryGetValue(LanguageCode.en_US, out name))
                {
                    return name;
                }
                return Displaytitle;
            }
        }

        public override string EnglishName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.en_US, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.en_US, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string FrenchName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.fr_FR, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.fr_FR, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string SpanishSpainName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.es_ES, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.es_ES, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string JapaneseName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.ja_JP, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.ja_JP, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string ItalianName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.it_IT, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.it_IT, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string KoreanName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.ko_KR, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.ko_KR, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string GermanName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.de_DE, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.de_DE, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string ChineseTaiwanName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.zh_TW, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.zh_TW, out name))
                {
                    return name;
                }
                return "";
            }
        }

        public override string ChineseChinaName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.zh_CHS, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.zh_CHS, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string CzechName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.cs_CZ, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.cs_CZ, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string DanishName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.da_DK, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.da_DK, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string DutchName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.nl_NL, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.nl_NL, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string FinnishName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.fi_FI, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.fi_FI, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string GreekName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.el_GR, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.el_GR, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string HungarianName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.hu_HU, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.hu_HU, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string NorwegianName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.no, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.no, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string PolishName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.pl_PL, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.pl_PL, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string PortuguesePortugalName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.pt_PT, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.pt_PT, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string RussianName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.ru_RU, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.ru_RU, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string SwedishName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.sv_SE, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.sv_SE, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string ThaiName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.th_TH, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.th_TH, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string SpanishMexicoName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.es_MX, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.es_MX, out name))
                {
                    return name;
                }
                return "";
            }
        }
        public override string PortugueseBrazilName
        {
            get
            {
                string name;
                if (LocalizedNames.TryGetValue(LanguageCode.pt_BR, out name) || SamplePackage.LocalizedNames.TryGetValue(LanguageCode.pt_BR, out name))
                {
                    return name;
                }
                return "";
            }
        }

        public void ReevaluateEncryptionState()
        {
            int packagesWithoutEncryption = 0;
            int packagesWithEncryption = 0;

            bool isPending = false;

            foreach (TS3_Package package in Packages)
            {
                switch (package.EncryptionState)
                {
                    case FileEncryptionState.None:
                        packagesWithoutEncryption++;
                        break;
                    case FileEncryptionState.Present:
                        packagesWithEncryption++;
                        break;
                    case FileEncryptionState.Pending:
                        isPending = true;
                        break;
                    default:
                        EncryptionState = FileEncryptionState.Error_Invalid;
                        return;
                }
            }

            if (isPending)
            {
                EncryptionState = FileEncryptionState.Pending;
            }

            if (packagesWithEncryption == 0)
            {
                EncryptionState = FileEncryptionState.None;
            }
            else if (packagesWithEncryption > 0 && packagesWithoutEncryption == 0)
            {
                EncryptionState = FileEncryptionState.Present;
            }
            else
            {
                EncryptionState = FileEncryptionState.Mixed;
            }
        }

        private Initialization_ResultCode Initialize(FileStream fs, DecryptionLogic decryptionTool)
        {
            byte[] fileReadBuffer = MainLogic.ReadBuffer(fs, 4, 0xD);
            if (fileReadBuffer[3] >= 0x80)
            {
                return Initialization_ResultCode.Error_MetadataTooBig;
            }
            offset = fileReadBuffer[0] + fileReadBuffer[1] * 0x100 + fileReadBuffer[2] * 0x10000 + fileReadBuffer[3] * 0x1000000;
            fileReadBuffer = MainLogic.ReadBuffer(fs, offset);

            XmlDocument sims3PackXML = new XmlDocument();
            using (MemoryStream ms = new MemoryStream(fileReadBuffer))
            {
                sims3PackXML.Load(ms);
            }

            string xmlDocElemName = sims3PackXML.DocumentElement?.Name.ToLower() ?? "";
            if (xmlDocElemName != "sims3package" && xmlDocElemName != "sims3challengepackage")
            {
                return Initialization_ResultCode.Error_WrongXMLFormat;
            }
            XmlNode sims3Package = sims3PackXML.DocumentElement;

            foreach (XmlAttribute xmlAttribute in sims3Package.Attributes)
            {
                try
                {
                    switch (xmlAttribute.Name.ToLower())
                    {
                        case "type":
                            if (Enum.TryParse(xmlAttribute.Value, true, out ContentType contentType))
                            {
                                SuperType = contentType;
                            }
                            break;
                        case "subtype":
                            SubType = Convert.ToUInt32(xmlAttribute.Value, 16);
                            break;
                        case "agsflags":
                            GenderSpeciesAgeFlags = Convert.ToUInt32(xmlAttribute.Value, 16);
                            break;
                    }
                }
                catch (Exception)
                {
                }
            }

            string displayName = GetXmlNodeInnerText(sims3Package, "displayname");
            if (!String.IsNullOrEmpty(displayName))
            {
                Displaytitle = displayName;
            }

            string _packageId = GetXmlNodeInnerText(sims3Package, "packageid");
            if (!String.IsNullOrEmpty(_packageId))
            {
                PackageId = _packageId;
            }

            string _date = GetXmlNodeInnerText(sims3Package, "date");
            if (!String.IsNullOrEmpty(_date))
            {
                try
                {
                    Date = DateTime.Parse(_date, new CultureInfo("en-US", false));
                }
                catch (Exception)
                {
                }
            }


            XmlNode localizedNames = GetXmlNode(sims3Package, "localizednames");
            if (localizedNames != null)
            {
                foreach (XmlNode localizedName in localizedNames)
                {
                    if (localizedName.Name.ToLower() == "localizedname")
                    {
                        foreach (XmlAttribute xmlAttribute in localizedName.Attributes)
                        {
                            try
                            {
                                switch (xmlAttribute.Name.ToLower())
                                {
                                    case "language":
                                        string language = xmlAttribute.Value.Replace("-", "_");
                                        if (Enum.TryParse(language, true, out LanguageCode languageCode))
                                        {
                                            string localizedNameString = localizedName.InnerText;
                                            if (localizedNameString.StartsWith("![CDATA[") && localizedNameString.EndsWith("]]"))
                                            {
                                                localizedNameString = localizedNameString.Substring(8, localizedNameString.Length - 10);
                                            }
                                            LocalizedNames.Add(languageCode, localizedNameString);
                                        }
                                        break;
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }

            byte gender = 0x0;
            bool isSpeciesSim = false;
            bool isSpeciesHorse = false;
            bool isSpeciesCat = false;
            bool isSpeciesLargedog = false;
            bool isSpeciesSmalldog = false;
            bool isSpeciesUndefined = false;
            byte age = 0x00;
            bool ignoreAgegenderflags = false;
            uint packageCount = 0;
            int packagesStartIndex = GetOffset();
            XmlNodeList packagedFiles = GetXmlNodeList(sims3Package, "packagedfile");
            foreach (XmlNode packageFile in packagedFiles)
            {
                string packageName = "";
                int packageLength = -1;
                int packageOffset = -1;
                TS3_Package newPackage;

                string name = GetXmlNodeInnerText(packageFile, "name");
                if (!String.IsNullOrEmpty(name))
                {
                    packageName = name;
                }

                string length = GetXmlNodeInnerText(packageFile, "length");
                if (String.IsNullOrEmpty(length) || !Int32.TryParse(length, out packageLength))
                {
                    packageLength = -1;
                }
                string offset = GetXmlNodeInnerText(packageFile, "offset");
                if (String.IsNullOrEmpty(offset) || !Int32.TryParse(offset, out packageOffset))
                {
                    packageOffset = -1;
                }

                if (!ignoreAgegenderflags)
                {
                    string agegenderflags = GetXmlNodeInnerText(packageFile, "metatags", "agegenderflags");
                    if (!String.IsNullOrEmpty(agegenderflags))
                    {
                        try
                        {
                            uint agegenderflagsValue = Convert.ToUInt32(agegenderflags, 16);
                            gender |= GetGenderFlags(agegenderflagsValue);
                            switch (GetSpeciesFlags(agegenderflagsValue))
                            {
                                case VALUE_SPECIES_NONE:
                                    break;
                                case VALUE_SPECIES_SIM:
                                    isSpeciesSim = true;
                                    break;
                                case VALUE_SPECIES_HORSE:
                                    isSpeciesHorse = true;
                                    break;
                                case VALUE_SPECIES_CAT:
                                    isSpeciesCat = true;
                                    break;
                                case VALUE_SPECIES_LARGEDOG:
                                    isSpeciesLargedog = true;
                                    break;
                                case VALUE_SPECIES_SMALLDOG:
                                    isSpeciesSmalldog = true;
                                    break;
                                default:
                                    isSpeciesUndefined = true;
                                    break;
                            }
                            age |= GetAgeFlags(agegenderflagsValue);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        ignoreAgegenderflags = true;
                    }
                }

                if (packageLength >= 0 && packageOffset >= 0)
                {
                    int packageStartIndex = packagesStartIndex + packageOffset;
                    byte[] fileReadBufferPackage = MainLogic.ReadBuffer(fs, 4, packageStartIndex);

                    switch (TS3_File.GetTS3FileType(fileReadBufferPackage, packageLength))
                    {
                        case TS3_FileType.Package:
                            packageCount++;
                            try
                            {
                                newPackage = new TS3_Package(packageName, packageLength, packageOffset, packageCount, this, fs, decryptionTool);
                            }
                            catch (Exception)
                            {
                                newPackage = new TS3_Package(false, packageName, packageCount, this);
                            }
                            break;
                        case TS3_FileType.PNG:
                            if (Thumbnail == null)
                            {
                                try
                                {
                                    byte[] pngBytes = MainLogic.ReadBuffer(fs, packageLength, packageStartIndex);
                                    CreateThumbnail(pngBytes);
                                }
                                catch (Exception)
                                {
                                }
                            }
                            continue;
                        default:
                            if (Thumbnail == null)
                            {
                                try
                                {
                                    byte[] unknownBytes = MainLogic.ReadBuffer(fs, packageLength, packageStartIndex);
                                    if (TS3_File.IsSpecialPNGFile(fileReadBufferPackage, packagesStartIndex + packageOffset, packageLength, out int specialPNGOffset))
                                    {
                                        byte[] specialPNGBytes = new byte[packageLength - specialPNGOffset];
                                        for (int i = 0; i < specialPNGBytes.Length; i++)
                                        {
                                            specialPNGBytes[i] = unknownBytes[specialPNGOffset + i];
                                        }
                                        CreateThumbnail(specialPNGBytes);
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }
                            continue;
                    }
                }
                else if (packageOffset >= 0)
                {
                    int packageStartIndex = packagesStartIndex + packageOffset;
                    byte[] fileReadBufferPackage = MainLogic.ReadBuffer(fs, 4, packageStartIndex);
                    if (TS3_File.GetTS3FileType(fileReadBufferPackage, packageLength) == TS3_FileType.Package)
                    {
                        packageCount++;
                        newPackage = new TS3_Package(false, packageName, packageCount, this);
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    packageCount++;
                    newPackage = new TS3_Package(false, packageName, packageCount, this);
                }

                packages.Add(newPackage);
                if (newPackage.IsErroneous) IsErroneous = true;
            }

            if (GenderSpeciesAgeFlags == 0x00000000 && !ignoreAgegenderflags)
            {
                byte species = 0x0;
                if (isSpeciesSim && !isSpeciesHorse && !isSpeciesCat && !isSpeciesLargedog && !isSpeciesSmalldog && !isSpeciesUndefined)
                {
                    species = VALUE_SPECIES_SIM;
                }
                else if (isSpeciesHorse && !isSpeciesSim && !isSpeciesCat && !isSpeciesLargedog && !isSpeciesSmalldog && !isSpeciesUndefined)
                {
                    species = VALUE_SPECIES_HORSE;
                }
                else if (isSpeciesCat && !isSpeciesSim && !isSpeciesHorse && !isSpeciesLargedog && !isSpeciesSmalldog && !isSpeciesUndefined)
                {
                    species = VALUE_SPECIES_CAT;
                }
                else if (isSpeciesLargedog && !isSpeciesSim && !isSpeciesHorse && !isSpeciesCat && !isSpeciesSmalldog && !isSpeciesUndefined)
                {
                    species = VALUE_SPECIES_LARGEDOG;
                }
                else if (isSpeciesSmalldog && !isSpeciesSim && !isSpeciesHorse && !isSpeciesCat && !isSpeciesLargedog && !isSpeciesUndefined)
                {
                    species = VALUE_SPECIES_SMALLDOG;
                }
                SetGenderSpeciesAgeFlags(gender, species, age);
            }

            if (Packages.Count > 0)
            {
                SamplePackage = Packages[0];
            }
            else
            {
                SamplePackage = new TS3_Package(this);
            }

            IsPaidContent = SamplePackage.IsPaidContent;
            if (string.IsNullOrEmpty(PackageId))
            {
                PackageId = SamplePackage.PackageId;
            }
            if (Date == DateTime.MinValue)
            {
                Date = SamplePackage.Date;
            }
            if (Thumbnail == null)
            {
                Thumbnail = SamplePackage.Thumbnail;
            }
            if (GenderSpeciesAgeFlags == 0x00000000)
            {
                GenderSpeciesAgeFlags = SamplePackage.GenderSpeciesAgeFlags;
            }

            return Initialization_ResultCode.Success;
        }
    }
}
