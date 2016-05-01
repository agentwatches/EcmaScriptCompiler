using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class IdentifierToken : SyntaxToken
    {
        string _value;

        public IdentifierToken(SyntaxNode parent, int relativePosition, string text, string value)
            : base(parent, SyntaxKind.IdentifierToken, relativePosition, text)
        {
            _value = value;
        }

        public string Value
        {
            get
            {
                return _value;
            }
        }

    }
}
