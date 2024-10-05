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
    /// <summary>
    /// Represents a hook for getting the value of the <c>HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WinUI\Xaml\EnableUWPWindow</c> registry key always returning <see langword="00000001"/>.
    /// </summary>
    public partial class HookRegistry : IDisposable
    {
        /// <summary>
        /// The value that indicates whether the class has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The reference count for the hook.
        /// </summary>
        private static int refCount;

        /// <summary>
        /// The dictionary that maps the <see cref="HKEY"/> to a value that indicates whether the key is a real key.
        /// </summary>
        private static readonly Dictionary<HKEY, bool> xamlKeyMap = [];

        /// <summary>
        /// The object used to synchronize access to the <see cref="xamlKeyMap"/> dictionary.
        /// </summary>
        private static readonly object locker = new();

        /// <remarks>The original <see cref="PInvoke.RegOpenKeyEx(HKEY, PCWSTR, uint, REG_SAM_FLAGS, HKEY*)"/> function.</remarks>
        /// <inheritdoc cref="PInvoke.RegOpenKeyEx(HKEY, PCWSTR, uint, REG_SAM_FLAGS, HKEY*)"/>
        private static unsafe delegate* unmanaged[Stdcall]<HKEY, PCWSTR, uint, REG_SAM_FLAGS, HKEY*, WIN32_ERROR> RegOpenKeyExW;

        /// <remarks>The original <see cref="PInvoke.RegCloseKey(HKEY)"/> function.</remarks>
        /// <inheritdoc cref="PInvoke.RegCloseKey(HKEY)"/>
        private static unsafe delegate* unmanaged[Stdcall]<HKEY, WIN32_ERROR> RegCloseKey;

        /// <remarks>The original <see cref="PInvoke.RegQueryValueEx(HKEY, PCWSTR, uint*, REG_VALUE_TYPE*, byte*, uint*)"/> function.</remarks>
        /// <inheritdoc cref="PInvoke.RegQueryValueEx(HKEY, PCWSTR, uint*, REG_VALUE_TYPE*, byte*, uint*)"/>
        private static unsafe delegate* unmanaged[Stdcall]<HKEY, PCWSTR, uint*, REG_VALUE_TYPE*, byte*, uint*, WIN32_ERROR> RegQueryValueExW;

        /// <summary>
        /// Initializes a new instance of the <see cref="HookRegistry"/> class.
        /// </summary>
        public HookRegistry()
        {
            refCount++;
            StartHook();
        }

        /// <summary>
        /// Finalizes this instance of the <see cref="HookRegistry"/> class.
        /// </summary>
        ~HookRegistry()
        {
            Dispose();
        }

        /// <summary>
        /// Gets the value that indicates whether the hook is active.
        /// </summary>
        public static bool IsHooked { get; private set; }

        /// <summary>
        /// Starts the hook for the <see cref="PInvoke.AppPolicyGetWindowingModel(HANDLE, AppPolicyWindowingModel*)"/> function.
        /// </summary>
        private static unsafe void StartHook()
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

        /// <summary>
        /// Ends the hook for the <see cref="PInvoke.AppPolicyGetWindowingModel(HANDLE, AppPolicyWindowingModel*)"/> function.
        /// </summary>
        public static unsafe void EndHook()
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

        /// <remarks>The overridden <see cref="PInvoke.RegOpenKeyEx(HKEY, PCWSTR, uint, REG_SAM_FLAGS, HKEY*)"/> function.</remarks>
        /// <inheritdoc cref="PInvoke.RegOpenKeyEx(HKEY, PCWSTR, uint, REG_SAM_FLAGS, HKEY*)"/>
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

        /// <remarks>The overridden <see cref="PInvoke.RegCloseKey(HKEY)"/> function.</remarks>
        /// <inheritdoc cref="PInvoke.RegCloseKey(HKEY)"/>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static unsafe WIN32_ERROR OverrideRegCloseKey(HKEY hKey)
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

        /// <remarks>The overridden <see cref="PInvoke.RegQueryValueEx(HKEY, PCWSTR, uint*, REG_VALUE_TYPE*, byte*, uint*)"/> function.</remarks>
        /// <inheritdoc cref="PInvoke.RegQueryValueEx(HKEY, PCWSTR, uint*, REG_VALUE_TYPE*, byte*, uint*)"/>
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

        /// <inheritdoc/>
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
