using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Quartz.Utils;

/// <summary>
/// Provides access to the currently executing job.
/// </summary>
public static class JobContext
{
    private static readonly AsyncLocal<SysJobDto?> _current = new();

    /// <summary>
    /// Gets or sets the job currently being executed.
    /// </summary>
    public static SysJobDto? CurrentJob
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}