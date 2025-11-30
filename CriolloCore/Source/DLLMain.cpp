// Copyright (c) 2025 Evangelion Manuhutu

#include "pch.h"
#include "Base.h"
#include "CoreCLRHost.h"

using namespace Criollo;

static CoreCLRHost* g_pCoreHost = nullptr;

extern "C"
{
    CRIOLLO_API bool InitializeCoreRuntime(const wchar_t* runtimePath, const wchar_t* assemblyPath)
    {
        if (g_pCoreHost != nullptr)
        {
            return false; // Already initialized
        }

        g_pCoreHost = new CoreCLRHost();
        if (!g_pCoreHost->Initialize(runtimePath, assemblyPath))
        {
            delete g_pCoreHost;
            g_pCoreHost = nullptr;
            return false;
        }

        return true;
    }

    // Example function to shutdown CoreCLR
    CRIOLLO_API void ShutdownCoreRuntime()
    {
        if (g_pCoreHost != nullptr)
        {
            g_pCoreHost->Shutdown();
            delete g_pCoreHost;
            g_pCoreHost = nullptr;
        }
    }

    CRIOLLO_API bool ExecuteManagedAssembly(const wchar_t* assemblyPath)
    {
        if (g_pCoreHost == nullptr || !g_pCoreHost->IsInitialized())
        {
            return false;
        }

        unsigned int exitCode = 0;
        return g_pCoreHost->ExecuteAssembly(assemblyPath, 0, nullptr, &exitCode);
    }

    // Example function to get the host instance (for creating delegates)
    CRIOLLO_API CoreCLRHost* GetCoreHost()
    {
        return g_pCoreHost;
    }

    // Helper function to create a delegate for TestMethod specifically
    // This demonstrates how to create type-safe exported functions for specific delegates
    CRIOLLO_API bool CreateTestMethodDelegate(void** outDelegate)
    {
        if (g_pCoreHost == nullptr || !g_pCoreHost->IsInitialized() || outDelegate == nullptr)
        {
            return false;
        }

        return g_pCoreHost->CreateDelegate(
            "TestScript",           // Assembly name (without .dll extension)
            "Criollo.Test",         // Fully qualified type name
            "TestMethod",           // Method name
            outDelegate
        );
    }

    // Generic helper function to create any delegate
    CRIOLLO_API bool CreateManagedDelegate(const char* assemblyName, const char* typeName, const char* methodName, void** outDelegate)
    {
        if (g_pCoreHost == nullptr || !g_pCoreHost->IsInitialized() ||
            outDelegate == nullptr || assemblyName == nullptr ||
            typeName == nullptr || methodName == nullptr)
        {
            return false;
        }

        return g_pCoreHost->CreateDelegate(
            assemblyName,
            typeName,
            methodName,
            outDelegate
        );
    }
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        // CoreCLR will be initialized explicitly via InitializeCoreRuntime
        break;
    case DLL_THREAD_ATTACH:
        break;
    case DLL_THREAD_DETACH:
        break;
    case DLL_PROCESS_DETACH:
        // Clean up CoreCLR on detach
        ShutdownCoreRuntime();
        break;
    }
    return TRUE;
}

