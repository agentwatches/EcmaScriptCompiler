using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class BooleanLiteralToken : SyntaxToken
    {
        bool _value;

        public BooleanLiteralToken(SyntaxNode parent, int relativePosition, string text, bool value)
            : base(parent, SyntaxKind.BooleanLiteralToken, relativePosition, text)
        {
            _value = value;
        }

        public bool Value
        {
            get
            {
                return _value;
            }
        }
    }
}
