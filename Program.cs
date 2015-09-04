using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xandernate.Dao;
using Xandernate.Dto;

namespace Xandernate
{
    class Program
    {
        static void Main(string[] args)
        {
            Contexto db = new Contexto();
            Pessoa p1 = new Pessoa { Altura = 1.85, Idade = 26, Nome = "Joseane", Peso = 56 };
            Pessoa p2 = new Pessoa { Altura = 1.66, Idade = 19, Nome = "Yasser", Peso = 69 };
            Pessoa p3 = new Pessoa { Altura = 1.66, Idade = 19, Nome = "Yasser", Peso = 69 };
            Pessoa p4 = new Pessoa { Altura = 1.66, Idade = 19, Nome = "Yasser", Peso = 69 };
            Pessoa p5 = new Pessoa { Altura = 1.67, Idade = 19, Nome = "Yasser", Peso = 69 };
            Pessoa p6 = new Pessoa { Altura = 1.66, Idade = 20, Nome = "Yasser", Peso = 69 };

            p1 = db.Pessoas.Add(p1);
            p2 = db.Pessoas.Add(p2);
            p3 = db.Pessoas.Add(p3);
            p4 = db.Pessoas.Add(p4);
            p5 = db.Pessoas.Add(p5);
            p6 = db.Pessoas.Add(p6);

            Funcionario f1 = new Funcionario { pessoa = p1, Salario = 400.00 };
            Funcionario f2 = new Funcionario { pessoa = p2, Salario = 1400.00 };

            f1 = db.Funcionarios.Add(f1);
            f2 = db.Funcionarios.Add(f2);

            p1 = db.Pessoas.Find(1);

            Pessoa[] pessoas = db.Pessoas.FindAll();

            db.Pessoas.Remove(p3);
            db.Pessoas.Remove(4);
            db.Pessoas.Remove(p => p.Altura == 1.67);
            db.Pessoas.Remove(p => p.Idade, 20);

            db.Pessoas.AddRange(pessoas);

            db.Pessoas.AddOrUpdate(p6);
            db.Pessoas.AddOrUpdate(p => p.Altura, p5);

            p1.Idade = 25;

            db.Pessoas.Update(p1);

            pessoas = db.Pessoas.WhereEquals(() => p1.Nome);

            pessoas = db.Pessoas.Where(p => p.Nome == "Joseane" && p.Altura > 1 && p.Peso > 1);
        }
    }
}
