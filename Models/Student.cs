using System;
using System.Collections.Generic;

namespace StudentLeaveSystem.Models;

public partial class Student
{
    public string Sid { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string Sname { get; set; } = null!;

    public string Spassword { get; set; } = null!;

    public string Sphone { get; set; } = null!;

    public string Cid { get; set; } = null!;

    public virtual Class CidNavigation { get; set; } = null!;

    public virtual ICollection<LeaveApplication> LeaveApplications { get; set; } = new List<LeaveApplication>();
}
