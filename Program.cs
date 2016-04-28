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

            Endereco e1 = new Endereco() { Cep = "92200-270", Estado = "RS", Cidade = "Canoas", Rua = "Paes Lemes", Numero = "720", Complemento = "c/48" };
            Endereco e2 = new Endereco() { Cep = "56284-468", Estado = "RS", Cidade = "Tramandai", Rua = "Almirante Tramandaré", Numero = "936" };
            Endereco e3 = new Endereco() { Cep = "65723-293", Estado = "RS", Cidade = "Porto Alegre", Rua = "General Portinho", Numero = "543", Complemento = "ap/6" };

            e1 = db.Enderecos.Add(e1);
            e2 = db.Enderecos.Add(e2);
            e3 = db.Enderecos.Add(e3);

            Pessoa p1 = new Pessoa { endereco = e1, Altura = 1.55, Idade = 26, Nome = "Joseane", Peso = 56 };
            Pessoa p2 = new Pessoa { endereco = e2, Altura = 1.67, Idade = 19, Nome = "Yasser", Peso = 69 };
            Pessoa p3 = new Pessoa { endereco = e3, Altura = 1.72, Idade = 32, Nome = "João", Peso = 72 };
            Pessoa p4 = new Pessoa { endereco = e1, Altura = 1.84, Idade = 17, Nome = "Mariléia", Peso = 60 };
            Pessoa p5 = new Pessoa { endereco = e2, Altura = 2.00, Idade = 25, Nome = "Roger", Peso = 70 };
            Pessoa p6 = new Pessoa { endereco = e3, Altura = 1.66, Idade = 20, Nome = "Alexandre", Peso = 68 };

            p1 = db.Pessoas.Add(p1);
            p2 = db.Pessoas.Add(p2);
            p3 = db.Pessoas.Add(p3);
            p4 = db.Pessoas.Add(p4);
            p5 = db.Pessoas.Add(p5);
            p6 = db.Pessoas.Add(p6);

            Funcionario f1 = new Funcionario { pessoa = p1, Salario = 400.00 };
            Funcionario f2 = new Funcionario { pessoa = p2, Salario = 1400.00 };
            Funcionario f3 = new Funcionario { pessoa = p3, Salario = 300.00 };
            Funcionario f4 = new Funcionario { pessoa = p4, Salario = 1300.00 };
            Funcionario f5 = new Funcionario { pessoa = p5, Salario = 200.00 };
            Funcionario f6 = new Funcionario { pessoa = p6, Salario = 1200.00 };

            f1 = db.Funcionarios.Add(f1);
            f2 = db.Funcionarios.Add(f2);
            f3 = db.Funcionarios.Add(f3);
            f4 = db.Funcionarios.Add(f4);
            f5 = db.Funcionarios.Add(f5);
            f6 = db.Funcionarios.Add(f6);

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

            pessoas = db.Pessoas.Where(p => p.Nome != "Joseane" && p.Altura > 1 && p.Peso > 1);
        }
    }
}
