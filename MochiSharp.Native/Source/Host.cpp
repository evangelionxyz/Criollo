// Copyright (c) 2025 Evangelion Manuhutu

#include "Host.h"
#include <print>
#include <assert.h>

#define STR(s) L ## s
#define CH(c) L ## c
#define DIR_SEPARATOR L'\\'

hostfxr_initialize_for_runtime_config_fn init_fptr = nullptr;
hostfxr_get_runtime_delegate_fn get_delegate_fptr = nullptr;
hostfxr_close_fn close_fptr = nullptr;

#include <filesystem>

namespace MochiSharp
{
    void DotNetHost::EngineLog(const char *msg)
    {
        std::println("[C++ Engine] {}", msg);
    }

    bool DotNetHost::Init(const std::wstring &configPath)
    {
        if (!LoadHostFxr())
        {
            return false;
        }

        int rc = init_fptr(configPath.c_str(), nullptr, &m_Ctx);
        if (rc != 0 || m_Ctx == nullptr)
        {
            return false;
        }

        load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;
        rc = get_delegate_fptr(
            m_Ctx,
            hdt_load_assembly_and_get_function_pointer,
            (void **)&load_assembly_and_get_function_pointer);

        if (rc != 0 || load_assembly_and_get_function_pointer == nullptr)
        {
            return false;
        }

        // Load ManagedCore and get the function pointers
        assert(std::filesystem::exists("MochiSharp.Managed.dll") && "MochiSharp.Managed.dll not found");

        // Get Initialize
        rc = load_assembly_and_get_function_pointer(
            STR("MochiSharp.Managed.dll"),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("Initialize"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedInit);

        if (rc != 0 || ManagedInit == nullptr)
        {
            std::println("[C++ Engine] Failed to load Initialize function (rc: 0x{:X})", rc);
            return false;
        }

        // Get LoadGameAssembly
        rc = load_assembly_and_get_function_pointer(
            STR("MochiSharp.Managed.dll"),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("LoadGameAssembly"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedLoad);

        if (rc != 0 || ManagedLoad == nullptr)
        {
            std::println("[C++ Engine] Failed to load LoadGameAssembly function (rc: 0x{:X})", rc);
            return false;
        }

        // Get Update
        rc = load_assembly_and_get_function_pointer(
            STR("MochiSharp.Managed.dll"),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("Update"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedUpdate);

        if (rc != 0 || ManagedUpdate == nullptr)
        {
            std::println("[C++ Engine] Failed to load Update function (rc: 0x{:X})", rc);
            return false;
        }

        // Call Initialize
        EngineInterface api;
        api.LogMessage = &EngineLog;
        ManagedInit(&api);

        return true;
    }

    void DotNetHost::LoadScript(const char *path)
    {
        if (ManagedLoad)
        {
            ManagedLoad(path);
        }
    }

    void DotNetHost::Update()
    {
        if (ManagedUpdate)
        {
            ManagedUpdate();
        }
    }

    bool DotNetHost::LoadHostFxr()
    {
        char_t buffer[MAX_PATH];
        size_t bufferSize = sizeof(buffer) / sizeof(buffer[0]);
        int rc = get_hostfxr_path(buffer, &bufferSize, nullptr);
        if (rc != 0)
        {
            return false;
        }

        HMODULE lib = LoadLibraryW(buffer);
        init_fptr = (hostfxr_initialize_for_runtime_config_fn)GetProcAddress(lib, "hostfxr_initialize_for_runtime_config");
        get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)GetProcAddress(lib, "hostfxr_get_runtime_delegate");
        close_fptr = (hostfxr_close_fn)GetProcAddress(lib, "hostfxr_close");

        return (init_fptr && get_delegate_fptr && close_fptr);
    }

}