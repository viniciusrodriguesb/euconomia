# Frontend do Euconomia

O Angular carrega as telas sob demanda em `src/app/features`. Cada tela mantém seu componente, template e testes juntos. `src/app/core` concentra transporte HTTP, interceptor de carregamento, sessão, guarda de rota e contratos da API. `src/app/shared` reúne widgets, pipes e formatação reutilizáveis.

Tailwind CSS passa pelo PostCSS em `.postcssrc.json`. Use utilitários nos templates e tokens em `src/styles.css`. Os estilos de componentes concentram seletores internos e efeitos que precisam de regras próprias, como tabelas empilhadas e o gráfico de rosca.

## Verificação local

Inicie o PostgreSQL e execute a API pelo perfil `http` no Visual Studio, na porta 5202. Com Node 22, execute `npm start` ou `npx ng serve` nesta pasta e abra `http://127.0.0.1:4200`.

O servidor de desenvolvimento do Angular configura automaticamente o proxy de `/api/**` para `http://127.0.0.1:5202` em `angular.json` e `proxy.conf.json`. As chamadas relativas preservam a sessão por cookie e o token antifalsificação. Ao alterar a porta HTTP da API, atualize o destino no proxy e reinicie o Angular.

Com Node 22, execute na pasta `src/Web`:

```sh
npm ci
npm run typecheck
npm run lint
npm run test:unit
npm run build
```

`npm run test:unit` usa Vitest com componentes compilados pelo Angular e aplica **90% mínimos por arquivo executável** para linhas, instruções, funções e ramificações. Os DTOs declarativos são verificados pela compilação TypeScript. O relatório HTML fica em `coverage/index.html` e o LCOV em `coverage/lcov.info`.

`npm run lint` executa o perfil recomendado do SonarJS. Para uma análise no SonarQube Server, execute os testes primeiro e use o `sonar-project.properties` desta pasta para importar o LCOV.

O script `./TestarNavegador.ps1` na raiz executa o teste de integração com o `playwright.config.ts` e um banco temporário. O teste valida cadastro, compra parcelada, recorrências, pagamentos, paginação e responsividade.

O build de produção verifica os limites de tamanho do bundle inicial e dos arquivos JavaScript em `angular.json`. Use `npm run build -- --stats-json` para inspecionar a composição dos chunks.
