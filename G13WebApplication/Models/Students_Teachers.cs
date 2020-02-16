using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class Students_Teachers
    {
        [Key]
        public int Students_TeachersId { get; set; }

        [Required]
        public int StudentIdFk { get; set; }

        [Required]
        public int TeacherIdFk { get; set; }
    }
}
