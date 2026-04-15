# MOTORCORE CRM - Backend

Este projeto é a API backend do sistema MOTORCORE, responsável por gerenciar dados, regras de negócio e autenticação para oficinas mecânicas e funilarias.

## Funcionalidades Principais

- **Autenticação e autorização**
  - Cadastro e login de usuários
  - Recuperação e redefinição de senha
  - Controle de acesso por planos e permissões (Identity + JWT)
- **Gestão de empresas (tenants)**
  - Criação, edição e consulta de empresas
  - Controle de renovação de plano
- **Gestão de lojas, funcionários, clientes e veículos**
  - CRUD completo para unidades, usuários, clientes e veículos
  - Vinculação de clientes e veículos às lojas
- **Gestão de ordens de serviço (OS)**
  - Criação, edição, exclusão e consulta de OS
  - Controle de peças, serviços e status
  - Geração automática de PDF das OSs
- **Importação de planilhas (CSV/Excel)**
  - Agrupamento de veiculos por loja
- **Limites e regras por plano**
  - Bloqueio automático de funcionalidades conforme expiração do plano

## Tecnologias Utilizadas

- **C# 14 / .NET 10**
- **ASP.NET Core Web API**  
- **Entity Framework Core**  
- **Pomelo.EntityFrameworkCore.MySql**  
- **MySQL**  
- **ASP.NET Core Identity**  
- **JWT Bearer Authentication**  
- **Playwright**  
- **Mailpit**  

## Como Executar

1. Instale as dependências do .NET:

    ```sh
    dotnet restore
    ```

2. Execute as migrations para criar o banco de dados:

    ```sh
    dotnet ef database update
    ```

3. Configure as variáveis de ambiente em `.env`.

4. Inicie o projeto:

    ```sh
    dotnet run
    ```

5. Acesse a API em:
    ```
    http://localhost:5000
    ```
