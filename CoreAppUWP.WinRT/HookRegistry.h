#pragma once

#include "HookRegistry.g.h"

namespace winrt::CoreAppUWP::WinRT::implementation
{
    struct HookRegistry : HookRegistryT<HookRegistry>
    {
        static bool IsHooked();
    };
}

namespace winrt::CoreAppUWP::WinRT::factory_implementation
{
    struct HookRegistry : HookRegistryT<HookRegistry, implementation::HookRegistry>
    {
    };
}
