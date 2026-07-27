using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AoE1Control.Memory;
using AoE1Control.Native;
using AoE1Control.Profiles;
using AoE1Control.Utilities;
using Microsoft.Win32.SafeHandles;

namespace AoE1Control.Internal;

internal sealed class GameConnection : IDisposable
{
    private readonly Process _process;
    private bool _disposed;

    private GameConnection(
        Process process,
        SafeProcessHandle handle,
        ProcessMemoryReader memory,
        nint moduleBase,
        GameProfile profile,
        GameVersionInfo gameVersion)
    {
        _process = process;
        Handle = handle;
        Memory = memory;
        ModuleBase = moduleBase;
        Profile = profile;
        GameVersion = gameVersion;
    }

    internal SafeProcessHandle Handle { get; }
    internal ProcessMemoryReader Memory { get; }
    internal nint ModuleBase { get; }
    internal GameProfile Profile { get; }
    internal GameVersionInfo GameVersion { get; }

    internal bool IsConnected
    {
        get
        {
            if (_disposed)
                return false;

            try
            {
                _process.Refresh();
                return !_process.HasExited && !Handle.IsInvalid && !Handle.IsClosed;
            }
            catch
            {
                return false;
            }
        }
    }

    internal static GameConnection Connect(
        AoE1ControlOptions options,
        IReadOnlyList<GameProfile> profiles)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "AoE1Control requer Windows.");

        Process[] processes =
            Process.GetProcessesByName(options.ProcessName)
                .OrderBy(p => p.Id)
                .ToArray();

        if (processes.Length == 0)
            throw new GameProcessNotFoundException(
                $"O processo {options.ProcessName}.EXE não foi encontrado.");

        if (processes.Length > 1)
        {
            foreach (Process extra in processes)
                extra.Dispose();

            throw new MultipleGameProcessesException(
                $"Foram encontrados {processes.Length} processos {options.ProcessName}.EXE.");
        }

        Process process = processes[0];

        try
        {
            process.Refresh();

            if (process.HasExited)
                throw new GameProcessExitedException(
                    "O processo foi encerrado antes da conexão.");

            ProcessModule? module = process.MainModule;
            if (module is null)
                throw new AoE1ControlException(
                    "Não foi possível obter o módulo principal.");

            if (!ArchitectureUtility.IsWow64X86(process.Handle))
                throw new AoE1ControlException(
                    "O processo encontrado não é x86.");

            string executablePath = module.FileName;
            string sha256 = HashUtility.ComputeSha256(executablePath);

            GameProfile? profile =
                profiles.FirstOrDefault(p =>
                    string.Equals(
                        p.Executable.Sha256,
                        sha256,
                        StringComparison.OrdinalIgnoreCase));

            if (profile is null)
            {
                if (options.RequireValidatedProfile)
                    throw new UnsupportedGameVersionException(
                        $"SHA-256 não cadastrado: {sha256}");

                profile = profiles.FirstOrDefault()
                    ?? throw new UnsupportedGameVersionException(
                        "Nenhum perfil está disponível.");
            }

            SafeProcessHandle handle = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_VM_READ |
                NativeMethods.PROCESS_QUERY_INFORMATION,
                false,
                process.Id);

            if (handle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                throw new AoE1ControlException(
                    $"OpenProcess falhou. Win32={error}",
                    new Win32Exception(error));
            }

            ProcessMemoryReader memory = new(handle);

            GameVersionInfo version = new(
                profile.ProfileId,
                profile.Game,
                profile.Edition,
                profile.Executable.Name,
                sha256);

            return new GameConnection(
                process,
                handle,
                memory,
                module.BaseAddress,
                profile,
                version);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    internal void Refresh()
    {
        if (!IsConnected)
            throw new GameProcessExitedException(
                "O processo do jogo não está mais conectado.");

        _process.Refresh();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Handle.Dispose();
        _process.Dispose();
        _disposed = true;
    }
}
