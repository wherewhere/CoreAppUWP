#include "pch.h"
#include "HookWindowingModel.h"
#include "HookWindowingModel.g.cpp"
#include "detours.h"

namespace winrt::CoreAppUWP::WinRT::implementation
{
    void HookWindowingModel::IsHooked(bool value)
    {
        if (value == isHooked) { return; }
        if (value) { StartHook(); }
        else { EndHook(); }
    }

    void HookWindowingModel::StartHook()
    {
        if (!isHooked)
        {
            DetourTransactionBegin();
            DetourUpdateThread(currentThread);
            DetourAttach((PVOID*)&BaseAppPolicyGetWindowingModel, OverrideAppPolicyGetWindowingModel);
            DetourTransactionCommit();
            isHooked = true;
        }
    }

    void HookWindowingModel::EndHook()
    {
        if (isHooked)
        {
            DetourTransactionBegin();
            DetourUpdateThread(currentThread);
            DetourDetach((PVOID*)&BaseAppPolicyGetWindowingModel, OverrideAppPolicyGetWindowingModel);
            DetourTransactionCommit();
            isHooked = false;
        }
    }

    void HookWindowingModel::Close()
    {
        if (currentThread)
        {
            EndHook();
            currentThread = nullptr;
        }
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
