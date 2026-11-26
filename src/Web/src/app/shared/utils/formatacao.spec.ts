import { describe, expect, it, vi } from 'vitest';
import { BrlPipe } from '../pipes/brl.pipe';
import { DiaBrPipe } from '../pipes/dia-br.pipe';
import { MesBrPipe } from '../pipes/mes-br.pipe';
import { RotuloPipe } from '../pipes/rotulo.pipe';
import { centavos, valorApi } from './moeda';
import { hoje, inicioMes, mesAtual, somarMes } from './datas';

describe('datas e formatação de valores', () => {
  it('usa a data local e atravessa mudanças de ano', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 0, 5, 12));
    expect(hoje()).toBe('2026-01-05');
    expect(mesAtual()).toBe('2026-01');
    expect(inicioMes(mesAtual())).toBe('2026-01-01');
    expect(somarMes('2026-12', 2)).toBe('2027-02');
    expect(somarMes('2026-01', -1)).toBe('2025-12');
    expect(new DiaBrPipe().transform('2026-01-05T21:00')).toBe('05/01/2026');
    expect(new MesBrPipe().transform('2026-01')).toBe('jan/2026');
  });

  it('converte centavos sem ponto flutuante e rejeita entradas inválidas', () => {
    expect(valorApi(' -100,07 ')).toBe('-100.07');
    expect(valorApi('6')).toBe('6');
    expect(() => valorApi('1.234,56')).toThrow('duas casas decimais');
    expect(centavos('-100.07')).toBe(-10007n);
    expect(centavos('9999999999999999')).toBe(999999999999999900n);
    const formatador = new BrlPipe();
    expect(formatador.transform('-1234.5')).toBe('−R$ 1.234,50');
    expect(formatador.transform(52n)).toBe('R$ 0,52');
    expect(formatador.transform(null)).toBe('—');
  });

  it('aplica rótulos conhecidos e mantém desconhecidos', () => {
    const formatador = new RotuloPipe();
    expect(formatador.transform('CartaoCredito')).toBe('Cartão de crédito');
    expect(formatador.transform('AssinaturaCartao')).toBe('Assinatura no cartão');
    expect(formatador.transform('Outro')).toBe('Outro');
  });
});
