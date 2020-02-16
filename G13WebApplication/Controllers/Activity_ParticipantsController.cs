using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using G13WebApplication.Data;
using G13WebApplication.Models;
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    /**
     * Classe que trata das ações relativas aos participantes das atividades
     */
    public class Activity_ParticipantsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public Activity_ParticipantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /**
         * Método que marca uma falta a um participante da atividade
         */
        public async Task<IActionResult> MarkFoul(int userId, int activityId)
        {

            var absencesList = _context.Activity_Participant.FromSqlRaw("Select * from [dbo].[Activity_Participant] where UserId = " + userId + "and ActivityId = " + activityId).FirstOrDefault();
            absencesList.WontAttend = 1;
            await _context.SaveChangesAsync();
            
            return RedirectToAction("Details", "Activities", new { id = activityId });
        }

        /**
         * Método que retira uma falta a um participante da atividade.
         */
        public async Task<IActionResult> RemoveFoul(int userId, int activityId)
        {

            var absencesList = _context.Activity_Participant.FromSqlRaw("Select * from [dbo].[Activity_Participant] where UserId = " + userId + "and ActivityId = " + activityId).FirstOrDefault();
            absencesList.WontAttend = 0;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Activities", new { id = activityId });
        }

        /**
         * Método que marca a não presença de um utilizador
         */
        public IActionResult MarkAbsence(int? id, int? userId)
        {
            return RedirectToAction("AbsenceManager", new { id = id, userId = userId, value = 1 });
        }

        /**
         * Método que marca a presença de um utilizador
         */
        public IActionResult MarkPresence(int? id, int? userId)
        {
            return RedirectToAction("AbsenceManager", new { id = id, userId = userId, value = -1 });
        }

        /**
         * Método que retira a não presença de um utilizador
         */
        public IActionResult RemoveAbsence(int? id, int? userId)
        {
            return RedirectToAction("AbsenceManager", new { id = id, userId = userId, value = 0});
        }

        /**
         * MAnager das presenças dos utilizadores nas atividades
         */
        public IActionResult AbsenceManager(int? id, int? userId, int value)
        {
            var activity_Participant = _context.Activity_Participant.FromSqlRaw("Select * from [dbo].[Activity_Participant] where UserId = " + userId + "and ActivityId = " + id).FirstOrDefault();
            activity_Participant.Absence = value;
            _context.SaveChanges();
            return RedirectToAction("Details", "Activities", new { id = activity_Participant.ActivityId });
        }

        /**
         * Adiciona um participante a uma atividade
         */
        public async Task<IActionResult> AddToActivity(int? id)
        {
            var participants = _context.Activity_Participant.FromSqlRaw("Select * from [dbo].[Activity_Participant] where ActivityId = " + id).ToList();
            var users = _context.User.FromSqlRaw("Select * from [dbo].[User]").ToList();
            foreach (var p in participants)
            {
                var userAux = _context.User.FromSqlRaw("Select * from [dbo].[User] where UserId = " + p.UserId).FirstOrDefault();
                users.Remove(userAux);
            }
            ViewData["ActivityId"] = id;
            return View(users);
        }

        // POST: Activity_Participants/Create
        public async Task<IActionResult> Create(int id, int userId)
        {
            var user = _context.User.FromSqlRaw("Select * from [dbo].[User] where UserId = " + userId).FirstOrDefault();
            var Participant = new Activity_Participant();
            Participant.ActivityId = id;
            Participant.UserId = userId;
            if (user.StudentId != null)
            {
                Participant.IsJuri = 0;
            }
            try
            {
                _context.Add(Participant);
                await _context.SaveChangesAsync();
                Alert("Participante Adicionado!", "O utilizador selecionado foi adicionado como um participante na atividade!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "O utilizador selecionado não foi adicionado como um participante na atividade", NotificationType.error);
            }
            

            return RedirectToAction(nameof(AddToActivity), new { id = id });
        }

        // GET: Activity_Participants/Delete/5
        public async Task<IActionResult> Delete(int? id, int? userId)
        {
            if (id == null)
            {
                Alert("Ocorreu um erro!", "A atividade não foi encontrada no sistema", NotificationType.error);
                return RedirectToAction("Details", "Activities", new { id = id });
            }

            var activity_Participant = _context.Activity_Participant.FromSqlRaw("Select * from Activity_Participant where ActivityId = " + id + " and UserId = " + userId).FirstOrDefault();
            if (activity_Participant == null)
            {
                Alert("Ocorreu um erro!", "O utilizador selecionado não é um participante na atividade", NotificationType.error);
                return RedirectToAction("Details", "Activities", new { id = id });
            }

            var activity = _context.Activity.FromSqlRaw("Select * from Activity where ActivityId = " + activity_Participant.ActivityId).FirstOrDefault();
            ViewData["Activity_Participant"] = activity_Participant.Activity_ParticipantId;
            ViewData["Atividade"] = activity.ActivityType + " - " + activity.DateT;
            var user = _context.User.FromSqlRaw("Select * from [dbo].[User] where UserId = " + activity_Participant.UserId).FirstOrDefault();
            ViewData["Participante"] = "";
            if (user.StudentId != null)
            {
                var participant = _context.Student.FromSqlRaw("Select * from Student where StudentNumber = " + user.StudentId).FirstOrDefault();
                ViewData["Participante"] = participant.FirstName + " " + participant.LastName;
                ViewData["StudentNumber"] = participant.StudentNumber;
            }
            else if(user.TeacherId != null)
            {
                var participant = _context.Teacher.FromSqlRaw("Select * from Teacher where TeacherId = " + user.TeacherId).FirstOrDefault();
                ViewData["Participante"] = participant.FirstName + " " + participant.LastName;
            }
            else
            {
                var participant = _context.TO.FromSqlRaw("Select * from [dbo].[TO] where TOId = " + user.TOId).FirstOrDefault();
                ViewData["Participante"] = participant.FirstName + " " + participant.LastName;
            }
            return View();
        }

        // POST: Activity_Participants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var activity_Participant = await _context.Activity_Participant.FindAsync(id);
            try
            {
                _context.Activity_Participant.Remove(activity_Participant);
                await _context.SaveChangesAsync();
                Alert("Participante Removido!", "O utilizador selecionado foi removido da atividade!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro", "O utilizador selecionado não foi removido da atividade!", NotificationType.error);
            }
            
            return RedirectToAction("Details", "Activities", new { id = activity_Participant.ActivityId });
        }

        private bool Activity_ParticipantExists(int id)
        {
            return _context.Activity_Participant.Any(e => e.Activity_ParticipantId == id);
        }
    }
}
