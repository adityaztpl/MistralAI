// Teaching sketch for 16-capstone-instagram.
// Demonstrates a background worker that executes the existing Python
// instagram_content_creator CLI without running long AI work in controllers.

using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InstagramCapstone.Api.ContentRuns;

public sealed class CrewRunnerOptions
{
    public string PythonExecutable { get; init; } = "python";
    public string OutputRoot { get; init; } = "outputs/runs";
    public required string MistralApiKey { get; init; }
    public TimeSpan RunTimeout { get; init; } = TimeSpan.FromMinutes(10);
    public int MaxCapturedLogChars { get; init; } = 20_000;
}

public interface IContentRunQueueReader
{
    ValueTask<string> DequeueAsync(CancellationToken cancellationToken);
}

public interface IEvalScoreService
{
    Task ScoreRunAsync(string runId, CancellationToken cancellationToken);
}

public sealed class RunJobService(
    IServiceScopeFactory scopeFactory,
    IContentRunQueueReader queue,
    IOptions<CrewRunnerOptions> options,
    ILogger<RunJobService> logger) : BackgroundService
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedArtifacts =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["market_research"] = "market_research.md",
            ["content_strategy"] = "content_strategy.md",
            ["visual_content"] = "visual_content.md",
            ["captions"] = "captions.md",
            ["final_pack"] = "final_content_pack.md"
        };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var runId = await queue.DequeueAsync(stoppingToken);

            try
            {
                await ProcessRunAsync(runId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled worker error for run {RunId}", runId);
            }
        }
    }

    private async Task ProcessRunAsync(string runId, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eval = scope.ServiceProvider.GetRequiredService<IEvalScoreService>();
        var runnerOptions = options.Value;

        var run = await db.ContentRuns.SingleOrDefaultAsync(item => item.Id == runId, stoppingToken);
        if (run is null)
        {
            logger.LogWarning("Dequeued missing content run {RunId}", runId);
            return;
        }

        if (run.Status != ContentRunStatus.Queued)
        {
            logger.LogInformation(
                "Skipping run {RunId} because status is {Status}",
                run.Id,
                run.Status);
            return;
        }

        run.Status = ContentRunStatus.Running;
        run.StartedAt = DateTimeOffset.UtcNow;
        run.OutputDirectory = BuildOutputDirectory(runnerOptions.OutputRoot, run.TenantId, run.Id);
        await db.SaveChangesAsync(stoppingToken);

        Directory.CreateDirectory(run.OutputDirectory);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(runnerOptions.RunTimeout);

        var result = await RunCrewProcessAsync(run, runnerOptions, timeoutCts.Token);
        if (result.ExitCode != 0)
        {
            run.Status = ContentRunStatus.Failed;
            run.ErrorCode = result.TimedOut ? "TimedOut" : "CrewFailed";
            run.ErrorMessage = BuildSafeErrorMessage(result);
            run.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(stoppingToken);
            return;
        }

        var artifacts = DiscoverArtifacts(run);
        if (artifacts.Count == 0)
        {
            run.Status = ContentRunStatus.Failed;
            run.ErrorCode = "ArtifactMissing";
            run.ErrorMessage = "The crew completed but no expected artifacts were found.";
            run.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(stoppingToken);
            return;
        }

        db.RunArtifacts.AddRange(artifacts);
        run.Status = ContentRunStatus.Succeeded;
        run.CompletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(stoppingToken);

        await eval.ScoreRunAsync(run.Id, stoppingToken);
    }

    private static string BuildOutputDirectory(string root, string tenantId, string runId)
    {
        // Tenant and run ids should be server-generated/validated identifiers.
        // User input must never become a path segment.
        return Path.Combine(root, tenantId, runId);
    }

    private static async Task<ProcessResult> RunCrewProcessAsync(
        ContentRun run,
        CrewRunnerOptions options,
        CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = options.PythonExecutable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-m");
        startInfo.ArgumentList.Add("instagram_content_creator.main");
        startInfo.ArgumentList.Add("--description");
        startInfo.ArgumentList.Add(run.BrandDescription);
        startInfo.ArgumentList.Add("--topic");
        startInfo.ArgumentList.Add(BuildTopic(run));
        startInfo.ArgumentList.Add("--posts");
        startInfo.ArgumentList.Add(run.NumberOfPosts.ToString());
        startInfo.ArgumentList.Add("--model");
        startInfo.ArgumentList.Add($"mistral/{run.Model}");
        startInfo.ArgumentList.Add("--output-dir");
        startInfo.ArgumentList.Add(run.OutputDirectory!);

        // Pass secrets through environment, not command-line arguments.
        startInfo.Environment["MISTRAL_API_KEY"] = options.MistralApiKey;
        startInfo.Environment["CREWAI_TRACING_ENABLED"] = "false";

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null && stdout.Length < options.MaxCapturedLogChars)
            {
                stdout.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null && stderr.Length < options.MaxCapturedLogChars)
            {
                stderr.AppendLine(args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }

        return new ProcessResult(
            process.HasExited ? process.ExitCode : -1,
            timedOut,
            Redact(stdout.ToString()),
            Redact(stderr.ToString()));
    }

    private static string BuildTopic(ContentRun run)
    {
        var parts = new List<string> { run.Topic };

        if (!string.IsNullOrWhiteSpace(run.Audience))
        {
            parts.Add($"Audience: {run.Audience}");
        }

        if (!string.IsNullOrWhiteSpace(run.Tone))
        {
            parts.Add($"Tone: {run.Tone}");
        }

        return string.Join("\n", parts);
    }

    private static List<RunArtifact> DiscoverArtifacts(ContentRun run)
    {
        var artifacts = new List<RunArtifact>();
        foreach (var (kind, fileName) in ExpectedArtifacts)
        {
            var path = Path.Combine(run.OutputDirectory!, fileName);
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                continue;
            }

            artifacts.Add(new RunArtifact
            {
                ContentRunId = run.Id,
                Kind = kind,
                Path = path,
                ContentType = "text/markdown",
                SizeBytes = info.Length,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return artifacts;
    }

    private static string BuildSafeErrorMessage(ProcessResult result)
    {
        if (result.TimedOut)
        {
            return "The content run exceeded its maximum duration.";
        }

        var stderr = result.StandardError.Trim();
        if (string.IsNullOrWhiteSpace(stderr))
        {
            return "The content run failed without a detailed error.";
        }

        return stderr.Length <= 500 ? stderr : stderr[..500];
    }

    private static string Redact(string value)
    {
        // Add provider-key, JWT, and connection-string redaction here.
        return value.Replace("MISTRAL_API_KEY", "MISTRAL_API_KEY_REDACTED", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ProcessResult(
        int ExitCode,
        bool TimedOut,
        string StandardOutput,
        string StandardError);
}

