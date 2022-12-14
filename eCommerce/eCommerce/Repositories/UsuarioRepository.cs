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
            //return _connection.Query<Usuario>("SELECT * FROM Usuarios").ToList(); //Compara os nomes das tabelas do banco com o objeto, necessario os nomes serem iguais
            List<Usuario> usuariosList = new List<Usuario>();

            //Necessario especificar o select, devido a ter a tabela UsuariosDepartamentos e ela n ser usada no mapeamento dapper
            string sql = $@"SELECT U.*,C.*,EE.*,D.* FROM Usuarios AS U 
	                            LEFT JOIN Contatos C ON C.UsuarioId = U.id 
	                            LEFT JOIN EnderecosEntrega EE ON EE.UsuarioId = U.Id
								LEFT JOIN UsuariosDepartamentos UD ON UD.UsuarioId = U.Id
								LEFT JOIN Departamentos D ON UD.DepartamentoId = D.Id";

            _connection.Query<Usuario, Contato, EnderecoEntrega, Departamento, Usuario>(sql,
                    (usuario, contato, enderecoEntrega, departamento) =>
                    {
                        //Verificacao do usuario
                        if(usuariosList.SingleOrDefault(_ => _.Id == usuario.Id) == null)
                        {
                            usuario.Departamentos = new List<Departamento>();
                            usuario.EnderecosEntrega = new List<EnderecoEntrega>();
                            usuario.Contato = contato;
                            usuariosList.Add(usuario);
                        }
                        else
                        {
                            usuario = usuariosList.SingleOrDefault(_ => _.Id == usuario.Id);
                        }

                        //Verificacao do endereco de entrega
                        if(usuario.EnderecosEntrega.SingleOrDefault(endereco => endereco.Id == enderecoEntrega.Id) == null)
                            usuario.EnderecosEntrega.Add(enderecoEntrega);

                        //Verificacao do departamento
                        if ((usuario.Departamentos.SingleOrDefault(departamento => departamento.Id == departamento.Id)) == null)
                            usuario.Departamentos.Add(departamento);

                        return usuario;
                    });

            return usuariosList;
        } 
        public Usuario Get(int id)
        {
            // -- RETORNA USUARIO, CONTATO, ENDEREÇO DE ENTREGA POR ID --
            List<Usuario> usuariosList = new List<Usuario>();

            string sql = $@"SELECT U.*,C.*,EE.*,D.* FROM Usuarios AS U 
	                            LEFT JOIN Contatos C ON C.UsuarioId = U.id 
	                            LEFT JOIN EnderecosEntrega EE ON EE.UsuarioId = U.Id
								LEFT JOIN UsuariosDepartamentos UD ON UD.UsuarioId = U.Id
								LEFT JOIN Departamentos D ON UD.DepartamentoId = D.Id
                                WHERE U.Id = @Id";

            _connection.Query<Usuario, Contato, EnderecoEntrega, Departamento, Usuario>(sql,
                    (usuario, contato, enderecoEntrega, departamento) =>
                    {
                        //Verificacao do usuario
                        if (usuariosList.SingleOrDefault(_ => _.Id == usuario.Id) == null)
                        {
                            usuario.Departamentos = new List<Departamento>();
                            usuario.EnderecosEntrega = new List<EnderecoEntrega>();
                            usuario.Contato = contato;
                            usuariosList.Add(usuario);
                        }
                        else
                        {
                            usuario = usuariosList.SingleOrDefault(_ => _.Id == usuario.Id);
                        }

                        //Verificacao do endereco de entrega
                        if (usuario.EnderecosEntrega.SingleOrDefault(endereco => endereco.Id == enderecoEntrega.Id) == null)
                            usuario.EnderecosEntrega.Add(enderecoEntrega);

                        //Verificacao do departamento
                        if ((usuario.Departamentos.SingleOrDefault(departamento => departamento.Id == departamento.Id)) == null)
                            usuario.Departamentos.Add(departamento);

                        return usuario;

                    }, new {Id = id});

            return usuariosList.FirstOrDefault();


            //-- RETORNA SOMENTE USUARIO {Id} --
            //return _connection.Query<Usuario, Contato, Usuario>($" SELECT * FROM Usuarios AS U LEFT JOIN Contatos C ON C.UsuarioId = U.id WHERE U.Id = {id}",
            //    (usuario, contato) =>
            //    {
            //        usuario.Contato = contato;
            //        return usuario;
            //    }
            //    ).SingleOrDefault();
        }
        public void Insert(Usuario usuario)
        {
            _connection.Open();
            var transaction = _connection.BeginTransaction(); //Transaction

            try
            {
                string sql = $@"INSERT INTO Usuarios(Nome, Email, Sexo, RG, CPF, NomeMae, SituacaoCadastro, DataCadastro)
                                            VALUES (@Nome, @Email, @Sexo, @RG, @CPF, @NomeMae, @SituacaoCadastro, @DataCadastro); 
                                            SELECT CAST (SCOPE_IDENTITY() AS INT);";

                usuario.Id = _connection.Query<int>(sql, usuario, transaction).Single();

                if (usuario.Contato != null)
                {
                    usuario.Contato.UsuarioId = usuario.Id;

                    string sqlContato = $@"INSERT INTO Contatos(UsuarioId, Telefone, Celular)
                                                        VALUES (@UsuarioId, @Telefone, @Celular); 
                                                        SELECT CAST (SCOPE_IDENTITY() AS INT);";

                    usuario.Contato.Id = _connection.Query<int>(sqlContato, usuario.Contato, transaction).Single();
                }

                if (usuario.EnderecosEntrega != null && usuario.EnderecosEntrega.Count > 0)
                {
                    foreach (var endereco in usuario.EnderecosEntrega)
                    {
                        endereco.UsuarioId = usuario.Id;

                        string sqlEndereco = $@"INSERT INTO EnderecosEntrega(UsuarioId, NomeEndereco, CEP, Estado, Cidade, Bairro, Endereco, Numero, Complemento)
                                                                      VALUES(@UsuarioId, @NomeEndereco, @CEP, @Estado, @Cidade, @Bairro, @Endereco, @Numero, @Complemento)
                                                                      SELECT CAST (SCOPE_IDENTITY() AS INT);";

                        endereco.Id = _connection.Query<int>(sqlEndereco, endereco, transaction).Single();
                    }
                }

                if (usuario.Departamentos != null && usuario.Departamentos.Count > 0)
                {
                    foreach (var departamento in usuario.Departamentos)
                    {
                        string sqlUsuariosDepartamentos = $@"INSERT INTO UsuariosDepartamentos (UsuarioId, DepartamentoId)
                                                                      VALUES(@UsuarioId, @DepartamentoId)";

                        _connection.Execute(sqlUsuariosDepartamentos, new { UsuarioId = usuario.Id, DepartamentoId = departamento.Id }, transaction);
                    }
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
            _connection.Open();
            var transaction = _connection.BeginTransaction();

            try
            {
                string sql = "UPDATE Usuarios SET Nome = @Nome, Email  = @Email, Sexo = @Sexo, RG = @RG, CPF = @CPF, NomeMae = @NomeMae, SituacaoCadastro = @SituacaoCadastro, DataCadastro = @DataCadastro WHERE Id = @Id";
                _connection.Execute(sql, usuario, transaction);

                if (usuario.Contato != null)
                {
                    string sqlContato = "UPDATE Contatos SET Telefone = @Telefone, Celular = @Celular WHERE Id = @Id";
                    _connection.Execute(sqlContato, usuario.Contato, transaction);
                }

                //Primeiro excluir o registro do banco
                string sqlDeletarEnderecosDeEntrega = "DELETE FROM EnderecosEntrega WHERE UsuarioId = @Id";
                _connection.Execute(sqlDeletarEnderecosDeEntrega, usuario, transaction);

                //Adicionar novamente com os novos dados mantendo o ID
                if (usuario.EnderecosEntrega != null && usuario.EnderecosEntrega.Count > 0)
                {
                    foreach (var endereco in usuario.EnderecosEntrega)
                    {
                        endereco.UsuarioId = usuario.Id;

                        string sqlEndereco = $@"INSERT INTO EnderecosEntrega(UsuarioId, NomeEndereco, CEP, Estado, Cidade, Bairro, Endereco, Numero, Complemento)
                                                                      VALUES(@UsuarioId, @NomeEndereco, @CEP, @Estado, @Cidade, @Bairro, @Endereco, @Numero, @Complemento)
                                                                      SELECT CAST (SCOPE_IDENTITY() AS INT);";

                        endereco.Id = _connection.Query<int>(sqlEndereco, endereco, transaction).Single();
                    }
                }

                //Deletar usuariosDepartamentos
                string sqlDeletarUsuariosDepartamento = "DELETE FROM UsuariosDepartamentos WHERE UsuarioId = @Id";
                _connection.Execute(sqlDeletarUsuariosDepartamento, usuario, transaction);

                //Adicionar novamente com os novos dados mantendo o ID
                if (usuario.Departamentos != null && usuario.Departamentos.Count > 0)
                {
                    foreach (var departamento in usuario.Departamentos)
                    {
                        string sqlUsuariosDepartamentos = $@"INSERT INTO UsuariosDepartamentos (UsuarioId, DepartamentoId)
                                                                      VALUES(@UsuarioId, @DepartamentoId)";

                        _connection.Execute(sqlUsuariosDepartamentos, new { UsuarioId = usuario.Id, DepartamentoId = departamento.Id }, transaction);
                    }
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception)
                {}
            }
            finally
            {
                _connection.Close();
            }

        }
        public void Delete(int id)
        {
            //Já é configurado no banco ON DELETE CASCADE, dizendo que todos vinculados ao UsuarioId deletado serão excluídos em um efeito cascata
            _connection.Execute("DELETE FROM Usuarios WHERE Id = @Id", new { Id = id });
        }
    }
}
