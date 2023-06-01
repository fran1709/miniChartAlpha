using Antlr4.Runtime;

namespace miniChartAlpha.Logica.TypeManager
{

    public class TipoClase : Tipo
    {
        public readonly string tipoDato;

        public TipoClase(IToken tok, string td, ParserRuleContext ctx) : base(tok, "tipoClase")
        {
            tipoDato = td;
            this.decl = ctx;
        }

        public static bool IsTipoClase(string ident)
        {
            return Clase.IsClase(ident);
        }

        public override string ToString()
        {
            return $"Token: {tok.Text}, Tipo: {tipo}, Tipo de dato: {tipoDato}, Nivel: {nivel} " + "\n";
        }


    }
}