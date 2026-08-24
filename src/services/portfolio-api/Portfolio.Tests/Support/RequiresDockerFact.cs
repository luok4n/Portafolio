using System.Diagnostics;

namespace Portfolio.Tests.Support;

/// <summary>
/// A test that needs a container engine, skipped rather than failed when there is none.
/// </summary>
/// <remarks>
/// CI always has Docker, so these always run there. On a developer machine without it, failing the
/// whole suite would teach people to stop running <c>dotnet test</c> — and a suite nobody runs
/// catches nothing. Skipping says plainly what was not checked instead.
/// </remarks>
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!DockerProbe.IsAvailable)
        {
            Skip = "Docker is not available on this machine.";
        }
    }
}

internal static class DockerProbe
{
    private static readonly Lazy<bool> Available = new(Probe, isThreadSafe: true);

    public static bool IsAvailable => Available.Value;

    private static bool Probe()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("docker", "info --format {{.ServerVersion}}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            // A stopped engine leaves the CLI hanging on the socket rather than returning, so the
            // probe is bounded — a test run must not stall for minutes deciding whether to skip.
            if (!process.WaitForExit(20_000))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
