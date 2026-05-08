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

  out = out.replace(/<think>[\s\S]*?<\/think>/gi, '');
  out = out.replace(/<thinking>[\s\S]*?<\/thinking>/gi, '');

  const reasoningPrefix = /^(?:\s*(?:Reasoning|Thinking)\s*:\s*[\s\S]*?(?:\n\s*\n|\n\s*(?:Answer|Final\s+answer)\s*:\s*))/i;
  out = out.replace(reasoningPrefix, '');

  return out.trim();
}
