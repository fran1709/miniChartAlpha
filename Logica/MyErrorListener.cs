using System.Collections.Generic;
using System.IO;

namespace miniChartAlpha.Logica{

using Antlr4.Runtime;

public class MyErrorListener : BaseErrorListener
{
    public List<string> SyntaxErrors { get; } = new List<string>();

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
        int charPositionInLine,
        string msg, RecognitionException e)
    {
        base.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
        string error = $"Error de sintaxis en línea {line}, posición {charPositionInLine}: {msg}";
        SyntaxErrors.Add(error);

    }


}
}
