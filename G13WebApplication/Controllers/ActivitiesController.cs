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
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    /**
     * Controlador que trata de todas as ações relativamente às atividades
     */
    public class ActivitiesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ActivitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Activities
        /**
         * Método que devolve todas as atividades dependendo do utilizador que está logado. 
         */
        public async Task<IActionResult> Index(string? filter)
        {

            var activities = getActivities();
            activities = FilterActivitiesList(activities, filter).ToList();
            return View(activities);    
        }

        public IEnumerable<Activity> getActivities()
        {
            var activities = _context.Activity.ToList();
            var userId = 0;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;
            if (role == "DO")
            {
                var teacherId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
                userId = _context.User.FromSqlRaw("Select * from [dbo].[User] where TeacherId = " + teacherId).ToList().FirstOrDefault().UserId;
            }
            else if (role == "Aluno")
            {
                var studentId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value.ToString());
                userId = _context.User.FromSqlRaw("Select * from [dbo].[User] where StudentId = " + studentId).ToList().FirstOrDefault().UserId;
            }
            else if (role == "TO")
            {
                var toId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
                userId = _context.User.FromSqlRaw("Select * from [dbo].[User] where TOId = " + toId).ToList().FirstOrDefault().UserId;
            }
            if (role != "RUC")
            {
                activities = _context.Activity.FromSqlRaw("Select * from Activity where ActivityId = 0").ToList();
                var activitiesAux = _context.Activity_Participant.FromSqlRaw("Select * from Activity_Participant where UserId = " + userId).ToList();
                foreach (var act in activitiesAux)
                {
                    activities.Add(_context.Activity.FromSqlRaw("Select * from Activity where ActivityId = " + act.ActivityId).FirstOrDefault());
                }
            }

            return activities;
        }

        /**
         * Método que filtra as atividades por palestra/reunião/prova
         */
        private IEnumerable<Activity> FilterActivitiesList(IEnumerable<Activity> activities, String filter)
        {
            List<Activity> activitiesAux = new List<Activity>();
            if (filter == null)
            {
                return activities;
            }
            foreach (var a in activities)
            {
                if (a.ActivityType.Equals(filter))
                {
                    activitiesAux.Add(a);
                }
            }
            return activitiesAux;
        }

        /**
         * Abre o documento estilo ata/relatório
         */
        public async Task<FileResult> OpenDocument(int id)
        {
            var document = await _context.Activity_Document.FirstOrDefaultAsync(m => m.Activity_DocumentId == id);
            string fileName = document.DocumentName + ".pdf";
            byte[] fileBytes = System.IO.File.ReadAllBytes(document.DocumentPath);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        // GET: Activities/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                Alert("Ocorreu um erro!", "A atividade não foi encontrada no sistema", NotificationType.error);
                return View("Index");
            }

            var activity = _context.Activity.FromSqlRaw("Select * from Activity where ActivityId = " + id).FirstOrDefault();
            if (activity == null)
            {
                Alert("Ocorreu um erro!", "A atividade não foi encontrada no sistema", NotificationType.error);
                return View("Index");
            }

            var Participants = _context.Activity_Participant.FromSqlRaw("Select * from Activity_Participant where ActivityId = " + id).ToList();
            var SugestedDates = _context.Activity_Suggested_Date.FromSqlRaw("Select * from Activity_Suggested_Date where ActivityId = " + id).ToList();
            var UsersList = new List<User>();
            foreach (var i in Participants)
            {
                UsersList.Add(_context.User.FromSqlRaw("Select * from [dbo].[User] where UserId = " + i.UserId).ToList().FirstOrDefault());
            }

            var Activities_Documents = _context.Activity_Document.FromSqlRaw("Select * from Activity_Document where ActivityId = " + id).ToList();

            ViewBag.Activity = activity;
            ViewBag.Participantes = Participants;
            ViewBag.Atas = getActivityAtas(activity.ActivityId);
            ViewBag.Activity_Documents = Activities_Documents;
            ViewBag.Dates = SugestedDates;
            
            return View(UsersList);
        }

        /**
         * Método que cancela uma atividade
         */
        public async Task<IActionResult> Cancel(int? id)
        {
            var activity = _context.Activity.FromSqlRaw("Select * from Activity where ActivityId = " + id).FirstOrDefault();
            activity.Canceled = 1;
            _context.SaveChanges();
            Alert("Atividade cancelada!", "Todos os participantes receberão uma notificação " +
                "acerca do cancelamento! Poderá remarcar a atividade a qualquer momento!", NotificationType.success);
            return RedirectToAction(nameof(Index));
        }

        /**
         * Método para continuar a atividade de uma atividade que já foi cancelada
         */
        public async Task<IActionResult> ResumeActivity(int? id)
        {
            var activity = _context.Activity.FromSqlRaw("Select * from Activity where ActivityId = " + id).FirstOrDefault();
            activity.Canceled = 0;
            _context.SaveChanges();
            Alert("Atividade remarcada!", "Todos os participantes receberão uma notificação acerca da remarcação!", NotificationType.success);
            return RedirectToAction(nameof(Index));
        }

        // GET: Activities/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Activities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(String ActivityType, DateTime DateT, String Local)
        {
            var activity = new Activity();
            activity.ActivityType = ActivityType;
            activity.DateT = DateT;
            activity.Local = Local;
            _context.Add(activity);
            _context.SaveChanges();
            var activity_participant = new Activity_Participant();
            activity_participant.ActivityId = activity.ActivityId;
            activity_participant.UserId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value.ToString());
            try
            {
                _context.Add(activity_participant);
                _context.SaveChanges();
                Alert("Atividade Criada!", "Foi criada uma " +  ActivityType + " para dia ", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "A atividade pretendida não foi criada!", NotificationType.error);
            }
            
            return RedirectToAction(nameof(Details), new { id = activity.ActivityId });
        }

        // GET: Activities/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                Alert("Ocorreu um erro!", "A atividade não foi encontrada no sistema", NotificationType.error);
                return View("Index");
            }

            var activity = await _context.Activity.FindAsync(id);
            if (activity == null)
            {
                Alert("Ocorreu um erro!", "A atividade não foi encontrada no sistema", NotificationType.error);
                return View("Index");
            }
            return View(activity);
        }

        // POST: Activities/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ActivityId,ActivityType,DateT,Local")] Activity activity)
        {
            if (id != activity.ActivityId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(activity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActivityExists(activity.ActivityId))
                    {
                        Alert("Ocorreu um erro!", "A atividade não foi encontrada no sistema", NotificationType.error);
                        return View("Index");
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(activity);
        }

        private bool ActivityExists(int id)
        {
            return _context.Activity.Any(e => e.ActivityId == id);
        }

        /**
         * Devolve todas as atas de uma certa atividade recebida por parâmetro
         */
        private IEnumerable<Ata> getActivityAtas(int ActivityId)
        {
            var atas = new List<Ata>();
            var allAtas = _context.Ata.FromSqlRaw("Select * from [dbo].[Ata]").ToList();
            var activity = _context.Activity.FromSqlRaw("Select * from Activity where ActivityId = " + ActivityId).ToList().FirstOrDefault();

            foreach (var a in allAtas)
            {
                if (a.ActivityId == activity.ActivityId)
                {
                    atas.Add(a);
                }
            }

            return atas;
        }

        /**
         * Método que aceita uma sugestão de data, recebendo o id da sugestão por parâmetro
         */
        public IActionResult AcceptSuggestion(int? id, int? userId)
        {
            var sugestedDates = _context.Activity_Suggested_Date
                .FromSqlRaw("Select * from [dbo].[Activity_Suggested_Date] where ActivityId = " + id + " and UserId = " + userId).FirstOrDefault();
            if (sugestedDates.Suggested_Date > DateTime.Now)
            {
                var Activity = _context.Activity.FromSqlRaw("Select * from [dbo].[Activity] where ActivityId = " + id).FirstOrDefault();
                Activity.DateT = sugestedDates.Suggested_Date;
                _context.Remove(sugestedDates);
                _context.SaveChanges();
            }
            return RedirectToAction("Details", new { id = id });
        }

        /**
         * Método que rejeita uma sugestão de data, recebendo o id da sugestão por parâmetro.
         */
        public IActionResult RejectSuggestion(int? id, int? userId)
        {
            var sugestedDates = _context.Activity_Suggested_Date
                .FromSqlRaw("Select * from [dbo].[Activity_Suggested_Date] where ActivityId = " + id + " and UserId = " + userId).FirstOrDefault();
            sugestedDates.Accepted = -1;
            _context.SaveChanges();
            return RedirectToAction("Details", new { id = id });
        }

        /**
         * Método que devolve a view para uma nova data de prova pública.
         */
        public IActionResult SuggestNewDate(int? id)
        {
            if (id == null)
            {
                Alert("Ocorreu um erro!", "A atividade não foi encontrada no sistema", NotificationType.error);
                return View("Index");
            }

            var activity = _context.Activity.FirstOrDefaultAsync(m => m.ActivityId == id);
            if (activity == null)
            {
                Alert("Ocorreu um erro!", "A atividade não foi encontrada no sistema", NotificationType.error);
                return View("Index");
            }
            return View("SuggestNewDate", id);
        }

        /**
         * Método que confirma a nova data sugerida
         */
        public IActionResult SuggestNewDateConfirmed(int id, DateTime DateT)
        {
            var suggestedDate = new Activity_Suggested_Date();
            suggestedDate.ActivityId = id;
            suggestedDate.UserId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value.ToString());
            suggestedDate.Suggested_Date = DateT;
            _context.Add(suggestedDate);
            _context.SaveChanges();
            return RedirectToAction("Details", new { id = id});
        }
    }
}