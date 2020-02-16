using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class ProposalDO
    {
        [Key]
        [Display(Name = "ProposalId")]
        public int ProposalId { get; set; }

        [Display(Name = "IdTeacher")]
        public int TeacherIdFk { get; set; }

        [Display(Name = "StudentNumber")]
        public int StudentNumber { get; set; }
    }
}
