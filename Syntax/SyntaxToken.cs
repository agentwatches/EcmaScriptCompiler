using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class SyntaxToken : SyntaxTreeItem 
    {
        string _text = ""; // the original source text

        protected SyntaxToken(SyntaxTreeItem parent, SyntaxKind kind, int relativePosition, string text) 
            : base(parent, kind, relativePosition)
        {
            _text = text;
        }

        protected SyntaxToken(SyntaxTreeItem parent, SyntaxKind kind, int relativePosition, char text)
            : base(parent, kind, relativePosition)
        {
            _text = text.ToString();
        }

        public override string Text
        {
            get
            {
                return _text;
            }
        }

        public override int Width
        {
            get
            {
                return _text.Length;
            }
        }

    }
}
