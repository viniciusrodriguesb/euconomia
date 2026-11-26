import type { Cadastros } from '../dtos/cadastros.dto';

export const vazio: Cadastros = { contas: [], categorias: [], cartoes: [], estabelecimentos: [] };
export const tipos = [
  ['Receita', 'Receita'], ['Despesa', 'Despesa'], ['AporteInvestimento', 'Aporte de investimento'],
  ['ResgateInvestimento', 'Resgate de investimento'], ['RetiradaSaldo', 'Retirada de saldo'],
  ['PagamentoFatura', 'Pagamento de fatura'], ['Estorno', 'Estorno'],
];
export const formas = [
  ['Pix', 'Pix'], ['Debito', 'Débito'], ['Dinheiro', 'Dinheiro'], ['Boleto', 'Boleto'],
  ['Transferencia', 'Transferência'], ['CartaoCredito', 'Cartão de crédito'],
];
export const recorrencias = [['CustoFixo', 'Custo fixo'], ['ReceitaFixa', 'Receita fixa'], ['AporteFixo', 'Aporte fixo'], ['AssinaturaCartao', 'Assinatura no cartão']];
