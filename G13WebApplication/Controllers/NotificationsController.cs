using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using G13WebApplication.Data;
using G13WebApplication.Models;
using System.Security.Claims;
using System.Data;
using Microsoft.Data.SqlClient;

namespace G13WebApplication.Controllers
{
    /** 
     * Classe que controla todas as ações das notificações
     */
    public class NotificationsController : BaseController
    {
        /**
         * Marca uma notificação como lida
         */
        private void ReadNotification(int? id)
        {
            using (SqlConnection scnConnection = new SqlConnection(ApplicationDbContext.ConnectionString))
            {
                scnConnection.Open();
                string strQuery = "UPDATE Notification SET ReadNotification = 1 where NotificationId = @id";

                SqlCommand scmCommand = new SqlCommand(strQuery, scnConnection);
                scmCommand.Parameters.AddWithValue("@id", id);

                scmCommand.ExecuteNonQuery();
            }
        }

        /**
         * Vê os detalhes de uma notificação e mete-a como lida
         */
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            ReadNotification(id);
            Notification notification = null;
            using (SqlConnection scnConnection = new SqlConnection(ApplicationDbContext.ConnectionString))
            {
                scnConnection.Open();
                string strQuery = "Select * FROM Notification where NotificationId = @id";

                SqlCommand scmCommand = new SqlCommand(strQuery, scnConnection);
                scmCommand.Parameters.AddWithValue("@id", id);

                SqlDataReader reader = scmCommand.ExecuteReader();

                while (reader.Read())
                {
                    notification = new Notification { NotificationId = (int)reader[0], Message = (string)reader[1], state = (string)reader[2], AddedOn = (DateTime)reader[3], UserId = (int)reader[4], ReadNotification = (int)reader[5] };
                }
            }
            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
        }

        /**
         * Utilizado para ver quantas notificações é que os utilizadores têm
         */
        public int NotificationsCount(ClaimsPrincipal user)
        {
            using (SqlConnection scnConnection = new SqlConnection(ApplicationDbContext.ConnectionString))
            {
                scnConnection.Open();
                string strQuery = "SELECT COUNT(*) FROM Notification where UserId = @AccountId AND ReadNotification = 0";

                SqlCommand scmCommand = new SqlCommand(strQuery, scnConnection);
                scmCommand.Parameters.AddWithValue("@AccountId", GetCurrentUserID(user));
                SqlDataReader dtrReader = scmCommand.ExecuteReader();
                if (dtrReader.HasRows)
                {
                    while (dtrReader.Read())
                    {
                        return (int)dtrReader[0];
                    }
                }
            }

            return 0;
        }

        /**
         * Devolve o user id do utilizador que está logado.
         */
        public int GetCurrentUserID(ClaimsPrincipal user)
        {
            return int.Parse(user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value);
        }

        /**
         * Lê todas as notificações
         */
        [HttpPost]
        public ActionResult ReadNotifications()
        {
            using (SqlConnection scnConnection = new SqlConnection(ApplicationDbContext.ConnectionString))
            {
                scnConnection.Open();
                string strQuery = "UPDATE Notification SET ReadNotification = 1 where UserId = @AccountId";

                SqlCommand scmCommand = new SqlCommand(strQuery, scnConnection);
                scmCommand.Parameters.AddWithValue("@AccountId", GetCurrentUserID(User));

                scmCommand.ExecuteNonQuery();
            }

            return PartialView("~/Views/Shared/_Notifications.cshtml", NotificationsCount(User).ToString());
        }

        /**
         * Recebe todas as notificações para as inserir na caixa das notificaçôes.
         */
        public List<Notification> GetNotifications(ClaimsPrincipal user)
        {
            List<Notification> list = new List<Notification>();
            using (SqlConnection scnConnection = new SqlConnection(ApplicationDbContext.ConnectionString))
            {
                scnConnection.Open();
                string strQuery = "Select * FROM Notification where UserId = @AccountId AND ReadNotification = 0";

                SqlCommand scmCommand = new SqlCommand(strQuery, scnConnection);
                scmCommand.Parameters.AddWithValue("@AccountId", GetCurrentUserID(user));

                Notification aux = null;

                SqlDataReader reader = scmCommand.ExecuteReader();

                while (reader.Read())
                {
                    aux = new Notification { NotificationId = (int)reader[0], Message = (string)reader[1], state = (string)reader[2], AddedOn = (DateTime)reader[3], UserId = (int)reader[4], ReadNotification = (int)reader[5] } ;
                    list.Add(aux);
                }
            }

            return list;
        }

    }
}
