using System.ComponentModel.DataAnnotations;

namespace EmployeeManagment.Dtos
{
    public class EmployeeRequest
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(50)]
        public string Designation { get; set; }

        [Required, StringLength(50)]
        public string Department { get; set; }

        [Required, Phone]
        public string ContactNumber { get; set; }
    }
}
