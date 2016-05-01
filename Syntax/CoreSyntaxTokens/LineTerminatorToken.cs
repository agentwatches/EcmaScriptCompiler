using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class EndOfLineTrivia : SyntaxTrivia
    {
        public EndOfLineTrivia(SyntaxNode parent, int relativePosition, string text)
            : base(parent, SyntaxKind.EndOfLineTrivia, relativePosition, text)
        {
        }
    }
}
