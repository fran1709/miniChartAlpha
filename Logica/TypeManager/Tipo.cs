using Antlr4.Runtime;

namespace miniChartAlpha.Logica.TypeManager
{

    public abstract class Tipo
    {
        public IToken tok;
        public int nivel;

        public readonly string tipo;

        //se agrega este puntero para que cada identificador tenga la referencia al nodo del arbol donde
        //fue declarado (variables y métodos para Alpha)... CAMBIAN CONSTRUCTORES Y MÉTODO PARA AGREGAR
        public ParserRuleContext decl;
        public string MethodNombre { get; set; }


        protected Tipo(IToken tok, string tipo)
        {
            this.tok = tok;
            nivel = -1;
            this.tipo = tipo;
        }
    }
}