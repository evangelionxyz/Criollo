// Copyright (c) 2025 Evangelion Manuhutu

#include "Host.h"

#include <thread>
#include <chrono>
#include <print>

namespace ExampleInterop
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
}

enum ScriptMethodSignature : int
{
    Void = 0,
    Void_Float = 1,
    Void_Int = 2,
    Void_Bool = 3,

    Int_IntInt = 10,
    Vector3_Vector3Vector3 = 11,
    Void_Transform = 12,
    Transform = 13,
};

#ifdef _WIN32
int __cdecl wmain(int argc, wchar_t *argv[])
#else
int main(int argc, char *argv[])
#endif
{
    // MochiSharp::HostSettings settings;

    MochiSharp::DotNetHost host;
    if (!host.Init(L"MochiSharp.Managed.runtimeconfig.json"))
    {
        return 1;
    }

    // Load the script assembly
    if (!host.LoadAssembly("Example.Managed.dll"))
    {
        return 1;
    }

    // Register signatures (the core stays generic; the app defines what these IDs mean).
    // Note: use assembly-qualified names for app-defined structs.
    const char *vector3Type = "Example.Managed.Interop.Vector3, Example.Managed";
    const char *transformType = "Example.Managed.Interop.Transform, Example.Managed";

    {
        host.RegisterSignature(ScriptMethodSignature::Void, "System.Void", nullptr, 0);
    }

    {
        const char *p1[] = { "System.Single" };
        host.RegisterSignature(ScriptMethodSignature::Void_Float, "System.Void", p1, 1);
    }

    {
        const char *p2[] = { "System.Int32", "System.Int32" };
        host.RegisterSignature(ScriptMethodSignature::Int_IntInt, "System.Int32", p2, 2);
    }

    {
        const char *p2[] = { vector3Type, vector3Type };
        host.RegisterSignature(ScriptMethodSignature::Vector3_Vector3Vector3, vector3Type, p2, 2);
    }

    {
        const char *p1[] = { transformType };
        host.RegisterSignature(ScriptMethodSignature::Void_Transform, "System.Void", p1, 1);
    }

    {
        host.RegisterSignature(ScriptMethodSignature::Transform, transformType, nullptr, 0);
    }

    // Create a script instance and bind whatever lifecycle methods you want.
    int instance = host.CreateInstance("Example.Managed.Scripts.Player");
    if (instance == 0)
    {
        return 1;
    }

    int onAwake = host.BindInstanceMethod(instance, "OnAwake", ScriptMethodSignature::Void);
    int onStart = host.BindInstanceMethod(instance, "OnStart", ScriptMethodSignature::Void);
    int onUpdate = host.BindInstanceMethod(instance, "OnUpdate", ScriptMethodSignature::Void_Float);
    int addInt = host.BindInstanceMethod(instance, "AddInt", ScriptMethodSignature::Int_IntInt);
    int mulInt = host.BindInstanceMethod(instance, "MulInt", ScriptMethodSignature::Int_IntInt);
    int addVec = host.BindInstanceMethod(instance, "AddVector", ScriptMethodSignature::Vector3_Vector3Vector3);
    int mulVec = host.BindInstanceMethod(instance, "MulVector", ScriptMethodSignature::Vector3_Vector3Vector3);
    int setTransform = host.BindInstanceMethod(instance, "SetTransform", ScriptMethodSignature::Void_Transform);
    int getTransform = host.BindInstanceMethod(instance, "GetTransform", ScriptMethodSignature::Transform);

    if (onAwake)
    {
        host.Invoke(onAwake, nullptr, 0, nullptr);
    }

    if (onStart)
    {
        host.Invoke(onStart, nullptr, 0, nullptr);
    }

    // int addition / multiplication
    if (addInt)
    {
        int result = 0;
        int a = 2;
        int b = 3;
        void *args[] = { &a, &b };
        if (host.Invoke(addInt, args, 2, &result))
        {
            std::println("[C++] AddInt(2,3) = {}", result);
        }
    }

    if (mulInt)
    {
        int result = 0;
        int a = 6;
        int b = 7;
        void *args[] = { &a, &b };
        if (host.Invoke(mulInt, args, 2, &result))
        {
            std::println("[C++] MulInt(6,7) = {}", result);
        }
    }

    // Vector3 addition / multiplication
    ExampleInterop::Vector3 a{ 1.0f, 2.0f, 3.0f };
    ExampleInterop::Vector3 b{ 4.0f, 5.0f, 6.0f };

    if (addVec)
    {
        ExampleInterop::Vector3 sum{};
        void *args[] = { &a, &b };
        if (host.Invoke(addVec, args, 2, &sum))
        {
            std::println("[C++] AddVector({},{},{}) + ({},{},{}) = ({},{},{})",
                a.X, a.Y, a.Z, b.X, b.Y, b.Z,
                sum.X, sum.Y, sum.Z);
        }
    }

    if (mulVec)
    {
        ExampleInterop::Vector3 prod{};
        void *args[] = { &a, &b };
        if (host.Invoke(mulVec, args, 2, &prod))
        {
            std::println("[C++] MulVector({},{},{}) * ({},{},{}) = ({},{},{})",
                a.X, a.Y, a.Z, b.X, b.Y, b.Z,
                prod.X, prod.Y, prod.Z);
        }
    }

    // Transform roundtrip
    if (setTransform)
    {
        ExampleInterop::Transform t{};
        t.Position = { 10.0f, 0.0f, 5.0f };
        t.Rotation = { 0.0f, 90.0f, 0.0f };
        t.Scale = { 1.0f, 1.0f, 1.0f };
        void *args[] = { &t };
        host.Invoke(setTransform, args, 1, nullptr);
    }

    if (getTransform)
    {
        ExampleInterop::Transform t{};
        if (host.Invoke(getTransform, nullptr, 0, &t))
        {
            std::println("[C++] GetTransform -> Pos=({},{},{}) Rot=({},{},{}) Scale=({},{},{})",
                t.Position.X, t.Position.Y, t.Position.Z,
                t.Rotation.X, t.Rotation.Y, t.Rotation.Z,
                t.Scale.X, t.Scale.Y, t.Scale.Z);
        }
    }

    bool running = true;
    auto start = std::chrono::steady_clock::now();

    int runningCount = 0;
    while (running && runningCount <= 10)
    {
        auto end = std::chrono::steady_clock::now();
        float deltaTime = std::chrono::duration_cast<std::chrono::milliseconds>(end - start).count() / 1000.0f;
        start = end;

        if (onUpdate)
        {
            void *args[] = { &deltaTime };
            host.Invoke(onUpdate, args, 1, nullptr);
        }
        
        std::this_thread::sleep_for(std::chrono::milliseconds(16));
        runningCount++;
    }

    return 0;
}
