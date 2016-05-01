using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class InvalidSyntaxToken : SyntaxToken
    {
        public InvalidSyntaxToken(SyntaxNode parent, int relativePosition, string text)
            : base(parent, SyntaxKind.Invalid, relativePosition, text)
        {
        }

        public InvalidSyntaxToken(SyntaxNode parent, int relativePosition, char text)
            : base(parent, SyntaxKind.Invalid, relativePosition, text)
        {
        }
    }
}
