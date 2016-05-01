using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcmaScriptCompiler.Syntax
{
    public class SyntaxTree : SyntaxNode
    {
        public SyntaxTree() : base(null, SyntaxKind.SyntaxTreeRoot, 0)
        {
            _children = new List<SyntaxTreeItem>();
        }
    }
}
