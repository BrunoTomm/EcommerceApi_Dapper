using Dapper;
using eCommerceAPI.Models;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System;

namespace eCommerceAPI.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        //Config com banco de dados
        private IDbConnection _connection;
        public UsuarioRepository()
        {
            _connection = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=eCommerce;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
        }

        public List<Usuario> Get()
        {
            return _connection.Query<Usuario>("SELECT * FROM Usuarios").ToList(); //Compara os nomes das tabelas do banco com o objeto, necessario os nomes serem iguais
        }

        public Usuario Get(int id)
        {
            return _connection.Query<Usuario, Contato, Usuario>($" SELECT * FROM Usuarios AS U LEFT JOIN Contatos C ON C.UsuarioId = U.id WHERE U.Id = {id}",
                (usuario, contato) =>
                {
                    usuario.Contato = contato;
                    return usuario;
                }
                ).SingleOrDefault();
        }

        public void Insert(Usuario usuario)
        {
            _connection.Open();
            var transaction = _connection.BeginTransaction(); //Transaction

            try
            {
                string sql = "INSERT INTO Usuarios(Nome, Email, Sexo, RG, CPF, NomeMae, SituacaoCadastro, DataCadastro) " +
                                "VALUES (@Nome, @Email, @Sexo, @RG, @CPF, @NomeMae, @SituacaoCadastro, @DataCadastro); " +
                                     "SELECT CAST (SCOPE_IDENTITY() AS INT);";

                usuario.Id = _connection.Query<int>(sql, usuario, transaction).Single();

                if (usuario.Contato != null)
                {
                    usuario.Contato.UsuarioId = usuario.Id;

                    string sqlContato = "INSERT INTO Contatos(UsuarioId, Telefone, Celular) " +
                                            "VALUES (@UsuarioId, @Telefone, @Celular); " +
                                                "SELECT CAST (SCOPE_IDENTITY() AS INT);";

                    usuario.Contato.Id = _connection.Query<int>(sqlContato, usuario.Contato, transaction).Single();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception)
                {
                    //Retornar para o UsuarioController alguma mensagem. Ou lancar uma excessao adequada
                }
            }
            finally
            {
                _connection.Close();
            }

        }

        public void Update(Usuario usuario)
        {
            string sql = "UPDATE Usuarios SET Nome = @Nome, Email  = @Email, Sexo = @Sexo, RG = @RG, CPF = @CPF, NomeMae = @NomeMae, SituacaoCadastro = @SituacaoCadastro, DataCadastro = @DataCadastro " +
                "WHERE Id = @Id";

            _connection.Execute(sql, usuario);

        }
        public void Delete(int id)
        {
            _connection.Execute("DELETE FROM Usuarios WHERE Id = @Id", new { Id = id });
        }
    }
}
