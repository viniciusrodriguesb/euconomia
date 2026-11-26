import { expect, it, vi } from 'vitest';

it('inicializa o shell e encaminha falhas de bootstrap ao console', async () => {
  const falha = new Error('Bootstrap indisponível');
  const bootstrapApplication = vi.fn().mockRejectedValue(falha);
  const log = vi.spyOn(console, 'error').mockImplementation(() => undefined);
  vi.doMock('@angular/platform-browser', async importar => ({ ...await importar<object>(), bootstrapApplication }));
  try {
    await import('./main');
    expect(bootstrapApplication).toHaveBeenCalledOnce();
    expect(log).toHaveBeenCalledWith(falha);
  } finally {
    vi.doUnmock('@angular/platform-browser');
  }
});
