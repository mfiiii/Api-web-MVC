using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myappmvc.DTOs
{
    public class CreateEmployeeDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Position { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Lương buộc phải lớn hơn 0")]

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        public DepartmentDTO? Department { get; set; }

    }
}
