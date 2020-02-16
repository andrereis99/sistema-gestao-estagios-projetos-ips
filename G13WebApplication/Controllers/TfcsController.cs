using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using G13WebApplication.Data;
using G13WebApplication.Models;
using OfficeOpenXml;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Claims;
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    /**
     * Classe que controla as açôes relacionadas com os tfcs
     */
    public class TfcsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostingEnvironment _hostingEnvironment;

        public TfcsController(ApplicationDbContext context, IHostingEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        

        // GET: Tfcs
        public async Task<IActionResult> Index()
        {
            return View(await _context.Tfc.ToListAsync());
        }

        /*public async Task<IActionResult> TfcStudentIndex()
        {
            return View(await _context.Tfc.ToListAsync());
        }*/

        public async Task<FileResult> OpenFile(int? id)
        {
            var tfc = await _context.Tfc.FirstOrDefaultAsync(m => m.TfcId == id);
            string fileName = tfc.TfcType + "_" + tfc.Name + ".pdf";
            byte[] fileBytes = System.IO.File.ReadAllBytes(tfc.TfcFile);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        // GET: Tfcs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                Alert("Ocorreu um erro!", "Não foi possivel encontrar o tfc selecionado!", NotificationType.error);
                return RedirectToAction("Index");
            }

            var tfc = await _context.Tfc
                .FirstOrDefaultAsync(m => m.TfcId == id);
            if (tfc == null)
            {
                Alert("Ocorreu um erro!", "Não foi possivel encontrar o tfc selecionado!", NotificationType.error);
                return RedirectToAction("Index");
            }

            return View(tfc);
        }

        // GET: Tfcs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tfcs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(String TfcType, String Name, String Details, 
                                                String Company, String Location, IFormFile fil)
        {
            var tfc = new Tfc();

            if (fil != null)
            {
                string folderName = "Tfcs";
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
                string fileName = @"wwwroot/Tfcs/" + fiName;
                FileInfo file = new FileInfo(fileName);
                tfc.TfcFile = file.ToString();
            }

            using (_context)
            {
                tfc.TfcType = TfcType;
                tfc.Name = Name;
                tfc.Details = Details;
                tfc.Company = Company;
                tfc.Location = Location;
                _context.Add(tfc);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // GET: Tfcs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                Alert("Ocorreu um erro!", "Não foi possivel encontrar o tfc selecionado!", NotificationType.error);
                return RedirectToAction("Index");
            }

            var tfc = await _context.Tfc.FindAsync(id);
            if (tfc == null)
            {
                Alert("Ocorreu um erro!", "Não foi possivel encontrar o tfc selecionado!", NotificationType.error);
                return RedirectToAction("Index");
            }
            return View(tfc);
        }

        // POST: Tfcs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int TfcId, String TfcType, String Name, String Details,
                                                String Company, String Location, IFormFile fil)
        {
            var tfc = _context.Tfc.FromSqlRaw("SELECT * FROM [dbo].[Tfc] WHERE TfcId = " + TfcId).ToList().First();

            if (fil != null)
            {
                string folderName = "Tfcs";
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
                string fileName = @"wwwroot/Tfcs/" + fiName;
                FileInfo file = new FileInfo(fileName);

                if (tfc.TfcFile != null)
                {
                    // Delete existing photo paths of the user from Directory
                    System.IO.DirectoryInfo di = new DirectoryInfo(newPath);
                    foreach (FileInfo filesDelete in di.GetFiles())
                    {
                        var name = filesDelete.FullName;
                        var split = name.Split("\\");
                        var finalPath = "wwwroot/Tfcs/" + split[split.Length - 1];

                        if (finalPath.Equals(tfc.TfcFile))
                        {
                            filesDelete.Delete();
                        }
                    }// End Deleting files from directories
                }

                tfc.TfcFile = file.ToString();
            }
            try
            {
                tfc.TfcType = TfcType;
                tfc.Name = Name;
                tfc.Details = Details;
                tfc.Company = Company;
                tfc.Location = Location;
                _context.SaveChanges();
                Alert("Tfc Editado!", "O " + TfcType + " selecionado foi editado com sucesso!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel alterar o " + TfcType + " selecionado!", NotificationType.error);
            }

            return RedirectToAction("Index");
        }

        // GET: Tfcs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tfc = await _context.Tfc
                .FirstOrDefaultAsync(m => m.TfcId == id);
            if (tfc == null)
            {
                return NotFound();
            }

            return View(tfc);
        }

        // POST: Tfcs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int TfcId)
        {
            var tfc = await _context.Tfc.FindAsync(TfcId);

            if (tfc.TfcFile != null)
            {
                string folderName = "Tfcs";
                string webRootPath = _hostingEnvironment.WebRootPath;
                string newPath = Path.Combine(webRootPath, folderName);
                // Delete existing photo paths of the user from Directory
                System.IO.DirectoryInfo di = new DirectoryInfo(newPath);
                foreach (FileInfo filesDelete in di.GetFiles())
                {
                    var name = filesDelete.FullName;
                    var split = name.Split("\\");
                    var finalPath = "wwwroot/Tfcs/" + split[split.Length - 1];

                    if (finalPath.Equals(tfc.TfcFile))
                    {
                        filesDelete.Delete();
                    }
                }// End Deleting files from directories
            }

            try
            {
                _context.Tfc.Remove(tfc);
                await _context.SaveChangesAsync();
                Alert("TFC removido!", "O tfc foi removido com sucesso!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel alterar o tfc selecionado!", NotificationType.error);
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TfcExists(int id)
        {
            return _context.Tfc.Any(e => e.TfcId == id);
        }

        /**
         * Método que cria uma pasta para guardar o excel com os tfcs em wwwroot, e utiliza esse excel para 
         * descarregar todos os dados para a base de dados
         */
        [HttpPost]
        [Route("Tfcs/Index")]
        public IActionResult ImportUpload(IFormFile reportfile)
        {
            string folderName = "UploadTFC";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            // Delete Files from Directory
            System.IO.DirectoryInfo di = new DirectoryInfo(newPath);
            foreach (FileInfo filesDelete in di.GetFiles())
            {
                filesDelete.Delete();
            }// End Deleting files form directories

            if (!Directory.Exists(newPath))// Crate New Directory if not exist as per the path
            {
                Directory.CreateDirectory(newPath);
            }
            var fiName = Guid.NewGuid().ToString() + Path.GetExtension(reportfile.FileName);
            using (var fileStream = new FileStream(Path.Combine(newPath, fiName), FileMode.Create))
            {
                reportfile.CopyTo(fileStream);
            }
            // Get uploaded file path with root
            string rootFolder = _hostingEnvironment.WebRootPath;
            string fileName = @"UploadTFC/" + fiName;
            FileInfo file = new FileInfo(Path.Combine(rootFolder, fileName));

            using (ExcelPackage package = new ExcelPackage(file))
            {
                ExcelWorksheet workSheet = package.Workbook.Worksheets[0];
                int totalRows = workSheet.Dimension.Rows;
                List<Tfc> reportList = new List<Tfc>();
                for (int i = 2; i <= totalRows + 1; i++)
                {
                    try
                    {
                        string TfcType = workSheet?.Cells[i, 1]?.Value?.ToString();
                        string Name = workSheet?.Cells[i, 2]?.Value?.ToString();
                        string Details = workSheet?.Cells[i, 3]?.Value?.ToString();
                        string Company = workSheet?.Cells[i, 4]?.Value?.ToString();
                        string Location = workSheet?.Cells[i, 5]?.Value?.ToString();
                        string TfcFile = workSheet?.Cells[i, 6]?.Value?.ToString();
                        if (TfcType != null)
                        {
                            reportList.Add(new Tfc
                            {
                                TfcType = TfcType,
                                Name = Name,
                                Details = Details,
                                Company = Company,
                                Location = Location,
                                TfcFile = TfcFile,
                            });


                        }

                    }
                    catch (Exception Ex)
                    {
                        return RedirectToAction("Index", "Tfcs");
                    }
                }
                using (_context)
                {
                    _context.Tfc.AddRange(reportList);
                    _context.SaveChanges();
                    return RedirectToAction("Index", "Tfcs");
                }
            }
        }

        /**
         * Retorna a view de procura de um tfc como ruc
         */
        public async Task<IActionResult> SearchTFCasRUC(int? id)
        {
            ViewData["studentId"] = id;
            return View(await _context.Tfc.ToListAsync());
        }

        /**
         * Método que atribui um tfc a um certo aluno recebido por parametro, sendo um RUC
         */
        public async Task<IActionResult> AtribuirTFCasRUC(int? id, int? studentId)
        {
            var student = _context.Student.FromSqlRaw("Select * from Student where StudentId = " + studentId).ToList().FirstOrDefault();
            WorkPlan workplan = _context.WorkPlan
                .FromSqlRaw("Select * from WorkPlan as wp join Student as s on wp.PlanId = s.PlanIdFk where s.StudentNumber = " + student.StudentNumber)
                .ToList().FirstOrDefault();
            var tfc = _context.Tfc.FromSqlRaw("Select * from Tfc where TfcId = " + id).ToList().FirstOrDefault();

            if (workplan == null || workplan.TfcType != tfc.TfcType)
            {
                return NotFound();
            }
            var pedidoTfc = _context.TfcProposal.FromSqlRaw("Select * from TfcProposal where StudentNumber = " + student.StudentNumber).ToList().FirstOrDefault();

            try
            {
                workplan.TfcIdFk = id;
                workplan.Confirmed = 1;
                if (pedidoTfc != null)
                {
                    _context.Remove(pedidoTfc);
                }
                _context.SaveChanges();
                Alert("Tfc Atribuido!", "O tfc selecionado foi atribuido a " + student.FirstName + " com sucesso!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel atribuir o tfc a " + student.FirstName + "!", NotificationType.error);
            }

            return RedirectToAction("Index", "Teachers");
        }

        public async Task<ActionResult> SelectTfc(TO newTO)
        {
            ViewData["newTOId"] = newTO.TOId;
            return View(await _context.Tfc.ToListAsync());
        }

        /**
         * Método que atribui um tfc a um TO.
         */
        public IActionResult ChooseTFCtoTO(int? id, int? toId)
        {
            var to = _context.TO.FromSqlRaw("Select * from [dbo].[TO] where TOId = " + toId).ToList().FirstOrDefault();

            var tfc = _context.Tfc.FromSqlRaw("Select * from [dbo].[Tfc] where TfcId = " + id).ToList().FirstOrDefault();

            if (to == null || tfc == null)
            {
                return NotFound();
            }

            to.TfcIdFk = id;

            _context.Update(to);
            _context.SaveChanges();

            return RedirectToAction("Index", "Teachers");
        }
    }
}
