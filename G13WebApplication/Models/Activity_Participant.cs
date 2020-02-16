using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class Activity_Participant
    {
        [Key]
        public int Activity_ParticipantId { get; set; }

        [Display(Name = "Atividade")]
        [Required]
        public int ActivityId { get; set; }

        [Display(Name = "Participante")]
        [Required]
        public int UserId { get; set; }

        [Display(Name = "Ausência")]
        [Required]
        public int Absence { get; set; } = 0;

        [Display(Name = "Juri")]
        [Required]
        public int IsJuri { get; set; } = 1;

        [Display(Name = "Falta")]
        [Required]
        public int WontAttend { get; set; } = 0;
    }
}
