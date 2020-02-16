using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class Ata
    {
        [Key]
        public int AtaId { get; set; }

        [Display(Name = "Estudante")]
        [Required]
        public int StudentId { get; set; }

        public int UserId { get; set; }

        [Display(Name = "Ficheiro")]
        [Required]
        public String FilePath { get; set; }

        [Display(Name = "Data de Reunião")]
        [Required]
        public DateTime MeetingDate { get; set; }

        [Display(Name = "Estado")]
        [Required]
        public int FlagReject { get; set; }

        [Display(Name = "Atividade")]
        [Required]
        public int ActivityId { get; set; }
    }
}
