using System;
using HopFence.Core.Http;
using HopFence.Core.Models;

namespace HopFence.Core.Engine;

/// <summary>
/// Execution context supplied to all diagnostic test runners.
/// </summary>
public sealed class DiagnosticContext
{
    public HopFenceConfig Config { get; }
    public IProbeClient Client { get; }
    public ProbeResponse? BaselineProbe { get; set; }
    public Action<string>? LogVerbose { get; }

    public DiagnosticContext(HopFenceConfig config, IProbeClient client, Action<string>? logVerbose = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Client = client ?? throw new ArgumentNullException(nameof(client));
        LogVerbose = logVerbose;
    }

    public void Verbose(string message)
    {
        LogVerbose?.Invoke(message);
    }
}
