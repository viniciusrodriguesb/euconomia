export function hoje() {
  const data = new Date();
  return `${data.getFullYear()}-${String(data.getMonth() + 1).padStart(2, '0')}-${String(data.getDate()).padStart(2, '0')}`;
}
export function mesAtual() { return hoje().slice(0, 7); }
export function inicioMes(mes: string) { return `${mes}-01`; }
export function somarMes(mes: string, meses: number) {
  const [ano, mesDoAno] = mes.split('-').map(Number);
  const indice = ano * 12 + mesDoAno - 1 + meses;
  return `${String(Math.floor(indice / 12)).padStart(4, '0')}-${String(indice % 12 + 1).padStart(2, '0')}`;
}
