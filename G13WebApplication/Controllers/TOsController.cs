using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using G13WebApplication.Data;
using G13WebApplication.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    /**
     * Classe que controla as açôes dos tutores orientadores
     */
    public class TOsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostingEnvironment _hostingEnvironment;

        public TOsController(ApplicationDbContext context, IHostingEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: TOs
        public async Task<IActionResult> Index()
        {
            var userId = 0;
            var toId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
            userId = _context.User.FromSqlRaw("Select * from [dbo].[User] where TOId = " + toId).ToList().FirstOrDefault().UserId;
            var activities = new List<Activity>();
            var activitiesAux = _context.Activity_Participant.FromSqlRaw("Select * from Activity_Participant where UserId = " + userId).ToList();
            foreach (var act in activitiesAux)
            {
                activities.Add(_context.Activity.FromSqlRaw("Select * from Activity where ActivityId = " + act.ActivityId).FirstOrDefault());
            }
            return View(activities.OrderBy(i => i.DateT).Where(i => i.DateT >= DateTime.Now).Take(2));
        }

        // GET: TOs/Create
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

        // POST: TOs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(String FirstName,String LastName, String Email)
        {
            var newTO = new TO
            {
                Email = Email,
                FirstName = FirstName,
                LastName = LastName
            };
            _context.Add(newTO);
            await _context.SaveChangesAsync();
            var newUser = new User
            {
                Email = Email,
                Password = GenerateRandomPassword(),
                TOId = newTO.TOId
            };
            try
            {
                _context.Add(newUser);
                await _context.SaveChangesAsync();
                Alert("TO Adicionado com Sucesso!", "Foi adicionado um novo TO ao sistema!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel adicionar o TO ao sistema!", NotificationType.error);
                return RedirectToAction("Search", "Teachers");
            }
            return RedirectToAction("SelectTfc", "Tfcs", newTO);
        }

        private bool TOExists(int id)
        {
            return _context.TO.Any(e => e.TOId == id);
        }

        /**
         * Método que retorna a view com o perfil.
         */
        public IActionResult Perfil()
        {
            var toID = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
            var to = _context.TO.FromSqlRaw("SELECT * FROM [dbo].[TO] WHERE TOId = " + toID).FirstOrDefault();
            ViewBag.Image = GetUserPhoto();
            ViewBag.Company = _context.Tfc.FromSqlRaw("Select * from [dbo].[Tfc] join [dbo].[TO] on TfcIdFk = TfcId where TOId = " + to.TOId).FirstOrDefault().Company;
            return View(to);
        }

        /**
         * Método que retorna a view com o perfil.
         */
        public IActionResult OtherProfile(int id)
        {
            var to = _context.TO.FromSqlRaw("SELECT * FROM [dbo].[TO] WHERE TOId = " + id).FirstOrDefault();
            ViewBag.Image = GetOtherUserPhoto(id);
            ViewBag.Company = _context.Tfc.FromSqlRaw("Select * from [dbo].[Tfc] join [dbo].[TO] on TfcIdFk = TfcId where TOId = " + to.TOId).FirstOrDefault().Company;
            return View(to);
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
            var user = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE TOId = " + id).ToList().First();
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
                Alert("Imagem Alterada com Sucesso!", "A sua imagem de perfil foi atualizada com sucesso!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel alterar a sua imagem de perfil", NotificationType.error);
            }

            return RedirectToAction("Perfil");
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
         * Método que devolve uma lista de users com todos os alunos que o professor orienta.
         */
        private IEnumerable<User> getMyStudents()
        {
            var toId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
            var aux = _context.Student.FromSqlRaw("Select s.StudentId, s.Progress, s.Email, s.FirstName, s.LastName, s.StudentNumber, s.Field, s.PlanIdFk from [dbo].[Student] s join [dbo].[WorkPlan] wp on s.PlanIdFk = wp.PlanId join [dbo].[TO] t on wp.TfcIdFk = t.TfcIdFk where t.TOid = " + toId).ToList();
            var users = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE TOId = 0").ToList();

            foreach (Student st in aux)
            {
                var studentAux = _context.Student.FromSqlRaw("SELECT * FROM [dbo].[Student] WHERE StudentId = " + st.StudentId).ToList().First();
                var userAux = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE StudentId = " + studentAux.StudentNumber).ToList().First();
                users.Add(userAux);
            }
            return users;
        }
    }
}
