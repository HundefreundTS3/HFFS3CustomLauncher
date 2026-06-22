using System;
using System.Collections.Generic;
using System.IO;

namespace HFFS3CustomLauncher
{
    public enum Cryption_ResultCode
    {
        UnknownError = -1,
        NoFilesSelected,
        WrongOperation,
        Success
    }

    internal class DecryptionLogic
    {
        private List<byte[]> DecryptedPreambles { get; set; } = new List<byte[]>();
        internal byte[] GetDecryptedPreamble(int index)
        {
            if (index >= 0 && index < DecryptedPreambles.Count)
            {
                return DecryptedPreambles[index];
            }
            else
            {
                return null;
            }
        }

        private void DecryptAPackage(FileStream fs, TS3_Package package)
        {
            byte[] packagePreamble;
            try
            {
                packagePreamble = MainLogic.ReadBuffer(fs, TS3_Package.PREAMBLE_LENGTH, package.GetOffset());

                if (package.EncryptionState == FileEncryptionState.Present)
                {
                    packagePreamble = GetDecryptedPreamble(package.DecryptedPreambleIndex);
                }
                else if (package.EncryptionState != FileEncryptionState.None)
                {
                    package.EncryptionState = FileEncryptionState.Error_Invalid;
                    return;
                }

                package.EncryptionState = FileEncryptionState.None;

                fs.Position = package.GetOffset();
            }
            catch (Exception)
            {
                package.EncryptionState = FileEncryptionState.Error_DecryptionError;
                return;
            }

            try
            {
                fs.Write(packagePreamble, 0, TS3_Package.PREAMBLE_LENGTH);
            }
            catch (Exception)
            {
                package.EncryptionState = FileEncryptionState.Error_PossiblyCorrupted;
            }
        }

        internal void Decrypt(string filePath, TS3_File file)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    file.EncryptionState = FileEncryptionState.Error_Inaccessible;
                    return;
                }
                if (new FileInfo(filePath).LastWriteTime != file.LastWriteTime)
                {
                    file.EncryptionState = FileEncryptionState.Error_UnexpectedAlteration;
                    return;
                }

                if (file is TS3_Sims3Pack sims3Pack)
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        foreach (TS3_Package package in sims3Pack.Packages)
                        {
                            if (!package.IsSelected)
                            {
                                continue;
                            }
                            if (package.EncryptionState == FileEncryptionState.None)
                            {
                                package.EncryptionState = FileEncryptionState.Error_AlreadyUnencrypted;
                                continue;
                            }

                            try
                            {
                                fs.Position = package.GetOffset();
                            }
                            catch (Exception)
                            {
                                package.EncryptionState = FileEncryptionState.Error_SetReadPosError;
                                continue;
                            }
                            DecryptAPackage(fs, package);
                        }
                    }
                }
                else if (file is TS3_Package package)
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        if (package.EncryptionState == FileEncryptionState.None)
                        {
                            package.EncryptionState = FileEncryptionState.Error_AlreadyUnencrypted;
                        }
                        else
                        {
                            DecryptAPackage(fs, package);
                        }
                    }
                }
                else
                {
                    file.EncryptionState = FileEncryptionState.Error_WrongFormat;
                }
            }
            catch (Exception)
            {
                file.EncryptionState = FileEncryptionState.Error_UnknownError;
            }
        }

        internal void InvestigatePackageEncryption(FileStream fs, byte[] packagePreamble, PackageReadBuffer packageReadBuffer, TS3_Package package, out int indexPos, out int indexLen)
        {
            AnalysePackageEncryption(fs, packagePreamble, packageReadBuffer, package, out indexPos, out indexLen);
            DecryptedPreambles.Add(packagePreamble);
            package.DecryptedPreambleIndex = DecryptedPreambles.Count - 1;
        }

        internal void SetPackageEncryptionState(byte[] preamble, TS3_Package package)
        {
            if (preamble[3] == DataStore.ASCII_F)
            {
                package.EncryptionState = FileEncryptionState.None;
            }
            else
            {
                package.EncryptionState = FileEncryptionState.Present;
            }
        }

        private void AnalysePackageEncryption(FileStream fs, byte[] packagePreamble, PackageReadBuffer packageReadBuffer, TS3_Package package, out int indexPos, out int indexLen)
        {
            int packageLength = package.Length;

            int decResCount = 0;
            int decIndexLen = 0;
            int decIndexPos = 0;

            int specialOffset;
            for (specialOffset = 0; specialOffset < 2; specialOffset++)
            {
                for (int i = packageLength - 1; i >= TS3_Package.PREAMBLE_LENGTH; i--)
                {
                    if ((specialOffset == 0 && IsLegitResource(fs, packageReadBuffer, package, i)) || (specialOffset > 0 && IsLegitResourceWithoutTail(fs, packageReadBuffer, package, i)))
                    {
                        int furthestReachedIndex = i;

                        for (int j = specialOffset; j < 4 + specialOffset; j++)
                        {
                            decResCount = 1;
                            int intervalSize = 0x20 - j * 0x04;
                            while (decResCount * intervalSize < i - (TS3_Package.PREAMBLE_LENGTH + (0x24 - intervalSize)))
                            {
                                int resEndIndex = i - decResCount * intervalSize;
                                if ((specialOffset == 0 && IsLegitResource(fs, packageReadBuffer, package, resEndIndex)) || (specialOffset > 0 && IsLegitResourceWithoutTail(fs, packageReadBuffer, package, resEndIndex)))
                                {
                                    decResCount++;
                                    continue;
                                }
                                int indexStartpoint = resEndIndex - 3 - (0x20 - intervalSize);
                                byte typeByte = packageReadBuffer.ReadByte(indexStartpoint);
                                if (((typeByte & 0xF8) == (specialOffset * 0x08)) && (CountSetBits(typeByte, 3 + specialOffset) == j) &&
                                    packageReadBuffer.ReadByte(indexStartpoint + 1) == 0x00 && packageReadBuffer.ReadByte(indexStartpoint + 2) == 0x00 && packageReadBuffer.ReadByte(indexStartpoint + 3) == 0x00)
                                {
                                    decIndexLen = intervalSize * decResCount + (0x24 - intervalSize);
                                    decIndexPos = resEndIndex - (0x23 - intervalSize);
                                    break;
                                }
                                break;
                            }

                            if (decIndexLen > 0)
                            {
                                break;
                            }
                            if (i - decResCount * intervalSize + 1 < furthestReachedIndex)
                            {
                                furthestReachedIndex = i - decResCount * intervalSize + 1;
                            }
                        }

                        if (decIndexLen > 0)
                        {
                            break;
                        }
                        i = furthestReachedIndex;
                    }
                }

                if (decIndexLen > 0) break;
            }

            if (decIndexLen == 0)
            {
                decResCount = 0;
                for (int i = packageLength - 1; i >= TS3_Package.PREAMBLE_LENGTH; i--)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        int intervalSize = 0x20 - j * 0x04;
                        int indexStartpoint = i - 3 - (0x20 - intervalSize);
                        byte typeByte = packageReadBuffer.ReadByte(indexStartpoint);
                        if ((typeByte & 0xF0) == 0x00 && CountSetBits(typeByte, 4) == j &&
                            packageReadBuffer.ReadByte(indexStartpoint + 1) == 0x00 && packageReadBuffer.ReadByte(indexStartpoint + 2) == 0x00 && packageReadBuffer.ReadByte(indexStartpoint + 3) == 0x00)
                        {
                            decIndexLen = 0x24 - intervalSize;
                            decIndexPos = i - (0x23 - intervalSize);
                            break;
                        }
                    }

                    if (decIndexLen > 0)
                    {
                        break;
                    }
                }

                package.DecryptionSafetyLevel = DecryptionSafetyLevel.Not_Safe;
            }
            else if (specialOffset == 0)
            {
                if (decIndexPos + decIndexLen == packageLength)
                {
                    package.DecryptionSafetyLevel = DecryptionSafetyLevel.Totally_Safe;
                }
                else
                {
                    package.DecryptionSafetyLevel = DecryptionSafetyLevel.Very_Safe;
                }
            }
            else
            {
                package.DecryptionSafetyLevel = DecryptionSafetyLevel.Slightly_Safe;
            }

            CreateDecryptedPreamble(packagePreamble, decResCount, decIndexLen, decIndexPos);
            indexPos = decIndexPos;
            indexLen = decIndexLen;
        }

        private void CreateDecryptedPreamble(byte[] packageBytes, int decResCount, int decIndexLen, int decIndexPos)
        {
            for (int i = 0; i < 0x60; i++)
            {
                packageBytes[i] = 0x00;
            }

            packageBytes[0x00] = DataStore.ASCII_D;
            packageBytes[0x01] = DataStore.ASCII_B;
            packageBytes[0x02] = DataStore.ASCII_P;
            packageBytes[0x03] = DataStore.ASCII_F;

            packageBytes[0x04] = 0x02;

            packageBytes[0x24] = (byte)(decResCount & 0xFF);
            packageBytes[0x25] = (byte)((decResCount & 0xFF00) >> 8);
            packageBytes[0x26] = (byte)((decResCount & 0xFF0000) >> 16);
            packageBytes[0x27] = (byte)((decResCount & 0xFF000000) >> 24);

            packageBytes[0x2C] = (byte)(decIndexLen & 0xFF);
            packageBytes[0x2D] = (byte)((decIndexLen & 0xFF00) >> 8);
            packageBytes[0x2E] = (byte)((decIndexLen & 0xFF0000) >> 16);
            packageBytes[0x2F] = (byte)((decIndexLen & 0xFF000000) >> 24);

            packageBytes[0x3C] = 0x03;

            packageBytes[0x40] = (byte)(decIndexPos & 0xFF);
            packageBytes[0x41] = (byte)((decIndexPos & 0xFF00) >> 8);
            packageBytes[0x42] = (byte)((decIndexPos & 0xFF0000) >> 16);
            packageBytes[0x43] = (byte)((decIndexPos & 0xFF000000) >> 24);
        }

        private int CountSetBits(byte aByte, int offset)
        {
            int countedBits = 0;
            for (int i = 0; i < offset; i++)
            {
                countedBits += aByte & 1;
                aByte = (byte)(aByte >> 1);
            }
            return countedBits;
        }

        private bool IsLegitResource(FileStream fs, PackageReadBuffer packageReadBuffer, TS3_Package package, int resEndpointInsideIndex)
        {
            if (packageReadBuffer.ReadByte(resEndpointInsideIndex) == 0x00 && packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x1) == 0x01 &&
                    ((packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x2) == 0x00 && packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x3) == 0x00) || (packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x2) == 0xFF && packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x3) == 0xFF)) &&
                    (packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x8) & 0x80) == 0x80)
            {
                uint resChunkoffset = (uint)(packageReadBuffer.ReadByte(resEndpointInsideIndex - 0xF) + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0xE) * 0x100 + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0xD) * 0x10000 + (uint)packageReadBuffer.ReadByte(resEndpointInsideIndex - 0xC) * 0x1000000);
                int resFilesize = packageReadBuffer.ReadByte(resEndpointInsideIndex - 0xB) + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0xA) * 0x100 + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x9) * 0x10000 + (packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x8) & 0x7F) * 0x1000000;
                uint resMemsize = (uint)(packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x7) + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x6) * 0x100 + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x5) * 0x10000 + (uint)packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x4) * 0x1000000);

                if (resChunkoffset >= TS3_Package.PREAMBLE_LENGTH && UInt32.MaxValue - resChunkoffset >= resFilesize && resChunkoffset + resFilesize <= package.Length)
                {
                    if (packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x2) == 0x00 && packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x3) == 0x00)
                    {
                        if (resFilesize == resMemsize)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        byte[] resStartBytes = MainLogic.ReadBuffer(fs, 6, package.GetOffset() + resChunkoffset);

                        uint resMemsizeInResource;
                        if ((resStartBytes[0] == 0x10 || resStartBytes[0] == 0x40 || resStartBytes[0] == 0x50) && resStartBytes[1] == 0xFB)
                        {
                            resMemsizeInResource = (uint)(resStartBytes[2] * 0x10000 + resStartBytes[3] * 0x100 + resStartBytes[4]);

                        }
                        else if (resStartBytes[0] == 0x80 && resStartBytes[1] == 0xFB)
                        {
                            resMemsizeInResource = (uint)((uint)resStartBytes[2] * 0x1000000 + resStartBytes[3] * 0x10000 + resStartBytes[4] * 0x100 + resStartBytes[5]);
                        }
                        else
                        {
                            return false;
                        }

                        if (resMemsizeInResource == resMemsize)
                        {
                            byte[] resEndBytes = MainLogic.ReadBuffer(fs, 4, package.GetOffset() + resChunkoffset + resFilesize - 4);
                            if (resEndBytes[3] == 0xFC || resEndBytes[2] == 0xFD || resEndBytes[1] == 0xFE || resEndBytes[0] == 0xFF)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private bool IsLegitResourceWithoutTail(FileStream fs, PackageReadBuffer packageReadBuffer, TS3_Package package, int resEndpointInsideIndex)
        {
            if ((packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x4) & 0x80) == 0x80)
            {
                uint resChunkoffset = (uint)(packageReadBuffer.ReadByte(resEndpointInsideIndex - 0xB) + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0xA) * 0x100 + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x9) * 0x10000 + (uint)packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x8) * 0x1000000);
                int resFilesize = packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x7) + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x6) * 0x100 + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x5) * 0x10000 + (packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x4) & 0x7F) * 0x1000000;
                uint resMemsize = (uint)(packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x3) + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x2) * 0x100 + packageReadBuffer.ReadByte(resEndpointInsideIndex - 0x1) * 0x10000 + (uint)packageReadBuffer.ReadByte(resEndpointInsideIndex) * 0x1000000);

                if (resChunkoffset >= TS3_Package.PREAMBLE_LENGTH && UInt32.MaxValue - resChunkoffset >= resFilesize && resChunkoffset + resFilesize <= package.Length)
                {
                    if (resFilesize == resMemsize)
                    {
                        return true;
                    }

                    byte[] resStartBytes = MainLogic.ReadBuffer(fs, 6, package.GetOffset() + resChunkoffset);

                    uint resMemsizeInResource;
                    if ((resStartBytes[0] == 0x10 || resStartBytes[0] == 0x40 || resStartBytes[0] == 0x50) && resStartBytes[1] == 0xFB)
                    {
                        resMemsizeInResource = (uint)(resStartBytes[2] * 0x10000 + resStartBytes[3] * 0x100 + resStartBytes[4]);

                    }
                    else if (resStartBytes[0] == 0x80 && resStartBytes[1] == 0xFB)
                    {
                        resMemsizeInResource = (uint)((uint)resStartBytes[2] * 0x1000000 + resStartBytes[3] * 0x10000 + resStartBytes[4] * 0x100 + resStartBytes[5]);
                    }
                    else
                    {
                        return false;
                    }

                    if (resMemsizeInResource == resMemsize)
                    {
                        byte[] resEndBytes = MainLogic.ReadBuffer(fs, 4, package.GetOffset() + resChunkoffset + resFilesize - 4);

                        if (resEndBytes[3] == 0xFC || resEndBytes[2] == 0xFD || resEndBytes[1] == 0xFE || resEndBytes[0] == 0xFF)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
