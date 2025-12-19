project "MochiSharp.Managed"
    location "%{wks.location}/MochiSharp.Managed"
    kind "SharedLib"
    language "C#"
    dotnetframework "net9.0"

    -- Don't specify architecture here. (see https://github.com/premake/premake-core/issues/1758)

    targetdir (OUTPUT_DIR)
    objdir (INTOUTPUT_DIR)

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
        symbols "off"