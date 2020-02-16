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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Security.Cryptography;
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
   
    public class TeachersController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostingEnvironment _hostingEnvironment;

        public TeachersController(ApplicationDbContext context, IHostingEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: Teachers
        public IActionResult Index()
        {
            var userId = 0;
            var teacherId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
            userId = _context.User.FromSqlRaw("Select * from [dbo].[User] where TeacherId = " + teacherId).ToList().FirstOrDefault().UserId;
            var activities = new List<Activity>();
            var activitiesAux = _context.Activity_Participant.FromSqlRaw("Select * from Activity_Participant where UserId = " + userId).ToList();
            foreach (var act in activitiesAux)
            {
                activities.Add(_context.Activity.FromSqlRaw("Select * from Activity where ActivityId = " + act.ActivityId).FirstOrDefault());
            }
            return View(activities.OrderBy(i => i.DateT).Where(i => i.DateT >= DateTime.Now).Take(2));
        }
        //GET : Teachers
        public async Task<IActionResult> Search(string searchString)
        {
            if (!String.IsNullOrEmpty(searchString))
            {
                var teacherId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
                var usersAux = from s in _context.User where s.TeacherId != teacherId select s;

                usersAux = usersAux.Where(s => s.Email.Contains(searchString));

                return View(await usersAux.ToListAsync());
            }

            if (User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value == "RUC")
            {
                var users = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User]").ToList();
                return View(users);
            }
            else
            {
                var users = getMyStudents();
                return View(users);
            }
        }

        /**
         * Método que procura em todos os alunos orientados pelo teacher logado
         */
        public async Task<IActionResult> SearchMyStudents(string searchString)
        {
            var users = getMyStudents();

            if (!String.IsNullOrEmpty(searchString))
            {
                var teacherId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
                var usersAux = users;

                usersAux = usersAux.Where(s => s.Email.Contains(searchString));

                return View(usersAux);
            }

            return View(users);
        }

        /**
         * Método que retorna a view com o perfil.
         */
        public IActionResult Perfil()
        {
            var teacherId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
            var usersList = _context.Teacher.FromSqlRaw("SELECT * FROM [dbo].[Teacher] WHERE TeacherId = " + teacherId).ToList();
            ViewBag.Image = GetUserPhoto();
            return View(usersList.First());
        }

        /**
         * Método que retorna a view com o perfil.
         */
        public IActionResult OtherProfile(int id)
        {
            var teacher = _context.Teacher.FromSqlRaw("SELECT * FROM [dbo].[Teacher] WHERE TeacherId = " + id).ToList();
            ViewBag.Image = GetOtherUserPhoto(id);
            return View(teacher.First());
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

            try
            {
                user.photoPath = file.ToString();
                _context.SaveChanges();
                Alert("Imagem de Perfil Alterada!", "A imagem de perfil foi alterada com sucesso!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel alterar a imagem de perfil!", NotificationType.success);
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

        public String GetOtherUserPhoto(int id)
        {
            var user = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE TeacherId = " + id).ToList().First();
            if (user.photoPath != null)
            {
                var path = user.photoPath;
                var split = path.Split("/");
                Console.WriteLine("Split: " + split[split.Length - 1]);
                return split[split.Length - 1];
            }
            return "perfilTeste.png";
        }

        public IActionResult CreateUser()
        {
            return View();
        }

        // GET: Teachers/Create
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

        // POST: Teachers/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TeacherId,FirstName,LastName,Email,Taught_Area,Role")] Teacher teacher)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(teacher);
                    await _context.SaveChangesAsync();
                    User newUser = new User { Email = teacher.Email, TeacherId = teacher.TeacherId, Password = GenerateRandomPassword() };
                    _context.Add(newUser);
                    await _context.SaveChangesAsync();
                    Alert("Novo Docente Adicionado ao Sistema!", "O docente foi adicionado com sucesso!", NotificationType.success);
                }
                catch
                {
                    Alert("Ocorreu um erro!", "Não foi possivel adicionar o novo docente ao sistema!", NotificationType.success);
                }
                return RedirectToAction("Search");
            }
            return RedirectToAction("Search");
        }

        private bool TeacherExists(int id)
        {
            return _context.Teacher.Any(e => e.TeacherId == id);
        }

        /**
         * Método que devolve uma lista de users com todos os alunos que o professor orienta.
         */
        private IEnumerable<User> getMyStudents()
        {
            var teacherId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
            var students_teachersAux = from s in _context.Students_Teachers where s.TeacherIdFk == teacherId select s;
            var users = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE TeacherId = 0").ToList(); ;
            foreach (Students_Teachers st in students_teachersAux)
            {
                var studentAux = _context.Student.FromSqlRaw("SELECT * FROM [dbo].[Student] WHERE StudentId = " + st.StudentIdFk).ToList().First();
                var userAux = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE StudentId = " + studentAux.StudentNumber).ToList().First();
                users.Add(userAux);
            }
            return users;
        }

        /**
         * Método que redireciona para a criaçao de um utilizador dependendo do tipo de utilizador que recebe por parâmetro.
         */
        [HttpPost]
        public ActionResult CreateTypeOfUser(string UserType)
        {
            if (UserType == "Student")
            {
                return RedirectToAction("Create", "Students");
            }
            else if (UserType == "Teacher")
            {
                return RedirectToAction("Create", "Teachers");
            }
            else //if(UserType == "TO")
            {
                return RedirectToAction("Create", "TOs");
            }
        }
    }
}
