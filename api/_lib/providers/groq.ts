import type { ChatProvider, ProviderInput, ProviderResult } from './types';

const GROQ_URL = 'https://api.groq.com/openai/v1/chat/completions';

export const groqProvider: ChatProvider = {
  name: 'groq',
  async call(input: ProviderInput): Promise<ProviderResult> {
    const apiKey = process.env.GROQ_API_KEY;
    if (!apiKey) {
      throw new Error('GROQ_API_KEY missing');
    }

    const model = input.model || 'llama-3.1-70b-versatile';
    const messages: Array<{ role: string; content: string }> = [];
    if (input.systemPrompt) {
      messages.push({ role: 'system', content: input.systemPrompt });
    }
    for (const m of input.conversation) {
      if (!m || !m.role || !m.content) continue;
      messages.push({ role: m.role, content: m.content });
    }
    messages.push({ role: 'user', content: input.message });

    const res = await fetch(GROQ_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${apiKey}`,
      },
      body: JSON.stringify({
        model,
        messages,
        temperature: input.temperature,
        max_tokens: input.maxTokens,
        stream: false,
      }),
    });

    if (!res.ok) {
      throw new Error(`groq http ${res.status}`);
    }

    const json: any = await res.json();
    const answer: string = json?.choices?.[0]?.message?.content ?? '';
    if (!answer) {
      throw new Error('groq empty answer');
    }
    return { answer, model };
  },
};
