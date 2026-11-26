import type { EvolucaoGrafico } from './evolucao-grafico.dto';
import { MesBrPipe } from '../pipes/mes-br.pipe';
import { centavos } from '../utils/moeda';

export function desenharEvolucao(canvas: HTMLCanvasElement | undefined, dados: EvolucaoGrafico[]) {
  if (!canvas || !canvas.clientWidth || !dados.length) return;

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  const largura = canvas.clientWidth;
  const altura = canvas.clientHeight;
  const fator = window.devicePixelRatio || 1;
  canvas.width = Math.round(largura * fator);
  canvas.height = Math.round(altura * fator);
  ctx.scale(fator, fator);
  const pontos = dados.map(linha => ({
    mes: new MesBrPipe().transform(linha.mes).split('/')[0],
    entradas: Number(centavos(linha.entradas)),
    saidas: Number(typeof linha.saidas === 'bigint' ? linha.saidas : centavos(linha.saidas)),
    saldo: Number(centavos(linha.saldo)), ajustado: linha.ajustado
  }));
  const valores = [0, ...pontos.flatMap(p => [p.entradas, p.saidas, p.saldo])];
  const max = Math.max(1, ...valores) * 1.1;
  const min = Math.min(0, ...valores) * 1.1;
  const piso = altura - 27;
  const passo = (largura - 12) / pontos.length;
  const x = (i: number) => 6 + passo * (i + .5);
  const y = (v: number) => 12 + (max - v) * (piso - 12) / (max - min);
  ctx.strokeStyle = '#e3ebf5';
  for (let i = 0; i <= 4; i++) {
    const linha = 12 + i * (piso - 12) / 4;
    ctx.beginPath(); ctx.moveTo(5, linha); ctx.lineTo(largura - 5, linha); ctx.stroke();
  }
  const barra = Math.min(17, passo / 3.2);
  ctx.font = '11px Segoe UI, sans-serif';
  ctx.textAlign = 'center';
  pontos.forEach((p, i) => {
    const origem = y(0);
    ctx.fillStyle = '#3264d3';
    ctx.fillRect(x(i) - barra - 1, y(p.entradas), barra, origem - y(p.entradas));
    ctx.fillStyle = '#6e9fe0';
    ctx.fillRect(x(i) + 1, y(p.saidas), barra, origem - y(p.saidas));
    if (i % Math.max(1, Math.ceil(30 / passo)) === 0) {
      ctx.fillStyle = '#50617b';
      ctx.fillText(p.mes, x(i), altura - 8);
    }
  });
  ctx.strokeStyle = '#157858';
  ctx.fillStyle = '#157858';
  ctx.lineWidth = 2;
  ctx.beginPath();
  pontos.forEach((p, i) => i === 0 ? ctx.moveTo(x(i), y(p.saldo)) : ctx.lineTo(x(i), y(p.saldo)));
  ctx.stroke();
  pontos.forEach((p, i) => {
    ctx.beginPath();
    ctx.arc(x(i), y(p.saldo), p.ajustado ? 4.5 : 2.8, 0, 2 * Math.PI);
    ctx.fillStyle = p.ajustado ? '#fff' : '#157858';
    ctx.fill();
    if (p.ajustado) { ctx.strokeStyle = '#a76015'; ctx.stroke(); }
  });
}
