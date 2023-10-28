#include "pch.h"
#include "detours.h"
#include "dllmain.h"
#include "map"
#include "windows.h"

BOOL APIENTRY DllMain(HMODULE, DWORD  ul_reason_for_call, LPVOID)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        break;
    case DLL_THREAD_ATTACH:
        StartHook();
        break;
    case DLL_THREAD_DETACH:
        EndHook();
        break;
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

static void StartHook()
{
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourAttach((PVOID*)&BaseRegOpenKeyExW, OverrideRegOpenKeyExW);
    DetourAttach((PVOID*)&BaseRegCloseKey, OverrideRegCloseKey);
    DetourAttach((PVOID*)&BaseRegQueryValueExW, OverrideRegQueryValueExW);
    DetourTransactionCommit();
}

static void EndHook()
{
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourDetach((PVOID*)&BaseRegOpenKeyExW, OverrideRegOpenKeyExW);
    DetourDetach((PVOID*)&BaseRegCloseKey, OverrideRegCloseKey);
    DetourDetach((PVOID*)&BaseRegQueryValueExW, OverrideRegQueryValueExW);
    DetourTransactionCommit();
}

static LSTATUS APIENTRY OverrideRegOpenKeyExW(HKEY hkey, LPCWSTR lpSubKey, DWORD ulOptions, REGSAM samDesired, PHKEY phkResult)
{
    LSTATUS result = BaseRegOpenKeyExW(hkey, lpSubKey, ulOptions, samDesired, phkResult);

    if (hkey == HKEY_LOCAL_MACHINE && !_wcsicmp(lpSubKey, L"Software\\Microsoft\\WinUI\\Xaml"))
    {
        std::lock_guard<std::mutex> lock(xamlKeyMtx);
        if (result == ERROR_FILE_NOT_FOUND)
        {
            xamlKeyMap.emplace((HKEY)INVALID_HANDLE_VALUE, false);
            *phkResult = (HKEY)INVALID_HANDLE_VALUE;
            result = ERROR_SUCCESS;
        }
        else if (result == ERROR_SUCCESS)
        {
            xamlKeyMap.emplace(*phkResult, true);
        }
    }

    return result;
}

static LSTATUS APIENTRY OverrideRegCloseKey(HKEY hKey)
{
    bool isRealKey = false;

    std::lock_guard<std::mutex> lock(xamlKeyMtx);
    auto pos = xamlKeyMap.find(hKey);

    bool isXamlKey = pos != xamlKeyMap.end();
    if (isXamlKey)
    {
        isRealKey = pos->second;
        xamlKeyMap.erase(pos);
    }

    return isXamlKey
        ? isRealKey
        ? BaseRegCloseKey(hKey) // real key
        : ERROR_SUCCESS // simulated key
        : hKey == INVALID_HANDLE_VALUE
        ? ERROR_INVALID_HANDLE
        : BaseRegCloseKey(hKey);
}

static LSTATUS APIENTRY OverrideRegQueryValueExW(HKEY hKey, LPCWSTR lpValueName, LPDWORD lpReserved, LPDWORD lpType, LPBYTE lpData, LPDWORD lpcbData)
{
    LSTATUS result;
    if (lpValueName != NULL && !_wcsicmp(lpValueName, L"EnableUWPWindow"))
    {
        bool isRealKey = false;
        std::lock_guard<std::mutex> lock(xamlKeyMtx);
        auto pos = xamlKeyMap.find(hKey);

        bool   isXamlKey = pos != xamlKeyMap.end();
        if (isXamlKey)
        {
            isRealKey = pos->second;
        }

        if (isXamlKey)
        {
            if (isRealKey)
            {
                // real key
                result = BaseRegQueryValueExW(hKey, lpValueName, lpReserved, lpType, lpData, lpcbData);
                if (result == ERROR_SUCCESS && lpData != NULL)
                {
                    *lpData = 1;
                }
                else if (result == ERROR_FILE_NOT_FOUND)
                {
                    if (lpData == NULL && lpcbData != NULL)
                    {
                        *lpcbData = sizeof(DWORD);
                        result = ERROR_SUCCESS;
                    }
                    else if (lpData != NULL && lpcbData != NULL)
                    {
                        if (*lpcbData >= sizeof(DWORD))
                        {
                            *lpData = 1;
                            result = ERROR_SUCCESS;
                        }
                        else
                        {
                            result = ERROR_MORE_DATA;
                        }
                    }
                }
            }
            else
            {
                // simulated key
                result = ERROR_FILE_NOT_FOUND;
                if (lpData == NULL && lpcbData != NULL)
                {
                    *lpcbData = sizeof(DWORD);
                    result = ERROR_SUCCESS;
                }
                else if (lpData != NULL && lpcbData != NULL)
                {
                    if (*lpcbData >= sizeof(DWORD))
                    {
                        *lpData = 1;
                        result = ERROR_SUCCESS;
                    }
                    else
                    {
                        result = ERROR_MORE_DATA;
                    }
                }
            }
        }
        else
        {
            result = BaseRegQueryValueExW(hKey, lpValueName, lpReserved, lpType, lpData, lpcbData);
        }
    }
    else
    {
        result = BaseRegQueryValueExW(hKey, lpValueName, lpReserved, lpType, lpData, lpcbData);
    }

    return result;
}
