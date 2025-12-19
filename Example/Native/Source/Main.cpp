// Copyright (c) 2025 Evangelion Manuhutu

#include "Host.h"

#ifdef _WIN32
int __cdecl wmain(int argc, wchar_t *argv[])
#else
int main(int argc, char *argv[])
#endif
{
    MochiSharp::DotNetHost host;
    if (!host.Init(L"MochiSharp.Managed.runtimeconfig.json"))
    {
        return 1;
    }

    host.LoadScript("Example.Managed.dll");
    bool running = true;
    while (running)
    {
        host.Update();
    }

    return 0;
}
