import { ChatProvider, ProviderInput, ProviderResult } from './types';

/**
 * MiniMax chat provider — STUB.
 *
 * The MiniMax chat-completion endpoint contract has not been finalized for
 * this project. This stub keeps the provider chain shape stable so that
 * `ai_config/modul3.fallback_order` can list "minimax" without breaking;
 * the dispatcher will catch the throw and continue down the chain.
 *
 * To implement, replace the body with a call to the MiniMax chat endpoint
 * and read the API key from `process.env.MINIMAX_API_KEY`.
 */
export const minimaxProvider: ChatProvider = {
  name: 'minimax',
  async call(_input: ProviderInput): Promise<ProviderResult> {
    throw new Error('MINIMAX_NOT_IMPLEMENTED');
  },
};
