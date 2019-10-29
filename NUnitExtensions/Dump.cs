﻿namespace NUnit.Framework
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Native;

    /// <summary>
    /// Specify a dump type.
    /// </summary>
    public enum DumpType
    {
        /// <summary>
        /// On Windows, perform a dump that contains information about the system, process, modules (libraries), thread
        /// states, stack and optionally exception details.
        /// </summary>
        MiniDump,

        /// <summary>
        /// On Windows, perform a full memory dump of the process containing all information. This produces the largest
        /// memory dump, but allows debugging of objects on the heap, shared memory contents and global variables.
        /// </summary>
        FullHeap
    }

    /// <summary>
    /// Methods to allow core dumps for application debugging.
    /// </summary>
    public static class Dump
    {
        private static FileStream OpenFile(string path)
        {
            if (File.Exists(path)) {
                return File.Open(path, FileMode.Append);
            } else {
                return File.Create(path);
            }
        }

        /// <summary>
        /// Creates a crash dump using the DebugHelp library.
        /// </summary>
        /// <param name="path">The path to write the dump file.</param>
        /// <returns><see langword="true"/> if the mini-dump was successful.</returns>
        /// <remarks>
        /// This method is only supported on Windows. On other operating systems, the call is ignored and no crash dump
        /// is created. No exception is raised to keep the same behavior as if the program worked (i.e. a crash dump
        /// would be made for debugging purposes and then continue, it stands to reason on foreign operating systems we
        /// can't debug, so just let the program continue).
        /// </remarks>
        public static bool MiniDump(string path)
        {
            return MiniDump(path, DumpType.MiniDump);
        }

        /// <summary>
        /// Creates a crash dump using the DebugHelp library.
        /// </summary>
        /// <param name="path">The path to write the dump file.</param>
        /// <param name="dumpType">Select the type of crash dump to perform.</param>
        /// <returns><see langword="true" /> if the mini-dump was successful.</returns>
        /// <remarks>
        /// This method is only supported on Windows. On other operating systems, the call is ignored and no crash dump
        /// is created. No exception is raised to keep the same behavior as if the program worked (i.e. a crash dump
        /// would be made for debugging purposes and then continue, it stands to reason on foreign operating systems we
        /// can't debug, so just let the program continue).
        /// </remarks>
        public static bool MiniDump(string path, DumpType dumpType)
        {
            if (!Platform.IsWinNT()) return false;
            if (path == null) return false;

            // For some useful information about making dumps:
            //  http://www.debuginfo.com/articles/effminidumps.html
            //
            // There's a ClrDump too, but it's not visible by default
            //  http://www.debuginfo.com/tools/clrdump.html

            NativeMethods.MINIDUMP_TYPE dbgDumpType;
            switch (dumpType) {
            case DumpType.MiniDump:
                dbgDumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpNormal;
                break;
            case DumpType.FullHeap:
                dbgDumpType =
                    NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemory |
                    NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemoryInfo |
                    NativeMethods.MINIDUMP_TYPE.MiniDumpWithHandleData |
                    NativeMethods.MINIDUMP_TYPE.MiniDumpWithThreadInfo |
                    NativeMethods.MINIDUMP_TYPE.MiniDumpWithUnloadedModules;
                break;
            default:
                dbgDumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpNormal;
                break;
            }

            using (FileStream fsToDump = OpenFile(path)) {
                NativeMethods.MINIDUMP_EXCEPTION_INFORMATION miniDumpInfo =
                    new NativeMethods.MINIDUMP_EXCEPTION_INFORMATION {
                        ClientPointers = false,
                        ExceptionPointers = Marshal.GetExceptionPointers(),
                        ThreadId = SafeNativeMethods.GetCurrentThreadId()
                    };

                IntPtr mem = Marshal.AllocHGlobal(Marshal.SizeOf(miniDumpInfo));
                Marshal.StructureToPtr(miniDumpInfo, mem, false);

                bool result = UnsafeNativeMethods.MiniDumpWriteDump(
                    SafeNativeMethods.GetCurrentProcess(),
                    SafeNativeMethods.GetCurrentProcessId(),
                    fsToDump.SafeFileHandle,
                    dbgDumpType,
                    miniDumpInfo.ClientPointers ? mem : IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);

                Marshal.FreeHGlobal(mem);
                return result;
            }
        }
    }
}
