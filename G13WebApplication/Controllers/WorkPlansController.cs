using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using G13WebApplication.Data;
using G13WebApplication.Models;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace G13WebApplication.Controllers
{
    /**
     * Classe que controla as ações dos planos de trabalho
     */
    public class WorkPlansController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public WorkPlansController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public int GetCurrentStudentID(ClaimsPrincipal user)
        {
            return int.Parse(user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value);
        }

        public IActionResult ChooseTfc()
        {
            return View();
        }

        /**
         * Classe que cria um WorkPlan, sabendo que no formulário o aluno escolheu
         * um tipo de tfc , criando o workplan apenas com o tipo de tfc.
         */
        [HttpPost]
        public ActionResult formTfc(string TfcType)
        {
            var studentNumber = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value.ToString());
            WorkPlan workplan = _context.WorkPlan
                .FromSqlRaw("Select * from WorkPlan as wp join Student as s on wp.PlanId = s.PlanIdFk where s.StudentNumber = " + studentNumber)
                .ToList().FirstOrDefault();
            if(workplan == null) {
                using (SqlConnection scnConnection = new SqlConnection(ApplicationDbContext.ConnectionString))
                {
                    scnConnection.Open();
                    string strQuery = "Insert into WorkPlan(Confirmed, TfcType) values (0,@TfcType)";

                    SqlCommand scmCommand = new SqlCommand(strQuery, scnConnection);
                    scmCommand.Parameters.AddWithValue("@TfcType", TfcType);

                    scmCommand.ExecuteNonQuery();
                }
                var lastInsertId = _context.WorkPlan.Max(wp => wp.PlanId);
                using (SqlConnection scnConnection = new SqlConnection(ApplicationDbContext.ConnectionString))
                {
                    scnConnection.Open();
                    string strQuery = "Update Student Set PlanIdFk = @lastInsertId Where StudentNumber = @StudentId";

                    SqlCommand scmCommand = new SqlCommand(strQuery, scnConnection);
                    scmCommand.Parameters.AddWithValue("@lastInsertId", lastInsertId);
                    scmCommand.Parameters.AddWithValue("@StudentId", GetCurrentStudentID(User));

                    scmCommand.ExecuteNonQuery();
                }
            } else
            {
                using (SqlConnection scnConnection = new SqlConnection(ApplicationDbContext.ConnectionString))
                {
                    scnConnection.Open();
                    string strQuery = "Update WorkPlan Set TfcType = @TfcType Where PlanId = @PlanId";

                    SqlCommand scmCommand = new SqlCommand(strQuery, scnConnection);
                    scmCommand.Parameters.AddWithValue("@PlanId", workplan.PlanId);
                    scmCommand.Parameters.AddWithValue("@TfcType", TfcType);

                    scmCommand.ExecuteNonQuery();
                }
            }
            
            return RedirectToAction("Perfil","Students");
        }
    }
}
