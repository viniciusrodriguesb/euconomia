using System.Security.Cryptography;
using System.Text.Json;
using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infraestrutura;

public sealed class UnidadeFinanceira(FinanceiroDb db)
{
    public IQueryable<T> Ler<T>(Guid usuario) where T : class, IRegistroUsuario =>
        db.Set<T>().AsNoTracking().Where(x => x.UsuarioId == usuario);

    public async Task<T> ObterAsync<T>(Guid usuario, Guid id, string mensagem, CancellationToken ct)
        where T : class, IRegistroUsuario =>
        await Ler<T>(usuario).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(mensagem);

    public async Task<IDbContextTransaction> IniciarAsync(Guid usuario, CancellationToken ct)
    {
        var transacao = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""SELECT "Id" FROM usuario WHERE "Id" = {usuario} FOR UPDATE""", ct);
            return transacao;
        }
        catch
        {
            await transacao.DisposeAsync();
            throw;
        }
    }

    public void Auditar<T>(Guid usuario, Guid id, string acao, T? antes, T? depois) where T : class
    {
        db.Add(new Auditoria
        {
            UsuarioId = usuario,
            EntidadeId = id,
            Entidade = typeof(T).Name,
            Acao = acao,
            Antes = antes is null ? null : JsonSerializer.Serialize(antes),
            Depois = depois is null ? null : JsonSerializer.Serialize(depois)
        });
    }

    public async Task<ResultadoLancamento> ExecutarIdempotenteAsync<T>(Guid usuario, Guid chave, T comando,
        Func<Task<ResultadoLancamento>> executar, CancellationToken ct)
    {
        if (chave == Guid.Empty)
            throw new BusinessException("Informe a chave de idempotência da operação.");

        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { Tipo = typeof(T).Name, comando })));
        await using var transacao = await IniciarAsync(usuario, ct);
        var existente = await Ler<RegistroIdempotencia>(usuario).SingleOrDefaultAsync(x => x.ChaveIdempotencia == chave, ct);
        if (existente is not null)
        {
            if (existente.ImpressaoDigital != hash)
                throw new BusinessException("A chave já foi usada com outros dados. Gere uma nova chave para este lançamento.");
            return JsonSerializer.Deserialize<ResultadoLancamento>(existente.Resposta)!;
        }

        var resposta = await executar();
        db.Add(new RegistroIdempotencia
        {
            UsuarioId = usuario,
            ChaveIdempotencia = chave,
            ImpressaoDigital = hash,
            Resposta = JsonSerializer.Serialize(resposta)
        });
        await CommitAsync(transacao, ct);
        return resposta;
    }

    public async Task CommitAsync(IDbContextTransaction transacao, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            await transacao.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcorrenciaException();
        }
    }
}
