using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using G13WebApplication.Models;
using System.IO;
using Microsoft.AspNetCore.Http;
using G13WebApplication.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Text;
using System.Security.Cryptography;
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    /**
     * Classe que trata das ações executadas no ambito da homepage e algumas ações dos estudantes.
     */
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IHostingEnvironment _hostingEnvironment;

        public HomeController(ILogger<HomeController> logger, IHostingEnvironment hostingEnvironment, ApplicationDbContext db)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        /**
         * Devolve o chat dando set no nome do user que entrou no chat igual ao seu primeiro nome no site.
         */
        public IActionResult Chat()
        {
            var UserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
            var user = _db.User.FromSqlRaw("Select * from[dbo].[User] where UserId = " + UserId).ToList().First();
            if (User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value.Equals("Aluno"))
            {
                var student = _db.Student.FromSqlRaw("Select * from [dbo].[Student] where StudentNumber = " + user.StudentId).ToList().First();
                var username = student.FirstName;
                ViewBag.Username = student.FirstName;
            }
            else
            {
                var teacher = _db.Teacher.FromSqlRaw("Select * from [dbo].[Teacher] where TeacherId = " + user.TeacherId).ToList().First();
                var username = teacher.FirstName;
                ViewBag.Username = teacher.FirstName;
            }

            return View("~/Views/Chat_Views/Chat.cshtml");
        }

        /**
         * Menu dos documentos para auxilio dos alunos 
         */
        public IActionResult Documents()
        {
            var templates = _db.Activity_Document.FromSqlRaw("Select * From [dbo].[Activity_Document] Where ActivityId = 0").ToList();

            return View(templates);
        }

        /**
         * Método para adicionar um template ao menu de templates
         */
        public IActionResult AddTemplate()
        {
            ViewBag.UserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
            ViewBag.ActivityId = 0;
            return View("Create");
        }

        /**
         * Cria um novo template com o ficheiro respetivo
         */
        public IActionResult CreateTemplate(IFormFile fil, DateTime data, string comments, int userId, int activityId, String documentName)
        {
            string folderName = "templates";

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

            using (_db)
            {
                var document = new Activity_Document();
                document.DocumentName = documentName;
                document.DocumentPath = file.ToString();
                document.SubmitionData = DateTime.Now;
                document.Comments = comments;
                document.UserId = userId;
                document.ActivityId = activityId;
                document.FlagReject = 1;

                _db.Add(document);

                _db.SaveChanges();
            }

            return RedirectToAction("Documents", "Home");
        }

        /**
         * Abre o ficheiro template (para alunos etc.)
         */
        public async Task<FileResult> OpenTemplate(String name)
        {
            var template = await _db.Activity_Document.FirstOrDefaultAsync(m => m.DocumentName.Equals(name));
            string fileName = name + "." + template.DocumentPath.Split('.')[1];
            byte[] fileBytes = System.IO.File.ReadAllBytes(template.DocumentPath);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity_Document = await _db.Activity_Document
                .FirstOrDefaultAsync(m => m.Activity_DocumentId == id);
            if (activity_Document == null)
            {
                return NotFound();
            }

            return View(activity_Document);
        }

        // POST: Activity_Document/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var activity_Document = _db.Activity_Document.FromSqlRaw("Select * From [dbo].[Activity_Document] Where Activity_DocumentId = " + id).ToList().First();
            try
            {
                _db.Activity_Document.Remove(activity_Document);
                await _db.SaveChangesAsync();
                Alert("Documento Eliminado!", "O documento foi eliminado com sucesso!", NotificationType.success);
            }
            catch 
            {
                Alert("Ocorreu um erro!", "Não foi possivel eliminar o documento!", NotificationType.error);
            }

            return RedirectToAction("Documents", "Home");
        }

        /*
         * Método utilizado para fazer login numa conta, recebe como parametros o email e password
         * Para auxilio nos restantes métodos atribui-se um claimtypes em cada utilizador
         * dependendo do seu role.
         */
        public async Task<IActionResult> Login(string userEmail, string userPassword)
        {
            bool found_user = false;
            bool correct_password = false;

            var usersList = _db.User.FromSqlRaw("SELECT * FROM [User]").ToList();

            foreach (User u in usersList)
            {
                if (u.Email.Equals(userEmail))
                {
                    found_user = true;
                    if (u.Password.Equals(userPassword))
                    {
                        correct_password = true;

                        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, u.UserId.ToString()));
                        identity.AddClaim(new Claim(ClaimTypes.Email, u.Email));

                        if (u.TeacherId != null)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Name, "0"));
                            identity.AddClaim(new Claim(ClaimTypes.Actor, u.TeacherId.ToString()));
                            var teachersList = _db.Teacher.FromSqlRaw("SELECT * FROM [Teacher] WHERE TeacherId = " + u.TeacherId).ToList();
                            identity.AddClaim(new Claim(ClaimTypes.Role, teachersList.First().Role));
                        } else
                        {
                            if(u.TOId == null) { 

                            identity.AddClaim(new Claim(ClaimTypes.Name, u.StudentId.ToString()));
                            identity.AddClaim(new Claim(ClaimTypes.Actor, "0"));
                            identity.AddClaim(new Claim(ClaimTypes.Role, "Aluno"));
                            } else
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Name, "0"));
                                identity.AddClaim(new Claim(ClaimTypes.Actor, u.TOId.ToString()));
                                identity.AddClaim(new Claim(ClaimTypes.Role, "TO"));
                            }
                        }

                        var principal = new ClaimsPrincipal(identity);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = false });

                        if (u.TeacherId != null)
                        {
                            return RedirectToAction("Index", "Teachers");
                        } else
                        {
                            if(u.TOId == null) { 
                                return RedirectToAction("Index", "Students");
                            }
                            else
                            {
                                return RedirectToAction("Index", "TOs");
                            }
                        }
                    }
                }
            }

            if (!found_user)
            {
                //Email doesn't exist
                TempData["WrongEmail"] = true;
            }
            else if (found_user && !correct_password)
            {
                //Email found but current password incorrect
                TempData["WrongPass"] = true;
            }

            return View();
        }

        /*
         * Redireciona para o perfil do utilizador dependendo de cada role do user.
         */
        public IActionResult Perfil()
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;

            if (role == "Aluno"){
                return RedirectToAction("Perfil", "Students");
            } else if (role == "TO") {
                return RedirectToAction("Perfil", "TOs");
            } else {
                return RedirectToAction("Perfil", "Teachers");
            }
        }

        public IActionResult GoToLogin()
        {
            return View("Login");
        }

        public IActionResult Help()
        {
            var userGuide = _db.UserGuide.FromSqlRaw("Select TOP 1 * from[dbo].[UserGuide] order by UserGuideId desc").ToList();
            return View("Help", userGuide.First());
        }

        public IActionResult GoToPasswordRecovery()
        {
            return View("PasswordRecov");
        }

        /**
         * Método que envia mail aos estudantes para alteraçâo de password
         */
        private void SendEmailToStudent(string strEmailStudent)
        {
            string strSubject = "[G13-IPS] Alteração de password";
            string strLink = "https://g13webapplication20200103114626.azurewebsites.net/Home/PasswordRecovery"; //mudar quando for cloud

            var strbBody = new StringBuilder();
            strbBody.AppendLine("Caro utilizador,<br><br>");
            strbBody.AppendFormat(@"O seu pedido de alteração de password foi recebido.<br><br>");
            strbBody.AppendLine("Clique <a href=\"" + strLink + "\">aqui</a> para completar a alteração.<br>");
            strbBody.AppendLine("Caso não tenha efetuado nenhum pedido de alteração de password, por favor, contacte a equipa do G13-IPS");


            strbBody.AppendLine("Cumprimentos, <br> A Equipa do G13-IPS.");
            Email.SendEmail(strEmailStudent, strSubject, strbBody.ToString());
        }

        /**
         * Método para auxilio à recuperação da password
         */
        public  IActionResult PasswordRecov(string userEmail)
        {
            bool found_user = false;
            using (_db)
            {
                var usersList = _db.User.FromSqlRaw("SELECT * FROM [dbo].[User]").ToList();
                foreach (User u in usersList)
                {
                    if (u.Email.Equals(userEmail))
                    {
                        found_user = true;
                    }
                }

                if (!found_user)
                {
                    Alert("Ocorreu um erro!", "O email inserido não está registado no sistema!", NotificationType.error);
                    return View();
                }
                else if (found_user)
                {
                    SendEmailToStudent(userEmail); //envio do email

                }
                Alert("Email Enviado!", "Foi enviado um email de confirmação para alteração da password!", NotificationType.success);
                return RedirectToAction("Login");
            }
        }

        /**
         * Método para auxilio à recuperação da password
         */
        public IActionResult PasswordRecovery(string userEmail, string newPass)
        {
            bool found_user = false;

            using (_db)
            {
                var usersList = _db.User.FromSqlRaw("SELECT * FROM [dbo].[User]").ToList();

                foreach (User u in usersList)
                {
                    if (u.Email.Equals(userEmail))
                    {
                        found_user = true;        
                        u.Password = newPass;            //troca da password do utilizador
                        _db.SaveChanges();
                        return View("Login");
                    }
                }

                return View();
            }
        }
        private const int NEW_PW_MAX_LENGTH = 8;

        /**
         * Geração de uma password aleatória
         */
        private string GenerateRandomPassword()
        {
            RNGCryptoServiceProvider newpw = new RNGCryptoServiceProvider();

            byte[] arrTokenBuffer = new byte[NEW_PW_MAX_LENGTH];
            newpw.GetBytes(arrTokenBuffer);
            return Convert.ToBase64String(arrTokenBuffer).ToLower();
        }

        /**
         * Método que importa um ficheiro excel para uma pasta do wwwroot
         * e retira os dados dos alunos desse ficheiro para a base de dados
         */
        [HttpPost]
        [Route("ImportUpload")]
        public IActionResult ImportUpload(IFormFile reportfile)
        {
            string folderName = "Upload";  //nome da pasta wwwroot
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
            string fileName = @"Upload/" + fiName;
            FileInfo file = new FileInfo(Path.Combine(rootFolder, fileName));

            using (ExcelPackage package = new ExcelPackage(file))
            {
                ExcelWorksheet workSheet = package.Workbook.Worksheets[0];
                int totalRows = workSheet.Dimension.Rows;
                List<Student> reportList = new List<Student>();
                List<User> reportUserList = new List<User>();
                Console.WriteLine(totalRows);
                for (int i = 2; i <= totalRows; i++)
                {
                    try
                    {
                        string FirstName = workSheet?.Cells[i, 1]?.Value?.ToString();
                        string LastName = workSheet?.Cells[i, 2]?.Value?.ToString();
                        string Email = workSheet?.Cells[i, 3]?.Value?.ToString();
                        string StudentNumberAux = workSheet?.Cells[i, 4]?.Value?.ToString();
                        int StudentNumber = Convert.ToInt32(StudentNumberAux);
                        string Field = workSheet?.Cells[i, 5]?.Value?.ToString();
                        string ProgressAux = workSheet?.Cells[i, 8]?.Value?.ToString();
                        int Progress = Convert.ToInt32(ProgressAux);
                        if (Email != null)
                        {
                            reportList.Add(new Student
                            {
                                FirstName = FirstName,
                                LastName = LastName,
                                Email = Email,
                                StudentNumber = StudentNumber,
                                Field = Field
                            });


                        }

                    }
                    catch (Exception Ex)
                    {
                        return RedirectToAction("Index", "Teachers");
                    }
                }

                foreach (Student e in reportList.ToList())
                {
                    reportUserList.Add(new User
                    {
                        StudentId = e.StudentNumber,
                        Email = e.Email,
                        Password = GenerateRandomPassword(),

                    });
                }

                List<Student> toUpdateList = new List<Student>();
                var studentList = _db.Student.FromSqlRaw("SELECT * FROM [dbo].Student").ToList();
                foreach (Student s in studentList)
                {
                    foreach (Student i in reportList.ToList())
                    {
                        if (i.StudentNumber == s.StudentNumber)
                        {
                            Console.WriteLine(i.StudentId);
                            toUpdateList.Add(i);
                            foreach (User u in reportUserList.ToList())
                            {
                                if (i.StudentNumber == u.StudentId)
                                {
                                    reportUserList.Remove(u);
                                }
                            }
                            reportList.Remove(i);
                        }
                    }
                }

                using (_db)
                {
                    var studentList1 = _db.Student.FromSqlRaw("SELECT * FROM [dbo].Student").ToList();
                    foreach (Student s in studentList1)
                    {
                        foreach (Student i in toUpdateList)
                        {
                            if (s.StudentNumber == i.StudentNumber)
                            {
                                s.Email = i.Email;
                                s.FirstName = i.FirstName;
                                s.LastName = i.LastName;
                                s.Field = i.Field;
                            }
                        }
                    }

                    var usersList = _db.User.FromSqlRaw("SELECT * FROM [dbo].[User]").ToList();
                    foreach (User u in usersList){
                        foreach (Student s in toUpdateList)
                        {
                            if(s.StudentNumber == u.StudentId)
                            {
                                u.Email = s.Email;
                            }
                        }
                    }

                    _db.User.AddRange(reportUserList);
                    _db.Student.AddRange(reportList);
                    _db.SaveChanges();
                    return RedirectToAction("Index", "Teachers");
                }
            }
        }


        public IActionResult GoToStudentsList()
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;

            if (role != "Aluno")
            {
                return RedirectToAction("Search", "Teachers");
            } else
            {
                return RedirectToAction("Search", "Students");
            }
            
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

