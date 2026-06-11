using System;
using System.Collections.Generic;

namespace StudentLeaveSystem.Models;

public partial class Class
{
    public string Cid { get; set; } = null!;

    public string Cname { get; set; } = null!;

    public string Grade { get; set; } = null!;

    public string? Tid { get; set; }

    public string Did { get; set; } = null!;

    public virtual Department DidNavigation { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual Teacher? TidNavigation { get; set; }
}
