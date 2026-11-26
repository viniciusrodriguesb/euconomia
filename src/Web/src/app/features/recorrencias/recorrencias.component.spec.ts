import { expect, it, vi } from 'vitest';
import { cadastros, criarTela, estabilizar, fatura, ocorrencia, pagina } from '../../../../test/fixtures';
import { RecorrenciasComponent } from './recorrencias.component';

const rota = 'recorrencias/previsao/paginada';
const fontes = { cadastros, 'alertas/configuracao': { confirmarAoVirarMes: false }, [rota]: pagina([ocorrencia], 10, 32) };

it('pagina no servidor, mantém filtros aplicados e corrige uma página que deixou de existir', async () => {
  const { tela, fixture, api } = await criarTela(RecorrenciasComponent, fontes);
  expect(tela.ocorrencias()?.itens).toEqual([ocorrencia]);
  expect(tela.contas()).toHaveLength(1);
  expect(tela.categorias()).toHaveLength(1);
  expect(tela.cartoes()).toHaveLength(1);
  tela.filtros.patchValue({ inicio: '2026-06', meses: 12 });
  tela.buscar();
  await estabilizar(fixture);
  expect(api.get).toHaveBeenLastCalledWith(rota, { inicio: '2026-06-01', meses: 12, pagina: 1, tamanho: 10 });
  tela.filtros.controls.inicio.setValue('2026-07');
  tela.paginar(3, 20);
  await estabilizar(fixture);
  expect(api.get).toHaveBeenLastCalledWith(rota, { inicio: '2026-06-01', meses: 12, pagina: 2, tamanho: 20 });
});

it('valida cadastros para recorrências em conta e cartão', async () => {
  const { tela, fixture, api } = await criarTela(RecorrenciasComponent, fontes);
  tela.registrar();
  await estabilizar(fixture);
  expect(api.post).not.toHaveBeenCalled();

  tela.regra.patchValue({ descricao: 'Internet', valorBase: '100,50', categoriaId: 'categoria' });
  tela.registrar();
  await estabilizar(fixture);
  expect(tela.erro()).toBe('Selecione uma conta.');
  tela.regra.patchValue({ contaId: 'conta' });
  tela.registrar();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenLastCalledWith('recorrencias', expect.objectContaining({ valorBase: '100.50', competenciaFim: null }));

  tela.regra.patchValue({ tipo: 'AssinaturaCartao', descricao: 'Assinatura', valorBase: '1', categoriaId: 'categoria', competenciaFim: '2027-01' });
  tela.trocarTipo();
  tela.registrar();
  await estabilizar(fixture);
  expect(tela.erro()).toBe('Selecione um cartão.');
  tela.regra.patchValue({ cartaoId: 'cartao' });
  tela.registrar();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenLastCalledWith('recorrencias', expect.objectContaining({
    competenciaFim: '2027-01-01', contaId: 'conta', cartaoId: 'cartao', formaPagamento: 'CartaoCredito'
  }));
});

it('ativa e desativa a confirmação automática e reverte o estado em erro', async () => {
  const { tela, fixture, api } = await criarTela(RecorrenciasComponent, fontes);
  tela.alternarAutomatico(true);
  await estabilizar(fixture);
  expect(tela.aviso()).toContain('ativada');
  api.put.mockRejectedValueOnce(new Error('Indisponível'));
  tela.alternarAutomatico(false);
  await estabilizar(fixture);
  expect(tela.confirmacaoAutomatica()).toBe(true);
  expect(tela.erro()).toBe('Indisponível');
  tela.alternarAutomatico(false);
  await estabilizar(fixture);
  expect(tela.confirmacaoAutomatica()).toBe(false);
  expect(tela.aviso()).toContain('desativada');
});

it('ajusta mês ou versão, pula, restaura e encerra com as confirmações previstas', async () => {
  const { tela, fixture, api } = await criarTela(RecorrenciasComponent, fontes);
  const confirmar = vi.spyOn(window, 'confirm').mockReturnValue(false);
  tela.editar(ocorrencia);
  tela.ajuste.patchValue({ valor: '42,10' });
  tela.salvarAjuste(ocorrencia);
  await estabilizar(fixture);
  expect(api.put).toHaveBeenLastCalledWith('recorrencias/regra/excecao', expect.objectContaining({ valor: '42.10' }));
  tela.editar(ocorrencia);
  tela.ajuste.patchValue({ modo: 'adiante', valor: '49' });
  tela.salvarAjuste(ocorrencia);
  tela.encerrar(ocorrencia);
  expect(api.post).not.toHaveBeenCalled();
  confirmar.mockReturnValue(true);
  tela.salvarAjuste(ocorrencia);
  await estabilizar(fixture);
  expect(api.post).toHaveBeenLastCalledWith('recorrencias/regra/versao', expect.objectContaining({ valor: '49' }));
  tela.excecao(ocorrencia, true);
  await estabilizar(fixture);
  expect(tela.aviso()).toBe('Mês pulado.');
  tela.excecao(ocorrencia, false);
  await estabilizar(fixture);
  expect(api.put).toHaveBeenLastCalledWith('recorrencias/regra/excecao', expect.objectContaining({ restaurar: true }));
  tela.encerrar(ocorrencia);
  await estabilizar(fixture);
  expect(api.post).toHaveBeenLastCalledWith('recorrencias/regra/encerrar', expect.objectContaining({ versao: 1 }));
});

it('mostra o efeito de caixa ou fatura e guarda a chave quando o usuário cancela', async () => {
  const { tela, fixture, api } = await criarTela(RecorrenciasComponent, fontes);
  const confirmar = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValue(true);
  const efeitoCaixa = { impactoNoSaldo: '-40', faturas: [] };
  api.post.mockResolvedValueOnce(efeitoCaixa).mockResolvedValueOnce(efeitoCaixa);
  tela.confirmar({ ...ocorrencia, tipo: 'AporteFixo' });
  await estabilizar(fixture);
  expect(api.post).toHaveBeenCalledTimes(1);
  expect(confirmar).toHaveBeenLastCalledWith(expect.stringContaining('Impacto no saldo'));
  tela.confirmar({ ...ocorrencia, tipo: 'ReceitaFixa' });
  await estabilizar(fixture);
  expect(api.post).toHaveBeenLastCalledWith('recorrencias/regra/confirmar', expect.objectContaining({ efeitoEsperado: efeitoCaixa }));
  const pedidos = api.post.mock.calls.slice(0, 2).map(chamada => chamada[1]);
  expect(pedidos[1]).toMatchObject({ ...pedidos[0] as object, tipo: 'Receita' });
  api.post.mockResolvedValueOnce({ faturas: [{ valor: fatura.total, competencia: fatura.competencia, vencimento: fatura.dataVencimento }] });
  tela.confirmar(ocorrencia);
  await estabilizar(fixture);
  expect(confirmar).toHaveBeenLastCalledWith(expect.stringContaining('na fatura'));
  expect(tela.aviso()).toContain('Ocorrência confirmada');
});
