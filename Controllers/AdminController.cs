using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLeaveSystem.Data;
using StudentLeaveSystem.Models;

namespace StudentLeaveSystem.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("teachers")]
        public async Task<IActionResult> CreateTeacher([FromBody] Teacher teacher)
        {
            teacher.Tpassword = BCrypt.Net.BCrypt.HashPassword(teacher.Tpassword);
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTeacherDetails), new { tid = teacher.Tid }, teacher);
        }

        [HttpGet("teachers")]
        public async Task<IActionResult> GetTeachers([FromQuery] string? position)
        {
            IQueryable<Teacher> query = _context.Teachers.Include(t => t.DidNavigation);

            if (!string.IsNullOrEmpty(position))
            {
                query = query.Where(t => t.Position == position);
            }

            var teachers = await query.ToListAsync();
            
            var result = teachers.Select(t => new {
                tid = t.Tid,
                tname = t.Tname,
                gender = t.Gender,
                position = t.Position,
                tphone = t.Tphone,
                did = t.Did,
                department = t.DidNavigation != null ? new {
                    did = t.DidNavigation.Did,
                    dname = t.DidNavigation.Dname
                } : null
            });
            
            return Ok(result);
        }

        [HttpGet("work-teachers")]
        public async Task<IActionResult> GetWorkTeachers()
        {
            var teachers = await _context.Teachers
                .Where(t => t.Position == "学工老师")
                .Include(t => t.DidNavigation)
                .ToListAsync();

            var result = teachers.Select(t => new {
                tid = t.Tid,
                tname = t.Tname,
                gender = t.Gender,
                position = t.Position,
                tphone = t.Tphone,
                did = t.Did,
                department = t.DidNavigation != null ? new {
                    did = t.DidNavigation.Did,
                    dname = t.DidNavigation.Dname
                } : null,
                departmentName = t.DidNavigation?.Dname ?? ""
            }).ToList();

            var grouped = result
                .GroupBy(t => t.departmentName)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(t => new {
                        tid = t.tid,
                        tname = t.tname,
                        gender = t.gender,
                        position = t.position,
                        tphone = t.tphone,
                        did = t.did,
                        department = t.department
                    }).ToList()
                );

            return Ok(grouped);
        }

        [HttpGet("mentor-teachers")]
        public async Task<IActionResult> GetMentorTeachers()
        {
            var teachers = await _context.Teachers
                .Where(t => t.Position == "班导师")
                .ToListAsync();
            return Ok(teachers);
        }

        [HttpDelete("teachers/{tid}")]
        public async Task<IActionResult> DeleteTeacher(string tid)
        {
            var teacher = await _context.Teachers.FindAsync(tid);
            if (teacher == null)
            {
                return NotFound();
            }

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("teachers/{tid}")]
        public async Task<IActionResult> UpdateTeacher(string tid, [FromBody] TeacherUpdateRequest request)
        {
            if (tid != request.tid)
            {
                return BadRequest();
            }

            var existingTeacher = await _context.Teachers.FindAsync(tid);
            if (existingTeacher == null)
            {
                return NotFound();
            }

            // 验证院系是否存在
            if (!string.IsNullOrEmpty(request.did))
            {
                var departmentExists = await _context.Departments.AnyAsync(d => d.Did == request.did);
                if (!departmentExists)
                {
                    return BadRequest($"院系ID {request.did} 不存在");
                }
            }

            existingTeacher.Tname = request.tname;
            existingTeacher.Gender = request.gender;
            existingTeacher.Position = request.position;
            existingTeacher.Tphone = request.tphone;
            existingTeacher.Did = request.did;

            await _context.SaveChangesAsync();
            return Ok(existingTeacher);
        }

        public class TeacherUpdateRequest
        {
            public string tid { get; set; } = string.Empty;
            public string tname { get; set; } = string.Empty;
            public string gender { get; set; } = string.Empty;
            public string position { get; set; } = string.Empty;
            public string tphone { get; set; } = string.Empty;
            public string did { get; set; } = string.Empty;
        }

        [HttpPut("teachers/{tid}/reset-password")]
        public async Task<IActionResult> ResetPassword(string tid)
        {
            var teacher = await _context.Teachers.FindAsync(tid);
            if (teacher == null)
            {
                return NotFound();
            }

            teacher.Tpassword = BCrypt.Net.BCrypt.HashPassword("123456");
            await _context.SaveChangesAsync();
            return Ok(new { message = "密码已重置为默认密码 123456" });
        }

        [HttpPost("bind-mentor")]
        public async Task<IActionResult> BindMentor([FromBody] MentorBindRequest request)
        {
            var @class = await _context.Classes.FindAsync(request.classId);
            if (@class == null)
            {
                return NotFound("班级不存在");
            }

            var teacher = await _context.Teachers.FindAsync(request.teacherId);
            if (teacher == null)
            {
                return BadRequest("教师不存在");
            }

            @class.Tid = request.teacherId;
            await _context.SaveChangesAsync();
            return Ok(@class);
        }

        [HttpDelete("unbind-mentor/{classId}")]
        public async Task<IActionResult> UnbindMentor(string classId)
        {
            var @class = await _context.Classes.FindAsync(classId);
            if (@class == null)
            {
                return NotFound();
            }

            @class.Tid = null;
            await _context.SaveChangesAsync();
            return Ok(@class);
        }

        [HttpGet("classes-with-mentors")]
        public async Task<IActionResult> GetClassesWithMentors()
        {
            var classes = await _context.Classes
                .Include(c => c.TidNavigation)
                .Include(c => c.DidNavigation)
                .ToListAsync();

            var result = classes.Select(c => new {
                cid = c.Cid,
                cname = c.Cname,
                grade = c.Grade,
                did = c.Did,
                tid = c.Tid,
                department = c.DidNavigation != null ? new {
                    did = c.DidNavigation.Did,
                    dname = c.DidNavigation.Dname
                } : null,
                classTeacher = c.TidNavigation != null ? new {
                    tid = c.TidNavigation.Tid,
                    tname = c.TidNavigation.Tname
                } : null
            });

            return Ok(result);
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _context.Departments.ToListAsync();
            return Ok(departments);
        }

        [HttpPost("departments")]
        public async Task<IActionResult> CreateDepartment([FromBody] Department department)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDepartments), new { did = department.Did }, department);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var studentCount = await _context.Students.CountAsync();
            var teacherCount = await _context.Teachers.CountAsync();
            var classCount = await _context.Classes.CountAsync();
            var departmentCount = await _context.Departments.CountAsync();
            var leaveCount = await _context.LeaveApplications.CountAsync();
            var pendingCount = await _context.LeaveApplications
                .CountAsync(l => l.FirstResult == "待审批" || l.SecondResult == "待审批");

            return Ok(new
            {
                studentCount,
                totalTeachers = teacherCount,  // 修改为前端期望的字段名
                classCount,
                departmentCount,
                leaveCount,
                pendingCount
            });
        }

        [HttpGet("teachers/{tid}/details")]
        public async Task<IActionResult> GetTeacherDetails(string tid)
        {
            var teacher = await _context.Teachers
                .Include(t => t.DidNavigation)
                .FirstOrDefaultAsync(t => t.Tid == tid);

            if (teacher == null)
            {
                return NotFound();
            }

            return Ok(teacher);
        }

        public class MentorBindRequest
        {
            public string classId { get; set; } = string.Empty;
            public string teacherId { get; set; } = string.Empty;
        }
    }
}
