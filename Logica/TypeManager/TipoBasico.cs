using System;
using Antlr4.Runtime;

namespace miniChartAlpha.Logica.TypeManager
{

    public class TipoBasico : Tipo
    {
        public enum TiposBasicos
        {
            Int,
            Double,
            Char,
            String,
            Boolean,
            Error,
        }

        public int tipoDato;

        public TipoBasico(IToken tok, int td, ParserRuleContext ctx) : base(tok, "basico")
        {
            tipoDato = td;
            this.decl = ctx;
        }

        public static bool isTipoBasico(string tipo)
        {
            return tipo.Equals("int") || tipo.Equals("double") || tipo.Equals("char") || tipo.Equals("string") ||
                   tipo.Equals("bool");
        }

        public static string showTipo(int tipoDato)
        {
            switch (tipoDato)
            {
                case (int)TiposBasicos.Int: return TiposBasicos.Int.ToString().ToLower();
                case (int)TiposBasicos.Double: return TiposBasicos.Double.ToString().ToLower();
                case (int)TiposBasicos.Char: return TiposBasicos.Char.ToString().ToLower();
                case (int)TiposBasicos.String: return TiposBasicos.String.ToString().ToLower();
                case (int)TiposBasicos.Boolean: return TiposBasicos.Boolean.ToString().ToLower();
                case (int)Metodo.TipoMetodo.Multiple: return Metodo.TipoMetodo.Multiple.ToString().ToLower();
                default: return TiposBasicos.Error.ToString().ToLower();
            }
        }

        public override string ToString()
        {
            if (tok == null)
            {
                return $"Token: multipleParam, Tipo: {tipo}, Tipo de dato: {showTipo(tipoDato)}, Nivel: {nivel} " +
                       "\n";
            }

            return $"Token: {tok.Text}, Tipo: {tipo}, Tipo de dato: {showTipo(tipoDato)}, Nivel: {nivel} " + "\n";
        }
    }
}