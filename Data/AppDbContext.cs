using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using StudentLeaveSystem.Models;

namespace StudentLeaveSystem.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<LeaveApplication> LeaveApplications { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.Cid).HasName("PRIMARY");

            entity.ToTable("class");

            entity.HasIndex(e => e.Did, "fk_class_department");

            entity.HasIndex(e => e.Tid, "fk_class_teacher");

            entity.Property(e => e.Cid)
                .HasMaxLength(10)
                .HasColumnName("cid");
            entity.Property(e => e.Cname)
                .HasMaxLength(20)
                .HasColumnName("cname");
            entity.Property(e => e.Did)
                .HasMaxLength(10)
                .HasColumnName("did");
            entity.Property(e => e.Grade)
                .HasMaxLength(4)
                .HasColumnName("grade");
            entity.Property(e => e.Tid)
                .HasMaxLength(10)
                .HasColumnName("tid");

            entity.HasOne(d => d.DidNavigation).WithMany(p => p.Classes)
                .HasForeignKey(d => d.Did)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_class_department");

            entity.HasOne(d => d.TidNavigation).WithMany(p => p.Classes)
                .HasForeignKey(d => d.Tid)
                .HasConstraintName("fk_class_teacher");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Did).HasName("PRIMARY");

            entity.ToTable("department");

            entity.Property(e => e.Did)
                .HasMaxLength(10)
                .HasColumnName("did");
            entity.Property(e => e.Dhead)
                .HasMaxLength(10)
                .HasColumnName("dhead");
            entity.Property(e => e.Dname)
                .HasMaxLength(20)
                .HasColumnName("dname");
            entity.Property(e => e.Dphone)
                .HasMaxLength(11)
                .HasColumnName("dphone");
            entity.Property(e => e.Dplace)
                .HasMaxLength(50)
                .HasColumnName("dplace");
        });

        modelBuilder.Entity<LeaveApplication>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PRIMARY");

            entity.ToTable("leave_application");

            entity.HasIndex(e => e.FirstTid, "fk_leave_first_teacher");

            entity.HasIndex(e => e.SecondTid, "fk_leave_second_teacher");

            entity.HasIndex(e => e.Sid, "fk_leave_student");

            entity.Property(e => e.LeaveId)
                .HasMaxLength(14)
                .HasColumnName("leave_id");
            entity.Property(e => e.EndTime)
                .HasMaxLength(6)
                .HasColumnName("end_time");
            entity.Property(e => e.FirstApprovalTime)
                .HasMaxLength(6)
                .HasColumnName("first_approval_time");
            entity.Property(e => e.FirstComment)
                .HasColumnType("text")
                .HasColumnName("first_comment");
            entity.Property(e => e.FirstResult)
                .HasMaxLength(10)
                .HasColumnName("first_result");
            entity.Property(e => e.FirstTid)
                .HasMaxLength(10)
                .HasColumnName("first_tid");
            entity.Property(e => e.LeaveType)
                .HasMaxLength(255)
                .HasColumnName("leave_type");
            entity.Property(e => e.Reason)
                .HasColumnType("text")
                .HasColumnName("reason");
            entity.Property(e => e.SecondApprovalTime)
                .HasMaxLength(6)
                .HasColumnName("second_approval_time");
            entity.Property(e => e.SecondComment)
                .HasColumnType("text")
                .HasColumnName("second_comment");
            entity.Property(e => e.SecondResult)
                .HasMaxLength(10)
                .HasColumnName("second_result");
            entity.Property(e => e.SecondTid)
                .HasMaxLength(10)
                .HasColumnName("second_tid");
            entity.Property(e => e.Sid)
                .HasMaxLength(10)
                .HasColumnName("sid");
            entity.Property(e => e.StartTime)
                .HasMaxLength(6)
                .HasColumnName("start_time");
            entity.Property(e => e.SubmitTime)
                .HasMaxLength(6)
                .HasColumnName("submit_time");

            entity.HasOne(d => d.FirstT).WithMany(p => p.LeaveApplicationFirstTs)
                .HasForeignKey(d => d.FirstTid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_leave_first_teacher");

            entity.HasOne(d => d.SecondT).WithMany(p => p.LeaveApplicationSecondTs)
                .HasForeignKey(d => d.SecondTid)
                .HasConstraintName("fk_leave_second_teacher");

            entity.HasOne(d => d.SidNavigation).WithMany(p => p.LeaveApplications)
                .HasForeignKey(d => d.Sid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_leave_student");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Sid).HasName("PRIMARY");

            entity.ToTable("student");

            entity.HasIndex(e => e.Cid, "fk_student_class");

            entity.Property(e => e.Sid)
                .HasMaxLength(10)
                .HasColumnName("sid");
            entity.Property(e => e.Cid)
                .HasMaxLength(10)
                .HasColumnName("cid");
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .HasColumnName("gender");
            entity.Property(e => e.Sname)
                .HasMaxLength(10)
                .HasColumnName("sname");
            entity.Property(e => e.Spassword)
                .HasMaxLength(100)
                .HasColumnName("spassword");
            entity.Property(e => e.Sphone)
                .HasMaxLength(11)
                .HasColumnName("sphone");

            entity.HasOne(d => d.CidNavigation).WithMany(p => p.Students)
                .HasForeignKey(d => d.Cid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_student_class");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Tid).HasName("PRIMARY");

            entity.ToTable("teacher");

            entity.HasIndex(e => e.Did, "fk_teacher_department");

            entity.Property(e => e.Tid)
                .HasMaxLength(10)
                .HasColumnName("tid");
            entity.Property(e => e.Did)
                .HasMaxLength(10)
                .HasColumnName("did");
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .HasColumnName("gender");
            entity.Property(e => e.Position)
                .HasMaxLength(20)
                .HasColumnName("position");
            entity.Property(e => e.Tname)
                .HasMaxLength(10)
                .HasColumnName("tname");
            entity.Property(e => e.Tpassword)
                .HasMaxLength(100)
                .HasColumnName("tpassword");
            entity.Property(e => e.Tphone)
                .HasMaxLength(11)
                .HasColumnName("tphone");

            entity.HasOne(d => d.DidNavigation).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.Did)
                .HasConstraintName("fk_teacher_department");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
