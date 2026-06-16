using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLeaveSystem.Data;
using StudentLeaveSystem.Models;
using StudentLeaveSystem.Services;

namespace StudentLeaveSystem.Controllers
{
    [Route("api")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthenticationController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("students/login")]
        public async Task<IActionResult> StudentLogin([FromBody] LoginRequest request)
        {
            var student = await _context.Students
                .Include(s => s.CidNavigation)
                .FirstOrDefaultAsync(s => s.Sid == request.Sid);

            if (student == null || !BCrypt.Net.BCrypt.Verify(request.Password, student.Spassword))
            {
                return Unauthorized(new { message = "学号或密码错误" });
            }

            // 生成 JWT Token
            var token = _jwtService.GenerateToken(student.Sid, "student", student.Sname);

            return Ok(new
            {
                token,
                user = new
                {
                    sid = student.Sid,
                    sname = student.Sname,
                    gender = student.Gender,
                    sphone = student.Sphone,
                    role = "student",
                    classId = student.Cid,
                    className = student.CidNavigation?.Cname
                }
            });
        }

        [HttpPost("teachers/login")]
        public async Task<IActionResult> TeacherLogin([FromBody] LoginRequest request)
        {
            var teacher = await _context.Teachers
                .Include(t => t.DidNavigation)
                .FirstOrDefaultAsync(t => t.Tid == request.Tid);

            if (teacher == null || !BCrypt.Net.BCrypt.Verify(request.Password, teacher.Tpassword))
            {
                return Unauthorized(new { message = "工号或密码错误" });
            }

            // 生成 JWT Token
            var token = _jwtService.GenerateToken(teacher.Tid, teacher.Position, teacher.Tname);

            return Ok(new
            {
                token,
                user = new
                {
                    tid = teacher.Tid,
                    tname = teacher.Tname,
                    role = teacher.Position,
                    department = teacher.DidNavigation != null ? new
                    {
                        did = teacher.DidNavigation.Did,
                        dname = teacher.DidNavigation.Dname
                    } : null
                }
            });
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request)
        {
            var admin = await _context.Teachers
                .Include(t => t.DidNavigation)
                .FirstOrDefaultAsync(t => t.Tid == request.Tid && t.Position == "管理员");

            if (admin == null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.Tpassword))
            {
                return Unauthorized(new { message = "账号或密码错误，或非管理员权限" });
            }

            // 生成 JWT Token
            var token = _jwtService.GenerateToken(admin.Tid, "管理员", admin.Tname);

            return Ok(new
            {
                token,
                user = new
                {
                    tid = admin.Tid,
                    tname = admin.Tname,
                    role = "管理员",
                    department = admin.DidNavigation != null ? new
                    {
                        did = admin.DidNavigation.Did,
                        dname = admin.DidNavigation.Dname
                    } : null
                }
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // JWT 无需服务端注销，客户端删除 Token 即可
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