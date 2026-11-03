# language: pt
@alertas
Funcionalidade: Tratamento de alertas com controle de acesso e rastreabilidade
  Como operador da central de monitoramento
  Quero consultar e tratar os alertas gerados pelos sensores
  Para responder rápido aos desperdícios, com registro de quem pode fazer o quê e quando

  # ESG (G - Governança): alertas só com token JWT; excluir alerta é exclusivo do perfil Admin
  # (segregação de funções); resolver um alerta grava a data/hora (trilha de auditoria).

  Contexto:
    Dado que existe um sensor "Comercial" cadastrado com limite de 500 litros
    E que o sensor gerou um alerta de vazamento

  @negativo @esg-governanca
  Cenário: Acesso aos alertas sem autenticação é bloqueado
    Quando eu listo os alertas sem informar token
    Então o status da resposta deve ser 401

  @negativo @esg-governanca
  Cenário: Token adulterado não dá acesso aos alertas
    Quando eu listo os alertas com o token "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJoYWNrZXIifQ.assinatura-falsa"
    Então o status da resposta deve ser 401

  @positivo @contrato
  Cenário: Operador autenticado lista os alertas críticos do sensor
    Dado que eu estou autenticado como operador
    Quando eu listo os alertas do sensor com nível "critico"
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "alertas-paginado"
    E o total de registros deve ser 1
    E todos os registros devem ter o campo "Nivel" igual a "CRITICO"

  @positivo @contrato
  Cenário: Consultar um alerta pelo identificador
    Dado que eu estou autenticado como operador
    Quando eu consulto o alerta pelo seu identificador
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "alerta"
    E o campo "Tipo" da resposta deve ser "Vazamento"
    E o campo "Resolvido" da resposta deve ser "false"

  @negativo @contrato
  Cenário: Consultar um alerta inexistente
    Dado que eu estou autenticado como operador
    Quando eu consulto o alerta de identificador 999999
    Então o status da resposta deve ser 404
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "Alerta 999999 não encontrado."

  @positivo @contrato @esg-governanca
  Cenário: Resolver um alerta registra a data e a hora da resolução
    Dado que eu estou autenticado como operador
    Quando eu marco o alerta como resolvido
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "alerta"
    E o alerta deve estar resolvido com data de resolução registrada

  @negativo @contrato
  Cenário: Um alerta já resolvido não pode ser resolvido de novo
    Dado que eu estou autenticado como operador
    E que o alerta já foi resolvido
    Quando eu marco o alerta como resolvido
    Então o status da resposta deve ser 400
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "Alerta já está resolvido."

  @negativo
  Cenário: Resolver um alerta inexistente
    Dado que eu estou autenticado como operador
    Quando eu marco o alerta de identificador 999999 como resolvido
    Então o status da resposta deve ser 404
    E a mensagem de erro deve ser "Alerta 999999 não encontrado."

  @negativo @esg-governanca
  Cenário: Operador não tem permissão para excluir alertas (segregação de funções)
    Dado que eu estou autenticado como operador
    Quando eu excluo o alerta
    Então o status da resposta deve ser 403
    E o alerta ainda deve existir

  @positivo @admin @esg-governanca
  Cenário: Administrador exclui um alerta
    Dado que eu estou autenticado como administrador
    Quando eu excluo o alerta
    Então o status da resposta deve ser 204
    E o alerta não deve mais ser encontrado

  @negativo @admin @contrato
  Cenário: Administrador tenta excluir um alerta inexistente
    Dado que eu estou autenticado como administrador
    Quando eu excluo o alerta de identificador 999999
    Então o status da resposta deve ser 404
    E a resposta deve respeitar o contrato "erro-mensagem"
