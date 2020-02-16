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
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Security.Claims;
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    /**
     * Controlador que trata das ações dos documentos da atividade
     */
    public class Activity_DocumentController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostingEnvironment _hostingEnvironment;

        public Activity_DocumentController(ApplicationDbContext context, IHostingEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        /**
         * Método que aceita documentos que estão anexados na atividade
         */
        public IActionResult AcceptDocument(int documentId, int activityId)
        {
            using (_context)
            {
                var document = _context.Activity_Document.FromSqlRaw("Select * From [dbo].[Activity_Document] Where Activity_DocumentId = " + documentId).ToList().First();
                document.FlagReject = -1;

                _context.SaveChanges();
            }
            return RedirectToAction("Details", "Activities", new { id = activityId });
        }

        /**
         * Método que rejeita documentos que estão anexados na atividade
         */
        public IActionResult RejectDocument(int documentId, int activityId)
        {
            using (_context)
            {
                var document = _context.Activity_Document.FromSqlRaw("Select * From [dbo].[Activity_Document] Where Activity_DocumentId = " + documentId).ToList().First();
                document.FlagReject = 1;

                _context.SaveChanges();
            }
            return RedirectToAction("Details", "Activities", new { id = activityId });
        }

        // GET: Activity_Document/Create
        public IActionResult Create(int id, int userId)
        {
            ViewBag.UserId = userId;
            ViewBag.ActivityId = id;
            return View();
        }

        // POST: Activity_Document/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile fil, DateTime data, string comments, int userId, int activityId, String DocumentType, String documentName)
        {
            string folderName = "";

            if (DocumentType.Equals("Ata") || DocumentType.Equals("Ata_Corrigida"))
            {
                folderName = "atas";
            } else if (DocumentType.Equals("Relatorio"))
            {
                folderName = "documentos";
            }

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
            string fileName = @"wwwroot/" + folderName + "/" + fiName;
            FileInfo file = new FileInfo(fileName);

            try
            {
                if (DocumentType.Equals("Ata") || DocumentType.Equals("Ata_Corrigida"))
                {
                    var ata = new Ata();
                    ata.MeetingDate = data.Date;
                    ata.FilePath = file.ToString();
                    ata.ActivityId = activityId;
                    if (User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value != "Aluno")
                    {
                        ata.UserId = userId;
                    } else
                    {
                        ata.StudentId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value);
                    }
                    _context.Add(ata);
                } else
                {
                    var document = new Activity_Document();
                    document.DocumentName = documentName;
                    document.DocumentPath = file.ToString();
                    document.SubmitionData = data;
                    document.Comments = comments;
                    document.UserId = userId;
                    document.ActivityId = activityId;
                    _context.Add(document);
                }
                Alert("Documento Adicionado!", "O Documento foi criado com sucesso", NotificationType.success);
                _context.SaveChanges();
            }
            catch
            {
                Alert("Ocorreu um erro", "Não foi possivel adicionar o documento à atividade!", NotificationType.error);
            }

            return RedirectToAction("Details", "Activities", new { id = activityId});
        }

        // GET: Activity_Document/Delete/5
        public async Task<IActionResult> Delete(int? id/*id documento*/, int activityId)
        {
            if (id == null)
            {
                Alert("Ocorreu um erro!", "O documento a eliminar não foi encontrada no sistema", NotificationType.error);
                return RedirectToAction("Details", "Activities", new { id = activityId });
            }
            
            var activity_Document = await _context.Activity_Document
                .FirstOrDefaultAsync(m => m.Activity_DocumentId == id);
            if (activity_Document == null)
            {
                Alert("Ocorreu um erro!", "O documento a eliminar não foi encontrada no sistema", NotificationType.error);
                return RedirectToAction("Details", "Activities", new { id = activityId });
            }
            
            ViewBag.ActivityId = activityId;

            return View(activity_Document);
        }

        // POST: Activity_Document/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int activityId)
        {
            var activity_Document = _context.Activity_Document.FromSqlRaw("Select * From [dbo].[Activity_Document] Where Activity_DocumentId = " + id).ToList().First();
            try
            {
                string folderName = "documentos";
                if (activity_Document.ActivityId == 0)
                {
                    folderName = "templates";
                }

                string webRootPath = _hostingEnvironment.WebRootPath;
                string newPath = Path.Combine(webRootPath, folderName);
                // Delete existing ata paths of the user from Directory
                System.IO.DirectoryInfo di = new DirectoryInfo(newPath);
                foreach (FileInfo filesDelete in di.GetFiles())
                {
                    var name = filesDelete.FullName;
                    var split = name.Split("\\");
                    var finalPath = "wwwroot/" + folderName + "/" + split[split.Length - 1];
                    Console.WriteLine(finalPath);

                    if (finalPath.Equals(activity_Document.DocumentPath))
                    {
                        filesDelete.Delete();
                    }
                }// End Deleting files from directories

                _context.Activity_Document.Remove(activity_Document);
                await _context.SaveChangesAsync();
                Alert("Documento Eliminado", "O documento foi eliminado do sistema com sucesso!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu algum erro!", "Ocorreu um erro ao eliminar o documento do sistema!", NotificationType.error);
            }

            return RedirectToAction("Details", "Activities", new { id = activityId });
        }

        private bool Activity_DocumentExists(int id)
        {
            return _context.Activity_Document.Any(e => e.Activity_DocumentId == id);
        }
    }
}
