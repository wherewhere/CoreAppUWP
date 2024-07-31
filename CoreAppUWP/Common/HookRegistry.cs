using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;
using Detours = Microsoft.Detours.PInvoke;

namespace CoreAppUWP.Common
{
    public class HookRegistry : IDisposable
    {
        private bool disposed;
        private static int refCount;
        private static readonly Dictionary<HKEY, bool> xamlKeyMap = [];
        private static readonly object locker = new();

        private static unsafe delegate* unmanaged[Stdcall]<HKEY, PCWSTR, uint, REG_SAM_FLAGS, HKEY*, WIN32_ERROR> RegOpenKeyExW;
        private static unsafe delegate* unmanaged[Stdcall]<HKEY, WIN32_ERROR> RegCloseKey;
        private static unsafe delegate* unmanaged[Stdcall]<HKEY, PCWSTR, uint*, REG_VALUE_TYPE*, byte*, uint*, WIN32_ERROR> RegQueryValueExW;

        public HookRegistry()
        {
            refCount++;
            StartHook();
        }

        ~HookRegistry()
        {
            Dispose();
        }

        public static bool IsHooked { get; private set; }

        private unsafe static void StartHook()
        {
            if (!IsHooked)
            {
                using FreeLibrarySafeHandle library = PInvoke.GetModuleHandle("ADVAPI32.dll");
                if (!library.IsInvalid
                    && NativeLibrary.TryGetExport(library.DangerousGetHandle(), "RegOpenKeyExW", out nint regOpenKeyExW)
                    && NativeLibrary.TryGetExport(library.DangerousGetHandle(), nameof(PInvoke.RegCloseKey), out nint regCloseKey)
                    && NativeLibrary.TryGetExport(library.DangerousGetHandle(), "RegQueryValueExW", out nint regQueryValueExW))
                {
                    void* regOpenKeyExWPtr = (void*)regOpenKeyExW;
                    void* regCloseKeyPtr = (void*)regCloseKey;
                    void* regQueryValueExWPtr = (void*)regQueryValueExW;

                    delegate* unmanaged[Stdcall]<HKEY, PCWSTR, uint, REG_SAM_FLAGS, HKEY*, WIN32_ERROR> overrideRegOpenKeyExW = &OverrideRegOpenKeyExW;
                    delegate* unmanaged[Stdcall]<HKEY, WIN32_ERROR> overrideRegCloseKey = &OverrideRegCloseKey;
                    delegate* unmanaged[Stdcall]<HKEY, PCWSTR, uint*, REG_VALUE_TYPE*, byte*, uint*, WIN32_ERROR> overrideRegQueryValueExW = &OverrideRegQueryValueExW;

                    _ = Detours.DetourRestoreAfterWith();

                    _ = Detours.DetourTransactionBegin();
                    _ = Detours.DetourUpdateThread(PInvoke.GetCurrentThread());
                    _ = Detours.DetourAttach(ref regOpenKeyExWPtr, overrideRegOpenKeyExW);
                    _ = Detours.DetourAttach(ref regCloseKeyPtr, overrideRegCloseKey);
                    _ = Detours.DetourAttach(ref regQueryValueExWPtr, overrideRegQueryValueExW);
                    _ = Detours.DetourTransactionCommit();

                    RegOpenKeyExW = (delegate* unmanaged[Stdcall]<HKEY, PCWSTR, uint, REG_SAM_FLAGS, HKEY*, WIN32_ERROR>)regOpenKeyExWPtr;
                    RegCloseKey = (delegate* unmanaged[Stdcall]<HKEY, WIN32_ERROR>)regCloseKeyPtr;
                    RegQueryValueExW = (delegate* unmanaged[Stdcall]<HKEY, PCWSTR, uint*, REG_VALUE_TYPE*, byte*, uint*, WIN32_ERROR>)regQueryValueExWPtr;
                    
                    IsHooked = true;
                }
            }
        }

        public unsafe static void EndHook()
        {
            if (--refCount == 0 && IsHooked)
            {
                void* regOpenKeyExWPtr = RegOpenKeyExW;
                void* regCloseKeyPtr = RegCloseKey;
                void* regQueryValueExWPtr = RegQueryValueExW;

                delegate* unmanaged[Stdcall]<HKEY, PCWSTR, uint, REG_SAM_FLAGS, HKEY*, WIN32_ERROR> overrideRegOpenKeyExW = &OverrideRegOpenKeyExW;
                delegate* unmanaged[Stdcall]<HKEY, WIN32_ERROR> overrideRegCloseKey = &OverrideRegCloseKey;
                delegate* unmanaged[Stdcall]<HKEY, PCWSTR, uint*, REG_VALUE_TYPE*, byte*, uint*, WIN32_ERROR> overrideRegQueryValueExW = &OverrideRegQueryValueExW;

                _ = Detours.DetourTransactionBegin();
                _ = Detours.DetourUpdateThread(PInvoke.GetCurrentThread());
                _ = Detours.DetourDetach(&regOpenKeyExWPtr, overrideRegOpenKeyExW);
                _ = Detours.DetourDetach(&regCloseKeyPtr, overrideRegCloseKey);
                _ = Detours.DetourDetach(&regQueryValueExWPtr, overrideRegQueryValueExW);
                _ = Detours.DetourTransactionCommit();

                RegOpenKeyExW = null;
                RegCloseKey = null;
                RegQueryValueExW = null;

                IsHooked = false;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static unsafe WIN32_ERROR OverrideRegOpenKeyExW(HKEY hKey, PCWSTR lpSubKey, uint ulOptions, REG_SAM_FLAGS samDesired, HKEY* phkResult)
        {
            WIN32_ERROR result = RegOpenKeyExW(hKey, lpSubKey, ulOptions, samDesired, phkResult);
            if (hKey == HKEY.HKEY_LOCAL_MACHINE && lpSubKey.ToString().Equals(@"Software\Microsoft\WinUI\Xaml", StringComparison.OrdinalIgnoreCase))
            {
                if (result == WIN32_ERROR.ERROR_FILE_NOT_FOUND)
                {
                    HKEY key = new(HANDLE.INVALID_HANDLE_VALUE);
                    xamlKeyMap[key] = false;
                    *phkResult = key;
                    result = WIN32_ERROR.ERROR_SUCCESS;
                }
                else if (result == WIN32_ERROR.ERROR_SUCCESS)
                {
                    xamlKeyMap[*phkResult] = true;
                }
            }
            return result;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private unsafe static WIN32_ERROR OverrideRegCloseKey(HKEY hKey)
        {
            bool isXamlKey;
            lock (locker)
            {
                if (isXamlKey = xamlKeyMap.TryGetValue(hKey, out bool isRealKey))
                {
                    xamlKeyMap.Remove(hKey);
                }
                return isXamlKey
                    ? isRealKey
                        ? RegCloseKey(hKey) // real key
                        : WIN32_ERROR.ERROR_SUCCESS // simulated key
                    : hKey == HANDLE.INVALID_HANDLE_VALUE
                        ? WIN32_ERROR.ERROR_INVALID_HANDLE
                        : RegCloseKey(hKey);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static unsafe WIN32_ERROR OverrideRegQueryValueExW(HKEY hKey, PCWSTR lpValueName, [Optional] uint* lpReserved, [Optional] REG_VALUE_TYPE* lpType, [Optional] byte* lpData, [Optional] uint* lpcbData)
        {
            if (lpValueName.Value != default && lpValueName.ToString().Equals("EnableUWPWindow", StringComparison.OrdinalIgnoreCase))
            {
                lock (locker)
                {
                    if (xamlKeyMap.TryGetValue(hKey, out bool isRealKey))
                    {
                        WIN32_ERROR result;
                        if (isRealKey)
                        {
                            // real key
                            result = RegQueryValueExW(hKey, lpValueName, lpReserved, lpType, lpData, lpcbData);
                            if (result == WIN32_ERROR.ERROR_SUCCESS && lpData != default)
                            {
                                *lpData = 1;
                            }
                            else if (result == WIN32_ERROR.ERROR_FILE_NOT_FOUND)
                            {
                                if (lpData == default && lpcbData != default)
                                {
                                    *lpcbData = sizeof(int);
                                    result = WIN32_ERROR.ERROR_SUCCESS;
                                }
                                else if (lpData != default && lpcbData != default)
                                {
                                    if (*lpcbData >= sizeof(int))
                                    {
                                        *lpData = 1;
                                        result = WIN32_ERROR.ERROR_SUCCESS;
                                    }
                                    else
                                    {
                                        result = WIN32_ERROR.ERROR_MORE_DATA;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // simulated key
                            result = WIN32_ERROR.ERROR_FILE_NOT_FOUND;
                            if (lpData == default && lpcbData != default)
                            {
                                *lpcbData = sizeof(int);
                                result = WIN32_ERROR.ERROR_SUCCESS;
                            }
                            else if (lpData != default && lpcbData != default)
                            {
                                if (*lpcbData >= sizeof(int))
                                {
                                    *lpData = 1;
                                    result = WIN32_ERROR.ERROR_SUCCESS;
                                }
                                else
                                {
                                    result = WIN32_ERROR.ERROR_MORE_DATA;
                                }
                            }
                        }
                        return result;
                    }
                }
            }
            return RegQueryValueExW(hKey, lpValueName, lpReserved, lpType, lpData, lpcbData);
        }

        public void Dispose()
        {
            if (!disposed && IsHooked)
            {
                EndHook();
            }
            GC.SuppressFinalize(this);
            disposed = true;
        }
    }
}
