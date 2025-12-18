project "TestScript"
    location "%{wks.location}/TestScript"
    kind "SharedLib"
    language "C#"
    dotnetframework "net9.0"

    targetdir (OUTPUT_DIR)
    objdir (INTOOUTPUT_DIR)

    files {
        "Core/**.cs",
        "Mathf/**.cs",
        "Scene/**.cs"
    }

    filter { "action:vs* or system:windows" }
        vsprops {
            AppendTargetFrameworkToOutputPath = "false",
            Nullable = "enable",
            CopyLocalLockFileAssemblies = "true",
            EnableDynamicLoading = "true",
            ImplicitUsing = "enable"
        }
        
    filter "configurations:Debug"
        symbols "on"

    filter "configurations:Release"
        optimize "on"
        symbols "on"

    filter "configurations:Shipping"
        optimize "on"
        symbols "off"
