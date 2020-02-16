using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class Student
    {
        // Primary key
        [Key]
        public int StudentId { get; set; }

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

        [Display(Name = "StudentNumber")]
        public int StudentNumber { get; set; }

        [Display(Name = "Field")]
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public String Field { get; set; }

        [Display(Name = "PlanID")]
        public int? PlanIdFk { get; set; }
    }
}
