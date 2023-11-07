#pragma once

#include "HookRegistry.g.h"
#include "mutex"

namespace winrt::CoreAppUWP::WinRT::implementation
{
    static std::mutex xamlKeyMtx;
    static std::map<HKEY, bool> xamlKeyMap;

    static decltype(&RegOpenKeyExW) BaseRegOpenKeyExW = &RegOpenKeyExW;
    static decltype(&RegCloseKey) BaseRegCloseKey = &RegCloseKey;
    static decltype(&RegQueryValueExW) BaseRegQueryValueExW = &RegQueryValueExW;

    struct HookRegistry : HookRegistryT<HookRegistry>
    {
        HookRegistry() = default;

        bool IsHooked() const { return isHooked; }
        void IsHooked(bool value);

        void StartHook();
        void EndHook();
        void Close();

    private:
        bool isHooked = false;
        HANDLE currentThread = GetCurrentThread();

        static LSTATUS APIENTRY OverrideRegOpenKeyExW(HKEY hkey, LPCWSTR lpSubKey, DWORD ulOptions, REGSAM samDesired, PHKEY phkResult);
        static LSTATUS APIENTRY OverrideRegCloseKey(HKEY hKey);
        static LSTATUS APIENTRY OverrideRegQueryValueExW(HKEY hKey, LPCWSTR lpValueName, LPDWORD lpReserved, LPDWORD lpType, LPBYTE lpData, LPDWORD lpcbData);
    };
}

namespace winrt::CoreAppUWP::WinRT::factory_implementation
{
    struct HookRegistry : HookRegistryT<HookRegistry, implementation::HookRegistry>
    {
    };
}
