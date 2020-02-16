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
using static G13WebApplication.Enums.Enum.Enums;

namespace G13WebApplication.Controllers
{
    /**
     * Classe que trata das ações das propostas de docentes orientadores.
     */
    public class ProposalDOsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ProposalDOsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ProposalDOs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proposalDO = await _context.ProposalDO
                .FirstOrDefaultAsync(m => m.ProposalId == id);
            if (proposalDO == null)
            {
                return NotFound();
            }

            return View(proposalDO);
        }

        // POST: ProposalDOs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proposalDO = await _context.ProposalDO.FindAsync(id);
            _context.ProposalDO.Remove(proposalDO);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProposalDOExists(int id)
        {
            return _context.ProposalDO.Any(e => e.ProposalId == id);
        }

        /**
         * Método que retorna a view com as propostas do DO
         */
        public IActionResult ViewProposalDO()
        {
            var proposals = getMyProposals();
            return View(proposals);
        }

        /**
         * Retorna todas as propostas do DO
         */
        private IEnumerable<ProposalDO> getMyProposals()
        {
            var teacherId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
            var proposalDOAux = from s in _context.ProposalDO where s.TeacherIdFk == teacherId select s;
            return proposalDOAux;
        }

        /**
         * Retorna a view para o tratamento da proposta
         */
        public async Task<IActionResult> TreatProposal(int? id)
        {
            if (id == null)
            {
                Alert("Ocorreu um erro!","A proposta não foi encontrada!",NotificationType.error);
                return RedirectToAction("ViewProposalDO");
            }

            var proposalDO = await _context.ProposalDO
                .FirstOrDefaultAsync(m => m.ProposalId == id);
            if (proposalDO == null)
            {
                Alert("Ocorreu um erro!", "A proposta não foi encontrada!", NotificationType.error);
                return RedirectToAction("ViewProposalDO");
            }

            return View(proposalDO);
        }

        public int GetStudentIdFromStudentNumber(int? StudentNumber)
        {
            var student = _context.Student.FromSqlRaw("SELECT * FROM [dbo].[Student] WHERE StudentNumber = " + StudentNumber).ToList().First();
            return student.StudentId;
        }

        public int GetUserIdFromStudentId(int? StudentNumber)
        {
            var user = _context.User.FromSqlRaw("SELECT * FROM [dbo].[User] WHERE StudentId = " + StudentNumber).ToList().First();
            return user.UserId;
        }

        /**
         * Método utilizado para aceitar proposta do aluno
         */
        public async Task<IActionResult> AcceptProposal(int id)
        {
            var teacherId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor).Value.ToString());
            var proposalDO = await _context.ProposalDO.FindAsync(id);
            var studentId = GetStudentIdFromStudentNumber(proposalDO.StudentNumber);
            var studentUserId = GetUserIdFromStudentId(proposalDO.StudentNumber);
            Students_Teachers st = new Students_Teachers { TeacherIdFk = teacherId, StudentIdFk = studentId };
            Notification notification = new Notification { Message = "O seu pedido para o docente orientador foi aceite com sucesso!", state = "fechado", AddedOn = DateTime.Now, UserId = studentUserId, ReadNotification = 0 };

            try
            {
                _context.Students_Teachers.Add(st);
                _context.Notification.Add(notification);
                _context.ProposalDO.Remove(proposalDO);
                await _context.SaveChangesAsync();
                Alert("Proposta aceite!", "Voçê aceitou o aluno para seu orientado!", NotificationType.success);
            }
            catch
            {
                Alert("Ocorreu um erro!", "A proposta não foi aceite! Por favor tente mais tarde!", NotificationType.error);
            }
            return RedirectToAction("ViewProposalDO");
        }

        /**
          * Método utilizado para rejeitar proposta do aluno
          */
        public async Task<IActionResult> RejectProposal(int id)
        {
            var proposalDO = await _context.ProposalDO.FindAsync(id);
            var studentUserId = GetUserIdFromStudentId(proposalDO.StudentNumber);
            Notification notification = new Notification { Message = "O seu pedido para o docente orientador foi rejeitado.", state = "fechado", AddedOn = DateTime.Now, UserId = studentUserId, ReadNotification = 0 };

            try { 
                _context.Notification.Add(notification);
                _context.ProposalDO.Remove(proposalDO);
                await _context.SaveChangesAsync();
                Alert("Proposta Recusada!", "Voçê recusou a proposta de orientação!", NotificationType.warning);
            }
            catch
            {
                Alert("Ocorreu um erro!", "A proposta não foi recusada!", NotificationType.error);
            }
            return RedirectToAction("ViewProposalDO");
        }
    }
}
