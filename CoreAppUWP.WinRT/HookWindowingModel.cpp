#include "pch.h"
#include "HookWindowingModel.h"
#include "HookWindowingModel.g.cpp"
#include "detours.h"

namespace winrt::CoreAppUWP::WinRT::implementation
{
    void HookWindowingModel::IsHooked(bool value)
    {
        if (value == isHooked)
        {
            return;
        }

        if (value)
        {
            StartHook();
        }
        else
        {
            EndHook();
        }
    }

    void HookWindowingModel::StartHook()
    {
        DetourTransactionBegin();
        DetourUpdateThread(currentThread);
        DetourAttach((PVOID*)&BaseAppPolicyGetWindowingModel, OverrideAppPolicyGetWindowingModel);
        DetourTransactionCommit();
    }

    void HookWindowingModel::EndHook()
    {
        DetourTransactionBegin();
        DetourUpdateThread(currentThread);
        DetourDetach((PVOID*)&BaseAppPolicyGetWindowingModel, OverrideAppPolicyGetWindowingModel);
        DetourTransactionCommit();
    }

    LONG APIENTRY HookWindowingModel::OverrideAppPolicyGetWindowingModel(HANDLE processToken, AppPolicyWindowingModel* policy)
    {
        if (processToken == currentProcessToken)
        {
            *policy = (AppPolicyWindowingModel)windowingModel;
            return ERROR_SUCCESS;
        }
        return BaseAppPolicyGetWindowingModel(processToken, policy);
    }
}
