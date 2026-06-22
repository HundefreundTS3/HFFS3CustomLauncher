using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Xml;

namespace HFFS3CustomLauncher
{
    public enum DecryptionSafetyLevel
    {
        Pending,
        Completely_Safe,
        Totally_Safe,
        Very_Safe,
        Slightly_Safe,
        Not_Safe
    }

    public class TS3_Package : TS3_File
    {
        public const int PREAMBLE_LENGTH = 0x60;

        private TS3_Sims3Pack parent;

        public override string PhysicalFilename
        {
            get
            {
                if (HasParent)
                {
                    return parent.PhysicalFilename;
                }
                else
                {
                    return Filename;
                }
            }
        }

        public override string ResourceName
        {
            get
            {
                if (HasParent)
                {
                    return Filename;
                }
                else
                {
                    return "";
                }
            }
        }

        public override int ContainedPackages
        {
            get
            {
                if (Packages.Count > 0)
                {
                    return Packages.Count;
                }
                return 1;
            }
        }

        private uint order;
        public override uint Order
        {
            get { return order; }
        }
        public override string OrderText
        {
            get
            {
                if (order == 0)
                {
                    return "";
                }
                return order.ToString() + "/" + (parent?.ContainedPackages.ToString() ?? "?");
            }
        }

        public override bool HasParent
        {
            get
            {
                return parent != null;
            }
        }

        private DecryptionSafetyLevel decryptionSafetyLevel = DecryptionSafetyLevel.Pending;
        public DecryptionSafetyLevel DecryptionSafetyLevel
        {
            get { return decryptionSafetyLevel; }
            set { decryptionSafetyLevel = value; }
        }

        public override int GetOffset()
        {
            if (parent != null)
            {
                return parent.GetOffset() + offset;
            }
            else
            {
                return 0;
            }
        }

        public override bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged("ContainsSelection");
                parent?.ReevaluateSelectedValue(value);
            }
        }

        public void EditSelectedAsParent(bool newValue)
        {
            isSelected = newValue;
            OnPropertyChanged("ContainsSelection");
            OnPropertyChanged("IsSelected");
        }

        private int decryptedPreambleIndex = -1;
        internal int DecryptedPreambleIndex
        {
            get
            {
                return decryptedPreambleIndex;
            }
            set
            {
                decryptedPreambleIndex = value;
            }
        }

        internal TS3_Package(TS3_Sims3Pack _parent) : base("", _parent.LastWriteTime)
        {
            length = 0;
            offset = 0;
            parent = _parent;
            encryptionState = FileEncryptionState.None;

            order = 1;
            superType = ContentType._;
            subType = 0x00000000;
            displaytitle = "";
            PackageId = "";
            Date = DateTime.MinValue;
            Thumbnail = null;
            IsPaidContent = false;
            IsErroneous = false;
        }

        internal TS3_Package(bool _, FileInfo file) : base(file.Name, file.LastWriteTime)
        {
            IsErroneous = true;
            encryptionState = FileEncryptionState.Unknown;
        }

        internal TS3_Package(bool _, string _filename, uint _packageCount, TS3_Sims3Pack _parent) : base(_filename, _parent.LastWriteTime)
        {
            IsErroneous = true;
            encryptionState = FileEncryptionState.Unknown;
            order = _packageCount;
            parent = _parent;
        }

        internal TS3_Package(FileInfo file) : base(file.Name, file.LastWriteTime)
        {
            if (file.Length > Int32.MaxValue)
            {
                throw new Exception();
            }
            length = (int)file.Length;
            offset = 0;
            parent = null;
            order = 0;
        }

        internal TS3_Package(FileInfo file, FileStream fs, DecryptionLogic decryptionTool) : base(file.Name, file.LastWriteTime)
        {
            if (file.Length > Int32.MaxValue)
            {
                throw new Exception();
            }
            length = (int)file.Length;
            offset = 0;
            parent = null;
            order = 0;
            Initialize(fs, decryptionTool);
        }

        internal TS3_Package(string _filename, int _length, int _offset, uint _order, TS3_Sims3Pack _parent, FileStream fs, DecryptionLogic decryptionTool) : base(_filename, _parent.LastWriteTime)
        {
            length = _length;
            offset = _offset;
            parent = _parent;
            order = _order;
            Initialize(fs, decryptionTool);
        }

        public static bool IsEncryptedPackage(byte[] packageFileBytes)
        {
            return IsEncryptedPackage(packageFileBytes, 0);
        }

        public static bool IsEncryptedPackage(byte[] fileBytes, int offset)
        {
            if (fileBytes != null && fileBytes.Length >= PREAMBLE_LENGTH && fileBytes[offset] == DataStore.ASCII_D && fileBytes[offset + 1] == DataStore.ASCII_B && fileBytes[offset + 2] == DataStore.ASCII_P && fileBytes[offset + 3] == DataStore.ASCII_P)
            {
                return true;
            }

            return false;
        }

        private Initialization_ResultCode Initialize(FileStream fs, DecryptionLogic decryptionTool)
        {
            byte[] packagePreamble = new byte[PREAMBLE_LENGTH];
            fs.Position = GetOffset();
            if (fs.Read(packagePreamble, 0, PREAMBLE_LENGTH) < PREAMBLE_LENGTH)
            {
                return Initialization_ResultCode.Error_PartlyInaccessible;
            }

            decryptionTool.SetPackageEncryptionState(packagePreamble, this);
            int indexPos = 0;
            int indexLen = 0;
            if (EncryptionState == FileEncryptionState.Present)
            {
                PackageReadBuffer packageReadBuffer = new PackageReadBuffer(fs, this);
                decryptionTool.InvestigatePackageEncryption(fs, packagePreamble, packageReadBuffer, this, out indexPos, out indexLen);
            }
            else
            {
                if (EncryptionState != FileEncryptionState.None)
                {
                    return Initialization_ResultCode.Error_InvalidEncryptionState;
                }

                indexPos = packagePreamble[0x40] + packagePreamble[0x41] * 0x100 + packagePreamble[0x42] * 0x10000 + packagePreamble[0x43] * 0x1000000;
                if (indexPos == 0)
                {
                    indexPos = packagePreamble[0x28] + packagePreamble[0x29] * 0x100 + packagePreamble[0x2A] * 0x10000 + packagePreamble[0x2B] * 0x1000000;
                }
                indexLen = packagePreamble[0x2C] + packagePreamble[0x2D] * 0x100 + packagePreamble[0x2E] * 0x10000 + packagePreamble[0x2F] * 0x1000000;
            }

            if (indexPos <= 0 || indexLen <= 0)
            {
                return Initialization_ResultCode.Success;
            }

            PackageResourceAdministrator packAdmin = new PackageResourceAdministrator(fs, GetOffset(), Length, indexPos, indexLen);
            byte[] uncompressedXMLResource = packAdmin.GetResourceByTypeAndId(0x73E93EEB, 0x0);

            if (uncompressedXMLResource.Length > 0)
            {
                try
                {
                    XmlDocument xml = new XmlDocument();
                    using (MemoryStream ms = new MemoryStream(uncompressedXMLResource))
                    {
                        xml.Load(ms);
                    }

                    string xmlDocElemName = xml.DocumentElement?.Name.ToLower() ?? "";
                    if (xmlDocElemName == "manifest")
                    {
                        XmlNode manifest = xml.DocumentElement;

                        foreach (XmlAttribute xmlAttribute in manifest.Attributes)
                        {
                            try
                            {
                                switch (xmlAttribute.Name.ToLower())
                                {
                                    case "packagetype":
                                        if (Enum.TryParse(xmlAttribute.Value, true, out ContentType contentType))
                                        {
                                            SuperType = contentType;
                                        }
                                        break;
                                    case "packagesubtype":
                                        SubType = Convert.ToUInt32(xmlAttribute.Value, 16);
                                        break;
                                    case "paidcontent":
                                        IsPaidContent = xmlAttribute.Value.ToLower().Equals("true") ? true : false;
                                        break;
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }

                        string packagetitle = GetXmlNodeInnerText(manifest, "packagetitle");
                        if (!String.IsNullOrEmpty(packagetitle))
                        {
                            Displaytitle = packagetitle;
                        }

                        string packageid = GetXmlNodeInnerText(manifest, "packageid");
                        if (!String.IsNullOrEmpty(packageid))
                        {
                            PackageId = packageid;
                        }

                        string packagedate = GetXmlNodeInnerText(manifest, "packagedate");
                        if (!String.IsNullOrEmpty(packagedate))
                        {
                            try
                            {
                                Date = DateTime.Parse(packagedate, new CultureInfo("en-US", false));
                            }
                            catch (Exception)
                            {
                            }
                        }

                        string thumbnail = GetXmlNodeInnerText(manifest, "thumbnail");
                        if (!String.IsNullOrEmpty(thumbnail))
                        {
                            try
                            {
                                string[] thumbnailValues = thumbnail.Split(':');
                                uint thumbType = Convert.ToUInt32(thumbnailValues[0], 16);
                                ulong thumbId = Convert.ToUInt64(thumbnailValues[2], 16);
                                byte[] uncompressedThumResource = packAdmin.GetResourceByTypeAndId(thumbType, thumbId);

                                if (uncompressedThumResource.Length > 0)
                                {
                                    CreateThumbnail(uncompressedThumResource);
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }

                        XmlNode localizednames = GetXmlNode(manifest, "localizednames");
                        if (localizednames != null)
                        {
                            foreach (XmlNode localizedname in localizednames)
                            {
                                if (localizedname.Name.ToLower() == "localizedname")
                                {
                                    foreach (XmlAttribute xmlAttribute in localizedname.Attributes)
                                    {
                                        try
                                        {
                                            switch (xmlAttribute.Name.ToLower())
                                            {
                                                case "language":
                                                    string language = xmlAttribute.Value.Replace("-", "_");
                                                    if (Enum.TryParse(language, true, out LanguageCode languageCode))
                                                    {
                                                        string localizednameString = localizedname.InnerText;
                                                        if (localizednameString.StartsWith("![CDATA[") && localizednameString.EndsWith("]]"))
                                                        {
                                                            localizednameString = localizednameString.Substring(8, localizednameString.Length - 10);
                                                        }
                                                        LocalizedNames.Add(languageCode, localizednameString);
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

                        XmlNode metatags = GetXmlNode(manifest, "metatags");
                        if (metatags != null)
                        {
                            string agegenderflags = GetXmlNodeInnerText(metatags, "agegenderflags");
                            if (!String.IsNullOrEmpty(agegenderflags))
                            {
                                try
                                {
                                    GenderSpeciesAgeFlags = Convert.ToUInt32(agegenderflags, 16);
                                }
                                catch (Exception)
                                {
                                }
                            }
                            else
                            {
                                string age = GetXmlNodeInnerText(metatags, "age");
                                uint ageValue = 0x0;
                                if (!String.IsNullOrEmpty(age))
                                {
                                    try
                                    {
                                        ageValue = Convert.ToUInt32(age, 16);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                string species = GetXmlNodeInnerText(metatags, "species");
                                uint speciesValue = 0x0;
                                if (!String.IsNullOrEmpty(species))
                                {
                                    try
                                    {
                                        speciesValue = Convert.ToUInt32(species, 16);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                string gender = GetXmlNodeInnerText(metatags, "gender");
                                uint genderValue = 0x0;
                                if (!String.IsNullOrEmpty(gender))
                                {
                                    try
                                    {
                                        genderValue = Convert.ToUInt32(gender, 16);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                GenderSpeciesAgeFlags = genderValue + speciesValue + ageValue;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return Initialization_ResultCode.Error_WrongXMLFormat;
                }
            }

            return Initialization_ResultCode.Success;
        }
    }
}
