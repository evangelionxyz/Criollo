// Copyright (c) 2025 Evangelion Manuhutu

#include "Host.h"

#include <thread>
#include <chrono>
#include <print>

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
        host.InvokeVoid(onAwake);
    }

    if (onStart)
    {
        host.InvokeVoid(onStart);
    }

    // int addition / multiplication
    if (addInt)
    {
        int result = 0;
        if (host.InvokeInt2(addInt, 2, 3, result))
        {
            std::println("[C++] AddInt(2,3) = {}", result);
        }
    }

    if (mulInt)
    {
        int result = 0;
        if (host.InvokeInt2(mulInt, 6, 7, result))
        {
            std::println("[C++] MulInt(6,7) = {}", result);
        }
    }

    // Vector3 addition / multiplication
    MochiSharp::Vector3 a{ 1.0f, 2.0f, 3.0f };
    MochiSharp::Vector3 b{ 4.0f, 5.0f, 6.0f };

    if (addVec)
    {
        MochiSharp::Vector3 sum{};
        if (host.InvokeVector3(addVec, a, b, sum))
        {
            std::println("[C++] AddVector({},{},{}) + ({},{},{}) = ({},{},{})",
                a.X, a.Y, a.Z, b.X, b.Y, b.Z,
                sum.X, sum.Y, sum.Z);
        }
    }

    if (mulVec)
    {
        MochiSharp::Vector3 prod{};
        if (host.InvokeVector3(mulVec, a, b, prod))
        {
            std::println("[C++] MulVector({},{},{}) * ({},{},{}) = ({},{},{})",
                a.X, a.Y, a.Z, b.X, b.Y, b.Z,
                prod.X, prod.Y, prod.Z);
        }
    }

    // Transform roundtrip
    if (setTransform)
    {
        MochiSharp::Transform t{};
        t.Position = { 10.0f, 0.0f, 5.0f };
        t.Rotation = { 0.0f, 90.0f, 0.0f };
        t.Scale = { 1.0f, 1.0f, 1.0f };
        host.InvokeTransformIn(setTransform, t);
    }

    if (getTransform)
    {
        MochiSharp::Transform t{};
        if (host.InvokeTransformOut(getTransform, t))
        {
            std::println("[C++] GetTransform -> Pos=({},{},{}) Rot=({},{},{}) Scale=({},{},{})",
                t.Position.X, t.Position.Y, t.Position.Z,
                t.Rotation.X, t.Rotation.Y, t.Rotation.Z,
                t.Scale.X, t.Scale.Y, t.Scale.Z);
        }
    }

    bool running = true;

    auto start = std::chrono::steady_clock::now();

    while (running)
    {
        auto end = std::chrono::steady_clock::now();
        float deltaTime = std::chrono::duration_cast<std::chrono::milliseconds>(end - start).count() / 1000.0f;
        start = end;

        if (onUpdate) host.InvokeFloat(onUpdate, deltaTime);
        
        std::this_thread::sleep_for(std::chrono::milliseconds(16));
    }

    return 0;
}
