using Antlr.Runtime;
using Errors;

namespace Syntaxis
{
    public partial class TigerLexer
    {
        private readonly Report Report;

        public TigerLexer(Report report, ICharStream characters) : this(characters)
        {
            Report = report;
        }

        public override void ReportError(RecognitionException e)
        {
            Report.AddError(e.Line, e.CharPositionInLine, GetErrorMessage(e, TokenNames));
        }
    }

    public partial class TigerParser
    {
        private readonly Report Report;

        public TigerParser(Report report, ITokenStream tokens) : this(tokens)
        {
            Report = report;
        }

        public override void ReportError(RecognitionException e)
        {
            Report.AddError(e.Line, e.CharPositionInLine, GetErrorMessage(e, TokenNames));
        }
    }
}
