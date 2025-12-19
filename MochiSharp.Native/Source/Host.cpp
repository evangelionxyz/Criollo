// Copyright (c) 2025 Evangelion Manuhutu

#include "Host.h"
#include <iostream>
#include <iomanip>
#include <assert.h>

#define STR(s) L ## s
#define CH(c) L ## c
#define DIR_SEPARATOR L'\\'

hostfxr_initialize_for_runtime_config_fn init_fptr = nullptr;
hostfxr_get_runtime_delegate_fn get_delegate_fptr = nullptr;
hostfxr_close_fn close_fptr = nullptr;

#include <filesystem>

static std::filesystem::path GetExecutablePath()
{
#ifdef _WIN32
    wchar_t buffer[MAX_PATH];
    DWORD len = GetModuleFileNameW(nullptr, buffer, MAX_PATH);
    if (len == 0)
    {
        return std::filesystem::current_path();
    }
    return std::filesystem::path(std::wstring(buffer, len));
#else
    return std::filesystem::current_path();
#endif
}

static std::filesystem::path GetExecutableDirectory()
{
    auto exePath = GetExecutablePath();
    return exePath.has_parent_path() ? exePath.parent_path() : std::filesystem::current_path();
}

static std::filesystem::path ResolvePathRelativeToExecutable(const std::filesystem::path &path)
{
    if (path.is_absolute())
    {
        return path;
    }

    auto exeDir = GetExecutableDirectory();
    auto candidate = exeDir / path;
    if (std::filesystem::exists(candidate))
    {
        return candidate;
    }

    return std::filesystem::current_path() / path;
}

namespace MochiSharp
{
    void DotNetHost::EngineLog(const char *msg)
    {
        std::cout << "[C++ Engine] " << msg << "\n";
    }

    bool DotNetHost::Init(const std::wstring &configPath)
    {
        if (!LoadHostFxr())
        {
            return false;
        }

        auto configFullPath = ResolvePathRelativeToExecutable(std::filesystem::path(configPath));
        if (!std::filesystem::exists(configFullPath))
        {
            std::wcout << L"[C++ Engine] runtimeconfig not found: " << configFullPath.wstring() << L"\n";
            return false;
        }

        m_BaseDir = configFullPath.parent_path();

        int rc = init_fptr(configFullPath.c_str(), nullptr, &m_Ctx);
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
        auto managedCorePath = (m_BaseDir / L"MochiSharp.Managed.dll");
        if (!std::filesystem::exists(managedCorePath))
        {
            std::wcout << L"[C++ Engine] MochiSharp.Managed.dll not found: " << managedCorePath.wstring() << L"\n";
            return false;
        }

        // Get Initialize
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("Initialize"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedInit);

        if (rc != 0 || ManagedInit == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load Initialize function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get LoadAssembly
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("LoadAssembly"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedLoadAssembly);

        if (rc != 0 || ManagedLoadAssembly == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load LoadAssembly function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get CreateInstance
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("CreateInstance"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedCreateInstance);

        if (rc != 0 || ManagedCreateInstance == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load CreateInstance function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get DestroyInstance
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("DestroyInstance"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedDestroyInstance);

        if (rc != 0 || ManagedDestroyInstance == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load DestroyInstance function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get BindInstanceMethod
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("BindInstanceMethod"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedBindInstanceMethod);

        if (rc != 0 || ManagedBindInstanceMethod == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load BindInstanceMethod function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get BindStaticMethod
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("BindStaticMethod"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedBindStaticMethod);

        if (rc != 0 || ManagedBindStaticMethod == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load BindStaticMethod function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get InvokeVoid
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("InvokeVoid"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedInvokeVoid);

        if (rc != 0 || ManagedInvokeVoid == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load InvokeVoid function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get InvokeFloat
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("InvokeFloat"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedInvokeFloat);

        if (rc != 0 || ManagedInvokeFloat == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load InvokeFloat function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get InvokeInt2
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("InvokeInt2"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedInvokeInt2);

        if (rc != 0 || ManagedInvokeInt2 == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load InvokeInt2 function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get InvokeVector3
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("InvokeVector3"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedInvokeVector3);

        if (rc != 0 || ManagedInvokeVector3 == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load InvokeVector3 function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get InvokeTransformIn
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("InvokeTransformIn"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedInvokeTransformIn);

        if (rc != 0 || ManagedInvokeTransformIn == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load InvokeTransformIn function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Get InvokeTransformOut
        rc = load_assembly_and_get_function_pointer(
            managedCorePath.c_str(),
            STR("MochiSharp.Managed.Core.Bootstrap, MochiSharp.Managed"),
            STR("InvokeTransformOut"),
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void **)&ManagedInvokeTransformOut);

        if (rc != 0 || ManagedInvokeTransformOut == nullptr)
        {
            std::cout << "[C++ Engine] Failed to load InvokeTransformOut function (rc: 0x" << std::hex << rc << std::dec << ")\n";
            return false;
        }

        // Call Initialize
        EngineInterface api;
        api.LogMessage = &EngineLog;
        ManagedInit(&api);

        return true;
    }

    bool DotNetHost::LoadAssembly(const char *path)
    {
        if (!ManagedLoadAssembly)
        {
            return false;
        }

        std::filesystem::path scriptPath(path);
        if (!scriptPath.is_absolute())
        {
            scriptPath = m_BaseDir / scriptPath;
        }

        auto resolved = scriptPath.string();
        return ManagedLoadAssembly(resolved.c_str()) != 0;
    }

    int DotNetHost::CreateInstance(const char *typeName)
    {
        if (!ManagedCreateInstance)
        {
            return 0;
        }

        return ManagedCreateInstance(typeName);
    }

    void DotNetHost::DestroyInstance(int instanceId)
    {
        if (ManagedDestroyInstance)
        {
            ManagedDestroyInstance(instanceId);
        }
    }

    int DotNetHost::BindInstanceMethod(int instanceId, const char *methodName, int signature)
    {
        if (!ManagedBindInstanceMethod)
        {
            return 0;
        }

        return ManagedBindInstanceMethod(instanceId, methodName, signature);
    }

    int DotNetHost::BindStaticMethod(const char *typeName, const char *methodName, int signature)
    {
        if (!ManagedBindStaticMethod)
        {
            return 0;
        }

        return ManagedBindStaticMethod(typeName, methodName, signature);
    }

    bool DotNetHost::InvokeVoid(int methodId)
    {
        if (!ManagedInvokeVoid)
        {
            return false;
        }

        return ManagedInvokeVoid(methodId) != 0;
    }

    bool DotNetHost::InvokeFloat(int methodId, float arg0)
    {
        if (!ManagedInvokeFloat)
        {
            return false;
        }

        return ManagedInvokeFloat(methodId, arg0) != 0;
    }

    bool DotNetHost::InvokeInt2(int methodId, int a, int b, int &outResult)
    {
        if (!ManagedInvokeInt2)
        {
            return false;
        }

        return ManagedInvokeInt2(methodId, a, b, &outResult) != 0;
    }

    bool DotNetHost::InvokeVector3(int methodId, const Vector3 &a, const Vector3 &b, Vector3 &outResult)
    {
        if (!ManagedInvokeVector3)
        {
            return false;
        }

        return ManagedInvokeVector3(methodId, &a, &b, &outResult) != 0;
    }

    bool DotNetHost::InvokeTransformIn(int methodId, const Transform &transform)
    {
        if (!ManagedInvokeTransformIn)
        {
            return false;
        }

        return ManagedInvokeTransformIn(methodId, &transform) != 0;
    }

    bool DotNetHost::InvokeTransformOut(int methodId, Transform &outTransform)
    {
        if (!ManagedInvokeTransformOut)
        {
            return false;
        }

        return ManagedInvokeTransformOut(methodId, &outTransform) != 0;
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