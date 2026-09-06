using System.Reflection;
using System.Runtime.InteropServices;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;

internal static unsafe class PhiNative
{
    public const uint AbiVersion = 1;
    private const string LibraryName = "phi_recorder";

    static PhiNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(PhiNative).Assembly, Resolve);
    }

    private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != LibraryName)
        {
            return nint.Zero;
        }

        var candidate = Path.Combine(AppContext.BaseDirectory, "native", PhiRecorderRuntimeLibraryName);
        return File.Exists(candidate) ? NativeLibrary.Load(candidate) : nint.Zero;
    }

    private static string PhiRecorderRuntimeLibraryName =>
        OperatingSystem.IsWindows() ? "phi_recorder.dll" : "libphi_recorder.so";

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint phi_abi_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_context_create(PhiContextOptions* options, out nint outContext);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void phi_context_destroy(nint context);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_context_clear_error(nint context);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_context_get_last_error(nint context, byte* buffer, nuint capacity, out nuint required);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_render_config_init_default(PhiRenderConfigStruct* config);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_render_config_validate(PhiRenderConfigStruct* config);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_chart_info_load(nint context, PhiStringView chartPath, out nint outInfo);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void phi_chart_info_destroy(nint info);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_chart_info_get_view(nint info, PhiChartInfoView* outView);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_chart_info_set_view(nint info, PhiChartInfoView* view);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_render_submit(
        nint context,
        PhiRenderRequestStruct* request,
        PhiJobCallbackFn? callback,
        nint userData,
        out nint outJob);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_job_get_snapshot(nint job, PhiJobSnapshotStruct* outSnapshot);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_job_cancel(nint job);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_job_pause(nint job);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PhiStatus phi_job_resume(nint job);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void phi_job_destroy(nint job);
}
