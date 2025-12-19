// Copyright (c) 2025 Evangelion Manuhutu

#ifndef HOST_H
#define HOST_H

#ifdef _WIN32
    #include <Windows.h>
#else
    #include <dlfcn.h>
#endif

#include <vector>
#include <iostream>
#include <string>

#include <nethost.h>

#include <coreclr_delegates.h>
#include <hostfxr.h>

extern hostfxr_initialize_for_runtime_config_fn init_fptr;
extern hostfxr_get_runtime_delegate_fn get_delegate_fptr;
extern hostfxr_close_fn close_fptr;

namespace MochiSharp
{
    struct EngineInterface
    {
        typedef void (*LogFunc)(const char *message);
        LogFunc LogMessage;
    };

    typedef int (CORECLR_DELEGATE_CALLTYPE *InitializeFn)(EngineInterface *engineApi);
    typedef int (CORECLR_DELEGATE_CALLTYPE *LoadAssemblyFn)(const char *path);
    typedef void (CORECLR_DELEGATE_CALLTYPE *UpdateFn)();

    struct HostSettings
    {
    };

    class DotNetHost
    {
    private:
        hostfxr_handle m_Ctx = nullptr;
        InitializeFn ManagedInit = nullptr;
        LoadAssemblyFn ManagedLoad = nullptr;
        UpdateFn ManagedUpdate = nullptr;

    public:
        static void EngineLog(const char *msg);
        bool Init(const std::wstring &configPath);
        void LoadScript(const char *path);
        void Update();

    private:
        bool LoadHostFxr();
    };
}

#endif // !HOST_H
