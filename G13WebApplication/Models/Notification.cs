using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace G13WebApplication.Models
{
    public class Notification
    {
        // Primary key
        [Key]
        public int NotificationId { get; set; }

        [Display(Name = "Mensagem")]
        [Required(ErrorMessage = "A {0} é obrigatória")]
        [StringLength(400, ErrorMessage = "A {0} não deve ter mais do que {1} caracteres")]
        public string Message { get; set; }


        [Display(Name = "Estado")]
        [Required(ErrorMessage = "A {0} é obrigatória")]
        [StringLength(50, ErrorMessage = "A {0} não deve ter mais do que {1} caracteres")]
        public string state { get; set; }

        [Display(Name = "Adicionado")]
        public DateTime AddedOn { get; set; }

        public int UserId { get; set; }

        public int ReadNotification { get; set; }
    }
}
