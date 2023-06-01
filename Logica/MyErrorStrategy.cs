using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace miniChartAlpha.Logica
{

    public class MyErrorStrategy : DefaultErrorStrategy
    {

        protected override void ReportInputMismatch(Parser recognizer, InputMismatchException e)
        {
            string msg = "Entrada no válida " + GetTokenErrorDisplay(e.OffendingToken) + " se esperaba " +
                         e.GetExpectedTokens().ToString(recognizer.Vocabulary);
            recognizer.NotifyErrorListeners(e.OffendingToken, msg, e);
        }

        protected override void ReportMissingToken(Parser recognizer)
        {
            IToken currentToken = recognizer.CurrentToken;
            string msg = "falta " + GetExpectedTokens(recognizer).ToString(recognizer.Vocabulary) + " en " +
                         GetTokenErrorDisplay(currentToken);
            recognizer.NotifyErrorListeners(currentToken, msg, null);
        }

        protected override void ReportUnwantedToken(Parser recognizer)
        {
            IToken currentToken = recognizer.CurrentToken;
            string msg = "Entrada no válida  " + GetTokenErrorDisplay(currentToken) + " se esperaba " +
                         GetExpectedTokens(recognizer).ToString(recognizer.Vocabulary);
            recognizer.NotifyErrorListeners(currentToken, msg, null);
        }

        protected override void ReportNoViableAlternative(Parser recognizer, NoViableAltException e)
        {
            ITokenStream inputStream = (ITokenStream)recognizer.InputStream;
            string msg = "Alternativa no viable en la entrada" + EscapeWSAndQuote(inputStream == null
                ? "<unknown input>"
                : (e.StartToken.Type != -1 ? inputStream.GetText(e.StartToken, e.OffendingToken) : "<EOF>"));
            NotifyErrorListeners(recognizer, msg, null);
        }

        protected override void ReportFailedPredicate(Parser recognizer, FailedPredicateException e)
        {
            string message = "regla " + recognizer.RuleNames[recognizer.RuleContext.RuleIndex] + " " + e.Message;
            NotifyErrorListeners(recognizer, message, (RecognitionException)e);
        }
    }
}