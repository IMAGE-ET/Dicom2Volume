﻿// ****************************************************************************************
// Copyright (C) 2010, Jorn Skaarud Karlsen 
// All rights reserved. 
//
// Redistribution and use in source and binary forms, with or without modification, are 
// permitted provided that the following conditions are met: 
//
// * Redistributions of source code must retain the above copyright notice, this list of 
//   conditions and the following disclaimer. 
// * Redistributions in binary form must reproduce the above copyright notice, this list 
//   of conditions and the following disclaimer in the documentation and/or other 
//   materials provided with the distribution. 
// * Neither the name of Dicom2Volume nor the names of its contributors may be used to 
//   endorse or promote products derived from this software without specific prior 
//   written permission. 
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY 
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL 
// THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT 
// OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//****************************************************************************************

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Dicom2Volume
{
    public enum FormatEnum : uint
    {
        R16Uint = 57,
    }

    [Flags]
    public enum PixelFormatFlags : uint
    {
        Luminance = 0x20000
    }

    [Flags]
    public enum SurfaceFlags : uint
    {
        CapsTexture = 0x1000
    }

    [Flags]
    public enum HeaderFlags : uint
    {
        Caps = 0x1,
        Height = 0x2,
        Width = 0x4,
        Pixelformat = 0x1000,
        Depth = 0x800000
    }

    [Flags]
    public enum CubeMapFlags : uint
    {
        Caps2Volume = 0x200000
    }

    // Ref: http://msdn.microsoft.com/en-us/library/bb943991(v=VS.85).aspx
    public class Dds
    {
        public static byte[] MagicBytes = new byte[] { 0x44, 0x44, 0x53, 0x20 }; // MagicWord: 0x20534444;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public uint Size;
            public HeaderFlags HeaderFlag;
            public uint Height;
            public uint Width;
            public uint PitchOrLinearSize;
            public uint Depth;
            public uint MipMapCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public uint[] Reserved1;
            public PixelFormat Format;
            public SurfaceFlags SurfaceFlag;
            public CubeMapFlags CubemapFlag;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Reserved2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PixelFormat
        {
            public uint Size;
            public PixelFormatFlags Flag;
            public uint FourCc;
            public uint RgbBitCount;
            public uint RBitMask;
            public uint GBitMask;
            public uint BBitMask;
            public uint ABitMask;
        }

        public static string ConvertRawToDds(string sourceFilename, int width, int height, int depth, string outputFilename)
        {
            var header = new Header()
            {
                Size = 124,
                HeaderFlag = HeaderFlags.Caps | HeaderFlags.Height | HeaderFlags.Width | HeaderFlags.Depth | HeaderFlags.Pixelformat,
                Height = (uint)height,
                Width =  (uint)width,
                PitchOrLinearSize = 0,
                Depth = (uint)depth,
                MipMapCount = 0,
                Format = new PixelFormat()
                {
                    Size = 32,
                    Flag = PixelFormatFlags.Luminance,
                    FourCc = 0,
                    RgbBitCount = 16,
                    RBitMask = 0xffff,
                    GBitMask = 0x0,
                    BBitMask = 0x0,
                    ABitMask = 0x0
                },
                SurfaceFlag = SurfaceFlags.CapsTexture,
                CubemapFlag = CubeMapFlags.Caps2Volume,
            };

            using (var outputStream = File.Create(outputFilename))
            {
                var headerBytes = Utils.RawSerialize(header);
                outputStream.Write(MagicBytes, 0, MagicBytes.Length);
                outputStream.Write(headerBytes, 0, headerBytes.Length);

                var byteBuffer = new byte[512 * 512];
                using (var sourceStream = File.OpenRead(sourceFilename))
                {
                    int rawDataLength = width * height * depth * 2;
                    while (sourceStream.Position < rawDataLength)
                    {
                        var read = sourceStream.Read(byteBuffer, 0, byteBuffer.Length);
                        outputStream.Write(byteBuffer, 0, read);
                    }
                }
            }

            return outputFilename;
        }

        public struct Surface
        {
            public Header Info;
            public Stream PixelDataStream;
        }

        public static Surface OpenRead(string filename)
        {
            var inputStream = File.OpenRead(filename);

            // Check magic header bytes.
            for (int i = 0; i < MagicBytes.Length; i++)
            {
                var magicByte = inputStream.ReadByte();
                if (MagicBytes[i] != magicByte)
                {
                    inputStream.Close();
                    throw new IOException("Unable to find DDS magic word in beginning of file!");
                }
            }

            var output = new Surface
            {
                Info = Utils.ReadStruct<Header>(inputStream),
                PixelDataStream = inputStream
            };

            if (output.Info.Format.FourCc != 0)
            {
                inputStream.Close();
                throw new IOException("DX10 Header is not supported!");
            }

            return output;
        }
    }
}
