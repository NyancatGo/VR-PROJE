import { dispatchChat } from '../index';
import { ChatProvider } from '../providers/types';
import { Modul3AiConfig } from '../config';

function makeProvider(name: string, impl: ChatProvider['call']): ChatProvider {
  return { name, call: impl };
}

describe('dispatchChat fallback chain', () => {
  const baseConfig: Modul3AiConfig = {
    enabled: true,
    provider: 'primary',
    fallback_order: ['secondary'],
    temperature: 0.4,
    max_tokens: 500,
  };

  it('falls back to secondary when primary throws', async () => {
    const providers = {
      primary: makeProvider('primary', async () => {
        throw new Error('primary down');
      }),
      secondary: makeProvider('secondary', async () => ({
        answer: 'secondary answer',
        model: 'm2',
      })),
    };

    const result = await dispatchChat(
      { message: 'hi', conversation: [] },
      { providers, loadConfig: async () => baseConfig }
    );

    expect(result.ok).toBe(true);
    expect(result.provider).toBe('secondary');
    expect(result.answer).toBe('secondary answer');
    expect(result.source).toBe('fallback');
  });

  it('returns AI_TEMPORARILY_UNAVAILABLE when all providers fail', async () => {
    const providers = {
      primary: makeProvider('primary', async () => {
        throw new Error('primary down');
      }),
      secondary: makeProvider('secondary', async () => {
        throw new Error('secondary down');
      }),
    };

    const result = await dispatchChat(
      { message: 'hi', conversation: [] },
      { providers, loadConfig: async () => baseConfig }
    );

    expect(result.ok).toBe(false);
    expect(result.error).toBe('AI_TEMPORARILY_UNAVAILABLE');
  });

  it('returns AI_DISABLED when config.enabled is false', async () => {
    const providers = {
      primary: makeProvider('primary', async () => ({ answer: 'unused', model: 'm' })),
    };

    const result = await dispatchChat(
      { message: 'hi', conversation: [] },
      {
        providers,
        loadConfig: async () => ({ ...baseConfig, enabled: false }),
      }
    );

    expect(result.ok).toBe(false);
    expect(result.error).toBe('AI_DISABLED');
  });
});
