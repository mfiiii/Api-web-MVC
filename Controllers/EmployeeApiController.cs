using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using myappmvc.Data;
using myappmvc.Models;
using Microsoft.EntityFrameworkCore;
using myappmvc.DTOs;
using Microsoft.AspNetCore.OData.Query;
using log4net;
using myappmvc.Interfaces;
using myappmvc.LogEventArgs;

namespace myappmvc.Controllers
{
    [Route("odata/[controller]")]
    [ApiController]
    public class EmployeeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // làm việc với database    
        private readonly ILoggerService _logger;// Log4Net logger
        private readonly IRedisCacheService _cache;// Redis cache 
        private readonly EventBasedLogger _eventLogger; // Sử dụng EventBasedLogger để ghi log sự kiện

        // Dependency injection(DI) cho DbContext + Redis.
        public EmployeeApiController(ApplicationDbContext context, IRedisCacheService cache, ILoggerService logger, EventBasedLogger eventLogger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _eventLogger = eventLogger;
        }



       /* public async Task Example()
        {
            Console.WriteLine("Start");
            await Task.Delay(2000);
            Console.WriteLine("End");
       */

            /*      
            private static int _check = 0;

                  [HttpGet("check-logger-singleton")]
                  public IActionResult CheckLoggerSingleton()
                  {
                      int checksingleton = _logger.GetHashCode();
                      var myClass1 = new MyClass() { id = 1 };
                      var myClass2 = new MyClass() { id = 2 };

                      if (_check == 0)
                      {
                          _check = checksingleton;
                          return Ok(new { Message = "First check", Logger = checksingleton });
                      }
                      else
                      {
                          bool isSingleton = _check == checksingleton;
                          return Ok(new { Message = "Subsequent check", Logger= checksingleton, IsSingleton = isSingleton });
                      }
                  }
                */





            [EnableQuery]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDTO>>> GetEmployees()
        {
            _logger.Info(this, "Event: Get all employees");

            try
            {
                const string cacheKey = "employee_list";

                var cached = await _cache.GetAsync<List<EmployeeDTO>>(cacheKey); // lấy danh sách nhân viên từ cache
                if (cached != null)
                {
                    _logger.Info(this, "Event: Returned employees from cache.");
                    return Ok(cached); // trả về danh sách nhân viên từ cache
                }



                var employees = await _context.Employees
                    .Include(e => e.Department) // lấy thông tin phòng ban
                    .Select(e => new EmployeeDTO
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Salary = e.Salary,
                        Position = e.Position,
                        Department = e.Department != null ? new DepartmentDTO
                        {
                            Id = e.Department.Id,
                            Name = e.Department.Name
                        } : null
                    })
                    .ToListAsync();
                await _cache.SetAsync(cacheKey, employees, TimeSpan.FromMinutes(10)); // cache kết quả, tồn tại 10 phút

                _logger.Info(this, $"Returned {employees.Count} employees.");
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.Info(this, $"Error getting employees: {ex.Message}");
                _logger.Error(this,"Error getting employees", ex);
                // Vẫn ghi log bằng log4net cho trường hợp lỗi
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDTO>> GetEmployee(int id)
        {
            _logger.Info(this, $"Get employee with ID {id}"); // Ghi log sự kiện lấy nhân viên theo ID

            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                {
                    _logger.Warn(this,$"Employee with ID {id} not found");
                    return NotFound();
                }

                var dto = new EmployeeDTO
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Salary = employee.Salary,
                    Position = employee.Position,
                    Department = employee.Department != null ? new DepartmentDTO
                    {
                        Id = employee.Department.Id,
                        Name = employee.Department.Name
                    } : null
                };
                _logger.Info(this, $"Employee with ID {id} found"); // Ghi log sự kiện thành công khi tìm thấy nhân viên

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.Error(this, $"Error getting employee with ID {id}", ex); // Sử dụng log4net để ghi log lỗi chi tiết
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<EmployeeDTO>> CreateEmployee([FromBody] CreateEmployeeDTO dto)
        {
            _logger.Info(this, "Creating a new employee");

            if (dto == null)
            {
                _logger.Warn(this, "Received null DTO");
                return BadRequest(new { message = "Invalid data." });
            }

            try
            {
                Department? department = null; 

                if (dto.Department != null)
                {
                    if (dto.Department.Id > 0)
                    {
                        department = await _context.Departments.FindAsync(dto.Department.Id);
                        if (department == null)
                        {
                            _logger.Warn(this, $"Department ID {dto.Department.Id} not found");
                            return BadRequest(new { message = "Department not found." });
                        }
                    }

                    // nếu department không có ID thì thêm mới department
                    else if (!string.IsNullOrWhiteSpace(dto.Department.Name))
                    {
                        department = new Department { Name = dto.Department.Name };
                        _context.Departments.Add(department);
                        await _context.SaveChangesAsync();
                    }
                }

                var employee = new Employee
                {
                    Name = dto.Name,
                    Position = dto.Position,
                    Salary = dto.Salary,
                    Department = department
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Xóa cache sau khi thêm nhân viên mới
                await _cache.RemoveAsync("employee_list");
                _logger.Info(this, "Employee list cache removed from Redis after deletion.");


                _logger.Info(this, $"Employee created with ID {employee.Id}");

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, new EmployeeDTO
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Position = employee.Position,
                    Salary = employee.Salary,
                    Department = department != null ?
                        new DepartmentDTO { Id = department.Id, Name = department.Name } : null
                });
            }
            catch (Exception ex)
            {
                _logger.Error(this, "Error creating employee", ex);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDTO dto)
        {
            _logger.Info(this, $"Updating employee with ID {id}");

            if (dto == null)
            {
                _logger.Warn(this, "Received null DTO for UpdateEmployee");
                return BadRequest(new { message = "Invalid data." });
            }

            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.Warn(this, $"Employee with ID {id} not found");
                    return NotFound();
                }

                Department? department = null;
                if (dto.Department != null)
                {
                    if (dto.Department.Id > 0)
                    {
                        department = await _context.Departments.FindAsync(dto.Department.Id);
                        if (department == null)
                        {
                            _logger.Warn(this, $"Department ID {dto.Department.Id} not found");
                            return BadRequest(new { message = "Department not found." });
                        }
                    }
                    else
                    {
                        department = new Department { Name = dto.Department.Name };
                        _context.Departments.Add(department);
                        await _context.SaveChangesAsync();
                    }
                }

                employee.Name = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name : employee.Name;
                employee.Position = !string.IsNullOrWhiteSpace(dto.Position) ? dto.Position : employee.Position;

                if (dto.Salary.HasValue && dto.Salary.Value > 0)
                {
                    employee.Salary = dto.Salary.Value;
                }
                employee.Department = department ?? employee.Department;

                await _context.SaveChangesAsync();
                await _cache.RemoveAsync("employee_list");



                _logger.Info(this, $"Employee with ID {id} updated");

                return Ok(new EmployeeDTO
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Position = employee.Position,
                    Salary = employee.Salary,
                    Department = employee.Department != null ?
                        new DepartmentDTO { Id = employee.Department.Id, Name = employee.Department.Name } : null
                });
            }
            catch (Exception ex)
            {
                _logger.Error(this, $"Error updating employee with ID {id}: {ex.Message}", ex); // Thêm .Message
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            _logger.Info(this, $"Deleting employee with ID {id}");

            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.Warn(this, $"Employee with ID {id} not found");
                    return NotFound();
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync("employee_list");

                _logger.Info(this, $"Employee with ID {id} deleted");

                return Ok(new EmployeeDTO
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Position = employee.Position,
                    Salary = employee.Salary
                });
            }
            catch (Exception ex)
            {
                _logger.Error(this, $"Error deleting employee with ID {id}", ex);
                return StatusCode(500, "Internal server error");
            }
        }
    }
    public class MyClass
    {
        public int id { get; set; }
    }
}
