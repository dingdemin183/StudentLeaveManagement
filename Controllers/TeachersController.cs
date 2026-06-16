using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLeaveSystem.Data;
using StudentLeaveSystem.Models;

namespace StudentLeaveSystem.Controllers
{
    [Route("api/teachers")]
    [ApiController]
    public class TeachersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeachersController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "管理员")]
        [HttpPost]
        public async Task<IActionResult> CreateTeacher([FromBody] Teacher teacher)
        {
            teacher.Tpassword = BCrypt.Net.BCrypt.HashPassword(teacher.Tpassword);
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTeacherProfile), new { tid = teacher.Tid }, teacher);
        }

        [HttpGet("position/{position}")]
        public async Task<IActionResult> GetTeachersByPosition(string position)
        {
            var teachers = await _context.Teachers
                .Where(t => t.Position == position)
                .ToListAsync();
            return Ok(teachers);
        }

        [HttpGet("department/{departmentId}")]
        public async Task<IActionResult> GetTeachersByDepartment(string departmentId)
        {
            var teachers = await _context.Teachers
                .Where(t => t.Did == departmentId)
                .ToListAsync();
            return Ok(teachers);
        }

        [HttpGet("{tid}/profile")]
        public async Task<IActionResult> GetTeacherProfile(string tid)
        {
            var teacher = await _context.Teachers
                .Include(t => t.DidNavigation)
                .FirstOrDefaultAsync(t => t.Tid == tid);

            if (teacher == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                tid = teacher.Tid,
                tname = teacher.Tname,
                tphone = teacher.Tphone,
                position = teacher.Position,
                department = teacher.DidNavigation != null ? new
                {
                    did = teacher.DidNavigation.Did,
                    dname = teacher.DidNavigation.Dname
                } : null
            });
        }

        [HttpPut("{tid}/profile")]
        public async Task<IActionResult> UpdateTeacherProfile(string tid, [FromBody] TeacherProfileUpdate update)
        {
            var teacher = await _context.Teachers.FindAsync(tid);
            if (teacher == null)
            {
                return NotFound();
            }

            // 更新姓名
            if (!string.IsNullOrEmpty(update.Tname))
            {
                teacher.Tname = update.Tname;
            }

            // 更新电话
            if (!string.IsNullOrEmpty(update.Tphone))
            {
                teacher.Tphone = update.Tphone;
            }

            // 如果需要修改密码
            if (!string.IsNullOrEmpty(update.NewPassword))
            {
                // 验证旧密码
                if (string.IsNullOrEmpty(update.OldPassword))
                {
                    return BadRequest(new { message = "请输入旧密码" });
                }

                if (!BCrypt.Net.BCrypt.Verify(update.OldPassword, teacher.Tpassword))
                {
                    return BadRequest(new { message = "旧密码不正确" });
                }

                // 更新密码
                teacher.Tpassword = BCrypt.Net.BCrypt.HashPassword(update.NewPassword);
            }

            await _context.SaveChangesAsync();
            return Ok(teacher);
        }

        [HttpPut("{tid}/password")]
        public async Task<IActionResult> UpdateTeacherPassword(string tid, [FromBody] PasswordUpdate update)
        {
            var teacher = await _context.Teachers.FindAsync(tid);
            if (teacher == null)
            {
                return NotFound();
            }

            teacher.Tpassword = BCrypt.Net.BCrypt.HashPassword(update.NewPassword);
            await _context.SaveChangesAsync();
            return Ok(new { message = "密码修改成功" });
        }

        [HttpGet("{tid}/class-students")]
        public async Task<IActionResult> GetClassStudents(string tid)
        {
            var classes = await _context.Classes
                .Include(c => c.DidNavigation)
                .Where(c => c.Tid == tid)
                .ToListAsync();

            var classIds = classes.Select(c => c.Cid).ToList();
            var students = await _context.Students
                .Where(s => classIds.Contains(s.Cid))
                .Include(s => s.CidNavigation)
                .ThenInclude(c => c.DidNavigation)
                .ToListAsync();

            var result = students.Select(student => new
            {
                sid = student.Sid,
                sname = student.Sname,
                gender = student.Gender,
                sphone = student.Sphone,
                cid = student.Cid,
                studentClass = student.CidNavigation != null ? new
                {
                    cid = student.CidNavigation.Cid,
                    cname = student.CidNavigation.Cname,
                    grade = student.CidNavigation.Grade,
                    department = student.CidNavigation.DidNavigation != null ? new
                    {
                        did = student.CidNavigation.DidNavigation.Did,
                        dname = student.CidNavigation.DidNavigation.Dname
                    } : null
                } : null
            });

            return Ok(result);
        }

        public class TeacherProfileUpdate
        {
            public string? Tphone { get; set; }
            public string? Tname { get; set; }
            public string? OldPassword { get; set; }
            public string? NewPassword { get; set; }
        }

        public class PasswordUpdate
        {
            public string NewPassword { get; set; } = string.Empty;
        }
    }
}
