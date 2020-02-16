using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class Activity_Suggested_Date
    {
        [Key]
        public int Suggested_DateId { get; set; }

        [Required]
        public int ActivityId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime Suggested_Date { get; set; }

        [Required]
        public int Accepted { get; set; }
    }
}
