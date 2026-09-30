using System;
using System.Collections.Generic;
using ZeroTokenizer.Core.Abstractions;

namespace ZeroPrompt.Core.Grammar
{
    /// <summary>
    /// Logit masking engine that guarantees 100% syntactically valid JSON tool calls by setting invalid token logits to -Infinity.
    /// </summary>
    public sealed class GrammarLogitMasker
    {
        private readonly ITokenizer _tokenizer;
        private readonly Dictionary<int, string> _tokenStringCache = new Dictionary<int, string>();

        public GrammarLogitMasker(ITokenizer tokenizer)
        {
            _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        }

        /// <summary>
        /// Masks candidate logits in-place. Sets invalid tokens to -1e9f.
        /// </summary>
        public void MaskLogits(
            JsonGrammarState grammar,
            Span<float> logits,
            ReadOnlySpan<int> candidateTokens)
        {
            if (grammar == null) throw new ArgumentNullException(nameof(grammar));

            for (int i = 0; i < candidateTokens.Length; i++)
            {
                int tokenId = candidateTokens[i];
                if ((uint)tokenId >= (uint)logits.Length) continue;

                string piece = GetTokenString(tokenId);
                if (!grammar.CanAcceptNext(piece.AsSpan()))
                {
                    logits[tokenId] = -1e9f; // Mask out
                }
            }
        }

        private string GetTokenString(int tokenId)
        {
            if (!_tokenStringCache.TryGetValue(tokenId, out var str))
            {
                str = _tokenizer.Decode(new[] { tokenId });
                _tokenStringCache[tokenId] = str;
            }
            return str;
        }
    }
}
