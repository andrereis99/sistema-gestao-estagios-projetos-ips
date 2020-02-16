using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class TO
    {
        [Key]
        [Display(Name = "TOId")]
        public int TOId { get; set; }

        [Display(Name = "FirstName")]
        public String FirstName { get; set; }

        [Display(Name = "LastName")]
        public String LastName { get; set; }

        [Display(Name = "Email")]
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public String Email { get; set; }

        public int? TfcIdFk { get; set; }
    }
}
