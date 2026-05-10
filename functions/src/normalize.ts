/**
 * Strips reasoning / thinking blocks emitted by some chat models so that only
 * the final user-facing answer is returned. Returns "" if nothing is left
 * after stripping (caller treats empty as failure).
 */
export function stripReasoning(text: string): string {
  if (!text) {
    return '';
  }

  let out = text;

  // <think>...</think> (greedy, multiline)
  out = out.replace(/<think>[\s\S]*?<\/think>/gi, '');
  // <thinking>...</thinking>
  out = out.replace(/<thinking>[\s\S]*?<\/thinking>/gi, '');

  // Leading "Reasoning:" / "Thinking:" prose blocks before the final answer.
  // We strip only when the block appears at the very start, up to a blank
  // line or an explicit "Answer:" / "Final answer:" marker.
  const reasoningPrefix = /^(?:\s*(?:Reasoning|Thinking)\s*:\s*[\s\S]*?(?:\n\s*\n|\n\s*(?:Answer|Final\s+answer)\s*:\s*))/i;
  out = out.replace(reasoningPrefix, '');

  return out.trim();
}
