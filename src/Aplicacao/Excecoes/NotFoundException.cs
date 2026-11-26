namespace Aplicacao;

public sealed class NotFoundException(string mensagem) : BusinessException(mensagem);
