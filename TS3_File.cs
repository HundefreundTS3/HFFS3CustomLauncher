using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;

namespace HFFS3CustomLauncher
{
    public abstract class TS3_File : AnyFile
    {
        private const int MAX_THUM_SIZE = 93;
        protected internal const byte FLAG_GENDER_MALE = 0x1;
        protected internal const byte FLAG_GENDER_FEMALE = 0x2;
        protected internal const byte FLAGS_GENDER_BOTH = 0x3;
        protected internal const byte VALUE_SPECIES_NONE = 0x0;
        protected internal const byte VALUE_SPECIES_SIM = 0x1;
        protected internal const byte VALUE_SPECIES_HORSE = 0x2;
        protected internal const byte VALUE_SPECIES_CAT = 0x3;
        protected internal const byte VALUE_SPECIES_LARGEDOG = 0x4;
        protected internal const byte VALUE_SPECIES_SMALLDOG = 0x5;
        protected internal const byte FLAG_AGE_BABY = 0x01;
        protected internal const byte FLAG_AGE_TODDLER = 0x02;
        protected internal const byte FLAG_AGE_CHILD = 0x04;
        protected internal const byte FLAG_AGE_TEEN = 0x08;
        protected internal const byte FLAG_AGE_YOUNGADULT = 0x10;
        protected internal const byte FLAG_AGE_ADULT = 0x20;
        protected internal const byte FLAG_AGE_ELDER = 0x40;
        protected internal const byte FLAG_AGE_UNDEFINED = 0x80;

        private static DataStore ds = null;
        internal static DataStore DS
        {
            get
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

        protected internal enum Initialization_ResultCode
        {
            UnknownError = -1,
            Success,
            Error_PartlyInaccessible,
            Error_MetadataTooBig,
            Error_WrongXMLFormat,
            Error_OutOfBoundsDuringReadSims3PackMetadata,
            Error_OutOfBoundsDuringReadName,
            Error_OutOfBoundsDuringReadLength,
            Error_OutOfBoundsDuringReadOffset,
            Error_PrematureLeaving,

            Error_InvalidEncryptionState
        }

        public enum ContentType
        {
            _,
            Object,
            CASpart,
            preset,
            pattern,
            sim,
            household,
            lot,
            world,
            coatset,
            haircolorinfo,
            dynamicchallenge
        }

        internal TS3_File(string fileName, DateTime lastWriteTime) : base(fileName, lastWriteTime)
        {
        }

        public virtual bool HasParent
        {
            get
            {
                return false;
            }
        }

        protected List<TS3_Package> packages = new List<TS3_Package>();

        public bool HasChildren
        {
            get
            {
                return Packages.Count > 0;
            }
        }

        internal List<TS3_Package> Packages
        {
            get
            {
                return packages;
            }
            set
            {
                packages = value;
            }
        }

        protected bool showChildren = false;
        public bool ShowChildren
        {
            get
            {
                return showChildren;
            }
            set
            {
                showChildren = value;
                OnPropertyChanged();
            }
        }

        protected bool isSelected = false;
        public virtual bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (value != IsSelected)
                {
                    isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ContainsSelection
        {
            get
            {
                if (isSelected)
                {
                    return true;
                }

                foreach (TS3_Package package in Packages)
                {
                    if (package.IsSelected)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool ContainsSelectedPackage()
        {
            foreach (TS3_Package package in Packages)
            {
                if (package.IsSelected)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual uint Order
        {
            get { return 0; }
        }
        public virtual string OrderText
        {
            get { return ""; }
        }

        public virtual string Itemname
        {
            get
            {
                if (LocalizedNames.TryGetValue(DS.LanguageCode, out string name))
                {
                    return name;
                }
                if (LocalizedNames.TryGetValue(LanguageCode.en_US, out name))
                {
                    return name;
                }
                return Displaytitle;
            }
        }

        public virtual string ResourceName
        {
            get
            {
                return "";
            }
        }

        protected string displaytitle = "";
        public string Displaytitle
        {
            get { return displaytitle; }
            protected set { displaytitle = value; }
        }

        public string Type
        {
            get
            {
                switch (SuperType)
                {
                    case ContentType.Object:
                        if ((SubType & 0x10000000) == 0x10000000)
                        {
                            return DS.GetDynamicResource("ContentType_object_BuildObject");
                        }
                        if ((SubType & 0x20000000) == 0x20000000)
                        {
                            return DS.GetDynamicResource("ContentType_object_BuyObject");
                        }
                        return DS.GetDynamicResource("ContentType_object");
                    case ContentType.CASpart:
                        switch (SubType)
                        {
                            case 0x00000001:
                                return DS.GetDynamicResource("ContentType_CASpart_Hair");
                            case 0x00000002:
                                return DS.GetDynamicResource("ContentType_CASpart_Scalp");
                            case 0x00000004:
                                return DS.GetDynamicResource("ContentType_CASpart_Fulloutfit");
                            case 0x00000005:
                                return DS.GetDynamicResource("ContentType_CASpart_Top");
                            case 0x00000006:
                                return DS.GetDynamicResource("ContentType_CASpart_Bottom");
                            case 0x00000007:
                                return DS.GetDynamicResource("ContentType_CASpart_Shoes");
                            case 0x00000009:
                                return DS.GetDynamicResource("ContentType_CASpart_Necklace");
                            case 0x0000000a:
                                return DS.GetDynamicResource("ContentType_CASpart_Nosering");
                            case 0x0000000b:
                                return DS.GetDynamicResource("ContentType_CASpart_Earrings");
                            case 0x0000000c:
                                return DS.GetDynamicResource("ContentType_CASpart_Glasses");
                            case 0x0000000d:
                                return DS.GetDynamicResource("ContentType_CASpart_Bracelet");
                            case 0x0000000e:
                                return DS.GetDynamicResource("ContentType_CASpart_Ring");
                            case 0x0000000f:
                                return DS.GetDynamicResource("ContentType_CASpart_WeddingRing");
                            case 0x00000010:
                                return DS.GetDynamicResource("ContentType_CASpart_Beard");
                            case 0x00000011:
                                return DS.GetDynamicResource("ContentType_CASpart_Lipstick");
                            case 0x00000012:
                                return DS.GetDynamicResource("ContentType_CASpart_Eyeshadow");
                            case 0x00000013:
                                return DS.GetDynamicResource("ContentType_CASpart_Eyeliner");
                            case 0x00000014:
                                return DS.GetDynamicResource("ContentType_CASpart_Blush");
                            case 0x00000015:
                                return DS.GetDynamicResource("ContentType_CASpart_CostumeMakeup");
                            case 0x00000016:
                                return DS.GetDynamicResource("ContentType_CASpart_Eyebrows");
                            case 0x00000017:
                                return DS.GetDynamicResource("ContentType_CASpart_EyeColor");
                            case 0x00000018:
                                return DS.GetDynamicResource("ContentType_CASpart_Gloves");
                            case 0x00000019:
                                return DS.GetDynamicResource("ContentType_CASpart_Socks");
                            case 0x0000001a:
                                return DS.GetDynamicResource("ContentType_CASpart_Mascara");
                            case 0x0000001b:
                            case 0x0000001c:
                            case 0x0000001d:
                                return DS.GetDynamicResource("ContentType_CASpart_Freckles");
                            default:
                                return DS.GetDynamicResource("ContentType_CASpart");
                        }
                    case ContentType.preset:
                        return DS.GetDynamicResource("ContentType_preset_Style");
                    case ContentType.pattern:
                        return DS.GetDynamicResource("ContentType_pattern");
                    case ContentType.sim:
                        uint speciesFlags = GetSpeciesFlags(GenderSpeciesAgeFlags);
                        if (speciesFlags == VALUE_SPECIES_NONE || speciesFlags == VALUE_SPECIES_SIM)
                        {
                            return DS.GetDynamicResource("ContentType_sim");
                        }
                        else
                        {
                            return DS.GetDynamicResource("ContentType_sim_Pet");
                        }
                    case ContentType.household:
                        return DS.GetDynamicResource("ContentType_household");
                    case ContentType.lot:
                        return DS.GetDynamicResource("ContentType_lot");
                    case ContentType.world:
                        return DS.GetDynamicResource("ContentType_world");
                    case ContentType.haircolorinfo:
                        return DS.GetDynamicResource("ContentType_haircolorinfo_HairColor");
                    case ContentType.coatset:
                        return DS.GetDynamicResource("ContentType_coatset");
                    default:
                        return "";
                }
            }
        }

        protected ContentType superType = ContentType._;
        public ContentType SuperType
        {
            get { return superType; }
            protected set { superType = value; }
        }
        public string SuperTypeText
        {
            get
            {
                switch (SuperType)
                {
                    case ContentType.Object:
                        return DS.GetDynamicResource("ContentType_object");
                    case ContentType.CASpart:
                        return DS.GetDynamicResource("ContentType_CASpart");
                    case ContentType.preset:
                        return DS.GetDynamicResource("ContentType_preset");
                    case ContentType.pattern:
                        return DS.GetDynamicResource("ContentType_pattern");
                    case ContentType.sim:
                        return DS.GetDynamicResource("ContentType_sim");
                    case ContentType.household:
                        return DS.GetDynamicResource("ContentType_household");
                    case ContentType.lot:
                        return DS.GetDynamicResource("ContentType_lot");
                    case ContentType.world:
                        return DS.GetDynamicResource("ContentType_world");
                    case ContentType.haircolorinfo:
                        return DS.GetDynamicResource("ContentType_haircolorinfo");
                    case ContentType.coatset:
                        return DS.GetDynamicResource("ContentType_coatset");
                    case ContentType.dynamicchallenge:
                        return DS.GetDynamicResource("ContentType_dynamicchallenge");
                    default:
                        return "";
                }
            }
        }

        protected uint subType = 0x00000000;
        public uint SubType
        {
            get { return subType; }
            protected set { subType = value; }
        }
        public string SubTypeText
        {
            get
            {
                return "0x" + SubType.ToString("x8");
            }
        }

        public uint GenderSpeciesAgeFlags { get; protected set; } = 0x00000000;

        protected void SetGenderSpeciesAgeFlags(byte gender, byte species, byte age)
        {
            GenderSpeciesAgeFlags = (uint)gender * 0x1000 + (uint)species * 0x100 + age;
        }

        public string Gender
        {
            get
            {
                byte genderValue = GetGenderFlags(GenderSpeciesAgeFlags);
                List<string> genders = new List<string>(2);

                if ((genderValue & FLAG_GENDER_FEMALE) != 0)
                {
                    genders.Add(DS.GetDynamicResource("Gender_Female"));
                }
                if ((genderValue & FLAG_GENDER_MALE) != 0)
                {
                    genders.Add(DS.GetDynamicResource("Gender_Male"));
                }

                return String.Join(", ", genders);
            }
        }

        public string Species
        {
            get
            {
                switch (GetSpeciesFlags(GenderSpeciesAgeFlags))
                {
                    case VALUE_SPECIES_SIM:
                        return DS.GetDynamicResource("Species_Sim");
                    case VALUE_SPECIES_HORSE:
                        return DS.GetDynamicResource("Species_Horse");
                    case VALUE_SPECIES_CAT:
                        return DS.GetDynamicResource("Species_Cat");
                    case VALUE_SPECIES_LARGEDOG:
                        return DS.GetDynamicResource("Species_Largedog");
                    case VALUE_SPECIES_SMALLDOG:
                        return DS.GetDynamicResource("Species_Smalldog");
                    default:
                        return "";
                }
            }
        }

        public string Age
        {
            get
            {
                byte ageFlags = GetAgeFlags(GenderSpeciesAgeFlags);
                List<string> ages = new List<string>(8);

                if ((ageFlags & FLAG_AGE_BABY) != 0)
                {
                    ages.Add(DS.GetDynamicResource("Age_Baby"));
                }
                if ((ageFlags & FLAG_AGE_TODDLER) != 0)
                {
                    ages.Add(DS.GetDynamicResource("Age_Toddler"));
                }
                if ((ageFlags & FLAG_AGE_CHILD) != 0)
                {
                    ages.Add(DS.GetDynamicResource("Age_Child"));
                }
                if ((ageFlags & FLAG_AGE_TEEN) != 0)
                {
                    ages.Add(DS.GetDynamicResource("Age_Teen"));
                }
                if ((ageFlags & FLAG_AGE_YOUNGADULT) != 0)
                {
                    ages.Add(DS.GetDynamicResource("Age_Youngadult"));
                }
                if ((ageFlags & FLAG_AGE_ADULT) != 0)
                {
                    ages.Add(DS.GetDynamicResource("Age_Adult"));
                }
                if ((ageFlags & FLAG_AGE_ELDER) != 0)
                {
                    ages.Add(DS.GetDynamicResource("Age_Elder"));
                }
                if ((ageFlags & FLAG_AGE_UNDEFINED) != 0)
                {
                    ages.Add(" ");
                }

                return String.Join(", ", ages);
            }
        }

        protected internal static byte GetGenderFlags(uint genderSpeciesAgeFlags)
        {
            return (byte)((genderSpeciesAgeFlags & 0x0000F000) >> 12);
        }
        protected internal static byte GetSpeciesFlags(uint genderSpeciesAgeFlags)
        {
            return (byte)((genderSpeciesAgeFlags & 0x00000F00) >> 8);
        }
        protected internal static byte GetAgeFlags(uint genderSpeciesAgeFlags)
        {
            return (byte)(genderSpeciesAgeFlags & 0x000000FF);
        }

        public byte GenderPriority
        {
            get
            {
                switch (GetGenderFlags(GenderSpeciesAgeFlags))
                {
                    case FLAGS_GENDER_BOTH:
                        return 3;
                    case FLAG_GENDER_MALE:
                        return 2;
                    case FLAG_GENDER_FEMALE:
                        return 1;
                    default:
                        return 0;
                }
            }
        }

        public byte AgeValue
        {
            get
            {
                return GetAgeFlags(GenderSpeciesAgeFlags);
            }
        }

        public string PackageId { get; protected set; } = "";

        public DateTime Date { get; protected set; } = DateTime.MinValue;

        public string DateString
        {
            get
            {
                if (Date != DateTime.MinValue)
                {
                    return Date.ToString();
                }
                return "";
            }
        }

        public bool IsPaidContent { get; protected set; } = false;
        public string IsPaidContentText
        {
            get
            {
                if (IsPaidContent)
                {
                    return DS.GetDynamicResource("PaidContent_True");
                }
                return DS.GetDynamicResource("PaidContent_False");
            }
        }

        protected int offset;
        public abstract int GetOffset();

        protected int length;
        public int Length
        {
            get { return length; }
            protected set { length = value; }
        }
        public string Size
        {
            get
            {
                if (IsErroneous && length == 0)
                {
                    return "? " + DS.GetDynamicResource("Byte_Abbreviation");
                }

                if (length < 1000)
                {
                    return length + " " + DS.GetDynamicResource("Byte_Abbreviation");
                }
                double size = length / 1024.0;
                if (size < 1000)
                {
                    return Math.Round(size, 3) + " " + DS.GetDynamicResource("Kibibyte_Abbreviation");
                }
                size /= 1024.0;
                if (size < 1000)
                {
                    return Math.Round(size, 3) + " " + DS.GetDynamicResource("Mebibyte_Abbreviation");
                }
                return Math.Round(size, 3) + " " + DS.GetDynamicResource("Gibibyte_Abbreviation");
            }
        }

        public byte[] Thumbnail { get; protected set; } = null;

        public bool HasThumbnail
        {
            get
            {
                return Thumbnail != null;
            }
        }

        protected void CreateThumbnail(byte[] imageBytes)
        {
            try
            {
                byte[] ReturnedThumbnail;

                using (MemoryStream StartMemoryStream = new MemoryStream(),
                                    NewMemoryStream = new MemoryStream())
                {
                    StartMemoryStream.Write(imageBytes, 0, imageBytes.Length);
                    Bitmap startBitmap = new Bitmap(StartMemoryStream);
                    int newHeight;
                    if (startBitmap.Height < MAX_THUM_SIZE)
                    {
                        newHeight = startBitmap.Height;
                    }
                    else
                    {
                        newHeight = MAX_THUM_SIZE;
                    }
                    double HW_ratio = (double)((double)newHeight / (double)startBitmap.Height);
                    int newWidth = (int)(HW_ratio * (double)startBitmap.Width);
                    Bitmap newBitmap = new Bitmap(newWidth, newHeight);
                    Bitmap resizedImage = new Bitmap(newWidth, newHeight);
                    using (Graphics gfx = Graphics.FromImage(resizedImage))
                    {
                        gfx.DrawImage(startBitmap, new Rectangle(0, 0, newWidth, newHeight),
                            new Rectangle(0, 0, startBitmap.Width, startBitmap.Height), GraphicsUnit.Pixel);
                    }
                    newBitmap = resizedImage;
                    newBitmap.Save(NewMemoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    ReturnedThumbnail = NewMemoryStream.ToArray();
                }
                Thumbnail = ReturnedThumbnail;
            }
            catch (Exception)
            {
            }
        }

        public virtual int ContainedPackages
        {
            get
            {
                return Packages.Count;
            }
        }

        protected FileEncryptionState encryptionState = FileEncryptionState.Pending;

        public FileEncryptionState EncryptionState
        {
            get { return encryptionState; }
            set
            {
                encryptionState = value;
                OnPropertyChanged("EncryptionStateText");
            }
        }

        public static TS3_FileType GetTS3FileType(byte[] fileReadBuffer, long length)
        {
            if (fileReadBuffer.Length < 4)
            {
                return TS3_FileType.Invalid;
            }

            if (length >= TS3_Sims3Pack.PREAMBLE_LENGTH && fileReadBuffer[0] == 0x07 && fileReadBuffer[1] == 0x00 && fileReadBuffer[2] == 0x00 && fileReadBuffer[3] == 0x00)
            {
                return TS3_FileType.Sims3Pack;
            }

            if (length >= TS3_Package.PREAMBLE_LENGTH && fileReadBuffer[0] == DataStore.ASCII_D && fileReadBuffer[1] == DataStore.ASCII_B && fileReadBuffer[2] == DataStore.ASCII_P && (fileReadBuffer[3] == DataStore.ASCII_P || fileReadBuffer[3] == DataStore.ASCII_F))
            {
                return TS3_FileType.Package;
            }

            if (fileReadBuffer[0] == DataStore.PNG_STARTBYTE && fileReadBuffer[1] == DataStore.ASCII_P && fileReadBuffer[2] == DataStore.ASCII_N && fileReadBuffer[3] == DataStore.ASCII_G)
            {
                return TS3_FileType.PNG;
            }

            return TS3_FileType.Invalid;
        }

        public static bool IsSpecialPNGFile(byte[] fileBytes, int offset, int length, out int specialPNGOffset)
        {
            for (int i = 1; i < length - 3; i++)
            {
                if (fileBytes[offset + i] == DataStore.PNG_STARTBYTE && fileBytes[offset + 1 + i] == DataStore.ASCII_P && fileBytes[offset + 2 + i] == DataStore.ASCII_N && fileBytes[offset + 3 + i] == DataStore.ASCII_G)
                {
                    specialPNGOffset = i;
                    return true;
                }
            }

            specialPNGOffset = -1;
            return false;
        }

        public string EncryptionStateText
        {
            get
            {
                switch (EncryptionState)
                {
                    case FileEncryptionState.Pending:
                        return DS.GetDynamicResource("Pending");
                    case FileEncryptionState.None:
                        return DS.GetDynamicResource("None");
                    case FileEncryptionState.Present:
                        return DS.GetDynamicResource("Present");
                    case FileEncryptionState.Mixed:
                        return DS.GetDynamicResource("Mixed");
                    case FileEncryptionState._:
                        return DS.GetDynamicResource("_");
                    case FileEncryptionState.Unknown:
                        return DS.GetDynamicResource("Unknown");
                    default:
                        return DS.GetDynamicResource("ERROR");
                }
            }
        }

        internal Dictionary<LanguageCode, string> LocalizedNames { get; set; } = new Dictionary<LanguageCode, string>();
        public virtual string EnglishName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.en_US, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string FrenchName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.fr_FR, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string SpanishSpainName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.es_ES, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string JapaneseName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.ja_JP, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string ItalianName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.it_IT, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string KoreanName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.ko_KR, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string GermanName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.de_DE, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string ChineseTaiwanName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.zh_TW, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string ChineseChinaName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.zh_CHS, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string CzechName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.cs_CZ, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string DanishName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.da_DK, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string DutchName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.nl_NL, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string FinnishName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.fi_FI, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string GreekName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.el_GR, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string HungarianName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.hu_HU, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string NorwegianName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.no, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string PolishName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.pl_PL, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string PortuguesePortugalName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.pt_PT, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string RussianName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.ru_RU, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string SwedishName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.sv_SE, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string ThaiName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.th_TH, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string SpanishMexicoName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.es_MX, out string name))
                {
                    return name;
                }
                return "";
            }
        }
        public virtual string PortugueseBrazilName
        {
            get
            {
                if (LocalizedNames.TryGetValue(LanguageCode.pt_BR, out string name))
                {
                    return name;
                }
                return "";
            }
        }

        protected static string GetXmlNodeInnerText(XmlNode sourceNode, string query)
        {
            return sourceNode.SelectNodes("*[translate(local-name(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='" + query + "']").Item(0)?.InnerText ?? "";
        }

        protected static string GetXmlNodeInnerText(XmlNode sourceNode, string query, string subquery)
        {
            XmlNode xmlNode = sourceNode.SelectNodes("*[translate(local-name(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='" + query + "']").Item(0);
            if (xmlNode != null)
            {
                return xmlNode.SelectNodes("*[translate(local-name(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='" + subquery + "']").Item(0)?.InnerText ?? "";
            }
            return "";
        }

        protected static XmlNode GetXmlNode(XmlNode sourceNode, string query)
        {
            return sourceNode.SelectNodes("*[translate(local-name(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='" + query + "']").Item(0);
        }

        protected static XmlNodeList GetXmlNodeList(XmlNode sourceNode, string query)
        {
            return sourceNode.SelectNodes("*[translate(local-name(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='" + query + "']");
        }


    }

    public enum FileEncryptionState
    {
        Pending,
        None,
        Present,
        Mixed,
        _,
        Unknown,
        Error_Inaccessible,
        Error_PartlyInaccessible,
        Error_WrongFormat,
        Error_UnexpectedAlteration,
        Error_AlreadyUnencrypted,
        Error_SetReadPosError,
        Error_DecryptionError,
        Error_PossiblyCorrupted,
        Error_Invalid,
        Error_UnknownError
    }

    public enum TS3_FileType
    {
        Sims3Pack,
        Package,
        PNG,
        Invalid
    }
}
