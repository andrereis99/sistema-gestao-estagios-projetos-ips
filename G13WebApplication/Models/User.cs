using System;
using System.Collections.Generic;
using System.Linq;

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Display(Name = "Email")]
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public String Email { get; set; }

        public int? StudentId { get; set; }

        public int? TeacherId { get; set; }

        public int? TOId { get; set; }

        [Display(Name = "Password")]
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public String Password { get; set; }

        public String photoPath { get; set; }
    }
}
