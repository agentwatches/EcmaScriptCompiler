using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class FutureReservedWordToken : SyntaxToken
    {
        string _value;

        public FutureReservedWordToken(SyntaxNode parent, int relativePosition, string text, string value)
            : base(parent, SyntaxKind.FutureReservedWordToken, relativePosition, text)
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
