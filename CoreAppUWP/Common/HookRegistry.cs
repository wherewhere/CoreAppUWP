using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;
using Detours = Microsoft.Detours.PInvoke;

namespace CoreAppUWP.Common
{
    public class HookRegistry : IDisposable
    {
        private unsafe delegate WIN32_ERROR RegOpenKeyEx(HKEY hKey, PCWSTR lpSubKey, uint ulOptions, REG_SAM_FLAGS samDesired, HKEY* phkResult);
        private delegate WIN32_ERROR RegCloseKey(HKEY hKey);
        private unsafe delegate WIN32_ERROR RegQueryValueEx(HKEY hKey, PCWSTR lpValueName, [Optional] uint* lpReserved, [Optional] REG_VALUE_TYPE* lpType, [Optional] byte* lpData, [Optional] uint* lpcbData);

        [ThreadStatic]
        private static HANDLE currentThread;
        [ThreadStatic]
        private static Dictionary<HKEY, bool> xamlKeyMap;
        [ThreadStatic]
        private static object locker;

        [ThreadStatic]
        private static unsafe FARPROC baseRegOpenKeyExW;
        [ThreadStatic]
        private static unsafe delegate*<HKEY, PCWSTR, uint, REG_SAM_FLAGS, HKEY*, WIN32_ERROR> overrideRegOpenKeyExW;

        [ThreadStatic]
        private static unsafe FARPROC baseRegCloseKey;
        [ThreadStatic]
        private static unsafe delegate*<HKEY, WIN32_ERROR> overrideRegCloseKey;

        [ThreadStatic]
        private static unsafe FARPROC baseRegQueryValueExW;
        [ThreadStatic]
        private static unsafe delegate*<HKEY, PCWSTR, uint*, REG_VALUE_TYPE*, byte*, uint*, WIN32_ERROR> overrideRegQueryValueExW;

        ~HookRegistry()
        {
            Dispose(disposing: true);
        }

        [ThreadStatic]
        private static bool isHooked;
        public bool IsHooked
        {
            get => isHooked;
            set => isHooked = value;
        }

        public unsafe void StartHook()
        {
            if (!IsHooked)
            {
                xamlKeyMap ??= [];
                locker ??= new object();

                currentThread = PInvoke.GetCurrentThread();

                _ = Detours.DetourTransactionBegin();
                _ = Detours.DetourUpdateThread(currentThread);

                using (FreeLibrarySafeHandle library = PInvoke.LoadLibrary("ADVAPI32.dll"))
                {
                    baseRegOpenKeyExW = PInvoke.GetProcAddress(library, "RegOpenKeyExW");
                    void* baseRegOpenKeyExWPointer = (void*)baseRegOpenKeyExW.Value;
                    overrideRegOpenKeyExW = &OverrideRegOpenKeyEx;
                    void* overrideRegOpenKeyExWPointer = overrideRegOpenKeyExW;
                    _ = Detours.DetourAttach(ref baseRegOpenKeyExWPointer, overrideRegOpenKeyExWPointer);

                    baseRegCloseKey = PInvoke.GetProcAddress(library, "RegCloseKey");
                    void* baseRegCloseKeyPointer = (void*)baseRegCloseKey.Value;
                    overrideRegCloseKey = &OverrideRegCloseKey;
                    void* overrideRegCloseKeyPointer = overrideRegCloseKey;
                    _ = Detours.DetourAttach(ref baseRegCloseKeyPointer, overrideRegCloseKeyPointer);

                    baseRegQueryValueExW = PInvoke.GetProcAddress(library, "RegQueryValueExW");
                    void* baseRegQueryValueExWPointer = (void*)baseRegQueryValueExW.Value;
                    overrideRegQueryValueExW = &OverrideRegQueryValueExW;
                    void* overrideRegQueryValueExWPointer = overrideRegQueryValueExW;
                    _ = Detours.DetourAttach(ref baseRegQueryValueExWPointer, overrideRegQueryValueExWPointer);
                }

                _ = Detours.DetourTransactionCommit();
                IsHooked = true;
            }
        }

        public unsafe void EndHook()
        {
            if (IsHooked)
            {
                _ = Detours.DetourTransactionBegin();
                _ = Detours.DetourUpdateThread(currentThread);

                void* baseRegOpenKeyExWPointer = (void*)baseRegOpenKeyExW.Value;
                void* overrideRegOpenKeyExWPointer = overrideRegOpenKeyExW;
                _ = Detours.DetourDetach(ref baseRegOpenKeyExWPointer, overrideRegOpenKeyExWPointer);
                baseRegOpenKeyExW = default;
                overrideRegOpenKeyExW = default;

                void* baseRegCloseKeyPointer = (void*)baseRegCloseKey.Value;
                void* overrideRegCloseKeyPointer = overrideRegCloseKey;
                _ = Detours.DetourDetach(ref baseRegCloseKeyPointer, overrideRegCloseKeyPointer);
                baseRegCloseKey = default;
                overrideRegCloseKey = default;

                void* baseRegQueryValueExWPointer = (void*)baseRegQueryValueExW.Value;
                void* overrideRegQueryValueExWPointer = overrideRegQueryValueExW;
                _ = Detours.DetourDetach(ref baseRegQueryValueExWPointer, overrideRegQueryValueExWPointer);
                baseRegQueryValueExW = default;
                overrideRegOpenKeyExW = default;

                _ = Detours.DetourTransactionCommit();

                locker = xamlKeyMap = null;
                IsHooked = false;
            }
        }

        private static unsafe WIN32_ERROR OverrideRegOpenKeyEx(HKEY hKey, PCWSTR lpSubKey, uint ulOptions, REG_SAM_FLAGS samDesired, HKEY* phkResult)
        {
            if (!isHooked) { return PInvoke.RegOpenKeyEx(hKey, lpSubKey, ulOptions, samDesired, phkResult); }
            RegOpenKeyEx RegOpenKeyEx = baseRegOpenKeyExW.CreateDelegate<RegOpenKeyEx>();
            WIN32_ERROR result = RegOpenKeyEx(hKey, lpSubKey, ulOptions, samDesired, phkResult);
            if (hKey == HKEY.HKEY_LOCAL_MACHINE && lpSubKey.ToString() == @"Software\Microsoft\WinUI\Xaml")
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

        private unsafe static WIN32_ERROR OverrideRegCloseKey(HKEY hKey)
        {
            if (!isHooked) { return PInvoke.RegCloseKey(hKey); }
            static WIN32_ERROR RegCloseKey(HKEY hKey) => baseRegCloseKey.CreateDelegate<RegCloseKey>()(hKey);
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

        private static unsafe WIN32_ERROR OverrideRegQueryValueExW(HKEY hKey, PCWSTR lpValueName, [Optional] uint* lpReserved, [Optional] REG_VALUE_TYPE* lpType, [Optional] byte* lpData, [Optional] uint* lpcbData)
        {
            if (!isHooked) { return PInvoke.RegQueryValueEx(hKey, lpValueName, lpReserved, lpType, lpData, lpcbData); }
            RegQueryValueEx RegQueryValueEx = baseRegQueryValueExW.CreateDelegate<RegQueryValueEx>();
            if (lpValueName.Value == default && lpValueName.ToString().Equals("EnableUWPWindow", StringComparison.OrdinalIgnoreCase))
            {
                lock (locker)
                {
                    if (xamlKeyMap.TryGetValue(hKey, out bool isRealKey))
                    {
                        WIN32_ERROR result;
                        if (isRealKey)
                        {
                            // real key
                            result = RegQueryValueEx(hKey, lpValueName, lpReserved, lpType, lpData, lpcbData);
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
            return RegQueryValueEx(hKey, lpValueName, lpReserved, lpType, lpData, lpcbData);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && IsHooked)
            {
                EndHook();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
