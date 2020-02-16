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
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    /**
     * Classe que controla as ações das atas.
     */
    public class AtasController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostingEnvironment _hostingEnvironment;

        public AtasController(ApplicationDbContext context, IHostingEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: All Atas
        public async Task<IActionResult> Index()
        {
            var studentNumber = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;
            var atas = _context.Ata.FromSqlRaw("SELECT * FROM [Ata] where StudentId = " + studentNumber).ToList();
            return View(atas);
        }

        // GET: Atas from student
        public async Task<IActionResult> ViewStudentAtas(int StudentNumber)
        {
            var atas = _context.Ata.FromSqlRaw("SELECT * FROM [Ata] where StudentId = " + StudentNumber).ToList();
            return View("Index", atas);
        }

        /**
         * Método que aceita a ata de um aluno
         */
        public IActionResult AcceptAta(int ataId, int activityId)
        {
            using (_context)
            {
                var ata = _context.Ata.FromSqlRaw("Select * From [dbo].[Ata] Where AtaId = " + ataId).ToList().First();
                ata.FlagReject = -1;

                _context.SaveChanges();
            }

            return RedirectToAction("Details", "Activities", new { id = activityId });
        }
        /**
         * Método que rejeita a ata de um aluno
         */
        public IActionResult RejectAta(int ataId, int activityId)
        {
            using (_context)
            {
                var ata = _context.Ata.FromSqlRaw("Select * From [dbo].[Ata] Where AtaId = " + ataId).ToList().First();
                ata.FlagReject = 1;

                _context.SaveChanges();
            }

            return RedirectToAction("Details", "Activities", new { id = activityId });
        }

        /**
         * Método que abre um ficheiro estilo ata ou relatório
         */
        public async Task<FileResult> OpenFile(int? id)
        {
            var ata = await _context.Ata.FirstOrDefaultAsync(m => m.AtaId == id);
            string fileName = "ata_" + ata.StudentId + "_" + ata.MeetingDate + ".pdf";
            byte[] fileBytes = System.IO.File.ReadAllBytes(ata.FilePath);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        // GET: Atas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Atas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile fil, DateTime data)
        {
            string folderName = "atas";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);

            var studentId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;

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
            string fileName = @"wwwroot/atas/" + fiName;
            FileInfo file = new FileInfo(fileName);

            using (_context)
            {
                var ata = new Ata();
                ata.StudentId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value);
                ata.MeetingDate = data.Date;
                ata.FilePath = file.ToString();
                _context.Add(ata);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // GET: Atas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ata = await _context.Ata
                .FirstOrDefaultAsync(m => m.AtaId == id);
            if (ata == null)
            {
                return NotFound();
            }

            return View(ata);
        }

        // GET: Atas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ata = await _context.Ata.FindAsync(id);
            if (ata == null)
            {
                return NotFound();
            }
            return View(ata);
        }

        // POST: Atas/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int AtaId, IFormFile fil, DateTime data)
        {

            var ata = _context.Ata.FromSqlRaw("SELECT * FROM [dbo].[Ata] where AtaId = " + AtaId).ToList().First();

            if (fil != null)
            {
                string folderName = "atas";
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
                string rootFolder = _hostingEnvironment.WebRootPath;
                string fileName = @"wwwroot/atas/" + fiName;
                FileInfo file = new FileInfo(fileName);

                if (ata.FilePath != null)
                {
                    // Delete existing photo paths of the user from Directory
                    System.IO.DirectoryInfo di = new DirectoryInfo(newPath);
                    foreach (FileInfo filesDelete in di.GetFiles())
                    {
                        var name = filesDelete.FullName;
                        var split = name.Split("\\");
                        var finalPath = "wwwroot/atas/" + split[split.Length - 1];

                        if (finalPath.Equals(ata.FilePath))
                        {
                            filesDelete.Delete();
                        }
                    }// End Deleting files from directories
                }

                ata.FilePath = file.ToString();
            }
            try
            {
                ata.MeetingDate = data;
                _context.SaveChanges();
                Alert("Ata Editada com sucesso!","",NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel editar a Ata pretendida!", NotificationType.error);
            }

            return RedirectToAction("Index");
        }

        // GET: Atas/Delete/5
        public async Task<IActionResult> Delete(int? id, int activityId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ata = await _context.Ata
                .FirstOrDefaultAsync(m => m.AtaId == id);
            if (ata == null)
            {
                return NotFound();
            }

            ViewBag.ActivityId = activityId;

            return View(ata);
        }

        // POST: Atas/Delete/5
        public async Task<IActionResult> DeleteConfirmed(int AtaId, int activityId)
        {
            var ata = await _context.Ata.FindAsync(AtaId);

            string folderName = "atas";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            // Delete existing ata paths of the user from Directory
            System.IO.DirectoryInfo di = new DirectoryInfo(newPath);
            foreach (FileInfo filesDelete in di.GetFiles())
            {
                var name = filesDelete.FullName;
                var split = name.Split("\\");
                var finalPath = "wwwroot/atas/" + split[split.Length - 1];
                Console.WriteLine(finalPath);

                if (finalPath.Equals(ata.FilePath))
                {
                    filesDelete.Delete();
                }
            }// End Deleting files from directories
            try
            {
                _context.Ata.Remove(ata);
                await _context.SaveChangesAsync();
                Alert("Ata Eliminada!", "A Ata selecionada foi eliminada do sistema!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel eliminar a ata do sistema!", NotificationType.error);
            }
            return RedirectToAction("Details", "Activities", new { id = activityId });
        }

        private bool AtaExists(int id)
        {
            return _context.Ata.Any(e => e.AtaId == id);
        }
    }
}
