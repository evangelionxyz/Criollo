project "Example.Native"
    location "%{wks.location}/Example/Native"
    kind "ConsoleApp"
    language "C++"
    cppdialect "c++23"
    architecture "x64"

    targetdir (OUTPUT_DIR)
    objdir (INTOUTPUT_DIR)

    files {
        "Source/**.cpp",
        "Source/**.h"
    }

    includedirs {
        "%{wks.location}/MochiSharp.Native/Source",
        "%{IncludeDirs.Hostfxr}"
    }

    libdirs {
        "%{IncludeDirs.Hostfxr}"
    }

    links {
        "MochiSharp.Native",
        "%{THIRDPARTY_DIR}/dotnet/host/fxr/9.0.11/x64/nethost.lib"
    }

    postbuildcommands {
        "{COPY} \"%{THIRDPARTY_DIR}/dotnet/host/fxr/9.0.11/x64/nethost.dll\" \"%{cfg.targetdir}\"",
        "{COPY} \"%{THIRDPARTY_DIR}/dotnet/host/fxr/9.0.11/x64/hostfxr.dll\" \"%{cfg.targetdir}\""
    }

    filter "system:windows"
        systemversion "latest"
        buildoptions { "/utf-8" }
        defines {
            "_WINDOWS",
            "WIN32",
            "WIN32_LEAN_AND_MEAN",
            "_CRT_SECURE_NO_WARNINGS",
            "_CONSOLE"
        }

    filter "configurations:Debug"
        runtime "Debug"
        optimize "off"
        symbols "on"
        defines { "_DEBUG" }

    filter "configurations:Release"
        runtime "Release"
        optimize "speed"
        symbols "off"
        defines { "NDEBUG" }
