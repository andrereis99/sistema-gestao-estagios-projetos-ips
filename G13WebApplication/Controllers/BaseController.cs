using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    public class BaseController : Controller
    {
        public void Alert(string title, string message, NotificationType notificationType)
        {
            var msg = "<script language='javascript'>swal('" + title + "', '" + message + "','" + notificationType + "')" + "</script>";
            TempData["notification"] = msg;
        }
    }
}