//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.AppMagic.Authoring.Texl
{
    internal class KeyToken : Token
    {
        public KeyToken(TokKind tid, Span span)
            : base(tid, span)
        { }

        /// <summary>
        /// Copy Ctor for KeyToken used by Clone
        /// </summary>
        /// <param name="tok">The token to be copied</param>
        /// <param name="newSpan">The new span</param>
        private KeyToken(KeyToken tok, Span newSpan)
            : this(tok.Kind, newSpan)
        {
        }

        public override bool IsDottedNamePunctuator { get { return Kind == TokKind.Dot || Kind == TokKind.Bang || Kind == TokKind.BracketOpen; } }

        public override Token Clone(Span ts)
        {
            return new KeyToken(this, ts);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is KeyToken))
                return false;
            return base.Equals(that);
        }
    }
}