using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class NullLiteralToken : SyntaxToken
    {
        public NullLiteralToken(SyntaxNode parent, int relativePosition, string text)
            : base(parent, SyntaxKind.NullLiteralToken, relativePosition, text)
        {
        }
    }
}
