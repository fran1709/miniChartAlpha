using System.Text.RegularExpressions;
using Antlr4.Runtime;

namespace miniChartAlpha.Logica.TypeManager{

public class Arreglo : Tipo
{
    public enum TiposArreglo
    {
        Int,
        Char,
        Error,
    }

    public readonly int tipoDato;
    
    public Arreglo(IToken tok, int td, ParserRuleContext ctx) : base(tok, "array")
    {
        tipoDato = td;
        this.decl = ctx;
    }
    
    public static bool isTipoArreglo(string tipo)
    {
        return Regex.IsMatch(tipo, "^(int|char)\\[[\\w\\s]*\\]$");
    }
    
    public override string ToString()
    {
        if (tok == null)
        {
            return $"Token: arrayParam, Tipo: {tipo}, Tipo de dato: {showTipo(tipoDato)}, Nivel: {nivel} " + "\n";
        }
        return $"Token: {tok.Text}, Tipo: {tipo}, Tipo de dato: {showTipo(tipoDato)}, Nivel: {nivel} " + "\n";
    }
    
    public static string showTipo(int tipoDato)
    {
        switch (tipoDato)
        {
            case (int)TiposArreglo.Int: return TiposArreglo.Int.ToString().ToLower();
            case (int)TiposArreglo.Char: return TiposArreglo.Char.ToString().ToLower();
            case (int)Metodo.TipoMetodo.Multiple: return Metodo.TipoMetodo.Multiple.ToString().ToLower();
            default: return TiposArreglo.Error.ToString();
        }
    }
}

}