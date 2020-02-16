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
    public class UsersController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Students
        public async Task<IActionResult> Index(string searchString)
        {
                var students = from s in _context.User where s.TeacherId == null
                               select s;

                if (!String.IsNullOrEmpty(searchString))
                {
                    int number = int.Parse(searchString);
                    students = students.Where(s => s.StudentId.Equals(number));
                }

                return View(await students.ToListAsync());
        }

        public IActionResult OtherProfile(int id)
        {
            var usersList = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE UserId = " + id).ToList();

            if (usersList.First().TeacherId != null)
            {
                return RedirectToAction("OtherProfile", "Teachers", new { id = usersList.First().TeacherId });
            } else
            {
                if (usersList.First().TOId != null)
                {
                    return RedirectToAction("OtherProfile", "TOs", new { id = usersList.First().TOId } );
                }
                return RedirectToAction("OtherProfile", "Students", new { id = usersList.First().StudentId });
            }
        }

        // GET: Users/Details/5
        /*public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.User
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }*/

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Email,StudentId,TeacherId,Password")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                Alert("Ocorreu um erro!", "Não foi possivel encontrar o utilizador no sistema!", NotificationType.error);
                return RedirectToAction("Search", "Teachers");
            }

            var user = await _context.User.FindAsync(id);
            if (user == null)
            {
                Alert("Ocorreu um erro!", "Não foi possivel encontrar o utilizador no sistema!", NotificationType.error);
                return RedirectToAction("Search", "Teachers");
            }

            return View(user);
        }

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.User.FindAsync(id);

            try
            {
                if (user.StudentId != null)
                {
                    var student = _context.Student.FromSqlRaw("SELECT * FROM [dbo].[Student] WHERE StudentNumber = " + user.StudentId).FirstOrDefault();
                    _context.Student.Remove(student);
                    Alert("Aluno Removido!", "O aluno foi removido do sistema!", NotificationType.success);
                }
                else if (user.TeacherId != null)
                {
                    var teacher = _context.Teacher.FromSqlRaw("SELECT * FROM [dbo].[Teacher] WHERE TeacherId = " + user.TeacherId).FirstOrDefault();
                    _context.Teacher.Remove(teacher);
                    Alert("Docente Removido!", "O docente foi removido do sistema!", NotificationType.success);
                }
                else
                {
                    var to = _context.TO.FromSqlRaw("SELECT * FROM [dbo].[TO] WHERE TOId = " + user.TOId).FirstOrDefault();
                    _context.TO.Remove(to);
                    Alert("Tutor Removido!", "O tutor foi removido do sistema!", NotificationType.success);
                }
            }
            catch
            {
                Alert("Ocorreu um erro!", "Não foi possivel remover o utilizador do sistema!", NotificationType.error);
            }

            _context.User.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Search", "Teachers");
        }

        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.UserId == id);
        }
    }
}
