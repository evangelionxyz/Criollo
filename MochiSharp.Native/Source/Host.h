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
#include <filesystem>

#include <nethost.h>

#include <coreclr_delegates.h>
#include <hostfxr.h>

extern hostfxr_initialize_for_runtime_config_fn init_fptr;
extern hostfxr_get_runtime_delegate_fn get_delegate_fptr;
extern hostfxr_close_fn close_fptr;

namespace MochiSharp
{
    struct Vector3
    {
        float X;
        float Y;
        float Z;
    };

    struct Transform
    {
        Vector3 Position;
        Vector3 Rotation;
        Vector3 Scale;
    };

    struct EngineInterface
    {
        typedef void (*LogFunc)(const char *message);
        LogFunc LogMessage;
    };

    typedef int (CORECLR_DELEGATE_CALLTYPE *InitializeFn)(EngineInterface *engineApi);
    typedef int (CORECLR_DELEGATE_CALLTYPE *LoadAssemblyFn)(const char *path);
    typedef int (CORECLR_DELEGATE_CALLTYPE *CreateInstanceFn)(const char *typeName);
    typedef void (CORECLR_DELEGATE_CALLTYPE *DestroyInstanceFn)(int instanceId);
    typedef int (CORECLR_DELEGATE_CALLTYPE *BindInstanceMethodFn)(int instanceId, const char *methodName, int signature);
    typedef int (CORECLR_DELEGATE_CALLTYPE *BindStaticMethodFn)(const char *typeName, const char *methodName, int signature);
    typedef int (CORECLR_DELEGATE_CALLTYPE *InvokeVoidFn)(int methodId);
    typedef int (CORECLR_DELEGATE_CALLTYPE *InvokeFloatFn)(int methodId, float arg0);
    typedef int (CORECLR_DELEGATE_CALLTYPE *InvokeInt2Fn)(int methodId, int a, int b, int *outResult);
    typedef int (CORECLR_DELEGATE_CALLTYPE *InvokeVector3Fn)(int methodId, const Vector3 *a, const Vector3 *b, Vector3 *outResult);
    typedef int (CORECLR_DELEGATE_CALLTYPE *InvokeTransformInFn)(int methodId, const Transform *transform);
    typedef int (CORECLR_DELEGATE_CALLTYPE *InvokeTransformOutFn)(int methodId, Transform *outTransform);

    struct HostSettings
    {
    };

    class DotNetHost
    {
    private:
        hostfxr_handle m_Ctx = nullptr;
        std::filesystem::path m_BaseDir;
        InitializeFn ManagedInit = nullptr;
        LoadAssemblyFn ManagedLoadAssembly = nullptr;
        CreateInstanceFn ManagedCreateInstance = nullptr;
        DestroyInstanceFn ManagedDestroyInstance = nullptr;
        BindInstanceMethodFn ManagedBindInstanceMethod = nullptr;
        BindStaticMethodFn ManagedBindStaticMethod = nullptr;
        InvokeVoidFn ManagedInvokeVoid = nullptr;
        InvokeFloatFn ManagedInvokeFloat = nullptr;
        InvokeInt2Fn ManagedInvokeInt2 = nullptr;
        InvokeVector3Fn ManagedInvokeVector3 = nullptr;
        InvokeTransformInFn ManagedInvokeTransformIn = nullptr;
        InvokeTransformOutFn ManagedInvokeTransformOut = nullptr;

    public:
        static void EngineLog(const char *msg);
        bool Init(const std::wstring &configPath);
        bool LoadAssembly(const char *path);
        int CreateInstance(const char *typeName);
        void DestroyInstance(int instanceId);
        int BindInstanceMethod(int instanceId, const char *methodName, int signature);
        int BindStaticMethod(const char *typeName, const char *methodName, int signature);
        bool InvokeVoid(int methodId);
        bool InvokeFloat(int methodId, float arg0);
        bool InvokeInt2(int methodId, int a, int b, int &outResult);
        bool InvokeVector3(int methodId, const Vector3 &a, const Vector3 &b, Vector3 &outResult);
        bool InvokeTransformIn(int methodId, const Transform &transform);
        bool InvokeTransformOut(int methodId, Transform &outTransform);

    private:
        bool LoadHostFxr();
    };
}

#endif // !HOST_H
