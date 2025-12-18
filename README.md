# MochiSharp

A modern C# scripting framework for C++ applications using CoreCLR hosting. MochiSharp provides a clean separation between core engine functionality and game scripts, enabling hot-reload capabilities and modular script management.

## Overview

MochiSharp consists of three main components:

- **MochiSharp.Native**: C++ CoreCLR host library for embedding .NET runtime
- **MochiSharp.Managed**: Core engine library with entity system, reflection, and script loading
- **TestScript**: Example game scripts demonstrating the framework

## Architecture

```
MochiSharp.Native (C++)
    |
    v
MochiSharp.Managed (Core Engine)
    - Debug logging system
    - Entity lifecycle management
    - C++ interop bridges
    - Assembly load context
    - Math and component types
    |
    v
TestScript (Game Scripts)
    - PlayerController
    - Custom game logic
```

## Features

- **CoreCLR Hosting**: Embed .NET 9 runtime in C++ applications
- **Entity Component System**: Flexible entity management with lifecycle hooks
- **Hot Reload Support**: Load and unload script assemblies at runtime using AssemblyLoadContext
- **Type Safety**: Strong typing between C# and C++ boundaries
- **Reflection Bridge**: Runtime type introspection for tooling and debugging
- **Isolated Loading**: Multiple script assemblies can coexist in separate contexts

## Requirements

- .NET 9 SDK
- Visual Studio 2022 or compatible C++ compiler
- Windows (current implementation uses Windows-specific APIs)

## Getting Started

### Building the Solution

1. Build the core engine:
```bash
cd MochiSharp.Managed
dotnet build
```

2. Build the example scripts:
```bash
cd TestScript
dotnet build
```

3. Build the native host (using Visual Studio or MSBuild):
```bash
msbuild MochiSharp.Native.sln
```

4. Build the test runtime:
```bash
msbuild TestRuntime.vcxproj
```

### Running the Example

1. Ensure all assemblies are in the TestRuntime output directory:
   - MochiSharp.Native.dll
   - MochiSharp.Managed.dll
   - TestScript.dll

2. Update the runtime path in `TestRuntime/Source/Main.cpp` if needed:
```cpp
std::string runtimePath = R"(C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.1)";
```

3. Run TestRuntime.exe

### Expected Output

```
Creating CoreCLR host with settings
Initializing CoreCLR
----- Entity Component System Test -----
Initializing internal call system..
Entity_GetTransform initialized!
Entity_SetTransform initialized!
DescribeType delegate initialized!
Successfully created all entity lifecycle delegates!
[EntityBridge] Attempting to create entity instance: ID=1, Type=TestScript.Scene.PlayerController
[EntityBridge] Type found: TestScript.Scene.PlayerController
PlayerController started!
--- Simulating game loop for 3 seconds --
Frame 0 - x:0 y:0 z:0
...
PlayerController stopped!
Entity system shutdown complete
```

## Creating Custom Scripts

### 1. Add a new script class in TestScript:

```csharp
using MochiSharp.Managed.Scene;
using MochiSharp.Managed.Core;
using MochiSharp.Managed.Mathf;

namespace TestScript.Scene;

public class MyCustomScript : ScriptableEntity
{
    public MyCustomScript(ulong id) : base(id) { }
    
    public override void Start()
    {
        Debug.Log("MyCustomScript started!");
    }
    
    public override void Update(float deltaTime)
    {
        // Update logic here
    }
    
    public override void Stop()
    {
        Debug.Log("MyCustomScript stopped!");
    }
}
```

### 2. Instantiate from C++:

```cpp
createInstanceDelegate(entityID, "TestScript.Scene.MyCustomScript");
```

## Hot Reload Example

Using ScriptAssemblyManager for runtime script reloading:

```csharp
// In MochiSharp.Managed
var manager = new ScriptAssemblyManager();

// Load scripts
manager.LoadScriptAssembly("path/to/TestScript.dll");

// ... game runs ...

// Unload scripts
manager.UnloadScriptAssembly();

// Rebuild TestScript.dll

// Reload scripts
manager.LoadScriptAssembly("path/to/TestScript.dll");
```

## API Reference

### Entity Lifecycle

All scriptable entities inherit from `ScriptableEntity` and can override:

- `Start()` - Called when entity is initialized
- `Update(float deltaTime)` - Called every frame
- `Stop()` - Called when entity is destroyed

### Transform Access

```csharp
// Get transform
var transform = Transform;
Vector3 position = transform.Position;

// Set transform
transform.Position = new Vector3(1.0f, 2.0f, 3.0f);
Transform = transform;
```

### Debug Logging

```csharp
Debug.Log("Information message");
Debug.LogWarning("Warning message");
Debug.LogError("Error message");
Debug.Assert(condition, "Assertion message");
```

## C++ Integration

### Initialize CoreCLR

```cpp
mochi::CoreCLRHostAPI* host = CreateCoreRuntimeHostWithSettings(
    runtimePath.c_str(), 
    assemblyPath.c_str(), 
    "MochiSharpHost"
);
host->Initialize();
```

### Create Delegates

```cpp
const char* ManagedDLLName = "MochiSharp.Managed";
const char* EntityBridgeClassName = "MochiSharp.Managed.Core.EntityBridge";

EntityStartDelegate startDelegate = nullptr;
host->CreateDelegate(ManagedDLLName, EntityBridgeClassName, "Start", 
    (void**)(&startDelegate));
```

### Entity Management

```cpp
// Create entity instance
createInstanceDelegate(entityID, "TestScript.Scene.PlayerController");

// Call lifecycle methods
startDelegate(entityID);
updateDelegate(entityID, deltaTime);
stopDelegate(entityID);
```

## Advanced Features

### Multiple Script Assemblies

Load multiple script assemblies simultaneously:

```csharp
var coreScripts = new ScriptAssemblyManager();
coreScripts.LoadScriptAssembly("CoreGame.dll");

var dlcScripts = new ScriptAssemblyManager();
dlcScripts.LoadScriptAssembly("DLC1.dll");

var modScripts = new ScriptAssemblyManager();
modScripts.LoadScriptAssembly("UserMod.dll");
```

### Type Introspection

Query type metadata at runtime:

```cpp
DescribeTypeDelegate describeType = nullptr;
host->CreateDelegate("MochiSharp.Managed", 
    "MochiSharp.Managed.Core.ReflectionBridge", 
    "DescribeType", (void**)(&describeType));

int size = describeType("TestScript.Scene.PlayerController", nullptr, 0);
std::string buffer(size, '\0');
describeType("TestScript.Scene.PlayerController", buffer.data(), size);
// buffer now contains JSON metadata
```

## Troubleshooting

### Assembly Not Found

Ensure all assemblies are in the same directory as the executable:
```
TestRuntime/
    TestRuntime.exe
    MochiSharp.Native.dll
    MochiSharp.Managed.dll
    TestScript.dll
```

### Runtime Path Error

Update the runtime path to match your .NET installation:
```cpp
std::string runtimePath = R"(C:\Program Files\dotnet\shared\Microsoft.NETCore.App\<version>)";
```

Check available versions:
```bash
dir "C:\Program Files\dotnet\shared\Microsoft.NETCore.App\"
```

### Type Not Found

Verify the full type name including namespace:
```cpp
// Correct
"TestScript.Scene.PlayerController"

// Incorrect
"PlayerController"
"TestScript.PlayerController"
```

### Delegate Creation Failed

Check that:
1. Assembly name is correct (no .dll extension)
2. Full namespace is specified
3. Method name matches exactly
4. Assembly is loaded in the TPA list

## Contributing

Contributions are welcome. Please ensure:

- Code follows existing style conventions
- All projects build without errors
- Changes are documented
- C++ changes maintain cross-platform compatibility where possible

## License

This project is licensed under the MIT License. See LICENSE file for details.

## Acknowledgments

Built with CoreCLR hosting APIs from the .NET Runtime project.

## Contact

For questions or issues, please open an issue on the GitHub repository.
