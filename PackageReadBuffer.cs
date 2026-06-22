using System;
using System.Collections.Generic;
using System.IO;

namespace HFFS3CustomLauncher
{
    internal class PackageReadBuffer
    {
        private readonly List<byte[]> buffer = new List<byte[]>();

        private readonly FileStream fileStream;
        private readonly int packageLength;
        private readonly int packageOffset;
        private readonly int bufferSize;
        private const double DIVISOR = 2098000;

        internal PackageReadBuffer(FileStream _fileStream, TS3_Package _package)
        {
            fileStream = _fileStream;
            packageLength = _package.Length;
            packageOffset = _package.GetOffset();
            bufferSize = (int)Math.Ceiling(packageLength / DIVISOR) * 4096;
        }

        internal byte ReadByte(int backwardsIndex)
        {
            int localIndex = bufferSize - (packageLength - backwardsIndex);
            int bufferIndex = 0;

            while (localIndex < 0)
            {
                localIndex += bufferSize;
                bufferIndex++;
            }

            while (bufferIndex >= buffer.Count)
            {
                AddBuffer();
            }

            return buffer[bufferIndex][localIndex - (bufferSize - buffer[bufferIndex].Length)];
        }

        private void AddBuffer()
        {
            byte[] newBuffer;
            int startIndex = packageOffset + packageLength - (bufferSize * (buffer.Count + 1));

            if (startIndex < packageOffset)
            {
                newBuffer = new byte[bufferSize - (packageOffset - startIndex)];
                fileStream.Position = packageOffset;
                if (fileStream.Read(newBuffer, 0, newBuffer.Length) < newBuffer.Length)
                {
                    throw new Exception();
                }
            }
            else
            {
                newBuffer = new byte[bufferSize];
                fileStream.Position = startIndex;
                if (fileStream.Read(newBuffer, 0, bufferSize) < bufferSize)
                {
                    throw new Exception();
                }
            }

            buffer.Add(newBuffer);
        }

    }
}
