import { stripReasoning } from '../normalize';

describe('stripReasoning', () => {
  it('strips <think>...</think> blocks', () => {
    const input = '<think>internal reasoning here</think>Final answer body.';
    expect(stripReasoning(input)).toBe('Final answer body.');
  });

  it('strips <thinking>...</thinking> blocks', () => {
    const input = '<thinking>weighing options</thinking>\n\nDoktor: Hasta stabil.';
    expect(stripReasoning(input)).toBe('Doktor: Hasta stabil.');
  });

  it('strips leading "Reasoning:" prose before answer', () => {
    const input = 'Reasoning: vitals are okay\n\nHasta yeşil triajda kalabilir.';
    expect(stripReasoning(input)).toBe('Hasta yeşil triajda kalabilir.');
  });

  it('returns "" when input is only a stripped block', () => {
    expect(stripReasoning('<think>only this</think>')).toBe('');
    expect(stripReasoning('<thinking>only this</thinking>')).toBe('');
    expect(stripReasoning('')).toBe('');
  });

  it('passes plain answers through (trimmed)', () => {
    expect(stripReasoning('  Plain answer.  ')).toBe('Plain answer.');
  });
});
