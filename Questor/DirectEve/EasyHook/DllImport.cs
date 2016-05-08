﻿// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace EasyHook
{
    using System;
    using System.GACManagedAccess;
    using System.Runtime.InteropServices;

#pragma warning disable 1591

    internal static class NativeAPI_x86
    {
        private const String DllName = "EasyHook32.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern String RtlGetLastErrorString();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 RtlGetLastError();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void LhUninstallAllHooks();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhInstallHook(
            IntPtr InEntryPoint,
            IntPtr InHookProc,
            IntPtr InCallback,
            IntPtr OutHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhUninstallHook(IntPtr RefHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhWaitForPendingRemovals();


        /*
            Setup the ACLs after hook installation. Please note that every
            hook starts suspended. You will have to set a proper ACL to
            make it active!
        */

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhSetInclusiveACL(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int32[] InThreadIdList,
            Int32 InThreadCount,
            IntPtr InHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhSetExclusiveACL(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int32[] InThreadIdList,
            Int32 InThreadCount,
            IntPtr InHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhSetGlobalInclusiveACL(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int32[] InThreadIdList,
            Int32 InThreadCount);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhSetGlobalExclusiveACL(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int32[] InThreadIdList,
            Int32 InThreadCount);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhIsThreadIntercepted(
            IntPtr InHandle,
            Int32 InThreadID,
            out Boolean OutResult);

        /*
            The following barrier methods are meant to be used in hook handlers only!

            They will all fail with STATUS_NOT_SUPPORTED if called outside a
            valid hook handler...
        */

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierGetCallback(out IntPtr OutValue);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierGetReturnAddress(out IntPtr OutValue);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierGetAddressOfReturnAddress(out IntPtr OutValue);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierBeginStackTrace(out IntPtr OutBackup);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierEndStackTrace(IntPtr OutBackup);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierGetCallingModule(out IntPtr OutValue);

        /*
            Debug helper API.
        */

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 DbgAttachDebugger();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 DbgGetThreadIdByHandle(
            IntPtr InThreadHandle,
            out Int32 OutThreadId);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 DbgGetProcessIdByHandle(
            IntPtr InProcessHandle,
            out Int32 OutProcessId);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 DbgHandleToObjectName(
            IntPtr InNamedHandle,
            IntPtr OutNameBuffer,
            Int32 InBufferSize,
            out Int32 OutRequiredSize);


        /*
            Injection support API.
        */

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern Int32 RhInjectLibrary(
            Int32 InTargetPID,
            Int32 InWakeUpTID,
            Int32 InInjectionOptions,
            String InLibraryPath_x86,
            String InLibraryPath_x64,
            IntPtr InPassThruBuffer,
            Int32 InPassThruSize);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 RhIsX64Process(
            Int32 InProcessId,
            out Boolean OutResult);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Boolean RhIsAdministrator();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 RhGetProcessToken(Int32 InProcessId, out IntPtr OutToken);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern Int32 RtlInstallService(
            String InServiceName,
            String InExePath,
            String InChannelName);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 RhWakeUpProcess();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern Int32 RtlCreateSuspendedProcess(
            String InEXEPath,
            String InCommandLine,
            Int32 InProcessCreationFlags,
            out Int32 OutProcessId,
            out Int32 OutThreadId);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern Int32 RhInstallDriver(
            String InDriverPath,
            String InDriverName);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 RhInstallSupportDriver();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Boolean RhIsX64System();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GacCreateContext();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void GacReleaseContext(ref IntPtr RefContext);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern bool GacInstallAssembly(
            IntPtr InContext,
            String InAssemblyPath,
            String InDescription,
            String InUniqueID);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern bool GacUninstallAssembly(
            IntPtr InContext,
            String InAssemblyName,
            String InDescription,
            String InUniqueID);
    }

    internal static class NativeAPI_x64
    {
        private const String DllName = "EasyHook64.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern String RtlGetLastErrorString();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 RtlGetLastError();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void LhUninstallAllHooks();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhInstallHook(
            IntPtr InEntryPoint,
            IntPtr InHookProc,
            IntPtr InCallback,
            IntPtr OutHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhUninstallHook(IntPtr RefHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhWaitForPendingRemovals();


        /*
            Setup the ACLs after hook installation. Please note that every
            hook starts suspended. You will have to set a proper ACL to
            make it active!
        */

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhSetInclusiveACL(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int32[] InThreadIdList,
            Int32 InThreadCount,
            IntPtr InHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhSetExclusiveACL(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int32[] InThreadIdList,
            Int32 InThreadCount,
            IntPtr InHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhSetGlobalInclusiveACL(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int32[] InThreadIdList,
            Int32 InThreadCount);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhSetGlobalExclusiveACL(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int32[] InThreadIdList,
            Int32 InThreadCount);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhIsThreadIntercepted(
            IntPtr InHandle,
            Int32 InThreadID,
            out Boolean OutResult);

        /*
            The following barrier methods are meant to be used in hook handlers only!

            They will all fail with STATUS_NOT_SUPPORTED if called outside a
            valid hook handler...
        */

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierGetCallback(out IntPtr OutValue);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierGetReturnAddress(out IntPtr OutValue);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierGetAddressOfReturnAddress(out IntPtr OutValue);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierBeginStackTrace(out IntPtr OutBackup);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierEndStackTrace(IntPtr OutBackup);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 LhBarrierGetCallingModule(out IntPtr OutValue);

        /*
            Debug helper API.
        */

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 DbgAttachDebugger();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 DbgGetThreadIdByHandle(
            IntPtr InThreadHandle,
            out Int32 OutThreadId);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 DbgGetProcessIdByHandle(
            IntPtr InProcessHandle,
            out Int32 OutProcessId);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 DbgHandleToObjectName(
            IntPtr InNamedHandle,
            IntPtr OutNameBuffer,
            Int32 InBufferSize,
            out Int32 OutRequiredSize);


        /*
            Injection support API.
        */

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern Int32 RhInjectLibrary(
            Int32 InTargetPID,
            Int32 InWakeUpTID,
            Int32 InInjectionOptions,
            String InLibraryPath_x86,
            String InLibraryPath_x64,
            IntPtr InPassThruBuffer,
            Int32 InPassThruSize);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 RhIsX64Process(
            Int32 InProcessId,
            out Boolean OutResult);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Boolean RhIsAdministrator();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 RhGetProcessToken(Int32 InProcessId, out IntPtr OutToken);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern Int32 RtlInstallService(
            String InServiceName,
            String InExePath,
            String InChannelName);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern Int32 RtlCreateSuspendedProcess(
            String InEXEPath,
            String InCommandLine,
            Int32 InProcessCreationFlags,
            out Int32 OutProcessId,
            out Int32 OutThreadId);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 RhWakeUpProcess();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern Int32 RhInstallDriver(
            String InDriverPath,
            String InDriverName);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 RhInstallSupportDriver();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern Boolean RhIsX64System();


        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GacCreateContext();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void GacReleaseContext(ref IntPtr RefContext);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern bool GacInstallAssembly(
            IntPtr InContext,
            String InAssemblyPath,
            String InDescription,
            String InUniqueID);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern bool GacUninstallAssembly(
            IntPtr InContext,
            String InAssemblyName,
            String InDescription,
            String InUniqueID);
    }

    public static class NativeAPI
    {
        public const Int32 MAX_HOOK_COUNT = 1024;
        public const Int32 MAX_ACE_COUNT = 128;
        public static readonly Boolean Is64Bit = IntPtr.Size == 8;

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern void CloseHandle(IntPtr InHandle);

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentProcessId();

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress(IntPtr InModule, String InProcName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibrary(String InPath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(String InPath);

        [DllImport("kernel32.dll")]
        public static extern Int16 RtlCaptureStackBackTrace(
            Int32 InFramesToSkip,
            Int32 InFramesToCapture,
            IntPtr OutBackTrace,
            IntPtr OutBackTraceHash);

        public const Int32 STATUS_SUCCESS = unchecked((Int32) 0);
        public const Int32 STATUS_INVALID_PARAMETER = unchecked((Int32) 0xC000000DL);
        public const Int32 STATUS_INVALID_PARAMETER_1 = unchecked((Int32) 0xC00000EFL);
        public const Int32 STATUS_INVALID_PARAMETER_2 = unchecked((Int32) 0xC00000F0L);
        public const Int32 STATUS_INVALID_PARAMETER_3 = unchecked((Int32) 0xC00000F1L);
        public const Int32 STATUS_INVALID_PARAMETER_4 = unchecked((Int32) 0xC00000F2L);
        public const Int32 STATUS_INVALID_PARAMETER_5 = unchecked((Int32) 0xC00000F3L);
        public const Int32 STATUS_NOT_SUPPORTED = unchecked((Int32) 0xC00000BBL);
        public const Int32 STATUS_INTERNAL_ERROR = unchecked((Int32) 0xC00000E5L);
        public const Int32 STATUS_INSUFFICIENT_RESOURCES = unchecked((Int32) 0xC000009AL);
        public const Int32 STATUS_BUFFER_TOO_SMALL = unchecked((Int32) 0xC0000023L);
        public const Int32 STATUS_NO_MEMORY = unchecked((Int32) 0xC0000017L);
        public const Int32 STATUS_WOW_ASSERTION = unchecked((Int32) 0xC0009898L);
        public const Int32 STATUS_ACCESS_DENIED = unchecked((Int32) 0xC0000022L);

        private static String ComposeString()
        {
            return String.Format("{0} (Code: {1})", RtlGetLastErrorString(), RtlGetLastError());
        }

        internal static void Force(Int32 InErrorCode)
        {
            switch (InErrorCode)
            {
                case STATUS_SUCCESS:
                    return;
                case STATUS_INVALID_PARAMETER:
                    throw new ArgumentException("STATUS_INVALID_PARAMETER: " + ComposeString());
                case STATUS_INVALID_PARAMETER_1:
                    throw new ArgumentException("STATUS_INVALID_PARAMETER_1: " + ComposeString());
                case STATUS_INVALID_PARAMETER_2:
                    throw new ArgumentException("STATUS_INVALID_PARAMETER_2: " + ComposeString());
                case STATUS_INVALID_PARAMETER_3:
                    throw new ArgumentException("STATUS_INVALID_PARAMETER_3: " + ComposeString());
                case STATUS_INVALID_PARAMETER_4:
                    throw new ArgumentException("STATUS_INVALID_PARAMETER_4: " + ComposeString());
                case STATUS_INVALID_PARAMETER_5:
                    throw new ArgumentException("STATUS_INVALID_PARAMETER_5: " + ComposeString());
                case STATUS_NOT_SUPPORTED:
                    throw new NotSupportedException("STATUS_NOT_SUPPORTED: " + ComposeString());
                case STATUS_INTERNAL_ERROR:
                    throw new ApplicationException("STATUS_INTERNAL_ERROR: " + ComposeString());
                case STATUS_INSUFFICIENT_RESOURCES:
                    throw new InsufficientMemoryException("STATUS_INSUFFICIENT_RESOURCES: " + ComposeString());
                case STATUS_BUFFER_TOO_SMALL:
                    throw new ArgumentException("STATUS_BUFFER_TOO_SMALL: " + ComposeString());
                case STATUS_NO_MEMORY:
                    throw new OutOfMemoryException("STATUS_NO_MEMORY: " + ComposeString());
                case STATUS_WOW_ASSERTION:
                    throw new OutOfMemoryException("STATUS_WOW_ASSERTION: " + ComposeString());
                case STATUS_ACCESS_DENIED:
                    throw new AccessViolationException("STATUS_ACCESS_DENIED: " + ComposeString());

                default:
                    throw new ApplicationException("Unknown error code (" + InErrorCode + "): " + ComposeString());
            }
        }

        public static Int32 RtlGetLastError()
        {
            if (Is64Bit) return NativeAPI_x64.RtlGetLastError();
            else return NativeAPI_x86.RtlGetLastError();
        }

        public static String RtlGetLastErrorString()
        {
            if (Is64Bit) return NativeAPI_x64.RtlGetLastErrorString();
            else return NativeAPI_x86.RtlGetLastErrorString();
        }

        public static void LhUninstallAllHooks()
        {
            if (Is64Bit) NativeAPI_x64.LhUninstallAllHooks();
            else NativeAPI_x86.LhUninstallAllHooks();
        }

        public static void LhInstallHook(
            IntPtr InEntryPoint,
            IntPtr InHookProc,
            IntPtr InCallback,
            IntPtr OutHandle)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhInstallHook(InEntryPoint, InHookProc, InCallback, OutHandle));
            else Force(NativeAPI_x86.LhInstallHook(InEntryPoint, InHookProc, InCallback, OutHandle));
        }

        public static void LhUninstallHook(IntPtr RefHandle)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhUninstallHook(RefHandle));
            else Force(NativeAPI_x86.LhUninstallHook(RefHandle));
        }

        public static void LhWaitForPendingRemovals()
        {
            if (Is64Bit) Force(NativeAPI_x64.LhWaitForPendingRemovals());
            else Force(NativeAPI_x86.LhWaitForPendingRemovals());
        }

        public static void LhIsThreadIntercepted(
            IntPtr InHandle,
            Int32 InThreadID,
            out Boolean OutResult)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhIsThreadIntercepted(InHandle, InThreadID, out OutResult));
            else Force(NativeAPI_x86.LhIsThreadIntercepted(InHandle, InThreadID, out OutResult));
        }

        public static void LhSetInclusiveACL(
            Int32[] InThreadIdList,
            Int32 InThreadCount,
            IntPtr InHandle)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhSetInclusiveACL(InThreadIdList, InThreadCount, InHandle));
            else Force(NativeAPI_x86.LhSetInclusiveACL(InThreadIdList, InThreadCount, InHandle));
        }

        public static void LhSetExclusiveACL(
            Int32[] InThreadIdList,
            Int32 InThreadCount,
            IntPtr InHandle)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhSetExclusiveACL(InThreadIdList, InThreadCount, InHandle));
            else Force(NativeAPI_x86.LhSetExclusiveACL(InThreadIdList, InThreadCount, InHandle));
        }

        public static void LhSetGlobalInclusiveACL(
            Int32[] InThreadIdList,
            Int32 InThreadCount)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhSetGlobalInclusiveACL(InThreadIdList, InThreadCount));
            else Force(NativeAPI_x86.LhSetGlobalInclusiveACL(InThreadIdList, InThreadCount));
        }

        public static void LhSetGlobalExclusiveACL(
            Int32[] InThreadIdList,
            Int32 InThreadCount)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhSetGlobalExclusiveACL(InThreadIdList, InThreadCount));
            else Force(NativeAPI_x86.LhSetGlobalExclusiveACL(InThreadIdList, InThreadCount));
        }

        public static void LhBarrierGetCallingModule(out IntPtr OutValue)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhBarrierGetCallingModule(out OutValue));
            else Force(NativeAPI_x86.LhBarrierGetCallingModule(out OutValue));
        }

        public static void LhBarrierGetCallback(out IntPtr OutValue)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhBarrierGetCallback(out OutValue));
            else Force(NativeAPI_x86.LhBarrierGetCallback(out OutValue));
        }

        public static void LhBarrierGetReturnAddress(out IntPtr OutValue)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhBarrierGetReturnAddress(out OutValue));
            else Force(NativeAPI_x86.LhBarrierGetReturnAddress(out OutValue));
        }

        public static void LhBarrierGetAddressOfReturnAddress(out IntPtr OutValue)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhBarrierGetAddressOfReturnAddress(out OutValue));
            else Force(NativeAPI_x86.LhBarrierGetAddressOfReturnAddress(out OutValue));
        }

        public static void LhBarrierBeginStackTrace(out IntPtr OutBackup)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhBarrierBeginStackTrace(out OutBackup));
            else Force(NativeAPI_x86.LhBarrierBeginStackTrace(out OutBackup));
        }

        public static void LhBarrierEndStackTrace(IntPtr OutBackup)
        {
            if (Is64Bit) Force(NativeAPI_x64.LhBarrierEndStackTrace(OutBackup));
            else Force(NativeAPI_x86.LhBarrierEndStackTrace(OutBackup));
        }

        public static void DbgAttachDebugger()
        {
            if (Is64Bit) Force(NativeAPI_x64.DbgAttachDebugger());
            else Force(NativeAPI_x86.DbgAttachDebugger());
        }

        public static void DbgGetThreadIdByHandle(
            IntPtr InThreadHandle,
            out Int32 OutThreadId)
        {
            if (Is64Bit) Force(NativeAPI_x64.DbgGetThreadIdByHandle(InThreadHandle, out OutThreadId));
            else Force(NativeAPI_x86.DbgGetThreadIdByHandle(InThreadHandle, out OutThreadId));
        }

        public static void DbgGetProcessIdByHandle(
            IntPtr InProcessHandle,
            out Int32 OutProcessId)
        {
            if (Is64Bit) Force(NativeAPI_x64.DbgGetProcessIdByHandle(InProcessHandle, out OutProcessId));
            else Force(NativeAPI_x86.DbgGetProcessIdByHandle(InProcessHandle, out OutProcessId));
        }

        public static void DbgHandleToObjectName(
            IntPtr InNamedHandle,
            IntPtr OutNameBuffer,
            Int32 InBufferSize,
            out Int32 OutRequiredSize)
        {
            if (Is64Bit) Force(NativeAPI_x64.DbgHandleToObjectName(InNamedHandle, OutNameBuffer, InBufferSize, out OutRequiredSize));
            else Force(NativeAPI_x86.DbgHandleToObjectName(InNamedHandle, OutNameBuffer, InBufferSize, out OutRequiredSize));
        }

        public static Int32 EASYHOOK_INJECT_DEFAULT = 0x00000000;
        public static Int32 EASYHOOK_INJECT_MANAGED = 0x00000001;

        public static Int32 RhInjectLibraryEx(
            Int32 InTargetPID,
            Int32 InWakeUpTID,
            Int32 InInjectionOptions,
            String InLibraryPath_x86,
            String InLibraryPath_x64,
            IntPtr InPassThruBuffer,
            Int32 InPassThruSize)
        {
            if (Is64Bit)
                return NativeAPI_x64.RhInjectLibrary(InTargetPID, InWakeUpTID, InInjectionOptions,
                    InLibraryPath_x86, InLibraryPath_x64, InPassThruBuffer, InPassThruSize);
            else
                return NativeAPI_x86.RhInjectLibrary(InTargetPID, InWakeUpTID, InInjectionOptions,
                    InLibraryPath_x86, InLibraryPath_x64, InPassThruBuffer, InPassThruSize);
        }

        public static void RhInjectLibrary(
            Int32 InTargetPID,
            Int32 InWakeUpTID,
            Int32 InInjectionOptions,
            String InLibraryPath_x86,
            String InLibraryPath_x64,
            IntPtr InPassThruBuffer,
            Int32 InPassThruSize)
        {
            if (Is64Bit)
                Force(NativeAPI_x64.RhInjectLibrary(InTargetPID, InWakeUpTID, InInjectionOptions,
                    InLibraryPath_x86, InLibraryPath_x64, InPassThruBuffer, InPassThruSize));
            else
                Force(NativeAPI_x86.RhInjectLibrary(InTargetPID, InWakeUpTID, InInjectionOptions,
                    InLibraryPath_x86, InLibraryPath_x64, InPassThruBuffer, InPassThruSize));
        }

        public static void RtlCreateSuspendedProcess(
            String InEXEPath,
            String InCommandLine,
            Int32 InProcessCreationFlags,
            out Int32 OutProcessId,
            out Int32 OutThreadId)
        {
            if (Is64Bit)
                Force(NativeAPI_x64.RtlCreateSuspendedProcess(InEXEPath, InCommandLine, InProcessCreationFlags,
                    out OutProcessId, out OutThreadId));
            else
                Force(NativeAPI_x86.RtlCreateSuspendedProcess(InEXEPath, InCommandLine, InProcessCreationFlags,
                    out OutProcessId, out OutThreadId));
        }

        public static void RhIsX64Process(
            Int32 InProcessId,
            out Boolean OutResult)
        {
            if (Is64Bit) Force(NativeAPI_x64.RhIsX64Process(InProcessId, out OutResult));
            else Force(NativeAPI_x86.RhIsX64Process(InProcessId, out OutResult));
        }

        public static Boolean RhIsAdministrator()
        {
            if (Is64Bit) return NativeAPI_x64.RhIsAdministrator();
            else return NativeAPI_x86.RhIsAdministrator();
        }

        public static void RhGetProcessToken(Int32 InProcessId, out IntPtr OutToken)
        {
            if (Is64Bit) Force(NativeAPI_x64.RhGetProcessToken(InProcessId, out OutToken));
            else Force(NativeAPI_x86.RhGetProcessToken(InProcessId, out OutToken));
        }

        public static void RhWakeUpProcess()
        {
            if (Is64Bit) Force(NativeAPI_x64.RhWakeUpProcess());
            else Force(NativeAPI_x86.RhWakeUpProcess());
        }

        public static void RtlInstallService(
            String InServiceName,
            String InExePath,
            String InChannelName)
        {
            if (Is64Bit) Force(NativeAPI_x64.RtlInstallService(InServiceName, InExePath, InChannelName));
            else Force(NativeAPI_x86.RtlInstallService(InServiceName, InExePath, InChannelName));
        }

        public static void RhInstallDriver(
            String InDriverPath,
            String InDriverName)
        {
            if (Is64Bit) Force(NativeAPI_x64.RhInstallDriver(InDriverPath, InDriverName));
            else Force(NativeAPI_x86.RhInstallDriver(InDriverPath, InDriverName));
        }

        public static void RhInstallSupportDriver()
        {
            if (Is64Bit) Force(NativeAPI_x64.RhInstallSupportDriver());
            else Force(NativeAPI_x86.RhInstallSupportDriver());
        }

        public static Boolean RhIsX64System()
        {
            if (Is64Bit) return NativeAPI_x64.RhIsX64System();
            else return NativeAPI_x86.RhIsX64System();
        }

        public static void GacInstallAssemblies(
            String[] InAssemblyPaths,
            String InDescription,
            String InUniqueID)
        {
            try
            {
                AssemblyCache.InstallAssemblies(
                    InAssemblyPaths,
                    new InstallReference(InstallReferenceGuid.OpaqueGuid, InUniqueID, InDescription),
                    AssemblyCommitFlags.Force);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to install assemblies to GAC, see inner exception for details", e);
            }
        }

        public static void GacUninstallAssemblies(
            String[] InAssemblyNames,
            String InDescription,
            String InUniqueID)
        {
            try
            {
                AssemblyCacheUninstallDisposition[] results;
                AssemblyCache.UninstallAssemblies(
                    InAssemblyNames,
                    new InstallReference(InstallReferenceGuid.OpaqueGuid, InUniqueID, InDescription),
                    out results);

                for (var i = 0; i < InAssemblyNames.Length; i++)
                    Config.PrintComment("GacUninstallAssembly: Assembly {0}, uninstall result {1}", InAssemblyNames[i], results[i]);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to uninstall assemblies from GAC, see inner exception for details", e);
            }
        }
    }
}