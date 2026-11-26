export interface Alerta {
  id: string | null;
  tipo: string;
  gravidade: string;
  competencia: string;
  mensagem: string;
  referencia: string;
  lidoEm: string | null;
}
