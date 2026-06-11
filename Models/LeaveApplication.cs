using System;
using System.Collections.Generic;

namespace StudentLeaveSystem.Models;

public partial class LeaveApplication
{
    public string LeaveId { get; set; } = null!;

    public DateTime EndTime { get; set; }

    public DateTime? FirstApprovalTime { get; set; }

    public string? FirstComment { get; set; }

    public string FirstResult { get; set; } = null!;

    public string LeaveType { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public DateTime? SecondApprovalTime { get; set; }

    public string? SecondComment { get; set; }

    public string? SecondResult { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime SubmitTime { get; set; }

    public string FirstTid { get; set; } = null!;

    public string? SecondTid { get; set; }

    public string Sid { get; set; } = null!;

    public virtual Teacher FirstT { get; set; } = null!;

    public virtual Teacher? SecondT { get; set; }

    public virtual Student SidNavigation { get; set; } = null!;
}
