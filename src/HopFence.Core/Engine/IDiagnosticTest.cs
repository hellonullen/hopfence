using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Models;

namespace HopFence.Core.Engine;

/// <summary>
/// Contract for an individual diagnostic test runner.
/// </summary>
public interface IDiagnosticTest
{
    /// <summary>Unique identifier for the test (e.g. "HOP-HTTPS-001").</summary>
    string Id { get; }

    /// <summary>Human-readable name of the test.</summary>
    string Name { get; }

    /// <summary>Summary description of the check.</summary>
    string Description { get; }

    /// <summary>Whether this test is enabled based on user configuration.</summary>
    bool IsEnabled(HopFenceConfig config);

    /// <summary>Executes the test and returns the diagnostic result.</summary>
    Task<TestResult> ExecuteAsync(DiagnosticContext context, CancellationToken cancellationToken);
}
