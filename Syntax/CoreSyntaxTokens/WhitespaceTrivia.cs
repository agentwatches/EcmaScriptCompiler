using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class WhitespaceTrivia : SyntaxTrivia
    {
        public WhitespaceTrivia(SyntaxNode parent, int relativePosition, string text) : 
            base(parent, SyntaxKind.WhitespaceTrivia, relativePosition, text)
        {
        }
    }
}
