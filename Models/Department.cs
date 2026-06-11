using System;
using System.Collections.Generic;

namespace StudentLeaveSystem.Models;

public partial class Department
{
    public string Did { get; set; } = null!;

    public string? Dhead { get; set; }

    public string Dname { get; set; } = null!;

    public string? Dphone { get; set; }

    public string? Dplace { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
}
