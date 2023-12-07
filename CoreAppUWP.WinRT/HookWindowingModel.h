#pragma once

#include "HookWindowingModel.g.h"
#include "appmodel.h"
#include "mutex"

namespace winrt::CoreAppUWP::WinRT::implementation
{
    static HANDLE currentProcessToken = GetCurrentThreadEffectiveToken();
    static WindowingModel windowingModel = WindowingModel::ClassicDesktop;

    static decltype(&AppPolicyGetWindowingModel) BaseAppPolicyGetWindowingModel = &AppPolicyGetWindowingModel;

    struct HookWindowingModel : HookWindowingModelT<HookWindowingModel>
    {
        HookWindowingModel() = default;

        static WindowingModel WindowingModel() { return windowingModel; }
        static void WindowingModel(CoreAppUWP::WinRT::WindowingModel value) { windowingModel = value; }

        bool IsHooked() const { return isHooked; }
        void IsHooked(bool value);

        void StartHook();
        void EndHook();
        void Close();

    private:
        bool isHooked = false;
        HANDLE currentThread = GetCurrentThread();

        static LONG APIENTRY OverrideAppPolicyGetWindowingModel(HANDLE processToken, AppPolicyWindowingModel* policy);
    };
}

namespace winrt::CoreAppUWP::WinRT::factory_implementation
{
    struct HookWindowingModel : HookWindowingModelT<HookWindowingModel, implementation::HookWindowingModel>
    {
    };
}
