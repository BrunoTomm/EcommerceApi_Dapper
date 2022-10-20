using eCommerceAPI.Models;
using System.Collections.Generic;

namespace eCommerceAPI.Repositories
{
    interface IUsuarioRepository
    {
        public List<Usuario> Get();
        public Usuario Get(int id);
        public void Insert(Usuario usuario);
        public void Update(Usuario usuario);
        public void Delete(int id);
    }
}
