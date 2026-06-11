using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLeaveSystem.Data;
using StudentLeaveSystem.Models;

namespace StudentLeaveSystem.Controllers
{
    [Route("api/leaves")]
    [ApiController]
    public class LeaveApplicationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LeaveApplicationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitLeave([FromBody] LeaveApplicationRequest request)
        {
            var student = await _context.Students.FindAsync(request.sid);
            if (student == null)
            {
                return BadRequest("学生不存在");
            }

            var @class = await _context.Classes.FindAsync(student.Cid);
            if (@class == null)
            {
                return BadRequest("班级不存在");
            }

            if (@class.Tid == null)
            {
                return BadRequest("班级未分配班导师");
            }

            var application = new LeaveApplication
            {
                LeaveId = request.leaveId ?? Guid.NewGuid().ToString("N").Substring(0, 12),
                Sid = request.sid,
                LeaveType = request.leaveType,
                StartTime = request.startTime,
                EndTime = request.endTime,
                Reason = request.reason,
                SubmitTime = DateTime.Now,
                FirstTid = @class.Tid,
                FirstResult = "待审批",
                SecondResult = "未提交"
            };

            _context.LeaveApplications.Add(application);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudentApplications), new { studentId = application.Sid }, application);
        }

        public class LeaveApplicationRequest
        {
            public string? leaveId { get; set; }
            public string sid { get; set; } = string.Empty;
            public string leaveType { get; set; } = string.Empty;
            public DateTime startTime { get; set; }
            public DateTime endTime { get; set; }
            public string reason { get; set; } = string.Empty;
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentApplications(string studentId)
        {
            var applications = await _context.LeaveApplications
                .Where(l => l.Sid == studentId)
                .OrderByDescending(l => l.SubmitTime)
                .ToListAsync();
            return Ok(applications);
        }

        [HttpPut("update/{leaveId}")]
        public async Task<IActionResult> UpdateLeave(string leaveId, [FromBody] LeaveApplicationUpdate update)
        {
            var application = await _context.LeaveApplications.FindAsync(leaveId);
            if (application == null)
            {
                return NotFound();
            }

            if (application.FirstResult != "待审批")
            {
                return BadRequest("只能修改待审批的申请");
            }

            if (!string.IsNullOrEmpty(update.LeaveType))
            {
                application.LeaveType = update.LeaveType;
            }
            if (update.StartTime.HasValue)
            {
                application.StartTime = update.StartTime.Value;
            }
            if (update.EndTime.HasValue)
            {
                application.EndTime = update.EndTime.Value;
            }
            if (!string.IsNullOrEmpty(update.Reason))
            {
                application.Reason = update.Reason;
            }

            await _context.SaveChangesAsync();
            return Ok(application);
        }

        [HttpDelete("delete/{leaveId}")]
        public async Task<IActionResult> DeleteLeave(string leaveId)
        {
            var application = await _context.LeaveApplications.FindAsync(leaveId);
            if (application == null)
            {
                return NotFound();
            }

            if (application.FirstResult != "待审批")
            {
                return BadRequest("只能删除待审批的申请");
            }

            _context.LeaveApplications.Remove(application);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("can-submit/{studentId}")]
        public async Task<IActionResult> CanSubmit(string studentId)
        {
            var hasPending = await _context.LeaveApplications
                .AnyAsync(l => l.Sid == studentId && l.FirstResult == "待审批");
            return Ok(new { canSubmit = !hasPending });
        }

        [HttpGet("pending/first/{teacherId}")]
        public async Task<IActionResult> GetPendingFirstApproval(string teacherId)
        {
            var applications = await _context.LeaveApplications
                .Include(l => l.SidNavigation)
                .Include(l => l.SidNavigation.CidNavigation)
                .Where(l => l.FirstTid == teacherId && l.FirstResult == "待审批")
                .OrderBy(l => l.SubmitTime)
                .ToListAsync();

            var result = applications.Select(leave => new
            {
                leaveId = leave.LeaveId,
                leaveType = leave.LeaveType,
                startTime = leave.StartTime,
                endTime = leave.EndTime,
                reason = leave.Reason,
                submitTime = leave.SubmitTime,
                firstResult = leave.FirstResult,
                firstComment = leave.FirstComment,
                student = leave.SidNavigation != null ? new
                {
                    sid = leave.SidNavigation.Sid,
                    sname = leave.SidNavigation.Sname,
                    className = leave.SidNavigation.CidNavigation?.Cname ?? "未知班级",
                    cid = leave.SidNavigation.Cid
                } : null
            });

            return Ok(result);
        }

        [HttpGet("approved/first/{teacherId}")]
        public async Task<IActionResult> GetApprovedByFirst(string teacherId)
        {
            var applications = await _context.LeaveApplications
                .Include(l => l.SidNavigation)
                .Include(l => l.SidNavigation.CidNavigation)
                .Where(l => l.FirstTid == teacherId && l.FirstResult == "已通过")
                .OrderByDescending(l => l.SubmitTime)
                .ToListAsync();

            var result = applications.Select(leave => new
            {
                leaveId = leave.LeaveId,
                leaveType = leave.LeaveType,
                startTime = leave.StartTime,
                endTime = leave.EndTime,
                reason = leave.Reason,
                submitTime = leave.SubmitTime,
                firstResult = leave.FirstResult,
                firstComment = leave.FirstComment,
                student = leave.SidNavigation != null ? new
                {
                    sid = leave.SidNavigation.Sid,
                    sname = leave.SidNavigation.Sname,
                    className = leave.SidNavigation.CidNavigation?.Cname ?? "未知班级",
                    cid = leave.SidNavigation.Cid
                } : null
            });

            return Ok(result);
        }

        [HttpGet("rejected/first/{teacherId}")]
        public async Task<IActionResult> GetRejectedByFirst(string teacherId)
        {
            var applications = await _context.LeaveApplications
                .Include(l => l.SidNavigation)
                .Include(l => l.SidNavigation.CidNavigation)
                .Where(l => l.FirstTid == teacherId && l.FirstResult == "已拒绝")
                .OrderByDescending(l => l.SubmitTime)
                .ToListAsync();

            var result = applications.Select(leave => new
            {
                leaveId = leave.LeaveId,
                leaveType = leave.LeaveType,
                startTime = leave.StartTime,
                endTime = leave.EndTime,
                reason = leave.Reason,
                submitTime = leave.SubmitTime,
                firstResult = leave.FirstResult,
                firstComment = leave.FirstComment,
                student = leave.SidNavigation != null ? new
                {
                    sid = leave.SidNavigation.Sid,
                    sname = leave.SidNavigation.Sname,
                    className = leave.SidNavigation.CidNavigation?.Cname ?? "未知班级",
                    cid = leave.SidNavigation.Cid
                } : null
            });

            return Ok(result);
        }

        [HttpGet("all/first/{teacherId}")]
        public async Task<IActionResult> GetAllFirstApproval(string teacherId)
        {
            var applications = await _context.LeaveApplications
                .Include(l => l.SidNavigation)
                .Include(l => l.SidNavigation.CidNavigation)
                .Where(l => l.FirstTid == teacherId)
                .OrderByDescending(l => l.SubmitTime)
                .ToListAsync();

            var result = applications.Select(leave => new
            {
                leaveId = leave.LeaveId,
                leaveType = leave.LeaveType,
                startTime = leave.StartTime,
                endTime = leave.EndTime,
                reason = leave.Reason,
                submitTime = leave.SubmitTime,
                firstResult = leave.FirstResult,
                firstComment = leave.FirstComment,
                student = leave.SidNavigation != null ? new
                {
                    sid = leave.SidNavigation.Sid,
                    sname = leave.SidNavigation.Sname,
                    className = leave.SidNavigation.CidNavigation?.Cname ?? "未知班级",
                    cid = leave.SidNavigation.Cid
                } : null
            });

            return Ok(result);
        }

        [HttpGet("stats/first/{teacherId}")]
        public async Task<IActionResult> GetFirstApprovalStats(string teacherId)
        {
            var total = await _context.LeaveApplications
                .CountAsync(l => l.FirstTid == teacherId);
            var approved = await _context.LeaveApplications
                .CountAsync(l => l.FirstTid == teacherId && l.FirstResult == "已通过");
            var rejected = await _context.LeaveApplications
                .CountAsync(l => l.FirstTid == teacherId && l.FirstResult == "已拒绝");
            var pending = await _context.LeaveApplications
                .CountAsync(l => l.FirstTid == teacherId && l.FirstResult == "待审批");

            return Ok(new { total, approved, rejected, pending });
        }

        [HttpPut("{leaveId}/first-approve")]
        public async Task<IActionResult> FirstApprove(string leaveId, [FromBody] ApprovalRequest request)
        {
            string comment = request.comment;
            string teacherId = request.teacherId;

            var application = await _context.LeaveApplications
                .Include(l => l.SidNavigation)
                .Include(l => l.SidNavigation.CidNavigation)
                .FirstOrDefaultAsync(l => l.LeaveId == leaveId);

            if (application == null)
            {
                return NotFound();
            }

            application.FirstResult = "已通过";
            application.FirstComment = comment;

            var days = (application.EndTime - application.StartTime).TotalDays + 1;

            if (days > 3)
            {
                // 添加空值检查
                if (application.SidNavigation == null)
                {
                    return BadRequest("学生信息不存在");
                }
                if (application.SidNavigation.CidNavigation == null)
                {
                    return BadRequest("班级信息不存在");
                }
                if (string.IsNullOrEmpty(application.SidNavigation.CidNavigation.Did))
                {
                    return BadRequest("班级未关联院系");
                }

                var departmentId = application.SidNavigation.CidNavigation.Did;
                var workTeacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.Did == departmentId && t.Position == "学工老师");

                if (workTeacher != null)
                {
                    application.SecondTid = workTeacher.Tid;
                    application.SecondResult = "待审批";
                }
                else
                {
                    return BadRequest("该院系未分配学工老师");
                }
            }
            else
            {
                application.SecondResult = "无需二级审批";
            }

            await _context.SaveChangesAsync();
            return Ok(application);
        }

        public class ApprovalRequest
        {
            public string comment { get; set; } = string.Empty;
            public string teacherId { get; set; } = string.Empty;
        }

        [HttpPut("{leaveId}/first-reject")]
        public async Task<IActionResult> FirstReject(string leaveId, [FromBody] ApprovalRequest request)
        {
            string comment = request.comment;
            string teacherId = request.teacherId;

            var application = await _context.LeaveApplications.FindAsync(leaveId);
            if (application == null)
            {
                return NotFound();
            }

            application.FirstResult = "已拒绝";
            application.FirstComment = comment;
            application.SecondResult = "一级拒绝";

            await _context.SaveChangesAsync();
            return Ok(application);
        }

        [HttpGet("pending/second/{teacherId}")]
        public async Task<IActionResult> GetPendingSecondApproval(string teacherId)
        {
            var applications = await _context.LeaveApplications
                .Include(l => l.SidNavigation)
                .Include(l => l.SidNavigation.CidNavigation)
                .Include(l => l.FirstT)
                .Where(l => l.SecondTid == teacherId && l.SecondResult == "待审批")
                .OrderBy(l => l.SubmitTime)
                .ToListAsync();

            var result = applications.Select(leave => new
            {
                leaveId = leave.LeaveId,
                leaveType = leave.LeaveType,
                startTime = leave.StartTime,
                endTime = leave.EndTime,
                reason = leave.Reason,
                submitTime = leave.SubmitTime,
                firstResult = leave.FirstResult,
                firstComment = leave.FirstComment,
                secondResult = leave.SecondResult,
                secondComment = leave.SecondComment,
                student = leave.SidNavigation != null ? new
                {
                    sid = leave.SidNavigation.Sid,
                    sname = leave.SidNavigation.Sname,
                    className = leave.SidNavigation.CidNavigation?.Cname ?? "未知班级",
                    cid = leave.SidNavigation.Cid
                } : null,
                firstApprover = leave.FirstT != null ? new
                {
                    tid = leave.FirstT.Tid,
                    tname = leave.FirstT.Tname
                } : null
            });

            return Ok(result);
        }

        [HttpGet("second-approval/history/{teacherId}")]
        public async Task<IActionResult> GetSecondApprovalHistory(string teacherId)
        {
            var applications = await _context.LeaveApplications
                .Include(l => l.SidNavigation)
                .Include(l => l.SidNavigation.CidNavigation)
                .Include(l => l.SidNavigation.CidNavigation.DidNavigation)
                .Include(l => l.FirstT)
                .Where(l => l.SecondTid == teacherId)
                .OrderByDescending(l => l.SubmitTime)
                .ToListAsync();

            var result = applications.Select(leave => new
            {
                leaveId = leave.LeaveId,
                leaveType = leave.LeaveType,
                startTime = leave.StartTime,
                endTime = leave.EndTime,
                reason = leave.Reason,
                submitTime = leave.SubmitTime,
                firstResult = leave.FirstResult,
                firstComment = leave.FirstComment,
                secondResult = leave.SecondResult,
                secondComment = leave.SecondComment,
                secondApprovalTime = leave.SecondApprovalTime,
                leaveDays = (int)Math.Ceiling((leave.EndTime - leave.StartTime).TotalDays + 1),
                student = leave.SidNavigation != null ? new
                {
                    sid = leave.SidNavigation.Sid,
                    sname = leave.SidNavigation.Sname,
                    className = leave.SidNavigation.CidNavigation?.Cname ?? "未知班级",
                    departmentName = leave.SidNavigation.CidNavigation?.DidNavigation?.Dname ?? "未知院系",
                    cid = leave.SidNavigation.Cid
                } : null,
                firstApprover = leave.FirstT != null ? new
                {
                    tid = leave.FirstT.Tid,
                    tname = leave.FirstT.Tname
                } : null
            });

            return Ok(result);
        }

        [HttpGet("department/{teacherId}")]
        public async Task<IActionResult> GetDepartmentApplications(string teacherId, [FromQuery] string? classId = null)
        {
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
            {
                return NotFound();
            }

            var query = _context.LeaveApplications
                .Include(l => l.SidNavigation)
                .Include(l => l.SidNavigation.CidNavigation)
                .Include(l => l.SidNavigation.CidNavigation.DidNavigation)
                .Where(l => l.SidNavigation.CidNavigation.Did == teacher.Did);

            if (!string.IsNullOrEmpty(classId))
            {
                query = query.Where(l => l.SidNavigation.Cid == classId);
            }

            var applications = await query
                .OrderByDescending(l => l.SubmitTime)
                .ToListAsync();

            var result = applications.Select(leave => new
            {
                leaveId = leave.LeaveId,
                leaveType = leave.LeaveType,
                startTime = leave.StartTime,
                endTime = leave.EndTime,
                reason = leave.Reason,
                submitTime = leave.SubmitTime,
                firstResult = leave.FirstResult,
                firstComment = leave.FirstComment,
                secondResult = leave.SecondResult,
                secondComment = leave.SecondComment,
                student = leave.SidNavigation != null ? new
                {
                    sid = leave.SidNavigation.Sid,
                    sname = leave.SidNavigation.Sname,
                    className = leave.SidNavigation.CidNavigation?.Cname ?? "未知班级",
                    departmentName = leave.SidNavigation.CidNavigation?.DidNavigation?.Dname ?? "未知院系",
                    cid = leave.SidNavigation.Cid
                } : null
            });

            return Ok(result);
        }

        [HttpGet("stats/second/{teacherId}")]
        public async Task<IActionResult> GetSecondApprovalStats(string teacherId)
        {
            var total = await _context.LeaveApplications
                .CountAsync(l => l.SecondTid == teacherId);
            var approved = await _context.LeaveApplications
                .CountAsync(l => l.SecondTid == teacherId && l.SecondResult == "已通过");
            var rejected = await _context.LeaveApplications
                .CountAsync(l => l.SecondTid == teacherId && l.SecondResult == "已拒绝");
            var pending = await _context.LeaveApplications
                .CountAsync(l => l.SecondTid == teacherId && l.SecondResult == "待审批");

            return Ok(new { total, approved, rejected, pending });
        }

        [HttpPut("{leaveId}/second-approve")]
        public async Task<IActionResult> SecondApprove(string leaveId, [FromBody] dynamic request)
        {
            string comment = request.comment;
            string teacherId = request.teacherId;

            var application = await _context.LeaveApplications.FindAsync(leaveId);
            if (application == null)
            {
                return NotFound();
            }

            application.SecondResult = "已通过";
            application.SecondComment = comment;
            await _context.SaveChangesAsync();
            return Ok(application);
        }

        [HttpPut("{leaveId}/second-reject")]
        public async Task<IActionResult> SecondReject(string leaveId, [FromBody] dynamic request)
        {
            string comment = request.comment;
            string teacherId = request.teacherId;

            var application = await _context.LeaveApplications.FindAsync(leaveId);
            if (application == null)
            {
                return NotFound();
            }

            application.SecondResult = "已拒绝";
            application.SecondComment = comment;
            await _context.SaveChangesAsync();
            return Ok(application);
        }

        [HttpGet("stats/by-class/{teacherId}")]
        public async Task<IActionResult> GetStatsByClass(string teacherId)
        {
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
            {
                return NotFound();
            }

            var stats = await _context.LeaveApplications
                .Include(l => l.SidNavigation)
                .Include(l => l.SidNavigation.CidNavigation)
                .Where(l => l.SidNavigation.CidNavigation.Did == teacher.Did)
                .GroupBy(l => l.SidNavigation.CidNavigation.Cname)
                .Select(g => new { className = g.Key, count = g.Count() })
                .ToListAsync();

            return Ok(stats);
        }

        [HttpGet("stats/by-type/{teacherId}")]
        public async Task<IActionResult> GetStatsByType(string teacherId)
        {
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
            {
                return NotFound();
            }

            var stats = await _context.LeaveApplications
                .Include(l => l.SidNavigation)
                .Include(l => l.SidNavigation.CidNavigation)
                .Where(l => l.SidNavigation.CidNavigation.Did == teacher.Did)
                .GroupBy(l => l.LeaveType)
                .Select(g => new { leaveType = g.Key, leaveCount = g.Count() })
                .ToListAsync();

            return Ok(stats);
        }

        public class LeaveApplicationUpdate
        {
            public string? LeaveType { get; set; }
            public DateTime? StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public string? Reason { get; set; }
        }
    }
}
