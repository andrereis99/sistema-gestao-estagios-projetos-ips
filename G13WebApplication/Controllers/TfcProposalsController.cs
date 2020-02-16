using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using G13WebApplication.Data;
using G13WebApplication.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    /**
     * Classe que controla todas as ações das propostas de tfc
     */
    public class TfcProposalsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostingEnvironment _hostingEnvironment;

        public TfcProposalsController(ApplicationDbContext context, IHostingEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: TfcProposals
        public async Task<IActionResult> Index()
        {
            return View(await _context.TfcProposal.ToListAsync());
        }

        public IActionResult ProposeNewTfc()
        {
            return View();
        }

        /**
         * Devolve o ficheiro de proposta de tfc externo
         */
        public async Task<FileResult> GetExternalProposalFile(int? StudentNumber)
        {
            WorkPlan workplan = _context.WorkPlan
                .FromSqlRaw("Select * from WorkPlan as wp join Student as s on wp.PlanId = s.PlanIdFk where s.StudentNumber = " + StudentNumber)
                .ToList().FirstOrDefault();
            string fileName = workplan.TfcType + "_" + StudentNumber + ".pdf";
            byte[] fileBytes = System.IO.File.ReadAllBytes(workplan.PlanFile);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        /**
         * Método para criar uma proposta de tfc
         */
        public async Task<IActionResult> Create(int id)
        {
            var tfc = _context.Tfc.FromSqlRaw("Select * from Tfc where TfcId = " + id).ToList().FirstOrDefault();
            var studentNumber = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value.ToString());
            var workplanList = _context.WorkPlan
                .FromSqlRaw("Select * from WorkPlan as wp join Student as s on wp.PlanId = s.PlanIdFk where s.StudentNumber = " + studentNumber)
                .ToList();


            if (workplanList.Count == 0)
            {
                Alert("Ocorreu um erro!", "Tem de escolher um tipo de TFC primeiro!", NotificationType.error);
                return RedirectToAction("Index", "Tfcs");
            }

            WorkPlan workplan = workplanList.FirstOrDefault();

            if (tfc == null || studentNumber == 0 || workplan == null || workplan != null && !workplan.TfcType.Equals(tfc.TfcType))
            {
                Alert("Ocorreu um erro!", "Tem que escolher um tipo de TFC primeiro!", NotificationType.error);
                return RedirectToAction("Index", "Tfcs");
            }

            string folderName = "StudentPlanFiles";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);

            if (workplan.PlanFile != null)
            {
                // Delete existing file paths of the student from Directory
                System.IO.DirectoryInfo di = new DirectoryInfo(newPath);
                foreach (FileInfo filesDelete in di.GetFiles())
                {
                    var name = filesDelete.FullName;
                    var split = name.Split("\\");
                    var finalPath = "wwwroot/StudentPlanFiles/" + split[split.Length - 1];

                    if (finalPath.Equals(workplan.PlanFile))
                    {
                        filesDelete.Delete();
                    }
                }// End Deleting files from directories
            }

            try
            {
                if (workplan == null)
                {
                    workplan = new WorkPlan();
                    workplan.TfcType = tfc.TfcType;
                    _context.Add(workplan);
                    var student = _context.WorkPlan.FromSqlRaw("Select * from Student where StudentNumber = " + studentNumber).ToList().FirstOrDefault();
                    student.PlanId = workplan.PlanId;
                }

                workplan.Confirmed = 0;
                workplan.PlanFile = null;
                workplan.TfcIdFk = null;

                var oldTfcProposal = _context.TfcProposal.FromSqlRaw("Select * from TfcProposal where StudentNumber = " + studentNumber).ToList().FirstOrDefault();
                
                if (oldTfcProposal == null)
                {
                    TfcProposal tfcProposal = new TfcProposal();
                    tfcProposal.StudentNumber = studentNumber;
                    tfcProposal.TfcId = id;
                    _context.Add(tfcProposal);

                    foreach (var ruc in getRUCs())
                    {
                        Notification notification = new Notification { Message = "Recebeu um novo pedido para escolha de TFC do aluno " + studentNumber + ". Vá ao paínel de propostas de TFC, para aceitar/rejeitar o pedido", state = "fechado", AddedOn = DateTime.Now, UserId = ruc.UserId, ReadNotification = 0 };
                        
                        _context.Add(notification);
                    }

                    _context.SaveChanges();
                }
                else
                {
                    oldTfcProposal.TfcId = id;
                    
                    foreach (var ruc in getRUCs())
                    {
                        Notification notification = new Notification { Message = "A proposta para escolha de TFC do aluno " + studentNumber + " foi alterada. Vá ao paínel de propostas de TFC, para aceitar/rejeitar o pedido", state = "fechado", AddedOn = DateTime.Now, UserId = ruc.UserId, ReadNotification = 0 };
                        
                        _context.Add(notification);
                    }

                    _context.SaveChanges();
                }
                Alert("Proposata enviada!", "A proposta para " + tfc.TfcType + " foi enviada ao responsável da UC!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel enviar a proposta!", NotificationType.error);
            }

            return RedirectToAction("Index", "Tfcs");
        }

        // POST: TfcProposals/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromNew(String TfcType, IFormFile fil)
        {
            var studentNumber = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value.ToString());
            WorkPlan workplan = _context.WorkPlan
                .FromSqlRaw("Select * from WorkPlan as wp join Student as s on wp.PlanId = s.PlanIdFk where s.StudentNumber = " + studentNumber)
                .ToList().FirstOrDefault();
            if (studentNumber == 0 || workplan != null && !workplan.TfcType.Equals(TfcType) || fil == null)
            {
                return NotFound();
            }

            string folderName = "StudentPlanFiles";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);

            if (!Directory.Exists(newPath))// Create New Directory if not exist as per the path
            {
                Directory.CreateDirectory(newPath);
            }
            var fiName = Guid.NewGuid().ToString() + Path.GetExtension(fil.FileName);
            using (var fileStream = new FileStream(Path.Combine(newPath, fiName), FileMode.Create))
            {
                fil.CopyTo(fileStream);
            }
            // Get uploaded file path with root
            string fileName = @"wwwroot/StudentPlanFiles/" + fiName;
            FileInfo file = new FileInfo(fileName);
            Console.WriteLine("Plan Path" + workplan.PlanFile);

            if (workplan.PlanFile != null)
            {
                // Delete existing file paths of the student from Directory
                System.IO.DirectoryInfo di = new DirectoryInfo(newPath);
                foreach (FileInfo filesDelete in di.GetFiles())
                {
                    var name = filesDelete.FullName;
                    var split = name.Split("\\");
                    var finalPath = "wwwroot/StudentPlanFiles/" + split[split.Length - 1];

                    if (finalPath.Equals(workplan.PlanFile))
                    {
                        filesDelete.Delete();
                    }
                }// End Deleting files from directories
            }

            try
            {
                if (workplan == null)
                {
                    workplan = new WorkPlan();
                    workplan.TfcType = TfcType;
                    _context.Add(workplan);
                    var student = _context.WorkPlan.FromSqlRaw("Select * from Student where StudentNumber = " + studentNumber).ToList().FirstOrDefault();
                    student.PlanId = workplan.PlanId;
                }

                workplan.Confirmed = 0;
                workplan.PlanFile = file.ToString();
                workplan.TfcIdFk = null;
                _context.SaveChanges();

                var oldTfcProposal = _context.TfcProposal.FromSqlRaw("Select * from TfcProposal where StudentNumber = " + studentNumber).ToList().FirstOrDefault();
                if (oldTfcProposal == null)
                {
                    TfcProposal tfcProposal = new TfcProposal();
                    tfcProposal.StudentNumber = studentNumber;
                    _context.Add(tfcProposal);

                    foreach (var ruc in getRUCs())
                    {
                        Notification notification = new Notification { Message = "Recebeu um novo pedido para escolha de TFC do aluno " + studentNumber + ". Vá ao paínel de propostas de TFC, para aceitar/rejeitar o pedido", state = "fechado", AddedOn = DateTime.Now, UserId = ruc.UserId, ReadNotification = 0 };

                        _context.Add(notification);
                    }

                    _context.SaveChanges();
                }
                else
                {
                    oldTfcProposal.TfcId = 0;
                    foreach (var ruc in getRUCs())
                    {
                        Notification notification = new Notification { Message = "A proposta para escolha de TFC do aluno " + studentNumber + " foi alterada. Vá ao paínel de propostas de TFC, para aceitar/rejeitar o pedido", state = "fechado", AddedOn = DateTime.Now, UserId = ruc.UserId, ReadNotification = 0 };

                        _context.Add(notification);
                    }

                    _context.SaveChanges();
                }
                Alert("Proposta Enviada!", "A sua proposta para um tfc externo foi enviada com sucesso!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel enviar a tua proposta para tfc! Por favor tenta mais tarde!", NotificationType.error);
            }

            return RedirectToAction("Index", "Tfcs");
        }

        // GET: TfcProposals/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var propose = _context.TfcProposal.FromSqlRaw("Select * from TfcProposal where TfcProposalId = " + id).ToList().FirstOrDefault();
            var tfc = await _context.Tfc
                .FirstOrDefaultAsync(m => m.TfcId == propose.TfcId);

            ViewData["StudentNumber"] = propose.StudentNumber;

            if (tfc == null)
            {
                return View("DetailsExternal", propose);
            }

            return View("DetailsInternal", tfc);
        }

        /**
         * Método que aceita uma proposta de TFC
         */
        public async Task<IActionResult> accept(int? id)
        {
            var propose = await _context.TfcProposal.FindAsync(id);
            WorkPlan workplan = _context.WorkPlan
                .FromSqlRaw("Select * from WorkPlan as wp join Student as s on wp.PlanId = s.PlanIdFk where s.StudentNumber = " + propose.StudentNumber)
                .ToList().FirstOrDefault();

            if (propose.TfcId == 0)
            {
                workplan.Confirmed = 1;
            }
            else
            {
                workplan.Confirmed = 1;
                workplan.TfcIdFk = propose.TfcId;
            }

            User student = _context.User.FromSqlRaw("Select * from [dbo].[User] where StudentId = " + propose.StudentNumber).ToList().FirstOrDefault();
            Notification notification = new Notification { Message = "A sua proposta para escolha de TFC foi aceite pelo responsável.", 
                state = "fechado", AddedOn = DateTime.Now, UserId = student.UserId, ReadNotification = 0 };
            try
            {
                _context.Add(notification);
                _context.TfcProposal.Remove(propose);
                await _context.SaveChangesAsync();
                Alert("Proposta aceite!", "A proposta para realização do trabalho finar de curso de " + student.StudentId + " foi aceite com sucesso!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel aceitar a proposta de " + student.StudentId + "!", NotificationType.error);
            }

            return RedirectToAction(nameof(Index));
        }
        /**
         * Método que rejeita uma proposta de TFC
         */
        public async Task<IActionResult> refuse(int? id)
        {
            var propose = await _context.TfcProposal.FindAsync(id);
            User student = _context.User.FromSqlRaw("Select * from [dbo].[User] where StudentId = " + propose.StudentNumber).ToList().FirstOrDefault();
            Notification notification = new Notification { Message = "A sua proposta para escolha de TFC foi reprovada pelo responsável.", 
                state = "fechado", AddedOn = DateTime.Now, UserId = student.UserId, ReadNotification = 0 };
            try
            {
                _context.Add(notification);
                _context.TfcProposal.Remove(propose);
                await _context.SaveChangesAsync();
                Alert("Proposta Recusada!", "A proposta para realização do trabalho finar de curso de " + student.StudentId + " foi recusada!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel recusar a proposta de " + student.StudentId + "!", NotificationType.error);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TfcProposalExists(int id)
        {
            return _context.TfcProposal.Any(e => e.TfcProposalId == id);
        }

        /**
         * Devolve todos os utilizadores que são RUC
         */
        private IEnumerable<User> getRUCs()
        {
            var RUCTeachers = _context.Teacher.FromSqlRaw("Select * from [dbo].[Teacher] where Role = 'RUC'").ToList();
            var RUCs = _context.User.FromSqlRaw("Select * from [dbo].[User] where TeacherId = 0").ToList();
            foreach (var ruc in RUCTeachers)
            {
                var auxUserRUC = _context.User.FromSqlRaw("Select * from [dbo].[User] where TeacherId = " + ruc.TeacherId).ToList().FirstOrDefault();
                RUCs.Add(auxUserRUC);
            }
            return RUCs;
        }
    }
}
