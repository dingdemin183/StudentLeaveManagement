using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLeaveSystem.Data;
using StudentLeaveSystem.Models;

namespace StudentLeaveSystem.Controllers
{
    [Route("api/students")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StudentsController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "管理员")]
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] StudentCreateRequest request)
        {
            // 不加密密码，保持明文（与Java后端一致）
            var student = new Student
            {
                Sid = request.sid,
                Sname = request.sname,
                Gender = request.gender,
                Sphone = request.sphone,
                Spassword = request.spassword,
                Cid = request.studentClass?.cid ?? string.Empty
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return Ok(student);
        }

        public class StudentCreateRequest
        {
            public string sid { get; set; } = string.Empty;
            public string sname { get; set; } = string.Empty;
            public string gender { get; set; } = string.Empty;
            public string sphone { get; set; } = string.Empty;
            public string spassword { get; set; } = string.Empty;
            public StudentClassInfo? studentClass { get; set; }
        }

        public class StudentClassInfo
        {
            public string cid { get; set; } = string.Empty;
        }

        [HttpPost("batch")]
        public async Task<IActionResult> BatchCreateStudents([FromBody] List<StudentBatchCreateRequest> requests)
        {
            // 批量创建学生（与Java后端一致）
            var students = requests.Select(r => new Student
            {
                Sid = r.sid,
                Sname = r.sname,
                Gender = r.gender,
                Sphone = r.sphone,
                Spassword = r.spassword,
                Cid = r.cid ?? r.studentClass?.cid ?? string.Empty
            }).ToList();

            _context.Students.AddRange(students);
            await _context.SaveChangesAsync();
            return Ok(students);
        }

        public class StudentBatchCreateRequest
        {
            public string sid { get; set; } = string.Empty;
            public string sname { get; set; } = string.Empty;
            public string gender { get; set; } = string.Empty;
            public string sphone { get; set; } = string.Empty;
            public string spassword { get; set; } = string.Empty;
            public string? cid { get; set; }
            public StudentClassInfo? studentClass { get; set; }
        }

        [HttpGet("{sid}")]
        public async Task<IActionResult> GetStudentById(string sid)
        {
            var student = await _context.Students.FindAsync(sid);
            if (student == null)
            {
                return NotFound();
            }
            return Ok(student);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _context.Students.ToListAsync();
            return Ok(students);
        }

        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetStudentsByClass(string classId)
        {
            var students = await _context.Students
                .Include(s => s.CidNavigation)
                .Where(s => s.Cid == classId)
                .ToListAsync();

            var result = students.Select(s => new
            {
                sid = s.Sid,
                sname = s.Sname,
                gender = s.Gender,
                sphone = s.Sphone,
                spassword = s.Spassword,
                cid = s.Cid,
                className = s.CidNavigation?.Cname ?? string.Empty,
                studentClass = s.CidNavigation != null ? new
                {
                    cid = s.CidNavigation.Cid,
                    cname = s.CidNavigation.Cname,
                    grade = s.CidNavigation.Grade
                } : null
            });

            return Ok(result);
        }

        [HttpGet("department/{departmentId}")]
        public async Task<IActionResult> GetStudentsByDepartment(string departmentId)
        {
            var students = await _context.Students
                .Include(s => s.CidNavigation)
                .Where(s => s.CidNavigation.Did == departmentId)
                .ToListAsync();

            var result = students.Select(s => new
            {
                sid = s.Sid,
                sname = s.Sname,
                gender = s.Gender,
                sphone = s.Sphone,
                spassword = s.Spassword,
                cid = s.Cid,
                className = s.CidNavigation?.Cname ?? string.Empty,
                studentClass = s.CidNavigation != null ? new
                {
                    cid = s.CidNavigation.Cid,
                    cname = s.CidNavigation.Cname,
                    grade = s.CidNavigation.Grade
                } : null
            });

            return Ok(result);
        }

        [HttpGet("department/{departmentId}/search")]
        public async Task<IActionResult> SearchStudentsByDepartment(string departmentId, [FromQuery] string? keyword)
        {
            var query = _context.Students
                .Include(s => s.CidNavigation)
                .Where(s => s.CidNavigation.Did == departmentId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(s =>
                    s.Sid.Contains(keyword) ||
                    s.Sname.Contains(keyword) ||
                    s.CidNavigation.Cname.Contains(keyword) ||
                    s.Sphone.Contains(keyword));
            }

            var students = await query.OrderBy(s => s.Cid).ThenBy(s => s.Sid).ToListAsync();

            var result = students.Select(s => new
            {
                sid = s.Sid,
                sname = s.Sname,
                gender = s.Gender,
                sphone = s.Sphone,
                spassword = s.Spassword,
                cid = s.Cid,
                className = s.CidNavigation?.Cname ?? string.Empty,
                studentClass = s.CidNavigation != null ? new
                {
                    cid = s.CidNavigation.Cid,
                    cname = s.CidNavigation.Cname,
                    grade = s.CidNavigation.Grade
                } : null
            });

            return Ok(result);
        }

        [HttpGet("profile/{sid}")]
        public async Task<IActionResult> GetProfile(string sid)
        {
            var student = await _context.Students
                .Include(s => s.CidNavigation)
                .ThenInclude(c => c.DidNavigation)
                .FirstOrDefaultAsync(s => s.Sid == sid);
            
            if (student == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                sid = student.Sid,
                sname = student.Sname,
                gender = student.Gender,
                sphone = student.Sphone,
                cid = student.Cid,
                className = student.CidNavigation?.Cname ?? string.Empty,
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
        }

        [HttpPut("profile/{sid}")]
        public async Task<IActionResult> UpdateProfile(string sid, [FromBody] StudentProfileUpdate update)
        {
            var student = await _context.Students.FindAsync(sid);
            if (student == null)
            {
                return NotFound();
            }

            // 更新姓名
            if (!string.IsNullOrEmpty(update.sname))
            {
                student.Sname = update.sname;
            }
            // 更新性别
            if (!string.IsNullOrEmpty(update.gender))
            {
                student.Gender = update.gender;
            }
            // 更新电话
            if (!string.IsNullOrEmpty(update.sphone))
            {
                student.Sphone = update.sphone;
            }
            // 更新密码（需要验证旧密码）
            if (!string.IsNullOrEmpty(update.newPassword) && !string.IsNullOrEmpty(update.oldPassword))
            {
                // 验证旧密码
                if (!BCrypt.Net.BCrypt.Verify(update.oldPassword, student.Spassword))
                {
                    return BadRequest(new { message = "旧密码不正确" });
                }
                student.Spassword = BCrypt.Net.BCrypt.HashPassword(update.newPassword);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "批量删除成功" });
        }

        [HttpPut("{sid}")]
        public async Task<IActionResult> UpdateStudent(string sid, [FromBody] StudentUpdateRequest update)
        {
            var existingStudent = await _context.Students.FindAsync(sid);
            if (existingStudent == null)
            {
                return NotFound();
            }

            // 更新基本信息
            if (!string.IsNullOrEmpty(update.Sname))
            {
                existingStudent.Sname = update.Sname;
            }
            if (!string.IsNullOrEmpty(update.Gender))
            {
                existingStudent.Gender = update.Gender;
            }
            if (!string.IsNullOrEmpty(update.Sphone))
            {
                existingStudent.Sphone = update.Sphone;
            }
            if (!string.IsNullOrEmpty(update.Spassword))
            {
                existingStudent.Spassword = update.Spassword;
            }
            if (!string.IsNullOrEmpty(update.Cid))
            {
                existingStudent.Cid = update.Cid;
            }

            await _context.SaveChangesAsync();
            return Ok(existingStudent);
        }

        public class StudentUpdateRequest
        {
            public string? Sname { get; set; }
            public string? Gender { get; set; }
            public string? Sphone { get; set; }
            public string? Spassword { get; set; }
            public string? Cid { get; set; }
        }

        [HttpDelete("{sid}")]
        public async Task<IActionResult> DeleteStudent(string sid)
        {
            var student = await _context.Students.FindAsync(sid);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("batch")]
        public async Task<IActionResult> BatchDeleteStudents([FromBody] List<string> sids)
        {
            var students = await _context.Students
                .Where(s => sids.Contains(s.Sid))
                .ToListAsync();

            _context.Students.RemoveRange(students);
            await _context.SaveChangesAsync();
            return Ok(new { count = students.Count });
        }

        [HttpGet("department-stats")]
        public async Task<IActionResult> GetDepartmentStats()
        {
            var stats = await _context.Students
                .Include(s => s.CidNavigation)
                .Include(s => s.CidNavigation.DidNavigation)
                .Where(s => s.CidNavigation != null && s.CidNavigation.DidNavigation != null)
                .GroupBy(s => new { s.CidNavigation.DidNavigation.Did, s.CidNavigation.DidNavigation.Dname })
                .Select(g => new
                {
                    did = g.Key.Did,
                    dname = g.Key.Dname,
                    count = g.Count()
                })
                .ToListAsync();

            return Ok(stats);
        }

        public class StudentProfileUpdate
        {
            public string? sname { get; set; }
            public string? gender { get; set; }
            public string? sphone { get; set; }
            public string? oldPassword { get; set; }
            public string? newPassword { get; set; }
        }
    }
}
