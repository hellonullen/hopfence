namespace HopFence.Core.Models;

/// <summary>
/// Status of a HopFence diagnostic test execution.
/// </summary>
public enum TestStatus
{
    /// <summary>The test passed all validation checks.</summary>
    Pass,

    /// <summary>The test failed a critical validation or identified a security misconfiguration.</summary>
    Fail,

    /// <summary>A potential issue or suboptimal configuration was identified.</summary>
    Warn,

    /// <summary>The test could not decisively confirm pass or fail due to insufficient target data.</summary>
    Inconclusive,

    /// <summary>The test was skipped because prerequisites or configuration flags were omitted.</summary>
    Skip
}

/// <summary>
/// Severity level associated with a diagnostic finding.
/// </summary>
public enum TestSeverity
{
    /// <summary>Informational note or benchmark.</summary>
    Info,

    /// <summary>Low severity; best-practice suggestion.</summary>
    Low,

    /// <summary>Medium severity; possible spoofing or routing ambiguity.</summary>
    Medium,

    /// <summary>High severity; direct header injection, spoofing, or protocol downgrade vulnerability.</summary>
    High,

    /// <summary>Critical vulnerability needing immediate remediation.</summary>
    Critical
}
