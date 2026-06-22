using System;
using System.Collections.Generic;
using System.IO;

namespace HFFS3CustomLauncher
{
    internal class PackageResourceAdministrator
    {
        private readonly FileStream fs;
        private readonly byte[] indexBytes;

        private readonly int packageOffset;
        private readonly int packageLength;
        private readonly int indexCount = 0;
        private readonly byte introSize;
        private readonly byte intervalSize;

        private readonly bool hasConstType = false;
        private readonly bool hasConstGroup = false;
        private readonly bool hasConstUpperId = false;
        private readonly bool hasNoTail = false;
        private readonly byte constTypeOffset = 0xFF;
        private readonly byte constGroupOffset = 0xFF;
        private readonly byte constUpperIdOffset = 0xFF;

        private readonly byte typeOffset = 0x00;
        private readonly byte groupOffset = 0x04;
        private readonly byte upperIdOffset = 0x08;
        private readonly byte lowerIdOffset = 0x0C;
        private readonly byte chunkoffsetOffset = 0x10;
        private readonly byte filesizeOffset = 0x14;
        private readonly byte memsizeOffset = 0x18;
        private readonly byte compressedOffset = 0x1C;

        internal PackageResourceAdministrator(FileStream _fs, int _packageOffset, int _packageLength, int indexPos, int indexLen)
        {
            fs = _fs;
            packageOffset = _packageOffset;
            packageLength = _packageLength;

            if (indexPos + indexLen > packageLength)
            {
                throw new Exception();
            }
            indexBytes = MainLogic.ReadBuffer(fs, indexLen, packageOffset + indexPos);

            byte indexType = indexBytes[0];
            introSize = 0x04;
            intervalSize = 0x20;
            byte localOffset = 0x0;

            if ((indexType & 0x1) == 0x1)
            {
                hasConstType = true;
                constTypeOffset = 0x04;
                typeOffset = 0xFF;
                introSize += 0x04;
                intervalSize -= 0x04;
                localOffset += 0x4;
            }

            if ((indexType & 0x2) == 0x2)
            {
                hasConstGroup = true;
                constGroupOffset = (byte)(0x04 + localOffset);
                groupOffset = 0xFF;
                introSize += 0x04;
                intervalSize -= 0x04;
                localOffset += 0x4;
            }
            else
            {
                groupOffset -= localOffset;
            }

            if ((indexType & 0x4) == 0x4)
            {
                hasConstUpperId = true;
                constUpperIdOffset = (byte)(0x04 + localOffset);
                upperIdOffset = 0xFF;
                introSize += 0x04;
                intervalSize -= 0x04;
                localOffset += 0x4;
            }
            else
            {
                upperIdOffset -= localOffset;
            }

            if ((indexType & 0x8) == 0x8)
            {
                hasNoTail = true;
                compressedOffset = 0xFF;
                introSize += 0x04;
                intervalSize -= 0x04;
            }
            else
            {
                compressedOffset -= localOffset;
            }

            lowerIdOffset -= localOffset;
            chunkoffsetOffset -= localOffset;
            filesizeOffset -= localOffset;
            memsizeOffset -= localOffset;

            if (indexLen > 0)
            {
                indexCount = (indexLen - introSize) / intervalSize;
            }
        }

        private uint GetType(int i)
        {
            if (hasConstType)
            {
                return indexBytes[constTypeOffset] + (uint)indexBytes[constTypeOffset + 1] * 0x100 + (uint)indexBytes[constTypeOffset + 2] * 0x10000 + (uint)indexBytes[constTypeOffset + 3] * 0x1000000;
            }

            int currEntryTypeOffset = introSize + intervalSize * i + typeOffset;
            return indexBytes[currEntryTypeOffset] + (uint)indexBytes[currEntryTypeOffset + 1] * 0x100 + (uint)indexBytes[currEntryTypeOffset + 2] * 0x10000 + (uint)indexBytes[currEntryTypeOffset + 3] * 0x1000000;
        }

        private uint GetGroup(int i)
        {
            if (hasConstGroup)
            {
                return indexBytes[constGroupOffset] + (uint)indexBytes[constGroupOffset + 1] * 0x100 + (uint)indexBytes[constGroupOffset + 2] * 0x10000 + (uint)indexBytes[constGroupOffset + 3] * 0x1000000;
            }

            int currEntryGroupOffset = introSize + intervalSize * i + groupOffset;
            return indexBytes[currEntryGroupOffset] + (uint)indexBytes[currEntryGroupOffset + 1] * 0x100 + (uint)indexBytes[currEntryGroupOffset + 2] * 0x10000 + (uint)indexBytes[currEntryGroupOffset + 3] * 0x1000000;
        }

        private ulong GetId(int i)
        {
            if (hasConstUpperId)
            {
                int currEntryLowerIdOffset = introSize + intervalSize * i + lowerIdOffset;
                return indexBytes[currEntryLowerIdOffset] + (ulong)indexBytes[currEntryLowerIdOffset + 1] * 0x100 + (ulong)indexBytes[currEntryLowerIdOffset + 2] * 0x10000 + (ulong)indexBytes[currEntryLowerIdOffset + 3] * 0x1000000 + (ulong)indexBytes[constUpperIdOffset] * 0x100000000 + (ulong)indexBytes[constUpperIdOffset + 1] * 0x10000000000 + (ulong)indexBytes[constUpperIdOffset + 2] * 0x1000000000000 + (ulong)indexBytes[constUpperIdOffset + 3] * 0x100000000000000;
            }

            int currEntryIdOffset = introSize + intervalSize * i + upperIdOffset;
            return indexBytes[currEntryIdOffset + 4] + (ulong)indexBytes[currEntryIdOffset + 5] * 0x100 + (ulong)indexBytes[currEntryIdOffset + 6] * 0x10000 + (ulong)indexBytes[currEntryIdOffset + 7] * 0x1000000 + (ulong)indexBytes[currEntryIdOffset] * 0x100000000 + (ulong)indexBytes[currEntryIdOffset + 1] * 0x10000000000 + (ulong)indexBytes[currEntryIdOffset + 2] * 0x1000000000000 + (ulong)indexBytes[currEntryIdOffset + 3] * 0x100000000000000;
        }

        private uint GetChunkoffset(int i)
        {
            int currEntryChunkoffsetOffset = introSize + intervalSize * i + chunkoffsetOffset;
            return indexBytes[currEntryChunkoffsetOffset] + (uint)indexBytes[currEntryChunkoffsetOffset + 1] * 0x100 + (uint)indexBytes[currEntryChunkoffsetOffset + 2] * 0x10000 + (uint)indexBytes[currEntryChunkoffsetOffset + 3] * 0x1000000;
        }

        private int GetFilesize(int i)
        {
            int currEntryFilesizeOffset = introSize + intervalSize * i + filesizeOffset;
            return indexBytes[currEntryFilesizeOffset] + indexBytes[currEntryFilesizeOffset + 1] * 0x100 + indexBytes[currEntryFilesizeOffset + 2] * 0x10000 + (indexBytes[currEntryFilesizeOffset + 3] & 0x7F) * 0x1000000;
        }

        private uint GetMemsize(int i)
        {
            int currEntryMemsizeOffset = introSize + intervalSize * i + memsizeOffset;
            return indexBytes[currEntryMemsizeOffset] + (uint)indexBytes[currEntryMemsizeOffset + 1] * 0x100 + (uint)indexBytes[currEntryMemsizeOffset + 2] * 0x10000 + (uint)indexBytes[currEntryMemsizeOffset + 3] * 0x1000000;
        }

        private bool IsCompressed(int i)
        {
            if (hasNoTail)
            {
                return GetFilesize(i) != GetMemsize(i);
            }

            int currEntryCompressedOffset = introSize + intervalSize * i + compressedOffset;
            return indexBytes[currEntryCompressedOffset] != 0x00 || indexBytes[currEntryCompressedOffset + 1] != 0x00;
        }

        internal List<byte[]> GetResourcesByType(uint wantedType)
        {
            List<byte[]> resourceBytes = new List<byte[]>();
            for (int i = 0; i < indexCount; i++)
            {
                if (GetType(i) == wantedType)
                {
                    resourceBytes.Add(GetResourceBytes(i));
                }
            }
            return resourceBytes;
        }

        internal byte[] GetResourceByTypeAndId(uint wantedType, ulong wantedId)
        {
            for (int i = 0; i < indexCount; i++)
            {
                if (GetType(i) == wantedType && GetId(i) == wantedId)
                {
                    return GetResourceBytes(i);
                }
            }
            return new byte[0];
        }

        private byte[] GetResourceBytes(int i)
        {
            uint chunkoffset = GetChunkoffset(i);
            int fileSize = GetFilesize(i);

            if (chunkoffset + fileSize > packageLength)
            {
                return new byte[0];
            }
            byte[] rawResourceBytes = MainLogic.ReadBuffer(fs, fileSize, packageOffset + chunkoffset);

            if (IsCompressed(i))
            {
                return DecompressResource(rawResourceBytes, GetMemsize(i));
            }

            return rawResourceBytes;
        }

        private byte[] DecompressResource(byte[] comResourceBytes, uint indexMemsize)
        {
            try
            {
                uint memsize;
                uint comCursor = 0;
                if ((comResourceBytes[0] == 0x10 || comResourceBytes[0] == 0x40 || comResourceBytes[0] == 0x50) && comResourceBytes[1] == 0xFB)
                {
                    memsize = (uint)(comResourceBytes[2] * 0x10000 + comResourceBytes[3] * 0x100 + comResourceBytes[4]);
                    comCursor += 5;
                }
                else if (comResourceBytes[0] == 0x80 && comResourceBytes[1] == 0xFB)
                {
                    memsize = (uint)((uint)comResourceBytes[2] * 0x1000000 + comResourceBytes[3] * 0x10000 + comResourceBytes[4] * 0x100 + comResourceBytes[5]);
                    comCursor += 6;
                }
                else
                {
                    return new byte[0];
                }

                if (memsize != indexMemsize)
                {
                    return new byte[0];
                }

                uint decCursor = 0;
                byte[] decResourceBytes = new byte[memsize];
                while (true)
                {
                    int numPlainText = 0;
                    int numToCopy = 0;
                    int copyOffset = 0;

                    if (comResourceBytes[comCursor] < 0x80)
                    {
                        numPlainText = comResourceBytes[comCursor] & 0x03;
                        numToCopy = ((comResourceBytes[comCursor] & 0x1C) >> 2) + 3;
                        copyOffset = ((comResourceBytes[comCursor] & 0x60) * 0x8) + comResourceBytes[comCursor + 1] + 1;
                        comCursor += 2;
                    }
                    else if (comResourceBytes[comCursor] < 0xC0)
                    {
                        numPlainText = ((comResourceBytes[comCursor + 1] & 0xC0) >> 6) & 0x03;
                        numToCopy = (comResourceBytes[comCursor] & 0x3F) + 4;
                        copyOffset = ((comResourceBytes[comCursor + 1] & 0x3F) * 0x100) + comResourceBytes[comCursor + 2] + 1;
                        comCursor += 3;
                    }
                    else if (comResourceBytes[comCursor] < 0xE0)
                    {
                        numPlainText = comResourceBytes[comCursor] & 0x03;
                        numToCopy = ((comResourceBytes[comCursor] & 0x0C) * 0x40) + comResourceBytes[comCursor + 3] + 5;
                        copyOffset = ((comResourceBytes[comCursor] & 0x10) * 0x1000) + (comResourceBytes[comCursor + 1] * 0x100) + comResourceBytes[comCursor + 2] + 1;
                        comCursor += 4;
                    }
                    else if (comResourceBytes[comCursor] < 0xFC)
                    {
                        numPlainText = ((comResourceBytes[comCursor] & 0x1F) << 2) + 4;
                        numToCopy = 0;
                        copyOffset = 0;
                        comCursor++;
                    }
                    else
                    {
                        numPlainText = comResourceBytes[comCursor] & 0x03;
                        comCursor++;
                        for (int i = 0; i < numPlainText; i++)
                        {
                            decResourceBytes[decCursor] = comResourceBytes[comCursor];
                            decCursor++;
                            comCursor++;
                        }

                        if (decCursor != memsize || comCursor > comResourceBytes.Length)
                        {
                            break;
                        }

                        return decResourceBytes;
                    }

                    for (int i = 0; i < numPlainText; i++)
                    {
                        decResourceBytes[decCursor] = comResourceBytes[comCursor];
                        decCursor++;
                        comCursor++;
                    }
                    for (int i = 0; i < numToCopy; i++)
                    {
                        decResourceBytes[decCursor] = decResourceBytes[decCursor - copyOffset];
                        decCursor++;
                    }
                }
            }
            catch (Exception)
            {
            }

            return new byte[0];
        }
    }
}
