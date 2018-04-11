using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using XandernateShowcase.Infra;
using XandernateShowcase.Models;

namespace XandernateShowcase
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfigurationRoot configuration = 
                new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();
            
            Contexto db = new Contexto(configuration.GetConnectionString("Default"));

            //db.Query("DROP TABLE Funcionario;\nDROP TABLE Pessoa;\nDROP TABLE Endereco;");

            db.CreateDb();

            Endereco e1 = new Endereco() { Cep = "92200-270", Estado = "RS", Cidade = "Canoas", Rua = "Paes Lemes", Numero = "720", Complemento = "c/48" };
            Endereco e2 = new Endereco() { Cep = "56284-468", Estado = "RS", Cidade = "Tramandai", Rua = "Almirante Tramandaré", Numero = "936" };
            Endereco e3 = new Endereco() { Cep = "65723-293", Estado = "RS", Cidade = "Porto Alegre", Rua = "General Portinho", Numero = "543", Complemento = "ap/6" };

            Pessoa p1 = new Pessoa { Endereco = e1, Altura = 1.55, Idade = 26, Nome = "Joseane", Peso = 56 };
            Pessoa p2 = new Pessoa { Endereco = e2, Altura = 1.67, Idade = 19, Nome = "Yasser", Peso = 69 };
            Pessoa p3 = new Pessoa { Endereco = e3, Altura = 1.72, Idade = 32, Nome = "João", Peso = 72 };
            Pessoa p4 = new Pessoa { Endereco = e1, Altura = 1.84, Idade = 17, Nome = "Mariléia", Peso = 60 };
            Pessoa p5 = new Pessoa { Endereco = e2, Altura = 2.00, Idade = 25, Nome = "Roger", Peso = 70 };
            Pessoa p6 = new Pessoa { Endereco = e3, Altura = 1.66, Idade = 20, Nome = "Alexandre", Peso = 68 };

            Funcionario f1 = new Funcionario { Pessoa = p1, Salario = 400.00 };
            Funcionario f2 = new Funcionario { Pessoa = p2, Salario = 1400.00 };
            Funcionario f3 = new Funcionario { Pessoa = p3, Salario = 300.00 };
            Funcionario f4 = new Funcionario { Pessoa = p4, Salario = 1300.00 };
            Funcionario f5 = new Funcionario { Pessoa = p5, Salario = 200.00 };
            Funcionario f6 = new Funcionario { Pessoa = p6, Salario = 1200.00 };

            db.Funcionarios.AddOrUpdate(f => f.Salario, f1, f2, f3, f4, f5, f6);

            IEnumerable<Funcionario> funcionarios = db.Funcionarios.FindAll();
            p1 = db.Pessoas.Find(1);
            p1 = db.Pessoas.Find(p => p.Nome, "Joseane");

            p3.Altura = 2;
            db.Pessoas.AddOrUpdate(p3);
            // TODO
            db.Pessoas.AddOrUpdate(x => x.Nome, new Pessoa { Endereco = e1, Altura = 2, Idade = 200, Nome = "Joseane", Peso = 56 });

            IEnumerable<Pessoa> pessoas = db.Pessoas.FindAll();

            p1.Idade = 25;
            p1.Peso = 35;
            db.Pessoas.Update(p => p.Idade, p1);
            p1.Peso = 35;
            db.Pessoas.Update(p1);

            pessoas = db.Pessoas.WhereEquals(() => p1.Nome);

            pessoas = db.Pessoas.Where(p => p.Nome != "Joseane" && p.Altura > 1 && p.Peso > 1);

            db.Pessoas.Remove(p3);
            db.Pessoas.Remove(4);
            db.Pessoas.Remove(p => p.Altura == 1.67);
            db.Pessoas.Remove(p => p.Idade, 20);

            pessoas = db.Pessoas.FindAll();
        }
    }
}
