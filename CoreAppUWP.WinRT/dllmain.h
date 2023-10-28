#pragma once
#include "pch.h"
#include "mutex"

static void StartHook();
static void EndHook();

static std::mutex xamlKeyMtx;
static std::map<HKEY, bool> xamlKeyMap;

static decltype(&RegOpenKeyExW) BaseRegOpenKeyExW = &RegOpenKeyExW;
static LSTATUS APIENTRY OverrideRegOpenKeyExW(HKEY hkey, LPCWSTR lpSubKey, DWORD ulOptions, REGSAM samDesired, PHKEY phkResult);

static decltype(&RegCloseKey) BaseRegCloseKey = &RegCloseKey;
static LSTATUS APIENTRY OverrideRegCloseKey(HKEY hKey);

static decltype(&RegQueryValueExW) BaseRegQueryValueExW = &RegQueryValueExW;
static LSTATUS APIENTRY OverrideRegQueryValueExW(HKEY hKey, LPCWSTR lpValueName, LPDWORD lpReserved, LPDWORD lpType, LPBYTE lpData, LPDWORD lpcbData);
