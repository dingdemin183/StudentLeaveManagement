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
                .Where(s => s.Cid == classId)
                .ToListAsync();

            var result = students.Select(s => new
            {
                sid = s.Sid,
                sname = s.Sname,
                gender = s.Gender,
                sphone = s.Sphone,
                spassword = s.Spassword,
                cid = s.Cid
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
            return Ok(students);
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

            if (!string.IsNullOrEmpty(update.sphone))
            {
                student.Sphone = update.sphone;
            }
            if (!string.IsNullOrEmpty(update.newPassword))
            {
                student.Spassword = BCrypt.Net.BCrypt.HashPassword(update.newPassword);
            }

            await _context.SaveChangesAsync();
            return Ok(student);
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

        public class StudentProfileUpdate
        {
            public string? sphone { get; set; }
            public string? newPassword { get; set; }
        }
    }
}
