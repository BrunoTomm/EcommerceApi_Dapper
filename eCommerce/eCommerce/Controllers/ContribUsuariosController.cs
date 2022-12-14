using eCommerce.Repositories;
using eCommerceAPI.Models;
using eCommerceAPI.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.Controllers
{
    [Route("api/Contrib/[controller]")]
    [ApiController]
    public class ContribUsuariosController : ControllerBase
    {
        private IUsuarioRepository _repository;
        public ContribUsuariosController()
        {
            _repository = new ContribUsuarioRepository();
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_repository.Get()); // HTTP 200 - OK
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var usuario = _repository.Get(id);

            if (usuario is null)
            {
                return NotFound();// HTTP 400 - Nao encontrado
            }
            return Ok(usuario);
        }

        [HttpPost]
        public IActionResult Insert(Usuario usuario)
        {
            _repository.Insert(usuario);
            return Ok(usuario);
        }

        [HttpPut]
        public IActionResult Update([FromBody] Usuario usuario)
        {
            _repository.Update(usuario);
            return Ok(usuario);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _repository.Delete(id);
            return Ok();
        }
    }
}
