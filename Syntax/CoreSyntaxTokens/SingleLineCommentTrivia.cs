using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class SingleLineCommentTrivia : SyntaxTrivia
    {
        public SingleLineCommentTrivia(SyntaxNode parent, int relativePosition, string text) :
            base(parent, SyntaxKind.SingleLineCommentTrivia, relativePosition, text)
        {
        }
    }
}
