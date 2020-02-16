using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class MarkedAbsences
    {
        [Key]
        public int MarkedAbsenceId { get; set; }

        [Display(Name = "Atividade")]
        [Required]
        public int ActivityId { get; set; }

        [Display(Name = "Participante")]
        [Required]
        public int UserId { get; set; }
    }
}
