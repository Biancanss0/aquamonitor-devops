# language: pt
@saude
Funcionalidade: Disponibilidade e documentação da plataforma
  Como equipe de operações
  Quero verificar se a API e o banco de dados estão saudáveis e documentados
  Para garantir o monitoramento contínuo da água, sem interrupções

  @positivo @contrato @esg-eficiencia
  Cenário: Health check informa API e banco de dados saudáveis
    Quando eu consulto o endpoint de saúde da API
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "health"
    E o campo "status" da resposta deve ser "Healthy"
    E o banco de dados deve estar saudável
    E a resposta deve ser recebida em menos de 3000 ms

  @positivo @esg-governanca
  Cenário: Documentação OpenAPI publica todos os recursos da API
    Quando eu consulto a documentação OpenAPI
    Então o status da resposta deve ser 200
    E a documentação deve listar o caminho "/api/Auth/login"
    E a documentação deve listar o caminho "/api/Sensores"
    E a documentação deve listar o caminho "/api/Leituras"
    E a documentação deve listar o caminho "/api/Alertas"
    E a documentação deve listar o caminho "/api/Relatorios/geral"
