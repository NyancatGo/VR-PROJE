export interface ProviderInput {
  systemPrompt?: string;
  message: string;
  conversation: Array<{ role: string; content: string }>;
  model?: string;
  temperature: number;
  maxTokens: number;
}

export interface ProviderResult {
  answer: string;
  model: string;
}

export interface ChatProvider {
  name: string;
  call(input: ProviderInput): Promise<ProviderResult>;
}
