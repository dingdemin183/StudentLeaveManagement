using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLeaveSystem.Data;
using StudentLeaveSystem.Models;

namespace StudentLeaveSystem.Controllers
{
    [Route("api")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthenticationController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("students/login")]
        public async Task<IActionResult> StudentLogin([FromBody] LoginRequest request)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Sid == request.Sid);

            // 原加密验证（数据库密码加密时使用）
            // if (student == null || !BCrypt.Net.BCrypt.Verify(request.Password, student.Spassword))
            // {
            //     return Unauthorized(new { message = "学号或密码错误" });
            // }

            // 临时明文验证（数据库密码未加密时使用）
            if (student == null || student.Spassword != request.Password)
            {
                return Unauthorized(new { message = "学号或密码错误" });
            }

            HttpContext.Session.SetString("UserId", student.Sid);
            HttpContext.Session.SetString("Role", "student");

            return Ok(new
            {
                sid = student.Sid,
                sname = student.Sname,
                role = "student",
                classId = student.Cid
            });
        }

        [HttpPost("teachers/login")]
        public async Task<IActionResult> TeacherLogin([FromBody] LoginRequest request)
        {
            var teacher = await _context.Teachers
                .Include(t => t.DidNavigation)
                .FirstOrDefaultAsync(t => t.Tid == request.Tid);

            // 原加密验证（数据库密码加密时使用）
            // if (teacher == null || !BCrypt.Net.BCrypt.Verify(password, teacher.Tpassword))
            // {
            //     return Unauthorized(new { message = "工号或密码错误" });
            // }

            // 临时明文验证（数据库密码未加密时使用）
            if (teacher == null || teacher.Tpassword != request.Password)
            {
                return Unauthorized(new { message = "工号或密码错误" });
            }

            HttpContext.Session.SetString("UserId", teacher.Tid);
            HttpContext.Session.SetString("Role", teacher.Position);

            return Ok(new
            {
                tid = teacher.Tid,
                tname = teacher.Tname,
                role = teacher.Position,
                department = teacher.DidNavigation != null ? new
                {
                    did = teacher.DidNavigation.Did,
                    dname = teacher.DidNavigation.Dname
                } : null
            });
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request)
        {
            var admin = await _context.Teachers
                .Include(t => t.DidNavigation)
                .FirstOrDefaultAsync(t => t.Tid == request.Tid && t.Position == "管理员");

            // 原加密验证（数据库密码加密时使用）
            // if (admin == null || !BCrypt.Net.BCrypt.Verify(password, admin.Tpassword))
            // {
            //     return Unauthorized(new { message = "账号或密码错误，或非管理员权限" });
            // }

            // 临时明文验证（数据库密码未加密时使用）
            if (admin == null || admin.Tpassword != request.Password)
            {
                return Unauthorized(new { message = "账号或密码错误，或非管理员权限" });
            }

            HttpContext.Session.SetString("UserId", admin.Tid);
            HttpContext.Session.SetString("Role", "管理员");

            return Ok(new
            {
                tid = admin.Tid,
                tname = admin.Tname,
                role = "管理员",
                department = admin.DidNavigation != null ? new
                {
                    did = admin.DidNavigation.Did,
                    dname = admin.DidNavigation.Dname
                } : null
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "退出成功" });
        }

        public class LoginRequest
        {
            public string Sid { get; set; } = string.Empty;
            public string Tid { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
