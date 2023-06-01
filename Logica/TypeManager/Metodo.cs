using System;
using System.Collections.Generic;
using Antlr4.Runtime;

namespace miniChartAlpha.Logica.TypeManager
{

    public class Metodo : Tipo
    {
        public enum TipoMetodo
        {
            Void = 7,
            Error = 8,
            Multiple = -99,
        }

        public readonly int tipoDato;
        public int cantidadParam;
        public LinkedList<Object> parametros;

        public Metodo(IToken tok, int td, ParserRuleContext ctx) : base(tok, "metodo")
        {
            tipoDato = td;
            cantidadParam = 0;
            parametros = new LinkedList<object>();
            this.decl = ctx;
        }

        public static string showTipo(int tipoDato)
        {
            switch (tipoDato)
            {
                case (int)TipoBasico.TiposBasicos.Int: return TipoBasico.TiposBasicos.Int.ToString().ToLower();
                case (int)TipoBasico.TiposBasicos.Double: return TipoBasico.TiposBasicos.Double.ToString().ToLower();
                case (int)TipoBasico.TiposBasicos.Char: return TipoBasico.TiposBasicos.Char.ToString().ToLower();
                case (int)TipoBasico.TiposBasicos.String: return TipoBasico.TiposBasicos.String.ToString().ToLower();
                case (int)TipoBasico.TiposBasicos.Boolean: return TipoBasico.TiposBasicos.Boolean.ToString().ToLower();
                case (int)TipoMetodo.Void: return TipoMetodo.Void.ToString().ToLower();
                default: return TipoMetodo.Error.ToString();
            }
        }

        public static bool checkParsType(Object declType, Object actType)
        {
            if (declType is TipoBasico basicoExpected && actType is int)
            {
                return basicoExpected.tipoDato == (int)actType;
            }

            if (declType is TipoBasico expected && actType is TipoBasico.TiposBasicos)
            {
                return expected.tipoDato == (int)actType;
            }

            if (declType is TipoClase claseExpected && actType is string)
            {
                return claseExpected.tipoDato == (string)actType;
            }

            if (declType is Arreglo arregloExpected && actType is int)
            {
                return arregloExpected.tipoDato == (int)actType;
            }

            if (declType is int && actType is int)
            {
                return (int)declType == (int)actType;
            }

            return false;
        }

        public string imprimirParametros()
        {
            string param = "";
            if (parametros.Count != 0)
            {
                param += "----- INICIO DE PARAMETROS -----\n";
                foreach (object id in parametros)
                {
                    param += id.ToString();
                }

                param += "----- FIN DE PARAMETROS ------\n";
            }

            return param;
        }

        public override string ToString()
        {
            if (tok == null)
            {
                return
                    $"Token: {MethodNombre}, Tipo: {tipo}, Tipo de dato: {showTipo(tipoDato)}, Nivel: {nivel}, Cantidad de parametros: {cantidadParam}" +
                    "\n" + imprimirParametros();

            }

            return
                $"Token: {tok.Text}, Tipo: {tipo}, Tipo de dato: {showTipo(tipoDato)}, Nivel: {nivel}, Cantidad de parametros: {cantidadParam}" +
                "\n" + imprimirParametros();
        }
    }
}