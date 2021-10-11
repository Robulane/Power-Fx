//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.AppMagic.Authoring.Texl.SourceInformation;

namespace Microsoft.AppMagic.Authoring.Texl
{
    internal sealed class BoolLitNode : TexlNode
    {
        public BoolLitNode(ref int idNext, Token tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
            Contracts.AssertValue(tok);
            Contracts.Assert(tok.Kind == TokKind.True || tok.Kind == TokKind.False);
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            return new BoolLitNode(ref idNext, Token.Clone(ts));
        }

        public bool Value { get { return Token.Kind == TokKind.True; } }

        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            visitor.Visit(this);
        }

        public override Result Accept<Result, Context>(TexlFunctionalVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }

        public override NodeKind Kind { get; } = NodeKind.BoolLit;

        public override BoolLitNode CastBoolLit()
        {
            return this;
        }

        public override BoolLitNode AsBoolLit()
        {
            return this;
        }
    }
}