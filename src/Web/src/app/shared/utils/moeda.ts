export function valorApi(valor: string) {
  const normal = valor.trim().replace(',', '.');
  if (!/^-?\d{1,16}(\.\d{1,2})?$/.test(normal)) throw new Error('Digite um valor com até duas casas decimais, sem separador de milhar.');
  return normal;
}
export function centavos(valor: string): bigint {
  const negativo = valor.startsWith('-');
  const [inteiro, fracao = ''] = valor.replace(/^-/, '').split('.');
  return (BigInt(inteiro) * 100n + BigInt(fracao.padEnd(2, '0').slice(0, 2))) * (negativo ? -1n : 1n);
}
