using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace InfluxShared.Helpers
{
    public static class FileStreamHelper
    {
        static FileStreamHelper()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                FastReadMethod = WinFastRead;
            }
            else
            {
                FastReadMethod = DotNetFastRead;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadFile(SafeFileHandle handle, IntPtr buffer, uint numBytesToRead, out uint numBytesRead, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetFilePointerEx(SafeFileHandle hFile, long liDistanceToMove, IntPtr lpNewFilePointer, uint dwMoveMethod);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(SafeFileHandle hFile, IntPtr aBuffer, UInt32 cbToWrite, ref UInt32 cbThatWereWritten, IntPtr pOverlapped);

        public static UInt64 FastRead<T>(this FileStream fs, T[] buffer, int arrindex, uint bytecount) =>
            (Environment.OSVersion.Platform == PlatformID.Win32NT) ?
            WinFastRead(fs, buffer, arrindex, bytecount) :
            DotNetFastRead(fs, buffer, arrindex, bytecount);

        private static UInt64 WinFastRead<T>(this FileStream fs, T[] buffer, int arrindex, uint bytecount)
        {
            SafeFileHandle nativeHandle = fs.SafeFileHandle; // clears Position property
            SetFilePointerEx(nativeHandle, fs.Position, IntPtr.Zero, 0);

            IntPtr bp = Marshal.UnsafeAddrOfPinnedArrayElement<T>(buffer, arrindex);
            ReadFile(nativeHandle, bp, bytecount, out uint BytesRead, IntPtr.Zero);
            return BytesRead;
        }

        private static UInt64 DotNetFastRead<T>(this FileStream fs, T[] buffer, int arrindex, uint bytecount)
        {
            IntPtr bp = Marshal.UnsafeAddrOfPinnedArrayElement<T>(buffer, arrindex);

            byte[] buff = new byte[bytecount];
            var BytesRead = fs.Read(buff, 0, buffer.Length);
            Marshal.Copy(buff, 0, bp, buffer.Length);
            return (ulong)BytesRead;
        }

        private delegate UInt64 OSFastRead(FileStream fs, IntPtr target, uint bytecount);
        private static OSFastRead FastReadMethod;
        public static UInt64 FastRead(this FileStream fs, IntPtr target, uint bytecount) => FastReadMethod(fs, target, bytecount);

        private static UInt64 WinFastRead(this FileStream fs, IntPtr target, uint bytecount)
        {
            SafeFileHandle nativeHandle = fs.SafeFileHandle; // clears Position property
            SetFilePointerEx(nativeHandle, fs.Position, IntPtr.Zero, 0);

            ReadFile(nativeHandle, target, bytecount, out uint BytesRead, IntPtr.Zero);
            return BytesRead;
        }

        private static UInt64 DotNetFastRead(this FileStream fs, IntPtr target, uint bytecount)
        {
            byte[] buffer = new byte[bytecount];
            var BytesRead = fs.Read(buffer, 0, buffer.Length);
            Marshal.Copy(buffer, 0, target, buffer.Length);
            return (ulong)BytesRead;
        }

        /*public unsafe static UInt64 FastRead(this FileStream fs, byte* target, uint bytecount)
        {
            SafeFileHandle nativeHandle = fs.SafeFileHandle; // clears Position property
            SetFilePointerEx(nativeHandle, fs.Position, IntPtr.Zero, 0);

            ReadFile(nativeHandle, target, bytecount, out uint BytesRead, IntPtr.Zero);
            return BytesRead;
        }*/

        public static void Write(this FileStream fs, byte[] buffer) => fs.Write(buffer, 0, buffer.Length);

        public static UInt64 FastWrite<T>(this FileStream fs, T[] buffer, int arrindex, uint bytecount) =>
            Environment.OSVersion.Platform == PlatformID.Win32NT ?
            WinFastWrite(fs, buffer, arrindex, bytecount) :
            DotNetFastWrite(fs, buffer, arrindex, bytecount);

        private static UInt64 WinFastWrite<T>(this FileStream fs, T[] buffer, int arrindex, uint bytecount)
        {
            SafeFileHandle nativeHandle = fs.SafeFileHandle; // clears Position property
            SetFilePointerEx(nativeHandle, fs.Position, IntPtr.Zero, 0);

            IntPtr bp = Marshal.UnsafeAddrOfPinnedArrayElement<T>(buffer, arrindex);
            uint written = 0;
            WriteFile(nativeHandle, bp, bytecount, ref written, IntPtr.Zero);
            return written;
        }

        private static UInt64 DotNetFastWrite<T>(this FileStream fs, T[] buffer, int arrindex, uint bytecount)
        {
            SafeFileHandle nativeHandle = fs.SafeFileHandle; // clears Position property
            SetFilePointerEx(nativeHandle, fs.Position, IntPtr.Zero, 0);

            IntPtr bp = Marshal.UnsafeAddrOfPinnedArrayElement<T>(buffer, arrindex);
            uint written = 0;
            WriteFile(nativeHandle, bp, bytecount, ref written, IntPtr.Zero);
            return written;
        }

        const int maxBufferSize = 5 * 0x100000; // 5 MB

        public static void Copy(this FileStream InputStream, FileStream OutputStream, Int64 InputOffset = 0, Int64 OutputOffset = -1, Int64 BufferSize = maxBufferSize)
        {
            byte[] buffer = new byte[BufferSize];
            int buffSize;

            if (InputOffset > -1)
                InputStream.Seek(InputOffset, SeekOrigin.Begin);
            if (OutputOffset > -1)
                OutputStream.Seek(OutputOffset, SeekOrigin.Begin);

            while ((buffSize = InputStream.Read(buffer, 0, (int)BufferSize)) > 0)
                OutputStream.Write(buffer, 0, buffSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// Input filename
        /// <param name="output"></param>
        /// Output filename
        /// <param name="inputOffset"></param>
        /// Offset of input from which it will start to copy
        /// <param name="outputOffset"></param>
        /// Offset to output before start to copy
        /// <br/>If value is -1 means append to end
        public static void Append(string input, string output, Int64 inputOffset = 0, Int64 outputOffset = -1)
        {
            using (FileStream fsInput = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream fsOutput = new FileStream(output, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                if (outputOffset == -1)
                    fsOutput.Seek(0, SeekOrigin.End);
                else
                    fsOutput.Seek(outputOffset, SeekOrigin.Begin);
                fsInput.Copy(fsOutput, inputOffset);
            }
        }

        /*public static IEnumerator<double> DoubleValues(this Stream stm)
        {
            stm.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[Marshal.SizeOf(typeof(double))];
            while (stm.Read(buffer, 0, buffer.Length) == buffer.Length)
                yield return BitConverter.ToDouble(buffer, 0);
        }*/
    }
}
