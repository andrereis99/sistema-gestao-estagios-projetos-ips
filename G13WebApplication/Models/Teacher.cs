using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class Teacher
    {
        [Key]
        public int TeacherId { get; set; }

        [Display(Name = "FirstName")]
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public String FirstName { get; set; }

        [Display(Name = "LastName")]
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public String LastName { get; set; }

        [Display(Name = "Email")]
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public String Email { get; set; }

        [Display(Name = "Taught_Area")]
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public String Taught_Area { get; set; }

        [Display(Name = "Role")]
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public String Role { get; set; }
    }
}
