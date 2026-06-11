using System;
using System.Collections.Generic;

namespace StudentLeaveSystem.Models;

public partial class Teacher
{
    public string Tid { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string Position { get; set; } = null!;

    public string Tname { get; set; } = null!;

    public string Tpassword { get; set; } = null!;

    public string Tphone { get; set; } = null!;

    public string? Did { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual Department? DidNavigation { get; set; }

    public virtual ICollection<LeaveApplication> LeaveApplicationFirstTs { get; set; } = new List<LeaveApplication>();

    public virtual ICollection<LeaveApplication> LeaveApplicationSecondTs { get; set; } = new List<LeaveApplication>();
}
