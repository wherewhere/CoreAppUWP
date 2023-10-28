#include "pch.h"
#include "HookRegistry.h"
#include "HookRegistry.g.cpp"

namespace winrt::CoreAppUWP::WinRT::implementation
{
    bool HookRegistry::IsHooked()
    {
        return true;
    }
}
