using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;

namespace miniChartAlpha.Logica.TypeManager
{

    public class Clase : Tipo
    {
        public readonly string tipoDato;
        public LinkedList<Object> variables;
        public LinkedList<Object> metodos;

        public Clase(IToken tok, ParserRuleContext ctx) : base(tok, "clase")
        {
            tipoDato = "null";
            variables = new LinkedList<object>();
            metodos = new LinkedList<object>();
            this.decl = ctx;
        }

        public static bool IsClase(string nombre)
        {
            return !TipoBasico.isTipoBasico(nombre) && !string.IsNullOrEmpty(nombre) &&
                   nombre.All(c => char.IsLetterOrDigit(c));
        }

        public void addVariable(object variable)
        {
            variables.AddLast(variable);
        }

        public string imprimirVars()
        {
            string vars = "";
            if (variables.Count != 0)
            {
                vars += "\n----- INICIO DE VARIABLES -----\n";
                foreach (object id in variables)
                {
                    vars += id.ToString();
                }

                vars += "----- FIN DE VARIABLES ------\n";
            }

            return vars;
        }

        public T buscarAtributo<T>(string nombre) where T : Tipo
        {
            foreach (object id in variables)
            {
                T obj = id as T;
                if (obj != null && obj.tok.Text.Equals(nombre))
                {
                    return obj;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return $"Token: {tok.Text}, Tipo: {tipo}, Tipo de dato: {tipoDato}, Nivel: {nivel} " + imprimirVars() +
                   "\n";
        }
    }
}