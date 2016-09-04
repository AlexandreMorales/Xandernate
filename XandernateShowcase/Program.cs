using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xandernate.Dao;
using XandernateShowcase.Dao;
using XandernateShowcase.Models;

namespace XandernateShowcase
{
    class Program
    {
        static void Main(string[] args)
        {
            //DBFirst.Init();

            Contexto db = new Contexto();

            Endereco e1 = new Endereco() { Cep = "92200-270", Estado = "RS", Cidade = "Canoas", Rua = "Paes Lemes", Numero = "720", Complemento = "c/48" };
            Endereco e2 = new Endereco() { Cep = "56284-468", Estado = "RS", Cidade = "Tramandai", Rua = "Almirante Tramandaré", Numero = "936" };
            Endereco e3 = new Endereco() { Cep = "65723-293", Estado = "RS", Cidade = "Porto Alegre", Rua = "General Portinho", Numero = "543", Complemento = "ap/6" };

            Pessoa p1 = new Pessoa { endereco = e1, Altura = 1.55, Idade = 26, Nome = "Joseane", Peso = 56 };
            Pessoa p2 = new Pessoa { endereco = e2, Altura = 1.67, Idade = 19, Nome = "Yasser", Peso = 69 };
            Pessoa p3 = new Pessoa { endereco = e3, Altura = 1.72, Idade = 32, Nome = "João", Peso = 72 };
            Pessoa p4 = new Pessoa { endereco = e1, Altura = 1.84, Idade = 17, Nome = "Mariléia", Peso = 60 };
            Pessoa p5 = new Pessoa { endereco = e2, Altura = 2.00, Idade = 25, Nome = "Roger", Peso = 70 };
            Pessoa p6 = new Pessoa { endereco = e3, Altura = 1.66, Idade = 20, Nome = "Alexandre", Peso = 68 };

            Funcionario f1 = new Funcionario { pessoa = p1, Salario = 400.00 };
            Funcionario f2 = new Funcionario { pessoa = p2, Salario = 1400.00 };
            Funcionario f3 = new Funcionario { pessoa = p3, Salario = 300.00 };
            Funcionario f4 = new Funcionario { pessoa = p4, Salario = 1300.00 };
            Funcionario f5 = new Funcionario { pessoa = p5, Salario = 200.00 };
            Funcionario f6 = new Funcionario { pessoa = p6, Salario = 1200.00 };

            db.Funcionarios.AddOrUpdate(f => f.Salario, f1, f2, f3, f4, f5, f6);

            List<Funcionario> funcionarios = db.Funcionarios.FindAll();
            p1 = db.Pessoas.Find(1);
            p1 = db.Pessoas.Find(p => p.Nome, "Joseane");

            p3.Altura = 2;
            db.Pessoas.AddOrUpdate(p3);
            db.Pessoas.AddOrUpdate(x => x.Nome, new Pessoa { endereco = e1, Altura = 2, Idade = 200, Nome = "Joseane", Peso = 56 });

            List<Pessoa> pessoas = db.Pessoas.FindAll();

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
