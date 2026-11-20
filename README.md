# BookClub

Aplicação web para organização de clubes do livro, desenvolvida em ASP.NET Core MVC (.NET 10), Entity Framework Core e PostgreSQL.

## Tecnologias

- C# / ASP.NET Core MVC (.NET 10)
- Entity Framework Core (Code-First com Migrations)
- PostgreSQL (Npgsql)
- Bootstrap 5
- Google Books API (com fallback para Open Library e cadastro manual)

## Pré-requisitos para rodar localmente

- .NET 10 SDK
- PostgreSQL instalado e em execução

## Como configurar e executar

1. Crie um banco de dados vazio no PostgreSQL (por exemplo, `bookclub`).
2. Ajuste a string de conexão no arquivo `BookClub/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bookclub;Username=postgres;Password=SUA_SENHA"
  }
}
```

3. Na raiz do projeto, execute:

```bash
dotnet run --project BookClub/BookClub.csproj
```

As tabelas (`user`, `club`, `user_club`, `book` e `club_book`) são criadas automaticamente na primeira execução pelas migrations do EF Core.

Caso prefira rodar as migrations manualmente:

```bash
dotnet tool restore
dotnet ef database update --project BookClub/BookClub.csproj
```

## Como testar as funcionalidades

1. Acesse o endereço exibido no terminal (ou pressione F5 no Visual Studio) e crie uma conta em **Criar conta**.
2. Em **Meus Clubes**, crie um novo clube (público ou privado).
3. Abra o clube criado e clique em **Adicionar Livro** para buscar pela API ou cadastrar manualmente.
4. Altere o status do livro entre **Lendo Agora**, **Próximo** e **Concluído** diretamente na tela do clube.
5. Para testar a gestão de membros, crie uma segunda conta em uma janela anônima:
   - Em clubes públicos, acesse **Explorar Clubes** e envie uma solicitação de entrada. Aprove, recuse, remova ou bana o usuário pela aba **Membros** na conta do administrador.
   - Em clubes privados, utilize o código de convite gerado na tela do clube.

## Variáveis de ambiente para deploy (Docker / Render / Supabase)

O projeto possui um `Dockerfile` na raiz pronto para publicação. Basta configurar as variáveis abaixo no servidor:

- `DATABASE_URL`: URI de conexão do PostgreSQL (ex.: Session Pooler do Supabase na porta 5432)
- `ASPNETCORE_ENVIRONMENT`: `Production`
- `Database__AutoMigrate`: `true`
