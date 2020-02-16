using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using G13WebApplication.Data;
using G13WebApplication.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Collections.Generic;
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    /**
     * Método que controla as ações dos estudantes
     */
    public class StudentsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostingEnvironment _hostingEnvironment;

        public StudentsController(ApplicationDbContext context, IHostingEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            var userId = 0;
            var studentId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value.ToString());
            userId = _context.User.FromSqlRaw("Select * from [dbo].[User] where StudentId = " + studentId).ToList().FirstOrDefault().UserId;
            var activities = new List<Activity>();
            var activitiesAux = _context.Activity_Participant.FromSqlRaw("Select * from Activity_Participant where UserId = " + userId).ToList();
            foreach (var act in activitiesAux)
            {
                activities.Add(_context.Activity.FromSqlRaw("Select * from Activity where ActivityId = " + act.ActivityId).FirstOrDefault());
            }
            return View(activities.OrderBy(i => i.DateT).Where(i=> i.DateT >= DateTime.Now).Take(2));
        }
  
        // GET: Students
        public async Task<IActionResult> Search(string searchString)
        {
            var studentId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value.ToString());

            var users = from s in _context.User where s.StudentId == 0 select s;

            if (!String.IsNullOrEmpty(searchString))
            {
                var usersAux = from s in _context.User where s.StudentId != studentId select s;

                usersAux = usersAux.Where(s => s.Email.Contains(searchString));

                return View(await usersAux.ToListAsync());
            }

            return View(await users.ToListAsync());
        }

        /**
         * Método que devolve a string to tipo de tfc de um certo estudante
         */
        public string GetTfcType(int studentId)
        {
            WorkPlan workplan = _context.WorkPlan.FromSqlRaw("Select * from WorkPlan as wp join Student as s on wp.PlanId = s.PlanIdFk where s.StudentNumber = " + studentId).ToList().FirstOrDefault();
            if(workplan == null)
            {
                return "";
            }
            else
            {
                var tfcTypeString = workplan.TfcType;
                return tfcTypeString;
            }
        }

        /**
         * Método da view que procura um DO
         */
        public async Task<IActionResult> SearchDO()
        {
            return View(await _context.Teacher.ToListAsync());
        }

        /**
         * Método da view que procura um DO como um RUC
         */
        public async Task<IActionResult> SearchDOasRUC(int? id)
        {
            ViewData["studentId"] = id;
            return View(await _context.Teacher.ToListAsync());
        }

        /**
         * Método que devolve o nome de um professor de um certo studentID
         */
        public string GetTeacherName(int studentId)
        {
            var teacher = _context.Teacher.FromSqlRaw("Select * From Teacher as t join Students_Teachers as st on t.TeacherId = st.TeacherIdFk where st.StudentIdFk = " + studentId).ToList().FirstOrDefault();
            if(teacher == null)
            {
                return "";
            }
            else {
                var teacherNameString = teacher.FirstName + " " + teacher.LastName;
                return teacherNameString;
            }
        }

        /**
         * Método que retorna a view com o perfil, para auxilio envia o nome do professor e do tipo de tfc.
         */
        public IActionResult Perfil()
        {
            var studentId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value.ToString());
            var usersList = _context.Student.FromSqlRaw("SELECT * FROM [dbo].[Student] WHERE StudentNumber = " + studentId).ToList().First();
            ViewData["auxTeacherNameString"] = GetTeacherName(usersList.StudentId);
            ViewData["auxTfcTypeString"] = GetTfcType(usersList.StudentNumber);
            ViewBag.Image = GetUserPhoto();
            return View(usersList);
        }


        /**
         * Método que retorna a view com o perfil, para auxilio envia o nome do professor e do tipo de tfc.
         */
        public IActionResult OtherProfile(int id)
        {
            var user = _context.Student.FromSqlRaw("SELECT * FROM [dbo].[Student] WHERE StudentNumber = " + id).FirstOrDefault();
            ViewData["auxTeacherNameString"] = GetTeacherName(user.StudentId);
            ViewData["auxTfcTypeString"] = GetTfcType(user.StudentNumber);
            var Dos = _context.Students_Teachers.FromSqlRaw("SELECT * FROM [dbo].[Students_Teachers] WHERE StudentIdFk = " + user.StudentId).ToList();
            if (Dos.Count >= 1)
            {
                ViewData["DO1"] = Dos[0].TeacherIdFk;
                if (Dos.Count == 2)
                {
                    ViewData["DO2"] = Dos[1].TeacherIdFk;
                }
            }
            ViewBag.Image = GetOtherUserPhoto(id);
            return View(user);
        }

        /**
         * Método para atualizar a foto , cria uma folder na wwwroot e armazena as fotos lá
         */
        [HttpPost]
        public IActionResult UpdatePhoto(IFormFile photo)
        {
            string folderName = "UsersImages";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);

            var userId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value.ToString());
            var user = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE UserId = " + userId).ToList().First();

            if (!Directory.Exists(newPath))// Create New Directory if not exist as per the path
            {
                Directory.CreateDirectory(newPath);
            }

            var fiName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
            using (var fileStream = new FileStream(Path.Combine(newPath, fiName), FileMode.Create))
            {
                photo.CopyTo(fileStream);
            }
            // Get uploaded file path with root
            string rootFolder = _hostingEnvironment.WebRootPath;
            string fileName = @"~/UsersImages/" + fiName;
            FileInfo file = new FileInfo(fileName);

            if (user.photoPath != null)
            {
                // Delete existing photo paths of the user from Directory
                System.IO.DirectoryInfo di = new DirectoryInfo(newPath);
                foreach (FileInfo filesDelete in di.GetFiles())
                {
                    var name = filesDelete.FullName;
                    var split = name.Split("\\");
                    var finalPath = "~/UsersImages/" + split[split.Length - 1];

                    if (finalPath.Equals(user.photoPath))
                    {
                        filesDelete.Delete();
                    }
                }// End Deleting files from directories
            }

            using (_context)
            {
                user.photoPath = file.ToString();
                _context.SaveChanges();
            }

            return RedirectToAction("Perfil");
        }

        /**
         * Método que devolve a string com o caminho para a foto updated.
         */
        public String GetUserPhoto()
        {
            var userId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value.ToString());
            var user = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE UserId = " + userId).ToList().First();
            if (user.photoPath != null)
            {
                var path = user.photoPath;
                var split = path.Split("/");
                Console.WriteLine("Split: " + split[split.Length - 1]);
                return split[split.Length - 1];
            }
            return "perfilTeste.png";
        }

        /**
         * Método que devolve a string com o caminho para a foto updated.
         */
        public String GetOtherUserPhoto(int id)
        {
            var user = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE StudentId = " + id).ToList().First();
            if (user.photoPath != null)
            {
                var path = user.photoPath;
                var split = path.Split("/");
                Console.WriteLine("Split: " + split[split.Length - 1]);
                return split[split.Length - 1];
            }
            return "perfilTeste.png";
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }
        private const int NEW_PW_MAX_LENGTH = 8;
        private string GenerateRandomPassword()
        {
            RNGCryptoServiceProvider newpw = new RNGCryptoServiceProvider();

            byte[] arrTokenBuffer = new byte[NEW_PW_MAX_LENGTH];
            newpw.GetBytes(arrTokenBuffer);
            return Convert.ToBase64String(arrTokenBuffer).ToLower();
        }

        // POST: Students/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentId,FirstName,LastName,Email,StudentNumber,Field")] Student student)
        {
            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                User newUser = new User { Email = student.Email, StudentId = student.StudentNumber, Password = GenerateRandomPassword() };
                try
                {
                    _context.Add(newUser);
                    await _context.SaveChangesAsync();
                    Alert("Aluno Adicionado!", "Foi adicionado um novo aluno ao sistema!", NotificationType.success);
                }
                catch
                {
                    Alert("Ocorreu um erro!", "Não foi possivel adicionar o aluno ao sistema!", NotificationType.error);
                }
                return RedirectToAction("Search", "Teachers");
            }
            return RedirectToAction("Search", "Teachers");
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Student
                .FirstOrDefaultAsync(m => m.StudentId == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Student.FindAsync(id);
            try
            {
                _context.Student.Remove(student);
                await _context.SaveChangesAsync();
                Alert("Aluno Removido!", "O aluno foi removido do sistema!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel remover o aluno do sistema!", NotificationType.error);
            }
            return RedirectToAction("Search", "Teachers");
        }

        private bool StudentExists(int id)
        {
            return _context.Student.Any(e => e.StudentId == id);
        }

        /**
         * Método para escolher o tipo de tfc de um aluno
         */
        public IActionResult SelectTfcType(int type)
        {
            var studentId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value.ToString());
            var student = _context.Student.FromSqlRaw("SELECT * FROM [dbo].[Student] WHERE StudentId = " + studentId).ToList().First();
            var workPlan = _context.WorkPlan.FromSqlRaw("SELECT * FROM [dbo].[WorkPlan] WHERE PlanId = " + student.PlanIdFk).ToList().First();
            var tfc = _context.Tfc.FromSqlRaw("SELECT * FROM [dbo].[Tfc] WHERE TfcId = " + workPlan.TfcIdFk).ToList().First();
            if (!tfc.TfcType.Equals(""+type))
            {
                using (_context)
                {
                    tfc.TfcType = "" + type;
                    _context.SaveChanges();
                }
                return View("Perfil");
            }
            else
            {
                return View("Perfil");
            }
        }

        /**
        * Método para escolher o tfc de um aluno
        */
        public IActionResult SelectTfc(int tfcId)
        {
            var studentId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value.ToString());
            var student = _context.Student.FromSqlRaw("SELECT * FROM [dbo].[Student] WHERE StudentId = " + studentId).ToList().First();
            var workPlan = _context.WorkPlan.FromSqlRaw("SELECT * FROM [dbo].[WorkPlan] WHERE PlanId = " + student.PlanIdFk).ToList().First();
            if (workPlan.TfcIdFk != tfcId)
            {
                using (_context)
                {
                    workPlan.TfcIdFk = tfcId;
                    _context.SaveChanges();
                }
                return View("Perfil");
            }
            else
            {
                return View("Perfil");
            }
        }

        public int GetUserIdFromTeacherId(int? TeacherId)
        {
            var user = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE TeacherId = "+TeacherId).ToList().First();
            return user.UserId;
        }

        public int GetCurrentStudentID(ClaimsPrincipal user)
        {
            return int.Parse(user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value);
        }

        public int GetStudentIdFromStudentNumber(int? StudentNumber)
        {
            var student = _context.Student.FromSqlRaw("SELECT * FROM [dbo].[Student] WHERE StudentNumber = " + StudentNumber).ToList().First();
            return student.StudentId;
        }

        /**
         * Método que envia um pedido a um docente orientador
         */
        public async Task<IActionResult> EnviarPedidoDO(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }
            var teacher = await _context.Teacher
                .FirstOrDefaultAsync(m => m.TeacherId == id);

            if (teacher == null)
            {
                return NotFound();
            }


            var TeacherUserId = GetUserIdFromTeacherId(id);

            var studentsTeachers = _context.Students_Teachers.FromSqlRaw("Select * From Students_Teachers").ToList();

            foreach(Students_Teachers st in studentsTeachers){
                if(st.StudentIdFk == GetStudentIdFromStudentNumber(GetCurrentStudentID(User)) && st.TeacherIdFk == id)
                {
                    return NotFound(); // depois adicionar o erro "Este docente já é o teu docente orientador
                }
            }

            var lastInsertId = _context.Notification.Max(wp => wp.NotificationId);

            Notification notification = new Notification {Message = "Recebeu um pedido para ser docente orientador do aluno com nº " + GetCurrentStudentID(User) + ". Vá ao paínel de propostas de Docente Orientador, para aceitar/rejeitar o pedido", state = "fechado", AddedOn = DateTime.Now, UserId = TeacherUserId, ReadNotification = 0 };

            try
            {
                _context.Add(notification);
                ProposalDO proposalDO = new ProposalDO { TeacherIdFk = (int)id, StudentNumber = GetCurrentStudentID(User) };
                _context.Add(proposalDO);
                await _context.SaveChangesAsync();
                Alert("Pedido Enviado!", "O pedido de orientação foi enviado com sucesso a " + teacher.FirstName + "!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "O pedido não foi enviado ao docente!", NotificationType.error);
            }

            return RedirectToAction("SearchDO");
        }

        /**
         * Método que envia um pedido a um docente orientador, sendo o RUC
         */
        public async Task<IActionResult> EnviarPedidoDOasRUC(int? id, int? studentId)
        {
            if (id == null)
            {
                return NotFound();
            }
            var teacher = await _context.Teacher
                .FirstOrDefaultAsync(m => m.TeacherId == id);

            var student = await _context.Student
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (teacher == null)
            {
                return NotFound();
            }


            var TeacherUserId = GetUserIdFromTeacherId(id);

            var studentsTeachers = _context.Students_Teachers.FromSqlRaw("Select * From Students_Teachers").ToList();

            foreach (Students_Teachers st in studentsTeachers)
            {
                if (st.StudentIdFk == student.StudentId && st.TeacherIdFk == id)
                {
                    return NotFound(); // depois adicionar o erro "Este docente já é o teu docente orientador
                }
            }

            var lastInsertId = _context.Notification.Max(wp => wp.NotificationId);

            Notification notification = new Notification { Message = "Recebeu um pedido para ser docente orientador do aluno com nº " + student.StudentNumber + ". Vá ao paínel de propostas de Docente Orientador, para aceitar/rejeitar o pedido", state = "fechado", AddedOn = DateTime.Now, UserId = TeacherUserId, ReadNotification = 0 };

            try 
            { 
                _context.Add(notification);
                ProposalDO proposalDO = new ProposalDO { TeacherIdFk = (int)id, StudentNumber = student.StudentNumber };
                _context.Add(proposalDO);
                await _context.SaveChangesAsync();
                Alert("Pedido Enviado!", "O pedido de orientação para " + student.FirstName + " foi enviado com sucesso a " + teacher.FirstName + "!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "O pedido não foi enviado ao docente!", NotificationType.error);
            }

            return RedirectToAction("Index", "Teachers");
        }

        /**
         * Método que mostra o detalhe da prova pública de um aluno, recebendo o seu numero por parâmetro
         */
        public async Task<IActionResult> ViewStudentPublicProof(int? studentNumber)
        {
            var UserId = _context.User.FromSqlRaw("Select * from [dbo].[User] where StudentId = " + studentNumber).ToList().FirstOrDefault().UserId;
            var Student_Activity_Participant = _context.Activity_Participant.FromSqlRaw("Select * from Activity_Participant where UserId =" + UserId).ToList();
            var publicProof = new Activity();
            foreach (var sap in Student_Activity_Participant)
            {
                var aux = _context.Activity.FromSqlRaw("Select * from Activity where ActivityId = " + sap.ActivityId + " and ActivityType = 'Prova'").FirstOrDefault();
                if (aux != null)  
                    publicProof = aux;
            }
            if (publicProof == null)
            {
                Alert("Ocorreu um erro!", "O aluno ainda não tem nenhuma prova publica marcada!", NotificationType.error);
                return RedirectToAction("Student", "OtherProfile", new { id = publicProof.ActivityId });
            }
            return RedirectToAction("Details", "Activities", new { id = studentNumber });
        }
    }
}
