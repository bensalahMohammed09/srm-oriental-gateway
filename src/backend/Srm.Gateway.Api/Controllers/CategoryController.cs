using Microsoft.AspNetCore.Mvc;
using Srm.Gateway.Application.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Srm.Gateway.API.Controllers
{
    [ApiController]
    // 🌟 La route devient dynamiquement /api/v1/category
    [Route("api/v1/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        // Injection directe de l'UnitOfWork (Pas de service intermédiaire)
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            // Récupère toutes les catégories depuis la base de données
            var categories = await _unitOfWork.Categories.GetAllAsync();

            // On formate la réponse directement ici en un objet simple pour le Front-end
            var response = categories.Select(c => new
            {
                id = c.Id,
                name = c.Name
            }).OrderBy(c => c.name);

            return Ok(response);
        }
    }
}