using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class MultiLineCommentTrivia : SyntaxTrivia
    {
        public MultiLineCommentTrivia(SyntaxNode parent, int relativePosition, string text) :
            base(parent, SyntaxKind.MultiLineCommentTrivia, relativePosition, text)
        {
        }
    }
}
