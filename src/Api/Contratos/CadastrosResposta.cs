using Dominio;

namespace Api.Contratos;

public sealed record CadastrosResposta(Conta[] Contas, Categoria[] Categorias, Cartao[] Cartoes, Estabelecimento[] Estabelecimentos);
